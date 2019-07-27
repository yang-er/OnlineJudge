using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[controller]/[action]")]
    public partial class TeamController : BaseController<TeamService>
    {
        private Team Team { get; set; }

        protected override IActionResult BeforeActionExecuting()
        {
            Team = Service.Team;

            if (Team is null || Team.Status != 1)
            {
                return IsWindowAjax
                     ? Message("401 Unauthorized", "This contest is not active for you (yet).", MessageType.Danger)
                     : View("AccessDenied");
            }

            return base.BeforeActionExecuting();
        }

        [HttpGet]
        [Route("/[area]/{cid}/[controller]")]
        public IActionResult Home()
        {
            var (cid, teamid) = (Team.ContestId, Team.TeamId);
            var model = Service.GetOneTeam(teamid);
            var clars = Service.GetClarifications();
            var subs = Service.GetSubmissions();

            model.ReceivedClarifications = clars
                .Where(c => c.Sender != teamid)
                .Reverse();

            model.RequestedClarifications = clars
                .Where(c => c.Sender == teamid)
                .Reverse();

            model.Submissions = subs;

            foreach (var submit in subs)
            {
                var prob = model.Problems.FirstOrDefault(cp => cp.ProblemId == submit.ProblemId);
                submit.ProblemName = prob?.Title;
                submit.ProblemShortName = prob?.ShortName;
            }

            model.MessageInfo = DisplayMessage;
            return View(model);
        }

        [HttpGet]
        public IActionResult Problemset()
        {
            return View(Service.GetOneTeam(Team.TeamId));
        }

        [HttpGet("{sid}")]
        [ValidateInAjax]
        public IActionResult Submission(int sid)
        {
            var model = Service.GetSubmission(sid);
            if (model is null) return NotFound();
            var prob = Service.Problems
                .FirstOrDefault(cp => cp.ProblemId == model.ProblemId);
            model.ProblemName = prob?.Title;
            model.ProblemShortName = prob?.ShortName;
            return Window(model);
        }

        [HttpGet("{prob}")]
        public async Task<IActionResult> Problemset(string prob)
        {
            if ((Contest.StartTime ?? DateTime.MaxValue) > DateTime.Now) return NotFound();
            var problem = Service.Problems
                .FirstOrDefault(a => a.ShortName == prob);
            if (problem == default) return NotFound();

            var pe = HttpContext.RequestServices.GetRequiredService<IFileRepository>();
            pe.SetContext("Problems");
            var view = await pe.ReadPartAsync($"p{problem.ProblemId}", $"view.html");
            if (string.IsNullOrEmpty(view)) return NotFound();

            ViewData["Content"] = view;
            return View("ProblemView");
        }

        [HttpGet("{op}")]
        [ValidateInAjax]
        public IActionResult Clarification(string op)
        {
            ViewData["Problems"] = Service.Problems;
            if (op == "add") return Window("AddClarification", new AddClarificationModel());
            if (!int.TryParse(op, out int clarid)) return BadRequest();

            var clars = Service.GetClarification(clarid);
            if (clars is null) return NotFound();
            ViewData["TeamName"] = Team.TeamName;
            return Window(clars);
        }

        [HttpPost("{op}")]
        [ValidateAntiForgeryToken]
        public IActionResult Clarification(string op, AddClarificationModel model)
        {
            var (cid, teamid) = (Contest.ContestId, Team.TeamId);
            var probs = Service.Problems;
            int repl = 0;
            if (op != "add" && !int.TryParse(op, out repl)) return BadRequest();
            if (Service.GetClarification(repl, false) == null) return NotFound();

            string SolveAndAdd()
            {
                if (string.IsNullOrWhiteSpace(model.Body))
                    return "Error sending empty clarification.";

                var newClar = new Clarification
                {
                    Body = model.Body,
                    SubmitTime = DateTime.Now,
                    ContestId = cid,
                    JuryMember = null,
                    Sender = teamid,
                    ResponseToId = op == "add" ? default(int?) : repl,
                    Recipient = null,
                    ProblemId = null,
                };

                if (model.Type == "general")
                    newClar.Category = ClarificationCategory.General;
                else if (model.Type == "tech")
                    newClar.Category = ClarificationCategory.Technical;
                else if (!model.Type.StartsWith("prob-"))
                    return "Error detecting category.";
                else
                {
                    var prob = probs.FirstOrDefault(p => "prob-" + p.ShortName == model.Type);
                    if (prob is null) return "Error detecting problem.";
                    newClar.ProblemId = prob.ProblemId;
                    newClar.Category = ClarificationCategory.Problem;
                }

                Service.SendClarification(newClar);
                return "Clarification sent to the jury";
            }

            DisplayMessage = SolveAndAdd();
            return RedirectToAction(nameof(Home), new { cid });
        }

        [HttpGet]
        [ValidateInAjax]
        public IActionResult Submit()
        {
            if (!Contest.StartTime.HasValue || Contest.StartTime.Value >= DateTime.Now)
                return Message("Submit", "Contest not started.", MessageType.Danger);
            ViewData["Problems"] = Service.Problems;
            ViewData["Languages"] = Service.QueryLanguages()
                .Where(l => l.AllowSubmit)
                .ToDictionary(a => a.LangId, a => a.Name);
            return Window(new TeamCodeSubmitModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(TeamCodeSubmitModel model)
        {
            if (!Contest.StartTime.HasValue || Contest.StartTime.Value >= DateTime.Now)
                return Message("Submit", "Contest not started.", MessageType.Danger);
            if (SubmitCore(model) == -1) return NotFound();

            DisplayMessage = "Submission done! Watch for the verdict in the list below.";
            return RedirectToAction(nameof(Home), new { cid = Contest.ContestId });
        }
    }
}
