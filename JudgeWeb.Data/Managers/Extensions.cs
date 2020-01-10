using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace JudgeWeb.Data
{
    public static class ManagersServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultManagers(this IServiceCollection services)
        {
            services.AddScoped<SubmissionManager>();
            return services;
        }

        public static IQueryable<T> SelectSubmissionJudging<T>(this AppDbContext src,
            Expression<Func<Submission, bool>> condition,
            Expression<Func<Submission, Judging, T>> selector)
        {
            return src.Submissions
                .Where(condition)
                .Join(
                    inner: src.Judgings,
                    outerKeySelector: s => new { s.SubmissionId, Active = true },
                    innerKeySelector: j => new { j.SubmissionId, j.Active },
                    resultSelector: selector);
        }
    }
}
