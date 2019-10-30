using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XDeploy.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/[action]")]
    public class AdminController
    {

    }
}
