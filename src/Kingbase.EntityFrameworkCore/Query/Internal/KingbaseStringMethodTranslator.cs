using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(string))
        {
            return null;
        }

        if (method.IsStatic)
        {
            return TranslateStatic(method, arguments);
        }

        if (instance is null)
        {
            return null;
        }

        return method.Name switch
        {
            nameof(string.ToLower) when arguments.Count == 0 => Function("lower", instance, method.ReturnType),
            nameof(string.ToUpper) when arguments.Count == 0 => Function("upper", instance, method.ReturnType),
            nameof(string.Replace) when arguments.Count == 2 => Function("replace", [instance, arguments[0], arguments[1]], method.ReturnType),
            nameof(string.Substring) when arguments.Count == 1 => Function("substr", [instance, OneBased(arguments[0])], method.ReturnType),
            nameof(string.Substring) when arguments.Count == 2 => Function("substr", [instance, OneBased(arguments[0]), arguments[1]], method.ReturnType),
            nameof(string.IndexOf) when arguments.Count == 1 => sqlExpressionFactory.Subtract(
                Function("strpos", [instance, AsString(arguments[0])], typeof(int)),
                sqlExpressionFactory.Constant(1)),
            nameof(string.Contains) when arguments.Count == 1 => sqlExpressionFactory.GreaterThan(
                Function("strpos", [instance, AsString(arguments[0])], typeof(int)),
                sqlExpressionFactory.Constant(0)),
            nameof(string.StartsWith) when arguments.Count == 1 => sqlExpressionFactory.Equal(
                Function("left", [instance, StringLength(arguments[0])], typeof(string)),
                AsString(arguments[0])),
            nameof(string.EndsWith) when arguments.Count == 1 => sqlExpressionFactory.Equal(
                Function("right", [instance, StringLength(arguments[0])], typeof(string)),
                AsString(arguments[0])),
            nameof(string.Trim) when arguments.Count == 0 => Function("btrim", instance, method.ReturnType),
            nameof(string.TrimStart) when arguments.Count == 0 => Function("ltrim", instance, method.ReturnType),
            nameof(string.TrimEnd) when arguments.Count == 0 => Function("rtrim", instance, method.ReturnType),
            _ => null
        };
    }

    private SqlExpression? TranslateStatic(MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        => method.Name switch
        {
            nameof(string.IsNullOrEmpty) when arguments.Count == 1 => sqlExpressionFactory.OrElse(
                sqlExpressionFactory.IsNull(arguments[0]),
                sqlExpressionFactory.Equal(arguments[0], sqlExpressionFactory.Constant(string.Empty))),
            nameof(string.Concat) when arguments.Count == 2 => sqlExpressionFactory.Add(AsString(arguments[0]), AsString(arguments[1])),
            _ => null
        };

    private SqlExpression StringLength(SqlExpression expression)
        => Function("length", AsString(expression), typeof(int));

    private SqlExpression OneBased(SqlExpression expression)
        => sqlExpressionFactory.Add(expression, sqlExpressionFactory.Constant(1));

    private SqlExpression AsString(SqlExpression expression)
        => expression.Type == typeof(char)
            ? sqlExpressionFactory.Convert(expression, typeof(string))
            : expression;

    private SqlExpression Function(string name, SqlExpression argument, Type returnType)
        => Function(name, [argument], returnType);

    private SqlExpression Function(string name, IReadOnlyList<SqlExpression> arguments, Type returnType)
        => sqlExpressionFactory.Function(
            name,
            arguments,
            nullable: true,
            argumentsPropagateNullability: Enumerable.Repeat(true, arguments.Count).ToArray(),
            returnType);
}
