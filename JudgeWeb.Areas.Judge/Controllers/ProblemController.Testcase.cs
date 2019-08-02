using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

            if (IoContext.ExistPart($"p{pid}", $"t{tid}.{filetype}"))
                return ContentFile(
                    $"Problems/p{pid}/t{tid}.{filetype}",
                    "application/octet-stream",
                    $"p{pid}.t{tid}.{filetype}");
            return NotFound();
        }

        [HttpPost("{pid}/{ttid}")]
        [Authorize(Roles = privilege)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Testcase(int pid, string ttid, TestcaseUploadModel model)
        {
            try
            {
                if (ttid == "add")
                {
                    var input = await model.InputContent.ReadAsync();
                    var output = await model.OutputContent.ReadAsync();
                    int rank = await DbContext.Testcases.CountAsync(a => a.ProblemId == pid);

                    var tc = DbContext.Testcases.Add(new Testcase
                    {
                        ProblemId = pid,
                        Rank = rank + 1,
                        Description = model.Description,
                        IsSecret = model.IsSecret,
                        Md5sumInput = input.Item2,
                        Md5sumOutput = output.Item2,
                        InputLength = input.Item1.Length,
                        OutputLength = output.Item1.Length,
                    });

                    await DbContext.SaveChangesAsync();

                    await IoContext.WriteBinaryAsync($"p{pid}", $"t{tc.Entity.TestcaseId}.in", input.Item1);
                    await IoContext.WriteBinaryAsync($"p{pid}", $"t{tc.Entity.TestcaseId}.out", output.Item1);

                    DbContext.AuditLogs.Add(new AuditLog
                    {
                        UserName = UserManager.GetUserName(User),
                        ContestId = 0,
                        Resolved = true,
                        Time = DateTimeOffset.Now,
                        Type = AuditLog.TargetType.Testcase,
                        Comment = $"added for p{pid} with rank {rank}",
                        EntityId = tc.Entity.TestcaseId,
                    });

                    await DbContext.SaveChangesAsync();

                    return Message(
                        "Testcase Import",
                        "Testcase added successfully. " +
                        $"New Testcase ID: {tc.Entity.TestcaseId}. " +
                        "Refresh this page to see it.",
                        MessageType.Success);
                }
                else
                {
                    if (!int.TryParse(ttid, out int tid))
                        return BadRequest();
                    var last = await DbContext.Testcases
                        .Where(t => t.TestcaseId == tid)
                        .FirstOrDefaultAsync();
                    if (last is null) return NotFound();

                    if (model.InputContent != null)
                    {
                        var tc = await model.InputContent.ReadAsync();
                        last.Md5sumInput = tc.Item2;
                        last.InputLength = tc.Item1.Length;
                        await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.in", tc.Item1);
                    }

                    if (model.OutputContent != null)
                    {
                        var tc = await model.OutputContent.ReadAsync();
                        last.Md5sumOutput = tc.Item2;
                        last.OutputLength = tc.Item1.Length;
                        await IoContext.WriteBinaryAsync($"p{pid}", $"t{tid}.out", tc.Item1);
                    }

                    last.Description = model.Description ?? last.Description;
                    last.IsSecret = model.IsSecret;
                    DbContext.Testcases.Update(last);

                    DbContext.AuditLogs.Add(new AuditLog
                    {
                        UserName = UserManager.GetUserName(User),
                        ContestId = 0,
                        Resolved = true,
                        Time = DateTimeOffset.Now,
                        Type = AuditLog.TargetType.Testcase,
                        Comment = "modified",
                        EntityId = last.TestcaseId,
                    });

                    await DbContext.SaveChangesAsync();
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
                var model = await DbContext.Testcases
                    .Where(t => t.TestcaseId == tid && t.ProblemId == pid)
                    .Select(t => new TestcaseUploadModel
                    {
                        Description = t.Description,
                        IsSecret = t.IsSecret,
                        Point = t.Point,
                    })
                    .FirstOrDefaultAsync();

                if (model is null) return NotFound();
                return Window(model);
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
