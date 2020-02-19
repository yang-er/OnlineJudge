using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class PrintingsController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int cid, int limit = 100)
        {
            var printQuery =
                from p in DbContext.Printing
                where p.ContestId == cid
                join u in DbContext.Users on p.UserId equals u.Id
                into uu from u in uu.DefaultIfEmpty()
                join tu in DbContext.TeamMembers on new { p.ContestId, p.UserId } equals new { tu.ContestId, tu.UserId }
                into tuu from tu in tuu.DefaultIfEmpty()
                join t in DbContext.Teams on new { tu.ContestId, tu.TeamId } equals new { t.ContestId, t.TeamId }
                into tt from t in tt.DefaultIfEmpty()
                orderby p.Time descending
                select new Models.ShowPrintModel
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    Language = p.LanguageId,
                    Done = p.Done,
                    Location = t.Location,
                    Time = p.Time,
                    TeamName = (t == null ? $"u{u.Id} - {u.UserName}" : $"t{t.TeamId} - {t.TeamName}")
                };

            var prints = await printQuery.Take(limit).ToListAsync();
            return View(prints);
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Done(int cid, int fid)
        {
            int cnt = await DbContext.Printing
                .Where(p => p.ContestId == cid && p.Id == fid)
                .Where(p => p.Done == null || p.Done == false)
                .BatchUpdateAsync(p => new Printing { Done = true });
            if (cnt == 0) return NotFound();
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Undone(int cid, int fid)
        {
            int cnt = await DbContext.Printing
                .Where(p => p.ContestId == cid && p.Id == fid)
                .Where(p => p.Done == true)
                .BatchUpdateAsync(new Printing(), new List<string> { "Done" });
            if (cnt == 0) return NotFound();
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{fid}/[action]")]
        public async Task<IActionResult> Download(int cid, int fid)
        {
            var item = await DbContext.Printing
                .Where(p => p.ContestId == cid && p.Id == fid)
                .SingleOrDefaultAsync();
            if (item == null) return NotFound();
            return File(item.SourceCode, "text/plain", item.FileName);
        }
    }
}
