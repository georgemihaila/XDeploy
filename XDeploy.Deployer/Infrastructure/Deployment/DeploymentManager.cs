using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;
using XDeploy.Core.IO.Extensions;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application update manager.
    /// </summary>
    public class DeploymentManager : UpdateManagerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentManager"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="apps">The applications.</param>
        public DeploymentManager(XDeployAPI api, IEnumerable<ApplicationInfo> apps, ISyncSignalNotifier notifier) : base(api, notifier)
        {
            if (apps is null)
                throw new ArgumentNullException(nameof(apps));

            _synchronizers = apps.Select(x => new ApplicationDeployer(api, x));
        }
    }
}
