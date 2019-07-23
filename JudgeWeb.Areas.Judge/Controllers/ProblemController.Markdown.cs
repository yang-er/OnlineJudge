using JudgeWeb.Areas.Judge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [Authorize(Roles = privilege)]
        [HttpGet("{backstore}/{target}")]
        public async Task<IActionResult> Markdown(string target, string backstore)
        {
            var lastVersion = await IoContext.ReadPartAsync(
                backstore, $"{target}.md") ?? "";

            return View(new ProblemMarkdownModel
            {
                Markdown = lastVersion,
                BackingStore = backstore,
                Target = target
            });
        }

        [Authorize(Roles = privilege)]
        [HttpPost("{backstore}/{target}")]
        public async Task<IActionResult> Markdown(string target,
            string backstore, ProblemMarkdownModel model)
        {
            if (target != model.Target || backstore != model.BackingStore)
                return BadRequest();
            await IoContext.WritePartAsync(
                backstore, $"{target}.md", model.Markdown);
            return View(model);
        }
    }
}
