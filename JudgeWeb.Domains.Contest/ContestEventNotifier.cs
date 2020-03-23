using JudgeWeb.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        public Task Update(int cid, Judging judging)
        {
            return Task.CompletedTask;
            //var its = new Data.Api.ContestJudgement(
            //    j, c.StartTime ?? DateTimeOffset.Now);
            //DbContext.Events.Add(its.ToEvent("update", c.ContestId));
        }
    }
}
