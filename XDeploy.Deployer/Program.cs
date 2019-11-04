using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;
using XDeploy.Core.IO.Extensions;

namespace XDeploy.Deployer
{
    class Program
    {
        private const string NL = "\r\n";
        private const string NLT = "\r\n\t";
        private const string ConfigFile = "config.json";
        private const string TimeFormat = "HH:mm:ss";
        private static bool _verbose;
        private static XDeployAPI _api;
        
        static async Task Main(string[] args)
        { 
            if (!File.Exists(ConfigFile))
            {
                Console.WriteLine($"Configuration file {ConfigFile} not found.");
                return;
            }
            StartupConfig config = null;
            try
            {
                config = JsonConvert.DeserializeObject<StartupConfig>(File.ReadAllText(ConfigFile));
            }
            catch
            {
                Console.WriteLine("Malformed configuration file");
                return;
            }
            _verbose = config.Verbose;
            _api = new XDeployAPI(config.Endpoint, config.Email, config.APIKey);

            WriteLine_Verbose("Validating credentials...");
            if (await _api.ValidateCredentialsAsync())
            {
                //Validate each app
                foreach(var app in config.Apps)
                {
                    try
                    {
                        WriteLine_Verbose($"Validating app {app.ID}...");
                        dynamic details = JsonConvert.DeserializeObject(await _api.GetAppDetailsAsync(app.ID));
                        app.Encrypted = details.encrypted;
                    }
                    catch
                    {
                        Console.WriteLine($"Invalid app ID \"{app.ID}\" or unauthorized IP.");
                        return;
                    }
                    if (!Directory.Exists(app.Location))
                    {
                        Console.WriteLine("Invalid application path: {0}", app.Location);
                        return;
                    }
                    //Force push all files on server to ensure synchronization
                    WriteLine_Verbose($"Force syncing {app.ID}...");
                    var res = await ForceSyncAsync(app);
                    WriteLine_Verbose($"{res.New} new files{NL}{res.AlreadyExisting} existing");
                }


                //Build tree dictionary to calculate differences between files
                var treeDict = new Dictionary<string, Tree>();
                config.Apps.ToList().ForEach(x => 
                {
                    WriteLine_Verbose($"Building file tree for app {x.ID}");
                    treeDict.Add(x.ID, new Tree(x.Location));
                });

                var server = new SyncSignalServer(config.SyncServerPort);
                server.SyncSignalReceived += async (_, id) =>
                {
                    Func<ApplicationInfo, bool> idSelector = app => app.ID == id;
                    if (config.Apps.Any(idSelector))
                    {
                        var app = config.Apps.First(idSelector);
                        var newTree = new Tree(app.Location);
                        var diffs = newTree.Diff(treeDict[id], Tree.FileUpdateCheckType.DateTime); //Local trees use DateTime diffs because they are faster

                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - {id} - Sync signal received");
                        WriteLine_Verbose($"{DateTime.Now.ToString(TimeFormat)} - {id} - local changes:{NL}" + diffs.Format());

                        var res = await SyncFiles(config.Apps.First(idSelector), diffs);

                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - {id} - {res.New} file{((res.New != 1) ? "s" : string.Empty)} uploaded.");

                        treeDict[id] = newTree;
                    }
                };
                server.Start();
                Console.WriteLine("Server listening on http://127.0.0.1:{0}", server.Port);
                Console.WriteLine("Trigger a sync signal by making a request to http://127.0.0.1:{0}?id=appid", server.Port);
                Console.CancelKeyPress += (_, __) => 
                {
                    Console.WriteLine("Stopping server...");
                    server.Stop();
                };
                while (true)
                {
                    await Task.Delay(500);
                }
            }
            else
            {
                Console.WriteLine("Invalid credentials.");
                return;
            }
        }

        private static void ClearLocallyEncryptedFiles(ApplicationInfo application)
        {
            foreach(var x in Directory.EnumerateFiles(application.Location, "*.enc", SearchOption.AllDirectories).ToList())
            {
                File.Delete(x);
            }
        }

