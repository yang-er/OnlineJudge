using JudgeWeb.Data.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    public class RunsController : ApiControllerBase
    {
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
        public async Task<ActionResult<ContestRun[]>> GetAll(
            int cid, int[] ids, int? first_id, int? last_id, int? judging_id, int? limit)
        {
            IQueryable<Data.Detail> detailSource = DbContext.Details;
            if (ids != null && ids.Length > 0)
                detailSource = detailSource.Where(d => ids.Contains(d.TestId));
            if (first_id.HasValue)
                detailSource = detailSource.Where(d => d.TestId >= first_id);
            if (last_id.HasValue)
                detailSource = detailSource.Where(d => d.TestId <= last_id);
            if (judging_id.HasValue)
                detailSource = detailSource.Where(d => d.JudgingId == judging_id);

            var runQuery =
                from d in detailSource
                join j in DbContext.Judgings on d.JudgingId equals j.JudgingId
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                join t in DbContext.Testcases on d.TestcaseId equals t.TestcaseId
                orderby d.TestId
                select new { d.JudgingId, d.CompleteTime, d.ExecuteTime, d.Status, d.TestId, t.Rank };

            if (limit.HasValue) runQuery = runQuery.Take(limit.Value);
            var runs = await runQuery.ToListAsync();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return runs
                .Select(run => new ContestRun(run.CompleteTime, run.CompleteTime - contestTime, run.TestId, run.JudgingId, run.Status, run.Rank, run.ExecuteTime))
                .ToArray();
        }

        /// <summary>
        /// Get the given run for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given run for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestRun>> GetOne(int cid, int id)
        {
            var runQuery =
                from d in DbContext.Details
                where d.TestId == id
                join j in DbContext.Judgings on d.JudgingId equals j.JudgingId
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                join t in DbContext.Testcases on d.TestcaseId equals t.TestcaseId
                select new { d.JudgingId, d.CompleteTime, d.ExecuteTime, d.Status, d.TestId, t.Rank };

            var run = await runQuery.SingleOrDefaultAsync();
            if (run == null) return NotFound();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return new ContestRun(run.CompleteTime, run.CompleteTime - contestTime, run.TestId, run.JudgingId, run.Status, run.Rank, run.ExecuteTime);
        }
    }
}
