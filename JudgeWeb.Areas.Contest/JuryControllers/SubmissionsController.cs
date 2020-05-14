using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class SubmissionsController : JuryControllerBase
    {
        private ISubmissionStore Store { get; }

        public SubmissionsController(ISubmissionStore store)
        {
            Store = store;
        }


        [HttpGet]
        public async Task<IActionResult> List(int cid, bool all = false)
        {
            ViewBag.TeamNames = await Facade.Teams.ListNamesAsync(cid);
            return View(await ListSubmissionsByJuryAsync(cid, null, all));
        }


        [HttpGet("{sid}/{jid?}")]
        public async Task<IActionResult> Detail(int cid, int sid, int? jid,
            [FromServices] IJudgingStore store2)
        {
            var submit = await Store.FindAsync(sid, true);
            if (submit == null || submit.ContestId != cid) return NotFound();
            var judgings = submit.Judgings;

            var prob = Problems.SingleOrDefault(p => p.ProblemId == submit.ProblemId);
            if (prob == null) return NotFound(); // the problem is deleted later

            var judging = jid.HasValue
                ? judgings.SingleOrDefault(j => j.JudgingId == jid.Value)
                : judgings.SingleOrDefault(j => j.Active);
            if (judging == null) return NotFound();

            return View(new JuryViewSubmissionModel
            {
                Submission = submit,
                Judging = judging,
                AllJudgings = judgings,
                Details = await store2.GetDetailsAsync(submit.ProblemId, judging.JudgingId),
                Team = await Facade.Teams.FindByIdAsync(cid, submit.Author),
                Problem = prob,
                Language = Languages[submit.Language],
            });
        }


        [HttpGet("{sid}/[action]")]
        public async Task<IActionResult> Source(int cid, int sid)
        {
            var submit = await Store.FindAsync(sid);
            if (submit == null || submit.ContestId != cid) return NotFound();

            var lasts = await Store.ListWithJudgingAsync(
                pagination: (1, 1),
                predicate: s => s.ContestId == cid && s.SubmissionId < sid &&
                    s.Author == submit.Author && s.ProblemId == submit.ProblemId,
                selector: (s, j) => new { s.Language, s.SourceCode, s.SubmissionId });
            var last = lasts.list.SingleOrDefault();

            return View(new SubmissionSourceModel
            {
                ProblemId = submit.ProblemId,
                TeamId = submit.Author,
                NewCode = submit.SourceCode,
                NewId = submit.SubmissionId,
                NewLang = Languages[submit.Language],
                OldCode = last?.SourceCode,
                OldId = last?.SubmissionId,
                OldLang = Languages.GetValueOrDefault(last?.Language ?? string.Empty),
            });
        }


        [HttpGet("{sid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Rejudge(int cid, int sid)
        {
            var sub = await Store.FindAsync(sid);
            if (sub == null || sub.ContestId != cid) return NotFound();

            if (sub.RejudgeId.HasValue)
                return RedirectToAction("Detail", "Rejudgings", new { rid = sub.RejudgeId });

            return Window(new AddRejudgingModel
            {
                Submission = sid,
                Reason = $"submission: {sid}",
            });
        }
    }
}
