using JudgeWeb.Areas.Judge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [Authorize(Roles = privilege)]
        [HttpGet("{pid}/{tid}/{filetype}")]
        public IActionResult Testcase(int pid, int tid, string filetype)
        {
            if (filetype == "input") filetype = "in";
            else if (filetype == "output") filetype = "out";
            else return NotFound();

            var fileSource = TestcaseManager.Download(pid, tid, filetype);
            if (fileSource != null)
                return ContentFile(fileSource, "application/octet-stream",
                    $"p{pid}.t{tid}.{filetype}");
            return NotFound();
        }

        [HttpPost("{pid}/{ttid}")]
        [Authorize(Roles = privilege)]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1 << 30, KeyLengthLimit = 1 << 30,
            MultipartBoundaryLengthLimit = 1 << 30, MultipartHeadersCountLimit = 1 << 30,
            MultipartHeadersLengthLimit = 1 << 30, BufferBodyLengthLimit = 1 << 30,
            ValueCountLimit = 1 << 30, ValueLengthLimit = 1 << 30)]
        public async Task<IActionResult> Testcase(int pid, string ttid, TestcaseUploadModel model)
        {
            try
            {
                if (ttid == "add")
                {
                    var input = await model.InputContent.ReadAsync();
                    var output = await model.OutputContent.ReadAsync();

                    int tcid = await TestcaseManager.CreateAsync(pid,
                        input, output, model.IsSecret, model.Description, User);

                    return Message(
                        "Testcase Import",
                        "Testcase added successfully. " +
                        $"New Testcase ID: {tcid}. " +
                        "Refresh this page to see it.",
                        MessageType.Success);
                }
                else
                {
                    if (!int.TryParse(ttid, out int tid))
                        return BadRequest();

                    var last = await TestcaseManager.GetAsync(pid, tid);
                    if (last == null) return NotFound();

                    (byte[], string)? input = null, output = null;
                    if (model.InputContent != null)
                        input = await model.InputContent.ReadAsync();
                    if (model.OutputContent != null)
                        output = await model.OutputContent.ReadAsync();
                    await TestcaseManager.EditAsync(last, input, output,
                        model.IsSecret, model.Description, User);

                    return Message(
                        "Testcase Edit",
                        $"Testcase #{tid} modified successfully. " +
                        "Refresh this page to see it.",
                        MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                return Message(
                    "Testcase Upload",
                    "Upload failed. Please contact XiaoYang. " + ex,
                    MessageType.Danger);
            }
        }

        [Authorize(Roles = privilege)]
        [HttpGet("{pid}/{ttid}")]
        public async Task<IActionResult> Testcase(int pid, string ttid)
        {
            ViewData["ttid"] = ttid;
            ViewData["pid"] = pid;

            if (ttid == "add")
            {
                return Window(new TestcaseUploadModel
                {
                    Description = "",
                    IsSecret = true
                });
            }
            else if (int.TryParse(ttid, out int tid))
            {
                var t = await TestcaseManager.GetAsync(pid, tid);
                if (t == null) return NotFound();

                var model = new TestcaseUploadModel
                {
                    Description = t.Description,
                    IsSecret = t.IsSecret,
                    Point = t.Point,
                };

                return Window(model);
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
