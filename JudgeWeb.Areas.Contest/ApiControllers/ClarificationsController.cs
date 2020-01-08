using JudgeWeb.Data.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
        /// <summary>
        /// Get all the clarifications for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="problem">Only show clarifications for the given problem</param>
        /// <response code="200">Returns all the clarifications for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestClarification[]>> GetAll(
            int cid, int[] ids, int? problem)
        {
            var query = DbContext.Clarifications
                .Where(c => c.ContestId == cid);
            if (ids != null && ids.Length > 0)
                query = query.Where(c => ids.Contains(c.ClarificationId));
            if (problem.HasValue)
                query = query.Where(c => c.ProblemId == problem);
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return await query
                .Select(c => new ContestClarification(c, contestTime))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get the given clarifications for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given clarification for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestClarification>> GetOne(int cid, int id)
        {
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return await DbContext.Clarifications
                .Where(c => c.ClarificationId == id && c.ContestId == cid)
                .Select(c => new ContestClarification(c, contestTime))
                .FirstOrDefaultAsync();
        }
    }
}
