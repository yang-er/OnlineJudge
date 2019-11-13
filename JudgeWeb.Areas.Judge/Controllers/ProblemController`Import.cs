using JudgeWeb.Areas.Judge.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [HttpGet("{pid}")]
        [ProblemAuthorize("pid")]
        public async Task<IActionResult> Export(int pid)
        {
            var contentFile = await ProblemManager.ExportXmlAsync(pid);
            if (contentFile == null) return NotFound();
            return ContentFile(contentFile, "text/xml", $"p{pid}.xml");
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Problem")]
        [ValidateInAjax]
        public IActionResult Import()
        {
            return Window();
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Problem")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(1 << 30)]
        public async Task<IActionResult> Import(IFormFile file,
            [FromServices] RoleManager<Data.Role> roleManager,
            [FromServices] SignInManager<Data.User> signInManager)
        {
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var p = await ProblemManager.ImportAsync(stream, User);

                    var rmr = await roleManager.CreateAsync(
                        new Data.Role("AuthorOfProblem" + p.ProblemId));
                    var log = "Refresh this page to apply updates.";

                    if (!rmr.Succeeded)
                    {
                        log = "Error creating user role, please contact XiaoYang. " + log;
                    }
                    else
                    {
                        var user = await UserManager.GetUserAsync(User);
                        await UserManager.AddToRoleAsync(user, "AuthorOfProblem" + p.ProblemId);
                        await signInManager.RefreshSignInAsync(user);
                    }

                    return Message("Problem Import",
                        $"Problem added succeeded. New Problem ID: {p.ProblemId}. {log}",
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
