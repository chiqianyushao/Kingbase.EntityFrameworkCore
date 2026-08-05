using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseDateTimeMethodTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is not null)
        {
            if (method.DeclaringType == typeof(DateOnly))
            {
                return method.Name switch
                {
                    nameof(DateOnly.ToDateTime) when arguments.Count >= 1 => sqlExpressionFactory.Convert(
                        sqlExpressionFactory.Add(instance, arguments[0]),
                        typeof(DateTime)),
                    nameof(DateOnly.AddYears) when arguments.Count == 1 => AddInterval(instance, arguments[0], "year"),
                    nameof(DateOnly.AddMonths) when arguments.Count == 1 => AddInterval(instance, arguments[0], "month"),
                    nameof(DateOnly.AddDays) when arguments.Count == 1 => AddInterval(instance, arguments[0], "day"),
                    _ => null
                };
            }

            if (method.DeclaringType == typeof(TimeOnly))
            {
                return method.Name switch
                {
                    nameof(TimeOnly.AddHours) when arguments.Count == 1 => AddInterval(instance, arguments[0], "hour"),
                    nameof(TimeOnly.AddMinutes) when arguments.Count == 1 => AddInterval(instance, arguments[0], "minute"),
                    nameof(TimeOnly.Add) when arguments.Count >= 1 => sqlExpressionFactory.Add(instance, arguments[0]),
                    _ => null
                };
            }

            if (method.DeclaringType == typeof(DateTime)
                || method.DeclaringType == typeof(DateTimeOffset))
            {
                return method.Name switch
                {
                    nameof(DateTime.AddYears) when arguments.Count == 1 => AddInterval(instance, arguments[0], "year"),
                    nameof(DateTime.AddMonths) when arguments.Count == 1 => AddInterval(instance, arguments[0], "month"),
                    nameof(DateTime.AddDays) when arguments.Count == 1 => AddInterval(instance, arguments[0], "day"),
                    nameof(DateTime.AddHours) when arguments.Count == 1 => AddInterval(instance, arguments[0], "hour"),
                    nameof(DateTime.AddMinutes) when arguments.Count == 1 => AddInterval(instance, arguments[0], "minute"),
                    nameof(DateTime.AddSeconds) when arguments.Count == 1 => AddInterval(instance, arguments[0], "second"),
                    nameof(DateTime.AddMilliseconds) when arguments.Count == 1 => AddInterval(instance, arguments[0], "millisecond"),
                    nameof(DateTime.AddMicroseconds) when arguments.Count == 1 => AddInterval(instance, arguments[0], "microsecond"),
                    nameof(DateTime.AddTicks) when arguments.Count == 1 => AddInterval(
                        instance,
                        sqlExpressionFactory.Divide(arguments[0], sqlExpressionFactory.Constant(10.0)),
                        "microsecond"),
                    nameof(DateTime.Add) when arguments.Count == 1 => sqlExpressionFactory.Add(instance, arguments[0]),
                    nameof(DateTime.Subtract) when arguments.Count == 1 => sqlExpressionFactory.Subtract(instance, arguments[0]),
                    _ => null
                };
            }
        }

        if (method.IsStatic && arguments.Count == 1)
        {
            if (method.DeclaringType == typeof(DateOnly) && method.Name == nameof(DateOnly.FromDateTime))
            {
                return sqlExpressionFactory.Convert(arguments[0], typeof(DateOnly));
            }

            if (method.DeclaringType == typeof(TimeOnly) && method.Name == nameof(TimeOnly.FromDateTime))
            {
                return sqlExpressionFactory.Convert(arguments[0], typeof(TimeOnly));
            }
        }

        return null;
    }

    private SqlExpression AddInterval(SqlExpression instance, SqlExpression value, string unit)
        => sqlExpressionFactory.Add(instance, MakeInterval(value, unit));

    private SqlExpression MakeInterval(SqlExpression value, string unit)
    {
        var zero = sqlExpressionFactory.Constant(0);
        var seconds = unit switch
        {
            "millisecond" => sqlExpressionFactory.Divide(value, sqlExpressionFactory.Constant(1000.0)),
            "microsecond" => sqlExpressionFactory.Divide(value, sqlExpressionFactory.Constant(1000000.0)),
            _ => unit == "second" ? value : sqlExpressionFactory.Constant(0.0)
        };

        var arguments = new SqlExpression[]
        {
            unit == "year" ? value : zero,
            unit == "month" ? value : zero,
            zero,
            unit == "day" ? value : zero,
            unit == "hour" ? value : zero,
            unit == "minute" ? value : zero,
            seconds
        };

        return sqlExpressionFactory.Function(
            "make_interval",
            arguments,
            nullable: true,
            argumentsPropagateNullability: [true, true, true, true, true, true, true],
            typeof(TimeSpan));
    }
}
