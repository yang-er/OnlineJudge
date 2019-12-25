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
    public class JudgementsController : ApiControllerBase
    {
        /// <summary>
        /// Get all the judgements for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="ids">Filter the objects to get on this list of ID's</param>
        /// <param name="result">Only show judgements with the given result</param>
        /// <param name="submission_id">Only show judgements for the given submission</param>
        /// <response code="200">Returns all the judgements for this contest</response>
        [HttpGet]
        public async Task<ActionResult<ContestJudgement[]>> GetAll(
            int cid, int[] ids, string result, int? submission_id)
        {
            IQueryable<Data.Judging> jQuery = DbContext.Judgings;
            if (ids != null && ids.Length > 0)
                jQuery = jQuery.Where(j => ids.Contains(j.JudgingId));
            if (submission_id.HasValue)
                jQuery = jQuery.Where(j => j.SubmissionId == submission_id);
            var r2 = JudgementType.For(result);
            if (r2 != Verdict.Unknown)
                jQuery = jQuery.Where(j => j.Status == r2);

            var jQuery2 =
                from j in jQuery
                where j.StartTime != null
                join h in DbContext.JudgeHosts on j.ServerId equals h.ServerId
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                select new { j, h.ServerName };

            var js = await jQuery2.ToListAsync();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            return js
                .Select(judging =>
                {
                    var cj = new ContestJudgement
                    {
                        id = $"{judging.j.JudgingId}",
                        submission_id = $"{judging.j.SubmissionId}",
                        judgehost = judging.ServerName,
                        judgement_type_id = JudgementType.For(judging.j.Status),
                        valid = judging.j.Active,
                        start_contest_time = judging.j.StartTime.Value - contestTime,
                        start_time = judging.j.StartTime.Value,
                    };

                    if (cj.judgement_type_id != null)
                    {
                        cj.end_contest_time = judging.j.StopTime.Value - contestTime;
                        cj.end_time = judging.j.StopTime.Value;
                        if (cj.judgement_type_id != "CE" && cj.judgement_type_id != "JE")
                            cj.max_run_time = judging.j.ExecuteTime / 1000.0;
                    }

                    return cj;
                })
                .ToArray();
        }

        /// <summary>
        /// Get the given judgement for this contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">Returns the given judgement for this contest</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContestJudgement>> GetOne(int cid, int id)
        {
            var jQuery =
                from j in DbContext.Judgings
                where j.StartTime != null && j.JudgingId == id
                join h in DbContext.JudgeHosts on j.ServerId equals h.ServerId
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                select new { j, h.ServerName };

            var judging = await jQuery.SingleOrDefaultAsync();
            if (judging == null) return NotFound();
            var contestTime = Contest.StartTime ?? DateTimeOffset.Now;

            var cj = new ContestJudgement
            {
                id = $"{judging.j.JudgingId}",
                submission_id = $"{judging.j.SubmissionId}",
                judgehost = judging.ServerName,
                judgement_type_id = JudgementType.For(judging.j.Status),
                valid = judging.j.Active,
                start_contest_time = judging.j.StartTime.Value - contestTime,
                start_time = judging.j.StartTime.Value,
            };

            if (cj.judgement_type_id != null)
            {
                cj.end_contest_time = judging.j.StopTime.Value - contestTime;
                cj.end_time = judging.j.StopTime.Value;
                if (cj.judgement_type_id != "CE" && cj.judgement_type_id != "JE")
                    cj.max_run_time = judging.j.ExecuteTime / 1000.0;
            }

            return cj;
        }
    }
}
