using JudgeWeb.Data;
using JudgeWeb.Domains.Contests;
using JudgeWeb.Domains.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

[assembly: Inject(typeof(ITeamStore), typeof(TeamStore))]
namespace JudgeWeb.Domains.Contests
{
    public class TeamStore :
        ITeamStore,
        ICrudRepositoryImpl<TeamMember>
    {
        private static AsyncLock TeamLock { get; } = new AsyncLock();

        public DbContext Context { get; }

        DbSet<Team> Teams => Context.Set<Team>();

        DbSet<TeamMember> Members => Context.Set<TeamMember>();

        public TeamStore(DbContextAccessor context)
        {
            Context = context;
        }

        public async Task<HashSet<int>> ListRegisteredAsync(int uid)
        {
            var members = await Members
                .Where(t => t.UserId == uid)
                .Select(t => t.ContestId)
                .ToArrayAsync();
            return members.ToHashSet();
        }

        public Task<List<TeamMember>> ListRegisteredWithDetailAsync(int uid)
        {
            return Members.Where(m => m.UserId == uid)
                .Include(m => m.Team)
                    .ThenInclude(m => m.Contest)
                .Include(m => m.Team)
                    .ThenInclude(m => m.Affiliation)
                .Include(m => m.Team)
                    .ThenInclude(m => m.Category)
                .ToListAsync();
        }

        public Task<T> FindAsync<T>(int cid, int tid,
            Expression<Func<Team, T>> selector)
        {
            return Teams
                .Where(t => t.ContestId == cid && t.TeamId == tid)
                .Select(selector)
                .SingleOrDefaultAsync();
        }

        public Task<List<T>> ListAsync<T>(int cid,
            Expression<Func<Team, T>> selector,
            Expression<Func<Team, bool>>? predicate,
            (string, TimeSpan)? cacheTag)
        {
            var query = Teams.Where(t => t.ContestId == cid && t.Status != 3);
            if (predicate != null) query = query.Where(predicate);
            var query2 = query.Select(selector);
            if (!cacheTag.HasValue) return query2.ToListAsync();
            return query2.CachedToListAsync(cacheTag.Value.Item1, cacheTag.Value.Item2);
        }

        public Task<Dictionary<int, string>> ListNamesAsync(int cid)
        {
            return Teams
                .Where(t => t.ContestId == cid && t.Status == 1)
                .Select(t => new { t.TeamId, t.TeamName })
                .CachedToDictionaryAsync(
                    keySelector: t => t.TeamId,
                    valueSelector: t => t.TeamName,
                    tag: $"`c{cid}`teams`names_dict",
                    timeSpan: TimeSpan.FromMinutes(10));
        }

        public Task<ILookup<int, string>> ListMembersAsync(int cid)
        {
            return Context.CachedGetAsync($"`c{cid}`teams`members", TimeSpan.FromMinutes(10),
            async () =>
            {
                var query =
                    from tu in Members
                    where tu.ContestId == cid
                    join u in Context.Set<User>() on tu.UserId equals u.Id
                    select new { tu.TeamId, u.UserName };

                return (await query.ToListAsync())
                    .ToLookup(a => a.TeamId, a => a.UserName);
            });
        }

        public Task<Dictionary<int, (int ac, int tot)>> StatisticsSubmissionAsync(int cid, int teamid)
        {
            return Context.Set<SubmissionStatistics>()
                .Where(s => s.ContestId == cid && s.Author == teamid)
                .CachedToDictionaryAsync(
                    keySelector: s => s.ProblemId,
                    valueSelector: s => (s.AcceptedSubmission, s.TotalSubmission),
                    $"`c{cid}`teams`{teamid}`substat", TimeSpan.FromMinutes(1));
        }

        public Task<Team> FindByIdAsync(int cid, int teamid)
        {
            return Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`t{teamid}", TimeSpan.FromMinutes(5));
        }

        public Task<Team> FindByUserAsync(int cid, int uid)
        {
            return Members
                .Where(tu => tu.ContestId == cid && tu.UserId == uid)
                .Select(tu => tu.Team)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`u{uid}", TimeSpan.FromMinutes(5));
        }

        public async Task<int> CreateAsync(Team team, int[]? uids)
        {
            using var _lock = await TeamLock.LockAsync();
            int cid = team.ContestId;

            team.TeamId = 1 + await Teams.CountAsync(tt => tt.ContestId == cid);
            Teams.Add(team);

            if (uids != null)
            {
                foreach (var uid in uids)
                {
                    Members.Add(new TeamMember
                    {
                        ContestId = team.ContestId,
                        TeamId = team.TeamId,
                        UserId = uid,
                        Temporary = false
                    });
                }
            }

            await Context.SaveChangesAsync();
            Context.RemoveCacheEntry($"`c{cid}`teams`list_jury");
            Context.RemoveCacheEntry($"`c{cid}`teams`t{team.TeamId}");
            Context.RemoveCacheEntry($"`c{cid}`teams`members");

            if (uids != null)
                foreach (var uid in uids)
                    Context.RemoveCacheEntry($"`c{cid}`teams`u{uid}");
            return team.TeamId;
        }

        public Task<ScoreboardDataModel> LoadScoreboardAsync(int cid)
        {
            return Context.CachedGetAsync($"`c{cid}`scoreboard", TimeSpan.FromSeconds(3),
            async () =>
            {
                var value = await Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Include(t => t.rc)
                    .Include(t => t.ScoreCache)
                    .ToDictionaryAsync(a => a.TeamId);

                var result = new ScoreboardDataModel
                {
                    Data = value,
                    RefreshTime = DateTimeOffset.Now,
                    Statistics = new Dictionary<int, int>()
                };

                foreach (var (_, item) in value)
                {
                    foreach (var ot in item.ScoreCache)
                    {
                        var val = result.Statistics.GetValueOrDefault(ot.ProblemId);
                        if (ot.IsCorrectRestricted)
                            result.Statistics[ot.ProblemId] = ++val;
                    }
                }

                return result;
            });
        }

        public Task<List<TeamAffiliation>> ListAffiliationAsync(int cid, bool filtered = true)
        {
            var query = Context.Set<TeamAffiliation>().AsQueryable();

            if (filtered)
            {
                var avail = Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Select(t => t.AffiliationId);
                query = query.Where(a => avail.Contains(a.AffiliationId));
            }

            return query.CachedToListAsync(
                tag: $"`c{cid}`teams`aff{(filtered ? 0 : 1)}",
                timeSpan: TimeSpan.FromMinutes(5));
        }

        public Task<List<TeamCategory>> ListCategoryAsync(int cid, bool? requirePublic = null)
        {
            if (requirePublic.HasValue)
            {
                var items = Teams
                    .Where(t => t.ContestId == cid && t.Status == 1)
                    .Select(t => t.CategoryId);
                var query = Context.Set<TeamCategory>()
                    .Where(c => items.Contains(c.CategoryId));
                if (requirePublic.Value)
                    query = query.Where(tc => tc.IsPublic);

                return query.CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`{(requirePublic.Value ? 2 : 1)}",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
            else
            {
                return Context.Set<TeamCategory>().CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`0",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
        }

        public async Task UpdateAsync(int cid, int teamid, Expression<Func<Team, Team>> activator)
        {
            var affected = await Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .BatchUpdateAsync(activator);
            if (affected != 1)
                throw new DbUpdateException();

            var list = await Members
                .Where(tu => tu.ContestId == cid && tu.TeamId == teamid)
                .ToListAsync();
            Context.RemoveCacheEntry($"`c{cid}`teams`t{teamid}");
            foreach (var uu in list)
                Context.RemoveCacheEntry($"`c{cid}`teams`u{uu.UserId}");
            Context.RemoveCacheEntry($"`c{cid}`teams`list_jury");
            Context.RemoveCacheEntry($"`c{cid}`teams`aff0");
            Context.RemoveCacheEntry($"`c{cid}`teams`cat`1");
            Context.RemoveCacheEntry($"`c{cid}`teams`cat`2");
        }

        public async Task<IEnumerable<int>> DeleteAsync(Team team)
        {
            var list = await Members
                .Where(tu => tu.ContestId == team.ContestId && tu.TeamId == team.TeamId)
                .ToListAsync();

            team.Status = 3;
            Teams.Update(team);
            await Context.SaveChangesAsync();

            await Members
                .Where(tu => tu.ContestId == team.ContestId && tu.TeamId == team.TeamId)
                .BatchDeleteAsync();

            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`t{team.TeamId}");
            foreach (var uu in list)
                Context.RemoveCacheEntry($"`c{team.ContestId}`teams`u{uu.UserId}");
            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`list_jury");
            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`aff0");
            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`cat`1");
            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`cat`2");
            Context.RemoveCacheEntry($"`c{team.ContestId}`teams`members");
            return list.Select(t => t.UserId);
        }

        public Task<int> GetJuryStatusAsync(int cid)
        {
            return Teams
                .Where(t => t.Status == 0 && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`teams`pending_count", TimeSpan.FromSeconds(10));
        }

        public async Task<HashSet<int>> ListMemberUidsAsync(int cid)
        {
            var ids = await Members
                .Where(m => m.ContestId == cid)
                .Select(m => m.UserId)
                .ToListAsync();
            return ids.ToHashSet();
        }

        private static Func<string> CreatePasswordGenerator()
        {
            const string passwordSource = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var rng = new Random(unchecked((int)DateTimeOffset.Now.Ticks));
            return () =>
            {
                Span<char> pwd = stackalloc char[8];
                for (int i = 0; i < 8; i++) pwd[i] = passwordSource[rng.Next(passwordSource.Length)];
                return new string(pwd);
            };
        }

        private async Task EnsureTeamWithPassword(UserManager userManager, int cid, int teamId, string password)
        {
            string username = UserNameForTeamId(teamId);

            var user = await userManager.FindByNameAsync(username);

            if (user != null)
            {
                if (await userManager.HasPasswordAsync(user))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, password);
                }
                else
                {
                    await userManager.AddPasswordAsync(user, password);
                }

                if (await userManager.IsLockedOutAsync(user))
                {
                    await userManager.SetLockoutEndDateAsync(user, null);
                }
            }
            else
            {
                user = new User { UserName = username, Email = $"{username}@contest.acm.xylab.fun" };
                await userManager.CreateAsync(user, password);
            }

            await Context.Set<TeamMember>().MergeAsync(
                sourceTable: new[] { new { ContestId = cid, TeamId = teamId, UserId = user.Id } },
                targetKey: t => new { t.ContestId, t.TeamId, t.UserId },
                sourceKey: t => new { t.ContestId, t.TeamId, t.UserId },
                updateExpression: null, delete: false,
                insertExpression: t => new TeamMember { ContestId = t.ContestId, TeamId = t.TeamId, UserId = t.UserId, Temporary = true });

            Context.RemoveCacheEntry($"`c{cid}`teams`t{teamId}");
            Context.RemoveCacheEntry($"`c{cid}`teams`u{user.Id}");
        }

        private string UserNameForTeamId(int teamId) => $"team{teamId:D3}";

        public async Task<List<(Team, string)>> BatchCreateAsync(
            UserManager userManager,
            int cid,
            TeamAffiliation aff,
            TeamCategory cat,
            string[] names)
        {
            var rng = CreatePasswordGenerator();
            var result = new List<(Team, string)>();

            var list2 = await Context.Set<Team>()
                .Where(c => c.ContestId == cid && c.AffiliationId == aff.AffiliationId && c.CategoryId == cat.CategoryId)
                .Select(c => new { c.TeamId, c.TeamName })
                .ToListAsync();
            var list = list2.ToLookup(a => a.TeamName, a => a.TeamId);
            
            foreach (var item2 in names)
            {
                var item = item2.Trim();

                if (list.Contains(item))
                {
                    var e = list[item];
                    foreach (var teamId in e)
                    {
                        await EnsureTeamWithPassword(userManager, cid, teamId, rng());
                    }
                }
                else
                {
                    int teamId = await CreateAsync(uids: null, team: new Team
                    {
                        AffiliationId = aff.AffiliationId,
                        CategoryId = cat.CategoryId,
                        ContestId = cid,
                        Status = 1,
                        TeamName = item,
                    });

                    await EnsureTeamWithPassword(userManager, cid, teamId, rng());
                }
            }

            return result;
        }

        public async Task<int> BatchLockOutAsync(int cid)
        {
            var lockOuts = Context.Set<TeamMember>()
                .Where(m => m.ContestId == cid && m.Temporary);

            var lockOuts2 = lockOuts.Select(m => m.UserId);

            await Context.Set<User>()
                .Where(u => lockOuts2.Contains(u.Id))
                .BatchUpdateAsync(u => new User { LockoutEnd = DateTimeOffset.MaxValue });

            return await lockOuts.BatchDeleteAsync();
        }
    }
}
