using EFCore.BulkExtensions;
using JudgeWeb.Areas.Dashboard.Models;
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
    public class LanguagesController : Controller3
    {
        public LanguagesController(AppDbContext db) : base(db) { }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View(await DbContext.Languages.ToListAsync());
        }

        [HttpGet("{langid}")]
        public async Task<IActionResult> Detail(string langid)
        {
            var lang = await DbContext.Languages
                .Where(l => l.ExternalId == langid)
                .FirstOrDefaultAsync();
            if (lang is null) return NotFound();

            var query = await DbContext.Submissions
                .Where(s => s.Language == lang.LangId)
                .OrderByDescending(a => a.SubmissionId)
                .Join(
                    inner: DbContext.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: g => new { g.SubmissionId, g.Active },
                    resultSelector: (s, g) => new { s, g })
                .Take(100)
                .ToListAsync();

            ViewBag.Language = lang;
            ViewBag.Submissions = query.Select(a => (a.s, a.g));
            return View();
        }

        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubmit(string langid)
        {
            await DbContext.Languages
                .Where(l => l.ExternalId == langid)
                .BatchUpdateAsync(l =>
                    new Language { AllowSubmit = !l.AllowSubmit });

            return RedirectToAction(nameof(Detail), new { langid });
        }

        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJudge(string langid)
        {
            await DbContext.Languages
                .Where(l => l.ExternalId == langid)
                .BatchUpdateAsync(l =>
                    new Language { AllowJudge = !l.AllowJudge });

            return RedirectToAction(nameof(Detail), new { langid });
        }

        [HttpGet("{langid}/[action]")]
        public async Task<IActionResult> Edit(string langid)
        {
            var lang = await DbContext.Languages
                .Where(l => l.ExternalId == langid)
                .FirstOrDefaultAsync();
            if (lang == null) return NotFound();

            ViewBag.Executables = await DbContext.Executable
                .Where(e => e.Type == "compile")
                .Select(e => e.ExecId)
                .ToListAsync();

            ViewBag.Operator = "Edit";
            return View(new LanguageEditModel
            {
                CompileScript = lang.CompileScript,
                ExternalId = lang.ExternalId,
                FileExtension = lang.FileExtension,
                Name = lang.Name,
                TimeFactor = lang.TimeFactor,
            });
        }

        [HttpPost("{langid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string langid, LanguageEditModel model)
        {
            var lang = await DbContext.Languages
                .Where(l => l.ExternalId == langid)
                .FirstOrDefaultAsync();
            if (lang == null) return NotFound();

            lang.CompileScript = model.CompileScript;
            lang.FileExtension = model.FileExtension;
            lang.TimeFactor = model.TimeFactor;
            lang.Name = model.Name;

            DbContext.Languages.Update(lang);
            await DbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Detail), new { langid });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Add()
        {
            ViewBag.Executables = await DbContext.Executable
                .Where(e => e.Type == "compile")
                .Select(e => e.ExecId)
                .ToListAsync();

            ViewBag.Operator = "Add";
            return View("Edit", new LanguageEditModel { TimeFactor = 1 });
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(LanguageEditModel model)
        {
            var entity = DbContext.Languages.Add(new Language
            {
                CompileScript = model.CompileScript,
                FileExtension = model.FileExtension,
                AllowJudge = false,
                AllowSubmit = false,
                ExternalId = model.ExternalId,
                Name = model.Name,
                TimeFactor = model.TimeFactor,
            });

            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { langid = entity.Entity.ExternalId });
        }
    }
}
