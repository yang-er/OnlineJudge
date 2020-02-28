using JudgeWeb.Data.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

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
    public class AwardsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the awards standings for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="strict">Whether to only include CCS compliant properties in the response</param>
        /// <response code="200">Returns the current teams qualifying for each award</response>
        [HttpGet]
        public ActionResult<ContestAward[]> GetAll(int cid, bool strict = false)
        {
            // There's no design for this module.
            return Array.Empty<ContestAward>();
        }

        /// <summary>
        /// Get the given award for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <param name="strict">Whether to only include CCS compliant properties in the response</param>
        /// <response code="200">Returns the award for this contest</response>
        [HttpGet("{id}")]
        public ActionResult<ContestGroup> GetOne(int cid, int id, bool strict = false)
        {
            // There's no design for this module.
            return null;
        }
    }
}
