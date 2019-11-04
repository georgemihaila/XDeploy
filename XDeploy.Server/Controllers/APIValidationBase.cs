using Microsoft.AspNetCore.Http;
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
        private readonly string _authHeaderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="APIValidationBase"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        protected APIValidationBase(ApplicationDbContext context)
        {
            _context = context;
            _authHeaderName = "Authorization";
        }

        /// <summary>
        /// Validates a user's credentials (taken from the Authorization header) against the database context and returns a result indicating whether they are valid or not.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected bool ValidateCredentials(HttpRequest request) => ValidateCredentials(GetCredentialsFromAuthorizationHeader(request));

        /// <summary>
        /// Gets a user's credentials from the Authorization header.
        /// </summary>
        protected (string Email, string KeyHash)? GetCredentialsFromAuthorizationHeader(HttpRequest request)
        {
            if (request.Headers.ContainsKey(_authHeaderName))
            {
                return Decode(request.Headers[_authHeaderName]);
            }
            return null;
        }

        protected enum RequestValidationType { Credentials, CredentialsAndOwner, CredentialsOwnerAndIP }

        protected bool ValidateRequest(HttpRequest request, RequestValidationType validationType, Application application = null)
        {
            var creds = GetCredentialsFromAuthorizationHeader(request);
            if (ValidateCredentials(creds))
            {
                if (application is null)
                {
                    return false;
                }
                return (validationType == RequestValidationType.CredentialsAndOwner) ? application.OwnerEmail.ToUpper() == creds.Value.Email.ToUpper() : application.OwnerEmail.ToUpper() == creds.Value.Email.ToUpper() && ValidateIPIfNecessary(application);
            }
            else
            {
                return false;
            }
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
