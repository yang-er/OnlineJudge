using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Judge.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Judge.Controllers
{
    public partial class ProblemController
    {
        const string notPermitted = "You are not permitted to submit code.";

        private IEnumerable<SelectListItem> GetLanguageList()
        {
            return DbContext.Languages
                .Where(l => l.AllowSubmit)
                .Select(l => new Tuple<int, string>(l.LangId, l.Name))
                .Cacheable(TimeSpan.FromMinutes(5))
                .ToList()
                .Select(t => new SelectListItem(t.Item1.ToString(), t.Item2));
        }

        /// <summary>
        /// 展示提交代码的页面。
        /// </summary>
        /// <param name="pid">题目编号</param>
        [HttpGet("{pid}")]
        [Authorize]
        public async Task<IActionResult> Submit(int pid)
        {
            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .Select(p => new { p.Title, p.Flag })
                .FirstOrDefaultAsync();

            if (prob is null) return NotFound();
            if (prob.Flag != 0 && !User.IsInRoles(privilege)) return NotFound();

            ViewData["ProblemTitle"] = prob.Title;
            ViewData["ProblemId"] = pid;
            ViewData["Title"] = "Submit Code";
            ViewBag.Language = GetLanguageList();

            return View(new CodeSubmitModel
            {
                Code = "",
                Language = 2,
                ProblemId = pid,
            });
        }

        /// <summary>
        /// 提交代码并存入数据库。
        /// </summary>
        /// <param name="pid">问题编号</param>
        /// <param name="model">代码视图模型</param>
        [HttpPost("{pid}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int pid, CodeSubmitModel model)
        {
            if (model.ProblemId != pid) return BadRequest();

            var prob = await DbContext.Problems
                .Where(s => s.ProblemId == pid)
                .FirstOrDefaultAsync();

            if (prob is null) return NotFound();
            if (prob.Flag != 0 && !User.IsInRoles(privilege)) return NotFound();

            if (User.IsInRole("Blocked"))
            {
                ModelState.AddModelError("xys::blocked", notPermitted);
            }

            if (ModelState.ErrorCount > 0)
            {
                ViewData["ProblemTitle"] = prob.Title;
                ViewData["ProblemId"] = pid;
                ViewData["Title"] = "Submit Code";
                ViewBag.Language = GetLanguageList();

                return View(model);
            }
            else
            {
                var uid = int.Parse(UserManager.GetUserId(User));
                var guid = Guid.NewGuid();

                var s = DbContext.Submissions.Add(new Submission
                {
                    Author = uid,
                    CodeLength = model.Code.Length,
                    Ip = HttpContext.Connection.RemoteIpAddress.ToString(),
                    Language = model.Language,
                    ProblemId = model.ProblemId,
                    SourceCode = model.Code.ToBase64(),
                    ContestId = 0,
                    Time = DateTimeOffset.Now,
                });
                
                await DbContext.SaveChangesAsync();

                DbContext.AuditLogs.Add(new AuditLog
                {
                    ContestId = 0,
                    EntityId = s.Entity.SubmissionId,
                    Time = s.Entity.Time.DateTime,
                    Resolved = true,
                    Type = AuditLog.TargetType.Submission,
                    UserName = UserManager.GetUserName(User),
                    Comment = "added via problem list",
                });

                var j = DbContext.Judgings.Add(new Judging
                {
                    SubmissionId = s.Entity.SubmissionId,
                    FullTest = false,
                    Active = true,
                    Status = Verdict.Pending,
                    RejudgeId = 0,
                });

                await DbContext.SaveChangesAsync();
                return RedirectToAction("View", "Status", new { area = "Judge", id = s.Entity.SubmissionId });
            }
        }
    }
}
