using EFCore.BulkExtensions;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Polygon.Services;
using JudgeWeb.Data;
using JudgeWeb.Features;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[controller]")]
    public class JuryController : JuryControllerBase
    {
        private async Task UpdateContestAsync(Data.Contest template, params string[] contents)
        {
            await DbContext.UpdateContestAsync(Contest.ContestId, template, contents);

            var newcont = await DbContext.GetContestAsync(Contest.ContestId);

            DbContext.Events.Add(
                new Data.Api.ContestInfo(newcont)
                    .ToEvent("update", Contest.ContestId));

            DbContext.Events.Add(
                new Data.Api.ContestTime(newcont)
                    .ToEvent("update", Contest.ContestId));

            InternalLog(AuditlogType.Contest, $"{Contest.ContestId}", "updated");

            await DbContext.SaveChangesAsync();
        }


        private IActionResult GoBackHome(string str)
        {
            StatusMessage = str;
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]")]
        public IActionResult Print() => PrintView();


        [HttpPost("[action]")]
        public Task<IActionResult> Print(int cid, AddPrintModel model) => PrintDo(cid, model);


        [HttpGet]
        public IActionResult Home()
        {
            return View();
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetEventFeed(int cid)
        {
            await DbContext.Events
                .Where(e => e.ContestId == cid)
                .BatchDeleteAsync();

            // contests
            DbContext.Events.AddCreate(cid, new Data.Api.ContestInfo(Contest));
            await DbContext.SaveChangesAsync();

            // judgement-types
            DbContext.Events.AddCreate(cid, Data.Api.JudgementType.Defaults);
            await DbContext.SaveChangesAsync();

            // languages
            DbContext.Events.AddCreate(cid,
                Languages.Values.Select(l => new Data.Api.ContestLanguage(l)));
            await DbContext.SaveChangesAsync();

            // groups
            var groups = await DbContext.ListTeamCategoryAsync(cid, null);
            DbContext.Events.AddCreate(cid,
                groups.Select(c => new Data.Api.ContestGroup(c)));
            await DbContext.SaveChangesAsync();

            // organizations
            var cts = await DbContext.ListTeamAffiliationAsync(cid, false);
            DbContext.Events.AddCreate(cid,
                cts.Select(a => new Data.Api.ContestOrganization(a)));
            await DbContext.SaveChangesAsync();

            // problems
            DbContext.Events.AddCreate(cid,
                Problems.Select(a => new Data.Api.ContestProblem2(a)));
            await DbContext.SaveChangesAsync();

            // teams
            DbContext.Events.AddCreate(cid, await (
                from t in DbContext.Teams
                where t.ContestId == cid && t.Status == 1
                join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                select new Data.Api.ContestTeam(t, a))
                .ToListAsync());
            await DbContext.SaveChangesAsync();

            // clarifications
            DbContext.Events.AddCreate(cid,
                await DbContext.Clarifications
                    .Where(c => c.ContestId == cid)
                    .Select(c => new Data.Api.ContestClarification(c, Contest.StartTime.Value))
                    .ToListAsync());
            await DbContext.SaveChangesAsync();

            InternalLog(AuditlogType.Scoreboard, null, "reset event");
            await DbContext.SaveChangesAsync();

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
                routeValues: new Dictionary<string, string> { ["cid"] = Contest.ContestId.ToString() },
                type: MessageType.Warning);
        }


        [HttpGet("[action]")]
        public Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "") =>
            ScoreboardView(false, true, clear == "clear", affiliations, categories);


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
            var counts = await DbContext.Auditlogs
                .Where(l => l.ContestId == cid)
                .CachedCountAsync($"`c{cid}`logcount", TimeSpan.FromSeconds(15));
            int totalPage = (counts + 999) / 1000;
            if (page > totalPage) return NotFound();

            var model = await DbContext.Auditlogs
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

            await UpdateContestAsync(newcont,
                nameof(Contest.StartTime),
                nameof(Contest.EndTime),
                nameof(Contest.FreezeTime),
                nameof(Contest.UnfreezeTime));

            InternalLog(AuditlogType.Contest, $"{Contest.ContestId}", "changed time");
            await DbContext.SaveChangesAsync();
            return GoBackHome("Contest state changed.");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Balloon(int cid)
        {
            var balloonQuery =
                from b in DbContext.Balloon
                join s in DbContext.Submissions on b.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                join t in DbContext.Teams on new { s.ContestId, TeamId = s.Author } equals new { t.ContestId, t.TeamId }
                join c in DbContext.TeamCategories on t.CategoryId equals c.CategoryId
                select new Balloon(b, s.ProblemId, s.Author, t.TeamName, t.Location, s.Time, c.Name, c.SortOrder);

            var balloons = await balloonQuery.ToListAsync();
            balloons.Sort((b1, b2) => b1.Time.CompareTo(b2.Time));
            foreach (var g in balloons
                .OrderBy(b => b.Time)
                .GroupBy(b => new { b.ProblemId, b.SortOrder }))
            {
                var fb = true;
                foreach (var item in g)
                {
                    item.FirstToSolve = fb;
                    fb = false;
                    var p = Problems.Find(item.ProblemId);
                    item.BalloonColor = p.Color;
                    item.ProblemShortName = p.ShortName;
                }
            }

            return View(balloons);
        }


        [HttpGet("balloon/{bid}/set-done")]
        public async Task<IActionResult> BalloonSetDone(int cid, int bid)
        {
            var bquery =
                from b in DbContext.Balloon
                where b.Id == bid
                join s in DbContext.Submissions on b.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                select b.Id;

            await DbContext.Balloon
                .Where(b => bquery.Contains(b.Id))
                .BatchUpdateAsync(b => new Balloon { Done = true });

            return RedirectToAction(nameof(Balloon));
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        [Authorize(Roles = "Administrator")]
        public IActionResult Assign() => Window(new JuryAssignModel());


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Assign(int cid, JuryAssignModel model)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                StatusMessage = "Error user not found.";
            }
            else
            {
                var result = await UserManager
                    .AddToRoleAsync(user, $"JuryOfContest{cid}");

                if (result.Succeeded)
                {
                    await UserManager.SlideExpirationAsync(user);
                    StatusMessage = $"Jury role of user {user.UserName} assigned.";
                    InternalLog(AuditlogType.User, $"{user.Id}", "assigned jury");
                    await DbContext.SaveChangesAsync();
                }
                else
                    StatusMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
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
                area: "Contest", ctrl: "Jury", act: "Unassign",
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
            {
                StatusMessage = $"Jury role of user {user.UserName} unassigned.";
                await UserManager.SlideExpirationAsync(user);
                InternalLog(AuditlogType.User, $"{uid}", "unassigned jury");
                await DbContext.SaveChangesAsync();
            }
            else
                StatusMessage = "Error " + string.Join('\n', result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Home));
        }


        [HttpGet("[action]/{userName?}")]
        public async Task<IActionResult> TestUser(string userName)
        {
            if (userName != null)
            {
                var user = await UserManager.FindByNameAsync(userName);
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
            ViewBag.Categories = await DbContext.ListTeamCategoryAsync(cid);

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
                UsePrintings = Contest.PrintingAvaliable,
                UseBalloon = Contest.BalloonAvaliable,
            });
        }
        

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cid, JuryEditModel model)
        {
            // check the category id
            var cates = await DbContext.ListTeamCategoryAsync(cid);
            if (model.DefaultCategory != 0 && !cates.Any(c => c.CategoryId == model.DefaultCategory))
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

            var update = new Data.Contest
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
                RegisterDefaultCategory = model.DefaultCategory,
                BalloonAvaliable = model.UseBalloon,
                PrintingAvaliable = model.UsePrintings,
            };

            await UpdateContestAsync(update,
                nameof(update.ShortName),
                nameof(update.Name),
                nameof(update.RankingStrategy),
                nameof(update.IsPublic),
                nameof(update.GoldMedal),
                nameof(update.SilverMedal),
                nameof(update.BronzeMedal),
                nameof(update.StartTime),
                nameof(update.FreezeTime),
                nameof(update.EndTime),
                nameof(update.UnfreezeTime),
                nameof(update.RegisterDefaultCategory),
                nameof(update.PrintingAvaliable),
                nameof(update.BalloonAvaliable));

            InternalLog(AuditlogType.Contest, $"{cid}", "updated", "via edit-page");
            await DbContext.SaveChangesAsync();

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
        public async Task<IActionResult> RefreshCache(int cid,
            [FromServices] IScoreboardService scoreboardService)
        {
            scoreboardService.RefreshCache(Contest, DateTimeOffset.Now);
            StatusMessage = "Scoreboard cache will be refreshed in minutes...";
            InternalLog(AuditlogType.Scoreboard, null, "refresh cache");
            await DbContext.SaveChangesAsync();
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
                area: "Contest", ctrl: "Jury", act: "RefreshCache",
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

            var document = md.Parse(model.Markdown);
            await io.WritePartAsync($"c{cid}", "readme.html", md.RenderAsHtml(document));

            InternalLog(AuditlogType.Contest, $"{cid}", "updated", "description");
            await DbContext.SaveChangesAsync();
            return View(model);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateStatement(int cid,
            [FromServices] IProblemViewProvider generator)
        {
            var probs =
                from cp in DbContext.ContestProblem
                where cp.ContestId == cid
                join p in DbContext.Problems on cp.ProblemId equals p.ProblemId
                select new { cp.ShortName, p };
            await probs.ToListAsync();

            var memstream = new MemoryStream();
            using (var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true))
            {
                zip.CreateEntryFromFile("wwwroot/static/olymp.sty", "olymp.sty");
                var documentStart = await System.IO.File.ReadAllTextAsync("wwwroot/static/contest.tex-begin");
                var documentBuilder = new System.Text.StringBuilder(documentStart)
                    .Append("\\begin {document}\n\n")
                    .Append("\\contest\n{")
                    .Append(Contest.Name)
                    .Append("}%\n{Location}%\n{")
                    .Append("Monday, January 13, 2020")
                    .Append("}%\n\n")
                    .Append("\\binoppenalty=10000\n")
                    .Append("\\relpenalty=10000\n\n")
                    .Append("\\renewcommand{\\t}{\\texttt}\n\n");

                foreach (var item in probs)
                {
                    var folderPrefix = $"{item.ShortName}/";
                    var statement = await generator
                        .LoadStatement(item.p, DbContext.Testcases);
                    generator.BuildLatex(zip, statement, folderPrefix);

                    documentBuilder
                        .Append("\\graphicspath{{./")
                        .Append(item.ShortName)
                        .Append("/}}\n\\import{./")
                        .Append(item.ShortName)
                        .Append("/}{./problem.tex}\n\n");
                }

                documentBuilder.Append("\\end{document}\n\n");
                zip.CreateEntryFromString(documentBuilder.ToString(), "contest.tex");
            }

            memstream.Position = 0;
            return File(memstream, "application/zip", $"c{cid}-statements.zip");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Updates(int cid)
        {
            var clarifications = await DbContext.Clarifications
                .Where(c => c.ContestId == cid && !c.Answered)
                .CachedCountAsync($"`c{cid}`clar`una_count", TimeSpan.FromSeconds(10));
            var teams = await DbContext.Teams
                .Where(t => t.Status == 0 && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`teams`pending_count", TimeSpan.FromSeconds(10));
            var rejudgings = await DbContext.Rejudges
                .Where(t => t.Applied == null && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`rejs`pending_count", TimeSpan.FromSeconds(10));
            return Json(new { clarifications, teams, rejudgings });
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
