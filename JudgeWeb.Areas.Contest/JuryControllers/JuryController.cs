using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[controller]")]
    public class JuryController : JuryControllerBase
    {
        private IContestStore Store => Facade.Contests;


        [HttpGet("[action]")]
        public IActionResult Print() => PrintView();


        [HttpPost("[action]")]
        [AuditPoint(AuditlogType.Printing)]
        public Task<IActionResult> Print(int cid, AddPrintModel model) => PrintDo(cid, model);


        [HttpGet]
        public IActionResult Home()
        {
            return View();
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Scoreboard)]
        public async Task<IActionResult> ResetEventFeed(int cid)
        {
            await Notifier.ResetAsync(cid);
            await HttpContext.AuditAsync("reset event", null);
            StatusMessage = "Event feed reset.";
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult ResetEventFeed()
        {
            return AskPost(
                title: "Reset event feed",
                message: "After reseting event feed, you can connect to CDS. " +
                    "But you shouldn't change any settings more, and you should use it only before contest start. " +
                    "Or it will lead to event missing. Are you sure?",
                area: "Contest", ctrl: "Jury", act: "ResetEventFeed",
                routeValues: new { cid = Contest.ContestId },
                type: MessageType.Warning);
        }


        [HttpGet("[action]")]
        public Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "") =>
            ScoreboardView(false, true, clear == "clear", affiliations, categories);


        [HttpGet("[action]")]
        public async Task<IActionResult> Auditlog(
            [FromServices] IAuditlogger logger,
            int cid, int page = 1)
        {
            if (page <= 0) return NotFound();
            var (model, totalPage) = await logger.ViewLogsAsync(cid, page, 1000);
            ViewBag.Page = page;
            ViewBag.TotalPage = totalPage;
            return View(model);
        }


        [HttpPost("[action]/{target}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        [AuditPoint(AuditlogType.Contest)]
        public async Task<IActionResult> ChangeState(string target)
        {
            var now = DateTimeOffset.Now;

            var newcont = new Data.Contest
            {
                StartTime = Contest.StartTime,
                EndTime = Contest.EndTime,
                FreezeTime = Contest.FreezeTime,
                UnfreezeTime = Contest.UnfreezeTime,
            };

            var state = newcont.GetState(now);

            if (target == "startnow")
            {
                if (!newcont.EndTime.HasValue)
                    return GoBackHome("Error no end time specified.");
                now += TimeSpan.FromSeconds(30);
                DateTimeOffset old;

                if (newcont.StartTime.HasValue)
                {
                    // from scheduled to start
                    if (newcont.StartTime.Value < now)
                        return GoBackHome("Error starting contest for the remaining time is less than 30 secs.");
                    old = newcont.StartTime.Value;
                }
                else
                {
                    // from delay to start
                    old = DateTimeOffset.UnixEpoch;
                }

                newcont.StartTime = now;
                newcont.EndTime = now + (newcont.EndTime.Value - old);
                if (newcont.FreezeTime.HasValue)
                    newcont.FreezeTime = now + (newcont.FreezeTime.Value - old);
                if (newcont.UnfreezeTime.HasValue)
                    newcont.UnfreezeTime = now + (newcont.UnfreezeTime.Value - old);
            }
            else if (target == "freeze")
            {
                if (state != ContestState.Started)
                    return GoBackHome("Error contest is not started.");
                newcont.FreezeTime = now;
            }
            else if (target == "endnow")
            {
                if (state != ContestState.Started && state != ContestState.Frozen)
                    return GoBackHome("Error contest has not started or has ended.");
                newcont.EndTime = now;

                if (newcont.FreezeTime.HasValue && newcont.FreezeTime.Value > now)
                    newcont.FreezeTime = now;
            }
            else if (target == "unfreeze")
            {
                if (state != ContestState.Ended)
                    return GoBackHome("Error contest has not ended.");
                newcont.UnfreezeTime = now;
            }
            else if (target == "delay")
            {
                if (state != ContestState.ScheduledToStart)
                    return GoBackHome("Error contest has been started.");

                var old = newcont.StartTime.Value;
                newcont.StartTime = null;
                if (newcont.EndTime.HasValue)
                    newcont.EndTime = DateTimeOffset.UnixEpoch + (newcont.EndTime.Value - old);
                if (newcont.FreezeTime.HasValue)
                    newcont.FreezeTime = DateTimeOffset.UnixEpoch + (newcont.FreezeTime.Value - old);
                if (newcont.UnfreezeTime.HasValue)
                    newcont.UnfreezeTime = DateTimeOffset.UnixEpoch + (newcont.UnfreezeTime.Value - old);
            }

            await Store.UpdateAsync(Contest.ContestId,
                c => new Data.Contest
                {
                    StartTime = newcont.StartTime,
                    EndTime = newcont.EndTime,
                    FreezeTime = newcont.FreezeTime,
                    UnfreezeTime = newcont.UnfreezeTime,
                });

            
            await HttpContext.AuditAsync("changed time", $"{Contest.ContestId}");
            return GoBackHome("Contest state changed.");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Balloon(int cid,
            [FromServices] IBalloonStore store)
        {
            var model = await store.ListAsync(cid, Problems);
            return View(model);
        }


        [HttpGet("balloon/{bid}/set-done")]
        public async Task<IActionResult> BalloonSetDone(int cid, int bid,
            [FromServices] IBalloonStore store)
        {
            await store.SetDoneAsync(cid, bid);
            return RedirectToAction(nameof(Balloon));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult Assign() => Window(new JuryAssignModel());


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        [AuditPoint(AuditlogType.User)]
        public async Task<IActionResult> Assign(
            int cid, JuryAssignModel model,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                StatusMessage = "Error user not found.";
            }
            else
            {
                var result = await userManager.AddToRoleAsync(user, $"JuryOfContest{cid}");

                if (result.Succeeded)
                {
                    StatusMessage = $"Jury role of user {user.UserName} assigned.";
                    await HttpContext.AuditAsync("assigned jury", $"{user.Id}", $"c{cid}");
                }
                else
                {
                    StatusMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
                }
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]/{uid}")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unassign(int uid,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();

            return AskPost(
                title: "Unassign jury",
                message: $"Do you want to unassign jury {user.UserName} (u{uid})?",
                area: "Contest", ctrl: "Jury", act: "Unassign",
                routeValues: new { uid, cid = Contest.ContestId },
                type: MessageType.Danger);
        }


        [HttpPost("[action]/{uid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        [AuditPoint(AuditlogType.User)]
        public async Task<IActionResult> Unassign(int cid, int uid,
            [FromServices] UserManager userManager)
        {
            var user = await userManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();
            var result = await userManager.RemoveFromRoleAsync(user, $"JuryOfContest{cid}");

            if (result.Succeeded)
            {
                StatusMessage = $"Jury role of user {user.UserName} unassigned.";
                await HttpContext.AuditAsync("unassigned jury", $"{uid}", $"c{cid}");
            }
            else
            {
                StatusMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]/{userName?}")]
        public async Task<IActionResult> TestUser(string userName,
            [FromServices] UserManager userManager)
        {
            if (userName != null)
            {
                var user = await userManager.FindByNameAsync(userName);
                if (user == null)
                    return Content("No such user.", "text/html");
                return Content("", "text/html");
            }
            else
            {
                return Content("Please enter the username.", "text/html");
            }
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Edit(int cid)
        {
            ViewBag.Categories = await Facade.Teams.ListCategoryAsync(cid);
            return View(new JuryEditModel(Contest));
        }
        

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cid, JuryEditModel model)
        {
            // check the category id
            var cates = await Facade.Teams.ListCategoryAsync(cid);
            if (model.DefaultCategory != 0 && !cates.Any(c => c.CategoryId == model.DefaultCategory))
                ModelState.AddModelError("xys::nocat", "No corresponding category found.");

            // check time sequence
            if (!string.IsNullOrEmpty(model.StartTime) && string.IsNullOrEmpty(model.StopTime))
                ModelState.AddModelError("xys::startstop", "No stop time when start time filled.");

            bool contestTimeChanged = false;
            DateTimeOffset @base;
            DateTimeOffset? startTime, endTime = null, freezeTime = null, unfreezeTime = null;

            if (string.IsNullOrEmpty(model.StartTime))
            {
                @base = DateTimeOffset.UnixEpoch;
                startTime = null;
            }
            else
            {
                @base = DateTimeOffset.Parse(model.StartTime);
                startTime = @base;
            }

            if (!string.IsNullOrWhiteSpace(model.StopTime)
                && model.StopTime.TryParseAsTimeSpan(out var ts1))
                endTime = @base + ts1.Value;

            if (!string.IsNullOrWhiteSpace(model.FreezeTime)
                && model.FreezeTime.TryParseAsTimeSpan(out var ts2))
                freezeTime = @base + ts2.Value;

            if (!string.IsNullOrWhiteSpace(model.UnfreezeTime)
                && model.UnfreezeTime.TryParseAsTimeSpan(out var ts3))
                unfreezeTime = @base + ts3.Value;

            if (!InSequence(startTime, freezeTime, endTime, unfreezeTime))
                ModelState.AddModelError("xys::time", "Time sequence is wrong.");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = cates;
                return View(model);
            }

            var cst = Contest;

            if (startTime != cst.StartTime
                || endTime != cst.EndTime
                || freezeTime != cst.FreezeTime
                || unfreezeTime != cst.UnfreezeTime)
                contestTimeChanged = true;

            await Store.UpdateAsync(cid, c => new Data.Contest
            {
                ShortName = model.ShortName,
                Name = model.Name,
                RankingStrategy = model.RankingStrategy,
                IsPublic = model.IsPublic,
                StartTime = startTime,
                FreezeTime = freezeTime,
                EndTime = endTime,
                UnfreezeTime = unfreezeTime,
                RegisterDefaultCategory = model.DefaultCategory,
                BalloonAvaliable = model.UseBalloon,
                PrintingAvaliable = model.UsePrintings,
                StatusAvaliable = model.StatusAvailable,
            });

            await HttpContext.AuditAsync("updated", $"{cid}", "via edit-page");

            StatusMessage = "Contest updated successfully.";
            if (contestTimeChanged)
            {
                StatusMessage += " Scoreboard cache will be refreshed later.";
                HttpContext.RequestServices
                    .GetRequiredService<IScoreboardService>()
                    
                    .RefreshCache(Contest, DateTimeOffset.Now);
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        [AuditPoint(AuditlogType.Scoreboard)]
        public async Task<IActionResult> RefreshCache(int cid,
            [FromServices] IScoreboardService scoreboardService)
        {
            scoreboardService.RefreshCache(Contest, DateTimeOffset.Now);
            StatusMessage = "Scoreboard cache will be refreshed in minutes...";
            await HttpContext.AuditAsync("refresh cache", null);
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult RefreshCache(int cid)
        {
            return AskPost(
                title: "Refresh scoreboard cache",
                message: "Do you want to refresh scoreboard cache? " +
                    "This will lead to a heavy database load in minutes.",
                area: "Contest", ctrl: "Jury", act: "RefreshCache",
                routeValues: new { cid },
                type: MessageType.Warning);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Description(int cid,
            [FromServices] IProblemFileRepository io)
        {
            var fileInfo = io.GetFileInfo($"c{cid}/readme.md");
            var content = await fileInfo.ReadAsync();
            return View(new JuryMarkdownModel { Markdown = content });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Description(
            int cid, JuryMarkdownModel model,
            [FromServices] IProblemFileRepository io,
            [FromServices] IMarkdownService md)
        {
            model.Markdown ??= "";
            await io.WriteStringAsync($"c{cid}/readme.md", model.Markdown);

            var document = md.Parse(model.Markdown);
            await io.WriteStringAsync($"c{cid}/readme.html", md.RenderAsHtml(document));

            await HttpContext.AuditAsync("updated", $"{cid}", "description");
            return View(model);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Updates(int cid,
            [FromServices] IClarificationStore clars,
            [FromServices] ITeamStore teams,
            [FromServices] IRejudgingStore rejs)
        {
            return Json(new
            {
                clarifications = await clars.GetJuryStatusAsync(cid),
                teams = await teams.GetJuryStatusAsync(cid),
                rejudgings = await rejs.GetJuryStatusAsync(cid)
            });
        }


        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => ExplicitNotFound();
        [Route("[action]")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();


        private bool InSequence(params DateTimeOffset?[] dateTimes)
        {
            if (dateTimes.Length == 0) return true;
            var item = dateTimes[0];

            for (int i = 1; i < dateTimes.Length; i++)
            {
                if (dateTimes[i].HasValue)
                {
                    if (item.HasValue && item.Value > dateTimes[i].Value)
                        return false;
                    item = dateTimes[i];
                }
            }

            return true;
        }
    }
}
