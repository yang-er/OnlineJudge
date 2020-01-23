using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Authorize]
    public abstract class JuryControllerBase : Controller3
    {
        public override Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            // check the permission
            if (!ViewData.ContainsKey("IsJury"))
            {
                context.Result = Forbid();
                return Task.CompletedTask;
            }

            ViewData["InJury"] = true;
            return base.OnActionExecutingAsync(context);
        }

        protected void RefreshScoreboardCache(int cid)
        {
            DbContext.UpdateScoreboard(new ScoreboardState
            {
                ContestId = cid,
                SubmissionId = -1,
                Time = DateTimeOffset.Now,
            });
        }

        protected async Task<IEnumerable<SubmissionViewModel>> ListSubmissionsByJuryAsync(
            int cid, int? teamid = null, bool all = true)
        {
            var teamNames = await DbContext.GetTeamNameAsync(cid);

            return await Cache.GetOrCreateAsync($"`c{cid}`t{teamid ?? -1}`sub_jury`{all}", async entry =>
            {
                var submissions = DbContext.Submissions
                    .Where(s => s.ContestId == cid);
                if (teamid.HasValue)
                    submissions = submissions.Where(s => s.Author == teamid);
                submissions = submissions.OrderByDescending(s => s.Time);
                if (!all) submissions = submissions.Take(75);
                else submissions = submissions.Take(10000);

                var query =
                    from s in submissions
                    join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                    join d in DbContext.Details on j.JudgingId equals d.JudgingId into dd
                    from d in dd.DefaultIfEmpty()
                    select new { s.SubmissionId, s.Time, j.Status, s.ProblemId, s.Author, s.Language, d = (Verdict?)d.Status, s.Ip };
                var result = await query.ToListAsync();

                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);

                return result
                    .GroupBy(a => new { a.Status, a.SubmissionId, a.Time, a.ProblemId, a.Author, a.Language, a.Ip }, a => a.d)
                    .OrderByDescending(g => g.Key.Time)
                    .Select(g => new SubmissionViewModel
                    {
                        Language = Languages.GetValueOrDefault(g.Key.Language),
                        TeamId = g.Key.Author,
                        TeamName = teamNames.GetValueOrDefault(g.Key.Author),
                        Problem = Problems.Find(g.Key.ProblemId),
                        SubmissionId = g.Key.SubmissionId,
                        Verdict = g.Key.Status,
                        Time = g.Key.Time,
                        CompilerOutput = g.Key.Ip,
                        Details = g.Where(v => v.HasValue).Select(v => v.Value).ToArray()
                    })
                    .ToList();
            });
        }
    }
}
