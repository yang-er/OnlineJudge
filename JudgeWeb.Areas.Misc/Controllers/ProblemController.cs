using JudgeWeb.Areas.Misc.Models;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Misc.Controllers
{
    [Area("Misc")]
    [Route("[controller]")]
    public class ProblemController : Controller2
    {
        private IProblemFacade Facade { get; }

        private IJudgingStore Judgings { get; }

        private ISubmissionStore Submits { get; }

        public ProblemController(IProblemFacade probs, IJudgementFacade submits)
        {
            Facade = probs;
            Submits = submits.SubmissionStore;
            Judgings = submits.JudgingStore;
        }

        private async Task<IEnumerable<SelectListItem>> LanguagesAsync()
        {
            var lst = await Facade.Languages.ListAsync(true);
            return lst.Select(l => new SelectListItem(l.Name, l.Id));
        }


        [HttpGet("/[controller]s")]
        public async Task<IActionResult> List(int page = 1)
        {
            if (page < 1) page = 1;
            ViewBag.Page = page;
            ViewBag.TotalPage = await Facade.Archives.MaxPageAsync();
            int uid = int.Parse(User.GetUserId() ?? "-100");
            var model = await Facade.Archives.ListAsync(page, uid);
            return View(model);
        }


        [HttpGet("{pid}")]
        public async Task<IActionResult> View(int pid,
            [FromServices] IProblemFileRepository ioContext)
        {
            var prob = await Facade.Archives.FindAsync(pid);
            if (prob == null) return NotFound();

            var fileInfo = Facade.GetFile(prob, "view.html");
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
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submissions(int pid, int page)
        {
            var prob = await Facade.Archives.FindAsync(pid);
            if (prob == null) return NotFound();
            var uid = int.Parse(User.GetUserId() ?? "-100");
            if (page <= 0) page = 1;

            var subs = await Submits.ListWithJudgingAsync(
                selector: (s, j) => new { Id = s.SubmissionId, s.Time, s.Language, j.Status },
                pagination: (page, 15),
                predicate: s => s.ProblemId == prob.ProblemId && s.Author == uid && s.ContestId == 0);
            return Json(subs);
        }


        [HttpGet("{pid}/[action]/{sid}")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submission(int pid, int sid)
        {
            var prob = await Facade.Archives.FindAsync(pid);
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
                    LanguageName = s.Language,
                    Status = j.Status,
                    JudgingId = j.JudgingId,
                    SubmissionId = s.SubmissionId,
                    DateTime = s.Time,
                });

            var sub = subs.SingleOrDefault();
            if (sub == null) return NotFound();
            var lang = await Facade.Languages.FindAsync(sub.LanguageName);
            sub.LanguageName = lang.Name;
            sub.FileExtensions = lang.FileExtension;
            sub.ProblemTitle = prob.Title;
            sub.Details = await Judgings.GetDetailsAsync(prob.ProblemId, sub.JudgingId);
            return Window(sub);
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        [Authorize]
        public async Task<IActionResult> Submit(int pid)
        {
            var prob = await Facade.Archives.FindAsync(pid);
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit.Value)
                return Message(
                    title: $"Submit Problem {pid}",
                    message: $"Problem {pid} is not allowed for submitting.",
                    MessageType.Danger);

            ViewBag.ProblemTitle = prob.Title;
            ViewBag.Language = await LanguagesAsync();

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
        public async Task<IActionResult> Submit(int pid, CodeSubmitModel model)
        {
            if (model.ProblemId != pid) return BadRequest();

            // check user blocking
            if (User.IsInRole("Blocked"))
                ModelState.AddModelError("xys::blocked",
                    "You are not permitted to submit code.");

            // check problem submit
            var prob = await Facade.Archives.FindAsync(pid);
            if (prob == null) return NotFound();

            if (!prob.AllowSubmit.Value)
            {
                StatusMessage = $"Problem {pid} is not allowed for submitting.";
                return RedirectToAction(nameof(View));
            }

            // check language blocking
            var langs = await LanguagesAsync();
            if (!langs.Any(a => a.Value == model.Language))
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
