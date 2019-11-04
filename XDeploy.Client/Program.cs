using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;
using XDeploy.Core.IO.Extensions;

namespace XDeploy.Client
{
    class Program
    {
        private const string NL = "\r\n";
        private const string NLT = "\r\n\t";
        private const string ConfigFile = "config.json";
        private const string TimeFormat = "HH:mm:ss";
        private static XDeployAPI _api;
        private static bool _verbose;

        public async static Task Main(string[] args)
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
                foreach (var app in config.Apps)
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
                    WriteLine_Verbose($"Initial sync for app {app.ID}...");
                    await SyncFilesAsync(app);
                    WriteLine_Verbose($"Initial sync for app {app.ID} completed");
                    _api.SubscribeToUpdateWebSockets(app.ID);
                }
                Console.WriteLine("Listening for changes...");
                _api.ApplicationUpdate += async(_, id) =>
                {
                    WriteLine_Verbose($"WebSocket message received; data: \"{id}\"");
                    Func<ApplicationInfo, bool> idSelector = app => app.ID == id;
                    if (config.Apps.Any(idSelector))
                    {
                        var app = config.Apps.First(idSelector);
                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - {id} - Updating app...");
                        var changes = await SyncFilesAsync(app);
                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - {id} - Updated ({changes} change{(changes != 1 ? "s" : string.Empty)})");
                    }
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

        private static async Task<int> SyncFilesAsync(ApplicationInfo application)
        {
            if (application is null)
                throw new ArgumentNullException(nameof(application));

            var remoteTree = await _api.GetRemoteTreeAsync(application.ID);
            //Create local required directories
            foreach (var topDir in remoteTree.BaseDirectory.Subdirectories) 
            {
                CreateDirectoriesFromDirectoryInfo(application.Location, topDir);
            }
            return await DownloadFilesFromTree(application, remoteTree.BaseDirectory);
        }

        private static async Task<int> DownloadFilesFromTree(ApplicationInfo application, Core.IO.DirectoryInfo baseDir)
        {
            foreach(var file in baseDir.Files)
            {
                var dstFilePath = Path.Join(application.Location, file.Name);
                if (File.Exists(dstFilePath))
                {
                    if (Cryptography.ComputeSHA256(dstFilePath) == file.SHA256CheckSum)
                        continue; //Ignore already synced files; doesn't work for encrypted files because we delete them right away (maybe we shouldn't; or we should have a separate (cached) tree for them :/)
                    File.Delete(dstFilePath);
                }
                var bytes = await _api.DownloadFileAsync(application.ID, file.Name);
                await File.WriteAllBytesAsync(dstFilePath, bytes);
                if (application.Encrypted)
                {
                    Cryptography.AES256FileDecrypt(dstFilePath, dstFilePath.Replace(".enc", string.Empty), application.EncryptionKey);
                    File.Delete(dstFilePath);
                }
            }
            foreach(var subdir in baseDir.Subdirectories)
            {
                return 1 + await DownloadFilesFromTree(application, subdir);
            }
            return 0;
        }

        private static void CreateDirectoriesFromDirectoryInfo(string basePath, Core.IO.DirectoryInfo info)
        {
            Directory.CreateDirectory(Path.Join(basePath, info.Name));
            foreach(var dir in info.Subdirectories)
            {
                CreateDirectoriesFromDirectoryInfo(Path.Join(basePath, dir.Name), dir);
            }
        }

        /// <summary>
        /// Creates a new directory called "encrypted" in the application base directory, encrypts all files in the application base directory and places them there.
        /// </summary>
        /// <param name="application">The application.</param>
        private static void CreateApplicationEncryptedVersion(ApplicationInfo application)
        {
            var encPath = Path.Join(application.Location, "encrypted");
            if (Directory.Exists(encPath))
            {
                Directory.Delete(encPath, true);
            }
            Directory.CreateDirectory(encPath);

            var allFiles = Directory.EnumerateFiles(application.Location, "*.*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".enc"));
            foreach (var file in allFiles)
            {
                var relativeDir = Path.Combine(file.Split(Path.DirectorySeparatorChar)[..^1]).Replace(application.Location, string.Empty);
                Directory.CreateDirectory(Path.Combine("encrypted", relativeDir));
                var dstPath = Path.Join(application.Location, "encrypted", file.Replace(application.Location, string.Empty));
                Cryptography.AES256FileEncrypt(file, application.EncryptionKey);
                var dstFilename = dstPath + ".enc";
                File.Move(file + ".enc", dstFilename);
            }
        }

        private static void WriteLine_Verbose(params object[] args)
        {
            if (_verbose)
            {
                foreach (var x in args)
                {
                    Console.WriteLine(x);
                }
            }
        }
    }
}
