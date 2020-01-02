using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Authorize]
    [Route("[area]/{cid}/[controller]")]
    public partial class TeamController : Controller3
    {
        private Team Team { get; set; }

        private SubmissionManager SubmissionManager { get; }

        public TeamController(SubmissionManager sm) => SubmissionManager = sm;

        public bool TooEarly => (Contest.StartTime ?? DateTimeOffset.MaxValue) > DateTimeOffset.Now;

        public override async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            bool isOk = ViewData.ContainsKey("HasTeam");

            if (isOk)
            {
                Team = ViewBag.Team;
                if (Team.Status != 1) isOk = false;
            }

            if (!isOk)
            {
                context.Result = IsWindowAjax
                     ? Message("401 Unauthorized", "This contest is not active for you (yet).", MessageType.Danger)
                     : View("AccessDenied");
            }

            await base.OnActionExecutingAsync(context);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            var board = await Service.FindScoreboardAsync(cid, true, false);

            if (clear == "clear")
            {
                affiliations = Array.Empty<int>();
                categories = Array.Empty<int>();
            }

            if (affiliations.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => affiliations.Contains(t.Team.AffiliationId));
                ViewData["Filter_affiliations"] = affiliations.ToHashSet();
            }

            if (categories.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => categories.Contains(t.Team.CategoryId));
                ViewData["Filter_categories"] = categories.ToHashSet();
            }

            return View(board);
        }


        [HttpGet]
        public async Task<IActionResult> Home(int cid)
        {
            int teamid = Team.TeamId;
            var board = await Service.FindScoreboardAsync(cid, teamid);
            var probs = board.Problems;
            var langs = await Service.GetLanguagesAsync(cid);

            var clars = await Service.Clarifications
                .Where(c => c.ContestId == cid)
                .Where(c => (c.Sender == null && c.Recipient == null)
                    || c.Recipient == teamid || c.Sender == teamid)
                .CachedToListAsync($"`c{cid}`t{teamid}`clar", TimeSpan.FromSeconds(15));
            
            ViewBag.Clarifications = clars;

            var subQuery = await SubmissionManager.Submissions
                .Where(s => s.ContestId == cid && s.Author == teamid)
                .Join(
                    inner: SubmissionManager.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s.SubmissionId, g.Status, s.Time, s.ProblemId, s.Language })
                .OrderBy(a => a.SubmissionId)
                .ToListAsync();

            ViewBag.Submissions = subQuery
                .Select(a => new SubmissionViewModel
                {
                    Grade = 0,
                    Language = langs[a.Language],
                    SubmissionId = a.SubmissionId,
                    Time = a.Time,
                    Verdict = a.Status,
                    Problem = probs.FirstOrDefault(cp => cp.ProblemId == a.ProblemId),
                });

            return View(board);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Problemset(int cid)
        {
            return View(await Service.GetProblemsAsync(cid));
        }


        [HttpGet("[action]/{sid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Submission(int cid, int sid)
        {
            int teamid = Team.TeamId;
            var langs = await Service.GetLanguagesAsync(cid);
            var probs = await Service.GetProblemsAsync(cid);

            var model = await SubmissionManager.Submissions
                .Where(s => s.SubmissionId == sid && s.ContestId == cid && s.Author == teamid)
                .Join(
                    inner: SubmissionManager.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s.SubmissionId, g.Status, s.Time, s.ProblemId, g.CompileError, s.Language })
                .SingleOrDefaultAsync();

            return Window(new SubmissionViewModel
            {
                SubmissionId = model.SubmissionId,
                Grade = 0,
                Language = langs[model.Language],
                Time = model.Time,
                Verdict = model.Status,
                Problem = probs.FirstOrDefault(cp => cp.ProblemId == model.ProblemId),
                CompilerOutput = model.CompileError,
            });
        }


        [HttpGet("problems/{prob}")]
        public async Task<IActionResult> Problemset(
            int cid, string prob, [FromServices] IFileRepository pe)
        {
            if (TooEarly) return NotFound();
            var probs = await Service.GetProblemsAsync(cid);
            var problem = probs.FirstOrDefault(a => a.ShortName == prob);
            if (problem == null) return NotFound();

            pe.SetContext("Problems");
            var view = await pe.ReadPartAsync($"p{problem.ProblemId}", $"view.html");
            if (string.IsNullOrEmpty(view)) return NotFound();

            ViewData["Content"] = view;
            return View("ProblemView");
        }


        [HttpGet("clarifications/add")]
        [ValidateInAjax]
        public async Task<IActionResult> AddClarification(int cid)
        {
            if (TooEarly) return Message("Clarification", "Contest has not started.");
            ViewBag.Problems = await Service.GetProblemsAsync(cid);
            return Window(new AddClarificationModel());
        }


        [HttpGet("clarifications/{clarid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Clarification(int cid, int clarid)
        {
            var clars = await Service.ClarFindByIdAsync(cid, Team.TeamId, clarid, true);
            if (!clars.Any()) return NotFound();
            ViewData["TeamName"] = Team.TeamName;
            return Window(clars);
        }


        [HttpPost("clarifications/{op}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clarification(string op, AddClarificationModel model)
        {
            var (cid, teamid) = (Contest.ContestId, Team.TeamId);
            var probs = await Service.GetProblemsAsync(cid);
            int repl = 0;
            if (op != "add" && !int.TryParse(op, out repl)) return BadRequest();

            if (repl != 0 && await Service.ClarFindByIdAsync(cid, repl) == null)
                ModelState.AddModelError("xys::replyto", "The clarification replied to is not found.");

            if (string.IsNullOrWhiteSpace(model.Body))
                ModelState.AddModelError("xys::empty", "No empty clarification");

            var avaliableCategories = probs
                .Select(cp => ($"prob-{cp.ShortName}", (ClarificationCategory.Problem), (int?)cp.ProblemId))
                .Prepend(("tech", ClarificationCategory.Technical, null))
                .Prepend(("general", ClarificationCategory.General, null));
            var usage = avaliableCategories.SingleOrDefault(cp => model.Type == cp.Item1);
            if (usage.Item1 == null)
                ModelState.AddModelError("xys::error_cate", "The category specified is wrong.");

            if (!ModelState.IsValid)
            {
                DisplayMessage = string.Join('\n', ModelState.Values
                    .SelectMany(m => m.Errors)
                    .Select(e => e.ErrorMessage));
            }
            else
            {
                await Service.SendClarificationAsync(new Clarification
                {
                    Body = model.Body,
                    SubmitTime = DateTimeOffset.Now,
                    ContestId = cid,
                    Sender = teamid,
                    ResponseToId = model.ReplyTo,
                    ProblemId = usage.Item3,
                    Category = usage.Item2,
                });

                DisplayMessage = "Clarification sent to the jury.";
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Submit(int cid)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury"))
                return Message("Submit", "Contest not started.", MessageType.Danger);
            ViewBag.Problems = await Service.GetProblemsAsync(cid);
            ViewBag.Languages = await Service.GetLanguagesAsync(cid);
            return Window(new TeamCodeSubmitModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int cid, TeamCodeSubmitModel model)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury"))
            {
                DisplayMessage = "Contest not started.";
                return RedirectToAction(nameof(Home));
            }

            var probs = await Service.GetProblemsAsync(cid);
            var prob = probs.FirstOrDefault(cp => cp.ShortName == model.Problem);
            if (prob is null || !prob.AllowSubmit)
            {
                DisplayMessage = "Error problem not found.";
                return RedirectToAction(nameof(Home));
            }

            var langs = await Service.GetLanguagesAsync(cid);
            if (!langs.ContainsKey(model.Language))
            {
                DisplayMessage = "Error language not found.";
                return RedirectToAction(nameof(Home));
            }

            var s = await SubmissionManager.CreateAsync(
                code: model.Code,
                langid: model.Language,
                probid: prob.ProblemId,
                cid: cid, uid: Team.TeamId,
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "team-page",
                username: UserManager.GetUserName(User));

            DisplayMessage = "Submission done! Watch for the verdict in the list below.";
            return RedirectToAction(nameof(Home));
        }
    }
}
