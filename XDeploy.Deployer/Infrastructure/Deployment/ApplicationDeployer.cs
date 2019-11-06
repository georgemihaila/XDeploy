using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;
using XDeploy.Core.IO.Extensions;
using XDeploy.Core.Extensions;
using System.Threading;

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
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        public override async Task<SynchronizationResult> SynchronizeAsync()
        {
            SynchronizationResult result = null;
            
            var remoteTree = await _api.GetRemoteTreeAsync(_app);
            _localTree = new Tree(_app.Location);
            _localTree.Relativize();
            var diffs = remoteTree.Diff(_localTree, Tree.FileUpdateCheckType.Checksum);
            var toBeUploaded = diffs
                .Where(x => x.Type == IODifference.ObjectType.File && (x.DifferenceType == IODifference.IODifferenceType.Addition || x.DifferenceType == IODifference.IODifferenceType.Update))
                .Select(x => new ExpectedFileInfo()
            {
                Filename = x.Path.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\'),
                Checksum = x.Checksum
            });

            //Ensure removed files and directories are also removed on the server so we don't take up too much of the owner's space. jk, not doing so will (also) fill the deployment computer(s) with no-longer-necessary files.
            await _api.DoCleanupAsync(_app, diffs);
            //It doesn't matter whether we have new files to upload
            if (toBeUploaded.Count() != 0)
            {
                result = new SynchronizationResult();
                LogToConsole("Upload started");

                var jobid = await _api.CreateDeploymentJobAsync(_app, toBeUploaded);

                var chunks = toBeUploaded.ChunkBy(FILES_CHUNK_SIZE); //Do multiple uploads at once instead of one at a time
                foreach (var chunk in chunks)
                {
                    var uploadTasks = chunk.Select(x => Task.Run(async() => 
                    {
                        try
                        {
                            LogToConsole($"Uploading {x.Filename}...");
                            var res = await _api.UploadFileIfNotExistsAsync(_app, jobid, x.Filename, x.Checksum, _fileManager.GetFileBytes(x.Filename));
                            if (res != "Exists")
                            {
                                result.NewFiles++;
                            }
                        }
                        catch (Exception e)
                        {
                            LogToConsole($"Error uploading file {x.Filename} ({e.GetType().ToString()})");
                        }
                    }));
                    await Task.WhenAll(uploadTasks);
                }
                await _api.DeleteDeploymentJobAsync(_app, jobid);

                LogToConsole("Upload completed");
            }
            else
            {
                LogToConsole("Up to date");
            }

            return result;
        }
    }
}
