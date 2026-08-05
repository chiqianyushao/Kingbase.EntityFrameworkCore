using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies)
    : QuerySqlGenerator(dependencies)
{
    protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
    {
        if (sqlBinaryExpression.OperatorType == ExpressionType.Add
            && sqlBinaryExpression.Type == typeof(string))
        {
            Sql.Append("(");
            Visit(sqlBinaryExpression.Left);
            Sql.Append(" || ");
            Visit(sqlBinaryExpression.Right);
            Sql.Append(")");
            return sqlBinaryExpression;
        }

        return base.VisitSqlBinary(sqlBinaryExpression);
    }

    protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
    {
        Sql.Append("JOIN LATERAL ");
        Visit(crossApplyExpression.Table);
        Sql.Append(" ON TRUE");
        return crossApplyExpression;
    }

    protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
    {
        Sql.Append("LEFT JOIN LATERAL ");
        Visit(outerApplyExpression.Table);
        Sql.Append(" ON TRUE");
        return outerApplyExpression;
    }

    protected override void GenerateTop(SelectExpression selectExpression)
    {
    }

    protected override void GenerateLimitOffset(SelectExpression selectExpression)
    {
        if (selectExpression.Limit is not null)
        {
            Sql.AppendLine().Append("LIMIT ");
            Visit(selectExpression.Limit);
        }

        if (selectExpression.Offset is not null)
        {
            Sql.Append(selectExpression.Limit is null ? Environment.NewLine : " ");
            Sql.Append("OFFSET ");
            Visit(selectExpression.Offset);
        }
    }
}
