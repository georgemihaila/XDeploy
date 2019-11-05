using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;

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
        /// Does a normal synchronization.
        /// </summary>
        protected override async Task<SynchronizationResult> NormalSyncAsync()
        {
            return new SynchronizationResult();
        }

        /// <summary>
        /// Does a force synchronization.
        /// </summary>
        protected override async Task<SynchronizationResult> ForceSyncAsync()
        {
            return new SynchronizationResult();
        }
    }
}
