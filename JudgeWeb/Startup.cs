using idunno.Authentication.Basic;
using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Mailing;
using JudgeWeb.Features.OjUpdate;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace JudgeWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            AssemblyPrefix = "JudgeWeb.Areas.";
            EnabledAreas = new[] { "Misc", "Account", "Contest", "Dashboard", "Polygon" };
        }

        public IConfiguration Configuration { get; }

        public IHostingEnvironment Environment { get; }

        public string[] EnabledAreas { get; }

        public string AssemblyPrefix { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("UserDbConnection")));

            services.AddIdentity<User, Role>(
                options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequiredUniqueChars = 2;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 10;
                    options.Lockout.AllowedForNewUsers = true;

                    options.User.RequireUniqueEmail = true;
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.@";

                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddUserManager<UserManager>()
                .AddDefaultTokenProviders()
                .UseClaimsPrincipalFactory<UserWithNickNameClaimsPrincipalFactory, User>()
                .AddTokenProvider<Email2TokenProvider>("Email2");

            services.AddAuthentication()
                .AddCookie2(options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.Expiration = TimeSpan.FromDays(30);
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/access-denied";
                    options.SlidingExpiration = true;
                    options.Events = new CookieAuthenticationValidator();
                })
                .AddBasic(options =>
                {
                    options.Realm = "JudgeWeb";
                    options.AllowInsecureProtocol = true;
                    options.Events = new BasicAuthenticationValidator<User, Role, int, AppDbContext>();
                });

            services.AddSingleton(
                HtmlEncoder.Create(
                    UnicodeRanges.BasicLatin,
                    UnicodeRanges.CjkUnifiedIdeographs));

            services.AddEmailSender(options =>
                options.Bind(Configuration.GetSection("SendGrid")));

            services.AddOjUpdateService(
                Environment.IsDevelopment() ? 24 * 7 * 60 : 3 * 24 * 60);
            services.AddHostedService<ArchiveCacheService>();
            services.AddHostedService<Features.Scoreboard.ScoreboardUpdateService>();

            services.AddProblemRepository();
            services.AddMarkdown();

            services.AddMvc()
                .SetTokenTransform<SlugifyParameterTransformer>()
                .EnableContentFileResult()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .UseAreaParts(AssemblyPrefix, EnabledAreas);

            services.AddDefaultManagers();

            services.AddSwagger(options =>
            {
                foreach (var item in EnabledAreas)
                {
                    var xmlFile = $"{AssemblyPrefix}{item}.xml";
                    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (System.IO.File.Exists(xmlPath))
                        options.IncludeXmlComments(xmlPath);
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMiddleware<AjaxExceptionMiddleware>();
                app.UseDatabaseErrorPage();
                app.UseApiExplorer();
            }
            else if (Environment.EnvironmentName == "Test")
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });

                app.UseDeveloperExceptionPage();
                app.UseMiddleware<AjaxExceptionMiddleware>();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseMiddleware<RealIpMiddleware>();
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
