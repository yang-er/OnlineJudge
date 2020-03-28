using JudgeWeb.Domains.Contests;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class PrintingsController : JuryControllerBase
    {
        IPrintingStore Store { get; }

        public PrintingsController(IPrintingStore store)
        {
            Store = store;
        }


        [HttpGet]
        public async Task<IActionResult> List(int cid, int limit = 100)
        {
            return View(await Store.ListAsync(limit, 1,
                predicate: p => p.ContestId == cid,
                expression: (p, u, t) => new Models.ShowPrintModel
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    Language = p.LanguageId,
                    Done = p.Done,
                    Location = t.Location,
                    Time = p.Time,
                    TeamName = (t == null ? $"u{u.Id} - {u.UserName}" : $"t{t.TeamId} - {t.TeamName}")
                }));
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Done(int cid, int fid)
        {
            bool result = await Store.SetStateAsync(cid, fid, true);
            if (!result) return NotFound();
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Undone(int cid, int fid)
        {
            bool result = await Store.SetStateAsync(cid, fid, null);
            if (!result) return NotFound();
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Download(int cid, int fid)
        {
            var items = await Store.ListAsync(1, 1,
                predicate: p => p.ContestId == cid && p.Id == fid,
                expression: (p, u, t) => new { p.FileName, p.SourceCode });
            if (items.Count == 0) return NotFound();
            var item = items.Single();
            return File(item.SourceCode, "text/plain", item.FileName);
        }
    }
}
