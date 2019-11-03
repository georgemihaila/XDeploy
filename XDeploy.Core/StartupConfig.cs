using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents a startup configuration.
    /// </summary>
    public class StartupConfig
    {
        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string APIKey { get; set; }

        /// <summary>
        /// Gets or sets the synchronization server's port.
        /// </summary>
        public int SyncServerPort { get; set; }

        /// <summary>
        /// Gets or sets the apps.
        /// </summary>
        public IEnumerable<ApplicationInfo> Apps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the application will show detailed execution logs.
        /// </summary>
        public bool Verbose { get; set; }
    }
}
