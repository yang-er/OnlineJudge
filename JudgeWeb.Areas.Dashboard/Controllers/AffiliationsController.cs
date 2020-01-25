using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private async Task AuditlogAsync(TeamAffiliation aff, string act)
        {
            // solve the events
            var now = System.DateTimeOffset.Now;
            var cts = await DbContext.Contests
                .Where(c => c.EndTime == null || c.EndTime > now)
                .Select(c => c.ContestId)
                .ToArrayAsync();
            DbContext.Events.AddRange(cts.Select(t =>
                new Data.Api.ContestOrganization(aff).ToEvent(act, t)));

            // solve the auditlogs
            DbContext.Auditlogs.Add(new Auditlog
            {
                Action = act + "d",
                DataId = $"{aff.AffiliationId}",
                DataType = AuditlogType.TeamAffiliation,
                UserName = UserManager.GetUserName(User),
                Time = now,
            });

            await DbContext.SaveChangesAsync();
        }


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
            await AuditlogAsync(e.Entity, "create");

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
            aff.CountryCode = model.CountryCode;

            DbContext.TeamAffiliations.Update(aff);
            await DbContext.SaveChangesAsync();
            await SolveLogo(logo, model.ExternalId);
            await AuditlogAsync(aff, "update");

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
                area: "Dashboard", ctrl: "Affiliations", act: "Delete",
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

            try
            {
                DbContext.TeamAffiliations.Remove(desc);
                await DbContext.SaveChangesAsync();
                StatusMessage = $"Team affiliation {desc.ExternalId} deleted successfully.";
                await AuditlogAsync(desc, "delete");
            }
            catch (DbUpdateException)
            {
                StatusMessage = $"Error deleting team affiliation {desc.ExternalId}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
