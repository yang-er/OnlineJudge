using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[controller]/[action]")]
    public class JuryController : BaseController<JuryService>
    {
        protected override IActionResult BeforeActionExecuting()
        {
            if (!User.IsInRoles("Administrator,JuryOfContest" + Contest.ContestId))
                return Forbid();

            ViewData["InJury"] = true;
            var res = base.BeforeActionExecuting();
            int ucc = Service.GetUnansweredClarificationCount();
            int ptc = Service.GetPendingTeamCount();
            ViewBag.ucc = ucc != 0 ? ucc.ToString() : "";
            ViewBag.ptc = ptc != 0 ? ptc.ToString() : "";
            return res;
        }

        protected override (bool showPublic, bool isJury) ScoreboardChooseStrategy()
        {
            return (false, true);
        }

        protected override string GoAfterSubmit => nameof(Submission);

        [HttpGet]
        [Route("/[area]/{cid}/[controller]")]
        public IActionResult Home()
        {
            return View(new JuryHomeModel
            {
                Contest = Contest,
                Problem = Service.Problems,
                Message = DisplayMessage,
            });
        }

        [HttpPost("{target}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult ChangeState(string target)
        {
            DisplayMessage = Service.ChangeState(target);
            return RedirectToAction(nameof(Home), new { cid = Contest.ContestId });
        }

        [HttpGet("{page?}")]
        public IActionResult Auditlog(int page = 1)
        {
            if (page <= 0) return BadRequest();
            var model = Service.GetAuditLogs(page);
            ViewBag.Page = page;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(JuryEditModel model)
        {
            string msg;
            bool suc = ModelState.IsValid;

            if (suc)
            {
                (msg, suc) = Service.UpdateContest(model);
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var err in ModelState)
                    foreach (var errr in err.Value.Errors)
                        sb.AppendLine(errr.ErrorMessage);
                msg = sb.ToString();
            }

            if (suc)
            {
                DisplayMessage = msg;
                return RedirectToAction(nameof(Edit), new { cid = Contest.ContestId });
            }
            else
            {
                ViewBag.Categories = Service.QueryCategories(null).ToList();
                ViewBag.Repository = Service.GetAllProblems();
                ViewBag.Message = msg;

                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Edit()
        {
            ViewBag.Categories = Service.QueryCategories(null).ToList();
            ViewBag.Repository = Service.GetAllProblems();
            ViewBag.Message = DisplayMessage;

            var startTime = Contest.StartTime?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "";
            var startDateTime = Contest.StartTime ?? DateTimeOffset.UnixEpoch;
            var stopTime = Contest.EndTime.HasValue ? (Contest.EndTime.Value - startDateTime).ToDeltaString() : "";
            var unfTime = Contest.UnfreezeTime.HasValue ? (Contest.UnfreezeTime.Value - startDateTime).ToDeltaString() : "";
            var freTime = Contest.FreezeTime.HasValue ? (Contest.FreezeTime.Value - startDateTime).ToDeltaString() : "";

            return View(new JuryEditModel
            {
                ContestId = Contest.ContestId,
                FreezeTime = freTime,
                Name = Contest.Name,
                ShortName = Contest.ShortName,
                RankingStrategy = Contest.RankingStrategy,
                StartTime = startTime,
                StopTime = stopTime,
                UnfreezeTime = unfTime,
                DefaultCategory = Contest.RegisterDefaultCategory,
                BronzeMedal = Contest.BronzeMedal,
                GoldenMedal = Contest.GoldMedal,
                SilverMedal = Contest.SilverMedal,
                IsPublic = Contest.IsPublic,
                Problems = Service.Problems.ToDictionary(p => p.Rank - 1),
            });
        }

        [HttpGet]
        public IActionResult Submission()
        {
            var model = Service.GetSubmissions();
            return View("Submissions", model);
        }

        [HttpGet("{sid}/{gid?}")]
        public IActionResult Submission(int sid, int? gid)
        {
            var tc = Service.QueryTestcaseCount().ToDictionary(t => t.ProblemId);
            var lang = Service.QueryLanguages().ToDictionary(l => l.LangId);
            var model = Service.GetSubmission(sid, gid);
            if (model is null) return NotFound();

            model.LanguageName = lang[model.LanguageId].Name;
            model.LanguageExternalId = lang[model.LanguageId].ExternalId;
            model.Team = Service.QueryTeam(model.Author).FirstOrDefault();
            model.ProblemShortName = tc[model.ProblemId].ShortName;
            model.TestcaseNumber = tc[model.ProblemId].TestcaseCount;
            return View(model);
        }

        [HttpGet]
        [ValidateInAjax]
        public IActionResult Submit()
        {
            if (Service.Team is null || Service.Team.Status != 1)
                return Message("Submit", "You don't belong to a team yet.", MessageType.Danger);
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
            if (Service.Team is null || Service.Team.Status != 1)
                return Message("Submit", "You don't belong to a team yet.", MessageType.Danger);
            var sid = SubmitCore(model);
            if (sid == -1) return NotFound();
            return RedirectToAction(nameof(Submission), new { cid = Contest.ContestId, sid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult RefreshCache(bool post = true)
        {
            Service.RefreshScoreboardCache();
            DisplayMessage = "Scoreboard cache will be refreshed in minutes...";
            return RedirectToAction(nameof(Home));
        }

        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult RefreshCache()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Statistics()
        {
            return View(Service.GetStatistics());
        }

        [HttpGet]
        public IActionResult Rejudging() => Ok();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Assign(JuryAssignModel model)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                DisplayMessage = "Error user not found.";
            }
            else
            {
                var result = await UserManager
                    .AddToRoleAsync(user, "JuryOfContest" + Contest.ContestId);

                if (result.Succeeded)
                    DisplayMessage = $"Jury role of user {user.UserName} assigned.";
                else
                    DisplayMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Home), new { cid = Contest.ContestId });
        }

        [HttpGet]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult Assign()
        {
            return Window(new JuryAssignModel());
        }

        [HttpPost("{uid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unassign(int uid, bool unassign = true)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();
            var result = await UserManager
                .RemoveFromRoleAsync(user, "JuryOfContest" + Contest.ContestId);

            if (result.Succeeded)
                DisplayMessage = $"Jury role of user {user.UserName} unassigned.";
            else
                DisplayMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Home), new { cid = Contest.ContestId });
        }

        [HttpGet("{uid}")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unassign(int uid)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();
            return Window(user);
        }
    }
}
