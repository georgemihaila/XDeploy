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
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        public override Task<SynchronizationResult> SynchronizeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
