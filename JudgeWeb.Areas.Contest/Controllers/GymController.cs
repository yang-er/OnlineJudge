using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Authorize]
    [Route("[controller]/{cid}")]
    public class GymController : Controller3
    {
        public bool TooEarly => Contest.GetState() < ContestState.Started;

        private new IActionResult NotFound() => ExplicitNotFound();

        private IActionResult GoBackHome(string message)
        {
            StatusMessage = message;
            return RedirectToAction(nameof(Home));
        }

        public override Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            if (!Contest.Gym)
                context.Result = RedirectToAction("Info", "Public");
            return base.OnActionExecutingAsync(context);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            ViewBag.Members = await Facade.Teams.ListMembersAsync(cid);
            return await ScoreboardView(
                isPublic: Contest.GetState() < ContestState.Finalized,
                isJury: false, clear == "clear", affiliations, categories);
        }


        [HttpGet]
        public async Task<IActionResult> Home(int cid,
            [FromServices] IProblemFileRepository io,
            [FromServices] IClarificationStore clars)
        {
            var board = await Facade.Teams.LoadScoreboardAsync(cid);
            ViewBag.ScoreboardData = Team == null ? null : board.Data.GetValueOrDefault(Team.TeamId);
            ViewBag.Statistics = board.Statistics;

            int? teamid = Team?.TeamId;
            ViewBag.Clarifications =
                await clars.ListAsync(cid, c => c.Recipient == null);

            var readme = io.GetFileInfo($"c{cid}/readme.html");
            ViewBag.Markdown = await readme.ReadAsync();
            return View();
        }


        [HttpGet("[action]/{sid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Submission(int cid, int sid,
            [FromServices] ISubmissionStore submissions,
            [FromServices] IJudgingStore judgings)
        {
            if (Team == null) return Forbid();

            var models = await submissions.ListWithJudgingAsync(
                predicate: s => s.SubmissionId == sid && s.ContestId == cid,
                selector: (s, j) => new SubmissionViewModel
                {
                    SubmissionId = s.SubmissionId,
                    Verdict = j.Status,
                    Time = s.Time,
                    Problem = Problems.Find(s.ProblemId),
                    CompilerOutput = j.CompileError,
                    Language = Languages[s.Language],
                    SourceCode = s.SourceCode,
                    Grade = j.TotalScore ?? 0,
                    TeamId = s.Author,
                    JudgingId = j.JudgingId,
                    ExecuteMemory = j.ExecuteMemory,
                    ExecuteTime = j.ExecuteTime,
                });

            var model = models.SingleOrDefault();
            if (model == null) return NotFound();
            var board = await Facade.Teams.LoadScoreboardAsync(cid);
            var boardQuery = board.Data.GetValueOrDefault(Team.TeamId);
            model.TeamName = boardQuery.TeamName;

            if (model.TeamId != Team.TeamId)
            {
                if (Contest.StatusAvaliable == 2)
                {
                    if (!boardQuery.ScoreCache.Any(sc => sc.ProblemId == model.Problem.ProblemId && sc.IsCorrectRestricted))
                        return Forbid();
                }
                else if (Contest.StatusAvaliable == 0)
                {
                    return Forbid();
                }
            }

            var tcs = await judgings.GetDetailsAsync(model.Problem.ProblemId, model.JudgingId);
            if (!model.Problem.Shared)
                tcs = tcs.Where(t => !t.Item2.IsSecret);
            ViewBag.Runs = tcs;

            return Window(model);
        }


        [HttpGet("problem/{prob}")]
        public async Task<IActionResult> ProblemView(string prob,
            [FromServices] ISubmissionStore submissions,
            [FromServices] IProblemStore problems)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury")) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();
            ViewBag.CurrentProblem = problem;

            var fileInfo = problems.GetFile(problem, "view.html");
            var view = await fileInfo.ReadAsync();
            if (string.IsNullOrEmpty(view)) return NotFound();
            ViewData["Content"] = view;

            int pid = problem.ProblemId;
            int tid = Team?.TeamId ?? -1000;
            int cid = Contest.ContestId;

            var model = await submissions.ListWithJudgingAsync(
                s => s.ProblemId == pid && s.Author == tid && s.ContestId == cid);
            return View(model);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Submit(string prob)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury"))
                return Message("Submit", "Contest not started.", MessageType.Danger);
            return Window(new TeamCodeSubmitModel { Problem = prob });
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Register(
            [FromServices] ITrainingStore training)
        {
            if (Team != null) return NotFound();
            var items = await training.ListAsync(int.Parse(User.GetUserId()));
            ViewData["TeamsJson"] = items.Select(g => new
            {
                team = new { name = g.Key.TeamName, id = g.Key.TrainingTeamId },
                users = g.Select(v => new { name = v.UserName, id = v.UserId }).ToList()
            })
            .ToJson();

            return View(new GymRegisterModel { AsIndividual = true });
        }


        [HttpPost("[action]")]
        [AuditPoint(AuditlogType.Team)]
        public async Task<IActionResult> Register(
            int cid, GymRegisterModel model,
            [FromServices] ITrainingStore training,
            [FromServices] UserManager userManager)
        {
            if (ViewData.ContainsKey("HasTeam"))
                return GoBackHome("Already registered");
            if (Contest.RegisterDefaultCategory == 0 || User.IsInRole("Blocked"))
                return GoBackHome("Error registration closed.");

            string teamName;
            int[] uids;
            int affId;
            var uid = int.Parse(User.GetUserId());

            if (model.AsIndividual)
            {
                var affs = await Facade.Teams.ListAffiliationAsync(cid, false);
                string defaultAff = User.IsInRole("Student") ? "jlu" : "null";
                var aff = affs.FirstOrDefault(a => a.ExternalId == defaultAff);
                if (aff == null) return GoBackHome("No default affiliation.");
                affId = aff.AffiliationId;
                uids = new[] { uid };

                var user = await userManager.GetUserAsync(User);
                if (user.StudentId.HasValue && user.StudentVerified)
                    teamName = (await userManager.FindStudentAsync(user.StudentId.Value)).Name;
                else
                    teamName = user.NickName;
                teamName ??= user.UserName;
            }
            else
            {
                var team = await training.FindTeamByIdAsync(model.TeamId);
                if (team == null)
                    return GoBackHome("Error team or team member.");
                (teamName, affId) = (team.TeamName, team.AffiliationId);

                var users = await training.ListMembersAsync(team);
                uids = (model.UserIds ?? Enumerable.Empty<int>()).Append(uid).Distinct().ToArray();
                if (uids.Except(users.Select(g => g.UserId)).Any())
                    return GoBackHome("Error team or team member.");
            }

            int tid = await Facade.Teams.CreateAsync(
                uids: uids,
                team: new Team
                {
                    AffiliationId = affId,
                    ContestId = Contest.ContestId,
                    CategoryId = Contest.RegisterDefaultCategory,
                    RegisterTime = DateTimeOffset.Now,
                    Status = 1,
                    TeamName = teamName,
                });

            await HttpContext.AuditAsync("added", $"{tid}");
            StatusMessage = "Registration succeeded.";
            return RedirectToAction(nameof(Home));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int cid, TeamCodeSubmitModel model,
            [FromServices] ISubmissionStore submissions,
            [FromServices] IScoreboardService scoreboardService)
        {
            if (!ViewData.ContainsKey("HasTeam"))
            {
                StatusMessage = "You should register first.";
                return RedirectToAction(nameof(Register));
            }

            if (TooEarly && !ViewData.ContainsKey("IsJury"))
            {
                StatusMessage = "Contest not started.";
                return RedirectToAction(nameof(Home));
            }

            var prob = Problems.Find(model.Problem);
            if (prob is null || !prob.AllowSubmit)
            {
                StatusMessage = "Error problem not found.";
                return RedirectToAction(nameof(Home));
            }

            var lang = Languages.GetValueOrDefault(model.Language);
            if (lang == null)
            {
                StatusMessage = "Error language not found.";
                return RedirectToAction(nameof(Home));
            }

            var s = await submissions.CreateAsync(
                code: model.Code,
                language: lang.Id,
                problemId: prob.ProblemId,
                contestId: cid,
                userId: Team.TeamId,
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "gym-page",
                username: User.GetUserName());

            scoreboardService.SubmissionCreated(Contest, s);
            StatusMessage = "Submission done!";
            return RedirectToAction(nameof(ProblemView), new { prob = prob.ShortName });
        }


        [HttpGet("problem/{prob}/testcase/{tcid}/fetch/{filetype}")]
        public async Task<IActionResult> FetchTestcase(
            string prob, int tcid, string filetype,
            [FromServices] ITestcaseStore testcases)
        {
            if (filetype == "input") filetype = "in";
            else if (filetype == "output") filetype = "out";
            else return NotFound();

            if (TooEarly && !ViewData.ContainsKey("IsJury")) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();

            var tc = await testcases.FindAsync(problem.ProblemId, tcid);
            if (tc == null) return NotFound();
            if (tc.IsSecret && !problem.Shared) return NotFound();

            var file = testcases.GetFile(tc, filetype);
            if (!file.Exists) return NotFound();

            return File(
                fileStream: file.CreateReadStream(),
                contentType: "application/octet-stream",
                fileDownloadName: $"{problem.ShortName}.t{tcid}.{filetype}");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Submissions(
            [FromServices] ISubmissionStore submissions,
            int cid, int page = 1)
        {
            if (page <= 0) return BadRequest();

            var (model, count) = await submissions.ListWithJudgingAsync(
                pagination: (page, 50),
                predicate: s => s.ContestId == cid);

            ViewBag.Page = page;
            ViewBag.TeamsName = await Facade.Teams.ListNamesAsync(cid);

            var board = await Facade.Teams.LoadScoreboardAsync(cid);
            ViewBag.ScoreboardData = Team == null ? null : board.Data.GetValueOrDefault(Team.TeamId);
            return View(model);
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
