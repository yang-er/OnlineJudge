using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Areas.Contest.Services
{
    public partial class ContestManager
    {
        public Task<Dictionary<int, string>> GetTeamNameAsync(int cid) =>
            DbContext.Teams
                .Where(t => t.ContestId == cid && t.Status == 1)
                .Select(t => new { t.TeamId, t.TeamName })
                .CachedToDictionaryAsync(
                    keySelector: t => t.TeamId,
                    valueSelector: t => t.TeamName,
                    tag: $"`c{cid}`teams`names_dict",
                    timeSpan: TimeSpan.FromMinutes(5));

        public Task<int> CountPendingTeamAsync(int cid) =>
            DbContext.Teams
                .Where(t => t.Status == 0 && t.ContestId == cid)
                .CachedCountAsync($"`c{cid}`teams`pending_count", TimeSpan.FromSeconds(10));
        
        public Task<Team> FindTeamByIdAsync(int cid, int teamid) =>
            DbContext.Teams
                .Where(t => t.ContestId == cid && t.TeamId == teamid)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`t{teamid}", TimeSpan.FromMinutes(5));
        
        public Task<Team> FindTeamByUserAsync(int cid, int uid) =>
            DbContext.Teams
                .Where(t => t.ContestId == cid && t.UserId == uid)
                .CachedSingleOrDefaultAsync($"`c{cid}`teams`u{uid}", TimeSpan.FromMinutes(5));
        
        public Task<List<TeamCategory>> ListTeamCategoryAsync(int cid, bool? requirePublic = null)
        {
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

                return query.CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`{(requirePublic.Value ? 2 : 1)}",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
            else
            {
                return DbContext.TeamCategories.CachedToListAsync(
                    tag: $"`c{cid}`teams`cat`0",
                    timeSpan: TimeSpan.FromMinutes(5));
            }
        }

        public Task<List<TeamAffiliation>> ListTeamAffiliationAsync(int cid, bool filtered = true) =>
            filtered
            ? DbContext.Teams
                .Where(t => t.ContestId == cid)
                .Select(t => new { t.AffiliationId })
                .Distinct()
                .Join(
                    inner: DbContext.TeamAffiliations,
                    outerKeySelector: a => a.AffiliationId,
                    innerKeySelector: a => a.AffiliationId,
                    resultSelector: (i, a) => a)
                .CachedToListAsync($"`c{cid}`teams`aff0", TimeSpan.FromMinutes(5))
            : DbContext.TeamAffiliations
                .CachedToListAsync($"`c{cid}`teams`aff1", TimeSpan.FromMinutes(5));

        public async Task<int> CreateTeamAsync(Team team)
        {
            using (await _locker.LockAsync())
            {
                int cid = team.ContestId;
                team.TeamId = 1 + await DbContext.Teams
                    .CountAsync(tt => tt.ContestId == cid);
                DbContext.Teams.Add(team);

                InternalLog(new AuditLog
                {
                    Type = AuditLog.TargetType.Contest,
                    Resolved = true,
                    ContestId = team.ContestId,
                    Comment = $"add team t{team.TeamId} " +
                        $"a{team.AffiliationId}, " +
                        $"c{team.CategoryId}, " +
                        $"u{team.UserId}, " +
                        $"{team.TeamName}",
                    EntityId = team.TeamId,
                });

                await DbContext.SaveChangesAsync();
                Cache.Remove($"`c{team.ContestId}`teams`list_jury");
                return team.TeamId;
            }
        }

        public async Task UpdateTeamAsync(Team team, string comment, int? oldUid = null)
        {
            DbContext.Teams.Update(team);

            InternalLog(new AuditLog
            {
                Type = AuditLog.TargetType.Contest,
                Resolved = true,
                ContestId = team.ContestId,
                Comment = comment,
                EntityId = team.TeamId,
            });

            await DbContext.SaveChangesAsync();
            Cache.Remove($"`c{team.ContestId}`teams`t{team.TeamId}");
            Cache.Remove($"`c{team.ContestId}`teams`u{oldUid ?? team.UserId}");
            Cache.Remove($"`c{team.ContestId}`teams`list_jury");
        }
    }
}
