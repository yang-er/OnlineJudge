using JudgeWeb.Data.Api;
using JudgeWeb.Features.Scoreboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 CDS 连接的API控制器。
    /// </summary>
    [Area("Api")]
    [Route("[area]/contests/{cid}/[controller]")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "CDS,Administrator")]
    [Produces("application/json")]
    public class ScoreboardController : ApiControllerBase
    {
        /// <summary>
        /// Get the scoreboard for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="public">Show publicly visible scoreboard, even for users with more permissions</param>
        /// <response code="200">Returns the scoreboard</response>
        [HttpGet]
        public async Task<ActionResult<ContestScoreboard>> OnGet(int cid, bool @public)
        {
            if (!Contest.StartTime.HasValue)
                return null;
            var scb = await DbContext.LoadScoreboardAsync(cid);
            var affs = await DbContext.ListTeamAffiliationAsync(cid);
            var orgs = await DbContext.ListTeamCategoryAsync(cid, false);
            var probs = await DbContext.GetProblemsAsync(cid);

            var events = await DbContext.Events
                .Where(e => e.ContestId == cid)
                .OrderByDescending(e => e.EventId)
                .Select(e => e.EventId)
                .Take(1)
                .SingleOrDefaultAsync();
            
            var board = new FullBoardViewModel
            {
                RankCache = scb.Data.Values,
                UpdateTime = scb.RefreshTime,
                Problems = probs,
                IsPublic = @public,
                Categories = orgs,
                Contest = Contest,
                Affiliations = affs,
            };

            var opt = new int[probs.Length];
            for (int i = 0; i < probs.Length; i++)
                opt[i] = i;

            var go = board
                .SelectMany(a => a)
                .Select(t => new ContestScoreboard.Row
                {
                    rank = t.Rank.Value,
                    team_id = $"{t.TeamId}",
                    score = new ContestScoreboard.Score(t.Points, t.Penalty),
                    problems = opt.Select(i => MakeProblem(t.Problems[i], probs[i]))
                });

            return new ContestScoreboard
            {
                time = Contest.StartTime.Value,
                contest_time = DateTimeOffset.Now - Contest.StartTime.Value,
                event_id = events.ToString(),
                state = new ContestTime(Contest),
                rows = go,
            };
        }

        private ContestScoreboard.Problem MakeProblem(ScoreCellModel s, Data.ContestProblem p)
        {
            if (s == null)
            {
                return new ContestScoreboard.Problem
                {
                    problem_id = $"{p.ProblemId}",
                    label = p.ShortName
                };
            }
            else
            {
                return new ContestScoreboard.Problem
                {
                    first_to_solve = s.IsFirstToSolve,
                    num_judged = s.JudgedCount,
                    num_pending = s.PendingCount,
                    problem_id = $"{p.ProblemId}",
                    solved = s.SolveTime.HasValue,
                    label = p.ShortName,
                    time = s.SolveTime ?? 0
                };
            }
        }
    }
}
