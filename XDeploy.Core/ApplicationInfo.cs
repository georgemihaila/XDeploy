using System;
using System.Collections.Generic;
using System.Text;

namespace XDeploy.Core
{
    /// <summary>
    /// Represents a basic application description.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ApplicationInfo"/> is encrypted.
        /// </summary>
        public bool Encrypted { get; set; } = false;

        /// <summary>
        /// Gets or sets the encryption key.
        /// </summary>
        public string EncryptionKey { get; set; }
    }
}
