using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryTranslationPreprocessor(
    QueryTranslationPreprocessorDependencies dependencies,
    RelationalQueryTranslationPreprocessorDependencies relationalDependencies,
    QueryCompilationContext queryCompilationContext)
    : RelationalQueryTranslationPreprocessor(dependencies, relationalDependencies, queryCompilationContext)
{
    public override Expression Process(Expression query)
        => base.Process(new ByteArraySequenceEqualRewriter().Visit(query));

    private sealed class ByteArraySequenceEqualRewriter : ExpressionVisitor
    {
        private static readonly MethodInfo SequenceEqualMethod = typeof(KingbaseByteArrayMethods)
            .GetMethod(nameof(KingbaseByteArrayMethods.SequenceEqual))!;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(Enumerable.SequenceEqual)
                && node.Arguments.Count == 2
                && UnwrapByteSpan(node.Arguments[0]).Type == typeof(byte[])
                && UnwrapByteSpan(node.Arguments[1]).Type == typeof(byte[]))
            {
                return Expression.Call(
                    SequenceEqualMethod,
                    UnwrapByteSpan(node.Arguments[0]),
                    UnwrapByteSpan(node.Arguments[1]));
            }

            return base.VisitMethodCall(node);
        }

        private static Expression UnwrapByteSpan(Expression expression)
            => expression is MethodCallExpression
            {
                Method.Name: "op_Implicit",
                Arguments.Count: 1
            } conversion
                ? conversion.Arguments[0]
                : expression;
    }
}
