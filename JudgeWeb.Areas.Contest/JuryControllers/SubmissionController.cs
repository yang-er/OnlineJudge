using JudgeWeb.Areas.Contest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/submissions")]
    public class JurySubmissionController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int cid, bool all = false)
        {
            ViewBag.TeamNames = await Service.GetTeamNameAsync(cid);
            return View(await Service.ListSubmissionsByJuryAsync(cid, null, all));
        }


        [HttpGet("{sid}/{jid?}")]
        public async Task<IActionResult> Detail(int cid, int sid, int? jid,
            [FromServices] Data.SubmissionManager submitMgr)
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
                .GroupJoin(
                    inner: submitMgr.Judgehosts,
                    outerKeySelector: j => j.ServerId,
                    innerKeySelector: h => h.ServerId,
                    resultSelector: (j, hh) => new { j, hh })
                .SelectMany(
                    collectionSelector: a => a.hh.DefaultIfEmpty(),
                    resultSelector: (a, h) => new { a.j, n = h.ServerName })
                .ToListAsync();

            var details = await submitMgr.GetDetailsAsync(result.j.JudgingId, result.s.ProblemId);
            var team = await Service.FindTeamByIdAsync(cid, result.s.Author);
            var prob = (await Service.GetProblemsAsync(cid))
                .FirstOrDefault(cp => cp.ProblemId == result.s.ProblemId);
            var lang = (await Service.GetLanguagesAsync(cid))[result.s.Language];

            return View(new JuryViewSubmissionModel
            {
                Submission = result.s,
                Judging = result.j,
                Details = details,
                AllJudgings = judgings.Select(a => (a.j, a.n)),
                Team = team,
                Problem = prob,
                Language = lang,
            });
        }
    }
}
