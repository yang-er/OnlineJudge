using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Domains.Identity;
using JudgeWeb.Features.OjUpdate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    /// <summary>
    /// 队伍管理相关的控制器。
    /// </summary>
    [Area("Misc")]
    [Authorize(Roles = "Student,Administrator")]
    public class TeamController : Controller2
    {
        [HttpGet("/ranklist/{name}/{year?}")]
        public async Task<IActionResult> Ranklist(
            [FromServices] UserManager userManager,
            string name, int year = -1)
        {
            if (!OjUpdateService.OjList.ContainsKey(name))
                return NotFound();
            var oj = OjUpdateService.OjList[name];
            var title = name + " Ranklist";
            if (year != -1) title += " " + year;
            ViewData["Title"] = title;

            var ojac = await userManager.GetRanklistAsync(oj.CategoryId, year);
            return View(new RanklistViewModel
            {
                OjName = name,
                LastUpdate = oj.LastUpdate ?? DateTimeOffset.UnixEpoch,
                IsUpdating = oj.IsUpdating,
                RankTemplate = oj.RankTemplate,
                AccountTemplate = oj.AccountTemplate,
                CountColumn = oj.ColumnName,
                OjAccounts = ojac,
            });
        }


        [HttpGet("/ranklist/{oj}/[action]")]
        [ValidateInAjax]
        public IActionResult Refresh(string oj)
        {
            if (!OjUpdateService.OjList.ContainsKey(oj))
            {
                return BadRequest();
            }
            else
            {
                OjUpdateService.OjList[oj].RequestUpdate();
                return Message("Ranklist Refresh", "Ranklist will be refreshed in minutes...\n" +
                    "Please refresh this page a minute later.");
            }
        }
    }
}