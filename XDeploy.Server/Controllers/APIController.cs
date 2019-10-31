using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        [HttpPost]
        public IActionResult ValidateCredentials([FromHeader(Name = "Authorization")] string authString)
        {
            var creds = Decode(authString);
            if (creds == null)
            {
                return BadRequest();
            }
            if (ValidateCredentials(creds.Value))
            {
                return Ok();
            }
            else
            {
                return Unauthorized();
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

        [HttpGet]
        public IActionResult App([FromHeader(Name = "Authorization")] string authString, string id)
        {
            var creds = Decode(authString);
            if (creds == null)
            {
                return BadRequest();
            }
            if (ValidateCredentials(creds.Value))
            {
                return Content(JsonConvert.SerializeObject(_context.Applications.First(x => x.OwnerEmail == creds.Value.Email && x.ID == id), Formatting.Indented));
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public IActionResult CachedFilesForApp([FromHeader(Name = "Authorization")] string authString, string id)
        {
            var creds = Decode(authString);
            if (creds == null)
            {
                return BadRequest();
            }
            if (ValidateCredentials(creds.Value))
            {
                var dict = new List<(string, string)>();
                if (Directory.Exists(id))
                {
                    string[] allFiles = Directory.GetFiles(id, "*.*", SearchOption.AllDirectories);
                    foreach (var file in allFiles)
                    {
                        var pair = (file, SHA256CheckSum(file));
                        dict.Add(pair);
                    }
                }
                return Content(JsonConvert.SerializeObject(dict));
            }
            else
            {
                return Unauthorized();
            }
        }

        private static string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                byte[] bytes = null;
                using (FileStream fileStream = System.IO.File.OpenRead(filePath))
                {
                    bytes = SHA256.ComputeHash(fileStream);
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
