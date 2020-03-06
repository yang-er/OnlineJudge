using idunno.Authentication.Basic;
using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Mailing;
using JudgeWeb.Features.OjUpdate;
using JudgeWeb.Features.Scoreboard;
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
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace JudgeWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            AssemblyPrefix = "JudgeWeb.Areas.";
            EnabledAreas = new[] { "Misc", "Account", "Contest", "Dashboard", "Polygon" };
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

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

            services.AddApplicationInsightsTelemetry();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlServer(Configuration.GetConnectionString("UserDbConnection"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

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
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
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

            services.AddEmailSender()
                .Bind(Configuration.GetSection("SendGrid"));

            services.AddOjUpdateService(
                Environment.IsDevelopment() ? 24 * 7 * 60 : 3 * 24 * 60);
            services.AddHostedService<ArchiveCacheService>();
            services.AddScoreboard();

            services.AddProblemRepository();
            services.AddMarkdown();

            services.AddControllersWithViews()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new TimeSpanJsonConverter()))
                .SetTokenTransform<SlugifyParameterTransformer>()
                .ReplaceLinkGenerator()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .UseAreaParts(AssemblyPrefix, EnabledAreas);

            services.AddDefaultManagers();

            services.AddApiExplorer()
                .AddDocument("DOMjudge", "DOMjudge compact API v4", "7.2.0")
                .AddSecurityScheme("basic", Microsoft.OpenApi.Models.SecuritySchemeType.Http)
                .IncludeXmlComments(EnabledAreas.Select(item => System.IO.Path.Combine(AppContext.BaseDirectory, $"{AssemblyPrefix}{item}.xml")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMiddleware<AjaxExceptionMiddleware>();
                app.UseDatabaseErrorPage();
                app.UseStatusCodePage();
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
                app.UseStatusCodePage();
            }
            else
            {
                app.UseMiddleware<RealIpMiddleware>();
                app.UseExceptionHandler("/error");
                app.UseStatusCodePage();
                app.UseCatchException();
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapSwaggerUI("/api/doc")
                    .RequireRoles("Administrator");

                endpoints.MapFallbackNotFound("/api/{**slug}");

                endpoints.MapFallbackNotFound("/images/{**slug}");

                endpoints.MapFallbackNotFound("/static/{**slug}");

                endpoints.MapFallbackToAreaController(
                    pattern: "/dashboard/problems/{**slug}",
                    "NotFound2", "Root", "Polygon");

                endpoints.MapFallbackToAreaController(
                    pattern: "/dashboard/{**slug}",
                    "NotFound2", "Root", "Dashboard");

                endpoints.MapFallbackToAreaController(
                    pattern: "/polygon/{pid}/{**slug}",
                    "NotFound2", "Editor", "Polygon");

                endpoints.MapFallbackToAreaController(
                    pattern: "/gym/{cid}/{**slug}",
                    "NotFound2", "Gym", "Contest");

                endpoints.MapFallbackToAreaController(
                    pattern: "/contest/{cid}/jury/{**slug}",
                    "NotFound2", "Jury", "Contest");

                endpoints.MapFallbackToAreaController(
                    pattern: "/contest/{cid}/{**slug}",
                    "NotFound2", "Public", "Contest");

                endpoints.MapFallbackToAreaController(
                    "NotFound2", "Home", "Misc");
            });
        }
    }
}
