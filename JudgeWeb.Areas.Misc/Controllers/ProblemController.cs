using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Domains.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[controller]")]
    public class ProblemController : Controller2
    {
        private IArchiveStore Store { get; }

        private ISubmissionStore Submits { get; }

        public ProblemController(IArchiveStore probs, ISubmissionStore submits)
        {
            Store = probs;
            Submits = submits;
        }


        [HttpGet("/[controller]s")]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;
            ViewBag.Page = page;
            ViewBag.TotalPage = await Store.MaxPageAsync();
            int uid = int.Parse(User.GetUserId() ?? "-100");
            var model = await Store.ListAsync(page, uid);
            return View(model);
        }


        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid,
            [FromServices] IProblemStore probs)
        {
            var prob = await Store.FindAsync(pid);
            if (prob == null || prob.AllowSubmit == false) return NotFound();

            var fileInfo = probs.GetFile(prob, "view.html");
            var view = await fileInfo.ReadAsync();

            if (string.IsNullOrEmpty(view))
            {
                StatusMessage = "Error no descriptions avaliable now.";
                return RedirectToAction(nameof(List));
            }

            ViewData["Content"] = view;
            ViewData["Id"] = pid;
            ViewData["Source"] = prob.Source;
            ViewData["Tag"] = prob.TagName;
            ViewData["ReadId"] = prob.ProblemId;
            var uid = int.Parse(User.GetUserId() ?? "-100");
            return View();
        }


        [HttpGet("{pid}/[action]")]
        [Authorize]
        public async Task<IActionResult> Submissions(int pid, int draw, int start, int length)
        {
            const int PageCount = 15;
            var prob = await Store.FindAsync(pid);
            if (prob == null) return NotFound();
            var uid = int.Parse(User.GetUserId() ?? "-100");

            if (length != PageCount || start % PageCount != 0)
                return BadRequest();
            start = start / PageCount + 1;

            var (data, count) = await Submits.ListWithJudgingAsync(
                selector: (s, j) => new { Id = s.SubmissionId, s.Time, s.Language, j.Status },
                pagination: (start, 15),
                predicate: s => s.ProblemId == prob.ProblemId && s.Author == uid && s.ContestId == 0);
            return Json(new { draw, recordsTotal = count, recordsFiltered = count, data });
        }


        [HttpGet("{pid}/[action]/{sid}")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submission(int pid, int sid,
            [FromServices] IJudgingStore judgings)
        {
            var prob = await Store.FindAsync(pid);
            if (prob == null) return NotFound();
            var uid = int.Parse(User.GetUserId() ?? "-100");

            var subs = await Submits.ListWithJudgingAsync(
                predicate: s => s.ProblemId == prob.ProblemId && s.ContestId == 0
                             && s.Author == uid && s.SubmissionId == sid,
                selector: (s, j) => new CodeViewModel
                {
                    CompileError = j.CompileError,
                    CodeLength = s.CodeLength,
                    ExecuteMemory = j.ExecuteMemory,
                    ExecuteTime = j.ExecuteTime,
                    Code = s.SourceCode,
                    LanguageName = s.l.Name,
                    Status = j.Status,
                    JudgingId = j.JudgingId,
                    SubmissionId = s.SubmissionId,
                    DateTime = s.Time,
                    FileExtensions = s.l.FileExtension,
                });

            var sub = subs.SingleOrDefault();
            if (sub == null) return NotFound();
            sub.ProblemTitle = prob.Title;
            sub.Details = await judgings.GetDetailsAsync(prob.ProblemId, sub.JudgingId);
            return Window(sub);
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submit(int pid,
            [FromServices] ILanguageStore lang)
        {
            var prob = await Store.FindAsync(pid);
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit.Value)
                return Message(
                    title: $"Submit Problem {pid}",
                    message: $"Problem {pid} is not allowed for submitting.",
                    MessageType.Danger);

            ViewBag.ProblemTitle = prob.Title;
            ViewBag.Language = await lang.ListAsync(true);

            return Window(new CodeSubmitModel
            {
                Code = "",
                Language = "cpp",
                ProblemId = pid,
            });
        }


        [HttpPost("{pid}/[action]")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int pid, CodeSubmitModel model,
            [FromServices] ILanguageStore lang)
        {
            if (model.ProblemId != pid) return BadRequest();

            // check user blocking
            if (User.IsInRole("Blocked"))
                ModelState.AddModelError("xys::blocked",
                    "You are not permitted to submit code.");

            // check problem submit
            var prob = await Store.FindAsync(pid);
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit.Value)
            {
                StatusMessage = $"Problem {pid} is not allowed for submitting.";
                return RedirectToAction(nameof(View));
            }

            // check language blocking
            var langs = await lang.ListAsync(true);
            if (!langs.Any(a => a.Id == model.Language))
                ModelState.AddModelError("lang::notallow",
                    "You can't submit solutions with this language.");

            if (ModelState.ErrorCount > 0)
            {
                ViewBag.ProblemTitle = prob.Title;
                ViewBag.Language = langs;
                return View(model);
            }
            else
            {
                var sub = await Submits.CreateAsync(
                    code: model.Code,
                    language: model.Language,
                    problemId: prob.ProblemId,
                    contestId: null,
                    userId: int.Parse(User.GetUserId()),
                    ipAddr: HttpContext.Connection.RemoteIpAddress,
                    via: "problem-list",
                    username: User.GetUserName());

                int id = sub.SubmissionId;

                return RedirectToAction(nameof(View));
            }
        }
    }
}
