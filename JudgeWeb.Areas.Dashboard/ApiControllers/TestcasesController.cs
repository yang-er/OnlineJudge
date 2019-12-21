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
            var statusQuery =
                from j in DbContext.Judgings
                where j.JudgingId == id && j.Status == Verdict.Running
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                select new { j.JudgingId, s.ProblemId, j.FullTest, p.AllowJudge };
            var stat = await statusQuery.FirstOrDefaultAsync();
            if (stat is null || !stat.AllowJudge) return JsonEmpty();

            // 没有什么是一个join不能解决的，如果有，那就join两次
            var testcasesQuery =
                from t in DbContext.Testcases
                where t.ProblemId == stat.ProblemId
                join d in DbContext.Details
                    on new { t.TestcaseId, JudgingId = id }
                    equals new { d.TestcaseId, d.JudgingId }
                    into ds
                from d in ds.DefaultIfEmpty()
                orderby t.Rank ascending
                select new { Status = (Verdict?)d.Status, t.TestcaseId,
                    t.Rank, t.Md5sumInput, t.Md5sumOutput };
            var result = await testcasesQuery.ToListAsync();

            if (!stat.FullTest && result.Any(
                    s => s.Status.HasValue
                    && s.Status != Verdict.Accepted))
                return JsonEmpty();

            var item = result.FirstOrDefault(a => !a.Status.HasValue);
            if (item == null) return JsonEmpty();

            return new TestcaseToJudge
            {
                probid = stat.ProblemId,
                rank = item.Rank,
                md5sum_input = item.Md5sumInput,
                md5sum_output = item.Md5sumOutput,
                testcaseid = item.TestcaseId
            };
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
