using JudgeWeb.Data;
using JudgeWeb.Data.Api;
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
            var langs = await DbContext.GetLanguagesAsync(cid);
            var query = langs.Values.Where(l => l.AllowSubmit);
            if (ids != null && ids.Length > 0)
                query = query.Where(l => ids.Contains(l.ExternalId));
            return query.Select(l => new ContestLanguage(l)).ToArray();
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
            var langs = await DbContext.GetLanguagesAsync(cid);
            var ll = langs.Values
                .Where(l => l.ExternalId == id && l.AllowSubmit)
                .SingleOrDefault();
            return new ContestLanguage(ll);
        }
    }
}
