using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class CategoriesController : Controller3
    {
        public CategoriesController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await DbContext.TeamCategories.ToListAsync());
        }

        [HttpGet("{catid}")]
        public async Task<IActionResult> Detail(int catid)
        {
            var cat = await DbContext.TeamCategories
                .Where(ccsu_cat => ccsu_cat.CategoryId == catid)
                .FirstOrDefaultAsync();
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpGet("[action]")]
        public IActionResult Add() => View("Edit", new TeamCategory());

        [HttpGet("{catid}/[action]")]
        public async Task<IActionResult> Edit(int catid)
        {
            var cat = await DbContext.TeamCategories
                .Where(ccsu_cat => ccsu_cat.CategoryId == catid)
                .FirstOrDefaultAsync();
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TeamCategory model)
        {
            model.CategoryId = 0;
            var e = DbContext.TeamCategories.Add(model);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { catid = e.Entity.CategoryId });
        }

        [HttpPost("{catid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int catid, TeamCategory model)
        {
            var cat = await DbContext.TeamCategories
                .Where(ccsu_cat => ccsu_cat.CategoryId == catid)
                .FirstOrDefaultAsync();
            if (cat == null) return NotFound();

            if (model.Name == null || model.Color == null)
                return BadRequest();
            cat.Color = model.Color;
            cat.IsPublic = model.IsPublic;
            cat.Name = model.Name;
            cat.SortOrder = model.SortOrder;

            DbContext.TeamCategories.Update(cat);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { catid });
        }

        [HttpGet("{catid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int catid)
        {
            var desc = await DbContext.TeamCategories
                .Where(e => e.CategoryId == catid)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete team category {catid} - \"{desc.Name}\"",
                message: $"You're about to delete team category {catid} - \"{desc.Name}\".\n" +
                    "Are you sure?",
                area: "Dashboard",
                ctrl: "Categories",
                act: "Delete",
                routeValues: new Dictionary<string, string> { { "catid", catid.ToString() } },
                type: MessageType.Danger);
        }

        [HttpPost("{catid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int catid, int inajax)
        {
            var desc = await DbContext.TeamCategories
                .Where(e => e.CategoryId == catid)
                .FirstOrDefaultAsync();
            if (desc == null) return NotFound();

            DbContext.TeamCategories.Remove(desc);

            try
            {
                await DbContext.SaveChangesAsync();
                StatusMessage = $"Team category {catid} deleted successfully.";
            }
            catch (DbUpdateException)
            {
                StatusMessage = $"Error deleting team category {catid}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
