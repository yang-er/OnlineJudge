﻿using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    public class ContestController : Controller2
    {
        private AppDbContext DbContext { get; }

        private UserManager UserManager { get; }

        public ContestController(AppDbContext db, UserManager um)
        {
            DbContext = db;
            UserManager = um;
        }

        [HttpGet("/contests")]
        public async Task<IActionResult> List()
        {
            int.TryParse(UserManager.GetUserId(User), out int uid);

            var cts = await DbContext.CachedGetAsync("cont::list", TimeSpan.FromMinutes(5), async () =>
            {
                var contests = await DbContext.Contests
                    .Where(c => !c.Gym)
                    .Select(c => new ContestListModel
                    {
                        Name = c.Name,
                        RankingStrategy = c.RankingStrategy,
                        ContestId = c.ContestId,
                        EndTime = c.EndTime,
                        IsPublic = c.IsPublic,
                        StartTime = c.StartTime,
                        OpenRegister = c.RegisterDefaultCategory > 0
                    })
                    .ToListAsync();

                var teamQuery =
                    from t in DbContext.Teams
                    join c in DbContext.TeamCategories on t.CategoryId equals c.CategoryId
                    where c.IsPublic && t.Status < 3
                    group 1 by t.ContestId into g
                    select new { g.Key, Count = g.Count() };
                var results = await teamQuery.ToDictionaryAsync(a => a.Key, a => a.Count);
                contests.ForEach(c => c.TeamCount = results.GetValueOrDefault(c.ContestId));

                contests.Sort();
                return contests;
            });

            ViewBag.RegisteredContests = (await DbContext.TeamMembers
                .Where(t => t.UserId == uid)
                .Select(t => t.ContestId)
                .ToArrayAsync()).ToHashSet();

            return View(cts);
        }

        [HttpGet("/gyms")]
        public async Task<IActionResult> ListGyms()
        {
            int.TryParse(UserManager.GetUserId(User), out int uid);

            var cts = await DbContext.CachedGetAsync("gym::list", TimeSpan.FromMinutes(5), async () =>
            {
                var contests = await DbContext.Contests
                    .Where(c => c.Gym)
                    .Select(c => new ContestListModel
                    {
                        Name = c.Name,
                        RankingStrategy = c.RankingStrategy,
                        ContestId = c.ContestId,
                        EndTime = c.EndTime,
                        IsPublic = c.IsPublic,
                        StartTime = c.StartTime,
                        OpenRegister = c.RegisterDefaultCategory > 0
                    })
                    .ToListAsync();

                var teamQuery =
                    from t in DbContext.Teams
                    join c in DbContext.TeamCategories on t.CategoryId equals c.CategoryId
                    where c.IsPublic && t.Status < 3
                    group 1 by t.ContestId into g
                    select new { g.Key, Count = g.Count() };
                var results = await teamQuery.ToDictionaryAsync(a => a.Key, a => a.Count);
                contests.ForEach(c => c.TeamCount = results.GetValueOrDefault(c.ContestId));

                contests.Sort();
                return contests;
            });

            ViewBag.RegisteredContests = (await DbContext.TeamMembers
                .Where(t => t.UserId == uid)
                .Select(t => t.ContestId)
                .ToArrayAsync()).ToHashSet();

            return View(cts);
        }
    }
}
