using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Features.Scoreboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
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
        /// <param name="store"></param>
        /// <response code="200">Returns the scoreboard</response>
        [HttpGet]
        public async Task<ActionResult<Scoreboard>> OnGet(int cid, bool @public,
            [FromServices] ITeamStore store)
        {
            if (!Contest.StartTime.HasValue)
                return null;
            var scb = await store.LoadScoreboardAsync(cid);
            var affs = await store.ListAffiliationAsync(cid);
            var orgs = await store.ListCategoryAsync(cid, false);
            var probs = Problems;
            
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
                .Select(t => new Scoreboard.Row
                {
                    rank = t.Rank.Value,
                    team_id = $"{t.TeamId}",
                    score = new Scoreboard.Score(t.Points, t.Penalty),
                    problems = opt.Select(i => MakeProblem(t.Problems[i], probs[i]))
                });

            return new Scoreboard
            {
                time = Contest.StartTime.Value,
                contest_time = DateTimeOffset.Now - Contest.StartTime.Value,
                event_id = $"{MaxEventId}",
                state = new State(Contest),
                rows = go,
            };
        }

        private Scoreboard.Problem MakeProblem(ScoreCellModel s, Data.ContestProblem p)
        {
            if (s == null)
            {
                return new Scoreboard.Problem
                {
                    problem_id = $"{p.ProblemId}",
                    label = p.ShortName
                };
            }
            else
            {
                return new Scoreboard.Problem
                {
                    first_to_solve = s.IsFirstToSolve,
                    num_judged = s.JudgedCount,
                    num_pending = s.PendingCount,
                    problem_id = $"{p.ProblemId}",
                    solved = s.Score.HasValue,
                    label = p.ShortName,
                    time = s.Score ?? 0
                };
            }
        }
    }
}
