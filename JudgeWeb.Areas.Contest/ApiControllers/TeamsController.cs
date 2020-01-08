using JudgeWeb.Data.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class TeamsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the teams for this contest
        /// </summary>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="cid">The contest ID</param>
        /// <param name="category">Only show teams for the given category</param>
        /// <param name="affiliation">Only show teams for the given affiliation / organization</param>
        /// <param name="public">Only show visible teams, even for users with more permissions</param>
        /// <response code="200">Returns all the teams for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestTeam[]>> GetAll(
            int cid, int[] ids, int? category, string affiliation, bool @public)
        {
            var teams = DbContext.Teams
                .Where(t => t.ContestId == cid);
            if (category.HasValue)
                teams = teams.Where(t => t.CategoryId == category);
            if (ids != null && ids.Length > 0)
                teams = teams.Where(t => ids.Contains(t.TeamId));
            var query2 = teams.Join(
                inner: DbContext.TeamAffiliations,
                outerKeySelector: t => t.AffiliationId,
                innerKeySelector: a => a.AffiliationId,
                resultSelector: (t, a) => new { t, a });
            if (affiliation != null)
                query2 = query2.Where(a => a.a.ExternalId == affiliation);
            if (@public)
                query2 = query2
                    .Join(
                        inner: DbContext.TeamCategories,
                        outerKeySelector: t => t.t.CategoryId,
                        innerKeySelector: c => c.CategoryId,
                        resultSelector: (a, c) => new { a.t, a.a, c })
                    .Where(a => a.c.IsPublic)
                    .Select(a => new { a.t, a.a });

            return await query2
                .Select(a => new ContestTeam(a.t, a.a))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get the given team for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given team for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestTeam>> GetOne(int cid, int id)
        {
            return await DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == id)
                .Join(
                    inner: DbContext.TeamAffiliations,
                    outerKeySelector: t => t.AffiliationId,
                    innerKeySelector: a => a.AffiliationId,
                    resultSelector: (t, a) => new ContestTeam(t, a))
                .FirstOrDefaultAsync();
        }
    }
}
