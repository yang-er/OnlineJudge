using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Contests.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("[area]/contests/{cid}/[controller]")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "CDS,Administrator")]
    [Produces("application/json")]
    public class ClarificationsController : ApiControllerBase
    {
        IClarificationStore Store { get; }
        public ClarificationsController(IClarificationStore store) => Store = store;


        /// <summary>
        /// Get all the clarifications for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="problem">Only show clarifications for the given problem</param>
        /// <response code="200">Returns all the clarifications for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Clarification[]>> GetAll(
            int cid, int[] ids, int? problem)
        {
            Expression<Func<Data.Clarification, bool>>? cond = null;
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(c => ids.Contains(c.ClarificationId));
            if (problem.HasValue)
                cond = cond.Combine(c => c.ProblemId == problem);
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            var res = await Store.ListAsync(cid, cond);
            return res.Select(c => new Clarification(c, contestTime)).ToArray();
        }


        /// <summary>
        /// Get the given clarifications for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given clarification for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Clarification>> GetOne(int cid, int id)
        {
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            var clar = await Store.FindAsync(cid, id);
            if (clar == null) return null;
            return new Clarification(clar, contestTime);
        }
    }
}
