using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Kdbndp;
using KdbndpTypes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal sealed class KingbaseJsonTypeMapping : RelationalTypeMapping
{
    private static readonly MethodInfo GetStringMethod = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString))!;
    private static readonly MethodInfo ParseDocumentMethod = typeof(KingbaseJsonTypeMapping).GetMethod(nameof(ParseDocument), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ParseElementMethod = typeof(KingbaseJsonTypeMapping).GetMethod(nameof(ParseElement), BindingFlags.Static | BindingFlags.NonPublic)!;

    public KingbaseJsonTypeMapping(
        string storeType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type clrType,
        ValueComparer comparer)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(clrType, comparer: comparer),
            storeType,
            dbType: System.Data.DbType.Object))
    {
    }

    private KingbaseJsonTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new KingbaseJsonTypeMapping(parameters);
    public override MethodInfo GetDataReaderMethod() => GetStringMethod;
    public override Expression CustomizeDataReaderExpression(Expression expression)
        => Expression.Call(ClrType == typeof(JsonDocument) ? ParseDocumentMethod : ParseElementMethod, expression);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter is KdbndpParameter kdbndpParameter)
        {
            kdbndpParameter.KdbndpDbType = StoreType == "json" ? KdbndpDbType.Json : KdbndpDbType.Jsonb;
        }
    }
    protected override string GenerateNonNullSqlLiteral(object value)
    {
        var json = value is JsonDocument document ? document.RootElement.GetRawText() : ((JsonElement)value).GetRawText();
        return $"'{json.Replace("'", "''", StringComparison.Ordinal)}'::{StoreType}";
    }
    private static JsonDocument ParseDocument(string value) => JsonDocument.Parse(value);
    private static JsonElement ParseElement(string value) => JsonDocument.Parse(value).RootElement.Clone();
}
