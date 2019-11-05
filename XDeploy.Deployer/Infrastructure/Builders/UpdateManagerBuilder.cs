using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
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
            switch (_config.Mode)
            {
                case ApplicationMode.Deployer:
                    notifier = new SyncSignalServer(_config.SyncServerPort);
                    return new DeploymentManager(api, apps, notifier);
                case ApplicationMode.Updater:
                    notifier = new WebSocketsSignalNotifier(apps, _config.Endpoint, _config.Email, _config.APIKey);
                    return new UpdateManager(api, apps, notifier);
                default:
                    throw new NotImplementedException($"Mode {_config.Mode} is not supported.");
            }
            
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
                    dynamic details = JsonConvert.DeserializeObject(await api.GetAppDetailsAsync(app.ID));
                    app.Encrypted = details.encrypted;
                }
                catch (Exception e)
                {
                    throw new StartupException($"Invalid app ID \"{app.ID}\" or unauthorized IP.", e);
                }
                if (!Directory.Exists(app.Location))
                {
                    throw new StartupException($"Invalid application path: {0}", new DirectoryNotFoundException(app.Location));
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
