using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server
{
    /// <summary>
    /// Represents a basic interface for defining an application.
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// Gets or sets the ID of the application.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the application.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this application can only be updated from a deployer with a specific IP address.
        /// </summary>
        public bool IPRestrictedDeployer { get; set; }

        /// <summary>
        /// Gets or sets the deployer's IP address (if necessary).
        /// </summary>
        public string DeployerIP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this application needs to be encrypted before being deployed and decrypted once deployed.
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets the pre-deploy actions.
        /// </summary>
        public string PredeployActions { get; set; }

        /// <summary>
        /// Gets or sets the post-deploy actions.
        /// </summary>
        public string PostdeployActions { get; set; }
    }
}
