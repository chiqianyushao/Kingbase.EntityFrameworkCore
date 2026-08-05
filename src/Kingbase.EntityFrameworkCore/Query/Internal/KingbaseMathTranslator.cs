using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseMathTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
{
    private static readonly IReadOnlyDictionary<string, string> Functions = new Dictionary<string, string>
    {
        [nameof(Math.Abs)] = "abs",
        [nameof(Math.Acos)] = "acos",
        [nameof(Math.Acosh)] = "acosh",
        [nameof(Math.Asin)] = "asin",
        [nameof(Math.Asinh)] = "asinh",
        [nameof(Math.Atan)] = "atan",
        [nameof(Math.Atan2)] = "atan2",
        [nameof(Math.Atanh)] = "atanh",
        [nameof(Math.Cbrt)] = "cbrt",
        [nameof(Math.Ceiling)] = "ceil",
        [nameof(Math.Cos)] = "cos",
        [nameof(Math.Cosh)] = "cosh",
        [nameof(Math.Exp)] = "exp",
        [nameof(Math.Floor)] = "floor",
        [nameof(Math.Log10)] = "log",
        [nameof(Math.Pow)] = "power",
        [nameof(Math.Round)] = "round",
        [nameof(Math.Sign)] = "sign",
        [nameof(Math.Sin)] = "sin",
        [nameof(Math.Sinh)] = "sinh",
        [nameof(Math.Sqrt)] = "sqrt",
        [nameof(Math.Tan)] = "tan",
        [nameof(Math.Tanh)] = "tanh",
        [nameof(Math.Truncate)] = "trunc"
    };

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(Math) && method.DeclaringType != typeof(MathF))
        {
            return null;
        }

        if (method.Name == nameof(Math.Log) && arguments.Count == 1)
        {
            return Function("ln", arguments, method.ReturnType);
        }

        if (method.Name == nameof(Math.Log) && arguments.Count == 2)
        {
            return sqlExpressionFactory.Divide(
                Function("ln", [arguments[0]], method.ReturnType),
                Function("ln", [arguments[1]], method.ReturnType));
        }

        if (method.Name == nameof(Math.Log2) && arguments.Count == 1)
        {
            return sqlExpressionFactory.Divide(
                Function("ln", arguments, method.ReturnType),
                Function("ln", [sqlExpressionFactory.Constant(2.0)], method.ReturnType));
        }

        if (method.Name == nameof(Math.Max) && arguments.Count == 2)
        {
            return Function("greatest", arguments, method.ReturnType);
        }

        if (method.Name == nameof(Math.Min) && arguments.Count == 2)
        {
            return Function("least", arguments, method.ReturnType);
        }

        if (method.Name == nameof(Math.Clamp) && arguments.Count == 3)
        {
            return Function(
                "greatest",
                [arguments[1], Function("least", [arguments[2], arguments[0]], method.ReturnType)],
                method.ReturnType);
        }

        if (method.Name == nameof(Math.FusedMultiplyAdd) && arguments.Count == 3)
        {
            return sqlExpressionFactory.Add(
                sqlExpressionFactory.Multiply(arguments[0], arguments[1]),
                arguments[2]);
        }

        if (method.Name == nameof(Math.Round)
            && arguments.Count == 2
            && method.GetParameters()[1].ParameterType == typeof(MidpointRounding))
        {
            return null;
        }

        return Functions.TryGetValue(method.Name, out var function)
            ? Function(function, arguments, method.ReturnType)
            : null;
    }

    private SqlExpression Function(string name, IReadOnlyList<SqlExpression> arguments, Type returnType)
        => sqlExpressionFactory.Function(
            name,
            arguments,
            nullable: true,
            argumentsPropagateNullability: Enumerable.Repeat(true, arguments.Count).ToArray(),
            returnType);
}
