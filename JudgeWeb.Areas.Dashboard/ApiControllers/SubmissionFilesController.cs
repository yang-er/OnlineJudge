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
    [Route("[area]/contests/{cid}/submissions")]
    [Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(Roles = "Administrator,Judgehost,CDS")]
    [Produces("application/json")]
    public class SubmissionFilesController : ControllerBase
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        AppDbContext DbContext { get; }

        /// <summary>
        /// 构建控制器。
        /// </summary>
        /// <param name="rdbc">数据库上下文</param>
        public SubmissionFilesController(AppDbContext rdbc)
        {
            DbContext = rdbc;
        }


        /// <summary>
        /// Get the files for the given submission as a ZIP archive
        /// </summary>
        /// <param name="sid">The ID of the entity to get</param>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">The files for the submission as a ZIP archive</response>
        /// <response code="500">An error occurred while creating the ZIP file</response>
        [HttpGet("{sid}/[action]")]
        [Produces("application/zip")]
        public async Task<IActionResult> Files(int cid, int sid)
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

            var srcDecoded = Convert.FromBase64String(src.SourceCode);
            var memStream = new MemoryStream();

            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                var entry = zip.CreateEntry("Main." + src.FileExtension);
                using (var fileStream = entry.Open())
                    await fileStream.WriteAsync(srcDecoded, 0, srcDecoded.Length);
            }

            memStream.Position = 0;
            return File(memStream, "application/zip");
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
