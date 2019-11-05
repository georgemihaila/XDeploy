using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a synchronization signal notifier.
    /// </summary>
    public interface ISyncSignalNotifier
    {
        /// <summary>
        /// Occurs when the server receives a correct synchronization signal.
        /// </summary>
        public event EventHandler<string> SyncSignalReceived;

        /// <summary>
        /// Starts listening.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening.
        /// </summary>
        void StopListening();
    }
}
