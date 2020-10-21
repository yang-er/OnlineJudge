using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.FileProviders;
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

        private new IActionResult NotFound() => StatusCodePage(404);

        public override async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            if (Contest.Gym)
                context.Result = RedirectToAction("Home", "Gym");

            else if (Team == null || Team.Status != 1)
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
        public IActionResult Print() => PrintView();


        [HttpPost("[action]")]
        [AuditPoint(AuditlogType.Printing)]
        public Task<IActionResult> Print(int cid, AddPrintModel model) => PrintDo(cid, model);


        [HttpGet("[action]")]
        public Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "") =>
            ScoreboardView(
                isPublic: Contest.GetState() < ContestState.Finalized,
                isJury: false, clear == "clear", affiliations, categories);


        [HttpGet]
        public async Task<IActionResult> Home(int cid,
            [FromServices] ISubmissionStore submits,
            [FromServices] IClarificationStore clars)
        {
            int teamid = Team.TeamId;
            var board = await FindScoreboardAsync(teamid);
            
            ViewBag.Clarifications = await clars.ListAsync(cid,
                c => (c.Sender == null && c.Recipient == null)
                || c.Recipient == teamid || c.Sender == teamid);

            ViewBag.Submissions = 
                await submits.ListWithJudgingAsync(
                predicate: s => s.ContestId == cid && s.Author == teamid,
                selector: (s, j) => new SubmissionViewModel
                {
                    Grade = j.TotalScore ?? 0,
                    Language = Languages[s.Language],
                    SubmissionId = s.SubmissionId,
                    Time = s.Time,
                    Verdict = j.Status,
                    Problem = Problems.Find(s.ProblemId),
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
        public async Task<IActionResult> Submission(int cid, int sid,
            [FromServices] ISubmissionStore submissions)
        {
            int teamid = Team.TeamId;

            var models = await submissions.ListWithJudgingAsync(
                predicate: s => s.ContestId == cid && s.SubmissionId == sid,
                selector: (s, j) => new SubmissionViewModel
                {
                    SubmissionId = s.SubmissionId,
                    Grade = j.TotalScore ?? 0,
                    Language = Languages[s.Language],
                    Time = s.Time,
                    Verdict = j.Status,
                    Problem = Problems.Find(s.ProblemId),
                    CompilerOutput = j.CompileError,
                    SourceCode = s.SourceCode,
                });

            var model = models.SingleOrDefault();
            if (model == null) return NotFound();
            return Window(model);
        }


        [HttpGet("problems/{prob}")]
        public async Task<IActionResult> Problemset(string prob,
            [FromServices] IProblemStore probs)
        {
            if (TooEarly && !ViewData.ContainsKey("IsJury")) return NotFound();
            var problem = Problems.Find(prob);
            if (problem == null) return NotFound();

            var viewFile = probs.GetFile(problem, "view.html");
            var view = await viewFile.ReadAsync();
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
        public async Task<IActionResult> Clarification(
            [FromServices] IClarificationStore claris,
            int cid, int clarid, bool needMore = true)
        {
            var toSee = await claris.FindAsync(cid, clarid);
            var clars = Enumerable.Empty<Clarification>();

            if (toSee?.CheckPermission(Team.TeamId) ?? true)
            {
                clars = clars.Append(toSee);

                if (needMore && toSee.ResponseToId.HasValue)
                {
                    int respid = toSee.ResponseToId.Value;
                    var toSee2 = await claris.FindAsync(cid, respid);
                    if (toSee2 != null) clars = clars.Prepend(toSee2);
                }
            }

            if (!clars.Any()) return NotFound();
            ViewData["TeamName"] = Team.TeamName;
            return Window(clars);
        }


        [HttpPost("clarifications/{op}")]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Clarification)]
        public async Task<IActionResult> Clarification(
            string op, AddClarificationModel model,
            [FromServices] IClarificationStore clars)
        {
            var (cid, teamid) = (Contest.ContestId, Team.TeamId);
            int repl = 0;
            if (op != "add" && !int.TryParse(op, out repl)) return NotFound();

            var replit = await clars.FindAsync(cid, repl);
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
                int id = await clars.SendAsync(
                    new Clarification
                    {
                        Body = model.Body,
                        SubmitTime = DateTimeOffset.Now,
                        ContestId = cid,
                        Sender = teamid,
                        ResponseToId = model.ReplyTo,
                        ProblemId = usage.Item3,
                        Category = usage.Item2,
                    });

                await HttpContext.AuditAsync("added", $"{id}");
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
            [FromServices] ISubmissionStore submits)
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

            var s = await Facade.SubmitAsync(
                code: model.Code,
                language: lang.Id,
                problemId: prob.ProblemId,
                contest: Contest,
                teamId: Team.TeamId,
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "team-page",
                username: User.GetUserName());

            await Mediator.SubmissionCreated(Contest, s);
            StatusMessage = "Submission done! Watch for the verdict in the list below.";
            return RedirectToAction(nameof(Home));
        }
    }
}
