using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.Exceptions;

namespace XDeploy.Client.Infrastructure.Builders
{
    /// <summary>
    /// Represents an update manager builder.
    /// </summary>
    public class UpdateManagerBuilder
    {
        private readonly StartupConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManagerBuilder"/> class.
        /// </summary>
        /// <param name="config">The startup configuration.</param>
        /// <exception cref="ArgumentNullException">config</exception>
        public UpdateManagerBuilder(StartupConfig config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            _config = config;
        }

        /// <summary>
        /// Builds (and validates) a <see cref="DeploymentManager"/> asynchronously.
        /// </summary>
        public async Task<IUpdateManager> BuildAsync()
        {
            XDeployAPI api = new XDeployAPI(_config);
            await ValidateAPIAsync(api);
            var apps = await GetApplicationDetailsAsync(api, _config.Apps);
            ISyncSignalNotifier notifier;
            IEnumerable<IApplicationSynchronizer> synchronizers;
            switch (_config.Mode)
            {
                case ApplicationMode.Deployer:
                    notifier = new SyncSignalServer(_config.SyncServerPort);
                    synchronizers = apps.Select(x => new ApplicationDeployer(api, x));
                    break;
                case ApplicationMode.Updater:
                    if (_config.Proxy != null)
                    {
                        notifier = new WebSocketsSignalNotifier(apps, _config.Endpoint, _config.Email, _config.APIKey, _config.Proxy);
                    }
                    else
                    {
                        notifier = new WebSocketsSignalNotifier(apps, _config.Endpoint, _config.Email, _config.APIKey);
                    }
                    synchronizers = apps.Select(x => new ApplicationUpdater(api, x));
                    break;
                default:
                    throw new NotImplementedException($"Mode {_config.Mode} is not supported.");
            }
            return new UpdateManager<IApplicationSynchronizer>(api, notifier, synchronizers);
        }

        /// <summary>
        /// Gets additional details for applications.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="apps">The apps.</param>
        /// <returns></returns>
        /// <exception cref="StartupException">
        /// Invalid app ID \"{app.ID}\" or unauthorized IP.
        /// or
        /// Invalid application path: {0}
        /// </exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        [Pure]
        private static async Task<IEnumerable<ApplicationInfo>> GetApplicationDetailsAsync(XDeployAPI api, IEnumerable<ApplicationInfo> apps)
        {
            foreach(var app in apps)
            {
                try
                {
                    var anon = new { encrypted = false, preDeployment = string.Empty, postDeployment = string.Empty };
                    var details = JsonConvert.DeserializeAnonymousType(await api.GetAppDetailsAsync(app.ID), anon);
                    app.Encrypted = details.encrypted;
                    app.PredeployActions = details.preDeployment?.ToString().Replace("\r", string.Empty).Split('\n').ToList();
                    app.PostdeployActions = details.postDeployment?.ToString().Replace("\r", string.Empty).Split('\n').ToList();
                }
                catch (Exception e)
                {
                    throw new StartupException($"Invalid app ID \"{app.ID}\" or unauthorized IP.", e);
                }
                if (!Directory.Exists(app.Location))
                {
                    throw new StartupException($"Invalid application path: {app.Location}", new DirectoryNotFoundException(app.Location));
                }
            }
            return apps;
        }

        /// <summary>
        /// Validates an API.
        /// </summary>
        [Pure]
        private static async Task ValidateAPIAsync(XDeployAPI api)
        {
            try
            {
                _ = await api.ValidateCredentialsAsync();
            }
            catch (Exception e)
            {
                throw new StartupException($"Invalid API credentials provided.", e);
            }
        }

    }
}
