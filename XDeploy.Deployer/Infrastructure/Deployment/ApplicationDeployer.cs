using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a synchronization manager for an app.
    /// </summary>
    public class ApplicationDeployer : ApplicationSynchronizerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDeployer"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="app">The application.</param>
        public ApplicationDeployer(XDeployAPI api, ApplicationInfo app) : base(api, app)
        {
            
        }

        /// <summary>
        /// Does a force synchronization.
        /// </summary>
        protected override async Task<SynchronizationResult> ForceSyncAsync()
        {
            var result = new SynchronizationResult();
            //Force push everything
            var expectedFiles = _localTree.AllFiles.Select(x => new ExpectedFileInfo()
            {
                Filename = x.Name,
                Checksum = Cryptography.SHA256CheckSum(Path.Join(_app.Location, x.Name))
            });
            var jobid = await _api.CreateDeploymentJobAsync(_app, expectedFiles);
            foreach (var file in expectedFiles)
            {
                try
                {
                    var dFile = _fileManager.GetFileChecksumAndBytes(file.Filename);
                    var res = await _api.UploadFileIfNotExistsAsync(_app, jobid, file.Filename, dFile.Checksum, dFile.Bytes);
                    if (res != "Exists")
                    {
                        result.NewFiles++;
                    }
                }
                catch
                {
                    Console.WriteLine($"Error uploading file {file.Filename}");
                }
            }
            //Clear job
            await _api.DeleteDeploymentJobAsync(_app, jobid);
            return result;
        }

        /// <summary>
        /// Does a normal synchronization.
        /// </summary>
        protected async override Task<SynchronizationResult> NormalSyncAsync()
        {
            var result = new SynchronizationResult();

            var newTree = new Tree(_app.Location);
            newTree.Relativize();
            var localDiffs = newTree.Diff(_localTree, Tree.FileUpdateCheckType.Checksum);
            var expectedFiles = localDiffs.Where(x => x.Type == IODifference.ObjectType.File && (x.DifferenceType == IODifference.IODifferenceType.Addition || x.DifferenceType == IODifference.IODifferenceType.Update)).Select(x => new ExpectedFileInfo()
            {
                Filename = x.Path,
                Checksum = x.Checksum
            });
            var jobid = await _api.CreateDeploymentJobAsync(_app, expectedFiles);
            foreach (var file in expectedFiles)
            {
                try
                {
                    var dFile = _fileManager.GetFileChecksumAndBytes(file.Filename);
                    var res = await _api.UploadFileIfNotExistsAsync(_app, jobid, file.Filename, dFile.Checksum, dFile.Bytes);
                    if (res != "Exists")
                    {
                        result.NewFiles++;
                    }
                }
                catch
                {
                    Console.WriteLine($"Error uploading file {file.Filename}");
                }
            }
            //Clear job
            await _api.DeleteDeploymentJobAsync(_app, jobid);
            _localTree = newTree;

            return result;
        }
    }
}
