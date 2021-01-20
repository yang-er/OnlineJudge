using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JudgeWeb.Migration
{
    public class RoleRestoreService : BackgroundService
    {
        public UserManager UserManager { get; }

        public RoleManager<IdentityRole<int>> RoleManager { get; }

        public OldDbContext DbContext { get; }

        public AppDbContext AppDbContext { get; }

        public ILogger<UserRestoreService> Logger { get; }

        public RoleRestoreService(AppDbContext app, RoleManager<IdentityRole<int>> rm, UserManager um, OldDbContext odbc, ILogger<UserRestoreService> lg)
        {
            AppDbContext = app;
            UserManager = um;
            RoleManager = rm;
            DbContext = odbc;
            Logger = lg;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            /*
            var roleCreation = await RoleManager.CreateAsync(new IdentityRole<int>("Administrator"));
            if (!roleCreation.Succeeded)
            {

            }

            var blockCreation = await RoleManager.CreateAsync(new IdentityRole<int>("Blocked"));
            var problemCreation = await RoleManager.CreateAsync(new IdentityRole<int>("Problem"));
            var guideCreation = await RoleManager.CreateAsync(new IdentityRole<int>("Guide"));
            */

            var admins = await DbContext.Userj
                .Where(u => u.Usertype == "admin")
                .ToListAsync();

            foreach (var u in admins)
            {
                var uu = await UserManager.FindByIdAsync(u.Uid.ToString());
                await UserManager.AddToRoleAsync(uu, "Administrator");
            }
        }
    }
}
