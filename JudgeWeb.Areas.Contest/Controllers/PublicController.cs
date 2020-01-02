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
        [HttpGet]
        public async Task<IActionResult> Scoreboard(int cid,
            [FromQuery(Name = "affiliations[]")] int[] affiliations,
            [FromQuery(Name = "categories[]")] int[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            var board = await Service.FindScoreboardAsync(cid, true, false);

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


        [HttpGet]
        [Route("/[area]/{cid}")]
        public async Task<IActionResult> Info(int cid,
            [FromServices] Features.Storage.IFileRepository io)
        {
            var affs = await Service.ListTeamAffiliationAsync(cid);
            ViewBag.Affiliations = affs.ToDictionary(a => a.AffiliationId);
            var cats = await Service.ListTeamCategoryAsync(cid);
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
                DisplayMessage = "Already registered";
                return RedirectToAction(nameof(Info));
            }

            if (Contest.RegisterDefaultCategory == 0 || User.IsInRole("Blocked"))
            {
                DisplayMessage = "Error registration closed.";
                return RedirectToAction(nameof(Info));
            }

            string defaultAff = User.IsInRole("Student") ? "jlu" : "null";
            var affs = await Service.ListTeamAffiliationAsync(cid, false);
            var aff = affs.FirstOrDefault(a => a.ExternalId == defaultAff)?.AffiliationId;
            if (!aff.HasValue) throw new System.ApplicationException("No default affiliation.");

            await Service.CreateTeamAsync(new Data.Team
            {
                AffiliationId = aff.Value,
                ContestId = Contest.ContestId,
                CategoryId = Contest.RegisterDefaultCategory,
                RegisterTime = System.DateTimeOffset.Now,
                Status = 0,
                TeamName = UserManager.GetNickName(User) ?? UserManager.GetUserName(User),
                UserId = int.Parse(UserManager.GetUserId(User)),
            });

            DisplayMessage = "Registration succeeded.";
            return RedirectToAction(nameof(Info));
        }
    }
}
