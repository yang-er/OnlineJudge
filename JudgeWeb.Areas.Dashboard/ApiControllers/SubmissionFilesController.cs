﻿using JudgeWeb.Areas.Api.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Compression;
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
    [ControllerName("Submissions")]
    public class SubmissionFilesController : ControllerBase
    {
        /// <summary>
        /// Get the files for the given submission as a ZIP archive
        /// </summary>
        /// <param name="sid">The ID of the entity to get</param>
        /// <param name="submissions"></param>
        /// <param name="cid">The contest ID</param>
        /// <response code="200">The files for the submission as a ZIP archive</response>
        /// <response code="500">An error occurred while creating the ZIP file</response>
        [HttpGet("{sid}/[action]")]
        [Produces("application/zip")]
        public async Task<IActionResult> Files(int cid, int sid,
            [FromServices] ISubmissionStore submissions)
        {
            var src = await submissions.GetFileAsync(sid);
            if (src == null) return NotFound();

            var srcDecoded = Convert.FromBase64String(src.Value.src);
            var memStream = new MemoryStream();

            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
                zip.CreateEntryFromByteArray(srcDecoded, "Main." + src.Value.ext);
            memStream.Position = 0;
            return File(memStream, "application/zip");
        }


        /// <summary>
        /// Get the source code of all the files for the given submission
        /// </summary>
        /// <param name="cid">The contest ID</param>
        /// <param name="sid">The ID of the entity to get</param>
        /// <param name="submissions"></param>
        /// <response code="200">The files for the submission</response>
        [HttpGet("{sid}/[action]")]
        public async Task<ActionResult<SubmissionFile[]>> SourceCode(int cid, int sid,
            [FromServices] ISubmissionStore submissions)
        {
            var src = await submissions.GetFileAsync(sid);
            if (src == null) return NotFound();

            return new[]
            {
                new SubmissionFile
                {
                    id = sid.ToString(),
                    submission_id = sid.ToString(),
                    filename = "Main." + src.Value.ext,
                    source = src.Value.src
                }
            };
        }
    }
}
