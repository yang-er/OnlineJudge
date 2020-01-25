using JudgeWeb.Data;
using JudgeWeb.Data.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("[area]/contests/{cid}")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "CDS,Administrator")]
    [Produces("application/json")]
    public class ContestsController : ApiControllerBase
    {
        /// <summary>
        /// Get the given contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">Returns the given contest</response>
        [HttpGet]
        public ActionResult<ContestInfo> Info(int cid)
        {
            return new ContestInfo(Contest);
        }


        /// <summary>
        /// Change the start time of the given contest
        /// </summary>
        /// <param name="cid">The ID of the contest to change the start time for</param>
        /// <param name="start_time">The new start time of the contest</param>
        /// <response code="200">Contest start time changed successfully</response>
        /// <response code="403">Changing start time not allowed</response>
        [HttpPatch]
        public async Task<IActionResult> ChangeTime(int cid, DateTimeOffset? start_time)
        {
            var now = DateTimeOffset.Now;
            var newTime = start_time ?? (now + TimeSpan.FromSeconds(30));
            var oldtime = Contest.StartTime.Value;

            if (Contest.GetState(now) >= ContestState.Started)
                return StatusCode(403); // contest is started
            if (newTime < now + TimeSpan.FromSeconds(29.5))
                return StatusCode(403); // new start time is in the past or within 30s
            if (now + TimeSpan.FromSeconds(30) > oldtime)
                return StatusCode(403); // left time

            var delta = newTime - oldtime;
            var it = new Data.Contest
            {
                StartTime = newTime,
                EndTime = Contest.EndTime + delta,
                FreezeTime = Contest.FreezeTime + delta,
                UnfreezeTime = Contest.UnfreezeTime + delta,
            };

            await DbContext.UpdateContestAsync(cid, it,
                nameof(it.StartTime),
                nameof(it.EndTime),
                nameof(it.FreezeTime),
                nameof(it.UnfreezeTime));

            var newcont = await DbContext.GetContestAsync(Contest.ContestId);

            var userManager = HttpContext.RequestServices
                .GetRequiredService<UserManager>();

            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = "changed time",
                ContestId = Contest.ContestId,
                DataId = $"{Contest.ContestId}",
                DataType = AuditlogType.Contest,
                ExtraInfo = "via ccs-api",
                Time = DateTimeOffset.Now,
                UserName = userManager.GetUserName(User),
            });

            DbContext.Events.Add(
                new ContestInfo(newcont)
                    .ToEvent("update", Contest.ContestId));

            DbContext.Events.Add(
                new ContestTime(newcont)
                    .ToEvent("update", Contest.ContestId));

            await DbContext.SaveChangesAsync();

            return Ok();
        }


        /// <summary>
        /// Get the current contest state
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">The contest state</response>
        [HttpGet("[action]")]
        public ActionResult<ContestTime> State(int cid)
        {
            return new ContestTime(Contest);
        }


        /// <summary>
        /// Get the event feed for the given contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="since_id">Only get events after this event</param>
        /// <param name="types">Types to filter the event feed on</param>
        /// <param name="stream">Whether to stream the output or stop immediately</param>
        /// <response code="200">The events</response>
        [HttpGet("[action]")]
        [Produces("application/x-ndjson")]
        public IActionResult EventFeed(int cid,
            int? since_id = null, string types = null, bool stream = true)
        {
            var src = DbContext.Events
                .Where(e => e.ContestId == cid);

            if (!string.IsNullOrWhiteSpace(types))
            {
                var endpointTypes = types.Split(',');
                src = src.Where(e => endpointTypes.Contains(e.EndPointType));
            }

            return new EventFeedResult(src, stream, since_id ?? 0);
        }


        /// <summary>
        /// Get general status information
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">General status information for the given contest</response>
        [HttpGet("[action]")]
        public async Task<ActionResult<ServerStatus>> Status(int cid)
        {
            var query =
                from s in DbContext.Submissions
                where s.ContestId == cid
                join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                group new { s, j } by j.Status into gg
                select new { gg.Key, Count = gg.Count() };
            var g = await query.ToListAsync();

            return new ServerStatus
            {
                cid = cid,
                num_submissions = g.Sum(a => a.Count),
                num_queued = g
                    .Where(a => a.Key == Verdict.Pending)
                    .Select(a => a.Count)
                    .FirstOrDefault(),
                num_judging = g
                    .Where(a => a.Key == Verdict.Running)
                    .Select(a => a.Count)
                    .FirstOrDefault(),
            };
        }
    }
}
