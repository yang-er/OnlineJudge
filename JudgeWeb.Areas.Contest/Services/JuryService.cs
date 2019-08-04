using EntityFrameworkCore.Cacheable;
using JudgeWeb.Areas.Contest.Models;
using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JudgeWeb.Areas.Contest.Services
{
    public class JuryService : ContestContext
    {
        public IEnumerable<JuryListTeamModel> GetTeams()
        {
            int cid = ContestId;

            var query =
                from t in DbContext.Teams
                where t.ContestId == cid && t.Status != 3
                join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                join c in DbContext.TeamCategories on t.CategoryId equals c.CategoryId
                join u in DbContext.Users on t.UserId equals u.Id into uu
                from u in uu.DefaultIfEmpty()
                select new JuryListTeamModel
                {
                    Affiliation = a.ExternalId,
                    AffiliationName = a.FormalName,
                    Category = c.Name,
                    UserName = u.UserName,
                    Status = t.Status,
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    RegisterTime = t.RegisterTime,
                };

            return query
                .Cacheable(TimeSpan.FromSeconds(10))
                .ToList();
        }

        public string ChangeState(string target)
        {
            var contest = Contest;
            var state = contest.GetState();

            if (target == "startnow")
            {
                if (!contest.EndTime.HasValue)
                    return "Error when trying to set end time.";
                var now = DateTimeOffset.Now.AddSeconds(30);
                DateTimeOffset old;

                if (contest.StartTime.HasValue)
                {
                    // from scheduled to start
                    if (contest.StartTime.Value < now)
                        return "Error starting contest for the remaining time is less than 30 secs.";
                    old = contest.StartTime.Value;
                }
                else
                {
                    // from delay to start
                    old = DateTimeOffset.UnixEpoch;
                }

                contest.StartTime = now;
                contest.EndTime = now + (contest.EndTime.Value - old);
                if (contest.FreezeTime.HasValue)
                    contest.FreezeTime = now + (contest.FreezeTime.Value - old);
                if (contest.UnfreezeTime.HasValue)
                    contest.UnfreezeTime = now + (contest.UnfreezeTime.Value - old);
            }
            else if (target == "freeze")
            {
                if (state != ContestState.Started)
                    return "Error contest state occured.";

                contest.FreezeTime = DateTimeOffset.Now;
            }
            else if (target == "endnow")
            {
                if (state != ContestState.Started && state != ContestState.Frozen)
                    return "Error contest state occured.";

                var now = DateTimeOffset.Now;
                contest.EndTime = now;

                if (contest.FreezeTime.HasValue && contest.FreezeTime.Value > now)
                    contest.FreezeTime = now;
            }
            else if (target == "unfreeze")
            {
                if (state != ContestState.Ended)
                    return "Error contest state occured.";

                contest.UnfreezeTime = DateTimeOffset.Now;
            }
            else if (target == "delay")
            {
                if (state != ContestState.ScheduledToStart)
                    return "Error contest state occured.";

                var old = contest.StartTime.Value;
                contest.StartTime = null;
                if (contest.EndTime.HasValue)
                    contest.EndTime = DateTimeOffset.UnixEpoch + (contest.EndTime.Value - old);
                if (contest.FreezeTime.HasValue)
                    contest.FreezeTime = DateTimeOffset.UnixEpoch + (contest.FreezeTime.Value - old);
                if (contest.UnfreezeTime.HasValue)
                    contest.UnfreezeTime = DateTimeOffset.UnixEpoch + (contest.UnfreezeTime.Value - old);
            }

            DbContext.Contests.Update(contest);

            DbContext.AuditLogs.Add(new AuditLog
            {
                Comment = "modified time",
                ContestId = ContestId,
                EntityId = ContestId,
                Resolved = true,
                Time = DateTimeOffset.Now,
                Type = AuditLog.TargetType.Contest,
                UserName = GetUserName(),
            });

            DbContext.SaveChanges();
            QueryContest(true).FirstOrDefault();
            return "Contest state changed.";
        }

        public IDictionary<int, string> GetUnregisteredUsers()
        {
            int cid = Contest.ContestId;

            var registered = DbContext.Teams
                .Where(t => t.ContestId == cid)
                .Select(t => t.UserId)
                .Distinct();

            var query = DbContext.Users
                .Where(u => !registered.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .Cacheable(TimeSpan.FromMinutes(1));

            return query.ToDictionary(t => t.Id, t => t.UserName);
        }

        public void RefreshScoreboardCache()
        {
            DbContext.AuditLogs.Add(new AuditLog
            {
                Type = AuditLog.TargetType.Contest,
                Time = DateTimeOffset.Now,
                Resolved = false,
                UserName = GetUserName(),
                ContestId = Contest.ContestId,
                Comment = "updated",
                EntityId = Contest.ContestId,
            });

            DbContext.SaveChanges();
            Features.Scoreboard.RefreshService.Notify();
        }

        public void DeleteTeam(Team team)
        {
            var oldUid = team.UserId;
            team.Status = 3;
            team.UserId = 0;
            DbContext.Teams.Update(team);

            DbContext.AuditLogs.Add(new AuditLog
            {
                Type = AuditLog.TargetType.Contest,
                Time = DateTimeOffset.Now,
                Resolved = true,
                UserName = GetUserName(),
                ContestId = Contest.ContestId,
                Comment = $"deleted team {team.TeamName} (t{team.TeamId}, u{oldUid})",
                EntityId = team.TeamId,
            });

            DbContext.SaveChanges();
        }

        public void UpdateTeam(Team team, JuryEditTeamModel model)
        {
            DbContext.AuditLogs.Add(new AuditLog
            {
                Type = AuditLog.TargetType.Contest,
                Time = DateTimeOffset.Now,
                Resolved = true,
                UserName = UserName,
                ContestId = Contest.ContestId,
                EntityId = team.TeamId,
                Comment = $"edit team t{team.TeamId} " +
                    $"a{team.AffiliationId}->{model.AffiliationId}, " +
                    $"c{team.CategoryId}->{model.CategoryId}, " +
                    $"{team.TeamName}->{model.TeamName}",
            });

            team.TeamName = model.TeamName;
            team.CategoryId = model.CategoryId;
            team.AffiliationId = model.AffiliationId;

            DbContext.Teams.Update(team);
            DbContext.SaveChanges();
        }

        public Dictionary<int, string> TeamName
        {
            get
            {
                int cid = ContestId;
                return DbContext.Teams
                    .Where(c => c.ContestId == cid && c.Status == 1)
                    .Select(c => new { c.TeamId, c.TeamName })
                    .Cacheable(TimeSpan.FromMinutes(5))
                    .ToDictionary(a => a.TeamId, a => a.TeamName);
            }
        }

        public int GetPendingTeamCount()
        {
            int cid = ContestId;
            return DbContext.Teams
                .Where(t => t.Status == 0 && t.ContestId == cid)
                .Cacheable(TimeSpan.FromSeconds(10))
                .Count();
        }

        public int GetUnansweredClarificationCount()
        {
            int cid = ContestId;
            return DbContext.Clarifications
                .Where(c => c.ContestId == cid && !c.Answered)
                .Cacheable(TimeSpan.FromSeconds(10))
                .Count();
        }

        public void UpdateTeam(Team team, string act)
        {
            team.Status = act == "accept" ? 1 : 2;
            DbContext.Teams.Update(team);

            DbContext.AuditLogs.Add(new AuditLog
            {
                EntityId = team.TeamId,
                Type = AuditLog.TargetType.Contest,
                Time = DateTimeOffset.Now,
                Resolved = true,
                UserName = UserName,
                ContestId = Contest.ContestId,
                Comment = $"{act} team"
            });

            DbContext.SaveChanges();
            QueryTeam(team.TeamId, true).FirstOrDefault();
            if (team.UserId != 0)
                QueryUserTeam(team.UserId, true).FirstOrDefault();
        }

        public JuryListClarificationModel GetClarifications()
        {
            int cid = ContestId;

            var query = DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.Recipient == null)
                .Cacheable(TimeSpan.FromSeconds(5))
                .ToList();

            var teamName = TeamName;

            foreach (var item in query)
            {
                item.TeamName = item.Sender.HasValue ? teamName.GetValueOrDefault(item.Sender.Value) : null;
            }

            return new JuryListClarificationModel
            {
                AllClarifications = query,
                Problems = Problems,
                JuryName = UserName,
            };
        }

        public bool ClaimClarification(int clarid, bool claim)
        {
            var cid = ContestId;
            var clar = DbContext.Clarifications
                .Where(c => c.ClarificationId == clarid && c.ContestId == cid)
                .FirstOrDefault();
            if (clar is null) return false;

            if (clar.JuryMember == null && claim)
                clar.JuryMember = UserName;
            else if (clar.JuryMember == UserName && !claim)
                clar.JuryMember = null;
            else
                return false;

            DbContext.Clarifications.Update(clar);
            DbContext.SaveChanges();
            return true;
        }

        public bool SetAnswerClarification(int clarid, bool answer)
        {
            var cid = ContestId;
            var clar = DbContext.Clarifications
                .Where(c => c.ClarificationId == clarid && c.ContestId == cid)
                .FirstOrDefault();
            if (clar is null) return false;

            clar.Answered = answer;
            DbContext.Clarifications.Update(clar);
            DbContext.SaveChanges();
            return true;
        }

        public JuryViewClarificationModel GetClarification(int clarid)
        {
            var cid = ContestId;
            var clar = DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarid)
                .FirstOrDefault();

            if (clar == null) return null;
            var query = Enumerable.Repeat(clar, 1);

            if (!clar.Sender.HasValue && clar.ResponseToId.HasValue)
            {
                var clarid2 = clar.ResponseToId.Value;
                var clar2 = DbContext.Clarifications
                    .Where(c => c.ContestId == cid && c.ClarificationId == clarid2)
                    .FirstOrDefault();
                if (clar2 != null) query = query.Prepend(clar2);
            }
            else if (clar.Sender.HasValue)
            {
                var otherClars = DbContext.Clarifications
                    .Where(c => c.ContestId == cid)
                    .Where(c => c.ResponseToId == clarid && c.Sender == null)
                    .ToList();

                query = query.Concat(otherClars);
            }

            var teamName = TeamName;

            foreach (var item in query)
            {
                item.TeamName = item.Sender.HasValue
                    ? teamName.GetValueOrDefault(item.Sender.Value)
                    : item.Recipient.HasValue
                    ? teamName.GetValueOrDefault(item.Recipient.Value)
                    : null;
            }

            return new JuryViewClarificationModel
            {
                Associated = query,
                Main = query.First(),
                Problems = Problems,
                Teams = teamName,
                UserName = UserName,
            };
        }

        public Clarification GetClarification(int clarid, bool expires)
        {
            int cid = ContestId;
            return DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarid)
                .FirstOrDefault();
        }

        public IEnumerable<(Verdict, DateTimeOffset)> GetStatistics()
        {
            var query =
                from s in DbContext.Submissions
                join g in DbContext.Judgings
                    on new { s.SubmissionId, Active = true }
                    equals new { g.SubmissionId, g.Active }
                select new { s.Time, g.Status };

            return query.Cacheable(TimeSpan.FromMinutes(1))
                .ToList().Select(a => (a.Status, a.Time));
        }

        public IEnumerable<JuryListSubmissionModel> GetSubmissions(int? teamid = null)
        {
            var cid = ContestId;
            var tc = QueryTestcaseCount().ToDictionary(t => t.ProblemId);
            var lang = QueryLanguages().ToDictionary(l => l.LangId, l => l.ExternalId);

            var submissions = DbContext.Submissions
                .Where(s => s.ContestId == cid);

            if (teamid.HasValue)
                submissions = submissions
                    .Where(s => s.Author == teamid.Value);

            var subs = (
                from s in submissions
                orderby s.SubmissionId descending
                join g in DbContext.Judgings on new { s.SubmissionId, Active = true } equals new { g.SubmissionId, g.Active }
                join t in DbContext.Teams on s.Author equals t.TeamId
                join d in DbContext.Details on g.JudgingId equals d.JudgingId into dd
                from d in dd.DefaultIfEmpty()
                select new { g.Status, s.Time, s.ProblemId, s.Language, s.Author, s.SubmissionId, t.TeamName, d = (Verdict?)d.Status }
            ).Cacheable(TimeSpan.FromSeconds(20)).ToList();

            return subs
                .GroupBy(
                    keySelector: s => new { s.Status, s.ProblemId, s.Time, s.Language, s.Author, s.SubmissionId, s.TeamName },
                    elementSelector: a => a.d)
                .Select(g =>
                    new JuryListSubmissionModel
                    {
                        Details = g.Select(i => i ?? Verdict.Pending),
                        Language = lang[g.Key.Language],
                        Result = g.Key.Status,
                        SubmissionId = g.Key.SubmissionId,
                        TeamId = g.Key.Author,
                        TeamName = g.Key.TeamName,
                        Problem = tc.GetValueOrDefault(g.Key.ProblemId),
                        Time = g.Key.Time,
                    });
        }

        public JuryViewSubmissionModel GetSubmission(int sid, int? gid)
        {
            int cid = ContestId;
            var grade = DbContext.Judgings
                .Where(g => g.SubmissionId == sid);
            if (gid.HasValue) grade = grade.Where(g => g.JudgingId == gid);
            else grade = grade.Where(g => g.Active);

            var query =
                from g in grade
                join s in DbContext.Submissions on g.SubmissionId equals s.SubmissionId
                where s.ContestId == cid
                join p in DbContext.Problems on s.ProblemId equals p.ProblemId
                join h in DbContext.JudgeHosts on g.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new JuryViewSubmissionModel
                {
                    SubmissionId = s.SubmissionId,
                    Status = g.Status,
                    ContestId = cid,
                    ExecuteMemory = g.ExecuteMemory,
                    ExecuteTime = g.ExecuteTime,
                    Judging = g,
                    JudgingId = g.JudgingId,
                    ServerId = g.ServerId,
                    LanguageId = s.Language,
                    Time = s.Time,
                    SourceCode = s.SourceCode,
                    CompileError = g.CompileError,
                    ProblemTitle = p.Title,
                    Author = s.Author,
                    ProblemId = p.ProblemId,
                    TimeLimit = p.TimeLimit,
                    ServerName = h.ServerName ?? "UNKNOWN",
                };

            var model = query.FirstOrDefault();

            var grades =
                from g in DbContext.Judgings
                where g.SubmissionId == sid
                join h in DbContext.JudgeHosts on g.ServerId equals h.ServerId into hh
                from h in hh.DefaultIfEmpty()
                select new { g, n = h.ServerName ?? "-" };
            var gs = grades.ToList();
            model.AllJudgings = gs.Select(a => (a.g, a.n));

            model.Details = DbContext.Details
                .Where(d => d.JudgingId == model.JudgingId)
                .ToList();

            return model;
        }

        public void UpdateClarificationBeforeInsertOne(Clarification clar)
        {
            DbContext.Clarifications.Update(clar);
        }

        public Dictionary<int, string> GetAllProblems()
        {
            return DbContext.Problems
                .Select(p => new { p.ProblemId, p.Title })
                .Cacheable(TimeSpan.FromMinutes(5))
                .ToDictionary(a => a.ProblemId, a => a.Title);
        }

        private bool InSequence(params DateTimeOffset?[] dateTimes)
        {
            if (dateTimes.Length == 0) return true;
            var item = dateTimes[0];

            for (int i = 1; i < dateTimes.Length; i++)
            { 
                if (dateTimes[i].HasValue)
                {
                    if (item.HasValue && item.Value > dateTimes[i].Value)
                        return false;
                    item = dateTimes[i];
                }
            }

            return true;
        }

        public (string, bool) UpdateContest(JuryEditModel model)
        {
            (string, bool) SolveAndUpdate()
            {
                var cst = Contest;
                DateTimeOffset @base;
                DateTimeOffset? startTime, endTime, freezeTime, unfreezeTime;
                bool contestTimeChanged = false;

                if (model.DefaultCategory != 0)
                {
                    var cts = QueryCategories(null).ToList();
                    if (!cts.Any(c => c.CategoryId == model.DefaultCategory))
                        return ("Error default category.", false);
                }

                if (model.Problems.Select(p => p.Value.ProblemId).Distinct().Count() != model.Problems.Count)
                    return ("Error duplicate problems.", false);

                if (!string.IsNullOrEmpty(model.StartTime)
                    && string.IsNullOrEmpty(model.StopTime))
                    return ("Error end time for the start time has been filled.", false);

                if (string.IsNullOrEmpty(model.StartTime))
                {
                    @base = DateTimeOffset.UnixEpoch;
                    startTime = null;
                }
                else
                {
                    @base = DateTimeOffset.Parse(model.StartTime);
                    startTime = @base;
                }

                if (string.IsNullOrWhiteSpace(model.StopTime))
                {
                    endTime = null;
                }
                else
                {
                    model.StopTime.TryParseAsTimeSpan(out var ts);
                    endTime = @base + ts.Value;
                }

                if (string.IsNullOrWhiteSpace(model.FreezeTime))
                {
                    freezeTime = null;
                }
                else
                {
                    model.FreezeTime.TryParseAsTimeSpan(out var ts);
                    freezeTime = @base + ts.Value;
                }

                if (string.IsNullOrWhiteSpace(model.UnfreezeTime))
                {
                    unfreezeTime = null;
                }
                else
                {
                    model.UnfreezeTime.TryParseAsTimeSpan(out var ts);
                    unfreezeTime = @base + ts.Value;
                }

                if (!InSequence(startTime, freezeTime, endTime, unfreezeTime))
                    return ("Error time sequence.", false);

                if (startTime != cst.StartTime
                    || endTime != cst.EndTime
                    || freezeTime != cst.FreezeTime
                    || unfreezeTime != cst.UnfreezeTime)
                    contestTimeChanged = true;

                cst.ShortName = model.ShortName;
                cst.Name = model.Name;
                cst.RankingStrategy = model.RankingStrategy;
                cst.BronzeMedal = model.BronzeMedal;
                cst.EndTime = endTime;
                cst.FreezeTime = freezeTime;
                cst.GoldMedal = model.GoldenMedal;
                cst.IsPublic = model.IsPublic;
                cst.RegisterDefaultCategory = model.DefaultCategory;
                cst.SilverMedal = model.SilverMedal;
                cst.StartTime = startTime;
                cst.UnfreezeTime = unfreezeTime;
                DbContext.Contests.Update(cst);

                var probs = model.Problems.Select(p => p.Value).ToList();
                probs.Sort((cp1, cp2) => cp1.ShortName.CompareTo(cp2.ShortName));
                probs.ForEach(cp => { cp.Color = "#" + cp.Color; cp.ContestId = ContestId; });
                for (int i = 0; i < probs.Count; i++) probs[i].Rank = i + 1;

                var oldprobs = Problems;
                foreach (var item in oldprobs)
                    if (!probs.Any(cp => cp.ProblemId == item.ProblemId))
                        DbContext.ContestProblem.Remove(item);
                foreach (var item in probs)
                    if (!oldprobs.Any(cp => cp.ProblemId == item.ProblemId))
                        DbContext.ContestProblem.Add(item);
                    else
                        DbContext.ContestProblem.Update(item);

                DbContext.AuditLogs.Add(new AuditLog
                {
                    Comment = "updated",
                    EntityId = ContestId,
                    ContestId = ContestId,
                    Resolved = !contestTimeChanged,
                    Time = DateTimeOffset.Now,
                    UserName = GetUserName(),
                    Type = AuditLog.TargetType.Contest,
                });

                DbContext.SaveChanges();
                return ($"Contest {ContestId} updated. " +
                    (contestTimeChanged ? "Rank cache will be refreshed in minutes. " : ""), true);
            }

            var result = SolveAndUpdate();
            QueryContest(true).FirstOrDefault();
            QueryProblems(true).ToArray();
            return result;
        }

        public (IEnumerable<AuditLog> model, int page) GetAuditLogs(int pg)
        {
            int cid = ContestId;

            var query = DbContext.AuditLogs
                .Where(a => a.ContestId == cid)
                .OrderByDescending(a => a.LogId)
                .Skip((pg - 1) * 1000)
                .Take(1000)
                .Cacheable(TimeSpan.FromSeconds(5))
                .ToList();

            var page = DbContext.AuditLogs
                .Count(a => a.ContestId == cid);

            return (query, (page + 999) / 1000);
        }

        public void CreateTeams(IEnumerable<Team> teams)
        {
            int cid = ContestId;

            lock (_add_team_lock)
            {
                var teamId = 1 + DbContext.Teams
                    .Count(tt => tt.ContestId == cid);

                foreach (var team in teams)
                {
                    team.TeamId = teamId++;
                    DbContext.Teams.Add(team);

                    DbContext.AuditLogs.Add(new AuditLog
                    {
                        Type = AuditLog.TargetType.Contest,
                        Time = DateTimeOffset.Now,
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
                }

                DbContext.SaveChanges();
            }
        }
    }
}
