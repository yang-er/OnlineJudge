using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class JudgementsController : ApiControllerBase
    {
        private IJudgingStore Store { get; }
        public JudgementsController(IJudgingStore store) => Store = store;


        /// <summary>
        /// Get all the judgements for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="result">Only show judgements with the given result</param>
        /// <param name="submission_id">Only show judgements for the given submission</param>
        /// <response code="200">Returns all the judgements for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Judgement[]>> GetAll(
            int cid, int[] ids, string result, int? submission_id)
        {
            Expression<Func<Data.Judging, bool>> cond =
                j => j.StartTime != null;
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(j => ids.Contains(j.JudgingId));
            if (submission_id.HasValue)
                cond = cond.Combine(j => j.SubmissionId == submission_id);
            var r2 = JudgementType.For(result);
            if (r2 != Verdict.Unknown)
                cond = cond.Combine(j => j.Status == r2);

            var js = await Store.ListAsync(cond, 100000);
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return js.Select(judging => new Judgement(judging, contestTime)).ToArray();
        }


        /// <summary>
        /// Get the given judgement for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given judgement for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Judgement>> GetOne(int cid, int id)
        {
            var (j, _, cid2, _, _) = await Store.FindAsync(id);
            if (j == null || cid2 != cid || j.StartTime == null) return null;
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return new Judgement(j, contestTime);
        }
    }
}
