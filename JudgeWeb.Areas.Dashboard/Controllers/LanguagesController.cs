using JudgeWeb.Areas.Dashboard.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Administrator")]
    [Route("[area]/[controller]")]
    public class LanguagesController : Controller3
    {
        private ILanguageStore Store { get; }
        public LanguagesController(IProblemFacade facade)
            => Store = facade.LanguageStore;


        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await Store.ListAsync());
        }


        [HttpGet("{langid}")]
        public async Task<IActionResult> Detail(string langid,
            [FromServices] ISubmissionStore submissions)
        {
            var lang = await Store.FindAsync(langid);
            if (lang is null) return NotFound();

            ViewBag.Language = lang;
            ViewBag.Submissions = await submissions
                .ListWithJudgingAsync(s => s.Language == langid, limits: 100);
            return View();
        }


        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubmit(string langid)
        {
            await Store.UpdateAsync(
                predicate: l => l.Id == langid,
                update: l => new Language { AllowSubmit = !l.AllowSubmit });

            await HttpContext.AuditAsync("toggle allow submit", langid);
            return RedirectToAction(nameof(Detail), new { langid });
        }


        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJudge(string langid)
        {
            await Store.UpdateAsync(
                predicate: l => l.Id == langid,
                update: l => new Language { AllowJudge = !l.AllowJudge });

            await HttpContext.AuditAsync("toggle allow judge", langid);
            return RedirectToAction(nameof(Detail), new { langid });
        }


        [HttpGet("{langid}/[action]")]
        public async Task<IActionResult> Edit(string langid)
        {
            var lang = await Store.FindAsync(langid);
            if (lang == null) return NotFound();
            ViewBag.Executables = await Store.ListCompilersAsync();

            ViewBag.Operator = "Edit";
            return View(new LanguageEditModel
            {
                CompileScript = lang.CompileScript,
                ExternalId = lang.Id,
                FileExtension = lang.FileExtension,
                Name = lang.Name,
                TimeFactor = lang.TimeFactor,
            });
        }


        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string langid, LanguageEditModel model)
        {
            var lang = await Store.FindAsync(langid);
            if (lang == null) return NotFound();

            lang.CompileScript = model.CompileScript;
            lang.FileExtension = model.FileExtension;
            lang.TimeFactor = model.TimeFactor;
            lang.Name = model.Name;

            await Store.UpdateAsync(lang);
            await HttpContext.AuditAsync("updated", langid);
            return RedirectToAction(nameof(Detail), new { langid });
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Add()
        {
            ViewBag.Executables = await Store.ListCompilersAsync();
            ViewBag.Operator = "Add";
            return View("Edit", new LanguageEditModel { TimeFactor = 1 });
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(LanguageEditModel model)
        {
            var entity = await Store.CreateAsync(new Language
            {
                CompileScript = model.CompileScript,
                FileExtension = model.FileExtension,
                AllowJudge = false,
                AllowSubmit = false,
                Id = model.ExternalId,
                Name = model.Name,
                TimeFactor = model.TimeFactor,
            });

            await HttpContext.AuditAsync("added", entity.Id);
            return RedirectToAction(nameof(Detail), new { langid = entity.Id });
        }
    }
}
