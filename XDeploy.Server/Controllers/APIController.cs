using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Controllers
{
    public class APIController : Controller
    {
        private readonly ApplicationDbContext _context;

        public APIController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool ValidateCredentials((string Email, string KeyHash) creds)
        {
            var keys = _context.APIKeys.Where(x => x.UserEmail == creds.Email);
            if (keys.Count() > 0)
            {
                return keys.Any(x => x.KeyHash == creds.KeyHash);
            }
            return false;
        }

        private (string Email, string KeyHash)? Decode(string authString)
        {
            try
            {
                if (authString.Contains("Basic "))
                    authString = authString.Replace("Basic ", string.Empty);
                byte[] data = Convert.FromBase64String(authString);
                string decodedString = Encoding.UTF8.GetString(data);
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

        [HttpGet]
        public IActionResult ListApps([FromHeader(Name = "Authorization")] string authString)
        {
            var creds = Decode(authString);
            if (creds == null)
            {
                return BadRequest();
            }
            if (ValidateCredentials(creds.Value))
            {
                return Content(JsonConvert.SerializeObject(_context.Applications.Where(x => x.OwnerEmail == creds.Value.Email), Formatting.Indented));
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
