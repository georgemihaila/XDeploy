using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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
                    Applications = _context.Applications.Where(x => x.OwnerEmail == User.Identity.Name)
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
            var key = new APIKey(RNG.GetRandomString(32), User.Identity.Name);
            _context.APIKeys.Add(key);
            _context.SaveChanges();
            return Content(JsonConvert.SerializeObject(key.Key), "application/json");
        }

        [Authorize]
        [Route("/app")]
        public IActionResult EditApp(string id = "")
        {
            EditAppViewModel app = new EditAppViewModel();
            if (_context.Applications.Exists(id))
            {
                var foundApp = _context.Applications.FirstByID(id);
                if (foundApp.HasOwner(User))
                {
                    app = (EditAppViewModel)foundApp;
                }
                else
                {
                    return Unauthorized("You are not the owner of this app.");
                }
            }
            else if (!string.IsNullOrEmpty(id))
            {
                return BadRequest("Application not found.");
            }
            return View(app);
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
                    var foundApp = _context.Applications.FirstByID(appModel.ID);
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
                            Name = appModel.Name
                        };
                        _context.Applications.Remove(foundApp);
                        _context.Applications.Add(newApp);
                        _context.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return Unauthorized("Nice try."); //In case someone tries to modify someone else's app using a known ID
                    }
                }
                else
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
                        Name = appModel.Name
                    };
                    _context.Applications.Add(newApp);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
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
