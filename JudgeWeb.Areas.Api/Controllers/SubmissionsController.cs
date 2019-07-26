using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 实现 DOMjudge API 的提交控制器。
    /// </summary>
    [BasicAuthenticationFilter("DOMjudge API")]
    [Area("Api")]
    [Route("[area]/contests/{cid}/[controller]/{sid}/[action]")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public class SubmissionsController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 将对象转为Json输出。
        /// </summary>
        /// <param name="value">对象</param>
        private JsonResult Json(object value)
        {
            return new JsonResult(value);
        }

        /// <summary>
        /// 构建控制器。
        /// </summary>
        /// <param name="rdbc">数据库上下文</param>
        public SubmissionsController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }

        /// <summary>
        /// 获取某个提交记录的源代码。
        /// </summary>
        /// <param name="cid">比赛ID</param>
        /// <param name="sid">提交ID</param>
        [HttpGet]
        public async Task<IActionResult> SourceCode(int cid, int sid)
        {
            var src = await (
                from s in DbContext.Submissions
                where s.SubmissionId == sid && s.ContestId == cid
                join l in DbContext.Languages on s.Language equals l.LangId
                select new { s.SourceCode, l.FileExtension }
            ).FirstOrDefaultAsync();

            if (src is null) return NotFound();
            return Json(new[]
            {
                new
                {
                    id = sid.ToString(),
                    submission_id = sid.ToString(),
                    filename = "Main." + src.FileExtension,
                    source = src.SourceCode
                }
            });
        }
    }
}
