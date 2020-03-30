using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public abstract class EmptyEventNotifier : IContestEventNotifier
    {
        public virtual Task Create(int cid, Detail detail) => Task.CompletedTask;

        public virtual Task Create(int cid, ContestProblem problem) => Task.CompletedTask;

        public virtual Task Delete(int cid, ContestProblem problem) => Task.CompletedTask;

        public abstract IQueryable<Event> Query(int cid);

        public virtual Task ResetAsync(int cid) => Task.CompletedTask;

        public virtual Task Update(int cid, Judging judging) => Task.CompletedTask;

        public virtual Task Update(int cid, Contest contest) => Task.CompletedTask;

        public virtual Task Update(int cid, Contest contest, ContestState state) => Task.CompletedTask;

        public virtual Task Update(int cid, ContestProblem problem) => Task.CompletedTask;
    }

    public class ContestEventNotifier : EmptyEventNotifier
    {
        DbContext Context { get; }

        DbSet<Event> Events => Context.Set<Event>();

        public ContestEventNotifier(DbContext context)
        {
            Context = context;
        }

        public override IQueryable<Event> Query(int cid)
        {
            return Events.Where(e => e.ContestId == cid);
        }

        public override Task ResetAsync(int cid)
        {
            return base.ResetAsync(cid);
            /*
            await DbContext.Events
                .Where(e => e.ContestId == cid)
                .BatchDeleteAsync();

            // contests
            DbContext.Events.AddCreate(cid, new Data.Api.ContestInfo(Contest));
            await DbContext.SaveChangesAsync();

            // judgement-types
            DbContext.Events.AddCreate(cid, Data.Api.JudgementType.Defaults);
            await DbContext.SaveChangesAsync();

            // languages
            DbContext.Events.AddCreate(cid,
                Languages.Values.Select(l => new Data.Api.ContestLanguage(l)));
            await DbContext.SaveChangesAsync();

            // groups
            var groups = await DbContext.ListTeamCategoryAsync(cid, null);
            DbContext.Events.AddCreate(cid,
                groups.Select(c => new Data.Api.ContestGroup(c)));
            await DbContext.SaveChangesAsync();

            // organizations
            var cts = await DbContext.ListTeamAffiliationAsync(cid, false);
            DbContext.Events.AddCreate(cid,
                cts.Select(a => new Data.Api.ContestOrganization(a)));
            await DbContext.SaveChangesAsync();

            // problems
            DbContext.Events.AddCreate(cid,
                Problems.Select(a => new Data.Api.ContestProblem2(a)));
            await DbContext.SaveChangesAsync();

            // teams
            DbContext.Events.AddCreate(cid, await(
                from t in DbContext.Teams
                where t.ContestId == cid && t.Status == 1
                join a in DbContext.TeamAffiliations on t.AffiliationId equals a.AffiliationId
                select new Data.Api.ContestTeam(t, a))
                .ToListAsync());
            await DbContext.SaveChangesAsync();

            // clarifications
            DbContext.Events.AddCreate(cid,
                await DbContext.Clarifications
                    .Where(c => c.ContestId == cid)
                    .Select(c => new Data.Api.ContestClarification(c, Contest.StartTime.Value))
                    .ToListAsync());
            await DbContext.SaveChangesAsync();
            */
        }
    }
}
