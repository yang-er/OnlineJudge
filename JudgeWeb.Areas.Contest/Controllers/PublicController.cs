using JudgeWeb.Areas.Contest.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/[action]")]
    public class PublicController : BaseController<ContestContext>
    {
        protected override IActionResult BeforeActionExecuting()
        {
            ViewBag.Contest = Contest;
            ViewBag.Team = Service.Team;
            return base.BeforeActionExecuting();
        }

        [HttpGet]
        [Route("/[area]/{cid}")]
        public IActionResult Info()
        {
            ViewBag.Affiliations = Service.QueryAffiliations(true).ToDictionary(a => a.AffiliationId);
            ViewBag.Categories = Service.QueryCategories(null).ToDictionary(a => a.CategoryId);
            ViewBag.DisplayMessage = DisplayMessage;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register()
        {
            if (Contest.RegisterDefaultCategory == 0 || User.IsInRole("Blocked"))
            {
                DisplayMessage = "Error registration closed.";
                return RedirectToAction(nameof(Info));
            }

            var uid = UserManager.GetUserId(User);
            if (uid == null)
            {
                return RedirectToAction("Login", "Sign", new { area = "Account" });
            }

            var uuid = int.Parse(uid);
            if (Service.QueryUserTeam(uuid, true).FirstOrDefault() != null)
            {
                return RedirectToAction("Home", "Team");
            }

            var aff = Service.QueryAffiliations(false)
                .ToList().First(a => a.ExternalId == "null").AffiliationId;

            Service.CreateTeam(new Data.Team
            {
                AffiliationId = aff,
                ContestId = Contest.ContestId,
                CategoryId = Contest.RegisterDefaultCategory,
                RegisterTime = System.DateTime.Now,
                Status = 0,
                TeamName = UserManager.GetNickName(User) ?? UserManager.GetUserName(User),
                UserId = uuid,
            });

            return RedirectToAction(nameof(Info));
        }
    }
}
