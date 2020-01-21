using JudgeWeb.Data;
using JudgeWeb.Features.Scoreboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClarificationCategory = JudgeWeb.Data.Clarification.TargetCategory;

namespace JudgeWeb.Areas.Contest.Controllers
{
    public class Controller3 : Controller2
    {
        protected AppDbContext DbContext { get; private set; }

        protected UserManager UserManager { get; private set; }

        private static IScoreboard[] Scoreboards => Instance.Scoreboards;

        [TempData]
        public string StatusMessage { get; set; }

        [ViewData]
        public Data.Contest Contest { get; set; }

        [ViewData]
        public ContestProblem[] Problems { get; set; }

        [ViewData]
        public Dictionary<int, Language> Languages { get; set; }

        [ViewData]
        public Team Team { get; set; }


        protected IEnumerable<(string, ClarificationCategory, int?)> ClarCategories =>
            Problems
                .Select(cp => ($"prob-{cp.ShortName}", ClarificationCategory.Problem, (int?)cp.ProblemId))
                .Prepend(("tech", ClarificationCategory.Technical, null))
                .Prepend(("general", ClarificationCategory.General, null));

        protected IMemoryCache Cache => ContestCache._cache;

        protected Task<Team> FindTeamByIdAsync(int teamid)
        {
            int cid = Contest.ContestId;
            return DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`t{teamid}", TimeSpan.FromMinutes(5));
        }

        protected Task<Team> FindTeamByUserAsync(int uid)
        {
            int cid = Contest.ContestId;
            return DbContext.Teams
                .Where(t => t.ContestId == cid && t.UserId == uid)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`u{uid}", TimeSpan.FromMinutes(5));
        }

        protected async Task<int> SendClarificationAsync(Clarification clar, Clarification replyTo = null)
        {
            var cl = DbContext.Clarifications.Add(clar);

            if (replyTo != null)
            {
                replyTo.Answered = true;
                DbContext.Clarifications.Update(replyTo);
            }

            await DbContext.SaveChangesAsync();

            var ct = new Data.Api.ContestClarification(
                clar, Contest.StartTime ?? DateTimeOffset.Now);
            if (ct.text.Length > 300)
                ct.text = ct.text.Substring(0, 300) + "...";
            DbContext.Events.Add(ct.ToEvent("create", Contest.ContestId));

            await DbContext.SaveChangesAsync();
            return cl.Entity.ClarificationId;
        }

        protected async Task<int> CreateTeamAsync(Team team, TeamAffiliation aff)
        {
            using (await ContestCache._locker.LockAsync())
            {
                int cid = team.ContestId;
                
                team.TeamId = 1 + await DbContext.Teams
                    .CountAsync(tt => tt.ContestId == cid);
                DbContext.Teams.Add(team);

                InternalLog(new AuditLog
                {
                    Type = AuditLog.TargetType.Contest,
                    Resolved = true,
                    ContestId = team.ContestId,
                    Comment = $"add team t{team.TeamId}",
                    EntityId = team.TeamId,
                });

                if (team.Status == 1)
                {
                    var ct = new Data.Api.ContestTeam(team, aff);
                    DbContext.Events.Add(ct.ToEvent("create", cid));
                }

                await DbContext.SaveChangesAsync();
                Cache.Remove($"`c{cid}`teams`list_jury");
                Cache.Remove($"`c{cid}`teams`t{team.TeamId}");
                Cache.Remove($"`c{cid}`teams`u{team.UserId}");
                return team.TeamId;
            }
        }

        protected void InternalLog(AuditLog log)
        {
            log.LogId = 0;
            log.Time = DateTimeOffset.Now;
            log.UserName = UserManager.GetUserName(User);
            DbContext.AuditLogs.Add(log);
        }

        protected async Task<SingleBoardViewModel> FindScoreboardAsync(int teamid)
        {
            var scb = await DbContext.LoadScoreboardAsync(Contest.ContestId);
            var bq = scb.Data.GetValueOrDefault(teamid);
            var cats = await DbContext.ListTeamCategoryAsync(Contest.ContestId);

            return new SingleBoardViewModel
            {
                QueryInfo = bq,
                ExecutionStrategy = Scoreboards[Contest.RankingStrategy],
                Contest = Contest,
                Problems = Problems,
                Category = cats.FirstOrDefault(c => c.CategoryId == bq.Team.CategoryId),
            };
        }

