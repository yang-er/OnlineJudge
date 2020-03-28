using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Contests.ApiModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public IPrintingStore Store { get; }
        public PrintingController(IPrintingStore store) => Store = store;


        /// <summary>
        /// Get the next printing and mark as processed
        /// </summary>
        /// <response code="200">The next printing</response>
        [HttpPost]
        public async Task<ActionResult<Printing>> NextPrinting()
        {
            Data.Printing print;
            string user, location;

            using (await locker.LockAsync())
            {
                var query = await Store.ListAsync(1, -1,
                    predicate: p => p.Done == null,
                    expression: (p, u, t) => new { p, u.UserName, t.TeamName, TeamId = (int?)t.TeamId, t.Location });
                if (query.Count == 0) return new JsonResult("");

                var prt = query.Single();
                await Store.SetStateAsync(prt.p.Id, false);

                print = prt.p;
                user = prt.TeamId.HasValue ? $"t{prt.TeamId}: {prt.TeamName}" : $"u{prt.p.UserId}: {prt.UserName}";
                location = prt.Location;
            }

            return new Printing
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
        public async Task<ActionResult<Printing>> SetDone(int id)
        {
            var result = await Store.SetStateAsync(id, true);
            if (!result) return NotFound();

            var items = await Store.ListAsync(1, 1,
                predicate: p => p.Id == id,
                expression: (p, u, t) => new Printing
                {
                    done = true,
                    filename = p.FileName,
                    id = p.Id,
                    lang = p.LanguageId,
                    processed = true,
                    room = t.Location,
                    time = p.Time,
                    team = t != null ? $"t{t.TeamId}: {t.TeamName}" : $"u{u.Id}: {u.UserName}"
                });

            return items.SingleOrDefault();
        }
    }
}
