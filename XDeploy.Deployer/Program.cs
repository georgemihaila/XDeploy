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
        
        static async Task Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine($"Usage:{NLT}xdd.exe [endpoint] [email] [apikey] [appid] [path]");
                return;
            }
            var endpoint = args[0];
            var email = args[1];
            var key = args[2];
            var appid = args[3];
            var path = args[4].Replace('/', '\\');
            Console.Clear();
            var api = new XDeployAPI(endpoint, email, key);

            //Console.WriteLine("Validating credentials...");
            if (await api.ValidateCredentialsAsync())
            {
                //Console.WriteLine("Credentials ok.");
                try
                {
                    //Validate app
                    _ = await api.GetAppDetailsAsync(appid);
                }
                catch
                {
                    Console.WriteLine("Invalid app ID or unauthorized IP.");
                    return;
                }
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path: {0}", path);
                    return;
                }
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                foreach(var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    if ((new FileInfo(file)).Length > 30 * 1024 * 1024)
                        continue; //30MB max
                    await api.UploadFileIfNotExistsAsync(appid, path, file);
                }
                sw.Stop();
                Console.WriteLine("Completed in {0}ms", sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                Console.WriteLine("Invalid credentials.");
                return;
            }
        }
    }
}
