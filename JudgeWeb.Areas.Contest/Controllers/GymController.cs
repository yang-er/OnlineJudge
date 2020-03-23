using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
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
            ViewBag.Members = await DbContext.ListTeamMembersAsync(cid);
            return await ScoreboardView(
                isPublic: Contest.GetState() < ContestState.Finalized,
                isJury: false, clear == "clear", affiliations, categories);
        }


        [HttpGet]
        public async Task<IActionResult> Home(int cid,
            [FromServices] IProblemFileRepository io)
        {
            var board = await DbContext.LoadScoreboardAsync(cid);
            ViewBag.ScoreboardData = Team == null ? null : board.Data.GetValueOrDefault(Team.TeamId);
            ViewBag.Statistics = board.Statistics;

            int? teamid = Team?.TeamId;
            ViewBag.Clarifications = await DbContext.Clarifications
                .Where(c => c.ContestId == cid)
                .Where(c => c.Sender == null && (c.Recipient == null || c.Recipient == teamid))
                .ToListAsync();

            var readme = io.GetFileInfo($"c{cid}/readme.html");
            ViewBag.Markdown = await readme.ReadAsync();
            return View();
        }


        [HttpGet("[action]/{sid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Submission(int cid, int sid)
        {
            if (Team == null) return Forbid();

            var modelQuery =
                from s in DbContext.Submissions
                where s.SubmissionId == sid && s.ContestId == cid
                join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                join t in DbContext.Teams on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                select new SubmissionViewModel
                {
                    SubmissionId = s.SubmissionId,
                    Verdict = j.Status,
                    Time = s.Time,
                    Problem = Problems.Find(s.ProblemId),
                    CompilerOutput = j.CompileError,
                    Language = Languages[s.Language],
                    SourceCode = s.SourceCode,
                    Grade = j.TotalScore ?? 0,
                    TeamName = t.TeamName,
                    TeamId = t.TeamId,
                    JudgingId = j.JudgingId,
                    ExecuteMemory = j.ExecuteMemory,
                    ExecuteTime = j.ExecuteTime
                };

            var model = await modelQuery.SingleOrDefaultAsync();
            if (model == null) return NotFound();

            if (model.TeamId != Team.TeamId)
            {
                if (Contest.StatusAvaliable == 2)
                {
                    var board = await DbContext.LoadScoreboardAsync(cid);
                    var boardQuery = board.Data.GetValueOrDefault(Team.TeamId);
                    if (!boardQuery.ScoreCache.Any(sc => sc.ProblemId == model.Problem.ProblemId && sc.IsCorrectRestricted))
                        return Forbid();
                }
                else if (Contest.StatusAvaliable == 0)
                {
                    return Forbid();
                }
            }

            var testcaseQuery = DbContext.Testcases
                .Where(t => t.ProblemId == model.Problem.ProblemId);
            if (!model.Problem.Shared)
                testcaseQuery = testcaseQuery.Where(t => !t.IsSecret);
            var runQuery =
                from t in testcaseQuery
                orderby t.Rank ascending
                join r in DbContext.Details on new { model.JudgingId, t.TestcaseId } equals new { r.JudgingId, r.TestcaseId }
                into rr
                from r in rr.DefaultIfEmpty()
                select new { t, r };
            var runs = await runQuery.ToListAsync();
            ViewBag.Runs = runs.Select(a => (a.t, a.r));

            return Window(model);
        }


        [HttpGet("problem/{prob}")]
        public async Task<IActionResult> ProblemView(string prob,
            [FromServices] IProblemFileRepository io)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury")) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();
            ViewBag.CurrentProblem = problem;

            var fileInfo = io.GetFileInfo($"p{problem.ProblemId}/view.html");
            var view = await fileInfo.ReadAsync();
            if (string.IsNullOrEmpty(view)) return NotFound();
            ViewData["Content"] = view;

            int pid = problem.ProblemId, tid = Team?.TeamId ?? -1000, cid = Contest.ContestId;
            var query =
                from s in DbContext.Submissions
                where s.ProblemId == pid && s.Author == tid && s.ContestId == cid
                join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                select new { s, j };
            return View((await query.ToListAsync()).Select(a => (a.s, a.j)));
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
        public async Task<IActionResult> Register()
        {
            if (Team != null) return NotFound();
            int uid = int.Parse(UserManager.GetUserId(User));
            var teamQuery =
                from ttu in DbContext.TrainingTeamUsers
                where ttu.UserId == uid && ttu.Accepted == true
                join t in DbContext.TrainingTeams on ttu.TrainingTeamId equals t.TrainingTeamId
                join tu in DbContext.TrainingTeamUsers on t.TrainingTeamId equals tu.TrainingTeamId
                where tu.Accepted == true
                join u in UserManager.Users on tu.UserId equals u.Id
                select new { t.TeamName, t.TrainingTeamId, tu.UserId, u.UserName };
            var items = await teamQuery.ToListAsync();
            var result = items
                .GroupBy(
                    keySelector: k => new { name = k.TeamName, id = k.TrainingTeamId },
                    elementSelector: v => new { name = v.UserName, id = v.UserId })
                .Select(g => new { team = g.Key, users = g.ToList() })
                .ToJson();
            ViewData["TeamsJson"] = result;
            return View(new GymRegisterModel { AsIndividual = true });
        }


        [HttpPost("[action]")]
        public async Task<IActionResult> Register(int cid, GymRegisterModel model)
        {
            try
            {
                if (ViewData.ContainsKey("HasTeam"))
                    throw new ApplicationException("Already registered");
                if (Contest.RegisterDefaultCategory == 0 || User.IsInRole("Blocked"))
                    throw new ApplicationException("Error registration closed.");

                string teamName;
                int[] uids;
                TeamAffiliation aff;
                var uid = int.Parse(UserManager.GetUserId(User));
                var affs = await DbContext.ListTeamAffiliationAsync(cid, false);

                if (model.AsIndividual)
                {
                    string defaultAff = User.IsInRole("Student") ? "jlu" : "null";
                    aff = affs.FirstOrDefault(a => a.ExternalId == defaultAff);
                    if (aff == null) throw new ApplicationException("No default affiliation.");
                    teamName = (User.IsInRole("Student")
                        ? await (from u in DbContext.Users
                                 where u.Id == uid
                                 join s in DbContext.Students on u.StudentId equals s.Id
                                 select s.Name).FirstOrDefaultAsync()
                        : UserManager.GetNickName(User)) ?? UserManager.GetUserName(User);
                    uids = new[] { uid };
                }
                else
                {
                    var teamQuery =
                        from t in DbContext.TrainingTeams
                        where t.TrainingTeamId == model.TeamId
                        join tu in DbContext.TrainingTeamUsers on t.TrainingTeamId equals tu.TrainingTeamId
                        where tu.Accepted == true
                        select new { t, tu };
                    var users = await teamQuery.ToListAsync();

                    if (users.Count == 0)
                        throw new ApplicationException("Error team or team member.");
                    teamName = users[0].t.TeamName;
                    aff = affs.FirstOrDefault(a => a.AffiliationId == users[0].t.AffiliationId);
                    uids = (model.UserIds ?? Enumerable.Empty<int>()).Append(uid).Distinct().ToArray();
                    if (uids.Except(users.Select(g => g.tu.UserId)).Any())
                        throw new ApplicationException("Error team or team member.");
                }

                await CreateTeamAsync(
                    aff: aff,
                    uids: uids,
                    team: new Team
                    {
                        AffiliationId = aff.AffiliationId,
                        ContestId = Contest.ContestId,
                        CategoryId = Contest.RegisterDefaultCategory,
                        RegisterTime = DateTimeOffset.Now,
                        Status = 1,
                        TeamName = teamName,
                    });

                StatusMessage = "Registration succeeded.";
                return RedirectToAction(nameof(Home));
            }
            catch (ApplicationException ex)
            {
                StatusMessage = ex.Message;
                return RedirectToAction(nameof(Home));
            }
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int cid, TeamCodeSubmitModel model,
            [FromServices] SubmissionManager submgr,
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

            var s = await submgr.CreateAsync(
                code: model.Code,
                langid: lang,
                probid: prob.ProblemId,
                cid: Contest, uid: Team.TeamId,
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "gym-page",
                username: UserManager.GetUserName(User));

            scoreboardService.SubmissionCreated(Contest, s);
            StatusMessage = "Submission done!";
            return RedirectToAction(nameof(ProblemView), new { prob = prob.ShortName });
        }


        [HttpGet("problem/{prob}/testcase/{tcid}/fetch/{filetype}")]
        public async Task<IActionResult> FetchTestcase(
            string prob, int tcid, string filetype,
            [FromServices] IProblemFileRepository io)
        {
            if (filetype == "input") filetype = "in";
            else if (filetype == "output") filetype = "out";
            else return NotFound();

            if (TooEarly && !ViewData.ContainsKey("IsJury")) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();

            var tc = await DbContext.Testcases
                .Where(t => t.ProblemId == problem.ProblemId && t.TestcaseId == tcid)
                .FirstOrDefaultAsync();
            if (tc == null) return NotFound();
            if (tc.IsSecret && !problem.Shared) return NotFound();

            var file = io.GetFileInfo($"p{problem.ProblemId}/t{tcid}.{filetype}");
            if (!file.Exists) return NotFound();

            return File(
                fileStream: file.CreateReadStream(),
                contentType: "application/octet-stream",
                fileDownloadName: $"{problem.ShortName}.t{tcid}.{filetype}");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Submissions(int cid, int page = 1)
        {
            if (page <= 0) return BadRequest();

            var statusQuery =
                from s in DbContext.Submissions
                where s.ContestId == cid
                join j in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                orderby s.SubmissionId descending
                select new StatusListModel
                {
                    SubmissionId = s.SubmissionId,
                    Verdict = j.Status,
                    CodeLength = s.CodeLength,
                    ExecutionMemory = j.ExecuteMemory,
                    ExecutionTime = j.ExecuteTime,
                    Language = s.Language,
                    ProblemId = s.ProblemId,
                    Time = s.Time,
                    Author = s.Author,
                    ContestId = s.ContestId,
                    JudgingId = j.JudgingId,
                };

            var model = await statusQuery
                .Skip((page - 1) * 50)
                .Take(50)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TeamsName = await DbContext.GetTeamNameAsync(cid);

            var board = await DbContext.LoadScoreboardAsync(cid);
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
