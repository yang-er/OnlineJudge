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
using InternalErrorStatus = JudgeWeb.Data.InternalError.ErrorStatus;

[assembly: Inject(typeof(JudgeManager))]
namespace JudgeWeb.Areas.Judge.Services
{
    public class JudgeManager
    {
        static readonly string[] ExecType = new[] { "compile", "compare", "run" };

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

        public Task<List<Executable>> GetExecutablesAsync()
        {
            return DbContext.Executable
                .WithoutBlob()
                .ToListAsync();
        }

        public async Task ToggleJudgehostAsync(string hostname)
        {
            var cur = await DbContext.JudgeHosts
                .Where(l => l.ServerName == hostname)
                .FirstOrDefaultAsync();

            if (cur != null)
            {
                cur.Active = !cur.Active;
                DbContext.JudgeHosts.Update(cur);
            }

            await DbContext.SaveChangesAsync();
        }

        public async Task<bool> UpdateExecutableAsync(
            string execid, string description, string type, (byte[], string)? file)
        {
            var exec = await DbContext.Executable
                .Where(e => e.ExecId == execid)
                .FirstOrDefaultAsync();

            if (exec is null)
            {
                if (string.IsNullOrWhiteSpace(description)
                        || ExecType.Contains(type) || !file.HasValue)
                    return false;

                var uploaded = file.Value;
                DbContext.Executable.Add(new Executable
                {
                    Md5sum = uploaded.Item2,
                    ZipFile = uploaded.Item1,
                    Description = description,
                    ExecId = execid,
                    Type = type,
                });
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(type) && !ExecType.Contains(type))
                    return false;
                if (!string.IsNullOrWhiteSpace(description))
                    exec.Description = description.Trim();

                if (file.HasValue)
                {
                    var uploaded = file.Value;
                    exec.Md5sum = uploaded.Item2;
                    exec.ZipFile = uploaded.Item1;
                }

                DbContext.Executable.Update(exec);
            }

            await DbContext.SaveChangesAsync();
            return true;
        }

        public async Task<InternalError> UpsolveInternalErrorAsync(int eid, string todo)
        {
            var ie = await DbContext.InternalErrors
                .Where(i => i.ErrorId == eid)
                .FirstOrDefaultAsync();
            if (ie is null) return null;

            if (ie.Status == InternalErrorStatus.Open && todo != null)
            {
                ie.Status = todo == "resolve"
                          ? InternalErrorStatus.Resolved
                          : todo == "ignore"
                          ? InternalErrorStatus.Ignored
                          : InternalErrorStatus.Open;

                if (ie.Status != InternalErrorStatus.Open)
                {
                    DbContext.InternalErrors.Update(ie);
                    var toDisable = JObject.Parse(ie.Disabled);
                    var kind = toDisable["kind"].Value<string>();

                    if (kind == "language")
                    {
                        var langid = toDisable["langid"].Value<string>();
                        var lang = await DbContext.Languages
                            .Where(l => l.ExternalId == langid)
                            .FirstOrDefaultAsync();

                        if (lang != null)
                        {
                            lang.AllowJudge = true;
                            DbContext.Languages.Update(lang);
                        }
                    }
                    else if (kind == "judgehost")
                    {
                        var hostname = toDisable["hostname"].Value<string>();
                        var host = await DbContext.JudgeHosts
                            .Where(h => h.ServerName == hostname)
                            .FirstOrDefaultAsync();

                        if (host != null)
                        {
                            host.Active = true;
                            DbContext.JudgeHosts.Update(host);
                        }
                    }

                    await DbContext.SaveChangesAsync();
                }
            }

            return ie;
        }

        public async Task UpdateAffiliationAsync(int affid, TeamAffiliation model)
        {
            if (affid == 0)
            {
                DbContext.Add(model);
            }
            else
            {
                var aff = await DbContext.TeamAffiliations
                    .FirstAsync(a => a.AffiliationId == affid);
                aff.ExternalId = model.ExternalId;
                aff.FormalName = model.FormalName;
                DbContext.TeamAffiliations.Update(aff);
            }

            await DbContext.SaveChangesAsync();
        }

        public Task<TeamAffiliation> GetAffiliationAsync(int affid)
        {
            if (affid == 0) return Task.FromResult(new TeamAffiliation());
            return DbContext.TeamAffiliations.FirstAsync(a => a.AffiliationId == affid);
        }

        public Task<List<TeamAffiliation>> GetAffiliationsAsync()
        {
            return DbContext.TeamAffiliations.ToListAsync();
        }

        public Task<byte[]> GetExecutableAsync(string execid)
        {
            return DbContext.Executable
                .Where(e => e.ExecId == execid)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
        }

        public Task<List<JudgeHost>> GetJudgehostsAsync()
        {
            return DbContext.JudgeHosts
                .ToListAsync();
        }

        public Task<JudgeHost> GetJudgehostAsync(string hostname)
        {
            return DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<(Judging, IEnumerable<Detail>, int)>, int)> ListJudgingsByServerIdAsync(int hid)
        {
            var gradeQuery =
                from g in DbContext.Judgings
                where g.ServerId == hid
                orderby g.JudgingId descending
                select g;

            var detailQuery =
                from g in gradeQuery.Take(100)
                join d in DbContext.Details on g.JudgingId equals d.JudgingId into dd
                select new { g, dd = dd.ToList() };

            var result = await detailQuery.ToListAsync();

            var counts = await DbContext.Judgings
                .Where(g => g.ServerId == hid)
                .CountAsync();

            return (result.Select(gg => (gg.g, gg.dd.AsEnumerable(), gg.g.SubmissionId)), counts);
        }

        public Task<List<InternalError>> ListInternalErrorsAsync()
        {
            return DbContext.InternalErrors
                .Select(
                    e => new InternalError
                    {
                        ErrorId = e.ErrorId,
                        Status = e.Status,
                        Time = e.Time,
                        Description = e.Description,
                    })
                .OrderByDescending(e => e.ErrorId)
                .ToListAsync();
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
