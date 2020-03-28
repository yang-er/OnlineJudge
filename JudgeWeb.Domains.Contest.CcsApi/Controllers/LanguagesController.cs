using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public ILanguageStore Store { get; }
        public LanguagesController(ILanguageStore store) => Store = store;


        /// <summary>
        /// Get all the languages for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <response code="200">Returns all the languages for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Language[]>> GetAll(int cid, string[] ids)
        {
            var langs = await Store.ListAsync(true);
            var query = langs.AsEnumerable();
            if (ids != null && ids.Length > 0)
                query = query.Where(l => ids.Contains(l.Id));
            return query.Select(l => new Language(l)).ToArray();
        }


        /// <summary>
        /// Get the given language for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given language for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Language>> GetOne(int cid, string id)
        {
            var langs = await Store.ListAsync(true);
            var ll = langs
                .Where(l => l.Id == id && l.AllowSubmit)
                .SingleOrDefault();
            return new Language(ll);
        }
    }
}
