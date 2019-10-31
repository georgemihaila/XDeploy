using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            var path = args[4];
            Console.Clear();
            var api = new XDeployAPI(endpoint, email, key);

            Console.WriteLine("Validating credentials...");
            if (await api.ValidateCredentialsAsync())
            {
                Console.WriteLine("Credentials ok.");
                Console.WriteLine("Getting app details...");
                try
                {
                    Console.WriteLine(await api.GetAppDetailsAsync(appid));
                }
                catch
                {
                    Console.WriteLine("Invalid app ID.");
                    return;
                }
                Console.WriteLine("Building path tree...");
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path: {0}", path);
                    return;
                }
                string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                Console.WriteLine("Found {0} file{1}", allFiles.Length, (allFiles.Length != 1) ? "s" : string.Empty);
                Console.WriteLine("Calculating hashes...");
                var localFileHashPairs = new List<(string File, string Hash)>();
                foreach(var file in allFiles)
                {
                    var pair = (file, SHA256CheckSum(file));
                    localFileHashPairs.Add(pair);
                    Console.WriteLine("{1}", pair.file, pair.Item2);
                }
                Console.WriteLine("Getting remote file versions...");
                var remoteFileHashPairs = await api.GetRemoteFilesForAppAsync(appid);
                foreach (var pair in remoteFileHashPairs)
                {
                    Console.WriteLine("{0}", pair.Hash);
                }
                Console.WriteLine($"Local files: {localFileHashPairs.Count}{NL}Remote files: {remoteFileHashPairs.Count}");
                Console.WriteLine("Calculating differences...");
            }
            else
            {
                Console.WriteLine("Invalid credentials.");
                return;
            }
        }

        private static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                byte[] bytes = null;
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    bytes = SHA256.ComputeHash(fileStream);
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
