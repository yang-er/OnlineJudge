using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Services
{
    public class TeamService : ContestContext
    {
        public List<TeamViewSubmissionModel> GetSubmissions()
        {
            int cid = ContestId, teamid = TeamId;

            var query =
                from s in DbContext.Submissions
                where s.ContestId == cid && s.Author == teamid
                join g in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { g.SubmissionId, g.Active }
                join l in DbContext.Languages on s.Language equals l.LangId
                orderby s.SubmissionId descending
                select new TeamViewSubmissionModel
                {
                    Grade = 0,
                    Language = l.ExternalId,
                    SubmissionId = s.SubmissionId,
                    ProblemId = s.ProblemId,
                    Time = s.Time,
                    Verdict = g.Status,
                };

            return query.ToList();
        }

        public TeamViewSubmissionModel GetSubmission(int sid)
        {
            int cid = ContestId, teamid = TeamId;

            var query =
                from g in DbContext.Judgings
                where g.Active && g.SubmissionId == sid
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                where s.ContestId == cid && s.Author == teamid
                join l in DbContext.Languages on s.Language equals l.LangId
                select new TeamViewSubmissionModel
                {
                    Grade = 0,
                    Language = l.Name,
                    SubmissionId = s.SubmissionId,
                    ProblemId = s.ProblemId,
                    Time = s.Time,
                    Verdict = g.Status,
                    CompilerOutput = g.CompileError,
                };

            return query.FirstOrDefault();
        }

        public override string GetUserName()
        {
            return UserName;
        }

        public IEnumerable<Clarification> GetClarification(int clarid, bool needMore = true)
        {
            int cid = ContestId, teamid = TeamId;
            var toSee = DbContext.Clarifications
                .Where(c => c.ClarificationId == clarid && c.ContestId == cid)
                .FirstOrDefault();
            if (!(toSee?.CheckPermission(teamid) ?? true)) return null;

            var ret = Enumerable.Empty<Clarification>();
            ret = ret.Append(toSee);

            if (needMore && toSee.ResponseToId.HasValue)
            {
                int respid = toSee.ResponseToId.Value;
                var toSee2 = DbContext.Clarifications
                    .Where(c => c.ClarificationId == respid && c.ContestId == cid)
                    .FirstOrDefault();
                if (toSee2 != null) ret = ret.Prepend(toSee2);
            }

            return ret;
        }

        public List<Clarification> GetClarifications()
        {
            int cid = ContestId, teamid = TeamId;

            return DbContext.Clarifications
                .Where(c => c.ContestId == cid)
                .Where(c => (c.Sender == null && c.Recipient == null)
                    || c.Recipient == teamid || c.Sender == teamid)
                .Cacheable(TimeSpan.FromSeconds(10))
                .ToList();
        }
    }
}
