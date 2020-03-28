using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Contests.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    public class TeamsController : ApiControllerBase
    {
        ITeamStore Store { get; }

        public TeamsController(ITeamStore store) => Store = store;


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
        public async Task<ActionResult<List<Team>>> GetAll(
            int cid, int[] ids, int? category, string affiliation, bool @public)
        {
            Expression<Func<Data.Team, bool>>? cond = null;
            if (category.HasValue)
                cond = cond.Combine(t => t.CategoryId == category);
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(t => ids.Contains(t.TeamId));
            if (affiliation != null)
                cond = cond.Combine(t => t.Affiliation.ExternalId == affiliation);
            if (@public)
                cond = cond.Combine(t => t.Category.IsPublic);

            return await Store.ListAsync(cid,
                selector: t => new Team(t, t.Affiliation),
                predicate: cond);
        }


        /// <summary>
        /// Get the given team for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given team for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Team>> GetOne(int cid, int id)
        {
            return await Store.FindAsync(cid, id, t => new Team(t, t.Affiliation));
        }
    }
}
