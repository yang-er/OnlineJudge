using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Linq.Expressions;
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
    public class RunsController : ApiControllerBase
    {
        private IJudgingStore Store { get; }
        public RunsController(IJudgingStore store) => Store = store;


        /// <summary>
        /// Get all the runs for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="first_id">Only show runs starting from this ID</param>
        /// <param name="last_id">Only show runs until this ID</param>
        /// <param name="judging_id">Only show runs for this judgement</param>
        /// <param name="limit">Limit the number of returned runs to this amount</param>
        /// <response code="200">Returns all the runs for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Run[]>> GetAll(
            int cid, int[] ids, int? first_id, int? last_id, int? judging_id, int? limit)
        {
            Expression<Func<Data.Testcase, Data.Detail, bool>> cond =
                (t, d) => d.j.s.ContestId == cid;
            if (ids != null && ids.Length > 0)
                cond = cond.Combine((t, d) => ids.Contains(d.TestId));
            if (first_id.HasValue)
                cond = cond.Combine((t, d) => d.TestId >= first_id);
            if (last_id.HasValue)
                cond = cond.Combine((t, d) => d.TestId <= last_id);
            if (judging_id.HasValue)
                cond = cond.Combine((t, d) => d.JudgingId == judging_id);

            var runs = await Store.GetDetailsInnerJoinAsync(
                (t, d) => new { d.JudgingId, d.CompleteTime, d.ExecuteTime, d.Status, d.TestId, t.Rank },
                cond, limit);
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return runs
                .Select(run => new Run(run.CompleteTime, run.CompleteTime - contestTime, run.TestId, run.JudgingId, run.Status, run.Rank, run.ExecuteTime))
                .ToArray();
        }


        /// <summary>
        /// Get the given run for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given run for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Run>> GetOne(int cid, int id)
        {
            var runQuery = await Store.GetDetailsInnerJoinAsync(
                (t, d) => new { d.JudgingId, d.CompleteTime, d.ExecuteTime, d.Status, d.TestId, t.Rank },
                (t, d) => d.j.s.ContestId == cid && d.TestId == id);
            var run = runQuery.SingleOrDefault();
            if (run == null) return NotFound();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return new Run(run.CompleteTime, run.CompleteTime - contestTime, run.TestId, run.JudgingId, run.Status, run.Rank, run.ExecuteTime);
        }
    }
}
