using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Contests.ApiModels;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using ContestState = JudgeWeb.Data.ContestState;

namespace JudgeWeb.Areas.Api.Controllers
{
    [Area("Api")]
    [Route("[area]/contests/{cid}")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "CDS,Administrator")]
    [Produces("application/json")]
    public class ContestsController : ApiControllerBase
    {
        public IContestStore Store { get; }
        public ContestsController(IContestStore store) => Store = store;


        /// <summary>
        /// Get the given contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">Returns the given contest</response>
        [HttpGet]
        public ActionResult<Contest> Info(int cid)
        {
            return new Contest(Contest);
        }


        /// <summary>
        /// Change the start time of the given contest
        /// </summary>
        /// <param name="cid">The ID of the contest to change the start time for</param>
        /// <param name="start_time">The new start time of the contest</param>
        /// <param name="notifier"></param>
        /// <response code="200">Contest start time changed successfully</response>
        /// <response code="403">Changing start time not allowed</response>
        [HttpPatch]
        [AuditPoint(AuditlogType.Contest)]
        public async Task<IActionResult> ChangeTime(
            int cid, DateTimeOffset? start_time,
            [FromServices] IContestEventNotifier notifier)
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

            await Store.UpdateAsync(cid, c => new Data.Contest
            {
                StartTime = newTime,
                EndTime = Contest.EndTime + delta,
                FreezeTime = Contest.FreezeTime + delta,
                UnfreezeTime = Contest.UnfreezeTime + delta,
            });

            var newcont = await Store.FindAsync(Contest.ContestId);
            await HttpContext.AuditAsync("changed time", $"{Contest.ContestId}", "via ccs-api");
            await notifier.Update(cid, newcont);
            return Ok();
        }


        /// <summary>
        /// Get the current contest state
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">The contest state</response>
        [HttpGet("[action]")]
        public ActionResult<State> State(int cid)
        {
            return new State(Contest);
        }


        /// <summary>
        /// Get the event feed for the given contest
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="notifier"></param>
        /// <param name="since_id">Only get events after this event</param>
        /// <param name="types">Types to filter the event feed on</param>
        /// <param name="stream">Whether to stream the output or stop immediately</param>
        /// <response code="200">The events</response>
        [HttpGet("[action]")]
        [Produces("application/x-ndjson")]
        public IActionResult EventFeed(int cid,
            [FromServices] IContestEventNotifier notifier,
            int? since_id = null, string types = null, bool stream = true)
        {
            var src = notifier.Query(cid);

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
        /// <param name="judgings"></param>
        /// <response code="200">General status information for the given contest</response>
        [HttpGet("[action]")]
        public async Task<ActionResult<ServerStatus>> Status(int cid,
            [FromServices] IJudgingStore judgings)
        {
            var stats = await judgings.GetJudgeQueueAsync(cid);
            return stats.SingleOrDefault() ?? new ServerStatus { cid = cid };
        }
    }
}
