using System;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Notifies clients that a synchronization signal was received.
    /// </summary>
    public interface INotifySyncSignalReceived
    {
        /// <summary>
        /// Occurs when the server receives a correct synchronization signal.
        /// </summary>
        public event EventHandler<string> SyncSignalReceived;
    }
}
