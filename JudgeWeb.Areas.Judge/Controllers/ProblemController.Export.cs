using JudgeWeb.Features.Problem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [HttpGet("{pid}")]
        [Authorize(Roles = privilege)]
        public async Task<IActionResult> Export(int pid)
        {
            var backstore = $"p{pid}";

            if (!IoContext.ExistPart(backstore, "export.xml"))
            {
                var prob = await DbContext.Problems
                    .Where(p => p.ProblemId == pid)
                    .FirstOrDefaultAsync();
                if (prob is null) return NotFound();

                var description = await IoContext.ReadPartAsync(backstore, "description.md") ?? "";
                var inputdesc = await IoContext.ReadPartAsync(backstore, "inputdesc.md") ?? "";
                var outputdesc = await IoContext.ReadPartAsync(backstore, "outputdesc.md") ?? "";
                var hint = await IoContext.ReadPartAsync(backstore, "hint.md") ?? "";

                var xmlObj = new ProblemSet
                {
                    Description = description,
                    InputHint = inputdesc,
                    OutputHint = outputdesc,
                    HintAndNote = hint,
                    Author = prob.Source ?? "",
                    Title = prob.Title ?? "",
                    ProblemId = prob.ProblemId,
                    ExecuteTimeLimit = prob.TimeLimit,
                    MemoryLimit = prob.MemoryLimit,
                    RunScript = prob.RunScript,
                    CompareScript = prob.CompareScript,
                    JudgeType = prob.ComapreArguments,
                };

                var tcs = await DbContext.Testcases
                    .Where(t => t.ProblemId == pid)
                    .Select(t => new { t.IsSecret, t.Input, t.Output, t.Description, t.Point })
                    .ToListAsync();

                foreach (var tc in tcs)
                {
                    var target = tc.IsSecret ? xmlObj.TestCases : xmlObj.Samples;
                    target.Add(new TestCase
                    (
                        tc.Description,
                        Encoding.UTF8.GetString(tc.Input),
                        Encoding.UTF8.GetString(tc.Output),
                        tc.Point
                    ));
                }

                var sw = new StringWriter();
                sw.NewLine = "\n";
                await xmlObj.ToXml().SaveAsync(sw, SaveOptions.None, default);
                await IoContext.WritePartAsync(backstore, "export.xml", sw.ToString());
            }

            return ContentFile($"Problems/{backstore}/export.xml", "text/xml", $"{pid}.xml");
        }
    }
}
