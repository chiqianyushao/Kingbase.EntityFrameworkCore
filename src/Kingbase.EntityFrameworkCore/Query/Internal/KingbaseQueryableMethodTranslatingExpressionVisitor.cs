using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryableMethodTranslatingExpressionVisitor
    : RelationalQueryableMethodTranslatingExpressionVisitor
{
    public KingbaseQueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
        RelationalQueryCompilationContext queryCompilationContext)
        : base(dependencies, relationalDependencies, queryCompilationContext)
    {
    }

    private KingbaseQueryableMethodTranslatingExpressionVisitor(
        KingbaseQueryableMethodTranslatingExpressionVisitor parentVisitor)
        : base(parentVisitor)
    {
    }

    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new KingbaseQueryableMethodTranslatingExpressionVisitor(this);

    protected override ShapedQueryExpression? TranslateRightJoin(
        ShapedQueryExpression outer,
        ShapedQueryExpression inner,
        LambdaExpression outerKeySelector,
        LambdaExpression innerKeySelector,
        LambdaExpression resultSelector)
    {
        ((SelectExpression)outer.QueryExpression).PushdownIntoSubquery();
        return base.TranslateRightJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
    }
}
