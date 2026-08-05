using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseGuidAndRegexTranslator(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource) : IMethodCallTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType == typeof(DbFunctionsExtensions)
            && method.Name == nameof(DbFunctionsExtensions.Random))
        {
            return sqlExpressionFactory.Function(
                "random",
                [],
                nullable: false,
                argumentsPropagateNullability: [],
                typeof(double));
        }

        if (method.DeclaringType == typeof(KingbaseByteArrayMethods)
            && method.Name == nameof(KingbaseByteArrayMethods.SequenceEqual)
            && arguments.Count == 2)
        {
            var mapping = arguments[0].TypeMapping
                ?? arguments[1].TypeMapping
                ?? typeMappingSource.FindMapping(typeof(byte[]));
            return sqlExpressionFactory.Equal(
                Remap(arguments[0], mapping),
                Remap(arguments[1], mapping));
        }

        if (method.DeclaringType == typeof(Guid)
            && method.Name == nameof(Guid.NewGuid)
            && arguments.Count == 0)
        {
            var value = sqlExpressionFactory.Function(
                "sys_guid",
                [],
                nullable: false,
                argumentsPropagateNullability: [],
                typeof(string));

            return sqlExpressionFactory.Convert(value, typeof(Guid));
        }

        if (method.DeclaringType == typeof(Regex)
            && method.Name == nameof(Regex.IsMatch)
            && method.IsStatic
            && arguments.Count is 2 or 3)
        {
            var functionArguments = arguments.Count == 2
                ? arguments
                : [arguments[0], arguments[1], RegexFlags(arguments[2])];

            return sqlExpressionFactory.Function(
                "regexp_like",
                functionArguments,
                nullable: true,
                argumentsPropagateNullability: Enumerable.Repeat(true, functionArguments.Count).ToArray(),
                typeof(bool));
        }

        return null;
    }

    private SqlExpression Remap(SqlExpression expression, Microsoft.EntityFrameworkCore.Storage.RelationalTypeMapping? mapping)
        => mapping is null
            ? expression
            : expression switch
            {
                SqlParameterExpression parameter => parameter.ApplyTypeMapping(mapping),
                SqlConstantExpression { Value: not null } constant => new SqlConstantExpression(constant.Value, mapping),
                _ => sqlExpressionFactory.ApplyTypeMapping(expression, mapping)
            };

    private SqlExpression RegexFlags(SqlExpression options)
    {
        if (options is not SqlConstantExpression { Value: RegexOptions value })
        {
            return sqlExpressionFactory.Constant(string.Empty);
        }

        var flags = string.Concat(
            value.HasFlag(RegexOptions.IgnoreCase) ? "i" : string.Empty,
            value.HasFlag(RegexOptions.Multiline) ? "m" : string.Empty,
            value.HasFlag(RegexOptions.Singleline) ? "n" : string.Empty,
            value.HasFlag(RegexOptions.IgnorePatternWhitespace) ? "x" : string.Empty);

        return sqlExpressionFactory.Constant(flags);
    }
}