        protected IActionResult PrintView()
        {
            if (!Contest.PrintingAvaliable)
                return NotFound();
            return View(new Models.AddPrintModel());
        }

        protected async Task<IActionResult> PrintDo(int cid, Models.AddPrintModel model)
        {
            if (!Contest.PrintingAvaliable)
                return NotFound();

            DbContext.Printing.Add(new Data.Ext.Printing
            {
                ContestId = cid,
                LanguageId = model.Language ?? "plain",
                FileName = System.IO.Path.GetFileName(model.SourceFile.FileName),
                Time = DateTimeOffset.Now,
                UserId = int.Parse(UserManager.GetUserId(User)),
                SourceCode = (await model.SourceFile.ReadAsync()).Item1
            });

            await DbContext.SaveChangesAsync();
            StatusMessage = "File has been printed. Please wait.";
            return RedirectToAction("Home");
        }

        protected async Task<IActionResult> ScoreboardView(bool isPublic, bool isJury, bool clear, int[] aff, int[] cat)
        {
            var scb = await DbContext.LoadScoreboardAsync(Contest.ContestId);
            var affs = await DbContext.ListTeamAffiliationAsync(Contest.ContestId);
            var orgs = await DbContext.ListTeamCategoryAsync(Contest.ContestId, !isJury);

            var board = new FullBoardViewModel
            {
                RankCache = scb.Data.Values,
                UpdateTime = scb.RefreshTime,
                Problems = Problems,
                IsPublic = isPublic && !isJury,
                Categories = orgs,
                ExecutionStrategy = Scoreboards[Contest.RankingStrategy],
                Contest = Contest,
                Affiliations = affs,
            };

            if (clear) cat = aff = Array.Empty<int>();

            if (aff.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => aff.Contains(t.Team.AffiliationId));
                ViewData["Filter_affiliations"] = aff.ToHashSet();
            }

            if (cat.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => cat.Contains(t.Team.CategoryId));
                ViewData["Filter_categories"] = cat.ToHashSet();
            }

            return View(board);
        }

        [NonAction]
        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // check the contest info
            if (!context.RouteData.Values.TryGetValue("cid", out var __cid)
                || !int.TryParse(__cid.ToString(), out int cid))
            {
                context.Result = BadRequest();
                return;
            }

            // parse the base service
            UserManager = HttpContext.RequestServices
                .GetRequiredService<UserManager>();
            DbContext = HttpContext.RequestServices
                .GetRequiredService<AppDbContext>();
            DbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            // check the existence
            Contest = await DbContext.GetContestAsync(cid);
            ViewBag.Contest = Contest;
            if (Contest == null)
            {
                context.Result = NotFound();
                return;
            }

            Problems = await DbContext.GetProblemsAsync(cid);
            Languages = await DbContext.GetLanguagesAsync(cid);
            ViewBag.Problems = Problems;
            ViewBag.Languages = Languages;

            if (!Cache.TryGetValue($"`c{cid}`internal_state", out ContestState state)
                || state != Contest.GetState())
            {
                Cache.Set($"`c{cid}`internal_state", Contest.GetState(), TimeSpan.FromDays(365));
                DbContext.Events.AddUpdate(cid, new Data.Api.ContestTime(Contest));
                await DbContext.SaveChangesAsync();
            }

            // check the permission
            if (User.IsInRoles($"Administrator,JuryOfContest{cid}"))
                ViewData["IsJury"] = true;

            if (int.TryParse(UserManager?.GetUserId(User) ?? "-1", out int uid) && uid > 0)
            {
                ViewBag.Team = Team = await FindTeamByUserAsync(uid);
                if (Team != null) ViewData["HasTeam"] = true;
            }

            await OnActionExecutingAsync(context);
            ViewData["ContestId"] = cid;

            if (context.Result == null)
                await OnActionExecutedAsync(await next());
        }

        [NonAction]
        public virtual Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            OnActionExecuting(context);
            return Task.CompletedTask;
        }

        [NonAction]
        public virtual Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            OnActionExecuted(context);
            return Task.CompletedTask;
        }
    }
}
