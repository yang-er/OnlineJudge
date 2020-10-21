using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Submission = JudgeWeb.Domains.Contests.ApiModels.Submission;

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
    public class SubmissionsController : ApiControllerBase
    {
        ISubmissionStore Store { get; }
        public SubmissionsController(ISubmissionStore store) => Store = store;


        /// <summary>
        /// Get all the submissions for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="language_id">Only show submissions for the given language</param>
        /// <response code="200">Returns all the submissions for this contest</response>
        [HttpGet]
        public async Task<ActionResult<Submission[]>> GetAll(
            int cid, int[] ids, string language_id)
        {
            Expression<Func<Data.Submission, bool>> cond = s => s.ContestId == cid;
            if (ids != null && ids.Length > 0)
                cond = cond.Combine(s => ids.Contains(s.SubmissionId));
            if (language_id != null)
                cond = cond.Combine(s => s.Language == language_id);

            var submissions = await Store.ListAsync(
                predicate: cond,
                projection: s => new { s.Language, s.SubmissionId, s.ProblemId, s.Author, s.Time });
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return submissions
                .Select(s => new Submission(cid, s.Language, s.SubmissionId, s.ProblemId, s.Author, s.Time, s.Time - contestTime))
                .ToArray();
        }


        /// <summary>
        /// Get the given submission for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given submission for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Submission>> GetOne(int cid, int id)
        {
            var ss = await Store.FindAsync(id);
            if (ss == null || ss.ContestId != cid) return null;
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;
            return new Submission(cid, ss.Language, ss.SubmissionId, ss.ProblemId, ss.Author, ss.Time, ss.Time - contestTime);
        }


        /// <summary>
        /// Restore a submission for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="model">The content</param>
        /// <response code="201">Added</response>
        [HttpPost("[action]")]
        [ProducesResponseType(201, Type = typeof(Submission))]
        public async Task<ActionResult<Submission>> Restore(int cid, RestoreSubmissionModel model)
        {
            var s = await Store.CreateAsync(
                code: model.code,
                language: model.langid,
                problemId: model.probid,
                contestId: Contest.ContestId,
                userId: model.teamid,
                ipAddr: System.Net.IPAddress.Parse(model.ip),
                via: "restorer",
                username: "api",
                time: model.time);

            await Mediator.SubmissionCreated(Contest, s);
            return Created($"/api/contests/{cid}/submissions/{s.SubmissionId}",
                new Submission(cid, s.Language, s.SubmissionId, s.ProblemId, s.Author, s.Time, s.Time - (Contest.StartTime ?? DateTimeOffset.Now)));
        }
    }
}
