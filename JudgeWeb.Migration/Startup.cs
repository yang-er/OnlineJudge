using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace JudgeWeb.Migration
{
    public class Startup
    {
        public Startup()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Environment.CurrentDirectory = Environment.CurrentDirectory + "/../../../../JudgeWeb";
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<IdentityOptions>(options =>
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

                options.User.RequireUniqueEmail = false;
                options.User.AllowedUserNameCharacters = null;// "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.@";

                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            });

            services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(
                    Configuration.GetConnectionString("UserDbConnection")), ServiceLifetime.Singleton);

            services.AddDbContext<OldDbContext>(options =>
                options.UseMySQL(
                    Configuration.GetConnectionString("OldDbConnection")), ServiceLifetime.Singleton);

            services.AddIdentity<User, IdentityRole<int>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddUserManager<UserManager>()
                .AddDefaultTokenProviders();

            services.Replace(ServiceDescriptor.Scoped<IPasswordHasher<User>, MySqlOldPasswordHasher<User>>());

            services.AddHostedService<ProblemDescriptionRestoreService>();

            services.AddProblemRepository();
            services.AddMarkdown();
            services.AddDefaultManagers();

            var ass = System.Reflection.Assembly.Load("JudgeWeb.Areas.Judge");
            var wtf = System.Reflection.CustomAttributeExtensions.GetCustomAttributes<InjectAttribute>(ass);
            foreach (var item in wtf) services.Add(item.GetDescriptior());
        }

        public static IHostBuilder CreateDefaultBuilder(string[] args)
        {
            var startup = new Startup();
            return new HostBuilder()
                .ConfigureServices(startup.ConfigureServices)
                .ConfigureLogging(lb => lb.AddConsole().AddDebug());
        }
    }
}
