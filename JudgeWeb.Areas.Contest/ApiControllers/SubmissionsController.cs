using JudgeWeb.Areas.Contest.Models;
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
    public class SubmissionsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the submissions for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="language_id">Only show submissions for the given language</param>
        /// <response code="200">Returns all the submissions for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestSubmission[]>> GetAll(
            int cid, int[] ids, string language_id)
        {
            var sq = DbContext.Submissions
                .Where(s => s.ContestId == cid);
            if (ids != null && ids.Length > 0)
                sq = sq.Where(s => ids.Contains(s.SubmissionId));
            if (language_id != null)
                sq = sq.Where(s => s.Language == language_id);
            
            var submissions = await sq
                .Select(s => new { LangId = s.Language, s.SubmissionId, s.ProblemId, s.Author, s.Time })
                .ToListAsync();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return submissions
                .Select(s => new ContestSubmission(cid, s.LangId, s.SubmissionId, s.ProblemId, s.Author, s.Time, s.Time - contestTime))
                .ToArray();
        }

        /// <summary>
        /// Get the given submission for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given submission for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestSubmission>> GetOne(int cid, int id)
        {
            var sQuery =
                from s in DbContext.Submissions
                where s.ContestId == cid && s.SubmissionId == id
                select new { LangId = s.Language, s.SubmissionId, s.ProblemId, s.Author, s.Time };

            var ss = await sQuery.SingleOrDefaultAsync();
            if (ss == null) return null;
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return new ContestSubmission(cid, ss.LangId, ss.SubmissionId, ss.ProblemId, ss.Author, ss.Time, ss.Time - contestTime);
        }

        /// <summary>
        /// Restore a submission for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="model">The content</param>
        /// <param name="scoreboardService">Internal service</param>
        /// <param name="submissionManager">Internal service</param>
        /// <response code="201">Added</response>
        [HttpPost("[action]")]
        public async Task<ActionResult<ContestSubmission>> Restore(int cid,
            RestoreSubmissionModel model,
            [FromServices] IScoreboardService scoreboardService,
            [FromServices] SubmissionManager submissionManager)
        {
            var lang = await DbContext.Languages
                .Where(l => l.Id == model.langid)
                .SingleOrDefaultAsync();
            if (lang == null) return NotFound();

            var s = await submissionManager.CreateAsync(
                code: model.code,
                langid: lang,
                probid: model.probid,
                cid: Contest,
                uid: model.teamid,
                ipAddr: System.Net.IPAddress.Parse(model.ip),
                via: "restorer",
                username: "api",
                time: model.time);

            scoreboardService.SubmissionCreated(Contest, s);
            return Created($"/api/contests/{cid}/submissions/{s.SubmissionId}",
                new ContestSubmission(cid, lang.Id, s.SubmissionId, s.ProblemId, s.Author, s.Time, s.Time - (Contest.StartTime ?? DateTimeOffset.Now)));
        }
    }
}
