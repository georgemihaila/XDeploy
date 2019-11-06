using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XDeploy.Core;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a base class for an update manager.
    /// </summary>
    public class UpdateManager<T> : IUpdateManager
        where T: IApplicationSynchronizer
    {
        protected readonly XDeployAPI _api;
        protected readonly ISyncSignalNotifier _notifier;
        protected IEnumerable<T> _applicationSynchronizers;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManagerBase"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <exception cref="ArgumentNullException">api</exception>
        public UpdateManager(XDeployAPI api, ISyncSignalNotifier signalNotifier, IEnumerable<T> applicationSynchronizers)
        {
            if (api is null)
                throw new ArgumentNullException(nameof(api));
            if (signalNotifier is null)
                throw new ArgumentNullException(nameof(signalNotifier));
            if (applicationSynchronizers is null)
                throw new ArgumentNullException(nameof(applicationSynchronizers));

            _api = api;
            _notifier = signalNotifier;
            _applicationSynchronizers = applicationSynchronizers;
            _notifier.SyncSignalReceived += async (_, id) =>
            {
                Func<T, bool> idSelector = x => x.ApplicationID == id;
                if (_applicationSynchronizers.Any(idSelector))
                {
                    _ = await _applicationSynchronizers.First(idSelector).SynchronizeAsync();
                }
            };
        }

        /// <summary>
        /// Does the initial synchronization for all registered applications.
        /// </summary>
        public async Task<SynchronizationResult> SynchronizeAsync()
        {
            var result = new SynchronizationResult();
            foreach (var deployer in _applicationSynchronizers)
            {
                var res = await deployer.SynchronizeAsync();
                if (res != null)
                {
                    result += res;
                }
            }
            return result;
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void StartListening() => _notifier.StartListening();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public void StopListening() => _notifier.StopListening();
    }
}
