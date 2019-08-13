using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternalErrorStatus = JudgeWeb.Data.InternalError.ErrorStatus;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 DOMjudge 对接的API控制器。
    /// </summary>
    [BasicAuthenticationFilter("DOMjudge API")]
    [Area("Api")]
    [Route("[area]/[controller]/[action]")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public class JudgehostsController : ControllerBase
    {
        /// <summary>
        /// 日志记录
        /// </summary>
        ILogger<JudgehostsController> Logger { get; }

        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 遥测客户端
        /// </summary>
        TelemetryClient Telemetry { get; }

        /// <summary>
        /// 异步锁
        /// </summary>
        static AsyncLock Lock { get; } = new AsyncLock();

        /// <summary>
        /// 构造 DOMjudge API 的控制器。
        /// </summary>
        /// <param name="logger">日志记录</param>
        /// <param name="rdbc">数据库</param>
        public JudgehostsController(
            ILogger<JudgehostsController> logger,
            AppDbContext rdbc, TelemetryClient telemetry)
        {
            Logger = logger;
            DbContext = rdbc;
            Telemetry = telemetry;
        }

        private bool AnyNull(params object[] objs) => objs.Any(s => s is null);

        private byte[] TryUnbase64(string src)
        {
            try
            {
                return Convert.FromBase64String(src);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private JsonResult Json(object value) => new JsonResult(value);

        /// <summary>
        /// 获取所有评测机的信息。
        /// </summary>
        [HttpGet("/[area]/[controller]")]
        public async Task<IActionResult> OnGet()
        {
            var lists = await DbContext.JudgeHosts
                .Select(h => new { h.Active, h.PollTime, h.ServerName })
                .ToListAsync();

            return Json(lists.Select(a => new
            {
                hostname = a.ServerName,
                active = a.Active,
                polltime = a.PollTime.ToUnixTimeSeconds(),
                polltime_formatted = a.PollTime.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            }));
        }

        /// <summary>
        /// 创建一个JudgeHost内部错误。
        /// </summary>
        /// <param name="model">内部错误信息</param>
        [HttpPost]
        public async Task<IActionResult> InternalError(InternalErrorModel model)
        {
            if (AnyNull(model.JudgehostLog, model.Description, model.Disabled))
                return BadRequest();

            var toDisable = JObject.Parse(model.Disabled);
            var kind = toDisable["kind"].Value<string>();

            if (kind == "language")
            {
                var langid = toDisable["langid"].Value<string>();
                var lang = await DbContext.Languages
                    .Where(l => l.ExternalId == langid)
                    .FirstOrDefaultAsync();

                if (lang != null)
                {
                    lang.AllowJudge = false;
                    DbContext.Languages.Update(lang);
                }
                else
                {
                    Logger.LogWarning("Unknown language id from judgehost: {langid}", langid);
                }

                DbContext.AuditLogs.Add(new AuditLog
                {
                    ContestId = model.ContestId ?? 0,
                    EntityId = lang.LangId,
                    Comment = "internal error created",
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Contest,
                    UserName = "judgehost",
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "Language",
                    dependencyName: langid,
                    data: model.Description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }
            else if (kind == "judgehost")
            {
                var hostname = toDisable["hostname"].Value<string>();
                var host = await DbContext.JudgeHosts
                    .Where(h => h.ServerName == hostname)
                    .FirstOrDefaultAsync();

                if (host != null)
                {
                    host.Active = false;
                    DbContext.JudgeHosts.Update(host);
                }
                else
                {
                    Logger.LogWarning("Unknown judgehost: {hostname}", hostname);
                }

                DbContext.AuditLogs.Add(new AuditLog
                {
                    ContestId = model.ContestId ?? 0,
                    EntityId = host.ServerId,
                    Comment = "internal error created",
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Contest,
                    UserName = host.ServerName,
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: host.ServerName,
                    data: model.Description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }
            else
            {
                Telemetry.TrackDependency(
                    dependencyTypeName: "Unresolved",
                    dependencyName: kind,
                    data: model.Description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }

            var ie = new InternalError
            {
                JudgehostLog = model.JudgehostLog,
                JudgingId = model.JudgingId,
                ContestId = model.ContestId,
                Description = model.Description,
                Disabled = model.Disabled,
                Status = InternalErrorStatus.Open,
                Time = DateTimeOffset.Now,
            };

            if (model.JudgingId.HasValue)
            {
                var grade = await DbContext.Judgings
                    .Where(s => s.JudgingId == model.JudgingId)
                    .FirstOrDefaultAsync();

                if (grade != null)
                {
                    grade.Status = Verdict.UndefinedError;
                    bool oldActive = grade.Active;
                    grade.Active = false;
                    DbContext.Judgings.Update(grade);

                    DbContext.Judgings.Add(new Judging
                    {
                        Status = Verdict.Pending,
                        FullTest = grade.FullTest,
                        SubmissionId = grade.SubmissionId,
                        Active = oldActive,
                    });
                }
            }

            DbContext.InternalErrors.Add(ie);
            await DbContext.SaveChangesAsync();
            return Json(ie.ErrorId);
        }

        /// <summary>
        /// 注册所给的 judgehost，并返回所有未完成的评测任务。
        /// </summary>
        /// <param name="model">HOSTNAME</param>
        [HttpPost("/[area]/[controller]")]
        public async Task<IActionResult> OnPut(CreateJudgeHostModel model)
        {
            var item = await DbContext.JudgeHosts
                .Where(h => h.ServerName == model.hostname)
                .FirstOrDefaultAsync();

            if (item is null)
            {
                DbContext.JudgeHosts.Add(new JudgeHost
                {
                    ServerName = model.hostname,
                    PollTime = DateTimeOffset.Now,
                    Active = true
                });

                DbContext.AuditLogs.Add(new AuditLog
                {
                    ContestId = 0,
                    Comment = $"judgehost {model.hostname} on {HttpContext.Connection.RemoteIpAddress} registered",
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Contest,
                    EntityId = 0,
                    UserName = "judgerest",
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: item.ServerName,
                    data: "registed",
                    startTime: item.PollTime,
                    duration: TimeSpan.Zero,
                    success: true);

                await DbContext.SaveChangesAsync();
                return Json(new object[0]);
            }
            else
            {
                item.PollTime = DateTimeOffset.Now;
                DbContext.JudgeHosts.Update(item);

                var items = await (
                    from g in DbContext.Judgings
                    where g.Status == Verdict.Running && g.ServerId == item.ServerId
                    join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                    select new { g, s.ContestId }
                ).ToListAsync();

                foreach (var sg in items)
                {
                    var s = sg.g;
                    bool oldActive = s.Active;
                    s.Status = Verdict.UndefinedError;
                    s.StopTime = DateTimeOffset.Now;
                    s.Active = false;

                    Telemetry.TrackDependency(
                        dependencyTypeName: "JudgeHost",
                        dependencyName: item.ServerName,
                        data: $"j{s.JudgingId} of s{s.SubmissionId} returned to queue",
                        startTime: s.StartTime ?? DateTimeOffset.Now,
                        duration: (s.StopTime - s.StartTime) ?? TimeSpan.Zero,
                        success: false);

                    DbContext.Judgings.Update(s);
                    DbContext.Judgings.Add(new Judging
                    {
                        SubmissionId = s.SubmissionId,
                        Status = Verdict.Pending,
                        Active = oldActive,
                        FullTest = s.FullTest,
                    });
                }

                await DbContext.SaveChangesAsync();
                return Json(items.Select(s => new
                {
                    judgingid = s.g.JudgingId,
                    submitid = s.g.SubmissionId,
                    cid = s.ContestId
                }));
            }
        }

        /// <summary>
        /// 给评测机分配一个新的评测任务。
        /// </summary>
        /// <param name="hostname">评测机</param>
        [HttpPost("{hostname}")]
        public async Task<IActionResult> NextJudging([FromRoute] string hostname)
        {
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return Json("");
            host.PollTime = DateTimeOffset.Now;
            DbContext.JudgeHosts.Update(host);

            if (!host.Active)
            {
                await DbContext.SaveChangesAsync();
                return Json("");
            }

            Language lang;
            Problem problem;
            Judging judging;
            int cccid, teamid;

            using (await Lock.LockAsync())
            {
                var next = await (
                    from g in DbContext.Judgings
                    where g.Status == Verdict.Pending
                    join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                    join l in DbContext.Languages on s.Language equals l.LangId
                    join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                    where l.AllowJudge
                    select new { g, l, p, s.ContestId, s.Author }
                ).FirstOrDefaultAsync();

                if (next is null)
                {
                    await DbContext.SaveChangesAsync();
                    return Json("");
                }

                lang = next.l;
                problem = next.p;
                cccid = next.ContestId;
                teamid = next.Author;
                judging = next.g;
                judging.Status = Verdict.Running;
                judging.ServerId = host.ServerId;
                judging.StartTime = DateTimeOffset.Now;
                DbContext.Judgings.Update(judging);
                await DbContext.SaveChangesAsync();
            }

            int memlimit = problem.MemoryLimit + (lang.ExternalId == "java" ? 131072 : 0);
            int timelimit = (int)(problem.TimeLimit * lang.TimeFactor);

            var toFindMd5 = new[] { problem.RunScript, problem.CompareScript, lang.CompileScript };
            var md5s = await DbContext.Executable
                .Where(e => toFindMd5.Contains(e.ExecId))
                .Select(e => new { e.ExecId, e.Md5sum })
                .ToListAsync();

            var tcs = await DbContext.Testcases
                .Where(t => t.ProblemId == problem.ProblemId)
                .Select(t => new TestcaseNextToJudgeModel
                {
                    TestcaseId = t.TestcaseId,
                    ProblemId = t.ProblemId,
                    Md5sumInput = t.Md5sumInput,
                    Md5sumOutput = t.Md5sumOutput,
                    Rank = t.Rank
                }).ToListAsync();

            tcs.Sort((t, t2) => t.Rank.CompareTo(t2.Rank));
            var tcjo = new JObject();
            tcs.ForEach(t => tcjo[t.Rank.ToString()] = JToken.FromObject(t));

            return Json(new SubmissionNextToJudgeModel
            {
                SubmitId = judging.SubmissionId,
                ContestId = cccid,
                TeamId = teamid,
                ProblemId = problem.ProblemId,
                LanguageId = lang.ExternalId,
                RejudgingId = judging.RejudgeId,
                EntryPoint = null,
                OrigSubmitId = null,
                MaxRunTime = timelimit / 1000.0, // as seconds
                MemLimit = memlimit, // as kb
                OutputLimit = 12288, // KB
                Run = problem.RunScript,
                Compare = problem.CompareScript,
                CompareArgs = problem.ComapreArguments,
                CompileScript = lang.CompileScript,
                CombinedRunCompare = problem.CombinedRunCompare,
                CompareMd5sum = md5s.First(a => a.ExecId == problem.CompareScript).Md5sum,
                RunMd5sum = md5s.First(a => a.ExecId == problem.RunScript).Md5sum,
                CompileScriptMd5sum = md5s.First(a => a.ExecId == lang.CompileScript).Md5sum,
                JudgingId = judging.JudgingId,
                Testcases = tcjo
            });
        }

        /// <summary>
        /// 更新判题编译结果。
        /// </summary>
        /// <param name="hostname">主机名</param>
        /// <param name="jid">提交ID</param>
        /// <param name="model">更新数据</param>
        [HttpPut("{hostname}/{jid}")]
        public async Task<IActionResult> UpdateJudging([FromRoute] string hostname, [FromRoute] int jid, UpdateJudgingModel model)
        {
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return Json("");
            host.PollTime = DateTimeOffset.Now;
            DbContext.JudgeHosts.Update(host);

            var judging = await DbContext.Judgings
                .Where(g => g.JudgingId == jid)
                .FirstOrDefaultAsync();

            if (judging is null) return BadRequest();
            judging.CompileError = model.output_compile ?? "";

            if (model.compile_success != 1)
            {
                judging.Status = Verdict.CompileError;
                judging.StopTime = DateTimeOffset.Now;

                var cid = await (
                    from j in DbContext.Judgings
                    where j.JudgingId == jid
                    join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                    select s.ContestId
                ).FirstAsync();

                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = $"judged {judging.Status}",
                    EntityId = judging.JudgingId,
                    ContestId = cid,
                    Resolved = cid == 0,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Judging,
                    UserName = hostname,
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: host.ServerName,
                    data: $"j{judging.JudgingId} judged " + Verdict.CompileError,
                    startTime: judging.StartTime ?? DateTimeOffset.Now,
                    duration: (judging.StopTime - judging.StartTime) ?? TimeSpan.Zero,
                    success: true);
            }

            judging.ServerId = host.ServerId;
            DbContext.Judgings.Update(judging);
            await DbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// 更新判题编译结果。
        /// </summary>
        /// <param name="hostname">主机名</param>
        /// <param name="jid">提交ID</param>
        /// <param name="model">更新数据</param>
        [HttpPost("{hostname}/{jid}")]
        public async Task<IActionResult> AddJudgingRun([FromRoute] string hostname, [FromRoute] int jid, AddJudgingRunModel model)
        {
            var batches = model.Parse();
            if (batches is null) return BadRequest();

            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return Json("");
            host.PollTime = DateTimeOffset.Now;
            DbContext.JudgeHosts.Update(host);

            var runEntities = batches
                .Select(b => (b, DbContext.Details.Add(b.ParseInfo(jid))))
                .ToList();

            await DbContext.SaveChangesAsync();

            var io = HttpContext.RequestServices.GetRequiredService<IFileRepository>();
            io.SetContext("Runs");
            foreach (var (b, e) in runEntities)
            {
                var stderr = TryUnbase64(b.OutputError);
                var stdout = TryUnbase64(b.OutputRun);

                try
                {
                    await io.WriteBinaryAsync($"j{jid}", $"r{e.Entity.TestId}.out", stdout);
                    await io.WriteBinaryAsync($"j{jid}", $"r{e.Entity.TestId}.err", stderr);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred when saving OutputError and OutputRun for j{jid}", jid);
                }
            }

            var judging = await (
                from g in DbContext.Judgings
                where g.JudgingId == jid
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                select new { g, s.ProblemId, s.ContestId }
            ).FirstOrDefaultAsync();

            if (judging is null)
            {
                Logger.LogError("Unknown judging result: " + model.batch);
                return BadRequest();
            }

            // Check for the final status
            var countOfTestcase = await DbContext.Testcases
                .CountAsync(t => t.ProblemId == judging.ProblemId);
            var verdictsOfThis = await DbContext.Details
                .Where(d => d.JudgingId == jid)
                .GroupBy(d => d.JudgingId)
                .Select(g => new
                    {
                        Status = g.Min(d => d.Status),
                        Count = g.Count(),
                        Memory = g.Max(d => d.ExecuteMemory),
                        Time = g.Max(d => d.ExecuteTime),
                    })
                .FirstOrDefaultAsync();

            bool anyRejected = !judging.g.FullTest && verdictsOfThis.Status != Verdict.Accepted;
            bool fullTested = verdictsOfThis.Count >= countOfTestcase && countOfTestcase > 0;

            if (anyRejected || fullTested)
            {
                judging.g.ExecuteMemory = verdictsOfThis.Memory;
                judging.g.ExecuteTime = verdictsOfThis.Time;
                judging.g.Status = verdictsOfThis.Status;
                judging.g.StopTime = host.PollTime;
                DbContext.Judgings.Update(judging.g);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = $"judged {verdictsOfThis.Status}",
                    EntityId = judging.g.JudgingId,
                    ContestId = judging.ContestId,
                    Resolved = judging.ContestId == 0,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Judging,
                    UserName = hostname,
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: host.ServerName,
                    data: $"j{judging.g.JudgingId} judged " + verdictsOfThis.Status,
                    startTime: judging.g.StartTime ?? DateTimeOffset.Now,
                    duration: (judging.g.StopTime - judging.g.StartTime) ?? TimeSpan.Zero,
                    success: true);

                await DbContext.SaveChangesAsync();

                Features.Scoreboard.RefreshService.Notify();
            }

            return Ok();
        }
    }
}
