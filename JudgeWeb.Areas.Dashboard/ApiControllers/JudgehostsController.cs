using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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


        private async Task FinishJudging(
            Judging j, string hostname, Contest c,
            int pid, int teamid, DateTimeOffset submitTime)
        {
            DbContext.Judgings.Update(j);

            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = "judged",
                ExtraInfo = $"{j.Status}",
                ContestId = c?.ContestId,
                DataId = $"{j.JudgingId}",
                UserName = hostname,
                Time = DateTimeOffset.Now,
                DataType = AuditlogType.Judging,
            });

            Telemetry.TrackDependency(
                dependencyTypeName: "JudgeHost",
                dependencyName: hostname,
                data: $"j{j.JudgingId} judged " + j.Status,
                startTime: j.StartTime ?? DateTimeOffset.Now,
                duration: (j.StopTime - j.StartTime) ?? TimeSpan.Zero,
                success: j.Status != Verdict.UndefinedError);

            if (c != null)
            {
                var its = new Data.Api.ContestJudgement(
                    j, c.StartTime ?? DateTimeOffset.Now);
                DbContext.Events.Add(its.ToEvent("update", c.ContestId));
            }

            await DbContext.SaveChangesAsync();

            if (c != null && j.Active)
                HttpContext.RequestServices
                    .GetRequiredService<IScoreboardService>()
                    .JudgingFinished(c, submitTime, pid, teamid, j);
        }


        private async Task ReturnToQueue(Judging j, int cid, DateTimeOffset? cst)
        {
            DbContext.Judgings.Add(new Judging
            {
                Active = j.Active,
                Status = Verdict.Pending,
                FullTest = j.FullTest,
                RejudgeId = j.RejudgeId,
                PreviousJudgingId = j.PreviousJudgingId,
                SubmissionId = j.SubmissionId,
            });

            j.Active = false;
            j.Status = Verdict.UndefinedError;
            j.RejudgeId = null;
            j.PreviousJudgingId = null;
            if (!j.StopTime.HasValue)
                j.StopTime = DateTimeOffset.Now;

            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = "judged",
                ExtraInfo = $"{j.Status}",
                ContestId = cid == 0 ? default(int?) : cid,
                DataId = $"{j.JudgingId}",
                UserName = j.Server,
                Time = DateTimeOffset.Now,
                DataType = AuditlogType.Judging,
            });

            if (cid != 0)
            {
                var j2 = new Data.Api.ContestJudgement(
                    j, cst ?? DateTimeOffset.Now);
                DbContext.Events.Add(j2.ToEvent("update", cid));
            }

            DbContext.Judgings.Update(j);
            await DbContext.SaveChangesAsync();

            Telemetry.TrackDependency(
                dependencyTypeName: "JudgeHost",
                dependencyName: j.Server,
                data: $"j{j.JudgingId} of s{j.SubmissionId} returned to queue",
                startTime: j.StartTime ?? DateTimeOffset.Now,
                duration: (j.StopTime - j.StartTime) ?? TimeSpan.Zero,
                success: false);
        }


        private async Task ReturnToQueue(int jid)
        {
            var query =
                from j in DbContext.Judgings
                where j.JudgingId == jid
                join ss in DbContext.Submissions on j.SubmissionId equals ss.SubmissionId
                join c in DbContext.Contests on ss.ContestId equals c.ContestId into cc
                from c in cc.DefaultIfEmpty()
                select new { j, c.StartTime, ss.ContestId };
            var result = await query.SingleAsync();
            await ReturnToQueue(result.j, result.ContestId, result.StartTime);
        }


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
            [FromForm, Required] string hostname)
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

                var userManager = HttpContext.RequestServices
                    .GetRequiredService<UserManager>();

                DbContext.Auditlogs.Add(new Auditlog
                {
                    Action = "registered",
                    DataType = AuditlogType.Judgehost,
                    DataId = hostname,
                    ExtraInfo = $"on {HttpContext.Connection.RemoteIpAddress}",
                    Time = DateTimeOffset.Now,
                    UserName = userManager.GetUserName(User),
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

                var oldjudgingQuery =
                    from j in DbContext.Judgings
                    where j.Server == item.ServerName && j.Status == Verdict.Running
                    join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                    join c in DbContext.Contests on s.ContestId equals c.ContestId into cc
                    from c in cc.DefaultIfEmpty()
                    select new { j, cid = s.ContestId, c.StartTime };
                var oldJudgings = await oldjudgingQuery.ToListAsync();

                foreach (var sg in oldJudgings)
                    await ReturnToQueue(sg.j, sg.cid, sg.StartTime);

                return oldJudgings
                    .Select(s => new UnfinishedJudging
                    {
                        judgingid = s.j.JudgingId,
                        submitid = s.j.SubmissionId,
                        cid = s.cid
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

            var userManager = HttpContext.RequestServices
                .GetRequiredService<UserManager>();

            DbContext.Auditlogs.Add(new Auditlog
            {
                ExtraInfo = $"by api {HttpContext.Connection.RemoteIpAddress}",
                Action = $"mark {(active ? "" : "in")}active",
                DataType = AuditlogType.Judgehost,
                DataId = hostname,
                Time = DateTimeOffset.Now,
                UserName = userManager.GetUserName(User),
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
            bool full = false;

            using (await Lock.LockAsync())
            {
                var next = await (
                    from g in DbContext.Judgings
                    where g.Status == Verdict.Pending
                    join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                    join l in DbContext.Languages on s.Language equals l.Id
                    join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                    join c in DbContext.Contests on s.ContestId equals c.ContestId into cc
                    from c in cc.DefaultIfEmpty()
                    join cp in DbContext.ContestProblem on new { s.ContestId, s.ProblemId } equals new { cp.ContestId, cp.ProblemId } into cps
                    from cp in cps.DefaultIfEmpty()
                    where l.AllowJudge && p.AllowJudge && (cp == null || cp.AllowJudge)
                    select new { g, l, p, s.ContestId, c.StartTime, s.Author, s.RejudgeId }
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
                full = next.g.FullTest;
                judging.Status = Verdict.Running;
                judging.Server = host.ServerName;
                judging.StartTime = DateTimeOffset.Now;
                DbContext.Judgings.Update(judging);

                if (cccid != 0)
                {
                    var cts = new Data.Api.ContestJudgement(
                        judging, next.StartTime ?? DateTimeOffset.Now);
                    DbContext.Events.Add(cts.ToEvent("create", cccid));
                }

                await DbContext.SaveChangesAsync();
            }

            int memlimit = problem.MemoryLimit + (lang.Id == "java" ? 131072 : 0);
            int timelimit = (int)(problem.TimeLimit * lang.TimeFactor);

            var toFindMd5 = new[] { problem.RunScript, problem.CompareScript, lang.CompileScript };
            var md5s = await DbContext.Executable
                .Where(e => toFindMd5.Contains(e.ExecId))
                .Select(e => new { e.ExecId, e.Md5sum })
                .ToListAsync();

            var tcs = await DbContext.Testcases
                .Where(t => t.ProblemId == problem.ProblemId)
                .OrderBy(t => t.Rank)
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
                langid = lang.Id,
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
                full_judge = full && (cccid == 0 || judging.RejudgeId != null),
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
        [RequestFormLimits2(1 << 30)]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> UpdateJudging(
            [FromRoute] string hostname,
            [FromRoute] int judgingId,
            [FromForm] UpdateJudgingModel model)
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

                var cts = await (
                    from j in DbContext.Judgings
                    where j.JudgingId == jid
                    join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                    join c in DbContext.Contests on s.ContestId equals c.ContestId into cc
                    from c in cc.DefaultIfEmpty()
                    select new { c, s.ProblemId, s.Time, s.Author }
                ).FirstAsync();

                await FinishJudging(judging, hostname, cts.c,
                    cts.ProblemId, cts.Author, cts.Time);
            }
            else
            {
                DbContext.Judgings.Update(judging);
                await DbContext.SaveChangesAsync();
            }

            return Ok();
        }


        /// <summary>
        /// Add an array of JudgingRuns. When relevant, finalize the judging.
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost that wants to add the judging run</param>
        /// <param name="judgingId">The ID of the judging to add a run to</param>
        /// <param name="batch">Model</param>
        /// <response code="200">When the judging run has been added</response>
        [HttpPost("[action]/{hostname}/{judgingId}")]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits2(1 << 30)]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> AddJudgingRun(
            [FromRoute] string hostname,
            [FromRoute] int judgingId,
            [FromForm, ModelBinder(typeof(JudgingRunBinder))] List<JudgingRunModel> batch)
        {
            int jid = judgingId;
            if (batch is null) return BadRequest();

            var host = await DbContext.JudgeHosts
                .Where(h => h.ServerName == hostname)
                .FirstOrDefaultAsync();

            // Unknown or inactive judgehost requested
            if (host is null) return JsonEmpty();
            host.PollTime = DateTimeOffset.Now;
            DbContext.JudgeHosts.Update(host);

            var runEntities = batch
                .Select(b => (b, DbContext.Details.Add(b.ParseInfo(jid, host.PollTime))))
                .ToList();

            await DbContext.SaveChangesAsync();

            var io = HttpContext.RequestServices.GetRequiredService<IRunFileRepository>();
            foreach (var (b, e) in runEntities)
            {
                if (b.output_error is null || b.output_run is null)
                    continue;
                var stderr = TryUnbase64(b.output_error);
                var stdout = TryUnbase64(b.output_run);

                try
                {
                    await io.WriteBinaryAsync($"j{jid}/r{e.Entity.TestId}.out", stdout);
                    await io.WriteBinaryAsync($"j{jid}/r{e.Entity.TestId}.err", stderr);
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
                join c in DbContext.Contests on s.ContestId equals c.ContestId into cc
                from c in cc.DefaultIfEmpty()
                select new { g, s.ProblemId, s.Author, c, s.Time}
            ).FirstOrDefaultAsync();

            if (judging is null)
            {
                Logger.LogError("Unknown judging result.");
                return BadRequest();
            }

            if (judging.c != null)
            {
                var stt = judging.c.StartTime ?? DateTimeOffset.Now;
                var tids = runEntities.Select(e => e.Item2.Entity.TestcaseId).ToArray();
                var tcs = await DbContext.Testcases
                    .Where(t => tids.Contains(t.TestcaseId))
                    .Select(t => new { t.TestcaseId, t.Rank })
                    .ToDictionaryAsync(a => a.TestcaseId, a => a.Rank);

                runEntities.ForEach(t =>
                {
                    var run = t.Item2.Entity;
                    var it = new Data.Api.ContestRun(
                        run.CompleteTime, run.CompleteTime - stt,
                        run.TestId, run.JudgingId, run.Status,
                        tcs[run.TestcaseId], run.ExecuteTime);
                    DbContext.Events.Add(it.ToEvent("create", judging.c.ContestId));
                });
            }

            await DbContext.SaveChangesAsync();

            // Check for the final status
            var countOfTestcase = await DbContext.Testcases
                .CountAsync(t => t.ProblemId == judging.ProblemId);
            var verdictQuery =
                from d in DbContext.Details
                where d.JudgingId == jid
                join t in DbContext.Testcases on d.TestcaseId equals t.TestcaseId
                group new { d.Status, d.ExecuteMemory, d.ExecuteTime, t.Point } by d.JudgingId into g
                select new
                {
                    Status = g.Min(a => a.Status),
                    Count = g.Count(), 
                    Memory = g.Max(a => a.ExecuteMemory),
                    Time = g.Max(a => a.ExecuteTime),
                    Score = g.Sum(a => a.Status == Verdict.Accepted ? a.Point : 0)
                };

            var verdictsOfThis = await verdictQuery.FirstOrDefaultAsync();

            bool anyRejected = !judging.g.FullTest && verdictsOfThis.Status != Verdict.Accepted;
            bool fullTested = verdictsOfThis.Count >= countOfTestcase && countOfTestcase > 0;

            if (anyRejected || fullTested)
            {
                judging.g.ExecuteMemory = verdictsOfThis.Memory;
                judging.g.ExecuteTime = verdictsOfThis.Time;
                judging.g.Status = verdictsOfThis.Status;
                judging.g.StopTime = host.PollTime;
                judging.g.TotalScore = verdictsOfThis.Score;

                await FinishJudging(
                    judging.g, host.ServerName, judging.c,
                    judging.ProblemId, judging.Author, judging.Time);
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
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult<int>> InternalError(
            [FromForm] InternalErrorModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var toDisable = model.disabled.AsJson<InternalErrorDisable>();
            var kind = toDisable.kind;

            if (kind == "language")
            {
                var langid = toDisable.langid;
                await DbContext.Languages
                    .Where(l => l.Id == langid)
                    .BatchUpdateAsync(l => new Language { AllowJudge = false });

                DbContext.Auditlogs.Add(new Auditlog
                {
                    Action = "set allow judge",
                    DataId = langid,
                    Time = DateTimeOffset.Now,
                    DataType = AuditlogType.Language,
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
                var hostname = toDisable.hostname;
                await DbContext.JudgeHosts
                    .Where(h => h.ServerName == hostname)
                    .BatchUpdateAsync(h => new JudgeHost { Active = false });

                DbContext.Auditlogs.Add(new Auditlog
                {
                    Action = "set inactive",
                    DataId = hostname,
                    Time = DateTimeOffset.Now,
                    DataType = AuditlogType.Judgehost,
                    UserName = "judgehost",
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: hostname,
                    data: model.description,
                    startTime: DateTimeOffset.Now,
                    duration: TimeSpan.Zero,
                    success: false);
            }
            else if (kind == "problem")
            {
                var probid = toDisable.probid.Value;
                await DbContext.Problems
                    .Where(p => p.ProblemId == probid)
                    .BatchUpdateAsync(p => new Problem { AllowJudge = false });

                DbContext.Auditlogs.Add(new Auditlog
                {
                    Action = "set allow judge",
                    DataId = $"{probid}",
                    Time = DateTimeOffset.Now,
                    DataType = AuditlogType.Problem,
                    UserName = "judgehost",
                });

                Telemetry.TrackDependency(
                    dependencyTypeName: "Problem",
                    dependencyName: $"p{probid}",
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
                await ReturnToQueue(model.judgingid.Value);

            DbContext.InternalErrors.Add(ie);
            await DbContext.SaveChangesAsync();

            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = "added",
                UserName = "judgehost",
                DataId = $"{ie.ErrorId}",
                DataType = AuditlogType.InternalError,
                ExtraInfo = $"for {kind}",
                Time = ie.Time,
            });

            await DbContext.SaveChangesAsync();
            return ie.ErrorId;
        }
    }
}
