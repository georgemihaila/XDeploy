using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        public IActionResult RemoteTree([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {
                var tree = new Tree(Path.Combine(_cachedFilesPath, application.ID));
                tree.Relativize();
                return Content(JsonConvert.SerializeObject(tree));
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
        public IActionResult HasFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum)
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

                location = location.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\');
                var path = Path.Combine(_cachedFilesPath, application.ID);
                var res = (new FileManager(path).HasFile(location, checksum));
                return Content(JsonConvert.SerializeObject(res));
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult Cleanup([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromBody] IEnumerable<IODifference> differences)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                var fileManager = new FileManager(Path.Combine(_cachedFilesPath, application.ID));
                fileManager.Cleanup(differences.Where(x => x.DifferenceType == IODifference.IODifferenceType.Removal));
                return Ok();
            }
            return Unauthorized();
        }

        [HttpGet]
        public IActionResult DownloadFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location)
        {
            if (location is null)
            {
                return BadRequest("Content-Location header is required and must specify the relative path of the file.");
            }

            if (ValidateRequest(Request, RequestValidationType.CredentialsAndOwner, application))
            {

                location = location.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\');
                var path = Path.Combine(_cachedFilesPath, application.ID);
                var fileManager = new FileManager(path);
                if (fileManager.HasFile(location))
                {
                    return File(fileManager.GetFileBytes(location), "application/octet-stream");
                }
                else
                {
                    return NotFound(location);
                }
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult DeleteDeploymentJob([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [ModelBinder(BinderType = typeof(IDDeploymentJobBinder), Name = "jobid")] DeploymentJob deploymentJob)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                if (deploymentJob is null)
                {
                    return Ok();
                }
                if (deploymentJob.ApplicationID != application.ID)
                {
                    return BadRequest("Deployment job doesn't match application.");
                }
                try
                {
                    _context.ExpectedFile.RemoveRange(_context.ExpectedFile.Where(x => x.ParentJob.ID == deploymentJob.ID));
                    _context.DeploymentJobs.Remove(_context.DeploymentJobs.First(x => x.ID == deploymentJob.ID));
                    _context.SaveChanges();
                }
                catch //To do something with this
                {

                }
                StaticWebSocketsWorkaround.TriggerUpdate(application.ID);
                return Created(new Uri("api/DeleteDeploymentJob", UriKind.Relative), deploymentJob.ID);
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult CreateDeploymentJob([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromBody] IEnumerable<ExpectedFileInfo> expected)
        {
            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                var job = new DeploymentJob()
                {
                    ID = RNG.GetRandomString(8, RNG.StringType.Numeric).TrimStart('0'),
                    ApplicationID = application.ID
                };
                job.ExpectedFiles = expected.Select(x => new ExpectedFile()
                {
                    ID = RNG.GetRandomString(8, RNG.StringType.Alphanumeric),
                    Checksum = x.Checksum,
                    Filename = x.Filename.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\'),
                    ParentJob = job 
                }).ToList();
                _context.DeploymentJobs.Add(job);
                _context.SaveChanges();
                return Created(new Uri("api/CreateDeploymentJob", UriKind.Relative), job.ID);
            }
            return Unauthorized();
        }

        [HttpPost]
        public IActionResult UploadFile([FromHeader(Name = "Authorization")] string authString, [ModelBinder(Name = "id")] Application application, [FromHeader(Name = "Content-Location")] string location, [FromHeader(Name = "X-SHA256")] string checksum, [ModelBinder(BinderType = typeof(IDDeploymentJobBinder), Name = "jobid")] DeploymentJob deploymentJob)
        {
            if (location is null)
            {
                return BadRequest("Content-Location header is required and must specify the relative path of the file.");
            }
            if (checksum is null)
            {
                return BadRequest("X-SHA256 header is required and must specify the SHA-256 checksum of the file.");
            }
            if (deploymentJob is null)
            {
                return BadRequest("Parameter jobid is required and must specify a valid deployment job id.");
            }

            if (ValidateRequest(Request, RequestValidationType.CredentialsOwnerAndIP, application))
            {
                location = location.Replace('/', '\\').Replace("%5C", "\\").Replace("%20", " ").TrimStart('\\');
                if (deploymentJob.ApplicationID != application.ID)
                {
                    return BadRequest("Deployment job doesn't match application.");
                }
                /* //Is weird
                if (!deploymentJob.ExpectedFiles.Any(x => x.Checksum == checksum && x.Filename == location))
                {
                    return BadRequest("Unexpected file.");
                }
                */
                var path = Path.Combine(_cachedFilesPath, application.ID);
                var fileManager = new FileManager(path);
                if (fileManager.HasFile(location, checksum))
                {
                    return StatusCode(303);
                }
                else
                {
                    fileManager.WriteFile(location, Request.BodyReader.AsStream(), (int)Request.ContentLength);

                    _context.ExpectedFile.Remove(_context.ExpectedFile.First(x => x.ParentJob.ID == deploymentJob.ID && x.Checksum == checksum));
                    if (deploymentJob.ExpectedFiles.Count == 0)
                    {
                        _context.DeploymentJobs.Remove(deploymentJob);
                        StaticWebSocketsWorkaround.TriggerUpdate(application.ID);
                    }
                    application.LastUpdate = DateTime.Now;
                    _context.SaveChanges();
                }
                return Created(new Uri(location, UriKind.Relative), null);
            }
            return Unauthorized();
        }
    }
}
