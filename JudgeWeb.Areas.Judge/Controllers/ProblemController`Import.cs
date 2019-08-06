using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [HttpGet("{pid}")]
        [Authorize(Roles = privilege)]
        public async Task<IActionResult> Export(int pid)
        {
            var contentFile = await ProblemManager.ExportXmlAsync(pid);
            if (contentFile == null) return NotFound();
            return ContentFile(contentFile, "text/xml", $"p{pid}.xml");
        }

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
                    var p = await ProblemManager.ImportAsync(stream, User);

                    return Message("Problem Import",
                        $"Problem added succeeded. New Problem ID: {p.ProblemId}. " +
                        "Refresh this page to apply updates.",
                        MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                return Message("Problem Restore",
                    "Import failed. Please contact XiaoYang. " + ex,
                    MessageType.Danger);
            }
        }
    }
}
