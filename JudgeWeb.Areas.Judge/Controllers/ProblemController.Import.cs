using JudgeWeb.Data;
using JudgeWeb.Features.Problem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [HttpGet]
        [Authorize(Roles = privilege)]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return Window();
        }

        [HttpPost]
        [Authorize(Roles = privilege)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var st = ProblemSet.FromStream(stream, true, true);
                    var probId = 1001 + await DbContext.Problems.CountAsync();

                    // add problem entity
                    DbContext.Problems.Add(new Problem
                    {
                        RunScript = st.RunScript,
                        CompareScript = st.CompareScript,
                        Source = st.Author,
                        Flag = 1,
                        MemoryLimit = st.MemoryLimit,
                        TimeLimit = st.ExecuteTimeLimit,
                        ProblemId = probId,
                        Title = st.Title
                    });

                    await DbContext.SaveChangesAsync();

                    // Write all markdown files into folders.
                    var backstore = $"p{probId}";
                    await IoContext.WritePartAsync(backstore, "description.md", st.Description);
                    await IoContext.WritePartAsync(backstore, "inputdesc.md", st.InputHint);
                    await IoContext.WritePartAsync(backstore, "outputdesc.md", st.OutputHint);
                    await IoContext.WritePartAsync(backstore, "hint.md", st.HintAndNote);

                    // Add testcases.
                    int i = 0;
                    var testcases = Enumerable.Concat(
                        st.Samples.Select(t => (t, false)),
                        st.TestCases.Select(t => (t, true)));

                    foreach (var test in testcases)
                    {
                        i++;
                        var input = Encoding.UTF8.GetBytes(test.t.Input);
                        var inputHash = input.ToMD5().ToHexDigest(true);
                        var output = Encoding.UTF8.GetBytes(test.t.Output);
                        var outputHash = output.ToMD5().ToHexDigest(true);

                        var tcc = DbContext.Testcases.Add(new Testcase
                        {
                            InputLength = input.Length,
                            OutputLength = output.Length,
                            Point = test.t.Point,
                            ProblemId = probId,
                            Rank = i,
                            Description = test.t.Description,
                            IsSecret = test.Item2,
                            Md5sumInput = inputHash,
                            Md5sumOutput = outputHash,
                        });

                        await DbContext.SaveChangesAsync();

                        await IoContext.WriteBinaryAsync($"p{probId}", $"t{tcc.Entity.TestcaseId}.in", input);
                        await IoContext.WriteBinaryAsync($"p{probId}", $"t{tcc.Entity.TestcaseId}.out", output);
                    }

                    DbContext.AuditLogs.Add(new AuditLog
                    {
                        Comment = "uploaded by xml",
                        EntityId = probId,
                        Resolved = true,
                        ContestId = 0,
                        Time = DateTimeOffset.Now,
                        Type = AuditLog.TargetType.Problem,
                        UserName = UserManager.GetUserName(User),
                    });

                    return Message(
                        "Problem Import",
                        $"Problem added succeeded. New Problem ID: {probId}. Refresh this page to apply updates.",
                        MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                return Message(
                    "Problem Restore",
                    "Import failed. Please contact XiaoYang. " + ex,
                    MessageType.Danger);
            }
        }
    }
}
