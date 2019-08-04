using JudgeWeb.Areas.Judge.Services;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    [Area("Judge")]
    [Route("[area]/[controller]/[action]")]
    public class StatusController : Controller2
    {
        const string problemRole = "Administrator,Problem";

        private UserManager UserManager { get; }

        private JudgeContext Service { get; }

        public StatusController(JudgeContext ctx, UserManager jum)
        {
            Service = ctx;
            UserManager = jum;
        }

        [HttpGet("{id}/{full}")]
        [Authorize(Roles = problemRole)]
        public async Task<IActionResult> Rejudge(int id, string full)
        {
            var jid = await Service.Rejudge(id, full == "full");
            if (jid == -1) return NotFound();
            return RedirectToAction(nameof(View), new { gid = jid });
        }

        [HttpGet("{gid}")]
        [Authorize(Roles = problemRole)]
        public async Task<IActionResult> Activate(int gid)
        {
            var sid = await Service.ActivateJudging(gid, UserManager.GetUserName(User));

            if (sid == -1)
                return NotFound();
            else if (sid == int.MinValue)
                return BadRequest();
            return RedirectToAction("View", new { id = sid, gid });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> View(int id, int? gid)
        {
            var uid = UserManager.GetUserId(User);
            var model = await Service.ViewCode(id, gid, uid, User.IsInRoles(problemRole));
            if (model is null) return NotFound();

            if (User.IsInRoles(problemRole))
            {
                ViewBag.AllJudgings = await Service.GetJudgings(id);
            }

            return View(model);
        }

        [HttpGet("{pg?}")]
        public async Task<IActionResult> List(int pg = 1,
            int? pid = null, int? status = null, int? uid = null, string lang = null)
        {
            var page = pg;
            if (page == 0) page = 1;
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
            return View(await Service.GetStatusList(page, pid, status, uid, lang));
        }

        [HttpGet("{jid}/{rid}/{type}")]
        [Authorize(Roles = problemRole)]
        public IActionResult RunDetails(int jid, int rid, string type)
        {
            var io = HttpContext.RequestServices.GetRequiredService<IFileRepository>();
            io.SetContext("Runs");
            if (!io.ExistPart($"j{jid}", $"r{rid}.{type}")) return NotFound();
            return ContentFile($"Runs/j{jid}/r{rid}.{type}", "application/octet-stream", $"j{jid}_r{rid}.{type}");
        }
    }
}