using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseDateTimeMemberTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMemberTranslator
{
    private static readonly IReadOnlyDictionary<string, string> DateParts = new Dictionary<string, string>
    {
        [nameof(DateTime.Year)] = "year",
        [nameof(DateTime.Month)] = "month",
        [nameof(DateTime.DayOfYear)] = "doy",
        [nameof(DateTime.Day)] = "day",
        [nameof(DateTime.Hour)] = "hour",
        [nameof(DateTime.Minute)] = "minute",
        [nameof(DateTime.Second)] = "second",
        [nameof(DateTime.Microsecond)] = "microseconds",
        [nameof(DateTime.Nanosecond)] = "microseconds",
        [nameof(DateTime.DayOfWeek)] = "dow",
        [nameof(DateOnly.Year)] = "year",
        [nameof(DateOnly.Month)] = "month",
        [nameof(DateOnly.DayOfYear)] = "doy",
        [nameof(DateOnly.Day)] = "day",
        [nameof(DateOnly.DayOfWeek)] = "dow",
        [nameof(TimeOnly.Hour)] = "hour",
        [nameof(TimeOnly.Minute)] = "minute",
        [nameof(TimeOnly.Second)] = "second",
        [nameof(TimeOnly.Millisecond)] = "milliseconds"
        ,[nameof(TimeOnly.Microsecond)] = "microseconds"
        ,[nameof(TimeOnly.Nanosecond)] = "microseconds"
    };

    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null)
        {
            return TranslateStatic(member, returnType);
        }

        if (member.DeclaringType == typeof(DateOnly) && member.Name == nameof(DateOnly.DayNumber))
        {
            return sqlExpressionFactory.Convert(
                sqlExpressionFactory.Subtract(instance, sqlExpressionFactory.Constant(new DateOnly(1, 1, 1))),
                returnType);
        }

        if ((member.DeclaringType != typeof(DateTime)
                && member.DeclaringType != typeof(DateTimeOffset)
                && member.DeclaringType != typeof(DateOnly)
                && member.DeclaringType != typeof(TimeOnly))
            || !DateParts.TryGetValue(member.Name, out var datePart))
        {
            return null;
        }

        var extracted = sqlExpressionFactory.Function(
            "date_part",
            [sqlExpressionFactory.Constant(datePart), instance],
            nullable: true,
            argumentsPropagateNullability: [false, true],
            typeof(double));

        if (member.Name == nameof(TimeOnly.Millisecond))
        {
            extracted = sqlExpressionFactory.Modulo(
                sqlExpressionFactory.Convert(extracted, typeof(long)),
                sqlExpressionFactory.Constant(1000L));
        }

        else if (member.Name is nameof(DateTime.Microsecond) or nameof(TimeOnly.Microsecond))
        {
            extracted = sqlExpressionFactory.Modulo(
                sqlExpressionFactory.Convert(extracted, typeof(long)),
                sqlExpressionFactory.Constant(1000L));
        }

        else if (member.Name is nameof(DateTime.Nanosecond) or nameof(TimeOnly.Nanosecond))
        {
            extracted = sqlExpressionFactory.Constant(0.0);
        }

        return sqlExpressionFactory.Convert(extracted, returnType);
    }

    private SqlExpression? TranslateStatic(MemberInfo member, Type returnType)
    {
        if (member.DeclaringType == typeof(DateTime))
        {
            return member.Name switch
            {
                nameof(DateTime.Now) => sqlExpressionFactory.Fragment("LOCALTIMESTAMP", returnType),
                nameof(DateTime.UtcNow) => sqlExpressionFactory.Fragment("CURRENT_TIMESTAMP", returnType),
                nameof(DateTime.Today) => sqlExpressionFactory.Fragment("CURRENT_DATE", returnType),
                _ => null
            };
        }

        if (member.DeclaringType == typeof(DateTimeOffset))
        {
            return member.Name switch
            {
                nameof(DateTimeOffset.Now) or nameof(DateTimeOffset.UtcNow)
                    => sqlExpressionFactory.Fragment("CURRENT_TIMESTAMP", returnType),
                _ => null
            };
        }

        return null;
    }
}
