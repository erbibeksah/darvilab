
using Kachuwa.Core.Extensions;
using Kachuwa.Core.Utility;
using Kachuwa.Data;
using Kachuwa.Data.Crud;
using Kachuwa.Identity;
using Kachuwa.Job;
using Kachuwa.Log.Insight;
using Kachuwa.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPOI.OpenXml4Net.OPC;
using System;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace KachuwaWebApp
{
    public class Startup
    {
        private IWebHostEnvironment hostingEnvironment;
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            hostingEnvironment = env;
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddKachuwaIdentitySever(hostingEnvironment, Configuration);
            var serviceProvider = services.BuildServiceProvider();
            //TODO can be used in Action Config in KachuwaSetup
            //registering default database factory service
            IDatabaseFactory dbFactory = DatabaseFactories.SetFactory(Dialect.SQLServer, serviceProvider);
            services.AddSingleton(dbFactory);
            services.RegisterKachuwaCoreServices(serviceProvider);
            services.AddSignalR(o =>
            {

                o.EnableDetailedErrors = true;
                o.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;//camel casing

            });
            //services.ConfigureApplicationCookie(options =>
            //{
            //    options.Cookie.SameSite = SameSiteMode.None;
            //    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            //    options.Cookie.HttpOnly = true;
            //    // options.Cookie.Name = "egtkt";
            //    // options.LoginPath = new PathString("/account/login");
            //    // options.AccessDeniedPath = new PathString("/access-denied");
            //    // options.LogoutPath = new PathString("/account/logout");
            //    // options.Cookie.SameSite = SameSiteMode.Lax;
            //    // options.Cookie.HttpOnly = false;
            //    // options.ExpireTimeSpan = TimeSpan.FromDays(30);


            //});
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            })
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;

            }).AddJwtBearer(opts =>
            {
                opts.Authority = hostingEnvironment.IsDevelopment() ? "https://localhost:44360" : Configuration["KachuwaAppConfig:TokenAuthority"];
                opts.Audience = "KachuwaApi";
                opts.RequireHttpsMetadata = true;
                opts.IncludeErrorDetails = true;
                opts.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // replace "token" with whatever your param name is
                        if (ctx.Request.Method.Equals("GET") && ctx.Request.Query.ContainsKey("token"))
                            ctx.Token = ctx.Request.Query["token"];
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = Configuration["KachuwaAppConfig:Authentication:GoogleClientId"]?? "";
                options.ClientSecret = Configuration["KachuwaAppConfig:Authentication:GoogleClientSecret"]?? "";
                options.CallbackPath = Configuration["KachuwaAppConfig:Authentication:GoogleInCallBackUrl"]?? "";
                options.SaveTokens = true;
            })
            .AddOAuth("LinkedIn", options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = Configuration["KachuwaAppConfig:Authentication:LinkedInClientId"]?? "";
                options.ClientSecret = Configuration["KachuwaAppConfig:Authentication:LinkedInClientSecret"]?? "";
                options.CallbackPath = Configuration["KachuwaAppConfig:Authentication:LinkedInCallBackUrl"]?? "";
                options.AuthorizationEndpoint = "https://www.linkedin.com/oauth/v2/authorization";
                options.TokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken";
                options.UserInformationEndpoint = "https://api.linkedin.com/v2/userinfo";

                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.SaveTokens = true;

                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.UserData, "picture");

                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, options.UserInformationEndpoint);
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                        var response = await context.Backchannel.SendAsync(request);
                        response.EnsureSuccessStatusCode();

                        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        context.RunClaimActions(json.RootElement);
                    }
                };
            });
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
                options.Secure = CookieSecurePolicy.SameAsRequest;
            });
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(1);

            });
            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = hostingEnvironment.IsDevelopment() ? 44360 : 443;
            });
            services.AddDistributedMemoryCache();
            services.AddHttpContextAccessor();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<SessionExt>();

            //services.AddAuthentication().AddFacebook(facebookOptions =>
            //{
            //    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
            //    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
            //});
            // services.RegisterSchedulingServer(Configuration);

            services.AddSignalR();
            var mvcBuilder = services.AddControllersWithViews()
                            .AddNewtonsoftJson();

           // Automatically register all Admin area controllers from referenced assemblies
           var adminAssemblies = AppDomain.CurrentDomain.GetAssemblies()
              .Where(a => !a.IsDynamic
                        && !string.IsNullOrEmpty(a.Location)
                        && (
                            a.GetName().Name.Contains("Kachuwa.Admin") ||
                            a.GetName().Name.Contains("Kachuwa.Auth")
            ));

           foreach (var assembly in adminAssemblies)
           {
               if (!mvcBuilder.PartManager.ApplicationParts.Any(part => part.Name == assembly.GetName().Name))
               {
                   mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
               }
           }

        }
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider,
            IWebHostEnvironment env, ILoggerFactory loggerFactory,
            IOptions<ApplicationInsightsSettings> applicationInsightsSettings)
        {
            app.UseHttpsRedirection();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseCookiePolicy();
            app.UseSession();
            app.UseRouting();
            app.UseWebSockets();
            app.UseKachuwaCore(env, serviceProvider);
            app.UseKachuwaWeb(env,true);
        }
    }
}

