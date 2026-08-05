using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseSqlTranslatingExpressionVisitorFactory(
    RelationalSqlTranslatingExpressionVisitorDependencies dependencies)
    : IRelationalSqlTranslatingExpressionVisitorFactory
{
    public RelationalSqlTranslatingExpressionVisitor Create(
        QueryCompilationContext queryCompilationContext,
        QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
        => new KingbaseSqlTranslatingExpressionVisitor(
            dependencies,
            queryCompilationContext,
            queryableMethodTranslatingExpressionVisitor);
}
