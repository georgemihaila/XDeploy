using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a base class for an update manager.
    /// </summary>
    public abstract class UpdateManagerBase : IUpdateManager
    {
        protected readonly XDeployAPI _api;
        protected readonly ISyncSignalNotifier _notifier;
        protected IEnumerable<IApplicationSynchronizer> _synchronizers;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateManagerBase"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <exception cref="ArgumentNullException">api</exception>
        protected UpdateManagerBase(XDeployAPI api, ISyncSignalNotifier signalNotifier)
        {
            if (api is null)
                throw new ArgumentNullException(nameof(api));
            if (signalNotifier is null)
                throw new ArgumentNullException(nameof(signalNotifier));

            _api = api;
            _notifier = signalNotifier;
            _notifier.SyncSignalReceived += async (_, id) =>
            {
                Func<IApplicationSynchronizer, bool> idSelector = x => x.ApplicationID == id;
                if (_synchronizers.Any(idSelector))
                {
                    var result = await _synchronizers.First(idSelector).SynchronizeAsync();
                    Console.WriteLine(result);
                }
            };
        }

        /// <summary>
        /// Does the initial synchronization for all registered applications.
        /// </summary>
        public async Task DoInitialSyncAsync()
        {
            foreach (var deployer in _synchronizers)
            {
                var result = await deployer.SynchronizeAsync();
                Console.WriteLine(result);
            }
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void StartListener() => _notifier.StartListening();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public void StopListener() => _notifier.StopListening();
    }
}
