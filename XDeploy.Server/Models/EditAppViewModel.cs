﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Models
{
    /// <summary>
    /// Represents a view model for editing applications.
    /// </summary>
    /// <seealso cref="XDeploy.Server.IApplication" />
    public class EditAppViewModel : IApplication
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditAppViewModel"/> class.
        /// </summary>
        public EditAppViewModel()
        {
         
        }

        public static explicit operator EditAppViewModel(Application src) => new EditAppViewModel()
        {
            DeployerIP = src.DeployerIP,
            Description = src.Description,
            Encrypted = src.Encrypted,
            IPRestrictedDeployer = src.IPRestrictedDeployer,
            Name = src.Name,
            ID = src.ID,
            PredeployActions = src.PredeployActions,
            PostdeployActions = src.PostdeployActions
        };

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        [StringLength(32, MinimumLength = 3)]
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the application.
        /// </summary>
        [StringLength(512, MinimumLength = 10)]
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this application can only be updated from a deployer with a specific IP address.
        /// </summary>
        [Required]
        public bool IPRestrictedDeployer { get; set; } 

        /// <summary>
        /// Gets or sets the deployer's IP address (if necessary).
        /// </summary>
        [RegularExpression(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")]
        public string DeployerIP { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this application needs to be encrypted before being deployed and decrypted once deployed.
        /// </summary>
        [Required]
        public bool Encrypted { get; set; }

        /// <summary>
        /// Gets or sets the ID of the application.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the pre-deploy actions.
        /// </summary>
        public string PredeployActions { get; set; }

        /// <summary>
        /// Gets or sets the post-deploy actions.
        /// </summary>
        public string PostdeployActions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IApplication" /> is locked. This indicates that an application is currently being updated.
        /// </summary>
        public bool Locked { get; set; }
    }
}
