using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class AffiliationsController : Controller3
    {
        public AffiliationsController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await DbContext.TeamAffiliations.ToListAsync());
        }

        [HttpGet("{affid}")]
        public async Task<IActionResult> Detail(int affid)
        {
            var aff = await DbContext.TeamAffiliations
                .Where(a => a.AffiliationId == affid)
                .FirstOrDefaultAsync();
            if (aff == null) return NotFound();
            return View(aff);
        }

        [HttpGet("[action]")]
        public IActionResult Add() => View("Edit", new TeamAffiliation());

        [HttpGet("{affid}/[action]")]
        public async Task<IActionResult> Edit(int affid)
        {
            var aff = await DbContext.TeamAffiliations
                .Where(a => a.AffiliationId == affid)
                .FirstOrDefaultAsync();
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
            var e = DbContext.TeamAffiliations.Add(model);
            await DbContext.SaveChangesAsync();
            await SolveLogo(logo, model.ExternalId);
            return RedirectToAction(nameof(Detail), new { affid = e.Entity.AffiliationId });
        }

        [HttpPost("{affid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int affid, TeamAffiliation model, IFormFile logo)
        {
            var aff = await DbContext.TeamAffiliations
                .Where(a => a.AffiliationId == affid)
                .FirstOrDefaultAsync();
            if (aff == null) return NotFound();

            if (model.FormalName == null || model.ExternalId == null)
                return BadRequest();
            aff.FormalName = model.FormalName;
            aff.ExternalId = model.ExternalId;

            DbContext.TeamAffiliations.Update(aff);
            await DbContext.SaveChangesAsync();
            await SolveLogo(logo, model.ExternalId);
            return RedirectToAction(nameof(Detail), new { affid });
        }

        [HttpGet("{affid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int affid)
        {
            var desc = await DbContext.TeamAffiliations
                .Where(e => e.AffiliationId == affid)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete team affiliation {desc.ExternalId} - \"{desc.FormalName}\"",
                message: $"You're about to delete team affiliation {desc.ExternalId} - \"{desc.FormalName}\".\n" +
                    "Are you sure?",
                area: "Dashboard",
                ctrl: "Affiliations",
                act: "Delete",
                routeValues: new Dictionary<string, string> { { "affid", affid.ToString() } },
                type: MessageType.Danger);
        }

        [HttpPost("{affid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int affid, int inajax)
        {
            var desc = await DbContext.TeamAffiliations
                .Where(e => e.AffiliationId == affid)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            DbContext.TeamAffiliations.Remove(desc);

            try
            {
                await DbContext.SaveChangesAsync();
                StatusMessage = $"Team affiliation {desc.ExternalId} deleted successfully.";
            }
            catch (DbUpdateException)
            {
                StatusMessage = $"Error deleting team affiliation {desc.ExternalId}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
