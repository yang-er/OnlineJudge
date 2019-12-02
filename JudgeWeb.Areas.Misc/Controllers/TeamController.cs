using JudgeWeb.Features.OjUpdate;
using JudgeWeb.Areas.Misc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace JudgeWeb.Areas.Misc.Controllers
{
    /// <summary>
    /// 队伍管理相关的控制器。
    /// </summary>
    [Area("Misc")]
    [Authorize(Roles = "Student")]
    [Route("[controller]/[action]")]
    public class TeamController : Controller2
    {
        [HttpGet("{year?}")]
        public IActionResult Index(int year = -1)
        {
            return RedirectToAction(nameof(Ranklist), new { name = "HDOJ" });
        }

        /// <summary>
        /// 根据年份和OJ名称获取排名。
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="name">OJ名称</param>
        [HttpGet("{name}/{year?}")]
        public IActionResult Ranklist(string name, int year = -1)
        {
            if (!OjUpdateService.OjList.ContainsKey(name))
                return NotFound();
            var oj = OjUpdateService.OjList[name];
            var title = name + " Ranklist";
            if (year != -1) title += " " + year;
            ViewData["Title"] = title;

            return View(new RanklistViewModel
            {
                OjName = name,
                LastUpdate = oj.LastUpdate,
                IsUpdating = oj.IsUpdating,
                RankTemplate = oj.RankTemplate,
                AccountTemplate = oj.AccountTemplate,
                CountColumn = oj.ColumnName,
                OjAccounts = oj.GetAccounts(year),
            });
        }

        /// <summary>
        /// 从AJAX请求刷新内容。
        /// </summary>
        /// <param name="oj">对应的OJ</param>
        [HttpGet("{oj}")]
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