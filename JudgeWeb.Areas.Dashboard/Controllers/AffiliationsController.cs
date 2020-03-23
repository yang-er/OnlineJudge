using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.TeamAffiliation)]
    public class AffiliationsController : Controller3
    {
        private ITeamManager Store { get; }
        public AffiliationsController(ITeamManager tm) => Store = tm;


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListAffiliationsAsync());
        }


        [HttpGet("{affid}")]
        public async Task<IActionResult> Detail(int affid)
        {
            var aff = await Store.FindAffiliationAsync(affid);
            if (aff == null) return NotFound();
            return View(aff);
        }


        [HttpGet("[action]")]
        public IActionResult Add() => View("Edit", new TeamAffiliation());


        [HttpGet("{affid}/[action]")]
        public async Task<IActionResult> Edit(int affid)
        {
            var aff = await Store.FindAffiliationAsync(affid);
            if (aff == null) return NotFound();
            return View(aff);
        }


        private async Task SolveLogo(IFormFile logo, string extid)
        {
            if (logo != null && logo.FileName.EndsWith(".png"))
            {
                try
                {
                    var write = new FileStream($"wwwroot/images/affiliations/{extid}.png", FileMode.OpenOrCreate);
                    await logo.CopyToAsync(write);
                    write.Close();
                    write.Dispose();
                }
                catch
                {
                    StatusMessage = "Error, logo upload failed!";
                }
            }
            else if (logo != null)
            {
                StatusMessage = "Error, logo should be png!";
            }
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TeamAffiliation model, IFormFile logo)
        {
            model.AffiliationId = 0;
            if (model.FormalName == null || model.ExternalId == null)
                return BadRequest();
            var e = await Store.CreateAsync(model);
            await SolveLogo(logo, model.ExternalId);
            await HttpContext.AuditAsync("created", $"{e.AffiliationId}");
            return RedirectToAction(nameof(Detail), new { affid = e.AffiliationId });
        }


        [HttpPost("{affid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int affid, TeamAffiliation model, IFormFile logo)
        {
            var aff = await Store.FindAffiliationAsync(affid);
            if (aff == null) return NotFound();

            if (model.FormalName == null || model.ExternalId == null)
                return BadRequest();
            aff.FormalName = model.FormalName;
            aff.ExternalId = model.ExternalId;
            aff.CountryCode = model.CountryCode;

            await Store.UpdateAsync(aff);
            await SolveLogo(logo, model.ExternalId);
            await HttpContext.AuditAsync("updated", $"{affid}");
            return RedirectToAction(nameof(Detail), new { affid });
        }


        [HttpGet("{affid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int affid)
        {
            var desc = await Store.FindAffiliationAsync(affid);
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete team affiliation {desc.ExternalId} - \"{desc.FormalName}\"",
                message: $"You're about to delete team affiliation {desc.ExternalId} - \"{desc.FormalName}\".\n" +
                    "Are you sure?",
                area: "Dashboard", ctrl: "Affiliations", act: "Delete",
                type: MessageType.Danger);
        }


        [HttpPost("{affid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int affid, int inajax)
        {
            var desc = await Store.FindAffiliationAsync(affid);
            if (desc == null) return NotFound();

            try
            {
                await Store.DeleteAsync(desc);
                StatusMessage = $"Team affiliation {desc.ExternalId} deleted successfully.";
                await HttpContext.AuditAsync("deleted", $"{affid}");
                return RedirectToAction(nameof(List));
            }
            catch
            {
                StatusMessage = $"Error deleting team affiliation {desc.ExternalId}, foreign key constraints failed.";
                return RedirectToAction(nameof(Detail), new { affid });
            }
        }
    }
}
