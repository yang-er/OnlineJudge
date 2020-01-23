using EFCore.BulkExtensions;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Controllers
{
    [Area("Contest")]
    [Route("[area]/{cid}/jury/[controller]")]
    public class RejudgingsController : JuryControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(int cid)
        {
            var query =
                from r in DbContext.Rejudges
                where r.ContestId == cid
                join u in DbContext.Users on r.IssuedBy equals u.Id into uu1
                from u1 in uu1.DefaultIfEmpty()
                join u in DbContext.Users on r.OperatedBy equals u.Id into uu2
                from u2 in uu2.DefaultIfEmpty()
                select new Rejudge(r, u1.UserName, u2.UserName);
            var model = await query.ToListAsync();

            var query2 =
                from j in DbContext.Judgings
                where (from r in DbContext.Rejudges
                       where r.ContestId == cid && r.OperatedBy == null
                       select (int?)r.RejudgeId).Contains(j.RejudgeId)
                group 1 by new { j.RejudgeId, j.Status } into g
                select new { g.Key, Cnt = g.Count() };
            var q2 = await query2.ToListAsync();

            foreach (var qqq in q2.GroupBy(a => a.Key.RejudgeId))
            {
                int tot = qqq.Sum(a => a.Cnt);
                int ped = qqq
                    .Where(a => a.Key.Status == Verdict.Pending || a.Key.Status == Verdict.Running)
                    .DefaultIfEmpty()
                    .Sum(a => a?.Cnt) ?? 0;
                model.First(r => r.RejudgeId == qqq.Key).Ready = (tot, ped);
            }

            return View(model);
        }


        [HttpGet("{rid}")]
        public async Task<IActionResult> Detail(int cid, int rid)
        {
            var rquery =
                from r in DbContext.Rejudges
                where r.RejudgeId == rid && r.ContestId == cid
                join u in DbContext.Users on r.IssuedBy equals u.Id into uu
                from u1 in uu.DefaultIfEmpty()
                join u in DbContext.Users on r.OperatedBy equals u.Id into uu2
                from u2 in uu2.DefaultIfEmpty()
                select new Rejudge(r, u1.UserName, u2.UserName);

            var model = await rquery.SingleOrDefaultAsync();
            if (model == null) return NotFound();

            var jquery =
                from j in DbContext.Judgings
                where j.RejudgeId == rid
                join s in DbContext.Submissions on j.SubmissionId equals s.SubmissionId
                join j2 in DbContext.Judgings on j.PreviousJudgingId equals j2.JudgingId
                orderby s.Time descending
                select new { s.ProblemId, s.Language, s.Time, s.Author, j, j2 };
            var jitem = await jquery.ToListAsync();

            ViewBag.Teams = await DbContext.GetTeamNameAsync(cid);
            ViewBag.Judgings = jitem.Select(a =>
                (a.j, a.j2, a.ProblemId, a.Language, a.Time, a.Author));
            return View(model);
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> Add(int cid)
        {
            ViewBag.Teams = await DbContext.GetTeamNameAsync(cid);
            ViewBag.Judgehosts = await DbContext.JudgeHosts.ToListAsync();
            return View(new AddRejudgingModel());
        }


        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int cid, AddRejudgingModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var r = DbContext.Rejudges.Add(new Rejudge
            {
                ContestId = Contest.ContestId,
                Reason = model.Reason,
                StartTime = DateTimeOffset.Now,
                IssuedBy = int.Parse(UserManager.GetUserId(User))
            });

            await DbContext.SaveChangesAsync();
            int rejid = r.Entity.RejudgeId;
            IQueryable<int> q2;

            if (model.Submission.HasValue)
            {
                int sid = model.Submission.Value;
                q2 = DbContext.Submissions
                    .Where(s => s.ContestId == cid && s.RejudgeId == null && s.SubmissionId == sid)
                    .Select(s => s.SubmissionId);
            }
            else
            {
                var submissionSource = DbContext.Submissions
                    .Where(s => s.ContestId == cid && s.RejudgeId == null);

                var probs = model.Problems ?? Array.Empty<int>();
                if (probs.Length > 0)
                    submissionSource = submissionSource.Where(s => probs.Contains(s.ProblemId));

                var teams = model.Teams ?? Array.Empty<int>();
                if (teams.Length > 0)
                    submissionSource = submissionSource.Where(s => teams.Contains(s.Author));

                var langs = model.Languages ?? Array.Empty<string>();
                if (langs.Length > 0)
                    submissionSource = submissionSource.Where(s => langs.Contains(s.Language));

                if (model.TimeBefore.TryParseAsTimeSpan(out var tb) && tb.HasValue)
                {
                    var timed = (Contest.StartTime ?? DateTimeOffset.Now) + tb.Value;
                    submissionSource = submissionSource.Where(s => s.Time <= timed);
                }

                if (model.TimeAfter.TryParseAsTimeSpan(out var ta) && ta.HasValue)
                {
                    var timed = (Contest.StartTime ?? DateTimeOffset.Now) + ta.Value;
                    submissionSource = submissionSource.Where(s => s.Time >= timed);
                }

                var sjSource = submissionSource
                    .Join(
                        inner: DbContext.Judgings,
                        outerKeySelector: s => new { s.SubmissionId, Active = true },
                        innerKeySelector: j => new { j.SubmissionId, j.Active },
                        resultSelector: (s, j) => new { s, j });

                var hosts = model.Judgehosts ?? Array.Empty<string>();
                if (hosts.Length > 0)
                    sjSource = sjSource.Where(a => hosts.Contains(a.j.Server));

                var verds = model.Verdicts ?? Array.Empty<Verdict>();
                if (model.Verdicts.Length > 0)
                    sjSource = sjSource.Where(a => verds.Contains(a.j.Status));

                q2 = sjSource.Select(a => a.s.SubmissionId);
            }

            int count = await DbContext.Submissions
                .Where(s => q2.Contains(s.SubmissionId))
                .BatchUpdateAsync(s => new Submission { RejudgeId = rejid });

            if (count == 0)
            {
                await DbContext.Rejudges
                    .Where(rr => rr.RejudgeId == rejid)
                    .BatchDeleteAsync();
                StatusMessage = "Error no submissions was rejudged.";
                return RedirectToAction(nameof(List));
            }
            else
            {
                int tok = await DbContext.Database.ExecuteSqlCommandAsync(
                    "INSERT INTO [Judgings] ([Active], [SubmissionId], [FullTest], [Status], [TotalScore], [ExecuteTime], [ExecuteMemory], [RejudgeId], [PreviousJudgingId])\n      " +
                    "SELECT 0 AS [Active], [s].[SubmissionId], [j].[FullTest], 8 AS [Status], 0 AS [TotalScore], 0 AS [ExecuteTime], 0 AS [ExecuteMemory], @__rejid_0 AS [RejudgeId], [j].[JudgingId] AS [PreviousJudgingId]\n      " +
                    "FROM [Submissions] AS [s]\n      " +
                    "INNER JOIN [Judgings] AS [j] ON ([s].[SubmissionId] = [j].[SubmissionId]) AND ([j].[Active] = 1)\n      " +
                    "WHERE [s].[RejudgeId] = @__rejid_0",
                    new System.Data.SqlClient.SqlParameter("__rejid_0", rejid));
                StatusMessage = $"{tok} submissions will be rejudged.";
                return RedirectToAction(nameof(Detail), new { rid = rejid });
            }
        }


        [HttpGet("{rid}/[action]")]
        [ValidateInAjax]
        public IActionResult Repeat()
        {
            return AskPost(
                title: "Repeat rejudging",
                message: "This will create a new rejudging with the same submissions as this rejudging.",
                area: "Contest", ctrl: "Rejudgings", act: "Repeat",
                type: MessageType.Primary);
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Repeat(int cid, int rid)
        {
            var rej = await DbContext.Rejudges
                .Where(r => r.RejudgeId == rid && r.ContestId == cid)
                .SingleOrDefaultAsync();
            if (rej == null) return NotFound();

            if (rej.OperatedBy == null)
            {
                StatusMessage = "Error rejudging has not been finished.";
                return RedirectToAction(nameof(Detail));
            }

            var r2e = DbContext.Rejudges.Add(new Rejudge
            {
                ContestId = Contest.ContestId,
                StartTime = DateTimeOffset.Now,
                IssuedBy = int.Parse(UserManager.GetUserId(User)),
                Reason = "repeat: " + rej.Reason,
            });

            await DbContext.SaveChangesAsync();
            var rejid = r2e.Entity.RejudgeId;

            int count = await DbContext.Submissions
                .Where(s => (from j in DbContext.Judgings
                             where j.RejudgeId == rid
                             select j.SubmissionId).Contains(s.SubmissionId) && s.RejudgeId == null)
                .BatchUpdateAsync(s => new Submission { RejudgeId = rejid });

            if (count == 0)
            {
                await DbContext.Rejudges
                    .Where(rr => rr.RejudgeId == rejid)
                    .BatchDeleteAsync();
                StatusMessage = "Error no submissions was rejudged.";
                return RedirectToAction(nameof(Detail));
            }
            else
            {
                int tok = await DbContext.Database.ExecuteSqlCommandAsync(
                    "INSERT INTO [Judgings] ([Active], [SubmissionId], [FullTest], [Status], [TotalScore], [ExecuteTime], [ExecuteMemory], [RejudgeId], [PreviousJudgingId])\n      " +
                    "SELECT 0 AS [Active], [s].[SubmissionId], [j].[FullTest], 8 AS [Status], 0 AS [TotalScore], 0 AS [ExecuteTime], 0 AS [ExecuteMemory], @__rejid_0 AS [RejudgeId], [j].[JudgingId] AS [PreviousJudgingId]\n      " +
                    "FROM [Submissions] AS [s]\n      " +
                    "INNER JOIN [Judgings] AS [j] ON ([s].[SubmissionId] = [j].[SubmissionId]) AND ([j].[Active] = 1)\n      " +
                    "WHERE [s].[RejudgeId] = @__rejid_0",
                    new System.Data.SqlClient.SqlParameter("__rejid_0", rejid));
                StatusMessage = $"{tok} submissions will be rejudged.";
                return RedirectToAction(nameof(Detail), new { rid = rejid });
            }
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int cid, int rid)
        {
            var rej = await DbContext.Rejudges
                .Where(r => r.ContestId == cid && r.RejudgeId == rid && r.EndTime == null)
                .SingleOrDefaultAsync();
            if (rej == null) return NotFound();

            var cancelJudgings = await DbContext.Judgings
                .Where(j => j.RejudgeId == rid && j.Status == Verdict.Pending)
                .BatchDeleteAsync();

            var resetSubmits = await DbContext.Submissions
                .Where(s => s.RejudgeId == rid)
                .BatchUpdateAsync(
                    updateValues: new Submission(),
                    updateColumns: new List<string> { nameof(Submission.RejudgeId) });

            rej.EndTime = DateTimeOffset.Now;
            rej.Applied = false;
            rej.OperatedBy = int.Parse(UserManager.GetUserId(User));
            DbContext.Rejudges.Update(rej);
            await DbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Detail));
        }


        [HttpPost("{rid}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int cid, int rid)
        {
            var rej = await DbContext.Rejudges
                .Where(r => r.ContestId == cid && r.RejudgeId == rid && r.EndTime == null)
                .SingleOrDefaultAsync();
            if (rej == null) return NotFound();

            var pending = await DbContext.Judgings
                .Where(j => j.RejudgeId == rid && j.Status == Verdict.Pending || j.Status == Verdict.Running)
                .CountAsync();

            if (pending > 0)
            {
                StatusMessage = "Error some submissions are not ready.";
                return RedirectToAction(nameof(Detail));
            }

            var applyNew = await DbContext.Judgings
                .Where(j => j.RejudgeId == rid)
                .BatchUpdateAsync(j => new Judging { Active = true });

            var oldJudgings = DbContext.Judgings
                .Where(j => j.RejudgeId == rid)
                .Select(j => j.PreviousJudgingId);
            var supplyOld = await DbContext.Judgings
                .Where(j => oldJudgings.Contains(j.JudgingId))
                .BatchUpdateAsync(j => new Judging { Active = false });

            var oldSubmissions = DbContext.Judgings
                .Where(j => j.RejudgeId == rid)
                .Select(j => j.SubmissionId);
            var resetSubmit = await DbContext.Submissions
                .Where(s => oldSubmissions.Contains(s.SubmissionId))
                .BatchUpdateAsync(new Submission(), new List<string> { nameof(Submission.RejudgeId) });

            rej.Applied = true;
            rej.EndTime = DateTimeOffset.Now;
            rej.OperatedBy = int.Parse(UserManager.GetUserId(User));
            DbContext.Rejudges.Update(rej);
            await DbContext.SaveChangesAsync();

            RefreshScoreboardCache(cid);
            StatusMessage = "Rejudging applied. Scoreboard cache will be refreshed.";
            return RedirectToAction(nameof(Detail));
        }
    }
}
