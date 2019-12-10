using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using InternalErrorStatus = JudgeWeb.Data.InternalError.ErrorStatus;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 DOMjudge 对接的API控制器。
    /// </summary>
    [Area("Api")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "Judgehost")]
    [Route("[area]/[controller]")]
    [Produces("application/json")]
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
        /// <param name="telemetry">遥测终端</param>
        public JudgehostsController(
            ILogger<JudgehostsController> logger,
            AppDbContext rdbc, TelemetryClient telemetry)
        {
            Logger = logger;
            DbContext = rdbc;
            Telemetry = telemetry;
        }

        /// <summary>
        /// 尝试解Base64编码
        /// </summary>
        /// <param name="src">原字符串</param>
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

        /// <summary>
        /// Json返回空字符串
        /// </summary>
        private JsonResult JsonEmpty() => new JsonResult("");


        /// <summary>
        /// Get judgehosts
        /// </summary>
        /// <param name="hostname">Only show the judgehost with the given hostname</param>
        /// <response code="200">The judgehosts</response>
        [HttpGet]
        public async Task<ActionResult<List<Judgehost>>> OnGet(string hostname)
        {
            IQueryable<JudgeHost> query = DbContext.JudgeHosts;
            if (hostname != null)
                query = query.Where(h => h.ServerName == hostname);

            return await query
                .Select(a => new Judgehost
                {
                    hostname = a.ServerName,
                    active = a.Active,
                    polltime = a.PollTime.ToUnixTimeSeconds(),
                    polltime_formatted = a.PollTime.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                })
                .ToListAsync();
        }


        /// <summary>
        /// Add a new judgehost to the list of judgehosts
        /// </summary>
        /// <remarks>
        /// Also restarts (and returns) unfinished judgings.
        /// </remarks>
        /// <param name="hostname">The name of added judgehost</param>
        /// <response code="200">The returned unfinished judgings</response>
        [HttpPost]
        public async Task<ActionResult<List<UnfinishedJudging>>> OnPost(
            [Required] string hostname)
        {
            var item = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            if (item is null)
            {
                item = new JudgeHost
                {
                    ServerName = hostname,
                    PollTime = DateTimeOffset.Now,
                    Active = true
                };

                DbContext.JudgeHosts.Add(item);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    ContestId = 0,
                    Comment = $"judgehost {hostname} on {HttpContext.Connection.RemoteIpAddress} registered",
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
                return new List<UnfinishedJudging>();
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
                return items.Select(s =>
                    new UnfinishedJudging
                    {
                        judgingid = s.g.JudgingId,
                        submitid = s.g.SubmissionId,
                        cid = s.ContestId
                    })
                    .ToList();
            }
        }


        /// <summary>
        /// Update the configuration of the given judgehost
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost to update</param>
        /// <param name="active">The new active state of the judgehost</param>
        /// <response code="200">The modified judgehost</response>
        [HttpPut("{hostname}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<Judgehost>>> OnPut(
            [FromRoute] string hostname, bool active)
        {
            var item = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();
            if (item == null) return NotFound();

            item.Active = active;
            DbContext.JudgeHosts.Update(item);

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = 0,
                Comment = $"judgehost {hostname} set {(active ? "" : "in")}active by {HttpContext.Connection.RemoteIpAddress}",
                Resolved = true,
                Time = DateTimeOffset.Now,
                Type = AuditLog.TargetType.Contest,
                EntityId = 0,
                UserName = "judgerest",
            });

            await DbContext.SaveChangesAsync();

            return new[]
            {
                new Judgehost
                {
                    hostname = item.ServerName,
                    active = item.Active,
                    polltime = item.PollTime.ToUnixTimeSeconds(),
                    polltime_formatted = item.PollTime.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                }
            };
        }


        /// <summary>
        /// Get the next judging for the given judgehost
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost to get the next judging for</param>
        /// <response code="200">The next judging to judge</response>
        [HttpPost("[action]/{hostname}")]
        public async Task<ActionResult<NextJudging>> NextJudging(
            [FromRoute] string hostname)
        {
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return JsonEmpty();
            host.PollTime = DateTimeOffset.Now;
            DbContext.JudgeHosts.Update(host);

            if (!host.Active)
            {
                await DbContext.SaveChangesAsync();
                return JsonEmpty();
            }

            Language lang;
            Problem problem;
            Judging judging;
            int cccid, teamid;
            int? rjid;

            using (await Lock.LockAsync())
            {
                var next = await (
                    from g in DbContext.Judgings
                    where g.Status == Verdict.Pending
                    join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                    join l in DbContext.Languages on s.Language equals l.LangId
                    join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                    where l.AllowJudge
                    select new { g, l, p, s.ContestId, s.Author, s.RejudgeId }
                ).FirstOrDefaultAsync();

                if (next is null)
                {
                    await DbContext.SaveChangesAsync();
                    return JsonEmpty();
                }

                lang = next.l;
                problem = next.p;
                cccid = next.ContestId;
                teamid = next.Author;
                judging = next.g;
                rjid = next.RejudgeId;
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
                .Select(t =>
                    new TestcaseToJudge
                    {
                        testcaseid = t.TestcaseId,
                        probid = t.ProblemId,
                        md5sum_input = t.Md5sumInput,
                        md5sum_output = t.Md5sumOutput,
                        rank = t.Rank
                    })
                .ToDictionaryAsync(k => k.rank.ToString());

            return new NextJudging
            {
                submitid = judging.SubmissionId,
                cid = cccid,
                teamid = teamid,
                probid = problem.ProblemId,
                langid = lang.ExternalId,
                rejudgingid = rjid,
                entry_point = null,
                origsubmitid = null,
                maxruntime = timelimit / 1000.0, // as seconds
                memlimit = memlimit, // as kb
                outputlimit = 12288, // KB
                run = problem.RunScript,
                compare = problem.CompareScript,
                compare_args = problem.ComapreArguments,
                compile_script = lang.CompileScript,
                combined_run_compare = problem.CombinedRunCompare,
                compare_md5sum = md5s.First(a => a.ExecId == problem.CompareScript).Md5sum,
                run_md5sum = md5s.First(a => a.ExecId == problem.RunScript).Md5sum,
                compile_script_md5sum = md5s.First(a => a.ExecId == lang.CompileScript).Md5sum,
                judgingid = judging.JudgingId,
                testcases = tcs
            };
        }


        /// <summary>
        /// Update the given judging for the given judgehost
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost that wants to update the judging</param>
        /// <param name="judgingId">The ID of the judging to update</param>
        /// <response code="200">When the judging has been updated</response>
        /// <param name="model">Model</param>
        [HttpPut("[action]/{hostname}/{judgingId}")]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1 << 30, KeyLengthLimit = 1 << 30,
            MultipartBoundaryLengthLimit = 1 << 30, MultipartHeadersCountLimit = 1 << 30,
            MultipartHeadersLengthLimit = 1 << 30, BufferBodyLengthLimit = 1 << 30,
            ValueCountLimit = 1 << 30, ValueLengthLimit = 1 << 30)]
        public async Task<IActionResult> UpdateJudging(
            [FromRoute] string hostname,
            [FromRoute] int judgingId,
            UpdateJudgingModel model)
        {
            int jid = judgingId;
            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return JsonEmpty();
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
        /// Add an array of JudgingRuns. When relevant, finalize the judging.
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost that wants to add the judging run</param>
        /// <param name="judgingId">The ID of the judging to add a run to</param>
        /// <param name="model">Model</param>
        /// <response code="200">When the judging run has been added</response>
        [HttpPost("[action]/{hostname}/{judgingId}")]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1 << 30, KeyLengthLimit = 1 << 30,
            MultipartBoundaryLengthLimit = 1 << 30, MultipartHeadersCountLimit = 1 << 30,
            MultipartHeadersLengthLimit = 1 << 30, BufferBodyLengthLimit = 1 << 30,
            ValueCountLimit = 1 << 30, ValueLengthLimit = 1<< 30)]
        public async Task<IActionResult> AddJudgingRun(
            [FromRoute] string hostname,
            [FromRoute] int judgingId,
            AddJudgingRunModel model)
        {
            int jid = judgingId;
            var batches = model.Parse();
            if (batches is null) return BadRequest();

            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return JsonEmpty();
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
                if (b.OutputError is null || b.OutputRun is null)
                    continue;
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


        /// <summary>
        ///  Internal error reporting (back from judgehost)
        /// </summary>
        /// <param name="model">Model</param>
        /// <response code="200">The ID of the created internal error</response>
        [HttpPost("[action]")]
        [RequestSizeLimit(1 << 26)]
        public async Task<ActionResult<int>> InternalError(InternalErrorModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var toDisable = JObject.Parse(model.disabled);
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
                    ContestId = model.cid ?? 0,
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
                    data: model.description,
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
                    ContestId = model.cid ?? 0,
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
                    data: model.description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }
            else
            {
                Telemetry.TrackDependency(
                    dependencyTypeName: "Unresolved",
                    dependencyName: kind,
                    data: model.description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }

            var ie = new InternalError
            {
                JudgehostLog = model.judgehostlog,
                JudgingId = model.judgingid,
                ContestId = model.cid,
                Description = model.description,
                Disabled = model.disabled,
                Status = InternalErrorStatus.Open,
                Time = DateTimeOffset.Now,
            };

            if (model.judgingid.HasValue)
            {
                var grade = await DbContext.Judgings
                    .Where(s => s.JudgingId == model.judgingid)
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
            return ie.ErrorId;
        }
    }
}
