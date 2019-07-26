using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 实现 DOMjudge API 的测试样例控制器。
    /// </summary>
    [BasicAuthenticationFilter("DOMjudge API")]
    [Area("Api")]
    [Route("[area]/[controller]/[action]")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [Produces("application/json")]
    public class TestcasesController : ControllerBase
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
        public TestcasesController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }

        /// <summary>
        /// 判断还未执行的测试样例。
        /// </summary>
        /// <param name="jid">评测ID</param>
        [HttpGet("{jid}")]
        public async Task<IActionResult> NextToJudge(int jid)
        {
            // 先获取当前的评测信息
            var stat = await (
                from g in DbContext.Judgings
                where g.JudgingId == jid && g.Status == Verdict.Running
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                select new { g.JudgingId, s.ProblemId, g.FullTest }
            ).FirstOrDefaultAsync();
            if (stat is null) return Json("");

            // 获取当前评测通过样例信息
            var thisGrade = await DbContext.Details
                .Where(d => d.JudgingId == stat.JudgingId)
                .Select(d => new { d.Status, d.TestcaseId })
                .ToListAsync();

            // 获取该问题的所有样例信息
            var totalTest = await DbContext.Testcases
                .Where(t => t.ProblemId == stat.ProblemId)
                .OrderBy(t => t.Rank)
                .Select(t => new { t.TestcaseId, t.Rank, t.Md5sumInput, t.Md5sumOutput })
                .ToListAsync();

            // 如果不是全部测试并且存在非AC结果则停止判题
            if (!stat.FullTest && thisGrade.Any(a => a.Status != Verdict.Accepted))
                return Json("");

            // 检测有哪个样例还没跑
            // TODO: 世界上最蠢的算法，是否救得了QAQ
            foreach (var item in totalTest)
            {
                if (!thisGrade.Any(a => a.TestcaseId == item.TestcaseId))
                {
                    return Json(new TestcaseNextToJudgeModel
                    {
                        ProblemId = stat.ProblemId,
                        Rank = item.Rank,
                        Md5sumInput = item.Md5sumInput,
                        Md5sumOutput = item.Md5sumOutput,
                        TestcaseId = item.TestcaseId
                    });
                }
            }

            return Json("");
        }

        /// <summary>
        /// 从数据库中拉取一条测试样例。
        /// </summary>
        /// <param name="tcid">测试样例ID</param>
        /// <param name="filetype">文件类型</param>
        [HttpGet("/[area]/[controller]/{tcid}/[action]/{filetype}")]
        public async Task<IActionResult> File(int tcid, string filetype)
        {
            // TODO: 将测试样例缓存在本地磁盘
            var tcQuery = DbContext.Testcases
                .Where(tc => tc.TestcaseId == tcid);

            IQueryable<byte[]> bytesQuery;
            if (filetype == "input")
                bytesQuery = tcQuery.Select(t => t.Input);
            else if (filetype == "output")
                bytesQuery = tcQuery.Select(t => t.Output);
            else return BadRequest();

            var bytes = await bytesQuery.FirstOrDefaultAsync();
            if (bytes is null) return NotFound();
            var base64encoded = Convert.ToBase64String(bytes);
            return Json(base64encoded);
        }
    }
}
