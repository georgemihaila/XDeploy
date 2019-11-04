using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data
{
    /// <summary>
    /// Represents an expected file.
    /// </summary>
    public class ExpectedFile
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the checksum.
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Gets or sets the parent job.
        /// </summary>
        public DeploymentJob ParentJob { get; set; }
    }
}
