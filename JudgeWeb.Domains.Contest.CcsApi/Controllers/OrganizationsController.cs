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
    public class OrganizationsController : ApiControllerBase
    {
        IAffiliationStore Store { get; }
        public OrganizationsController(IAffiliationStore store) => Store = store;


        /// <summary>
        /// Get all the organizations for this contest
        /// </summary>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="cid">The contest ID</param>
        /// <param name="country">Only show organizations for the given country</param>
        /// <response code="200">Returns all the organizations for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Organization[]>> GetAll(
            int cid, string[] ids, string country)
        {
            var d = Store.GetContestFilter(cid);
            Expression<Func<Data.TeamAffiliation, bool>> cond =
                a => d.Contains(a.AffiliationId);
            if (country != null)
                cond = cond.Combine(t => t.CountryCode == country);
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(t => ids.Contains(t.ExternalId));
            var results = await Store.ListAsync(cond);
            return results.Select(a => new Organization(a)).ToArray();
        }


        /// <summary>
        /// Get the given organization for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given organization for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Organization>> GetOne(int cid, string id)
        {
            var org = await Store.FindAsync(id);
            return org == null ? null : new Organization(org);
        }
    }
}
