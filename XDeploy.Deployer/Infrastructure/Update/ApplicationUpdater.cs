﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.Extensions;
using XDeploy.Core.IO;
using XDeploy.Core.IO.Extensions;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application updater.
    /// </summary>
    /// <seealso cref="XDeploy.Client.Infrastructure.IApplicationSynchronizer" />
    public class ApplicationUpdater : ApplicationSynchronizerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationUpdater"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="app">The application.</param>
        public ApplicationUpdater(XDeployAPI api, ApplicationInfo app) : base(api, app)
        {

        }

        /// <summary>
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        public override async Task<SynchronizationResult> SynchronizeAsync()
        {
            var result = new SynchronizationResult();

            var remote = await _api.GetRemoteFilesAsync(_app);
            var diffs = IOExtensions.Diff(remote, _fileManager.AsFileInfoCollection());
            _fileManager.Cleanup(diffs.Where(x => x.DifferenceType == IODifference.IODifferenceType.Removal));
            var toBeDownloaded = diffs
               .Where(x => x.Type == IODifference.ObjectType.File && (x.DifferenceType == IODifference.IODifferenceType.Addition || x.DifferenceType == IODifference.IODifferenceType.Update));
            if (toBeDownloaded.Count() != 0)
            {
                _fileManager.Cleanup(diffs.Where(x => x.DifferenceType == IODifference.IODifferenceType.Removal));
                //Pre-deployment actions
                var cmd = new CommandLine(_app.Location);
                _app.PredeployActions?.ToList().ForEach(action => 
                {
                    cmd.Invoke(action);
                });

                result = new SynchronizationResult();
                LogToConsole("Download started");

                var chunks = toBeDownloaded.ChunkBy(MAX_UPLOAD_COUNT); //Do multiple downloads at once instead of one at a time

                foreach (var chunk in chunks)
                {
                    var uploadTasks = chunk.Select(x => Task.Run(async () =>
                    {
                        try
                        {
                            LogToConsole($"Downloading {x.Path}...");
                            var bytes = await _api.DownloadFileBytesAsync(_app, x.Path);
                            _fileManager.WriteFileBytes(x.Path, bytes);
                        }
                        catch (Exception e)
                        {
                            LogToConsole($"Error downloading file {x.Path} ({e.GetType().ToString()})");
                        }
                    }));
                    await Task.WhenAll(uploadTasks);
                }

                //Post-Deployment actions
                _app.PostdeployActions?.ToList().ForEach(action =>
                {
                    cmd.Invoke(action);
                });
                cmd.Close();
                LogToConsole("Download completed");
            }
            else
            {
                LogToConsole("Up to date");
            }
            return result;
        }
    }
}
