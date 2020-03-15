using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 CDS 连接的API控制器。
    /// </summary>
    [Area("Api")]
    [Route("[area]/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "CDS,Administrator")]
    [Produces("application/json")]
    public class PrintingController : ControllerBase
    {
        static readonly AsyncLock locker = new AsyncLock();

        public AppDbContext DbContext { get; set; }

        public PrintingController(AppDbContext db) => DbContext = db;

        /// <summary>
        /// Get the next printing and mark as processed
        /// </summary>
        /// <response code="200">The next printing</response>
        [HttpPost]
        public async Task<ActionResult<EntityPrintModel>> NextPrinting()
        {
            var prtQuery =
                from p in DbContext.Printing
                where p.Done == null
                join u in DbContext.Users on p.UserId equals u.Id
                into uu from u in uu.DefaultIfEmpty()
                join tu in DbContext.TeamMembers on new { p.ContestId, p.UserId } equals new { tu.ContestId, tu.UserId }
                into tuu from tu in tuu.DefaultIfEmpty()
                join t in DbContext.Teams on new { tu.ContestId, tu.TeamId } equals new { t.ContestId, t.TeamId }
                into tt from t in tt.DefaultIfEmpty()
                orderby p.Time
                select new { p, u.UserName, t.TeamName, TeamId = (int?)t.TeamId, t.Location };

            Printing print;
            string user, location;

            using (await locker.LockAsync())
            {
                var prt = await prtQuery.FirstOrDefaultAsync();
                if (prt == null) return new JsonResult("");

                prt.p.Done = false;
                DbContext.Printing.Update(prt.p);
                await DbContext.SaveChangesAsync();

                print = prt.p;
                user = prt.TeamId.HasValue ? $"t{prt.TeamId}: {prt.TeamName}" : $"u{prt.p.UserId}: {prt.UserName}";
                location = prt.Location;
            }

            return new EntityPrintModel
            {
                done = false,
                processed = true,
                filename = print.FileName,
                id = print.Id,
                lang = print.LanguageId,
                room = location,
                team = user,
                sourcecode = Convert.ToBase64String(print.SourceCode),
                time = print.Time,
            };
        }

        /// <summary>
        /// Set the printing as resolved
        /// </summary>
        /// <param name="id">The ID of the entity to get</param>
        /// <response code="200">The basic information for this printing</response>
        [HttpPost("{id}")]
        public async Task<ActionResult<EntityPrintModel>> SetDone(int id)
        {
            var count = await DbContext.Printing
                .Where(p => p.Id == id)
                .BatchUpdateAsync(p => new Printing { Done = true });
            if (count == 0) return NotFound();

            var prtQuery =
                from p in DbContext.Printing
                where p.Id == id
                join u in DbContext.Users on p.UserId equals u.Id
                into uu from u in uu.DefaultIfEmpty()
                join tu in DbContext.TeamMembers on new { p.ContestId, p.UserId } equals new { tu.ContestId, tu.UserId }
                into tuu from tu in tuu.DefaultIfEmpty()
                join t in DbContext.Teams on new { tu.ContestId, tu.TeamId } equals new { t.ContestId, t.TeamId }
                into tt from t in tt.DefaultIfEmpty()
                select new EntityPrintModel
                {
                    done = true,
                    filename = p.FileName,
                    id = p.Id,
                    lang = p.LanguageId,
                    processed = true,
                    room = t.Location,
                    time = p.Time,
                    team = t != null ? $"t{t.TeamId}: {t.TeamName}" : $"u{u.Id}: {u.UserName}"
                };

            return await prtQuery.SingleOrDefaultAsync();
        }
    }
}
