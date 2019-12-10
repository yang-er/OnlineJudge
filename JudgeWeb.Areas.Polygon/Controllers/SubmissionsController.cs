using JudgeWeb.Areas.Polygon.Models;
using JudgeWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Polygon.Controllers
{
    [Area("Polygon")]
    [Route("[area]/{pid}/[controller]")]
    public class SubmissionsController : Controller3
    {
        public SubmissionsController(AppDbContext db) : base(db, true) { }


        [HttpGet]
        public async Task<IActionResult> Submissions(int pid)
        {
            var query =
                from s in DbContext.Submissions
                where s.ExpectedResult != null && s.ProblemId == pid
                join l in DbContext.Languages on s.Language equals l.LangId
                join u in DbContext.Users on s.Author equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                join j in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { j.SubmissionId, j.Active }
                select new ListSubmissionModel
                {
                    SubmissionId = s.SubmissionId,
                    JudgingId = j.JudgingId,
                    Language = l.Name,
                    Result = j.Status,
                    Time = s.Time,
                    UserName = u.UserName ?? "SYSTEM",
                    Expected = s.ExpectedResult.Value,
                    ExecutionTime = j.ExecuteTime,
                };

            var result = await query.ToListAsync();
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
            return View(result);
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
                join h in DbContext.JudgeHosts on j.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new ViewSubmissionModel
                {
                    SubmissionId = s.SubmissionId,
                    Status = j.Status,
                    ExecuteMemory = j.ExecuteMemory,
                    ExecuteTime = j.ExecuteTime,
                    Judging = j,
                    JudgingId = j.JudgingId,
                    ServerId = j.ServerId,
                    LanguageId = s.Language,
                    Time = s.Time,
                    SourceCode = s.SourceCode,
                    CompileError = j.CompileError,
                    Author = s.Author,
                    ServerName = h.ServerName ?? "UNKNOWN",
                    LanguageName = l.Name,
                    LanguageExternalId = l.ExternalId,
                    TimeFactor = l.TimeFactor,
                };

            var model = await query.FirstOrDefaultAsync();
            if (model == null) return NotFound();
            model.TimeLimit = Problem.TimeLimit;
            
            var grades =
                from j in DbContext.Judgings
                where j.SubmissionId == sid
                join h in DbContext.JudgeHosts on j.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new { j, n = h.ServerName ?? "" };
            var gs = grades.ToList();
            model.AllJudgings = gs.Select(a => (a.j, a.n));

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
    }
}
