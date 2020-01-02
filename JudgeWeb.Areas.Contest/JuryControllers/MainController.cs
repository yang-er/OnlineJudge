using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury")]
    public class JuryMainController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Home(int cid)
        {
            ViewBag.Problems = await Service.GetProblemsAsync(cid);
            return View();
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            var board = await Service.FindScoreboardAsync(cid, false, true);

            if (clear == "clear")
            {
                affiliations = System.Array.Empty<int>();
                categories = System.Array.Empty<int>();
            }

            if (affiliations.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => affiliations.Contains(t.Team.AffiliationId));
                ViewData["Filter_affiliations"] = affiliations.ToHashSet();
            }

            if (categories.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => categories.Contains(t.Team.CategoryId));
                ViewData["Filter_categories"] = categories.ToHashSet();
            }

            return View(board);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Statistics(int cid,
            [FromServices] SubmissionManager submitMgr)
        {
            var query =
                from s in submitMgr.Submissions
                where s.ContestId == cid
                join g in submitMgr.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { g.SubmissionId, g.Active }
                select new { s.Time, g.Status };
            var result = await query.CachedToListAsync(
                $"`c{cid}`statistics", TimeSpan.FromMinutes(1));

            return View(result.Select(a => (a.Status, a.Time)));
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Auditlog(int cid, int page = 1)
        {
            if (page <= 0) return NotFound();
            var counts = await Service.Auditlogs
                .Where(l => l.ContestId == cid)
                .CachedCountAsync($"`c{cid}`logcount", TimeSpan.FromSeconds(15));
            int totalPage = (counts + 999) / 1000;
            if (page > totalPage) return NotFound();

            var model = await Service.Auditlogs
                .Where(l => l.ContestId == cid)
                .OrderByDescending(l => l.LogId)
                .Skip((page - 1) * 1000).Take(1000)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPage = totalPage;
            return View(model);
        }


        [HttpPost("[action]/{target}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ChangeState(string target)
        {
            var result = await Service.ChangeStateAsync(Contest, target, DateTimeOffset.Now);
            DisplayMessage = (result.IsValid ? "" : "Error: ") + result.Message;
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult Assign()
        {
            return Window(new JuryAssignModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Assign(int cid, JuryAssignModel model)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                DisplayMessage = "Error user not found.";
            }
            else
            {
                var result = await UserManager
                    .AddToRoleAsync(user, $"JuryOfContest{cid}");

                if (result.Succeeded)
                    DisplayMessage = $"Jury role of user {user.UserName} assigned.";
                else
                    DisplayMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]/{uid}")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unassign(int uid)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();

            return AskPost(
                title: "Unassign jury",
                message: $"Do you want to unassign jury {user.UserName} (u{uid})?",
                area: "Contest", ctrl: "JuryMain", act: "Unassign",
                routeValues: new Dictionary<string, string> { ["uid"] = $"{uid}" },
                type: MessageType.Danger);
        }


        [HttpPost("[action]/{uid}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Unassign(int cid, int uid)
        {
            var user = await UserManager.FindByIdAsync(uid.ToString());
            if (user == null) return NotFound();
            var result = await UserManager
                .RemoveFromRoleAsync(user, $"JuryOfContest{cid}");

            if (result.Succeeded)
                DisplayMessage = $"Jury role of user {user.UserName} unassigned.";
            else
                DisplayMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Edit(int cid)
        {
            ViewBag.Categories = await Service.ListTeamCategoryAsync(cid);

            var startTime = Contest.StartTime?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "";
            var startDateTime = Contest.StartTime ?? DateTimeOffset.UnixEpoch;
            var stopTime = (Contest.EndTime - startDateTime)?.ToDeltaString() ?? "";
            var unfTime = (Contest.UnfreezeTime - startDateTime)?.ToDeltaString() ?? "";
            var freTime = (Contest.FreezeTime - startDateTime)?.ToDeltaString() ?? "";

            return View(new JuryEditModel
            {
                ContestId = Contest.ContestId,
                FreezeTime = freTime,
                Name = Contest.Name,
                ShortName = Contest.ShortName,
                RankingStrategy = Contest.RankingStrategy,
                StartTime = startTime,
                StopTime = stopTime,
                UnfreezeTime = unfTime,
                DefaultCategory = Contest.RegisterDefaultCategory,
                BronzeMedal = Contest.BronzeMedal,
                GoldenMedal = Contest.GoldMedal,
                SilverMedal = Contest.SilverMedal,
                IsPublic = Contest.IsPublic,
            });
        }
        

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cid, JuryEditModel model)
        {
            // check the category id
            var cates = await Service.ListTeamCategoryAsync(cid);
            if (model.DefaultCategory != 0 && cates.Any(c => c.CategoryId == model.DefaultCategory))
                ModelState.AddModelError("xys::nocat", "No corresponding category found.");

            // check time sequence
            if (!string.IsNullOrEmpty(model.StartTime) && string.IsNullOrEmpty(model.StopTime))
                ModelState.AddModelError("xys::startstop", "No stop time when start time filled.");

            bool contestTimeChanged = false;
            DateTimeOffset @base;
            DateTimeOffset? startTime, endTime, freezeTime, unfreezeTime;

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

            if (string.IsNullOrWhiteSpace(model.StopTime))
            {
                endTime = null;
            }
            else
            {
                model.StopTime.TryParseAsTimeSpan(out var ts);
                endTime = @base + ts.Value;
            }

            if (string.IsNullOrWhiteSpace(model.FreezeTime))
            {
                freezeTime = null;
            }
            else
            {
                model.FreezeTime.TryParseAsTimeSpan(out var ts);
                freezeTime = @base + ts.Value;
            }

            if (string.IsNullOrWhiteSpace(model.UnfreezeTime))
            {
                unfreezeTime = null;
            }
            else
            {
                model.UnfreezeTime.TryParseAsTimeSpan(out var ts);
                unfreezeTime = @base + ts.Value;
            }

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

            await Service.UpdateContestAsync(cid, c => new Data.Contest
            {
                ShortName = model.ShortName,
                Name = model.Name,
                RankingStrategy = model.RankingStrategy,
                IsPublic = model.IsPublic,
                GoldMedal = model.GoldenMedal,
                SilverMedal = model.SilverMedal,
                BronzeMedal = model.BronzeMedal,
                StartTime = startTime,
                FreezeTime = freezeTime,
                EndTime = endTime,
                UnfreezeTime = unfreezeTime,
                RegisterDefaultCategory = model.DefaultCategory
            });

            DisplayMessage = "Contest updated successfully.";
            if (contestTimeChanged)
                DisplayMessage += " Scoreboard cache will be refreshed later.";
            return RedirectToAction(nameof(Home));
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult RefreshCache(int cid, bool post = true)
        {
            Service.RefreshScoreboardCache(cid);
            DisplayMessage = "Scoreboard cache will be refreshed in minutes...";
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult RefreshCache(int cid)
        {
            return AskPost(
                title: "Refresh scoreboard cache",
                message: "Do you want to refresh scoreboard cache? This will lead to a heavy database load in minutes.",
                area: "Contest", ctrl: "JuryMain", act: "RefreshCache",
                routeValues: new Dictionary<string, string> { ["cid"] = $"{cid}" },
                type: MessageType.Warning);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Description(int cid,
            [FromServices] IFileRepository io)
        {
            io.SetContext("Problems");
            var content = await io.ReadPartAsync($"c{cid}", "readme.md");
            return View(new JuryMarkdownModel { Markdown = content });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Description(
            int cid, JuryMarkdownModel model,
            [FromServices] IFileRepository io,
            [FromServices] IMarkdownService md)
        {
            io.SetContext("Problems");
            model.Markdown = model.Markdown ?? "";
            await io.WritePartAsync($"c{cid}", "readme.md", model.Markdown);
            await io.WritePartAsync($"c{cid}", "readme.html", md.Render(model.Markdown));
            return View(model);
        }


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
