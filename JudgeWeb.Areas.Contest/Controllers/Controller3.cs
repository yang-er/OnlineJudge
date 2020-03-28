using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
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
        protected UserManager UserManager { get; private set; }

        protected IScoreboardService ScoreboardService { get; private set; }

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

        protected IMemoryCache Cache => CachedQueryable.Cache;

        protected Task<Team> FindTeamByIdAsync(int teamid)
        {
            return Facade.Teams.FindByIdAsync(Contest.ContestId, teamid);
        }

        protected Task<Team> FindTeamByUserAsync(int uid)
        {
            return Facade.Teams.FindByUserAsync(Contest.ContestId, uid);
        }

        protected void InternalLog(AuditlogType type, string dataId, string action, string extraInfo = null)
        {
            DbContext.Auditlogs.Add(new Auditlog
            {
                Time = DateTimeOffset.Now,
                UserName = User.GetUserName(),
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
                Category = cats.FirstOrDefault(c => c.CategoryId == bq.CategoryId),
            };
        }

        protected IActionResult PrintView()
        {
            if (!Contest.PrintingAvaliable)
                return ExplicitNotFound();
            return View(new Models.AddPrintModel());
        }

        protected async Task<IActionResult> PrintDo(int cid, Models.AddPrintModel model)
        {
            if (!Contest.PrintingAvaliable)
                return ExplicitNotFound();

            var p = await Facade.Printings.CreateAsync(new Printing
            {
                ContestId = cid,
                LanguageId = model.Language ?? "plain",
                FileName = System.IO.Path.GetFileName(model.SourceFile.FileName),
                Time = DateTimeOffset.Now,
                UserId = int.Parse(User.GetUserId()),
                SourceCode = (await model.SourceFile.ReadAsync()).Item1
            });

            await HttpContext.AuditAsync("added", $"{p.Id}",
                $"from {HttpContext.Connection.RemoteIpAddress}");
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
                    .Where(t => aff.Contains(t.AffiliationId));
                ViewData["Filter_affiliations"] = aff.ToHashSet();
            }

            if (cat.Length > 0)
            {
                board.RankCache = board.RankCache
                    .Where(t => cat.Contains(t.CategoryId));
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
                context.Result = NotFound();
                return;
            }

            // parse the base service
            var store = HttpContext.RequestServices
                .GetRequiredService<IContestStore>();

            // check the existence
            Contest = await store.FindAsync(cid);
            ViewBag.Contest = Contest;
            if (Contest == null)
            {
                context.Result = NotFound();
                return;
            }

            Problems = await store.ListProblemsAsync(cid);
            Languages = await store.ListLanguageAsync(cid);
            ViewBag.Problems = Problems;
            ViewBag.Languages = Languages;

            /*
            if (!Cache.TryGetValue($"`c{cid}`internal_state", out ContestState state)
                || state != Contest.GetState())
            {
                Cache.Set($"`c{cid}`internal_state", Contest.GetState(), TimeSpan.FromDays(365));
                
                //DbContext.Events.AddUpdate(cid, new Data.Api.ContestTime(Contest));
                //await DbContext.SaveChangesAsync();
            }*/

            // check the permission
            if (User.IsInRoles($"Administrator,JuryOfContest{cid}"))
                ViewData["IsJury"] = true;

            if (int.TryParse(User.GetUserId() ?? "-1", out int uid) && uid > 0)
            {
                ViewBag.Team = Team = await FindTeamByUserAsync(uid);
                if (Team != null) ViewData["HasTeam"] = true;
            }

            if (!Contest.IsPublic &&
                !ViewData.ContainsKey("IsJury") &&
                !ViewData.ContainsKey("HasTeam"))
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
