using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: Inject(typeof(JudgeManager))]
namespace JudgeWeb.Areas.Judge.Services
{
    public class JudgeManager
    {
        private AppDbContext DbContext { get; }

        private IFileProvider FileProvider { get; }

        public JudgeManager(AppDbContext adbc, IHostingEnvironment he)
        {
            DbContext = adbc;
            FileProvider = he.WebRootFileProvider;
        }

        public IDirectoryContents GetImages()
        {
            return FileProvider.GetDirectoryContents("images/problem");
        }

        public Task<List<Executable>> GetExecutablesByProblemEditorAsync()
        {
            return DbContext.Executable
                .Where(e => e.Type == "run" || e.Type == "compare")
                .Select(e =>
                    new Executable
                    {
                        Type = e.Type,
                        Description = e.Description,
                        ExecId = e.ExecId,
                    })
                .ToListAsync();
        }

        public async Task<List<ContestListModel>> GetContestsAsync(int uid)
        {
            var cts = await DbContext.Contests
                .GroupJoin(
                    inner: DbContext.Teams,
                    outerKeySelector: c => c.ContestId,
                    innerKeySelector: t => t.ContestId,
                    resultSelector: (c, ts) =>
                        new ContestListModel
                        {
                            Name = c.Name,
                            RankingStrategy = c.RankingStrategy,
                            ContestId = c.ContestId,
                            EndTime = c.EndTime,
                            IsPublic = c.IsPublic,
                            StartTime = c.StartTime,
                            TeamCount = ts.Count(),
                            OpenRegister = c.RegisterDefaultCategory > 0
                        })
                .ToListAsync();

            cts.Sort();

            if (uid > 0)
            {
                var cids = await DbContext.Teams
                    .Where(t => t.UserId == uid)
                    .Select(t => t.ContestId)
                    .ToArrayAsync();
                foreach (var cid in cids)
                    cts.First(c => c.ContestId == cid).IsRegistered = true;
            }

            return cts;
        }

        public async Task<int> CreateContestAsync(string username)
        {
            var c = DbContext.Contests.Add(new Contest
            {
                IsPublic = false,
                RegisterDefaultCategory = 0,
                ShortName = "",
                Name = "",
            });

            await DbContext.SaveChangesAsync();

            if (username != null)
            {
                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = "created",
                    UserName = username,
                    ContestId = c.Entity.ContestId,
                    EntityId = c.Entity.ContestId,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Contest,
                });

                await DbContext.SaveChangesAsync();
            }

            return c.Entity.ContestId;
        }
    }
}
