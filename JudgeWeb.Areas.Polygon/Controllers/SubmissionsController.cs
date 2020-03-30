using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    public class SubmissionsController : Controller3
    {
        private ISubmissionStore Submissions { get; }

        private IRejudgingStore Rejudgings { get; }

        private IJudgingStore Judgings { get; }

        public SubmissionsController(
            ISubmissionStore s1,
            IRejudgingStore s2,
            IJudgingStore s3)
        {
            Submissions = s1;
            Rejudgings = s2;
            Judgings = s3;
        }


        [HttpGet]
        public async Task<IActionResult> List(
            [FromServices] ITestcaseStore tcs,
            int pid, int page = 1, bool all = false)
        {
            if (page < 0) return NotFound();

            Expression<Func<Submission, bool>> predicate;
            if (all) predicate = s => s.ProblemId == pid;
            else predicate = s => s.ExpectedResult != null && s.ProblemId == pid;

            var (result, totPage) = await Submissions.ListWithJudgingAsync(
                predicate: s => s.ProblemId == pid,
                includeDetails: true,
                pagination: (page, 30));
            totPage = (totPage - 1) / 30 + 1;
            var names = await Submissions.GetAuthorNamesAsync(
                result.Select(l => l.AuthorId).ToArray());
            foreach (var item in result)
                item.AuthorName = names.GetValueOrDefault(item.AuthorId, "SYSTEM");

            ViewBag.TotalPage = totPage;
            ViewBag.Page = page;
            ViewBag.AllSub = all;
            ViewBag.Testcase = await tcs.ListAsync(pid);
            return View(result);
        }


        [HttpGet("{sid}")]
        public async Task<IActionResult> Detail(
            int pid, int sid, int? jid,
            [FromServices] ILanguageStore langs)
        {
            var s = await Submissions.FindAsync(sid, true);
            if (s == null || s.ProblemId != pid) return NotFound();
            var j = s.Judgings.SingleOrDefault(jj => jid.HasValue ? jj.JudgingId == jid : jj.Active);
            if (j == null) return NotFound();
            var l = await langs.FindAsync(s.Language);
            var det = await Judgings.GetDetailsAsync(pid, j.JudgingId);
            var uname = await Submissions.GetAuthorNameAsync(sid);

            return View(new ViewSubmissionModel
            {
                SubmissionId = s.SubmissionId,
                ContestId = s.ContestId,
                Status = j.Status,
                ExecuteMemory = j.ExecuteMemory,
                ExecuteTime = j.ExecuteTime,
                Judging = j,
                Expected = s.ExpectedResult,
                JudgingId = j.JudgingId,
                LanguageId = s.Language,
                Time = s.Time,
                SourceCode = s.SourceCode,
                CompileError = j.CompileError,
                CombinedRunCompare = Problem.CombinedRunCompare,
                TimeLimit = Problem.TimeLimit,
                AllJudgings = s.Judgings,
                UserName = uname ?? "SYSTEM",
                Details = det,
                TestcaseNumber = det.Count(),
                Author = s.Author,
                ServerName = j.Server ?? "UNKNOWN",
                LanguageName = l.Name,
                LanguageExternalId = l.Id,
                TimeFactor = l.TimeFactor,
            });
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Submit(
            [FromServices] ILanguageStore langs)
        {
            ViewBag.Language = await langs.ListAsync();
            return Window(new CodeSubmitModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            CodeSubmitModel model,
            [FromServices] ILanguageStore langs)
        {
            var lang = await langs.FindAsync(model.Language);
            if (lang == null) return BadRequest();

            var sub = await Submissions.CreateAsync(
                code: model.Code,
                language: lang.Id,
                problemId: Problem.ProblemId,
                contestId: null,
                userId: int.Parse(User.GetUserId()),
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "polygon-page",
                username: User.GetUserName(),
                expected: Verdict.Unknown);

            return RedirectToAction(nameof(Detail), new { sid = sub.SubmissionId });
        }


        [HttpGet("{sid}/[action]")]
        public async Task<IActionResult> RejudgeOne(int pid, int sid)
        {
            var sub = await Submissions.FindAsync(sid);
            if (sub == null || sub.ProblemId != pid)
                return NotFound();

            if (sub.ContestId != 0)
                StatusMessage = "Error : contest submissions should be rejudged by jury.";
            else
                await Rejudgings.RejudgeAsync(sub, fullTest: true);
            return RedirectToAction(nameof(Detail));
        }


        [HttpGet("{sid}/[action]/{jid}/{rid}/{type}")]
        public async Task<IActionResult> RunDetails(int pid, int sid, int jid, int rid, string type)
        {
            var fileInfo = await Judgings.GetRunFileAsync(jid, rid, type, sid, pid);
            if (!fileInfo.Exists) return NotFound();

            return File(
                fileStream: fileInfo.CreateReadStream(),
                contentType: "application/octet-stream",
                fileDownloadName: $"j{jid}_r{rid}.{type}");
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Rejudge()
        {
            return AskPost(
                title: "Rejudge all",
                message: "Do you want to rejudge all polygon submissions? " +
                    "This may take time and cause server load.",
                area: "Polygon", ctrl: "Submissions", act: "Rejudge",
                routeValues: new { pid = Problem.ProblemId },
                type: MessageType.Warning);
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejudge(int pid)
        {
            await Rejudgings.BatchRejudgeAsync(
                (s, j) => s.ExpectedResult != null && s.ProblemId == pid && s.ContestId == 0);
            StatusMessage = "All submissions are being rejudged.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{sid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> ChangeExpected(
            int pid, int sid,
            [FromServices] ILanguageStore langs)
        {
            var sub = await Submissions.FindAsync(sid);
            if (sub == null || sub.ProblemId != pid) return NotFound();
            ViewBag.Languages = await langs.ListAsync();

            return Window(new ChangeExpectedModel
            {
                Verdict = !sub.ExpectedResult.HasValue ? -1 : (int)sub.ExpectedResult.Value,
                Language = sub.Language,
            });
        }


        [HttpPost("{sid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeExpected(int pid, int sid, ChangeExpectedModel model)
        {
            var it = await Submissions.FindAsync(sid);
            if (it == null || it.ProblemId == pid) return NotFound();

            it.ExpectedResult = model.Verdict == -1 ? default(Verdict?) : (Verdict)model.Verdict;
            it.Language = model.Language;
            await Submissions.UpdateAsync(it);
            return RedirectToAction(nameof(Detail));
        }
    }
}
