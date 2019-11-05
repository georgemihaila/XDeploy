using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an update manager.
    /// </summary>
    /// <seealso cref="XDeploy.Client.Infrastructure.IUpdateManager" />
    public class UpdateManager : UpdateManagerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManager"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        public UpdateManager(XDeployAPI api, IEnumerable<ApplicationInfo> apps, ISyncSignalNotifier notifier) : base(api, notifier)
        {
            if (apps is null)
                throw new ArgumentNullException(nameof(apps));

            _synchronizers = apps.Select(x => new ApplicationUpdater(api, x));
        }
    }
}
