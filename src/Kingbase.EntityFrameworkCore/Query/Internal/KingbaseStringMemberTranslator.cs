using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseStringMemberTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMemberTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        => instance is not null
            && member.DeclaringType == typeof(string)
            && member.Name == nameof(string.Length)
                ? sqlExpressionFactory.Function(
                    "length",
                    [instance],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    returnType)
                : null;
}
