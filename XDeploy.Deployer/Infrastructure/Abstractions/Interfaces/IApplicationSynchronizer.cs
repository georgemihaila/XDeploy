using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XDeploy.Client.Infrastructure
{
    /// <summary>
    /// Represents an application synchronizer.
    /// </summary>
    public interface IApplicationSynchronizer
    {
        /// <summary>
        /// Checks for local file changes, compares their versions to the ones on the server and synchronizes them if required.
        /// </summary>
        Task<SynchronizationResult> SynchronizeAsync();

        /// <summary>
        /// Gets the application ID.
        /// </summary>
        string ApplicationID { get; }
    }
}
