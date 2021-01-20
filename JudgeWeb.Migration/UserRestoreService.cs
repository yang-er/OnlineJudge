using JudgeWeb.Data;
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
    public class UserRestoreService : BackgroundService
    {
        public UserManager UserManager { get; }

        public OldDbContext DbContext { get; }

        public AppDbContext AppDbContext { get; }

        public ILogger<UserRestoreService> Logger { get; }

        public UserRestoreService(AppDbContext app, UserManager um, OldDbContext odbc, ILogger<UserRestoreService> lg)
        {
            AppDbContext = app;
            UserManager = um;
            DbContext = odbc;
            Logger = lg;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(2000);

            var ul = await DbContext.Userj
                .OrderBy(u => u.Uid)
                .Skip(25000)
                .Take(5000)
                .ToListAsync();

            foreach (var item in ul)
            {
                Logger.LogInformation("UserName = {un}, Email = {mail}, UserId = {uid}, Role = {role}", item.Userid, item.Mail, item.Uid, item.Usertype);

                item.Name = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Name));
                item.Userid = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Userid));
                item.Plan = Encoding.GetEncoding(936).GetString(Encoding.GetEncoding("ISO8859-1").GetBytes(item.Plan));

                if (await UserManager.FindByNameAsync(item.Userid) != null)
                    continue;

                if (item.Mail == null || !item.Mail.Contains('@'))
                {
                    item.Mail = item.Userid + "@" + item.Usertype + ".jlucpc";
                }


                DateTimeOffset? reg = item.RegDate < DateTime.UnixEpoch
                    ? default(DateTimeOffset?) : new DateTimeOffset(item.RegDate, TimeSpan.FromHours(8));

                var res = await UserManager.CreateAsync(new User
                {
                    UserName = item.Userid,
                    Email = item.Mail,
                    RegisterTime = reg,
                    PasswordHash = string.IsNullOrEmpty(item.Password) ? null : item.Password,
                    Plan = item.Plan,
                    NickName = item.Name != item.Userid ? item.Name : null,
                });

                if (!res.Succeeded)
                {
                    
                }

                var user = await UserManager.FindByNameAsync(item.Userid);

                if (user.Id != item.Uid)
                {

                }

                if (item.Name != item.Userid)
                    await UserManager.AddClaimAsync(user, new System.Security.Claims.Claim("XYS.NickName", item.Name));
            }
        }
    }
}
