using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using KdbndpTypes;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseAdvancedTypeMethodTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(KingbaseDbFunctionsExtensions))
        {
            return null;
        }

        var operands = arguments.Skip(1).ToArray();
        return method.Name switch
        {
            nameof(KingbaseDbFunctionsExtensions.JsonExtractPathText) => Function("jsonb_extract_path_text", operands, typeof(string)),
            nameof(KingbaseDbFunctionsExtensions.ArrayContains) when operands[1].Type == typeof(int)
                => sqlExpressionFactory.IsNotNull(Function("array_position", operands, typeof(int))),
            nameof(KingbaseDbFunctionsExtensions.ArrayContains) => Function("arraycontains", operands, typeof(bool)),
            nameof(KingbaseDbFunctionsExtensions.ArrayOverlaps) => Function("arrayoverlap", operands, typeof(bool)),
            nameof(KingbaseDbFunctionsExtensions.ArrayLength) => Function("cardinality", operands, typeof(int)),
            nameof(KingbaseDbFunctionsExtensions.RangeContains) when operands[1].Type == typeof(int)
                => Function("range_contains_elem", operands, typeof(bool)),
            nameof(KingbaseDbFunctionsExtensions.RangeContains) => Function("range_contains", operands, typeof(bool)),
            nameof(KingbaseDbFunctionsExtensions.RangeOverlaps) => Function("range_overlaps", operands, typeof(bool)),
            nameof(KingbaseDbFunctionsExtensions.FullTextMatches) => FullText(operands),
            _ => null
        };
    }

    private SqlExpression FullText(IReadOnlyList<SqlExpression> operands)
    {
        var configuration = operands.Count == 3 ? operands[1] : sqlExpressionFactory.Constant("simple");
        var query = operands.Count == 3 ? operands[2] : operands[1];
        var tsQuery = Function("plainto_tsquery", [configuration, query], typeof(KdbndpTsQuery));
        return Function("ts_match_tt", [operands[0], tsQuery], typeof(bool));
    }

    private SqlExpression Function(string name, IReadOnlyList<SqlExpression> arguments, Type returnType)
        => sqlExpressionFactory.Function(
            name,
            arguments,
            nullable: true,
            argumentsPropagateNullability: Enumerable.Repeat(true, arguments.Count).ToArray(),
            returnType);
}
