using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace JudgeWeb.Domains.Problems
{
    internal static class ExpressionExtensions
    {
        public static IQueryable<T> NatureJoin<T>(
            this IQueryable<Submission> submissions,
            IQueryable<Judging> judgings,
            Expression<Func<Submission, Judging, T>> selector)
        {
            return submissions.Join(
                inner: judgings,
                outerKeySelector: s => new { s.SubmissionId, Active = true },
                innerKeySelector: j => new { j.SubmissionId, j.Active },
                resultSelector: selector);
        }
    }
}
