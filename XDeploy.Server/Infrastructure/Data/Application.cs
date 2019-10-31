using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data
{
    /// <summary>
    /// Represents a registered application.
    /// </summary>
    [ModelBinder(BinderType = typeof(IDApplicationBinder))]
    public class Application : IApplication
    {
        /// <summary>
        /// Gets or sets the ID of the application.
        /// </summary>
        [Key]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the application owner's email address.
        /// </summary>
        [ForeignKey("Email")]
        public string OwnerEmail { get; set; }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the application.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the date of the last application update.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets a value indicating whether this application can only be updated from a deployer with a specific IP address.
        /// </summary>
        public bool IPRestrictedDeployer { get; set; } = false;

        /// <summary>
        /// Gets or sets the deployer's IP address (if necessary).
        /// </summary>
        public string DeployerIP { get; set; } 

        /// <summary>
        /// Gets or sets a value indicating whether this application needs to be encrypted before being deployed and decrypted once deployed.
        /// </summary>
        public bool Encrypted { get; set; } = false;

        /// <summary>
        /// Gets or sets the encryption algorithm.
        /// </summary>
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; } 
    }
}
