using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryableMethodTranslatingExpressionVisitorFactory(
    QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
    RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies)
    : IQueryableMethodTranslatingExpressionVisitorFactory
{
    public QueryableMethodTranslatingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
        => new KingbaseQueryableMethodTranslatingExpressionVisitor(
            dependencies,
            relationalDependencies,
            (RelationalQueryCompilationContext)queryCompilationContext);
}
