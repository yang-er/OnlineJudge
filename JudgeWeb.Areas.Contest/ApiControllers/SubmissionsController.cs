using JudgeWeb.Areas.Api.Models;
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
            {
                var lang = await DbContext.Languages
                    .Where(l => l.ExternalId == language_id)
                    .SingleOrDefaultAsync();
                if (lang == null) return Array.Empty<ContestSubmission>();
                int langid = lang.LangId;
                sq = sq.Where(s => s.Language == langid);
            }

            var sQuery =
                from s in sq
                join l in DbContext.Languages on s.Language equals l.LangId
                select new { LangId = l.ExternalId, s.SubmissionId, s.ProblemId, s.Author, s.Time };

            var submissions = await sQuery.ToListAsync();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return submissions
                .Select(submission => new ContestSubmission
                {
                    contest_time = submission.Time - contestTime,
                    id = $"{submission.SubmissionId}",
                    problem_id = $"{submission.ProblemId}",
                    team_id = $"{submission.Author}",
                    language_id = submission.LangId,
                    time = submission.Time,
                    files = new[]
                    {
                        new ContestSubmission.FileMeta
                        {
                            href = $"contests/{cid}/submissions/{submission.SubmissionId}/files",
                            mime = "application/zip"
                        }
                    }
                })
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
                join l in DbContext.Languages on s.Language equals l.LangId
                select new { LangId = l.ExternalId, s.SubmissionId, s.ProblemId, s.Author, s.Time };

            var submission = await sQuery.SingleOrDefaultAsync();
            if (submission == null) return null;
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return new ContestSubmission
            {
                contest_time = submission.Time - contestTime,
                id = $"{submission.SubmissionId}",
                problem_id = $"{submission.ProblemId}",
                team_id = $"{submission.Author}",
                language_id = submission.LangId,
                time = submission.Time,
                files = new[]
                {
                    new ContestSubmission.FileMeta
                    {
                        href = $"contests/{cid}/submissions/{id}/files",
                        mime = "application/zip"
                    }
                }
            };
        }
    }
}
