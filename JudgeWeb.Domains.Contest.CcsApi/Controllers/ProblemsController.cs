using JudgeWeb.Domains.Contests.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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
    public class ProblemsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the problems for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <response code="200">Returns all the problems for this contest</response>
        [HttpGet]
        public ActionResult<Problem[]> GetAll(int cid, int[] ids)
        {
            IEnumerable<Data.ContestProblem> probs = Problems;
            if (ids != null && ids.Length > 0)
                probs = probs.Where(cp => ids.Contains(cp.ProblemId));
            return probs.Select(cp => new Problem(cp)).ToArray();
        }


        /// <summary>
        /// Get the given problem for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given problem for this contest</response>
        [HttpGet("{id}")]
        public ActionResult<Problem> GetOne(int cid, int id)
        {
            var prob = Problems.FirstOrDefault(cp => cp.ProblemId == id);
            return new Problem(prob);
        }
    }
}
