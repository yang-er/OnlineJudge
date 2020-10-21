using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Problems;
using JudgeWeb.Features.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    [AuditPoint(AuditlogType.Problem)]
    public class ProblemsController : JuryControllerBase
    {
        private IProblemsetStore Store => Facade.Problemset;


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Add(int cid)
        {
            return Window(new ContestProblem
            {
                ContestId = cid,
                AllowSubmit = true
            });
        }


        [HttpGet("{pid}")]
        public async Task<IActionResult> Detail(int cid, int pid, bool all = false)
        {
            var prob = Problems.SingleOrDefault(p => p.ProblemId == pid);
            if (prob == null) return NotFound();
            ViewBag.TeamNames = await Facade.Teams.ListNamesAsync(cid);
            ViewBag.Submissions = await ListSubmissionsByProblemAsync(cid, pid, all);
            return View(prob);
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int cid, ContestProblem model)
        {
            if (Problems.Any(cp => cp.ShortName == model.ShortName))
                ModelState.AddModelError("xys::duplicate", "Duplicate short name for problem.");
            var probDetect = await Store.CheckAvailabilityAsync(cid, model.ProblemId, User);
            if (!probDetect.ok)
                ModelState.AddModelError("xys::prob", probDetect.msg);

            if (ModelState.IsValid)
            {
                var oldprobs = Problems;
                model.Color = "#" + model.Color.TrimStart('#');
                model.ContestId = cid;
                await Store.CreateAsync(model);
                await HttpContext.AuditAsync("attached", $"{model.ProblemId}");

                var newprobs = await Store.ListAsync(cid);
                var nowp = newprobs.SingleOrDefault(p => p.ProblemId == model.ProblemId);
                await Notifier.Create(cid, nowp);
                foreach (var @new in newprobs)
                    if (@new.Rank > nowp.Rank)
                        await Notifier.Update(cid, @new);

                StatusMessage = $"Problem {model.ShortName} saved.";
                return RedirectToAction("Home", "Jury");
            }
            else
            {
                return Window(model);
            }
        }


        [HttpGet("[action]/{pid}")]
        public async Task<IActionResult> Find(int cid, int pid)
        {
            var (_, msg) = await Store.CheckAvailabilityAsync(cid, pid, User);
            return Content(msg);
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        public IActionResult Edit(int pid)
        {
            var prob = Problems.SingleOrDefault(p => p.ProblemId == pid);
            if (prob == null) return NotFound();
            return Window(prob);
        }


        [HttpPost("{pid}/[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cid, int pid, ContestProblem model)
        {
            if (!Problems.Any(cp => cp.ProblemId == pid))
                return NotFound();
            if (Problems.Any(cp => cp.ShortName == model.ShortName && cp.ProblemId != pid))
                ModelState.AddModelError("xys::duplicate", "Duplicate short name for problem.");
            if (!ModelState.IsValid)
                return Window(model);

            await Store.UpdateAsync(cid, pid,
                () => new ContestProblem
                {
                    Color = "#" + model.Color.TrimStart('#'),
                    AllowSubmit = model.AllowSubmit,
                    ShortName = model.ShortName,
                    Score = model.Score,
                });

            await HttpContext.AuditAsync("updated", $"{pid}");

            var newprobs = await Store.ListAsync(cid);
            foreach (var @new in newprobs)
                await Notifier.Update(cid, @new);

            StatusMessage = $"Problem {model.ShortName} saved.";
            return RedirectToAction("Home", "Jury");
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        public IActionResult Delete(int cid, int pid)
        {
            var prob = Problems.SingleOrDefault(p => p.ProblemId == pid);
            if (prob == null) return NotFound();

            return AskPost(
                title: "Delete contest problem",
                message: $"Are you sure to delete problem {prob.ShortName}?",
                area: "Contest", ctrl: "Problems", act: "Delete",
                routeValues: new { cid, pid },
                type: MessageType.Danger);
        }


        [HttpPost("{pid}/[action]")]
        public async Task<IActionResult> Delete(int cid, int pid, bool post = true)
        {
            var prob = Problems.SingleOrDefault(p => p.ProblemId == pid);
            if (prob == null) return NotFound();

            await Store.DeleteAsync(prob);
            await HttpContext.AuditAsync("detached", $"{pid}");
            await Notifier.Delete(cid, prob);

            var newprobs = await Store.ListAsync(cid);
            foreach (var @new in newprobs)
                if (@new.Rank >= prob.Rank)
                    await Notifier.Update(cid, @new);

            StatusMessage = $"Contest problem {prob.ShortName} has been deleted.";
            return RedirectToAction("Home", "Jury");
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> GenerateStatement(int cid,
            [FromServices] IProblemViewProvider generator,
            [FromServices] IStaticFileRepository io)
        {
            var stmts = await Facade.Problemset.StatementsAsync(cid);

            var startTime = Contest.StartTime ?? DateTimeOffset.Now;
            var startDate = startTime.ToString("dddd, MMMM d, yyyy",
                formatProvider: System.Globalization.CultureInfo.GetCultureInfo(1033));

            var memstream = new MemoryStream();
            using (var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true))
            {
                var olymp = io.GetFileInfo("static/olymp.sty");
                using (var olympStream = olymp.CreateReadStream())
                    await zip.CreateEntryFromStream(olympStream, "olymp.sty");
                var texBegin = io.GetFileInfo("static/contest.tex-begin");
                var documentStart = await texBegin.ReadAsync();
                var documentBuilder = new System.Text.StringBuilder(documentStart)
                    .Append("\\begin {document}\n\n")
                    .Append("\\contest\n{")
                    .Append(Contest.Name)
                    .Append("}%\n{ACM.XYLAB.FUN}%\n{")
                    .Append(startDate)
                    .Append("}%\n\n")
                    .Append("\\binoppenalty=10000\n")
                    .Append("\\relpenalty=10000\n\n")
                    .Append("\\renewcommand{\\t}{\\texttt}\n\n");

                foreach (var item in stmts)
                {
                    var folderPrefix = $"{item.ShortName}/";
                    generator.BuildLatex(zip, item, folderPrefix);

                    documentBuilder
                        .Append("\\graphicspath{{./")
                        .Append(item.ShortName)
                        .Append("/}}\n\\import{./")
                        .Append(item.ShortName)
                        .Append("/}{./problem.tex}\n\n");
                }

                documentBuilder.Append("\\end{document}\n\n");
                zip.CreateEntryFromString(documentBuilder.ToString(), "contest.tex");
            }

            memstream.Position = 0;
            return File(memstream, "application/zip", $"c{cid}-statements.zip");
        }
    }
}