        private static async Task<(int AlreadyExisting, int New)> ForceSyncAsync(ApplicationInfo application)
        {
            if (application is null)
                throw new ArgumentNullException(nameof(application));
            ClearLocallyEncryptedFiles(application);

            var result = (0, 0);
            var allFiles = Directory.EnumerateFiles(application.Location, "*.*", SearchOption.AllDirectories).Select(x => x + ((application.Encrypted) ? ".enc" : string.Empty)).ToList();
            if (application.Encrypted)
            {
                allFiles.ToList().ForEach(x =>
                {
                    Cryptography.AES256FileEncrypt(x[0..^4], application.EncryptionKey);
                });
            }
            var fakeExpected = allFiles.Select(x => new FutureUploadedFileInfo() { Filename = x.Replace(application.Location, string.Empty), Checksum = Cryptography.SHA256CheckSum(x) });
            var jobid = await _api.CreateDeploymentJobAsync(application.ID, fakeExpected);
            foreach (var file in allFiles)
            {
#if DEBUG //30MB max uploads while debugging; can be changed in app.config after release; too lazy to test it r/n
                if ((new System.IO.FileInfo(file)).Length > 30 * 1024 * 1024)
                    continue;
#endif
                WriteLine_Verbose($"Uploading {file}...");
                var res = await _api.UploadFileIfNotExistsAsync(application.ID, application.Location, file, jobid);
                //Delete encrypted file
                if (application.Encrypted)
                {
                    File.Delete(file);
                }
                if (res == "Exists")
                {
                    result.Item1++;
                }
                else
                {
                    result.Item2++;
                }
            }
            await _api.DeleteDeploymentJobAsync(application.ID, jobid);
            return result;
        }

        private static async Task<(int AlreadyExisting, int New)> SyncFiles(ApplicationInfo application, IEnumerable<IODifference> diffs)
        {
            if (application is null)
                throw new ArgumentNullException(nameof(application));
            if (application.Encrypted && string.IsNullOrEmpty(application.EncryptionKey))
                throw new ArgumentNullException(nameof(application.EncryptionKey));
            if (diffs is null)
                throw new ArgumentNullException(nameof(diffs));
            ClearLocallyEncryptedFiles(application);

            Func<IODifference, bool> selector = x => (x.DifferenceType == IODifference.IODifferenceType.Addition || x.DifferenceType == IODifference.IODifferenceType.Update) &&
                 x.Type == IODifference.ObjectType.File;
            var result = (0, 0);
            var allFiles = diffs.Where(selector).Select(x => x.Path + ((application.Encrypted) ? ".enc" : string.Empty)).ToList();
            if (application.Encrypted)
            {
                allFiles.ToList().ForEach(x =>
                {
                    Cryptography.AES256FileEncrypt(x[0..^4], application.EncryptionKey);
                });
            }
            var jobid = await _api.CreateDeploymentJobAsync(application.ID, diffs.Where(selector).Select(x => new FutureUploadedFileInfo() { Filename = x.Path.Replace(application.Location, string.Empty) + ((application.Encrypted) ? ".enc" : string.Empty), Checksum = Cryptography.SHA256CheckSum(x.Path) }));
            foreach (var file in allFiles)
            {
#if DEBUG //30MB max uploads while debugging; can be changed in app.config after release; too lazy to test it r/n
                if ((new System.IO.FileInfo(file)).Length > 30 * 1024 * 1024)
                    continue;
#endif
                WriteLine_Verbose($"Uploading {file}...");
                var res = await _api.UploadFileIfNotExistsAsync(application.ID, application.Location, file, jobid);
                //Delete encrypted file
                if (application.Encrypted)
                {
                    File.Delete(file);
                }
                if (res == "Exists")
                {
                    result.Item1++;
                }
                else
                {
                    result.Item2++;
                }
            }
            await _api.DeleteDeploymentJobAsync(application.ID, jobid);
            return result;
        }

        private static void WriteLine_Verbose(params object[] args)
        {
            if (_verbose)
            {
                foreach(var x in args)
                {
                    Console.WriteLine(x);
                }
            }
        }
    }
}
