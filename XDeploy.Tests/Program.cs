using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO.FileManagement;
using XDeploy.Server.Infrastructure.Data.MongoDb;

namespace XDeploy.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var fm = new DiskFileManager("C:\\Users\\gmihaila\\source\\repos\\GSK\\Reassigner\\Reassigner\\bin\\Debug\\netcoreapp2.1");

            var x = fm.AsFileInfoCollection();
            ;
        }
    }
}
