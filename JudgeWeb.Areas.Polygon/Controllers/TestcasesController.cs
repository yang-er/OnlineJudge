using EFCore.BulkExtensions;
using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    public class TestcasesController : Controller3
    {
        private IFileRepository IoContext { get; }

        public TestcasesController(AppDbContext db, IFileRepository io) : base(db, true)
        {
            IoContext = io;
            io.SetContext("Problems");
        }


        [HttpGet]
        public async Task<IActionResult> Testcases(int pid)
        {
            ViewBag.Testcases = await DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .OrderBy(t => t.Rank)
                .ToListAsync();
            return View(Problem);
        }


        [HttpGet("{tid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Edit(int pid, int tid)
        {
            var tc = await DbContext.Testcases
                .Where(t => t.TestcaseId == tid && t.ProblemId == pid)
                .FirstOrDefaultAsync();
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
        [RequestFormLimits(MultipartBodyLengthLimit = 1 << 30, KeyLengthLimit = 1 << 30,
            MultipartBoundaryLengthLimit = 1 << 30, MultipartHeadersCountLimit = 1 << 30,
            MultipartHeadersLengthLimit = 1 << 30, BufferBodyLengthLimit = 1 << 30,
            ValueCountLimit = 1 << 30, ValueLengthLimit = 1 << 30)]
        public async Task<IActionResult> Edit(
            int pid, int tid, TestcaseUploadModel model,
            [FromServices] UserManager userManager)
        {
            try
            {
                var last = await DbContext.Testcases
                    .Where(t => t.TestcaseId == tid && t.ProblemId == pid)
                    .FirstOrDefaultAsync();
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
                    await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.in", input.Value.Item1);
                }

                if (output.HasValue)
                {
                    last.Md5sumOutput = output.Value.Item2;
                    last.OutputLength = output.Value.Item1.Length;
                    await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.out", output.Value.Item1);
                }

                last.Description = model.Description ?? last.Description;
                last.IsSecret = model.IsSecret;
                last.Point = model.Point;
                DbContext.Testcases.Update(last);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    UserName = userManager.GetUserName(User),
                    ContestId = 0,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Testcase,
                    Comment = "modified",
                    EntityId = last.TestcaseId,
                });

                await DbContext.SaveChangesAsync();

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
        [RequestFormLimits(MultipartBodyLengthLimit = 1 << 30, KeyLengthLimit = 1 << 30,
            MultipartBoundaryLengthLimit = 1 << 30, MultipartHeadersCountLimit = 1 << 30,
            MultipartHeadersLengthLimit = 1 << 30, BufferBodyLengthLimit = 1 << 30,
            ValueCountLimit = 1 << 30, ValueLengthLimit = 1 << 30)]
        public async Task<IActionResult> Create(
            int pid, TestcaseUploadModel model,
            [FromServices] UserManager userManager)
        {
            if (model.InputContent == null)
                return Message("Create testcase", "No input file specified.", MessageType.Danger);
            if (model.OutputContent == null)
                return Message("Create testcase", "No output file specified.", MessageType.Danger);

            try
            {
                var input = await model.InputContent.ReadAsync();
                var output = await model.OutputContent.ReadAsync();
                int rk = await DbContext.Testcases
                    .Where(p => p.ProblemId == pid)
                    .CountAsync();

                var e = DbContext.Testcases.Add(new Testcase
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

                await DbContext.SaveChangesAsync();

                int tid = e.Entity.TestcaseId;

                DbContext.AuditLogs.Add(new AuditLog
                {
                    UserName = userManager.GetUserName(User),
                    ContestId = 0,
                    Resolved = true,
                    Time = DateTimeOffset.Now,
                    Type = AuditLog.TargetType.Testcase,
                    Comment = "modified",
                    EntityId = tid,
                });

                await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.in", input.Item1);
                await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.out", output.Item1);
                await DbContext.SaveChangesAsync();

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
                area: "Polygon",
                ctrl: "Testcases",
                act: "Delete",
                routeValues:
                    new Dictionary<string, string>
                    {
                        ["pid"] = $"{Problem.ProblemId}",
                        ["tid"] = $"{tid}"
                    },
                type: MessageType.Danger);
        }


        [HttpPost("{tid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int pid, int tid)
        {
            var tc = await DbContext.Testcases
                .Where(t => t.ProblemId == pid && t.TestcaseId == tid)
                .FirstOrDefaultAsync();
            if (tc == null) return NotFound();

            int dts = await DbContext.Details
                .Where(d => d.TestcaseId == tid)
                .BatchDeleteAsync();
            DbContext.Testcases.Remove(tc);
            await DbContext.SaveChangesAsync();

            var tcs = await DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .ToListAsync();
            int tot = 0;
            foreach (var tc2 in tcs) tc2.Rank = ++tot;
            DbContext.Testcases.UpdateRange(tcs);
            await DbContext.SaveChangesAsync();

            StatusMessage = $"Testcase {tid} with {dts} runs deleted.";
            return RedirectToAction(nameof(Testcases));
        }


        [HttpGet("{tid}/[action]/{direction}")]
        public async Task<IActionResult> Move(int pid, int tid, string direction)
        {
            bool up = false;
            if (direction == "up") up = true;
            else if (direction != "down") return NotFound();

            var tc = await DbContext.Testcases
                .Where(t => t.ProblemId == pid && t.TestcaseId == tid)
                .FirstOrDefaultAsync();
            if (tc == null) return NotFound();

            int rk2 = tc.Rank + (up ? -1 : 1);
            var tc2 = await DbContext.Testcases
                .Where(t => t.ProblemId == pid && t.Rank == rk2)
                .FirstOrDefaultAsync();

            if (tc2 != null)
            {
                tc2.Rank = tc.Rank;
                tc.Rank = rk2;
                DbContext.Testcases.UpdateRange(tc, tc2);
                await DbContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Testcases));
        }


        [HttpGet("{tid}/[action]/{filetype}")]
        public IActionResult Fetch(int pid, int tid, string filetype)
        {
            if (filetype == "input") filetype = "in";
            else if (filetype == "output") filetype = "out";
            else return NotFound();

            if (!IoContext.ExistPart($"p{pid}", $"t{tid}.{filetype}"))
                return NotFound();

            return ContentFile(
                fileName: $"Problems/p{pid}/t{tid}.{filetype}",
                contentType: "application/octet-stream",
                downloadName: $"p{pid}.t{tid}.{filetype}");
        }
    }
}
