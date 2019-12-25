using JudgeWeb.Areas.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

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
    public class JudgementTypesController : ApiControllerBase
    {
        /// <summary>
        /// Get all the judgement types for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <response code="200">Returns all the judgement types for this contest</response>
        [HttpGet]
        public ActionResult<JudgementType[]> GetAll(int cid, string[] ids)
        {
            if (ids.Length == 0)
                return JudgementType.Defaults;
            return ids
                .Join(
                    inner: JudgementType.Defaults,
                    outerKeySelector: s => s,
                    innerKeySelector: j => j.id,
                    resultSelector: (s, j) => j)
                .ToArray();
        }

        /// <summary>
        /// Get the given judgement type for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given judgement type for this contest</response>
        [HttpGet("{id}")]
        public ActionResult<JudgementType> GetOne(int cid, string id)
        {
            return JudgementType.Defaults
                .FirstOrDefault(j => j.id == id);
        }
    }
}
