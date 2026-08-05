using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseSqlTranslatingExpressionVisitor(
    RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
    QueryCompilationContext queryCompilationContext,
    QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
    : RelationalSqlTranslatingExpressionVisitor(
        dependencies,
        queryCompilationContext,
        queryableMethodTranslatingExpressionVisitor)
{
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        return base.VisitMethodCall(methodCallExpression);
    }
}
