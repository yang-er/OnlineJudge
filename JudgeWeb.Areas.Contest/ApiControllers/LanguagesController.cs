using JudgeWeb.Areas.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
    public class LanguagesController : ApiControllerBase
    {
        /// <summary>
        /// Get all the languages for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <response code="200">Returns all the languages for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestLanguage[]>> GetAll(int cid, string[] ids)
        {
            var query = DbContext.Languages
                .Where(l => l.AllowSubmit);
            if (ids != null && ids.Length > 0)
                query = query.Where(l => ids.Contains(l.ExternalId));

            return await query
                .Select(l => new ContestLanguage
                {
                    allow_judge = l.AllowJudge,
                    entry_point_description = null,
                    require_entry_point = false,
                    time_factor = l.TimeFactor,
                    extensions = new[] { l.FileExtension },
                    id = l.ExternalId,
                    name = l.Name,
                })
                .ToArrayAsync();
        }

        /// <summary>
        /// Get the given language for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given language for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestLanguage>> GetOne(int cid, string id)
        {
            return await DbContext.Languages
                .Where(l => l.ExternalId == id && l.AllowSubmit)
                .Select(l => new ContestLanguage
                {
                    allow_judge = l.AllowJudge,
                    entry_point_description = null,
                    require_entry_point = false,
                    time_factor = l.TimeFactor,
                    extensions = new[] { l.FileExtension },
                    id = l.ExternalId,
                    name = l.Name,
                })
                .SingleOrDefaultAsync();
        }
    }
}
