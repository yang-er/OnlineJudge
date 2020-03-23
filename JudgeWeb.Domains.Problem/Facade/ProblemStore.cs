using JudgeWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace JudgeWeb.Domains.Problems
{
    public partial class ProblemFacade :
        IProblemStore,
        ICrudRepositoryImpl<Problem>,
        ICrudInstantUpdateImpl<Problem>
    {
        public IProblemStore ProblemStore => this;

        public DbSet<Problem> Problems => Context.Set<Problem>();

        Task<Problem> IProblemStore.FindAsync(int pid)
        {
            return Problems.SingleOrDefaultAsync(p => p.ProblemId == pid);
        }

        Task IProblemStore.ToggleAsync(
            int pid,
            Expression<Func<Problem, bool>> expression,
            bool tobe)
        {
            Expression<Func<Problem, Problem>> body = expression.Body switch
            {
                MemberExpression me
                when me.Member.Name == nameof(Problem.AllowJudge)
                    => p => new Problem { AllowJudge = tobe },
                MemberExpression me
                when me.Member.Name == nameof(Problem.AllowSubmit)
                    => p => new Problem { AllowSubmit = tobe },
                _ => throw new InvalidOperationException()
            };

            return Problems
                .Where(p => p.ProblemId == pid)
                .BatchUpdateAsync(body);
        }

        async Task<(IEnumerable<Problem> model, int totPage)> IProblemStore.ListAsync(int? uid, int page, int perCount)
        {
            IQueryable<Problem> problemSource = Problems;

            if (uid.HasValue)
            {
                var avaliable =
                    from ur in Context.Set<IdentityUserRole<int>>()
                    where ur.UserId == uid
                    join r in Context.Set<Role>() on ur.RoleId equals r.Id
                    where r.ProblemId != null
                    select r.ProblemId;
                problemSource = problemSource
                    .Where(p => avaliable.Contains(p.ProblemId));
            }

            int totalCount = await problemSource.CountAsync();
            int totPage = (totalCount - 1) / perCount + 1;

            var probs = await problemSource
                .Include(p => p.ArchiveCollection)
                .OrderBy(p => p.ProblemId)
                .Skip(perCount * (page - 1)).Take(perCount)
                .ToListAsync();

            return (probs, totPage);
        }
    }
}
