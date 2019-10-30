using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Models
{
    /// <summary>
    /// Represents a view model for the home page.
    /// </summary>
    public class IndexViewModel
    {
        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user has an API key registered.
        /// </summary>
        public bool HasAPIKey { get; set; }

        /// <summary>
        /// Gets or sets the applications.
        /// </summary>
        public IEnumerable<Application> Applications { get; set; }
    }
}
