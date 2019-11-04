using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data
{
    /// <summary>
    /// Represents a deployment job.
    /// </summary>
    public class DeploymentJob
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the application ID.
        /// </summary>
        [ForeignKey("ID")]
        public string ApplicationID { get; set; }

        /// <summary>
        /// Gets or sets the expected files.
        /// </summary>
        public ICollection<ExpectedFile> ExpectedFiles { get; set; }

    }
}
