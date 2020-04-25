using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.FileProviders;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[action]")]
    public class PublicController : Controller3
    {
        private ITeamStore Store => Facade.Teams;

        public override Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            if (Contest.Gym)
                context.Result = RedirectToAction("Home", "Gym");
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
            [FromServices] IProblemFileRepository io)
        {
            var affs = await Store.ListAffiliationAsync(cid);
            ViewBag.Affiliations = affs.ToDictionary(a => a.AffiliationId);
            var cats = await Store.ListCategoryAsync(cid);
            ViewBag.Categories = cats.ToDictionary(a => a.CategoryId);

            var fileInfo = io.GetFileInfo($"c{cid}/readme.html");
            ViewBag.Markdown = await fileInfo.ReadAsync();
            return View();
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [AuditPoint(AuditlogType.Team)]
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
            var affs = await Store.ListAffiliationAsync(cid, false);
            var aff = affs.FirstOrDefault(a => a.ExternalId == defaultAff);
            if (aff == null) throw new System.ApplicationException("No default affiliation.");

            int tid = await Facade.Teams.CreateAsync(
                uids: new[] { int.Parse(User.GetUserId()) },
                team: new Team
                {
                    AffiliationId = aff.AffiliationId,
                    ContestId = Contest.ContestId,
                    CategoryId = Contest.RegisterDefaultCategory,
                    RegisterTime = System.DateTimeOffset.Now,
                    Status = 0,
                    TeamName = User.GetNickName() ?? User.GetUserName(),
                });

            await HttpContext.AuditAsync("added", $"{tid}");
            StatusMessage = "Registration succeeded.";
            return RedirectToAction(nameof(Info));
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult NotFound2() => StatusCodePage(404);
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => StatusCodePage();
    }
}
