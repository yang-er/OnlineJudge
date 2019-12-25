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
    public class GroupsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the groups for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="public">Only show public groups, even for users with more permissions</param>
        /// <response code="200">Returns all the groups for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestGroup[]>> GetAll(
            int cid, int[] ids, bool @public)
        {
            IQueryable<Data.TeamCategory> query = DbContext.TeamCategories;
            if (@public) query = query.Where(c => c.IsPublic);

            if (ids != null && ids.Length > 0)
            {
                query = query.Where(c => ids.Contains(c.CategoryId));
            }
            else
            {
                var cts = DbContext.Teams
                    .Where(t => t.ContestId == cid)
                    .Select(t => t.CategoryId)
                    .Distinct();
                query = query.Where(c => cts.Contains(c.CategoryId));
            }

            return await query
                .Select(c => new ContestGroup
                {
                    hidden = !c.IsPublic,
                    color = c.Color,
                    icpc_id = "cat" + c.CategoryId,
                    id = c.CategoryId.ToString(),
                    name = c.Name,
                    sortorder = c.SortOrder,
                })
                .ToArrayAsync();
        }

        /// <summary>
        /// Get the given group for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given group for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestGroup>> GetOne(int cid, int id)
        {
            return await DbContext.TeamCategories
                .Where(c => c.CategoryId == id)
                .Select(c => new ContestGroup
                {
                    hidden = !c.IsPublic,
                    color = c.Color,
                    icpc_id = "cat" + c.CategoryId,
                    id = c.CategoryId.ToString(),
                    name = c.Name,
                    sortorder = c.SortOrder,
                })
                .FirstOrDefaultAsync();
        }
    }
}
