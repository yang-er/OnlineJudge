using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class SubmissionsController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int cid, bool all = false)
        {
            ViewBag.TeamNames = await Facade.Teams.ListNamesAsync(cid);
            return View(await ListSubmissionsByJuryAsync(cid, null, all));
        }


        [HttpGet("{sid}/{jid?}")]
        public async Task<IActionResult> Detail(int cid, int sid, int? jid,
            [FromServices] SubmissionManager submitMgr)
        {
            var judging = submitMgr.Judgings
                .Where(g => g.SubmissionId == sid);
            if (jid.HasValue)
                judging = judging.Where(g => g.JudgingId == jid);
            else
                judging = judging.Where(g => g.Active);

            var result = await judging
                .Join(
                    inner: submitMgr.Submissions,
                    outerKeySelector: j => j.SubmissionId,
                    innerKeySelector: s => s.SubmissionId,
                    resultSelector: (j, s) => new { j, s })
                .Where(a => a.s.ContestId == cid)
                .SingleOrDefaultAsync();
            if (result == null) return NotFound();

            var judgings = await submitMgr.Judgings
                .Where(j => j.SubmissionId == sid)
                .ToListAsync();

            var details = await submitMgr.GetDetailsAsync(result.j.JudgingId, result.s.ProblemId);
            var team = await FindTeamByIdAsync(result.s.Author);
            var prob = Problems.Find(result.s.ProblemId);
            if (prob == null) return NotFound(); // the problem is deleted later
            var lang = Languages[result.s.Language];

            return View(new JuryViewSubmissionModel
            {
                Submission = result.s,
                Judging = result.j,
                Details = details,
                AllJudgings = judgings,
                Team = team,
                Problem = prob,
                Language = lang,
            });
        }


        [HttpGet("{sid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Rejudge(int cid, int sid)
        {
            var sub = await DbContext.Submissions
                .Where(s => s.SubmissionId == sid && s.ContestId == cid)
                .FirstOrDefaultAsync();
            if (sub == null) return NotFound();

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
