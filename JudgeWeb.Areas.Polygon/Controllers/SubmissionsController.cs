using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    public class SubmissionsController : Controller3
    {
        private SubmissionManager SubmissionManager { get; }

        public SubmissionsController(AppDbContext db, SubmissionManager sm) : base(db, true)
        {
            SubmissionManager = sm;
        }


        [HttpGet]
        public async Task<IActionResult> List(int pid, int page = 1, bool all = false)
        {
            IQueryable<Submission> baseQuery;

            if (!all)
            {
                baseQuery = DbContext.Submissions
                    .Where(s => s.ExpectedResult != null && s.ProblemId == pid);
            }
            else
            {
                baseQuery = DbContext.Submissions
                    .Where(s => s.ProblemId == pid);
            }

            var query =
                from s in baseQuery
                join l in DbContext.Languages on s.Language equals l.LangId
                join u in DbContext.Users on s.Author equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                join j in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { j.SubmissionId, j.Active }
                select new ListSubmissionModel
                {
                    SubmissionId = s.SubmissionId,
                    JudgingId = j.JudgingId,
                    Language = l.ExternalId,
                    Result = j.Status,
                    Time = s.Time,
                    UserName = u.UserName ?? "SYSTEM",
                    Expected = s.ExpectedResult,
                    ExecutionTime = j.ExecuteTime,
                };

            if (all) query = query.OrderByDescending(s => s.SubmissionId);
            else query = query.OrderBy(s => s.SubmissionId);

            int tot = await baseQuery.CountAsync();
            tot = (tot - 1) / 30 + 1;
            ViewBag.TotalPage = tot;
            if (page < 1) page = 1;
            if (page > tot) page = tot;
            ViewBag.Page = page;
            ViewBag.AllSub = all;

            var result = await query
                .Skip((page - 1) * 30)
                .Take(30)
                .ToListAsync();

            var tcs = await DbContext.Testcases
                .Where(t => t.ProblemId == pid)
                .OrderBy(t => t.Rank)
                .ToListAsync();

            foreach (var item in result)
            {
                int jid = item.JudgingId;
                item.Details = await DbContext.Details
                    .Where(d => d.JudgingId == jid)
                    .Select(d => new JudgingDetailModel
                    {
                        Result = d.Status,
                        ExecutionTime = d.ExecuteTime,
                        TestcaseId = d.TestcaseId
                    })
                    .ToDictionaryAsync(k => k.TestcaseId);
            }

            ViewBag.Testcase = tcs;
            return View("List", result);
        }


        [HttpGet("all")]
        public Task<IActionResult> ListAll(int pid, int page = 1)
        {
            return List(pid, page, true);
        }


        [HttpGet("{sid}")]
        public async Task<IActionResult> Detail(int pid, int sid, int? jid)
        {
            var judging = DbContext.Judgings
                .Where(j => j.SubmissionId == sid);
            if (jid.HasValue)
                judging = judging.Where(j => j.JudgingId == jid);
            else
                judging = judging.Where(j => j.Active);
            
            var query =
                from j in judging
                join s in DbContext.Submissions on new { j.SubmissionId, ProblemId = pid } equals new { s.SubmissionId, s.ProblemId }
                join l in DbContext.Languages on s.Language equals l.LangId
                join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                select new ViewSubmissionModel
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
                    CombinedRunCompare = p.CombinedRunCompare,
                    Author = s.Author,
                    ServerName = j.Server ?? "UNKNOWN",
                    LanguageName = l.Name,
                    LanguageExternalId = l.ExternalId,
                    TimeFactor = l.TimeFactor,
                };

            var model = await query.FirstOrDefaultAsync();
            if (model == null) return NotFound();
            model.TimeLimit = Problem.TimeLimit;

            model.AllJudgings = await DbContext.Judgings
                .Where(j => j.SubmissionId == sid)
                .ToListAsync();

            int gid = model.JudgingId;
            var details =
                from t in DbContext.Testcases
                where t.ProblemId == pid
                join d in DbContext.Details on new { JudgingId = gid, t.TestcaseId } equals new { d.JudgingId, d.TestcaseId } into dd
                from d in dd.DefaultIfEmpty()
                select new { t, d };
            var dets = await details.ToListAsync();
            dets.Sort((a, b) => a.t.Rank.CompareTo(b.t.Rank));
            model.Details = dets.Select(a => (a.d, a.t));
            int uid = model.Author;
            model.UserName = await DbContext.Users
                .Where(u => u.Id == uid)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();
            return View(model);
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> Submit()
        {
            ViewData["Language"] = await DbContext.Languages
                .ToDictionaryAsync(k => k.LangId, v => v.Name);
            return Window(new CodeSubmitModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(CodeSubmitModel model,
            [FromServices] UserManager userManager)
        {
            var lang = await DbContext.Languages
                .Where(l => l.LangId == model.Language)
                .SingleOrDefaultAsync();

            var sub = await SubmissionManager.CreateAsync(
                code: model.Code,
                langid: lang,
                probid: Problem.ProblemId,
                cid: null,
                uid: int.Parse(userManager.GetUserId(User)),
                ipAddr: HttpContext.Connection.RemoteIpAddress,
                via: "polygon-page",
                username: userManager.GetUserName(User),
                expected: Verdict.Unknown);

            return RedirectToAction(nameof(Detail), new { sid = sub.SubmissionId });
        }


        [HttpGet("{sid}/[action]")]
        public async Task<IActionResult> RejudgeOne(int pid, int sid)
        {
            var sub = await SubmissionManager.FindAsync(sid, pid: pid);
            if (sub == null) return NotFound();

            if (sub.ContestId != 0)
                StatusMessage = "Error : contest submissions should be rejudged by jury.";
            else
                await SubmissionManager.RejudgeAsync(sub, fullTest: true);
            return RedirectToAction(nameof(Detail));
        }


        [HttpGet("{jid}/{rid}/{type}")]
        public IActionResult RunDetails(int jid, int rid, string type,
            [FromServices] IFileRepository io)
        {
            io.SetContext("Runs");

            if (!io.ExistPart($"j{jid}", $"r{rid}.{type}"))
                return NotFound();

            return ContentFile(
                fileName: $"Runs/j{jid}/r{rid}.{type}",
                contentType: "application/octet-stream",
                downloadName: $"j{jid}_r{rid}.{type}");
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Rejudge()
        {
            return AskPost(
                title: "Rejudge all",
                message: "Do you want to rejudge all polygon submissions? This may take time and cause server load.",
                area: "Polygon",
                ctrl: "Submissions",
                act: "Rejudge",
                routeValues: new Dictionary<string, string> { ["pid"] = $"{Problem.ProblemId}" },
                type: MessageType.Warning);
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejudge(int pid)
        {
            var subs = await SubmissionManager.Submissions
                .Where(s => s.ExpectedResult != null && s.ProblemId == pid && s.ContestId == 0)
                .ToListAsync();
            foreach (var sub in subs)
                await SubmissionManager.RejudgeAsync(sub);
            StatusMessage = "All submissions are being rejudged.";
            return RedirectToAction(nameof(List));
        }


        [HttpGet("{sid}/[action]")]
        [ValidateInAjax]
        public async Task<IActionResult> ChangeExpected(int pid, int sid)
        {
            var it = await SubmissionManager.Submissions
                .Where(s => s.SubmissionId == sid && s.ProblemId == pid)
                .Select(s => new { s.ExpectedResult })
                .FirstOrDefaultAsync();
            if (it == null) return NotFound();

            return Window(new ChangeExpectedModel
            {
                Verdict = !it.ExpectedResult.HasValue ? -1 : (int)it.ExpectedResult.Value
            });
        }


        [HttpPost("{sid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeExpected(int pid, int sid, ChangeExpectedModel model)
        {
            var it = await SubmissionManager.Submissions
                .Where(s => s.SubmissionId == sid && s.ProblemId == pid)
                .FirstOrDefaultAsync();
            if (it == null) return NotFound();

            it.ExpectedResult = model.Verdict == -1 ? default(Verdict?) : (Verdict)model.Verdict;
            await SubmissionManager.UpdateAsync(it);
            return RedirectToAction(nameof(Detail));
        }
    }
}
