using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Kdbndp;
using KdbndpTypes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal sealed class KingbaseIntListTypeMapping : RelationalTypeMapping
{
    private static readonly RelationalTypeMapping IntElementMapping = new IntTypeMapping("integer");
    private static readonly MethodInfo GetArrayMethod = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue))!.MakeGenericMethod(typeof(int[]));
    private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!.MakeGenericMethod(typeof(int));

    public KingbaseIntListTypeMapping()
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(List<int>),
                comparer: new ValueComparer<List<int>>(
                    (left, right) => left != null && right != null && left.SequenceEqual(right),
                    value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    value => value.ToList()),
                elementMapping: IntElementMapping),
            "integer[]",
            dbType: System.Data.DbType.Object))
    {
    }

    private KingbaseIntListTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new KingbaseIntListTypeMapping(parameters);
    public override MethodInfo GetDataReaderMethod() => GetArrayMethod;
    public override Expression CustomizeDataReaderExpression(Expression expression) => Expression.Call(ToListMethod, expression);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter.Value is List<int> value) parameter.Value = value.ToArray();
        if (parameter is KdbndpParameter kdbndpParameter) kdbndpParameter.KdbndpDbType = KdbndpDbType.Array | KdbndpDbType.Integer;
    }
    protected override string GenerateNonNullSqlLiteral(object value)
        => $"ARRAY[{string.Join(',', (List<int>)value)}]::integer[]";
}
