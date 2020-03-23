using JudgeWeb.Data;
using JudgeWeb.Domains.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    [AuditPoint(AuditlogType.TeamCategory)]
    public class CategoriesController : Controller3
    {
        private ITeamManager Store { get; }
        public CategoriesController(ITeamManager tm) => Store = tm;


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListCategoriesAsync());
        }


        [HttpGet("{catid}")]
        public async Task<IActionResult> Detail(int catid)
        {
            var cat = await Store.FindCategoryAsync(catid);
            if (cat == null) return NotFound();
            return View(cat);
        }


        [HttpGet("[action]")]
        public IActionResult Add() => View("Edit", new TeamCategory());


        [HttpGet("{catid}/[action]")]
        public async Task<IActionResult> Edit(int catid)
        {
            var cat = await Store.FindCategoryAsync(catid);
            if (cat == null) return NotFound();
            return View(cat);
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TeamCategory model)
        {
            model.CategoryId = 0;
            await Store.CreateAsync(model);
            await HttpContext.AuditAsync("created", $"{model.CategoryId}");
            return RedirectToAction(nameof(Detail), new { catid = model.CategoryId });
        }


        [HttpPost("{catid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int catid, TeamCategory model)
        {
            var cat = await Store.FindCategoryAsync(catid);
            if (cat == null) return NotFound();

            if (model.Name == null || model.Color == null)
                return BadRequest();
            cat.Color = model.Color;
            cat.IsPublic = model.IsPublic;
            cat.Name = model.Name;
            cat.SortOrder = model.SortOrder;

            await Store.UpdateAsync(cat);
            await HttpContext.AuditAsync("updated", $"{catid}");
            return RedirectToAction(nameof(Detail), new { catid });
        }


        [HttpGet("{catid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Delete(int catid)
        {
            var desc = await Store.FindCategoryAsync(catid);
            if (desc == null) return NotFound();

            return AskPost(
                title: $"Delete team category {catid} - \"{desc.Name}\"",
                message: $"You're about to delete team category {catid} - \"{desc.Name}\".\n" +
                    "Are you sure?",
                area: "Dashboard", ctrl: "Categories", act: "Delete",
                type: MessageType.Danger);
        }


        [HttpPost("{catid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int catid, int inajax)
        {
            var desc = await Store.FindCategoryAsync(catid);
            if (desc == null) return NotFound();

            try
            {
                await Store.DeleteAsync(desc);
                StatusMessage = $"Team category {catid} deleted successfully.";
                await HttpContext.AuditAsync("deleted", $"{catid}");
            }
            catch
            {
                StatusMessage = $"Error deleting team category {catid}, foreign key constraints failed.";
            }

            return RedirectToAction(nameof(List));
        }
    }
}
