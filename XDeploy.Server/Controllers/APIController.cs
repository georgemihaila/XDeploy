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
using XDeploy.Core.IO;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;

namespace XDeploy.Server.Controllers
{
    public class APIController : APIValidationBase
    {
        private readonly string _cachedFilesPath;

        public APIController(IConfiguration configuration, ApplicationDbContext context) : base(context)
        {
            _cachedFilesPath = configuration.GetValue<string>("CacheLocation");
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
        public IActionResult RemoteTree([FromHeader(Name = "Authorization")] string authString, string id)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                var tree = new Tree(Path.Combine(_cachedFilesPath, id));
                tree.Relativize();
                return Content(JsonConvert.SerializeObject(tree));
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
                return Content(JsonConvert.SerializeObject(new { encrypted = app.Encrypted }, Formatting.Indented));
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
                location = location.Replace('/', '\\').Replace("%5C", "\\").TrimStart('\\');
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

        [HttpGet]
        public IActionResult DownloadFile([FromHeader(Name = "Authorization")] string authString, string id, [FromHeader(Name = "Content-Location")] string location)
        {
            var creds = Decode(authString);
            var app = _context.Applications.Find(id);
            if (ValidateCredentials(creds) && ValidateIPIfNecessary(app) && app.OwnerEmail == creds.Value.Email)
            {
                if (location is null)
                {
                    return BadRequest("Content-Location header is required and must specify the relative path of the file.");
                }
                location = location.Replace('/', '\\').Replace("%5C", "\\").TrimStart('\\');
                var path = Path.Combine(_cachedFilesPath, id, location);
                if (System.IO.File.Exists(path))
                {
                    return File(System.IO.File.ReadAllBytes(path), "application/octet-stream");
                }
                else
                {
                    return NotFound(location);
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
                location = location.Replace('/', '\\').Replace("%5C", "\\").TrimStart('\\');
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
                        StaticWebSocketsWorkaround.TriggerUpdate(app.ID);
                    }
                }
                return Created(new Uri(location, UriKind.Relative), null);
            }
            return Unauthorized();
        }
    }
}
