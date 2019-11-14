using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
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
using XDeploy.Core.IO.FileManagement;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;
using XDeploy.Server.Infrastructure.Data.MongoDb;

namespace XDeploy.Server.Controllers
{
    public class APIController : APIValidationBase
    {
        private readonly MongoDbFileManager _fileManager;

        public APIController(MongoDbFileManager fileManager, ApplicationDbContext context) : base(context)
        {
            _fileManager = fileManager;
        }
        
        /// <summary>
        /// <para>Validates a user's credentials.</para>
        /// <para>POST /api/ValidateCredentials</para>
        /// <para>Headers: ["Authorization": "Basic [...]"]</para>
        /// </summary>
        [HttpPost]
        public IActionResult ValidateCredentials() => Content(JsonConvert.SerializeObject(ValidateCredentials(Request)));

        /// <summary>
        /// <para>Lists a user's applications.</para>
        /// <para>GET /api/ListApps</para>
        /// <para>Headers: ["Authorization": "Basic [...]"]</para>
        /// </summary>
        [HttpGet]
        public IActionResult ListApps()
        {
            if (ValidateRequest(Request, RequestValidationType.Credentials))
            {
                var user = GetCredentialsFromAuthorizationHeader(Request).Value;
                return Content(JsonConvert.SerializeObject(_context.Applications.Where(x => x.OwnerEmail == user.Email), Formatting.Indented));
            }
            return Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> RemoteFiles([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {
                return Content(JsonConvert.SerializeObject(await _fileManager.GetAllFilesAsync(application.ID)));
            }
            return Unauthorized();
        }

        [HttpGet]
        public IActionResult App([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {
                return Content(JsonConvert.SerializeObject(new 
                { 
                    encrypted = application.Encrypted, 
                    preDeployment = application.PredeployActions,
                    postDeployment = application.PostdeployActions
                }, Formatting.Indented));
            }
            return Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> HasFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum)
        {
            if (location is null)
            {
                return BadRequest("Content-Location header is required and must specify the relative path of the file.");
            }
            if (checksum is null)
            {
                return BadRequest("X-SHA256 header is required and must specify the SHA-256 checksum of the file.");
            }
            var creds = Decode(authString);
            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {

                location = FormatFilePathString(location);
                return Content(JsonConvert.SerializeObject(await _fileManager.HasFileAsync(application.ID, location, checksum)));
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Cleanup([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromBody] IEnumerable<IODifference> differences)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                foreach(var removedFile in differences.Where(x => x.DifferenceType == IODifference.IODifferenceType.Removal))
                {
                    await _fileManager.TryDeleteFileAsync(application.ID, FormatFilePathString(removedFile.Path));
                }
                return Ok();
            }
            return Unauthorized();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location)
        {
            if (location is null)
            {
                return BadRequest("Content-Location header is required and must specify the relative path of the file.");
            }

            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {
                location = FormatFilePathString(location);
                if (await _fileManager.HasFileAsync(application.ID, location))
                {
                    return File(await _fileManager.GetFileBytesAsync(application.ID, location), "application/octet-stream");
                }
                else
                {
                    return NotFound(location);
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult LockApplication([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                var app = _context.Applications.Find(application.ID);
                app.Locked = true;
                _context.SaveChanges();
                WebsocketsIOC.TriggerApplicationLockedChanged(application.ID, true, 0);
                return Created(new Uri("api/LockApplication", UriKind.Relative), application.ID);
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> UnlockApplication([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                var app = _context.Applications.Find(application.ID);
                app.Locked = false;
                app.LastUpdate = DateTime.Now;
                app.Size_Bytes = await _fileManager.GetSizeForAppAsync(application.ID);
                _context.SaveChanges();
                WebsocketsIOC.TriggerUpdateAvailable(application.ID);
                WebsocketsIOC.TriggerApplicationLockedChanged(application.ID, false, app.Size_Bytes);
                return Created(new Uri("api/UnlockApplication", UriKind.Relative), application.ID);
            }
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum)
        {
            if (location is null)
            {
                return BadRequest("Content-Location header is required and must specify the relative path of the file.");
            }
            if (checksum is null)
            {
                return BadRequest("X-SHA256 header is required and must specify the SHA-256 checksum of the file.");
            }

            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                location = FormatFilePathString(location);
                if (await _fileManager.HasFileAsync(application.ID, location, checksum))
                {
                    return StatusCode(303);
                }
                else
                {
                    var bytes = new byte[(int)Request.ContentLength];
                    using (BinaryReader reader = new BinaryReader(Request.BodyReader.AsStream()))
                    {
                        bytes = reader.ReadBytes(bytes.Length);
                    }
                    await _fileManager.InsertOrUpdateFileAsync(application.ID, location, bytes);
                    application.LastUpdate = DateTime.Now;
                    _context.SaveChanges();
                }
                return Created( new Uri(location, UriKind.Relative), null);
            }
            return Unauthorized();
        }

        private static string FormatFilePathString(string value) => value.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\');
    }
}
