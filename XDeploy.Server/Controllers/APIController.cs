using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XDeploy.Core;
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
        public IActionResult HasFile([FromHeader(Name = "Authorization")] string authString, string id, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                if (location is null)
                {
                    return BadRequest("Content-Location header is required and must specify the relative path of the file.");
                }
                if (checksum is null)
                {
                    return BadRequest("X-SHA256 header is required and must specify the SHA-256 checksum of the file.");
                }
                location = location.Replace('/', '\\').TrimStart('\\');
                var path = Path.Combine(_cachedFilesPath, id, location);
                if (System.IO.File.Exists(path) && Cryptography.SHA256CheckSum(path) == checksum)
                {
                    return Content(JsonConvert.SerializeObject(true));
                }
                else
                {
                    return Content(JsonConvert.SerializeObject(false));
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult UploadFile([FromHeader(Name = "Authorization")] string authString, string id, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                if (location is null)
                {
                    return BadRequest("Content-Location header is required and must specify the relative path of the file.");
                }
                if (checksum is null)
                {
                    return BadRequest("X-SHA256 header is required and must specify the SHA-256 checksum of the file.");
                }
                location = location.Replace('/', '\\').TrimStart('\\');
                var path = Path.Combine(_cachedFilesPath, id, location);
                if (System.IO.File.Exists(path) && Cryptography.SHA256CheckSum(path) == checksum)
                {
                    return StatusCode(303);
                }
                else
                {
                    var dir = Path.Combine(path.Split(Path.DirectorySeparatorChar)[..^1]);
                    Directory.CreateDirectory(dir);
                    using (var stream = System.IO.File.Create(path))
                    {
                        byte[] buffer = null;
                        var requestStream = Request.BodyReader.AsStream();
                        using (var reader = new BinaryReader(requestStream))
                        {
                            buffer = reader.ReadBytes((int)Request.ContentLength);
                        }
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Close();
                        app.LastUpdate = DateTime.Now;
                        _context.SaveChanges();
                    }
                }
                return Created(new Uri(location, UriKind.Relative), null);
            }
            return Unauthorized();
        }
    }
}
