using JudgeWeb.Areas.Api.Models;
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
    public class ProblemsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the problems for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <response code="200">Returns all the problems for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestProblem[]>> GetAll(int cid, int[] ids)
        {
            var src = DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid);
            if (ids != null && ids.Length > 0)
                src = src.Where(cp => ids.Contains(cp.ProblemId));

            var result = await src
                .Join(
                    inner: DbContext.Problems,
                    outerKeySelector: cp => cp.ProblemId,
                    innerKeySelector: p => p.ProblemId,
                    resultSelector: (cp, p) => new ContestProblem
                    {
                        ordinal = cp.Rank - 1,
                        label = cp.ShortName,
                        short_name = cp.ShortName,
                        internalid = cp.ProblemId,
                        id = $"{cp.ProblemId}",
                        time_limit = p.TimeLimit / 1000.0,
                        name = p.Title,
                        rgb = cp.Color,
                    })
                .ToArrayAsync();

            ids = result.Select(r => r.internalid).ToArray();
            var tcc = await DbContext.Testcases
                .Where(t => ids.Contains(t.ProblemId))
                .GroupBy(t => t.ProblemId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in tcc)
                result.First(p => p.internalid == item.Key).test_data_count = item.Count;
            return result;
        }

        /// <summary>
        /// Get the given problem for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given problem for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestProblem>> GetOne(int cid, int id)
        {
            var result = await DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid && cp.ProblemId == id)
                .Join(
                    inner: DbContext.Problems,
                    outerKeySelector: cp => cp.ProblemId,
                    innerKeySelector: p => p.ProblemId,
                    resultSelector: (cp, p) => new ContestProblem
                    {
                        ordinal = cp.Rank - 1,
                        label = cp.ShortName,
                        short_name = cp.ShortName,
                        internalid = cp.ProblemId,
                        id = $"{cp.ProblemId}",
                        time_limit = p.TimeLimit / 1000.0,
                        name = p.Title,
                        rgb = cp.Color,
                    })
                .SingleOrDefaultAsync();

            if (result != null)
                result.test_data_count = await DbContext.Testcases
                    .Where(t => t.ProblemId == id)
                    .CountAsync();
            return result;
        }
    }
}
