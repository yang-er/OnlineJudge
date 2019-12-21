using JudgeWeb.Areas.Judge.Providers;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public class StatusController : Controller2
    {
        [HttpGet("{id}/{full}")]
        public async Task<IActionResult> Rejudge(int id, string full,
            [FromServices] JudgingManager judgingManager)
        {
            if (!await judgingManager.WithUser(User)
                .CheckAvaliabilityForAdminAsync(id))
                return Forbid();

            var jid = await judgingManager.WithUser(User)
                .RejudgeSubmissionAsync(id, full == "full");

            if (jid == -1) return NotFound();
            return RedirectToAction(nameof(View), new { gid = jid });
        }

        [HttpGet("{gid}")]
        public async Task<IActionResult> Activate(int gid,
            [FromServices] JudgingManager judgingManager)
        {
            if (!await judgingManager.WithUser(User)
                .CheckAvaliabilityForAdminByJudgingAsync(gid))
                return Forbid();

            var sid = await judgingManager.WithUser(User)
                .ActivateByIdAsync(gid);

            if (sid == -1) return NotFound();
            if (sid == int.MinValue) return BadRequest();
            return RedirectToAction("View", new { id = sid, gid });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> View(int id, int? gid,
            [FromServices] StatusProvider statusProvider,
            [FromServices] JudgingManager judgingManager)
        {
            var model = await statusProvider
                .FetchCodeViewAsync(id, gid, User);
            if (model is null) return Forbid();

            bool probRole = User.IsInRoles("Administrator,AuthorOfProblem" + model.ProblemId);

            if (probRole)
                ViewBag.AllJudgings = await judgingManager.WithUser(User)
                    .EnumerateBySubmissionAsync(id);
            ViewBag.ProbRole = probRole;

            return View(model);
        }

        [HttpGet("{pg?}")]
        public async Task<IActionResult> List(
            [FromServices] StatusProvider statusProvider, 
            int pg = 1, int? pid = null, int? status = null,
            int? uid = null, string lang = null)
        {
            var page = pg == 0 ? 1 : pg;
            ViewData["Page"] = page;

            var filter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (pid.HasValue)
                filter.Add(nameof(pid), pid.Value.ToString());
            if (status.HasValue)
                filter.Add(nameof(status), status.Value.ToString());
            if (uid.HasValue)
                filter.Add(nameof(uid), uid.Value.ToString());
            if (lang != null)
                filter.Add(nameof(lang), lang);
            
            ViewBag.Filter = filter;

            var model = await statusProvider
                .FetchListAsync(page, pid, status, uid, lang);
            return View(model);
        }
    }
}