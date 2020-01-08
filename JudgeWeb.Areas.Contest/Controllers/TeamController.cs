using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Authorize]
    [Route("[area]/{cid}/[controller]")]
    public partial class TeamController : Controller3
    {
        public bool TooEarly => Contest.GetState() < ContestState.Started;

        public override async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            if (Team == null || Team.Status == 1)
            {
                context.Result = IsWindowAjax
                    ? Message("401 Unauthorized",
                        "This contest is not active for you (yet).",
                        MessageType.Danger)
                    : View("AccessDenied");
            }

            await base.OnActionExecutingAsync(context);
        }


        [HttpGet("[action]")]
        public Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "") =>
            ScoreboardView(
                isPublic: Contest.GetState() < ContestState.Finalized,
                isJury: false, clear == "clear", affiliations, categories);


        [HttpGet]
        public async Task<IActionResult> Home(int cid)
        {
            int teamid = Team.TeamId;
            var board = await FindScoreboardAsync(teamid);
            var probs = board.Problems;

            var clars = await DbContext.Clarifications
                .Where(c => c.ContestId == cid)
                .Where(c => (c.Sender == null && c.Recipient == null)
                    || c.Recipient == teamid || c.Sender == teamid)
                .CachedToListAsync($"`c{cid}`t{teamid}`clar", TimeSpan.FromSeconds(15));
            
            ViewBag.Clarifications = clars;

            var subQuery = await DbContext.Submissions
                .Where(s => s.ContestId == cid && s.Author == teamid)
                .Join(
                    inner: DbContext.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s.SubmissionId, g.Status, s.Time, s.ProblemId, s.Language })
                .OrderByDescending(a => a.SubmissionId)
                .ToListAsync();

            ViewBag.Submissions = subQuery
                .Select(a => new SubmissionViewModel
                {
                    Grade = 0,
                    Language = Languages[a.Language],
                    SubmissionId = a.SubmissionId,
                    Time = a.Time,
                    Verdict = a.Status,
                    Problem = probs.FirstOrDefault(cp => cp.ProblemId == a.ProblemId),
                });

            return View(board);
        }


        [HttpGet("[action]")]
        public IActionResult Problemset()
        {
            return View(Problems);
        }


        [HttpGet("[action]/{sid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Submission(int cid, int sid)
        {
            int teamid = Team.TeamId;

            var model = await DbContext.Submissions
                .Where(s => s.SubmissionId == sid && s.ContestId == cid && s.Author == teamid)
                .Join(
                    inner: DbContext.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s.SubmissionId, g.Status, s.Time, s.ProblemId, g.CompileError, s.Language, s.SourceCode })
                .SingleOrDefaultAsync();

            return Window(new SubmissionViewModel
            {
                SubmissionId = model.SubmissionId,
                Grade = 0,
                Language = Languages[model.Language],
                Time = model.Time,
                Verdict = model.Status,
                Problem = Problems.Find(model.ProblemId),
                CompilerOutput = model.CompileError,
                SourceCode = model.SourceCode,
            });
        }


        [HttpGet("problems/{prob}")]
        public async Task<IActionResult> Problemset(string prob, [FromServices] IFileRepository pe)
        {
            if (TooEarly) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();

            pe.SetContext("Problems");
            var view = await pe.ReadPartAsync($"p{problem.ProblemId}", $"view.html");
            if (string.IsNullOrEmpty(view)) return NotFound();

            ViewData["Content"] = view;
            return View("ProblemView");
        }


        [HttpGet("clarifications/add")]
        [ValidateInAjax]
        public IActionResult AddClarification()
        {
            if (TooEarly) return Message("Clarification", "Contest has not started.");
            return Window(new AddClarificationModel());
        }


        [HttpGet("clarifications/{clarid}")]
        [ValidateInAjax]
        public async Task<IActionResult> Clarification(int cid, int clarid, bool needMore = true)
        {
            var toSee = await DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarid)
                .SingleOrDefaultAsync();
            var clars = Enumerable.Empty<Clarification>();

            if (toSee?.CheckPermission(Team.TeamId) ?? true)
            {
                clars = clars.Append(toSee);

                if (needMore && toSee.ResponseToId.HasValue)
                {
                    int respid = toSee.ResponseToId.Value;
                    var toSee2 = await DbContext.Clarifications
                        .Where(c => c.ContestId == cid && c.ClarificationId == respid)
                        .SingleOrDefaultAsync();
                    if (toSee2 != null) clars = clars.Prepend(toSee2);
                }
            }

            if (!clars.Any()) return NotFound();
            ViewData["TeamName"] = Team.TeamName;
            return Window(clars);
        }


        [HttpPost("clarifications/{op}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clarification(string op, AddClarificationModel model)
        {
            var (cid, teamid) = (Contest.ContestId, Team.TeamId);
            int repl = 0;
            if (op != "add" && !int.TryParse(op, out repl)) return BadRequest();

            var replit = await DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == repl)
                .SingleOrDefaultAsync();
            if (repl != 0 && replit == null)
                ModelState.AddModelError("xys::replyto", "The clarification replied to is not found.");

            if (string.IsNullOrWhiteSpace(model.Body))
                ModelState.AddModelError("xys::empty", "No empty clarification");

            var usage = ClarCategories.SingleOrDefault(cp => model.Type == cp.Item1);
            if (usage.Item1 == null)
                ModelState.AddModelError("xys::error_cate", "The category specified is wrong.");

            if (!ModelState.IsValid)
            {
                StatusMessage = string.Join('\n', ModelState.Values
                    .SelectMany(m => m.Errors)
                    .Select(e => e.ErrorMessage));
            }
            else
            {
                await SendClarificationAsync(new Clarification
                {
                    Body = model.Body,
                    SubmitTime = DateTimeOffset.Now,
                    ContestId = cid,
                    Sender = teamid,
                    ResponseToId = model.ReplyTo,
                    ProblemId = usage.Item3,
                    Category = usage.Item2,
                });

                StatusMessage = "Clarification sent to the jury.";
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Submit()
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury"))
                return Message("Submit", "Contest not started.", MessageType.Danger);
            return Window(new TeamCodeSubmitModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int cid, TeamCodeSubmitModel model,
            [FromServices] SubmissionManager submgr)
        {
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
                via: "team-page",
                username: UserManager.GetUserName(User));

            DbContext.UpdateScoreboard(new ScoreboardState
            {
                ContestId = Contest.ContestId,
                EndTime = Contest.EndTime,
                FreezeTime = Contest.FreezeTime,
                ProblemId = prob.ProblemId,
                RankStrategy = Contest.RankingStrategy,
                StartTime = Contest.StartTime,
                SubmissionId = s.SubmissionId,
                TeamId = s.Author,
                Time = s.Time,
                UnfreezeTime = Contest.UnfreezeTime,
            });

            StatusMessage = "Submission done! Watch for the verdict in the list below.";
            return RedirectToAction(nameof(Home));
        }
    }
}
