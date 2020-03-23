using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        ILogger<JudgehostsController> Logger { get; }
        IJudgementFacade Facade { get; }
        IProblemFacade ProblemFacade { get; }
        IJudgehostStore Judgehosts => Facade.Judgehosts;
        IInternalErrorStore InternalErrors => Facade.InternalErrors;
        ILanguageStore Languages => ProblemFacade.Languages;
        IScoreboardService Scoreboard { get; }
        IProblemStore Problems => ProblemFacade.Problems;
        ITestcaseStore Testcases => ProblemFacade.Testcases;
        IExecutableStore Executables => ProblemFacade.Executables;
        IJudgingStore Judgings => Facade.Judgings;
        IContestEventNotifier Events { get; }
        TelemetryClient Telemetry { get; }

        static AsyncLock Lock { get; } = new AsyncLock();

        public JudgehostsController(
            ILogger<JudgehostsController> logger,
            IJudgementFacade facade2,
            IProblemFacade facade,
            IScoreboardService scb,
            IContestEventNotifier cen,
            TelemetryClient telemetry)
        {
            Logger = logger;
            Facade = facade2;
            Events = cen;
            ProblemFacade = facade;
            Scoreboard = scb;
            Telemetry = telemetry;
        }


        private JsonResult Empty() => new JsonResult("");


        /// <summary>
        /// Finish the judging pipe, persist into database,
        /// create the events and notify scoreboard.
        /// </summary>
        private async Task FinishJudging(Judging j, int? cid, int pid, int uid, DateTimeOffset subtime)
        {
            if (cid == 0) cid = null;
            await Judgings.UpdateAsync(j);

            await HttpContext.AuditAsync(
                AuditlogType.Judging, cid, j.Server,
                "judged", $"{j.JudgingId}", $"{j.Status}");

            if (cid.HasValue)
                await Events.Update(cid.Value, j);
            if (cid.HasValue && j.Active)
                Scoreboard.JudgingFinished(cid.Value, subtime, pid, uid, j);

            Telemetry.TrackDependency(
                dependencyTypeName: "JudgeHost",
                dependencyName: j.Server,
                data: $"j{j.JudgingId} judged " + j.Status,
                startTime: j.StartTime ?? DateTimeOffset.Now,
                duration: (j.StopTime - j.StartTime) ?? TimeSpan.Zero,
                success: j.Status != Verdict.UndefinedError);
        }


        /// <summary>
        /// The judging meet an internal error, require a rejudging.
        /// </summary>
        private async Task ReturnToQueue(Judging j, int? cid, int pid, int uid, DateTimeOffset subtime)
        {
            await Judgings.CreateAsync(new Judging
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

            if (cid == 0) cid = null;
            await FinishJudging(j, cid, pid, uid, subtime);
        }


        /// <summary>
        /// Get judgehosts
        /// </summary>
        /// <param name="hostname">Only show the judgehost with the given hostname</param>
        /// <response code="200">The judgehosts</response>
        [HttpGet]
        public async Task<ActionResult<List<Judgehost>>> OnGet(string hostname)
        {
            List<JudgeHost> hosts;

            if (hostname != null)
            {
                hosts = new List<JudgeHost>();
                var host = await Judgehosts.FindAsync(hostname);
                if (host != null) hosts.Add(host);
            }
            else
            {
                hosts = await Judgehosts.ListAsync();
            }

            return hosts.Select(a => new Judgehost(a)).ToList();
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
        [AuditPoint(AuditlogType.Judgehost)]
        public async Task<ActionResult<List<UnfinishedJudging>>> OnPost(
            [FromForm, Required] string hostname)
        {
            var item = await Judgehosts.FindAsync(hostname);

            if (item is null)
            {
                item = await Judgehosts.CreateAsync(new JudgeHost
                {
                    ServerName = hostname,
                    PollTime = DateTimeOffset.Now,
                    Active = true
                });

                await HttpContext.AuditAsync(
                    "registered", hostname,
                    $"on {HttpContext.Connection.RemoteIpAddress}");

                Telemetry.TrackDependency(
                    dependencyTypeName: "JudgeHost",
                    dependencyName: item.ServerName,
                    data: "registed",
                    startTime: item.PollTime,
                    duration: TimeSpan.Zero,
                    success: true);

                return new List<UnfinishedJudging>();
            }
            else
            {
                await Judgehosts.NotifyPollAsync(item);

                var oldJudgings = await Judgings.ListAsync(
                    predicate: j => j.Server == item.ServerName && j.Status == Verdict.Running,
                    selector: j => new { j, j.s.ContestId, j.s.Author, j.s.ProblemId, j.s.Time },
                    topCount: 10000);

                foreach (var sg in oldJudgings)
                    await ReturnToQueue(sg.j, sg.ContestId, sg.ProblemId, sg.Author, sg.Time );

                return oldJudgings
                    .Select(s => new UnfinishedJudging
                    {
                        judgingid = s.j.JudgingId,
                        submitid = s.j.SubmissionId,
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
        [AuditPoint(AuditlogType.Judgehost)]
        public async Task<ActionResult<IEnumerable<Judgehost>>> OnPut(
            [FromRoute] string hostname, bool active)
        {
            var item = await Judgehosts.FindAsync(hostname);
            if (item == null) return NotFound();

            item.Active = active;
            await Judgehosts.ToggleAsync(hostname, active);

            await HttpContext.AuditAsync(
                $"mark {(active ? "" : "in")}active", hostname,
                $"by api {HttpContext.Connection.RemoteIpAddress}");

            return new[] { new Judgehost(item) };
        }


        /// <summary>
        /// Get the next judging for the given judgehost
        /// </summary>
        /// <param name="hostname">The hostname of the judgehost to get the next judging for</param>
        /// <response code="200">The next judging to judge</response>
        [HttpPost("[action]/{hostname}")]
        public async Task<ActionResult<NextJudging>> NextJudging([FromRoute] string hostname)
        {
            var host = await Judgehosts.FindAsync(hostname);
            if (host is null) return Empty(); // Unknown or inactive judgehost requested
            await Judgehosts.NotifyPollAsync(host);
            if (!host.Active) return Empty();

            var r = new
            {
                judging = (Judging)null,
                lang = (Language)null,
                prob = (Problem)null,
                cid = 0,
                teamid = 0,
                rjid = new int?(),
            };

            using (await Lock.LockAsync())
            {
                var nextLst = await Judgings.ListAsync(
                    predicate: j => j.Status == Verdict.Pending
                            && j.s.l.AllowJudge
                            && j.s.p.AllowJudge,
                    topCount: 1,
                    selector: j => new
                    {
                        judging = j,
                        lang = j.s.l,
                        prob = j.s.p,
                        cid = j.s.ContestId,
                        teamid = j.s.Author,
                        rjid = j.s.RejudgeId
                    });

                r = nextLst.SingleOrDefault();
                if (r is null) return Empty();

                var judging = r.judging;
                judging.Status = Verdict.Running;
                judging.Server = host.ServerName;
                judging.StartTime = DateTimeOffset.Now;
                await Judgings.UpdateAsync(judging);

                //if (cid != 0)
                //{
                //    var cts = new Data.Api.ContestJudgement(
                //        judging, next.StartTime ?? DateTimeOffset.Now);
                //    DbContext.Events.Add(cts.ToEvent("create", cid));
                //}
            }

            var toFindMd5 = new[] { r.prob.RunScript, r.prob.CompareScript, r.lang.CompileScript };
            var md5s = await Executables.ListMd5Async(toFindMd5);
            var tcss = await Testcases.ListAsync(r.prob.ProblemId);

            return new NextJudging
            {
                submitid = r.judging.SubmissionId,
                cid = r.cid,
                teamid = r.teamid,
                probid = r.prob.ProblemId,
                langid = r.lang.Id,
                rejudgingid = r.rjid,
                entry_point = null,
                origsubmitid = null,
                maxruntime = r.prob.TimeLimit * r.lang.TimeFactor / 1000.0, // as seconds
                memlimit = r.prob.MemoryLimit + (r.lang.Id == "java" ? 131072 : 0), // as kb, java more 128M
                outputlimit = r.prob.OutputLimit, // KB
                run = r.prob.RunScript,
                compare = r.prob.CompareScript,
                compare_args = r.prob.ComapreArguments,
                compile_script = r.lang.CompileScript,
                combined_run_compare = r.prob.CombinedRunCompare,
                compare_md5sum = md5s[r.prob.CompareScript],
                run_md5sum = md5s[r.prob.RunScript],
                compile_script_md5sum = md5s[r.lang.CompileScript],
                judgingid = r.judging.JudgingId,
                full_judge = r.judging.FullTest && (r.cid == 0 || r.judging.RejudgeId != null),
                testcases = tcss.ToDictionary(t => $"{t.Rank}", t => new TestcaseToJudge(t))
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
            var host = await Judgehosts.FindAsync(hostname);

            // Unknown or inactive judgehost requested
            if (host is null) return Empty();
            await Judgehosts.NotifyPollAsync(host);

            var (judging, pid, cid, uid, time) = await Judgings.FindAsync(judgingId);
            if (judging is null) return BadRequest();
            judging.CompileError = model.output_compile ?? "";

            if (model.compile_success != 1)
            {
                judging.Status = Verdict.CompileError;
                judging.StopTime = DateTimeOffset.Now;
                await FinishJudging(judging, cid, pid, uid, time);
            }
            else
            {
                await Judgings.UpdateAsync(judging);
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
            if (batch is null) return BadRequest();

            var host = await Judgehosts.FindAsync(hostname);
            if (host is null) return BadRequest(); // Unknown or inactive judgehost requested
            await Judgehosts.NotifyPollAsync(host);
            var (judging, pid, cid, uid, time) = await Judgings.FindAsync(judgingId);

            if (judging is null)
            {
                Logger.LogError("Unknown judging result.");
                return BadRequest();
            }

            foreach (var run in batch)
            {
                var detail = await Judgings.CreateAsync(run.ParseInfo(judgingId, host.PollTime));
                if (cid != 0) await Events.Create(cid, detail);

                if (run.output_error is null || run.output_run is null)
                    continue;
                try
                {
                    var stderr = Convert.FromBase64String(run.output_error);
                    var stdout = Convert.FromBase64String(run.output_run);
                    await Judgings.SetRunFileAsync(judgingId, detail.TestId, "out", stdout);
                    await Judgings.SetRunFileAsync(judgingId, detail.TestId, "err", stderr);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred when saving OutputError and OutputRun for j{judgingId}", judgingId);
                }
            }

            // Check for the final status
            var countTc = await Testcases.CountAsync(pid);
            var verdict = await Judgings.SummarizeAsync(judging);
            // testId for score, testcaseId for count of tested cases

            bool anyRejected = !judging.FullTest && verdict.Status != Verdict.Accepted;
            bool fullTested = verdict.TestcaseId >= countTc && countTc > 0;

            if (anyRejected || fullTested)
            {
                judging.ExecuteMemory = verdict.ExecuteMemory;
                judging.ExecuteTime = verdict.ExecuteTime;
                judging.Status = verdict.Status;
                judging.StopTime = host.PollTime;
                judging.TotalScore = verdict.TestId;
                await FinishJudging(judging, cid, pid, uid, time);
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
        [AuditPoint(AuditlogType.InternalError)]
        public async Task<ActionResult<int>> InternalError(
            [FromForm] InternalErrorModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var toDisable = model.disabled.AsJson<InternalErrorDisable>();
            var kind = toDisable.kind;

            var ie = await InternalErrors.CreateAsync(
                new InternalError
                {
                    JudgehostLog = model.judgehostlog,
                    JudgingId = model.judgingid,
                    ContestId = model.cid,
                    Description = model.description,
                    Disabled = model.disabled,
                    Status = InternalErrorStatus.Open,
                    Time = DateTimeOffset.Now,
                });

            if (kind == "language")
            {
                var langid = toDisable.langid;
                await Languages.UpdateAsync(
                    predicate: l => l.Id == langid,
                    update: l => new Language { AllowJudge = false });

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
                await Judgehosts.ToggleAsync(hostname, false);

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
                await Problems.ToggleAsync(probid, p => p.AllowJudge, false);

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

            if (model.judgingid.HasValue)
            {
                var (j, pid, cid, uid, time) = await Judgings.FindAsync(model.judgingid.Value);
                await ReturnToQueue(j, cid, pid, uid, time);
            }

            await HttpContext.AuditAsync("added", $"{ie.ErrorId}", $"for {kind}");
            return ie.ErrorId;
        }
    }
}
