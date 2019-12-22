using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[controller]s")]
    public class ContestController : Controller2
    {
        private AppDbContext DbContext { get; }

        private UserManager UserManager { get; }

        public ContestController(AppDbContext db, UserManager um)
        {
            DbContext = db;
            UserManager = um;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            int.TryParse(UserManager.GetUserId(User), out int uid);

            var cts = await DbContext.Contests
                .GroupJoin(
                    inner: DbContext.Teams,
                    outerKeySelector: c => c.ContestId,
                    innerKeySelector: t => t.ContestId,
                    resultSelector: (c, ts) =>
                        new ContestListModel
                        {
                            Name = c.Name,
                            RankingStrategy = c.RankingStrategy,
                            ContestId = c.ContestId,
                            EndTime = c.EndTime,
                            IsPublic = c.IsPublic,
                            StartTime = c.StartTime,
                            TeamCount = ts.Count(),
                            OpenRegister = c.RegisterDefaultCategory > 0
                        })
                .ToListAsync();

            cts.Sort();

            if (uid > 0)
            {
                var cids = await DbContext.Teams
                    .Where(t => t.UserId == uid)
                    .Select(t => t.ContestId)
                    .ToArrayAsync();
                foreach (var cid in cids)
                    cts.First(c => c.ContestId == cid).IsRegistered = true;
            }

            return View(cts);
        }
    }
}
