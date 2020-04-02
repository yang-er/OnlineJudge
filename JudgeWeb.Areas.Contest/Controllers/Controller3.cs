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
        protected IContestFacade Facade { get; private set; }

        protected IContestEventNotifier Notifier { get; private set; }

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

        protected async Task<SingleBoardViewModel> FindScoreboardAsync(int teamid)
        {
            var scb = await Facade.Teams.LoadScoreboardAsync(Contest.ContestId);
            var bq = scb.Data.GetValueOrDefault(teamid);
            if (bq == null) return null;
            var cats = await Facade.Teams.ListCategoryAsync(Contest.ContestId);
            var affs = await Facade.Teams.ListAffiliationAsync(Contest.ContestId);

            return new SingleBoardViewModel
            {
                QueryInfo = bq,
                Contest = Contest,
                Problems = Problems,
                Affiliation = affs.FirstOrDefault(a => a.AffiliationId == bq.AffiliationId),
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

            var Printings = HttpContext.RequestServices
                .GetRequiredService<IPrintingStore>();
            var p = await Printings.CreateAsync(new Printing
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
            var store = Facade.Teams;
            var scb = await store.LoadScoreboardAsync(Contest.ContestId);
            var affs = await store.ListAffiliationAsync(Contest.ContestId);
            var orgs = await store.ListCategoryAsync(Contest.ContestId, !isJury);

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
            Facade = HttpContext.RequestServices
                .GetRequiredService<IContestFacade>();
            Notifier = HttpContext.RequestServices
                .GetRequiredService<IContestEventNotifier>();

            // check the existence
            Contest = await Facade.Contests.FindAsync(cid);
            if (Contest == null)
            {
                context.Result = NotFound();
                return;
            }

            HttpContext.Items[nameof(cid)] = cid;
            ViewBag.Contest = Contest;
            Problems = await Facade.Problemset.ListAsync(cid);
            Languages = await Facade.ListLanguageAsync(cid);
            ViewBag.Problems = Problems;
            ViewBag.Languages = Languages;

            // the event of contest state change
            var stateNow = Contest.GetState();
            if (!Cache.TryGetValue($"`c{cid}`internal_state", out ContestState state))
            {
                Cache.Set($"`c{cid}`internal_state", stateNow, TimeSpan.FromDays(365));
                if (stateNow != ContestState.Finalized)
                    await Notifier.Update(cid, Contest, stateNow);
            }
            else if (state != stateNow)
            {
                Cache.Set($"`c{cid}`internal_state", stateNow, TimeSpan.FromDays(365));
                if (stateNow != ContestState.Finalized)
                    await Notifier.Update(cid, Contest, stateNow);
            }

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
