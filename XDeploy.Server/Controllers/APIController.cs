using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
using XDeploy.Core.IO;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Controllers
{
    public class APIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _cachedFilesPath;

        public APIController(IConfiguration configuration, ApplicationDbContext context)
        {
            _context = context;
            _cachedFilesPath = configuration.GetValue<string>("CacheLocation");
        }

        private bool ValidateCredentials((string Email, string KeyHash)? creds)
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

        private bool ValidateIPIfNecessary(Application app)
        {
            if (app is null)
                return false;
            return app.IPRestrictedDeployer ? (HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString() == app.DeployerIP) : true;
        }

        private (string Email, string KeyHash)? Decode(string authString)
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

        [HttpPost]
        public IActionResult ValidateCredentials([FromHeader(Name = "Authorization")] string authString)
        {
            if (ValidateCredentials(Decode(authString)))
            {
                return Ok();
            }
            return Unauthorized();
        }

        [HttpGet]
        public IActionResult ListApps([FromHeader(Name = "Authorization")] string authString)
        {
            var creds = Decode(authString);
            if (ValidateCredentials(creds))
            {
                return Content(JsonConvert.SerializeObject(_context.Applications.Where(x => x.OwnerEmail == creds.Value.Email), Formatting.Indented));
            }
            return Unauthorized();
        }

        [HttpGet]
        public IActionResult App([FromHeader(Name = "Authorization")] string authString, string id)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                return Content(JsonConvert.SerializeObject(app, Formatting.Indented));
            }
            return Unauthorized();
        }

        [HttpGet]
        public IActionResult TreeForApp([FromHeader(Name = "Authorization")] string authString, string id)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                var path = Path.Join(_cachedFilesPath, id);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                var tree = new Tree(path);
                tree.Relativize();
                return Content(JsonConvert.SerializeObject(tree));
            }
            return Unauthorized();
        }
    }
}
