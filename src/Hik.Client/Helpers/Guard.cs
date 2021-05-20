using System;
using System.Linq.Expressions;

namespace Hik.Client.Helpers
{
    public static class Guard
    {
        public static void IsValid<T>(
            Expression<Func<T>> reference,
            T value,
            Func<T, bool> validate,
            string message)
        {
            if (!validate(value))
            {
                throw new ArgumentException(message, GetParameterName((Expression)reference));
            }
        }

        private static string GetParameterName(Expression reference)
        {
            var expression = reference is LambdaExpression lambdaExpression ? lambdaExpression.Body : (Expression)null;
            return (expression is MemberExpression memberExpression ? memberExpression.Member.Name : (string)null) ?? "NULL";
        }
    }
}
