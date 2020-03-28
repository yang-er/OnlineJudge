using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    [AuditPoint(AuditlogType.Testcase)]
    public class TestcasesController : Controller3
    {
        public ITestcaseStore Store { get; }

        public TestcasesController(ITestcaseStore store)
        {
            Store = store;
        }


        [HttpGet]
        public async Task<IActionResult> Testcases(int pid)
        {
            ViewBag.Testcases = await Store.ListAsync(pid);
            return View(Problem);
        }


        [HttpGet("{tid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int pid, int tid)
        {
            var tc = await Store.FindAsync(pid, tid);
            if (tc == null) return NotFound();

            ViewData["pid"] = pid;
            ViewData["tid"] = tid;
            ViewData["Title"] = $"Edit testcase t{tid}";

            return Window(new TestcaseUploadModel
            {
                Description = tc.Description,
                IsSecret = tc.IsSecret,
                Point = tc.Point,
            });
        }


        [HttpPost("{tid}/[action]")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits2(1 << 30)]
        public async Task<IActionResult> Edit(int pid, int tid, TestcaseUploadModel model)
        {
            try
            {
                var last = await Store.FindAsync(pid, tid);
                if (last == null) return NotFound();

                (byte[], string)? input = null, output = null;
                if (model.InputContent != null)
                    input = await model.InputContent.ReadAsync();
                if (model.OutputContent != null)
                    output = await model.OutputContent.ReadAsync();

                if (input.HasValue)
                {
                    last.Md5sumInput = input.Value.Item2;
                    last.InputLength = input.Value.Item1.Length;
                    await Problems.WriteFileAsync(Problem, $"t{tid}.in", input.Value.Item1);
                }

                if (output.HasValue)
                {
                    last.Md5sumOutput = output.Value.Item2;
                    last.OutputLength = output.Value.Item1.Length;
                    await Problems.WriteFileAsync(Problem, $"t{tid}.out", output.Value.Item1);
                }

                last.Description = model.Description ?? last.Description;
                last.IsSecret = model.IsSecret;
                last.Point = model.Point;
                await Store.UpdateAsync(last);

                await HttpContext.AuditAsync("modified", $"{last.TestcaseId}");
                StatusMessage = $"Testcase t{tid} updated successfully.";
                return RedirectToAction(nameof(Testcases));
            }
            catch (Exception ex)
            {
                return Message(
                    "Testcase Upload",
                    "Upload failed. Please contact XiaoYang. " + ex,
                    MessageType.Danger);
            }
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Create(int pid)
        {
            ViewData["pid"] = pid;
            ViewData["Title"] = "Add new testcase";
            return Window("Edit", new TestcaseUploadModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        [ValidateInAjax]
        [RequestSizeLimit(1 << 30)]
        [RequestFormLimits2(1 << 30)]
        public async Task<IActionResult> Create(int pid, TestcaseUploadModel model)
        {
            if (model.InputContent == null)
                return Message("Create testcase", "No input file specified.", MessageType.Danger);
            if (model.OutputContent == null)
                return Message("Create testcase", "No output file specified.", MessageType.Danger);

            try
            {
                var input = await model.InputContent.ReadAsync();
                var output = await model.OutputContent.ReadAsync();
                int rk = await Store.CountAsync(Problem);

                var e = await Store.CreateAsync(new Testcase
                {
                    Description = model.Description ?? "1",
                    IsSecret = model.IsSecret,
                    Point = model.Point,
                    Md5sumInput = input.Item2,
                    Md5sumOutput = output.Item2,
                    InputLength = input.Item1.Length,
                    OutputLength = output.Item1.Length,
                    ProblemId = pid,
                    Rank = rk + 1
                });

                int tid = e.TestcaseId;

                await Problems.WriteFileAsync(Problem, $"t{tid}.in", input.Item1);
                await Problems.WriteFileAsync(Problem, $"t{tid}.out", output.Item1);
                await HttpContext.AuditAsync("modified", $"{tid}");
                StatusMessage = $"Testcase t{tid} created successfully.";
                return RedirectToAction(nameof(Testcases));
            }
            catch (Exception ex)
            {
                return Message(
                    "Testcase Upload",
                    "Upload failed. Please contact XiaoYang. " + ex,
                    MessageType.Danger);
            }
        }


        [HttpGet("{tid}/[action]")]
        [ValidateInAjax]
        public IActionResult Delete(int tid)
        {
            return AskPost(
                title: "Delete testcase t" + tid,
                message: "You're about to delete testcase t" + tid + ". Are you sure? " +
                    "This operation is irreversible, and will make heavy load and data loss.",
                area: "Polygon", ctrl: "Testcases", act: "Delete",
                routeValues: new { pid = Problem.ProblemId, tid },
                type: MessageType.Danger);
        }


        [HttpPost("{tid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int pid, int tid)
        {
            var tc = await Store.FindAsync(pid, tid);
            if (tc == null) return NotFound();

            int dts = await Store.CascadeDeleteAsync(tc);
            await HttpContext.AuditAsync("deleted", $"p{pid}t{tid}");
            StatusMessage = dts < 0
                ? "Error occurred during the deletion."
                : $"Testcase {tid} with {dts} runs deleted.";
            return RedirectToAction(nameof(Testcases));
        }


        [HttpGet("{tid}/[action]/{direction}")]
        public async Task<IActionResult> Move(int pid, int tid, string direction)
        {
            bool up = false;
            if (direction == "up") up = true;
            else if (direction != "down") return NotFound();
            await Store.ChangeRankAsync(pid, tid, up);
            await HttpContext.AuditAsync("moved", $"p{pid}t{tid}");
            return RedirectToAction(nameof(Testcases));
        }


        [HttpGet("{tid}/[action]/{filetype}")]
        public IActionResult Fetch(int pid, int tid, string filetype)
        {
            if (filetype == "input") filetype = "in";
            else if (filetype == "output") filetype = "out";
            else return NotFound();

            var fileInfo = Problems.GetFile(Problem, $"t{tid}.{filetype}");
            if (!fileInfo.Exists)
                return NotFound();

            return File(
                fileStream: fileInfo.CreateReadStream(),
                contentType: "application/octet-stream",
                fileDownloadName: $"p{pid}.t{tid}.{filetype}");
        }
    }
}
