using System;
using System.Linq.Expressions;

namespace HikConsole.Helpers
{
    public static class Guard
    {
        public static void NotNull<T>(Expression<Func<T>> reference, T value)
        {
            if ((object)value == null)
            {
                throw new ArgumentNullException(GetParameterName((Expression)reference), "Parameter cannot be null.");
            }
        }

        public static void NotEmpty<T>(Expression<Func<T[]>> reference, T[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(GetParameterName((Expression)reference), "Parameter cannot be null.");
            }

            if (value.Length == 0)
            {
                throw new ArgumentOutOfRangeException(GetParameterName((Expression)reference), "Array is empty.");
            }
        }

        public static void NotNullOrEmpty(Expression<Func<string>> reference, string value)
        {
            NotNull<string>(reference, value);
            if (value.Length == 0)
            {
                throw new ArgumentException("Parameter cannot be empty.", GetParameterName((Expression)reference));
            }
        }

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

        public static void IsValid<T>(
            Expression<Func<T>> reference,
            T value,
            Func<T, bool> validate,
            string format,
            params object[] args)
        {
            if (!validate(value))
            {
                throw new ArgumentException(string.Format(format, args), GetParameterName((Expression)reference));
            }
        }

        private static string GetParameterName(Expression reference)
        {
            var expression = reference is LambdaExpression lambdaExpression ? lambdaExpression.Body : (Expression)null;
            return (expression is MemberExpression memberExpression ? memberExpression.Member.Name : (string)null) ?? "NULL";
        }
    }
}
