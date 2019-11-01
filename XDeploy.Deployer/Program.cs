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
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path: {0}", path);
                    return;
                }
                Console.WriteLine("Determining local tree...");
                var localTree = new Tree(path);
                localTree.Relativize();
                Console.WriteLine("Getting remote tree...");
                var remoteTree = await api.GetRemoteTree(appid);
                var diff = remoteTree.Diff(localTree, Tree.FileUpdateCheckType.Checksum);
                Console.WriteLine($"Differences:{NL}Files:{NLT}" +
                    $"{diff.Count(x=>x.DifferenceType == IODifference.IODifferenceType.Addition && x.Type == IODifference.ObjectType.File)} new{NLT}" +
                    $"{diff.Count(x => x.DifferenceType == IODifference.IODifferenceType.Update && x.Type == IODifference.ObjectType.File)} updated{NLT}" +
                    $"{diff.Count(x => x.DifferenceType == IODifference.IODifferenceType.Removal && x.Type == IODifference.ObjectType.File)} removed{NL}" +
                    $"Directories:{NLT}" +
                    $"{diff.Count(x => x.DifferenceType == IODifference.IODifferenceType.Addition && x.Type == IODifference.ObjectType.Directory)} new{NLT}" +
                    $"{diff.Count(x => x.DifferenceType == IODifference.IODifferenceType.Removal && x.Type == IODifference.ObjectType.Directory)} removed{NLT}");
            }
            else
            {
                Console.WriteLine("Invalid credentials.");
                return;
            }
        }

        void test()
        {
            var baseDirectory = @"C:\Users\gmihaila\source\repos\GSK\Reassigner\ReassignerWPF\bin\Debug";

            var initial = new Tree(baseDirectory);
            File.WriteAllText(Path.Join(baseDirectory, "testFile.txt"), "0");
            var checkType = Tree.FileUpdateCheckType.Checksum;
            var current = new Tree(baseDirectory);
            Console.WriteLine($"File addition{NL}" + JsonConvert.SerializeObject(initial.Diff(current, checkType), Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter())); //File addition
            initial = current;

            File.WriteAllText(Path.Join(baseDirectory, "testFile.txt"), "1");
            current = new Tree(baseDirectory);
            Console.WriteLine($"File update{NL}" + JsonConvert.SerializeObject(initial.Diff(current, checkType), Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter())); //File update
            initial = current;

            File.Delete(Path.Join(baseDirectory, "testFile.txt"));
            current = new Tree(baseDirectory);
            Console.WriteLine($"File deletion{NL}" + JsonConvert.SerializeObject(initial.Diff(current, checkType), Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter())); //File deletion
            initial = current;

            Directory.CreateDirectory(Path.Join(baseDirectory, "temp"));
            current = new Tree(baseDirectory);
            Console.WriteLine($"Directory addition{NL}" + JsonConvert.SerializeObject(initial.Diff(current, checkType), Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter())); ; //Directory addition
            initial = current;

            Directory.Delete(Path.Join(baseDirectory, "temp"));
            current = new Tree(baseDirectory);
            Console.WriteLine($"Directory deletion{NL}" + JsonConvert.SerializeObject(initial.Diff(current, checkType), Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter())); ; //Directory deletion
        }
    }
}
