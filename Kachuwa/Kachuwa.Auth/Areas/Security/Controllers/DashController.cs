using Kachuwa.Configuration;
using Kachuwa.Core.Utility;
using Kachuwa.Identity.Models;
using Kachuwa.Identity.Service;
using Kachuwa.Log;
using Kachuwa.Storage;
using Kachuwa.Web;
using Kachuwa.Web.API;
using Kachuwa.Web.Service;
using Kachuwa.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityUser = Kachuwa.Identity.Models.IdentityUser;

namespace Kachuwa.Auth.Areas.Security.Controllers
{
    [Area("Security")]
    public class DashController : BaseController
    {
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IIdentityUserService _identityUserService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAppUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILoginHistoryService _loginHistoryService;
        private readonly SessionExt _session;
        public DashController(ILogger logger, IOptionsSnapshot<KachuwaAppConfig> configOption, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, IAppUserService userService, IConfiguration configuration, IWebHostEnvironment hostingEnvironment,
            ILoginHistoryService loginHistoryService, IIdentityUserService identityUserService, SessionExt session)
        {
            _logger = logger;
            KachuwaAppConfig kachuwaAppConfig = configOption.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _configuration = configuration;
            _hostingEnvironment = hostingEnvironment;
            _loginHistoryService = loginHistoryService;
            _identityUserService = identityUserService;
            _session = session;
        }

        public async Task<IActionResult> Index()
        {
            CSession objSession = new CSession();
            objSession = _session.Get<CSession>("_user");
            if (objSession == null || objSession.UserId <= 0)
            {
                return RedirectToAction("Index", "KachuwaPage");
            }
            var model = new DashboardViewModel
            {
                Username = objSession.Username,
                CurrentDateTime = DateTime.Now,
                Users = await _userService.GetAllUsers()
            };
            return View(model);
            
        }
    }
}
