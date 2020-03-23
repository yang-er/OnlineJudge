using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.Buffers;
using System.IO;
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
        private ITestcaseStore Testcases { get; }

        private IJudgingStore Judgings { get; }

        private JsonResult JsonEmpty() => new JsonResult("");

        public TestcasesController(IProblemFacade facade, IJudgementFacade facade2)
        {
            Testcases = facade.Testcases;
            Judgings = facade2.Judgings;
        }


        /// <summary>
        /// Get the next to judge testcase for the given judging ID
        /// </summary>
        /// <param name="id">The ID of the entity to get</param>
        [HttpGet("[action]/{id}")]
        public async Task<ActionResult<TestcaseToJudge>> NextToJudge(int id)
        {
            // 先获取当前的评测信息
            var stats = await Judgings.ListAsync(
                predicate: j => j.JudgingId == id && j.Status == Verdict.Running,
                selector: j => new
                {
                    j.JudgingId,
                    j.s.ProblemId,
                    j.FullTest,
                    j.s.p.AllowJudge
                },
                topCount: 1);

            var stat = stats.SingleOrDefault();
            if (stat is null || !stat.AllowJudge) return JsonEmpty();

            var result = await Judgings.GetDetailsAsync(
                problemId: stat.ProblemId, judgingId: id,
                selector: (t, d) => new { Status = (Verdict?)d.Status, t });

            if (!stat.FullTest && result.Any(
                    s => s.Status.HasValue
                    && s.Status != Verdict.Accepted))
                return JsonEmpty();

            var item = result.FirstOrDefault(a => !a.Status.HasValue);
            if (item == null) return JsonEmpty();

            return new TestcaseToJudge(item.t);
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
            if (type == "input") type = "in";
            else if (type == "output") type = "out";
            else return BadRequest();

            var tc = await Testcases.FindAsync(id);
            if (tc is null) return NotFound();

            var fileInfo = Testcases.GetFile(tc, type);
            if (!fileInfo.Exists) return NotFound();
            // return Convert.ToBase64String(
            //     await fileInfo.ReadBinaryAsync());
            return new Base64StreamResult(fileInfo);
        }

        
        private class Base64StreamResult : ActionResult
        {
            public IFileInfo FileInfo { get; set; }

            public Base64StreamResult(IFileInfo file) => FileInfo = file;

            const int byteLen = 1024 * 3 * 256;
            const int charLen = 1024 * 4 * 256;
            private static readonly ArrayPool<byte> opts = ArrayPool<byte>.Create(byteLen, 16);
            private static readonly ArrayPool<char> ress = ArrayPool<char>.Create(charLen, 16);

            public override async Task ExecuteResultAsync(ActionContext context)
            {
                var response = context.HttpContext.Response;
                response.StatusCode = 200;
                response.ContentType = "application/json";
                response.ContentLength = (FileInfo.Length + 2) / 3 * 4 + 2;
                using var f1 = FileInfo.CreateReadStream();

                byte[] opt = opts.Rent(byteLen);
                char[] res = ress.Rent(charLen);
                var sw = new StreamWriter(response.Body);
                await sw.WriteAsync('"');

                while (true)
                {
                    int len = await f1.ReadAsync(opt, 0, byteLen);
                    if (len == 0) break;
                    int len2 = Convert.ToBase64CharArray(opt, 0, len, res, 0);
                    await sw.WriteAsync(res, 0, len2);
                }

                await sw.WriteAsync('"');
                await sw.DisposeAsync();
                opts.Return(opt);
                ress.Return(res);
            }
        }
    }
}
