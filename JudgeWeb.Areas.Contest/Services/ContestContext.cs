using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using TContest = JudgeWeb.Data.Contest;

namespace JudgeWeb.Areas.Contest.Services
{
    public class ContestContext
    {
        static protected object _add_team_lock = new object();

        protected AppDbContext DbContext { get; private set; }

        public int ContestId { get; private set; }

        public TContest Contest => QueryContest().FirstOrDefault();

        public ContestProblem[] Problems => QueryProblems().ToArray();

        public int UserId { get; private set; }

        public string UserName { get; private set; }

        public int TeamId => Team.TeamId;

        public Team Team => QueryUserTeam(UserId).FirstOrDefault();

        public void Init(int cid, int uid, string uname, AppDbContext adbc)
        {
            ContestId = cid;
            UserId = uid;
            UserName = uname;
            DbContext = adbc;
        }

        public IQueryable<TContest> QueryContest(bool expires = false)
        {
            int cid = ContestId;
            return DbContext.Contests
                .Where(c => c.ContestId == cid)
                .Cacheable(TimeSpan.FromMinutes(5), expires);
        }

        public IQueryable<ContestProblem> QueryProblems(bool expires = false)
        {
            int cid = ContestId;
            return DbContext.ContestProblem
                .Where(cp => cp.ContestId == cid)
                .OrderBy(cp => cp.Rank)
                .Join(
                    inner: DbContext.Problems,
                    outerKeySelector: cp => cp.ProblemId,
                    innerKeySelector: p => p.ProblemId,
                    resultSelector: (cp, p) =>
                        new ContestProblem
                        {
                            AllowJudge = cp.AllowJudge,
                            AllowSubmit = cp.AllowSubmit,
                            Color = cp.Color,
                            ContestId = cp.ContestId,
                            ProblemId = cp.ProblemId,
                            Rank = cp.Rank,
                            ShortName = cp.ShortName,
                            Title = p.Title,
                            TimeLimit = p.TimeLimit,
                            MemoryLimit = p.MemoryLimit,
                        })
                .Cacheable(TimeSpan.FromMinutes(5), expires);
        }

        public IQueryable<TeamCategory> QueryCategories(bool? requirePublic = true)
        {
            int cid = ContestId;

            if (requirePublic.HasValue)
            {
                var query = DbContext.Teams
                    .Where(t => t.ContestId == cid)
                    .Select(t => new { t.CategoryId })
                    .Distinct()
                    .Join(
                        inner: DbContext.TeamCategories,
                        outerKeySelector: i => i.CategoryId,
                        innerKeySelector: c => c.CategoryId,
                        resultSelector: (i, c) => c);

                if (requirePublic.Value)
                    query = query
                        .Where(tc => tc.IsPublic);

                return query.Cacheable(TimeSpan.FromMinutes(5));
            }
            else
            {
                return DbContext.TeamCategories
                    .Cacheable(TimeSpan.FromMinutes(5));
            }
        }

        public IQueryable<TeamAffiliation> QueryAffiliations(bool filtered = true)
        {
            int cid = ContestId;

            if (filtered)
            {
                return DbContext.Teams
                    .Where(t => t.ContestId == cid)
                    .Select(t => new { t.AffiliationId })
                    .Distinct()
                    .Join(
                        inner: DbContext.TeamAffiliations,
                        outerKeySelector: a => a.AffiliationId,
                        innerKeySelector: a => a.AffiliationId,
                        resultSelector: (i, a) => a)
                    .Cacheable(TimeSpan.FromMinutes(5));
            }
            else
            {
                return DbContext.TeamAffiliations
                    .Cacheable(TimeSpan.FromMinutes(5));
            }
        }

        public IQueryable<Team> QueryUserTeam(int uid, bool expiresNow = false)
        {
            int cid = ContestId;
            return DbContext.Teams
                .Where(t => t.UserId == uid && t.ContestId == cid)
                .Cacheable(TimeSpan.FromMinutes(5), expiresNow);
        }

        public IQueryable<Team> QueryTeam(int tid, bool expiresNow = false)
        {
            int cid = ContestId;
            return DbContext.Teams
                .Where(t => t.TeamId == tid && t.ContestId == cid)
                .Cacheable(TimeSpan.FromMinutes(5), expiresNow);
        }

        private IQueryable<ScoreboardOriginalModel> QueryOriginalModels()
        {
            int cid = ContestId;

            return DbContext.Teams
                .Where(t => t.ContestId == cid && t.Status == 1)
                .Join(
                    inner: DbContext.TeamAffiliations,
                    outerKeySelector: t => t.AffiliationId,
                    innerKeySelector: a => a.AffiliationId,
                    resultSelector: (t, a) => new { t, a })
                .GroupJoin(
                    inner: DbContext.RankCache,
                    outerKeySelector: t => new { t.t.TeamId, t.t.ContestId },
                    innerKeySelector: rc => new { rc.TeamId, rc.ContestId },
                    resultSelector: (a, rcs) => new { a.t, a.a, rc = rcs.DefaultIfEmpty() })
                .SelectMany(a => a.rc, (a, rc) => new { a.t, a.a, rc })
                .GroupJoin(
                    inner: DbContext.ScoreCache,
                    outerKeySelector: t => new { t.t.TeamId, t.t.ContestId },
                    innerKeySelector: sc => new { sc.TeamId, sc.ContestId },
                    resultSelector: (a, scs) =>
                        new ScoreboardOriginalModel
                        {
                            Affil = a.a,
                            Rank = a.rc ?? new RankCache(),
                            Score = scs.ToList(),
                            Team = a.t
                        })
                .Cacheable(TimeSpan.FromSeconds(30));
        }

