using System.Linq.Expressions;
using Kingbase.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microsoft.EntityFrameworkCore;

public static class KingbaseIndexBuilderExtensions
{
    public static IndexBuilder IncludeProperties(this IndexBuilder indexBuilder, params string[] propertyNames)
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);
        ArgumentNullException.ThrowIfNull(propertyNames);
        if (propertyNames.Length == 0 || propertyNames.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty include property name is required.", nameof(propertyNames));
        }
        indexBuilder.HasAnnotation(KingbaseAnnotationNames.IndexInclude, propertyNames.Distinct(StringComparer.Ordinal).ToArray());
        return indexBuilder;
    }

    public static IndexBuilder<TEntity> IncludeProperties<TEntity>(
        this IndexBuilder<TEntity> indexBuilder,
        Expression<Func<TEntity, object?>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(indexBuilder);
        ArgumentNullException.ThrowIfNull(includeExpression);
        IncludeProperties((IndexBuilder)indexBuilder, ExtractPropertyNames(includeExpression.Body));
        return indexBuilder;
    }

    private static string[] ExtractPropertyNames(Expression expression)
        => expression switch
        {
            NewExpression newExpression => newExpression.Arguments.Select(ExtractPropertyName).ToArray(),
            _ => [ExtractPropertyName(expression)]
        };

    private static string ExtractPropertyName(Expression expression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } conversion)
        {
            expression = conversion.Operand;
        }
        return expression is MemberExpression memberExpression
            ? memberExpression.Member.Name
            : throw new ArgumentException("Include expression must select one or more mapped properties.");
    }
}
