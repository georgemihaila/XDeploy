using System;
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
            await manager.DoInitialSyncAsync();
            manager.StartListener();
            Console.CancelKeyPress += (_, __) =>
            {
                manager.StopListener();
            };
            while (true)
            {
                await Task.Delay(500);
            }
        }
    }
}
