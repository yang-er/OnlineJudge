using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Areas.Contest.Services;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Controllers
{
    public abstract class BaseController<T> : Controller2
        where T : ContestContext, new()
    {
        protected T Service { get; private set; }

        protected UserManager UserManager { get; private set; }

        protected virtual string GoAfterSubmit => "Home";

        public Data.Contest Contest { get; private set; }

        [TempData]
        public string DisplayMessage { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // parse the ajax info
            if (HttpContext.Request.Headers.TryGetValue("X-Requested-With", out var val)
                    && val.FirstOrDefault() == "XMLHttpRequest")
                ViewData["InAjax"] = true;
            ViewData["RefreshUrl"] = HttpContext.Request.Path.Value +
                HttpContext.Request.QueryString.Value.Replace("&amp;", "&");

            // parse the base service
            var adbc = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            UserManager = HttpContext.RequestServices.GetRequiredService<UserManager>();
            int uid = int.Parse(UserManager.GetUserId(User) ?? "-1");
            var uname = UserManager.GetUserName(User);

            // parse the contest info
            context.RouteData.Values.TryGetValue("cid", out var cidInfo);
            int.TryParse(cidInfo.ToString(), out int cid);
            Service = new T();
            Service.Init(cid, uid, uname, adbc);
            Contest = Service.Contest;
            if (Contest is null) context.Result = NotFound();
            ViewData["ContestId"] = cid;
            ViewBag.Contest = Contest;

            // other filting
            if (context.Result == null)
                context.Result = BeforeActionExecuting();
        }

        protected virtual IActionResult BeforeActionExecuting() => null;

        protected virtual bool EnableScoreboard => true;

        protected virtual (bool showPublic, bool isJury) ScoreboardChooseStrategy()
        {
            bool showPublic = true;
            if (!Contest.FreezeTime.HasValue)
                showPublic = false;
            else if (Contest.UnfreezeTime.HasValue
                    && Contest.UnfreezeTime.Value < DateTimeOffset.Now)
                showPublic = false;
            return (showPublic, false);
        }

        [HttpGet]
        public IActionResult Scoreboard(
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            if (!EnableScoreboard) return NotFound();
            var (showPublic, isJury) = ScoreboardChooseStrategy();
            var cont = Service.GetTotalBoard(showPublic, isJury);

            if (clear == "clear")
            {
                affiliations = Array.Empty<int>();
                categories = Array.Empty<int>();
            }

            if (affiliations.Length > 0)
            {
                cont.RankCache = cont.RankCache
                    .Where(t => affiliations.Contains(t.Team.AffiliationId));
                ViewData["Filter_affiliations"] = affiliations.ToHashSet();
            }

            if (categories.Length > 0)
            {
                cont.RankCache = cont.RankCache
                    .Where(t => categories.Contains(t.Team.CategoryId));
                ViewData["Filter_categories"] = categories.ToHashSet();
            }

            return View(cont);
        }

        protected int SubmitCore(TeamCodeSubmitModel model)
        {
            var problems = Service.Problems;
            var prob = problems.FirstOrDefault(cp => cp.ShortName == model.Problem);
            if (prob is null) return -1;

            var s = new Submission
            {
                Author = Service.Team.TeamId,
                CodeLength = model.Code.Length,
                ContestId = Contest.ContestId,
                Ip = HttpContext.Connection.RemoteIpAddress.ToString(),
                Language = model.Language,
                ProblemId = prob.ProblemId,
                Time = DateTimeOffset.Now,
                SourceCode = model.Code.ToBase64()
            };

            return Service.CreateSubmission(s);
        }
    }
}
