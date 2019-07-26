using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 用于和 DOMjudge 对接的API控制器。
    /// </summary>
    [BasicAuthenticationFilter("DOMjudge API")]
    [Area("Api")]
    [Route("[area]/[action]")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public class GeneralController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构造 DOMjudge API 的控制器。
        /// </summary>
        /// <param name="rdbc">数据库</param>
        public GeneralController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }

        private JsonResult Json(object value)
        {
            return new JsonResult(value);
        }

        /// <summary>
        /// 获取当前 DOMjudge API 版本。
        /// </summary>
        [HttpGet]
        public IActionResult Version()
        {
            return Json(new { api_version = 4 });
        }

        /// <summary>
        /// 获取评测队列统计。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var judgingStatus = await DbContext.Judgings
                .GroupBy(g => g.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return Json(new[]
            {
                new
                {
                    cid = 0,
                    num_submissions = judgingStatus
                        .Sum(a => a.Count),
                    num_queued = judgingStatus
                        .Where(a => a.Status == Verdict.Pending)
                        .Select(a => a.Count)
                        .FirstOrDefault(),
                    num_judging = judgingStatus
                        .Where(a => a.Status == Verdict.Running)
                        .Select(a => a.Count)
                        .FirstOrDefault(),
                }
            });
        }

        /// <summary>
        /// 获取数据库中某一个可执行程序。
        /// </summary>
        /// <param name="target">可执行程序名</param>
        [HttpGet("{target}")]
        public async Task<IActionResult> Executables(string target)
        {
            var zipContent = await DbContext.Executable
                .Where(e => e.ExecId == target)
                .Select(e => e.ZipFile)
                .FirstOrDefaultAsync();
            if (zipContent is null) return NotFound();
            var base64encoded = Convert.ToBase64String(zipContent);
            return Json(base64encoded);
        }

        /// <summary>
        /// 获取数据库中的设置。
        /// </summary>
        /// <param name="name">设置名称</param>
        [HttpGet]
        public async Task<IActionResult> Config(string name)
        {
            var jo = new JObject();
            var query = DbContext.Configures
                .Select(c => new { c.Name, c.Value });

            if (name != null)
            {
                query = query.Where(c => c.Name == name);
            }

            var value = await query.ToListAsync();
            value.ForEach(a => jo[a.Name] = JToken.Parse(a.Value));
            return Json(jo);
        }
    }
}
