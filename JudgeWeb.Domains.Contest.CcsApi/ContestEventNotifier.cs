using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Contests
{
    public class ContestEventNotifier : IContestEventNotifier
    {
        DbContext Context { get; }

        DbSet<Event> Events => Context.Set<Event>();

        public Task Create(int cid, Detail detail)
        {
            return Task.CompletedTask;
            throw new NotImplementedException();
        }

        public IQueryable<Event> Query(int cid)
        {
            return Events.Where(e => e.ContestId == cid);
        }

        public Task Update(int cid, Judging judging)
        {
            return Task.CompletedTask;
            //var its = new Data.Api.ContestJudgement(
            //    j, c.StartTime ?? DateTimeOffset.Now);
            //DbContext.Events.Add(its.ToEvent("update", c.ContestId));
        }

        public Task Update(int cid, Contest contest)
        {
            throw new NotImplementedException();
        }
    }
}
