using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    [AuditPoint(AuditlogType.Rejudging)]
    public class RejudgingsController : JuryControllerBase
    {
        public IRejudgingStore Store { get; }

        public RejudgingsController(IRejudgingStore store)
        {
            Store = store;
        }


        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            return View(await Store.ListAsync(cid));
        }


        [HttpGet("{rid}")]
        public async Task<IActionResult> Detail(int cid, int rid)
        {
            var model = await Store.FindAsync(cid, rid);
            if (model == null) return NotFound();
            ViewBag.Teams = await Facade.Teams.ListNamesAsync(cid);
            ViewBag.Judgings = await Store.ViewAsync(model);
            return View(model);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Add(int cid,
            [FromServices] IJudgehostStore jhs)
        {
            ViewBag.Teams = await Facade.Teams.ListNamesAsync(cid);
            ViewBag.Judgehosts = await jhs.ListAsync();
            return View(new AddRejudgingModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int cid, AddRejudgingModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var r = await Store.CreateAsync(new Rejudge
            {
                ContestId = Contest.ContestId,
                Reason = model.Reason,
                StartTime = DateTimeOffset.Now,
                IssuedBy = int.Parse(User.GetUserId())
            });

            Expression<Func<Submission, Judging, bool>> cond
                = (s, j) => s.RejudgeId == null && s.ContestId == cid;

            if (model.Submission.HasValue)
            {
                int sid = model.Submission.Value;
                cond = cond.Combine((s, j) => s.SubmissionId == sid);
            }
            else
            {
                var probs = model.Problems ?? Array.Empty<int>();
                if (probs.Length > 0)
                    cond = cond.Combine((s, j) => probs.Contains(s.ProblemId));

                var teams = model.Teams ?? Array.Empty<int>();
                if (teams.Length > 0)
                    cond = cond.Combine((s, j) => teams.Contains(s.Author));

                var langs = model.Languages ?? Array.Empty<string>();
                if (langs.Length > 0)
                    cond = cond.Combine((s, j) => langs.Contains(s.Language));

                if (model.TimeBefore.TryParseAsTimeSpan(out var tb) && tb.HasValue)
                {
                    var timed = (Contest.StartTime ?? DateTimeOffset.Now) + tb.Value;
                    cond = cond.Combine((s, j) => s.Time <= timed);
                }

                if (model.TimeAfter.TryParseAsTimeSpan(out var ta) && ta.HasValue)
                {
                    var timed = (Contest.StartTime ?? DateTimeOffset.Now) + ta.Value;
                    cond = cond.Combine((s, j) => s.Time >= timed);
                }

                var hosts = model.Judgehosts ?? Array.Empty<string>();
                if (hosts.Length > 0)
                    cond = cond.Combine((s, j) => hosts.Contains(j.Server));

                var verds = model.Verdicts ?? Array.Empty<Verdict>();
                if (model.Verdicts.Length > 0)
                    cond = cond.Combine((s, j) => verds.Contains(j.Status));
            }

            int tok = await Store.BatchRejudgeAsync(cond, r,
                fullTest: Contest.RankingStrategy == 2);
            // if oi mode, force full judge

            if (tok == 0)
            {
                await Store.DeleteAsync(r);
                StatusMessage = "Error no submissions was rejudged.";
                return RedirectToAction(nameof(List));
            }
            else
            {
                StatusMessage = $"{tok} submissions will be rejudged.";
                await HttpContext.AuditAsync("added", $"{r.RejudgeId}", $"with {tok} submissions");
                return RedirectToAction(nameof(Detail), new { rid = r.RejudgeId });
            }
        }


        [HttpGet("{rid}/[action]")]
        [ValidateInAjax]
        public IActionResult Repeat()
        {
            return AskPost(
                title: "Repeat rejudging",
                message: "This will create a new rejudging with the same submissions as this rejudging.",
                area: "Contest", ctrl: "Rejudgings", act: "Repeat",
                type: MessageType.Primary);
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Repeat(int cid, int rid)
        {
            var rej = await Store.FindAsync(cid, rid);
            if (rej == null) return NotFound();

            if (rej.OperatedBy == null)
            {
                StatusMessage = "Error rejudging has not been finished.";
                return RedirectToAction(nameof(Detail));
            }

            var r2e = await Store.CreateAsync(new Rejudge
            {
                ContestId = Contest.ContestId,
                StartTime = DateTimeOffset.Now,
                IssuedBy = int.Parse(User.GetUserId()),
                Reason = "repeat: " + rej.Reason,
            });

            int tok = await Store.BatchRejudgeAsync(
                predicate: (s, j) => j.RejudgeId == rid,
                rejudge: r2e);

            if (tok == 0)
            {
                await Store.DeleteAsync(r2e);
                StatusMessage = "Error no submissions was rejudged.";
                return RedirectToAction(nameof(Detail));
            }
            else
            {
                StatusMessage = $"{tok} submissions will be rejudged.";
                await HttpContext.AuditAsync("added", $"{r2e.RejudgeId}", $"with {tok} submissions");
                return RedirectToAction(nameof(Detail), new { rid = r2e.RejudgeId });
            }
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int cid, int rid)
        {
            var rej = await Store.FindAsync(cid, rid);
            if (rej == null || rej.EndTime != null) return NotFound();
            await Store.CancelAsync(rej, int.Parse(User.GetUserId()));
            await HttpContext.AuditAsync("cancelled", $"{rid}");
            return RedirectToAction(nameof(Detail));
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int cid, int rid,
            [FromServices] IJudgingStore judgings)
        {
            var rej = await Store.FindAsync(cid, rid);
            if (rej == null || rej.EndTime != null) return NotFound();

            var pending = await judgings.CountAsync(j =>
                j.RejudgeId == rid &&
                (j.Status == Verdict.Pending || j.Status == Verdict.Running));

            if (pending > 0)
            {
                StatusMessage = "Error some submissions are not ready.";
                return RedirectToAction(nameof(Detail));
            }

            await Store.ApplyAsync(rej, int.Parse(User.GetUserId()));
            await HttpContext.AuditAsync("applied", $"{rid}");
            await Mediator.RefreshScoreboardCache(Contest);
            StatusMessage = "Rejudging applied. Scoreboard cache will be refreshed.";
            return RedirectToAction(nameof(Detail));
        }
    }
}
