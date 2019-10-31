using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Infrastructure.Data
{
    /// <summary>
    /// Represents an API key, linked to a user.
    /// </summary>
    public class APIKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="APIKey"/> class.
        /// </summary>
        public APIKey()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="APIKey"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="userEmail">The user email.</param>
        public APIKey(string key, string userEmail, string keyHash)
        {
            Key = key;
            UserEmail = userEmail;
            KeyHash = keyHash;
            FirstChars = key.Substring(0, 8);
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        [NotMapped]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the key hash.
        /// </summary>
        [Key]
        public string KeyHash { get; set; }

        /// <summary>
        /// Gets or sets the first characters of the key.
        /// </summary>
        public string FirstChars { get; set; }

        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        [ForeignKey("Email")]
        public string UserEmail { get; set; }
    }
}
