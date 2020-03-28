using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Domains.Identity;
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
    public class GroupsController : ApiControllerBase
    {
        ICategoryStore Store { get; }
        public GroupsController(ICategoryStore store) => Store = store;


        /// <summary>
        /// Get all the groups for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="public">Only show public groups, even for users with more permissions</param>
        /// <response code="200">Returns all the groups for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Group[]>> GetAll(
            int cid, int[] ids, bool @public)
        {
            Expression<Func<Data.TeamCategory, bool>>? cond = null;
            if (@public)
                cond = cond.Combine(c => c.IsPublic);
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(c => ids.Contains(c.CategoryId));

            else
            {
                var ids2 = Store.GetContestFilter(cid);
                cond = cond.Combine(c => ids2.Contains(c.CategoryId));
            }

            var results = await Store.ListAsync(cond);
            return results.Select(c => new Group(c)).ToArray();
        }


        /// <summary>
        /// Get the given group for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given group for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Group>> GetOne(int cid, int id)
        {
            var cat = await Store.FindAsync(id);
            return cat == null ? null : new Group(cat);
        }
    }
}
