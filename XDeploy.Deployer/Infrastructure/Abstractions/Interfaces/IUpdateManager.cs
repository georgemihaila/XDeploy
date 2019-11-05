using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an update manager.
    /// </summary>
    public interface IUpdateManager
    {
        /// <summary>
        /// Starts the listener.
        /// </summary>
        void StartListener();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void StopListener();

        /// <summary>
        /// Does the initial sychronization.
        /// </summary>
        Task DoInitialSyncAsync();
    }
}
