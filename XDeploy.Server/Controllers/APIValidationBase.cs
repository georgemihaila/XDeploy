using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Controllers
{
    /// <summary>
    /// Represents a base class that provides functionality for validating incoming API requests.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    public abstract class APIValidationBase : ControllerBase
    {
        protected readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="APIValidationBase"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        protected APIValidationBase(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates a user's credentials ((email, API key hash) tuple) against the database context and returns a result indicating whether they are valid or not.
        /// </summary>
        protected bool ValidateCredentials((string Email, string KeyHash)? creds)
        {
            if (creds.HasValue)
            {
                var keys = _context.APIKeys.Where(x => x.UserEmail == creds.Value.Email);
                if (keys.Count() > 0)
                {
                    return keys.Any(x => x.KeyHash == creds.Value.KeyHash);
                }
            }
            return false;
        }

        /// <summary>
        /// Checks whether an application is IP-restricted and if yes, returns a result indicating whether the requester's remote IP address matches the one the application is restricted to.
        /// </summary>
        protected bool ValidateIPIfNecessary(Application app)
        {
            if (app is null)
                return false;
            return app.IPRestrictedDeployer ? (HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString() == app.DeployerIP) : true;
        }

        /// <summary>
        /// Decodes the specified authentication string.
        /// </summary>
        protected (string Email, string KeyHash)? Decode(string authString)
        {
            try
            {
                if (authString.Contains("Basic "))
                    authString = authString.Replace("Basic ", string.Empty);
                var decodedString = Cryptography.Base64Decode(authString);
                var pair = decodedString.Split(':');
                var user = pair[0];
                var keyHash = Cryptography.ComputeSHA256(pair[1]);
                return (user, keyHash);
            }
            catch
            {
                return null;
            }
        }
    }
}