        public IQueryable<ContestTestcase> QueryTestcaseCount()
        {
            return DbContext
                .ContestTestcase(ContestId)
                .Cacheable(TimeSpan.FromMinutes(20));
        }

        public IQueryable<Language> QueryLanguages()
        {
            return DbContext.Languages
                .Cacheable(TimeSpan.FromMinutes(10));
        }

        public ScoreboardFullViewModel GetTotalBoard(bool isPublic, bool isJury)
        {
            if (Contest is null) return null;
            var categories = QueryCategories(!isJury).ToList();
            var affiliations = QueryAffiliations().ToList();
            var origModel = QueryOriginalModels().ToList();

            return new ScoreboardFullViewModel
            {
                Affiliations = affiliations,
                Categories = categories,
                Contest = Contest,
                IsPublic = isPublic && !isJury,
                Problems = Problems,
                RankCache = origModel,
            };
        }

        public ScoreboardSingleViewModel GetOneTeam(int tid)
        {
            int cid = ContestId;
            var model = DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == tid)
                .Join(
                    inner: DbContext.TeamAffiliations,
                    outerKeySelector: t => t.AffiliationId,
                    innerKeySelector: a => a.AffiliationId,
                    resultSelector: (t, a) => new { t, a })
                .Join(
                    inner: DbContext.TeamCategories,
                    outerKeySelector: t => t.t.CategoryId,
                    innerKeySelector: c => c.CategoryId,
                    resultSelector: (t, c) => new { t.t, t.a, c })
                .GroupJoin(
                    inner: DbContext.RankCache,
                    outerKeySelector: t => new { t.t.TeamId, t.t.ContestId },
                    innerKeySelector: rc => new { rc.TeamId, rc.ContestId },
                    resultSelector: (a, rcs) => new { a.t, a.a, a.c, rc = rcs.DefaultIfEmpty() })
                .SelectMany(a => a.rc, (a, rc) => new { a.t, a.a, a.c, rc })
                .GroupJoin(
                    inner: DbContext.ScoreCache,
                    outerKeySelector: t => new { t.t.TeamId, t.t.ContestId },
                    innerKeySelector: sc => new { sc.TeamId, sc.ContestId },
                    resultSelector: (a, scs) => new { a.t, a.rc, a.a, a.c, sc = scs.ToList() })
                .Cacheable(TimeSpan.FromSeconds(15))
                .FirstOrDefault();

            if (model is null) return null;

            return new ScoreboardSingleViewModel
            {
                Team = model.t,
                Rank = model.rc,
                Affiliation = model.a,
                Problems = Problems,
                Category = model.c,
                Contest = Contest,
                Score = model.sc,
            };
        }

        public virtual string GetUserName()
        {
            return UserName ?? "None";
        }

        public int CreateTeam(Team team)
        {
            EntityEntry<Team> t;
            int cid = ContestId;

            lock (_add_team_lock)
            {
                var teamId = 1 + DbContext.Teams
                    .Count(tt => tt.ContestId == cid);
                team.TeamId = teamId;
                t = DbContext.Teams.Add(team);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    Type = AuditLog.TargetType.Contest,
                    Time = DateTime.Now,
                    Resolved = true,
                    UserName = GetUserName(),
                    ContestId = Contest.ContestId,
                    Comment = $"add team t{teamId} " +
                        $"a{team.AffiliationId}, " +
                        $"c{team.CategoryId}, " +
                        $"u{team.UserId}, " +
                        $"{team.TeamName}",
                    EntityId = teamId,
                });

                DbContext.SaveChanges();
            }

            return t.Entity.TeamId;
        }

        public int CreateSubmission(Submission sub)
        {
            var s = DbContext.Submissions.Add(sub);
            DbContext.SaveChanges();

            DbContext.Judgings.Add(new Judging
            {
                SubmissionId = s.Entity.SubmissionId,
                FullTest = false,
                Active = true,
                Status = Verdict.Pending,
                RejudgeId = 0,
            });

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = ContestId,
                Resolved = false,
                Comment = "added via team-page",
                EntityId = s.Entity.SubmissionId,
                Time = sub.Time.DateTime,
                UserName = GetUserName(),
            });

            DbContext.SaveChanges();
            Features.Scoreboard.RefreshService.Notify();
            return s.Entity.SubmissionId;
        }

        public int SendClarification(Clarification clar)
        {
            var entity = DbContext.Clarifications.Add(clar);
            DbContext.SaveChanges();

            DbContext.AuditLogs.Add(new AuditLog
            {
                ContestId = ContestId,
                Resolved = true,
                Time = clar.SubmitTime,
                UserName = GetUserName(),
                Comment = "added",
                EntityId = entity.Entity.ClarificationId,
            });

            DbContext.SaveChanges();
            return entity.Entity.ClarificationId;
        }
    }
}
