using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[action]")]
    public class PublicController : Controller3
    {
        public override Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            if (Contest.Gym)
                context.Result = NotFound();
            return base.OnActionExecutingAsync(context);
        }


        [HttpGet]
        public Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "") =>
            ScoreboardView(
                isPublic: Contest.GetState() < ContestState.Finalized,
                isJury: false, clear == "clear", affiliations, categories);


        [HttpGet]
        [Route("/[area]/{cid}")]
        public async Task<IActionResult> Info(int cid,
            [FromServices] Features.Storage.IFileRepository io)
        {
            var affs = await DbContext.ListTeamAffiliationAsync(cid);
            ViewBag.Affiliations = affs.ToDictionary(a => a.AffiliationId);
            var cats = await DbContext.ListTeamCategoryAsync(cid);
            ViewBag.Categories = cats.ToDictionary(a => a.CategoryId);

            io.SetContext("Problems");
            ViewBag.Markdown = await io.ReadPartAsync("c" + Contest.ContestId, "readme.html");
            return View();
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int cid)
        {
            if (ViewData.ContainsKey("HasTeam"))
            {
                StatusMessage = "Already registered";
                return RedirectToAction(nameof(Info));
            }

            if (Contest.RegisterDefaultCategory == 0 || User.IsInRole("Blocked"))
            {
                StatusMessage = "Error registration closed.";
                return RedirectToAction(nameof(Info));
            }

            string defaultAff = User.IsInRole("Student") ? "jlu" : "null";
            var affs = await DbContext.ListTeamAffiliationAsync(cid, false);
            var aff = affs.FirstOrDefault(a => a.ExternalId == defaultAff);
            if (aff == null) throw new System.ApplicationException("No default affiliation.");

            await CreateTeamAsync(
                aff: aff,
                uids: new[] { int.Parse(UserManager.GetUserId(User)) },
                team: new Team
                {
                    AffiliationId = aff.AffiliationId,
                    ContestId = Contest.ContestId,
                    CategoryId = Contest.RegisterDefaultCategory,
                    RegisterTime = System.DateTimeOffset.Now,
                    Status = 0,
                    TeamName = UserManager.GetNickName(User) ?? UserManager.GetUserName(User),
                });

            StatusMessage = "Registration succeeded.";
            return RedirectToAction(nameof(Info));
        }
    }
}
