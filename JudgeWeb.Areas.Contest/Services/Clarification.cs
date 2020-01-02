using EFCore.BulkExtensions;
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
        static readonly TimeSpan ClarisCacheSpan = TimeSpan.FromSeconds(15);

        public Task<int> CountUnansweredClarificationAsync(int cid) =>
            DbContext.Clarifications
                .Where(c => c.ContestId == cid && !c.Answered)
                .CachedCountAsync($"`c{cid}`clar`una_count", ClarisCacheSpan);

        protected IQueryable<Clarification> ClarQueryOf(int cid, int clarid) =>
            DbContext.Clarifications
                .Where(c => c.ContestId == cid && c.ClarificationId == clarid);

        public Task<Clarification> ClarFindByIdAsync(int cid, int clarid) =>
            ClarQueryOf(cid, clarid)
                .SingleOrDefaultAsync();

        public async Task<IEnumerable<Clarification>> ClarFindByIdAsync(
            int cid, int teamid, int clarid, bool needMore)
        {
            var toSee = await ClarFindByIdAsync(cid, clarid);

            var ret = Enumerable.Empty<Clarification>();
            if (!(toSee?.CheckPermission(teamid) ?? true))
                return ret;
            ret = ret.Append(toSee);

            if (needMore && toSee.ResponseToId.HasValue)
            {
                var toSee2 = await ClarFindByIdAsync(cid, toSee.ResponseToId.Value);
                if (toSee2 != null) ret = ret.Prepend(toSee2);
            }

            return ret;
        }

        public async Task<int> SendClarificationAsync(Clarification clar, Clarification replyTo = null)
        {
            var cl = DbContext.Clarifications.Add(clar);

            if (replyTo != null)
            {
                replyTo.Answered = true;
                DbContext.Clarifications.Update(replyTo);
            }

            await DbContext.SaveChangesAsync();
            return cl.Entity.ClarificationId;
        }
    }
}
