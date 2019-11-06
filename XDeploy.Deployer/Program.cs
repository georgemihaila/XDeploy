using System;
using System.Threading;
using System.Threading.Tasks;
using XDeploy.Client.Infrastructure.Builders;
using XDeploy.Core;

namespace XDeploy.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Specify an input configuration file as an argument.");
                return;
            }
            var config = StartupConfig.FromJsonFile(args[0]);
            var manager = await (new UpdateManagerBuilder(config)).BuildAsync();
            await manager.SynchronizeAsync();
            manager.StartListening();
            Console.CancelKeyPress += (_, __) =>
            {
                Console.WriteLine("Terminating app...");
                manager.StopListening();
            };
            while (true)
            {
                await Task.Delay(500);
            }
        }
    }
}
