using Kachuwa.Configuration;
using Kachuwa.Core.Utility;
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
    public class AuthController : BaseApiController
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

        public AuthController(ILogger logger, IOptionsSnapshot<KachuwaAppConfig> configOption, UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager,IAppUserService userService, IConfiguration configuration, IWebHostEnvironment hostingEnvironment, 
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

        [HttpGet]
        [AllowAnonymous]
        [ActionName("SignInWithGoogle")]
        public async Task<object> SignInWithGoogle()
        {
            try
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(GoogleResponse))
                };
                return Challenge(properties, GoogleDefaults.AuthenticationScheme);
            }
            catch (Exception e)
            {
                _logger.Log(LogType.Error, () => e.Message, e);
                return HttpResponse(500, "Internal server error");
            }
            
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("Auth/GoogleResponse")]
        [ActionName("GoogleResponse")]
        public async Task<object> GoogleResponse()
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!authenticateResult.Succeeded)
                    return BadRequest("Google Authentication failed");
                List<Claim> googleClaims = authenticateResult.Principal.Claims.ToList();
                var authProvider = authenticateResult.Properties?.Items[".AuthScheme"] ?? "Google";
                var status = await _userService.AddExternalUser(authProvider, googleClaims[0]?.Value ?? "", googleClaims);
                if (status.HasError)
                {
                    return HttpResponse(400, status.Message);
                }
                return Redirect("~/Security/Dash");
                //return HttpResponse(200, "Successfully logged In");
            }
            catch (Exception e)
            {
                _logger.Log(LogType.Error, () => e.Message, e);
                return HttpResponse(500, "Internal server error");
            }            
        }

        [HttpGet]
        [AllowAnonymous]
        [ActionName("SignInWithLinkedIn")]
        public async Task<object> SignInWithLinkedIn()
        {
            try
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action(nameof(LinkedInResponse))
                };
                return Challenge(properties, "LinkedIn");
            }
            catch (Exception e)
            {
                _logger.Log(LogType.Error, () => e.Message, e);
                return HttpResponse(500, "Internal server error");
            }

        }

        [HttpGet]
        [AllowAnonymous]
        [Route("Auth/LinkedInResponse")]
        [ActionName("LinkedInResponse")]
        public async Task<object> LinkedInResponse()
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!authenticateResult.Succeeded)
                    return BadRequest("LinkedIn Authentication failed");
                List<Claim> linkedInClaims = authenticateResult.Principal.Claims.ToList();
                var authProvider = authenticateResult.Properties?.Items[".AuthScheme"];
                var status = await _userService.AddExternalUser(authProvider, linkedInClaims[0]?.Value ?? "", linkedInClaims);
                if (status.HasError)
                {
                    return HttpResponse(400, status.Message);
                }
                // return HttpResponse(200, "Successfully logged In");
                return Redirect("~/Security/Dash");
            }
            catch (Exception e)
            {
                _logger.Log(LogType.Error, () => e.Message, e);
                return HttpResponse(500, "Internal server error");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [ActionName("Logout")]
        public async Task<object> Logout()
        {
            try
            {
                _session.Remove("_user");
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }
                return Ok(new { redirectUrl = Url.Action("Index", "KachuwaPage") });
            }
            catch (Exception e)
            {
                _logger.Log(LogType.Error, () => e.Message, e);
                return HttpResponse(500, "Internal server error");
            }
        }
    }
}
