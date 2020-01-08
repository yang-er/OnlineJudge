﻿using EFCore.BulkExtensions;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/problems")]
    public class JuryProblemController : JuryControllerBase
    {
        private void MarkChanged()
        {
            Cache.Remove($"`c{Contest.ContestId}`probs");
        }


        private async Task<string> DetectProblemConflict(int pid, bool pname)
        {
            if (Problems.Find(pid) != null)
                return "Problem has been added into contest.";

            var prob = await DbContext.Problems
                .Where(p => p.ProblemId == pid)
                .SingleOrDefaultAsync();
            if (prob == null)
                return "Problem is not found.";
            if (!User.IsInRoles($"Administrator,AuthorOfProblem{pid}"))
                return "Permission denied.";
            return pname ? prob.Title : null;
        }


        [HttpGet("[action]")]
        [ValidateInAjax]
        public IActionResult Add(int cid)
        {
            return Window(new ContestProblem { ContestId = cid, AllowJudge = true, AllowSubmit = true });
        }


        [HttpPost("[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int cid, ContestProblem model)
        {
            var items = await DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid)
                .ToListAsync();

            if (items.Any(cp => cp.ShortName == model.ShortName))
                ModelState.AddModelError("xys::duplicate", "Duplicate short name for problem.");
            var probDetect = await DetectProblemConflict(model.ProblemId, false);
            if (probDetect != null)
                ModelState.AddModelError("xys::prob", probDetect);

            if (ModelState.IsValid)
            {
                var oldprobs = Problems;
                model.Color = "#" + model.Color.TrimStart('#');
                model.ContestId = cid;
                items.Add(model);
                items.Sort((cp1, cp2) => cp1.ShortName.CompareTo(cp2.ShortName));

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == model)
                    {
                        items[i].Rank = i + 1;
                        DbContext.ContestProblem.Add(model);
                    }
                    else if (items[i].Rank != i + 1)
                    {
                        items[i].Rank = i + 1;
                        DbContext.ContestProblem.Update(items[i]);
                    }
                }

                await DbContext.SaveChangesAsync();
                MarkChanged();

                var newprobs = await DbContext.GetProblemsAsync(cid);
                DbContext.Events.AddCreate(cid, new Data.Api.ContestProblem2(newprobs.First(cp => cp.ProblemId == model.ProblemId)));
                foreach (var (@new, old) in newprobs.Join(oldprobs, a => a.ProblemId, a => a.ProblemId, (a, b) => (a, b)))
                    if (@new.Rank != old.Rank)
                        DbContext.Events.AddUpdate(cid, new Data.Api.ContestProblem2(@new));
                await DbContext.SaveChangesAsync();

                StatusMessage = $"Problem {model.ShortName} saved.";
                return RedirectToAction("Home", "JuryMain");
            }
            else
            {
                return Window(model);
            }
        }


        [HttpGet("[action]/{pid}")]
        public async Task<IActionResult> Find(int pid)
        {
            return Content(await DetectProblemConflict(pid, true));
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        public IActionResult Edit(int pid)
        {
            var prob = Problems.Find(pid);
            if (prob == null) return NotFound();
            return Window(prob);
        }


        [HttpPost("{pid}/[action]")]
        [ValidateInAjax]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int cid, int pid, ContestProblem model)
        {
            // here we should split it out..
            var oldprobs = Problems;
            var items = await DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid)
                .ToListAsync();

            var prob = items.FirstOrDefault(cp => cp.ProblemId == pid);
            if (prob == null) return NotFound();
            if (items.Any(cp => cp.ShortName == model.ShortName && cp.ProblemId != pid))
                ModelState.AddModelError("xys::duplicate", "Duplicate short name for problem.");

            if (ModelState.IsValid)
            {
                prob.Color = "#" + model.Color.TrimStart('#');
                prob.AllowSubmit = model.AllowSubmit;
                prob.AllowJudge = model.AllowJudge;
                prob.ShortName = model.ShortName;
                DbContext.ContestProblem.Update(prob);

                items.Sort((cp1, cp2) => cp1.ShortName.CompareTo(cp2.ShortName));
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Rank != i + 1)
                    {
                        items[i].Rank = i + 1;
                        DbContext.ContestProblem.Update(items[i]);
                    }
                }

                await DbContext.SaveChangesAsync();
                MarkChanged();

                var newprobs = await DbContext.GetProblemsAsync(cid);
                foreach (var (@new, old) in newprobs.Join(oldprobs, a => a.ProblemId, a => a.ProblemId, (a, b) => (a, b)))
                    if (@new.ProblemId == pid || @new.Rank != old.Rank)
                        DbContext.Events.AddUpdate(cid, new Data.Api.ContestProblem2(@new));
                await DbContext.SaveChangesAsync();

                StatusMessage = $"Problem {model.ShortName} saved.";
                return RedirectToAction("Home", "JuryMain");
            }
            else
            {
                return Window(model);
            }
        }


        [HttpGet("{pid}/[action]")]
        [ValidateInAjax]
        public IActionResult Delete(int cid, int pid)
        {
            var prob = Problems.Find(pid);
            if (prob == null) return NotFound();

            return AskPost(
                title: "Delete contest problem",
                message: $"Are you sure to delete problem {prob.ShortName}?",
                area: "Contest", ctrl: "JuryProblem", act: "Delete",
                routeValues: new Dictionary<string, string> { ["cid"] = $"{cid}", ["pid"] = $"{pid}" },
                type: MessageType.Danger);
        }


        [HttpPost("{pid}/[action]")]
        public async Task<IActionResult> Delete(int cid, int pid, bool post = true)
        {
            var prob = Problems.Find(pid);
            if (prob == null) return NotFound();

            int tot = await DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid && cp.ProblemId == pid)
                .BatchDeleteAsync();
            DbContext.Events.AddDelete(cid, new Data.Api.ContestProblem2(prob));
            await DbContext.SaveChangesAsync();

            if (tot == 1) MarkChanged();
            StatusMessage = tot == 1
                ? $"Contest problem {prob.ShortName} has been deleted."
                : $"Error when deleting contest problem {prob.ShortName}.";
            return RedirectToAction("Home", "JuryMain");
        }
    }
}
