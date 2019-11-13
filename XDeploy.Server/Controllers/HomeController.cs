using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using XDeploy.Core;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;
using XDeploy.Server.Infrastructure.Data.Extensions;
using XDeploy.Server.Infrastructure.Data.MongoDb;
using XDeploy.Server.Models;

namespace XDeploy.Server.Controllers
{
    [Route("/[action]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly MongoDbFileManager _fileManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, MongoDbFileManager fileManager)
        {
            _logger = logger;
            _context = context;
            _fileManager = fileManager;
        }

        [Route("/")]
        public IActionResult Index()
        {
            IndexViewModel model = null;
            if (User.Identity.IsAuthenticated)
            {
                model = new IndexViewModel()
                {
                    UserEmail = User.Identity.Name,
                    HasAPIKey = _context.HasAPIKeys(User),
                    Applications = _context.Applications.Where(x => x.OwnerEmail == User.Identity.Name).OrderBy(x => x.ID)
                };
            }
            return View(model);
        }

        [HttpPost]
        [Route("/api/new-key")]
        [Authorize]
        public IActionResult GenerateAPIKey()
        {
            if (_context.HasAPIKeys(User))
                return BadRequest("An API key is already defined for this user.");
            string k = RNG.GetRandomString(32);
            var key = new APIKey(k, User.Identity.Name, Cryptography.ComputeSHA256(k));
            _context.APIKeys.Add(key);
            _context.SaveChanges();
            return Content(JsonConvert.SerializeObject(key.Key), "application/json");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete([ModelBinder(Name = "id")] Application application)
        {
            if (application != null && application.HasOwner(User))
            {
                await _fileManager.DeleteAllFilesAsync(application.ID);
                _context.Applications.Remove(application);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize]
        [Route("/app")]
        public IActionResult EditApp([ModelBinder(Name = "id")] Application application)
        {
            if (application is null)
            {
                return View((EditAppViewModel)null);
            }
            if (application.HasOwner(User))
            {
                return View((EditAppViewModel)application);
            }
            return NotFound();
        }

        [Authorize]
        [Route("/create-app", Name = "CreateApp")]
        [HttpPost]
        public IActionResult Create([FromForm] EditAppViewModel appModel)
        {
            var stupidRegexCheck = true; // [RegularExpressionAttribute] marks null or empty strings as valid
            if (appModel.IPRestrictedDeployer)
            {
                stupidRegexCheck = !string.IsNullOrEmpty(appModel.DeployerIP);
            }
            if (ModelState.IsValid && stupidRegexCheck)
            {
                var newApp = new Application()
                {
                    ID = RNG.GetRandomString(10),
                    OwnerEmail = User.Identity.Name,
                    DeployerIP = appModel.DeployerIP,
                    Description = appModel.Description,
                    Encrypted = appModel.Encrypted,
                    EncryptionAlgorithm = EncryptionAlgorithm.AES256,
                    IPRestrictedDeployer = appModel.IPRestrictedDeployer,
                    LastUpdate = DateTime.MinValue,
                    Name = appModel.Name,
                    PostdeployActions = appModel.PostdeployActions,
                    PredeployActions = appModel.PredeployActions
                };
                _context.Applications.Add(newApp);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                if (!stupidRegexCheck)
                {
                    ModelState.AddModelError(nameof(appModel.DeployerIP), "Invalid IP address");
                }
                return View("EditApp", appModel);
            }
        }


        [Authorize]
        [Route("/edit-app", Name = "EditApp")]
        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] EditAppViewModel appModel)
        {
            var stupidRegexCheck = true; // [RegularExpressionAttribute] marks null or empty strings as valid
            if (appModel.IPRestrictedDeployer)
            {
                stupidRegexCheck = !string.IsNullOrEmpty(appModel.DeployerIP);
            }
            if (ModelState.IsValid && stupidRegexCheck)
            {
                if (appModel.ID != null && _context.Applications.Exists(appModel.ID))
                {
                    var foundApp = _context.Applications.Find(appModel.ID);
                    if (foundApp.HasOwner(User))
                    {
                        var newApp = new Application()
                        {
                            ID = foundApp.ID,
                            OwnerEmail = foundApp.OwnerEmail,
                            DeployerIP = appModel.DeployerIP,
                            Description = appModel.Description,
                            Encrypted = appModel.Encrypted,
                            EncryptionAlgorithm = EncryptionAlgorithm.AES256,
                            IPRestrictedDeployer = appModel.IPRestrictedDeployer,
                            LastUpdate = foundApp.LastUpdate,
                            Name = appModel.Name,
                            PredeployActions = appModel.PredeployActions,
                            PostdeployActions = appModel.PostdeployActions
                        };
                        _context.Applications.Remove(foundApp);
                        _context.Applications.Add(newApp);
                        _context.SaveChanges();
                        if (newApp.Encrypted)
                        {
                            await _fileManager.DeleteAllNonEncryptedFilesAsync(newApp.ID);
                        }
                        else
                        {
                            await _fileManager.DeleteAllEncryptedFilesAsync(newApp.ID);
                        }
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return Unauthorized("Nice try."); //In case someone tries to modify someone else's app using a known ID
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                if (!stupidRegexCheck)
                {
                    ModelState.AddModelError(nameof(appModel.DeployerIP), "Invalid IP address");
                }
                return View("EditApp", appModel);
            }

        }

        [HttpGet]
        [Authorize]
        [Route("/config.json")]
        public IActionResult GenerateConfiguration() => Content(JsonConvert.SerializeObject(new StartupConfig() 
        {
            Mode = ApplicationMode.Deployer,
            Endpoint =  $"{HttpContext.Request.Scheme}://{Request.Host.Value}",
            Email = User.Identity.Name,
            APIKey = "your-api-key",
            SyncServerPort = 7745,
            Apps = _context.Applications
            .Where(x => x.OwnerEmail == User.Identity.Name)
            .Select(x => new ApplicationInfo 
            { 
                ID = x.ID,
                Location = "app-location",
                EncryptionKey = (x.Encrypted)? "encryption-key" : null
            })
        }, Formatting.Indented, new JsonSerializerSettings() 
        {
            NullValueHandling = NullValueHandling.Ignore
        }), "application/json");


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}