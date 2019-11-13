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
            
            var remote = await _api.GetRemoteFilesAsync(_app);
            var diffs = IOExtensions.Diff(_fileManager.AsFileInfoCollection(), remote);
            await _api.DoCleanupAsync(_app, diffs.Where(x => x.DifferenceType == IODifference.IODifferenceType.Removal));
            var toBeUploaded = diffs.Where(x => x.DifferenceType == IODifference.IODifferenceType.Addition);
            if (toBeUploaded.Count() != 0)
            {
                result = new SynchronizationResult();
                LogToConsole("Upload started");

                await _api.LockApplicationAsync(_app);

                var chunks = toBeUploaded.ChunkBy(MAX_UPLOAD_COUNT); //Do multiple uploads at once instead of one at a time
                foreach (var chunk in chunks)
                {
                    var uploadTasks = chunk.Select(x => Task.Run(async() => 
                    {
                        try
                        {
                            LogToConsole($"Uploading {x.Path}...");
                            var res = await _api.UploadFileIfNotExistsAsync(_app, x.Path.Replace(_fileManager.BaseLocation, string.Empty), x.Checksum, _fileManager.GetFileBytes(x.Path));
                            if (res != "Exists")
                            {
                                result.NewFiles++;
                            }
                        }
                        catch (Exception e)
                        {
                            LogToConsole($"Error uploading file {x.Path} ({e.GetType().ToString()})");
                        }
                    }));
                    await Task.WhenAll(uploadTasks);
                }
                await _api.UnlockApplicationAsync(_app);

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
