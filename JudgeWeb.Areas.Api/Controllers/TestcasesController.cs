using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 实现 DOMjudge API 的测试样例控制器。
    /// </summary>
    [Area("Api")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "Judgehost")]
    [Route("[area]/[controller]")]
    [Produces("application/json")]
    public class TestcasesController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构建控制器。
        /// </summary>
        /// <param name="rdbc">数据库上下文</param>
        public TestcasesController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }

        /// <summary>
        /// Json返回空字符串
        /// </summary>
        private JsonResult JsonEmpty() => new JsonResult("");


        /// <summary>
        /// Get the next to judge testcase for the given judging ID
        /// </summary>
        /// <param name="id">The ID of the entity to get</param>
        [HttpGet("[action]/{id}")]
        public async Task<ActionResult<TestcaseToJudge>> NextToJudge(int id)
        {
            // 先获取当前的评测信息
            var stat = await (
                from g in DbContext.Judgings
                where g.JudgingId == id && g.Status == Verdict.Running
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                select new { g.JudgingId, s.ProblemId, g.FullTest }
            ).FirstOrDefaultAsync();
            if (stat is null) return JsonEmpty();

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
                return JsonEmpty();

            // 检测有哪个样例还没跑
            // TODO: 世界上最蠢的算法，是否救得了QAQ
            foreach (var item in totalTest)
            {
                if (!thisGrade.Any(a => a.TestcaseId == item.TestcaseId))
                {
                    return new TestcaseToJudge
                    {
                        probid = stat.ProblemId,
                        rank = item.Rank,
                        md5sum_input = item.Md5sumInput,
                        md5sum_output = item.Md5sumOutput,
                        testcaseid = item.TestcaseId
                    };
                }
            }

            return JsonEmpty();
        }


        /// <summary>
        /// Get the input or output file for the given testcase
        /// </summary>
        /// <param name="id">The ID of the entity to get</param>
        /// <param name="type">Type of file to get</param>
        /// <response code="200">Information about the file of the given testcase</response>
        [HttpGet("{id}/[action]/{type}")]
        public async Task<ActionResult<string>> File(int id, string type)
        {
            var tcq = await DbContext.Testcases
                .Where(tc => tc.TestcaseId == id)
                .Select(tc => new { pid = tc.ProblemId })
                .FirstOrDefaultAsync();
            if (tcq is null) return NotFound();

            if (type == "input") type = "in";
            else if (type == "output") type = "out";
            else return BadRequest();

            var io = HttpContext.RequestServices
                .GetRequiredService<IFileRepository>();
            io.SetContext("Problems");

            var bytes = await io.ReadBinaryAsync($"p{tcq.pid}", $"t{id}.{type}");
            if (bytes is null) return NotFound();
            return Convert.ToBase64String(bytes);
        }
    }
}
