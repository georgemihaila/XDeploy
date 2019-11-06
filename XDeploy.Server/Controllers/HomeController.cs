using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using XDeploy.Core;
using XDeploy.Server.Infrastructure;
using XDeploy.Server.Infrastructure.Data;
using XDeploy.Server.Infrastructure.Data.Extensions;
using XDeploy.Server.Models;

namespace XDeploy.Server.Controllers
{
    [Route("/[action]")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly string _cachedFilesPath;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            _cachedFilesPath = configuration.GetValue<string>("CacheLocation");
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
        public IActionResult Delete([ModelBinder(Name = "id")] Application application)
        {
            if (application != null && application.HasOwner(User))
            {
                //Remove cache directory and all associated files if necessary
                var path = Path.Join(_cachedFilesPath, application.ID);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true); 
                }

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
                Directory.CreateDirectory(Path.Join(_cachedFilesPath, newApp.ID));
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
        public IActionResult Edit([FromForm] EditAppViewModel appModel)
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

                        //Remove encrypted or non-encrypted files from the associated cache directory (if possible)
                        var files = Directory.EnumerateFiles(Path.Combine(_cachedFilesPath, newApp.ID), "*.*", SearchOption.AllDirectories);
                        if (newApp.Encrypted)
                        {
                            foreach (var file in files.Where(x => !x.EndsWith(".enc")))
                            {
                                System.IO.File.Delete(file);
                            }
                        }
                        else
                        {
                            foreach (var file in files.Where(x => x.EndsWith(".enc")))
                            {
                                System.IO.File.Delete(file);
                            }
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}