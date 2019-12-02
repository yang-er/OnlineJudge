using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Api.Controllers
{
    /// <summary>
    /// 实现 DOMjudge API 的提交控制器。
    /// </summary>
    [Area("Api")]
    [Route("[area]/contests/{cid}/[controller]")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "Administrator,Judgehost")]
    [Produces("application/json")]
    public class SubmissionsController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构建控制器。
        /// </summary>
        /// <param name="rdbc">数据库上下文</param>
        public SubmissionsController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }


        /// <summary>
        /// Get the source code of all the files for the given submission
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="sid">The ID of the entity to get</param>
        /// <response code="200">The files for the submission</response>
        [HttpGet("{sid}/[action]")]
        public async Task<ActionResult<SubmissionFile[]>> SourceCode(int cid, int sid)
        {
            var src = await DbContext.Submissions
                .Where(s => s.SubmissionId == sid && s.ContestId == cid)
                .Join(
                    inner: DbContext.Languages,
                    outerKeySelector: s => s.Language,
                    innerKeySelector: l => l.LangId,
                    resultSelector: (s, l) => new { s.SourceCode, l.FileExtension })
                .FirstOrDefaultAsync();

            if (src is null) return NotFound();

            return new[]
            {
                new SubmissionFile
                {
                    id = sid.ToString(),
                    submission_id = sid.ToString(),
                    filename = "Main." + src.FileExtension,
                    source = src.SourceCode
                }
            };
        }
    }
}
