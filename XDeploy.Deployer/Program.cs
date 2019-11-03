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

namespace XDeploy.Deployer
{
    class Program
    {
        //xdd --Endpoint val --email val --key val --app val --path val
        private const string NL = "\r\n";
        private const string NLT = "\r\n\t";
        private const string ConfigFile = "config.json";
        private const string TimeFormat = "HH:mm:ss";
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
            _api = new XDeployAPI(config.Endpoint, config.Email, config.APIKey);

            //Validate credentials
            if (await _api.ValidateCredentialsAsync())
            {
                //Validate each app
                foreach(var app in config.Apps)
                {
                    try
                    {
                        _ = await _api.GetAppDetailsAsync(app.ID);
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
                }
                var server = new SyncSignalServer(config.SyncServerPort);
                server.SyncSignalReceived += async (_, id) =>
                {
                    Func<ApplicationInfo, bool> idSelector = app => app.ID == id;
                    if (config.Apps.Any(idSelector))
                    {
                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - Sync signal received for app {id}");
                        var res = await SyncFiles(config.Apps.First(idSelector));
                        Console.WriteLine($"{DateTime.Now.ToString(TimeFormat)} - {res.New} file{((res.New != 1)?"s":string.Empty)} uploaded.");
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

        private static async Task<(int AlreadyExisting, int New)> SyncFiles(ApplicationInfo application)
        {
            var result = (0, 0);
            foreach (var file in Directory.EnumerateFiles(application.Location, "*.*", SearchOption.AllDirectories))
            {
                if ((new FileInfo(file)).Length > 30 * 1024 * 1024)
                    continue; //30MB max
                var res = await _api.UploadFileIfNotExistsAsync(application.ID, application.Location, file);
                if (res == "Exists")
                {
                    result.Item1++;
                }
                else
                {
                    result.Item2++;
                }
            }
            return result;
        }
    }
}
