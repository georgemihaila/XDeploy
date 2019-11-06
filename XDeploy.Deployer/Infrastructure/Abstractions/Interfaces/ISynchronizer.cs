using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents a synchronizer.
    /// </summary>
    public interface ISynchronizer
    {
        /// <summary>
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        Task<SynchronizationResult> SynchronizeAsync();
    }
}
