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

        protected IScoreboardService ScoreboardService { get; private set; }

        [TempData]
        public string StatusMessage { get; set; }

        [ViewData]
        public Data.Contest Contest { get; set; }

        [ViewData]
        public ContestProblem[] Problems { get; set; }

        [ViewData]
        public Dictionary<string, Language> Languages { get; set; }

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
            return DbContext.TeamMembers
                .Where(tu => tu.ContestId == cid && tu.UserId == uid)
                .Join(
                    inner: DbContext.Teams,
                    outerKeySelector: tu => new { tu.ContestId, tu.TeamId },
                    innerKeySelector: t => new { t.ContestId, t.TeamId },
                    resultSelector: (tu, t) => t)
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

            InternalLog(AuditlogType.Clarification, $"{clar.ClarificationId}", "added");
            await DbContext.SaveChangesAsync();
            return cl.Entity.ClarificationId;
        }

        protected async Task<int> CreateTeamAsync(Team team, TeamAffiliation aff, int[] uids = null)
        {
            using (await ContestCache._locker.LockAsync())
            {
                int cid = team.ContestId;
                
                team.TeamId = 1 + await DbContext.Teams
                    .CountAsync(tt => tt.ContestId == cid);
                DbContext.Teams.Add(team);

                if (uids != null)
                {
                    foreach (var uid in uids)
                    {
                        DbContext.TeamMembers.Add(new TeamMember
                        {
                            ContestId = team.ContestId,
                            TeamId = team.TeamId,
                            UserId = uid,
                            Temporary = false
                        });
                    }
                }

                InternalLog(AuditlogType.Team, $"{team.TeamId}", "added");

                if (team.Status == 1)
                {
                    var ct = new Data.Api.ContestTeam(team, aff);
                    DbContext.Events.Add(ct.ToEvent("create", cid));
                }

                await DbContext.SaveChangesAsync();
                Cache.Remove($"`c{cid}`teams`list_jury");
                Cache.Remove($"`c{cid}`teams`t{team.TeamId}");
                Cache.Remove($"`c{cid}`teams`members");

                if (uids != null)
                    foreach (var uid in uids)
                        Cache.Remove($"`c{cid}`teams`u{uid}");
                return team.TeamId;
            }
        }

        protected void InternalLog(AuditlogType type, string dataId, string action, string extraInfo = null)
        {
            DbContext.Auditlogs.Add(new Auditlog
            {
                Time = DateTimeOffset.Now,
                UserName = UserManager.GetUserName(User),
                ContestId = Contest.ContestId,
                Action = action,
                ExtraInfo = extraInfo,
                DataId = dataId,
                DataType = type,
            });
        }

        protected async Task<SingleBoardViewModel> FindScoreboardAsync(int teamid)
        {
            var scb = await DbContext.LoadScoreboardAsync(Contest.ContestId);
            var bq = scb.Data.GetValueOrDefault(teamid);
            if (bq == null) return null;
            var cats = await DbContext.ListTeamCategoryAsync(Contest.ContestId);

            return new SingleBoardViewModel
            {
                QueryInfo = bq,
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

            var p = DbContext.Printing.Add(new Printing
            {
                ContestId = cid,
                LanguageId = model.Language ?? "plain",
                FileName = System.IO.Path.GetFileName(model.SourceFile.FileName),
                Time = DateTimeOffset.Now,
                UserId = int.Parse(UserManager.GetUserId(User)),
                SourceCode = (await model.SourceFile.ReadAsync()).Item1
            });

            await DbContext.SaveChangesAsync();

            InternalLog(AuditlogType.Printing, $"{p.Entity.Id}", "added", $"from {HttpContext.Connection.RemoteIpAddress}");
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

            if (!Contest.IsPublic && !ViewData.ContainsKey("IsJury") && !ViewData.ContainsKey("HasTeam"))
            {
                context.Result = NotFound();
                return;
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
