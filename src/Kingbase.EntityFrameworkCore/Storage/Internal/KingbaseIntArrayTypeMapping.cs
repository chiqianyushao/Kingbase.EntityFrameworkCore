using System.Data.Common;
using System.Globalization;
using Kdbndp;
using KdbndpTypes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal sealed class KingbaseIntArrayTypeMapping : RelationalTypeMapping
{
    private static readonly RelationalTypeMapping IntElementMapping = new IntTypeMapping("integer");

    public KingbaseIntArrayTypeMapping()
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(int[]),
                comparer: new ValueComparer<int[]>(
                    (left, right) => left != null && right != null && left.SequenceEqual(right),
                    value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    value => value.ToArray()),
                elementMapping: IntElementMapping),
            "integer[]",
            dbType: System.Data.DbType.Object))
    {
    }

    private KingbaseIntArrayTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new KingbaseIntArrayTypeMapping(parameters);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter is KdbndpParameter kdbndpParameter) kdbndpParameter.KdbndpDbType = KdbndpDbType.Array | KdbndpDbType.Integer;
    }
    protected override string GenerateNonNullSqlLiteral(object value)
        => $"ARRAY[{string.Join(',', ((int[])value).Select(item => item.ToString(CultureInfo.InvariantCulture)))}]::integer[]";
}
