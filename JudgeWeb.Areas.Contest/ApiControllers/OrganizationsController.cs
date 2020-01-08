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
    public class OrganizationsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the organizations for this contest
        /// </summary>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="cid">The contest ID</param>
        /// <param name="country">Only show organizations for the given country</param>
        /// <response code="200">Returns all the organizations for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestOrganization[]>> GetAll(
            int cid, string[] ids, string country)
        {
            var d = DbContext.Teams
                .Where(t => t.ContestId == cid && t.Status == 1)
                .Select(t => t.AffiliationId)
                .Distinct();

            var q = DbContext.TeamAffiliations
                .Where(a => d.Contains(a.AffiliationId));
            if (country != null)
                q = q.Where(t => t.CountryCode == country);
            if (ids != null && ids.Length > 0)
                q = q.Where(t => ids.Contains(t.ExternalId));

            return await q
                .Select(a => new ContestOrganization(a))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get the given organization for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given organization for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestOrganization>> GetOne(int cid, string id)
        {
            return await DbContext.TeamAffiliations
                .Where(a => a.ExternalId == id)
                .Select(a => new ContestOrganization(a))
                .FirstOrDefaultAsync();
        }
    }
}
