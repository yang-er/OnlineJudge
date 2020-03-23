using JudgeWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace JudgeWeb.Domains.Problems
{
    internal class ReplaceExpressionVisitor : ExpressionVisitor
    {
        public Expression this[Expression key] { get => Changes[key]; set => Changes[key] = value; }

        public Dictionary<Expression, Expression> Changes { get; }
            = new Dictionary<Expression, Expression>();

        public ReplaceExpressionVisitor Attach(Expression from, Expression to)
        {
            Changes.Add(from, to);
            return this;
        }

        public override Expression Visit(Expression node)
        {
            return Changes.GetValueOrDefault(node) ?? base.Visit(node);
        }
    }

    internal static class ExpressionExtensions
    {
        public static Expression<Func<T4, T3>> Combine<T1, T2, T3, T4>(
            this Expression<Func<T1, T2, T3>> expression,
            T4 objectTemplate,
            Expression<Func<T4, T1>> place1,
            Expression<Func<T4, T2>> place2)
        {
            var parameter = place1.Parameters.Single();
            var hold1 = place1.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(place2.Parameters[0], parameter)
                .Visit(place2.Body);
            var newBody = new ReplaceExpressionVisitor()
                .Attach(expression.Parameters[0], hold1)
                .Attach(expression.Parameters[1], hold2)
                .Visit(expression.Body);
            return Expression.Lambda<Func<T4, T3>>(newBody, parameter);
        }

        public static Expression<Func<T4, T5, T3>> Combine<T1, T2, T3, T4, T5>(
            this Expression<Func<T1, T2, T3>> expression,
            T4 objectTemplate1,
            T5 objectTemplate2,
            Expression<Func<T4, T5, T1>> place1,
            Expression<Func<T4, T5, T2>> place2)
        {
            var para1 = place1.Parameters[0];
            var para2 = place1.Parameters[1];
            var hold1 = place1.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(place2.Parameters[0], para1)
                .Attach(place2.Parameters[1], para2)
                .Visit(place2.Body);
            var newBody = new ReplaceExpressionVisitor()
                .Attach(expression.Parameters[0], hold1)
                .Attach(expression.Parameters[1], hold2)
                .Visit(expression.Body);
            return Expression.Lambda<Func<T4, T5, T3>>(newBody, para1, para2);
        }

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
