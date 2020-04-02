using System.Collections.Generic;

namespace System.Linq.Expressions
{
    public class ReplaceExpressionVisitor : ExpressionVisitor
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
            return node == null ? null : Changes.GetValueOrDefault(node) ?? base.Visit(node);
        }
    }

    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> Combine<T>(
            this Expression<Func<T, bool>>? expression,
            Expression<Func<T, bool>> other)
        {
            if (expression == null) return other;
            var parameter = expression.Parameters.Single();
            var hold1 = expression.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(other.Parameters[0], parameter)
                .Visit(other.Body);
            var newBody = Expression.AndAlso(hold1, hold2);
            return Expression.Lambda<Func<T, bool>>(newBody, parameter);
        }

        public static Expression<Func<T1, T2, bool>> Combine<T1, T2>(
            this Expression<Func<T1, T2, bool>>? expression,
            Expression<Func<T1, T2, bool>> other)
        {
            if (expression == null) return other;
            var para0 = expression.Parameters[0];
            var para1 = expression.Parameters[1];
            var hold1 = expression.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(other.Parameters[0], para0)
                .Attach(other.Parameters[1], para1)
                .Visit(other.Body);
            var newBody = Expression.AndAlso(hold1, hold2);
            return Expression.Lambda<Func<T1, T2, bool>>(newBody, para0, para1);
        }

        public static Expression<Func<T4, T3>> Combine<T1, T2, T3, T4>(
            this Expression<Func<T1, T2, T3>> expression,
            T4 objectTemplate,
            Expression<Func<T4, T1>> place1,
            Expression<Func<T4, T2>> place2)
        {
            if (expression == null) return null;
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

        public static Expression<Func<T5, T4>> Combine<T1, T2, T3, T4, T5>(
            this Expression<Func<T1, T2, T3, T4>> expression,
            T5 objectTemplate,
            Expression<Func<T5, T1>> place1,
            Expression<Func<T5, T2>> place2,
            Expression<Func<T5, T3>> place3)
        {
            if (expression == null) return null;
            var parameter = place1.Parameters.Single();
            var hold1 = place1.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(place2.Parameters[0], parameter)
                .Visit(place2.Body);
            var hold3 = new ReplaceExpressionVisitor()
                .Attach(place3.Parameters[0], parameter)
                .Visit(place3.Body);
            var newBody = new ReplaceExpressionVisitor()
                .Attach(expression.Parameters[0], hold1)
                .Attach(expression.Parameters[1], hold2)
                .Attach(expression.Parameters[2], hold3)
                .Visit(expression.Body);
            return Expression.Lambda<Func<T5, T4>>(newBody, parameter);
        }

        public static Expression<Func<T6, T5>> Combine<T1, T2, T3, T4, T5, T6>(
            this Expression<Func<T1, T2, T3, T4, T5>> expression,
            T6 objectTemplate,
            Expression<Func<T6, T1>> place1,
            Expression<Func<T6, T2>> place2,
            Expression<Func<T6, T3>> place3,
            Expression<Func<T6, T4>> place4)
        {
            if (expression == null) return null;
            var parameter = place1.Parameters.Single();
            var hold1 = place1.Body;
            var hold2 = new ReplaceExpressionVisitor()
                .Attach(place2.Parameters[0], parameter)
                .Visit(place2.Body);
            var hold3 = new ReplaceExpressionVisitor()
                .Attach(place3.Parameters[0], parameter)
                .Visit(place3.Body);
            var hold4 = new ReplaceExpressionVisitor()
                .Attach(place4.Parameters[0], parameter)
                .Visit(place4.Body);
            var newBody = new ReplaceExpressionVisitor()
                .Attach(expression.Parameters[0], hold1)
                .Attach(expression.Parameters[1], hold2)
                .Attach(expression.Parameters[2], hold3)
                .Attach(expression.Parameters[3], hold4)
                .Visit(expression.Body);
            return Expression.Lambda<Func<T6, T5>>(newBody, parameter);
        }

        public static Expression<Func<T4, T5, T3>> Combine<T1, T2, T3, T4, T5>(
            this Expression<Func<T1, T2, T3>> expression,
            T4 objectTemplate1,
            T5 objectTemplate2,
            Expression<Func<T4, T5, T1>> place1,
            Expression<Func<T4, T5, T2>> place2)
        {
            if (expression == null) return null;
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
    }
}
