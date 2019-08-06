﻿using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Areas.Judge.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        [Authorize(Roles = privilege)]
        [HttpGet("{ppid}")]
        public async Task<IActionResult> Edit(string ppid)
        {
            if (ppid == "add")
            {
                int pid = await ProblemManager.CreateAsync();
                return RedirectToAction(nameof(Edit), new { ppid = pid.ToString() });
            }
            else if (int.TryParse(ppid, out int pid))
            {
                var prob = await ProblemManager.GetEditModelAsync(pid);
                if (prob is null) return NotFound();
                ViewBag.Testcase = await TestcaseManager.EnumerateAsync(pid);
                return View(prob);
            }
            else
            {
                return BadRequest();
            }
        }
        
        [Authorize(Roles = privilege)]
        [HttpPost("{pid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int pid, ProblemEditModel model,
            [FromServices] IProblemViewProvider viewGenerator)
        {
            if (ModelState.ErrorCount == 0)
            {
                var prob = await ProblemManager.EditAsync(pid, model);
                if (prob == null) return NotFound();
                await ProblemManager.GenerateViewAsync(prob, viewGenerator);

                ViewData["MsgType"] = "success";
                ViewData["Message"] = "Problem modified successfully.";
            }
            else
            {
                var errors = new StringBuilder();
                foreach (var msg in ModelState)
                    foreach (var item in msg.Value.Errors)
                        errors.AppendLine(item.ErrorMessage);

                ViewData["MsgType"] = "danger";
                ViewData["Message"] = errors;
            }

            ViewBag.Testcase = await TestcaseManager.EnumerateAsync(pid);
            return View(model);
        }

        [Authorize(Roles = privilege)]
        [HttpGet("{backstore}/{target}")]
        public async Task<IActionResult> Markdown(string target, string backstore)
        {
            var lastVersion = await ProblemManager
                .ReadMarkdownAsync(backstore, target) ?? "";

            return View(new ProblemMarkdownModel
            {
                Markdown = lastVersion,
                BackingStore = backstore,
                Target = target
            });
        }

        [Authorize(Roles = privilege)]
        [HttpPost("{backstore}/{target}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Markdown(string target,
            string backstore, ProblemMarkdownModel model)
        {
            if (target != model.Target || backstore != model.BackingStore)
                return BadRequest();
            await ProblemManager.SaveMarkdownAsync(backstore, target, model.Markdown);
            return View(model);
        }
    }
}
