using System.Collections.ObjectModel;
using System.Data;
using System.Text.Json;
using KdbndpTypes;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

public sealed class KingbaseTypeMappingSource : RelationalTypeMappingSource
{
    private static readonly RelationalTypeMapping JsonDocumentMapping = new KingbaseJsonTypeMapping(
        "jsonb", typeof(JsonDocument),
        new ValueComparer<JsonDocument>(
            (left, right) => left != null && right != null && JsonElement.DeepEquals(left.RootElement, right.RootElement),
            value => value.RootElement.GetRawText().GetHashCode(),
            value => JsonDocument.Parse(value.RootElement.GetRawText())));
    private static readonly RelationalTypeMapping JsonElementMapping = new KingbaseJsonTypeMapping(
        "jsonb", typeof(JsonElement),
        new ValueComparer<JsonElement>(
            (left, right) => JsonElement.DeepEquals(left, right),
            value => value.GetRawText().GetHashCode(),
            value => JsonDocument.Parse(value.GetRawText()).RootElement.Clone()));
    private static readonly RelationalTypeMapping IntArrayMapping = new KingbaseIntArrayTypeMapping();
    private static readonly RelationalTypeMapping IntListMapping = new KingbaseIntListTypeMapping();
    private static readonly RelationalTypeMapping IntRangeMapping = new KingbaseObjectTypeMapping("int4range", typeof(KdbndpRange<int>), KdbndpDbType.IntegerRange);
    private static readonly RelationalTypeMapping TsVectorMapping = new KingbaseObjectTypeMapping("tsvector", typeof(KdbndpTsVector), KdbndpDbType.TsVector);
    private static readonly RelationalTypeMapping TsQueryMapping = new KingbaseObjectTypeMapping("tsquery", typeof(KdbndpTsQuery), KdbndpDbType.TsQuery);
    private static readonly IReadOnlyDictionary<Type, RelationalTypeMapping> ClrTypeMappings =
        new ReadOnlyDictionary<Type, RelationalTypeMapping>(new Dictionary<Type, RelationalTypeMapping>
        {
            [typeof(bool)] = new BoolTypeMapping("boolean"),
            [typeof(byte)] = new ByteTypeMapping("smallint"),
            [typeof(sbyte)] = new SByteTypeMapping("smallint", DbType.Int16),
            [typeof(char)] = new CharTypeMapping("character(1)", DbType.StringFixedLength),
            [typeof(short)] = new ShortTypeMapping("smallint"),
            [typeof(ushort)] = new UShortTypeMapping("integer", DbType.Int32),
            [typeof(int)] = new IntTypeMapping("integer"),
            [typeof(uint)] = new UIntTypeMapping("bigint", DbType.Int64),
            [typeof(long)] = new LongTypeMapping("bigint"),
            [typeof(ulong)] = new ULongTypeMapping("numeric(20,0)", DbType.Decimal),
            [typeof(float)] = new FloatTypeMapping("real"),
            [typeof(double)] = new DoubleTypeMapping("double precision"),
            [typeof(decimal)] = new DecimalTypeMapping("numeric"),
            [typeof(string)] = new StringTypeMapping("text", DbType.String),
            [typeof(Guid)] = new GuidTypeMapping("uuid"),
            [typeof(byte[])] = new ByteArrayTypeMapping("bytea"),
            [typeof(DateOnly)] = new KingbaseDateOnlyTypeMapping(),
            [typeof(TimeOnly)] = new KingbaseTimeOnlyTypeMapping(),
            [typeof(TimeSpan)] = new TimeSpanTypeMapping("interval", DbType.Object),
            [typeof(DateTime)] = new DateTimeTypeMapping("timestamp without time zone"),
            [typeof(DateTimeOffset)] = new DateTimeOffsetTypeMapping("timestamp with time zone")
            ,[typeof(JsonDocument)] = JsonDocumentMapping
            ,[typeof(JsonElement)] = JsonElementMapping
            ,[typeof(int[])] = IntArrayMapping
            ,[typeof(List<int>)] = IntListMapping
            ,[typeof(KdbndpRange<int>)] = IntRangeMapping
            ,[typeof(KdbndpTsVector)] = TsVectorMapping
            ,[typeof(KdbndpTsQuery)] = TsQueryMapping
        });

    private static readonly IReadOnlyDictionary<string, RelationalTypeMapping> StoreTypeMappings =
        new ReadOnlyDictionary<string, RelationalTypeMapping>(new Dictionary<string, RelationalTypeMapping>(StringComparer.OrdinalIgnoreCase)
        {
            ["boolean"] = ClrTypeMappings[typeof(bool)],
            ["smallint"] = ClrTypeMappings[typeof(short)],
            ["int2"] = ClrTypeMappings[typeof(short)],
            ["integer"] = ClrTypeMappings[typeof(int)],
            ["int4"] = ClrTypeMappings[typeof(int)],
            ["bigint"] = ClrTypeMappings[typeof(long)],
            ["int8"] = ClrTypeMappings[typeof(long)],
            ["real"] = ClrTypeMappings[typeof(float)],
            ["double precision"] = ClrTypeMappings[typeof(double)],
            ["numeric"] = ClrTypeMappings[typeof(decimal)],
            ["text"] = ClrTypeMappings[typeof(string)],
            ["uuid"] = ClrTypeMappings[typeof(Guid)],
            ["bytea"] = ClrTypeMappings[typeof(byte[])],
            ["date"] = ClrTypeMappings[typeof(DateOnly)],
            ["time without time zone"] = ClrTypeMappings[typeof(TimeOnly)],
            ["interval"] = ClrTypeMappings[typeof(TimeSpan)],
            ["timestamp without time zone"] = ClrTypeMappings[typeof(DateTime)],
            ["timestamp with time zone"] = ClrTypeMappings[typeof(DateTimeOffset)]
            ,["json"] = JsonDocumentMapping
            ,["jsonb"] = JsonDocumentMapping
            ,["integer[]"] = IntArrayMapping
            ,["int4[]"] = IntArrayMapping
            ,["int4range"] = IntRangeMapping
            ,["tsvector"] = TsVectorMapping
            ,["tsquery"] = TsQueryMapping
        });

    public KingbaseTypeMappingSource(
        TypeMappingSourceDependencies dependencies,
        RelationalTypeMappingSourceDependencies relationalDependencies)
        : base(dependencies, relationalDependencies)
    {
    }

    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        if (mappingInfo.StoreTypeName is { } storeTypeName)
        {
            var requestedClrType = Nullable.GetUnderlyingType(mappingInfo.ClrType ?? typeof(object)) ?? mappingInfo.ClrType;
            if (requestedClrType is not null
                && ClrTypeMappings.TryGetValue(requestedClrType, out var clrMapping)
                && IsCompatibleAdvancedStoreType(storeTypeName, requestedClrType))
            {
                return clrMapping;
            }

            if (StoreTypeMappings.TryGetValue(storeTypeName, out var storeTypeMapping))
            {
                return storeTypeMapping;
            }

            var storeTypeNameBase = GetStoreTypeNameBase(storeTypeName);
            if (TryCreateStoreTypeMapping(storeTypeName, storeTypeNameBase, mappingInfo.ClrType, out storeTypeMapping))
            {
                return storeTypeMapping;
            }
        }

        if (mappingInfo.ClrType == typeof(string) && mappingInfo.Size is { } size)
        {
            var fixedLength = mappingInfo.IsFixedLength == true;
            return new StringTypeMapping(
                fixedLength ? $"character({size})" : $"character varying({size})",
                fixedLength ? DbType.StringFixedLength : DbType.String,
                unicode: true,
                size);
        }

        if (mappingInfo.ClrType == typeof(decimal) && mappingInfo.Precision is { } precision)
        {
            var scale = mappingInfo.Scale ?? 0;
            return new DecimalTypeMapping($"numeric({precision},{scale})", DbType.Decimal, precision, scale);
        }

        if (mappingInfo.ClrType is { } clrType
            && ClrTypeMappings.TryGetValue(Nullable.GetUnderlyingType(clrType) ?? clrType, out var clrTypeMapping))
        {
            return clrTypeMapping;
        }

        return base.FindMapping(mappingInfo);
    }

    private static bool IsCompatibleAdvancedStoreType(string storeTypeName, Type clrType)
        => storeTypeName.ToLowerInvariant() switch
        {
            "json" or "jsonb" => clrType == typeof(JsonDocument) || clrType == typeof(JsonElement),
            "integer[]" or "int4[]" => clrType == typeof(int[]) || clrType == typeof(List<int>),
            "int4range" => clrType == typeof(KdbndpRange<int>),
            "tsvector" => clrType == typeof(KdbndpTsVector),
            "tsquery" => clrType == typeof(KdbndpTsQuery),
            _ => false
        };

    private static bool TryCreateStoreTypeMapping(
        string storeTypeName,
        string storeTypeNameBase,
        Type? clrType,
        out RelationalTypeMapping mapping)
    {
        switch (storeTypeNameBase.ToLowerInvariant())
        {
            case "varchar":
            case "character varying":
            case "nvarchar":
            case "varchar2":
                mapping = new StringTypeMapping(storeTypeName, DbType.String, unicode: true, TryParseSize(storeTypeName));
                return true;
            case "char":
            case "character":
            case "nchar":
                mapping = clrType == typeof(char)
                    ? new CharTypeMapping(storeTypeName, DbType.StringFixedLength)
                    : new StringTypeMapping(storeTypeName, DbType.StringFixedLength, unicode: true, TryParseSize(storeTypeName));
                return true;
            case "decimal":
            case "numeric":
            case "number":
                mapping = new DecimalTypeMapping(storeTypeName, DbType.Decimal);
                return true;
            case "timestamp":
            case "timestamp without time zone":
                mapping = new DateTimeTypeMapping(storeTypeName);
                return true;
            case "timestamp with time zone":
                mapping = new DateTimeOffsetTypeMapping(storeTypeName);
                return true;
            case "time":
            case "time without time zone":
                mapping = new TimeOnlyTypeMapping(storeTypeName);
                return true;
            case "blob":
            case "bytea":
                mapping = new ByteArrayTypeMapping(storeTypeName);
                return true;
            case "clob":
            case "text":
                mapping = new StringTypeMapping(storeTypeName, DbType.String);
                return true;
            default:
                mapping = null!;
                return false;
        }
    }

    private static string GetStoreTypeNameBase(string storeTypeName)
    {
        var openParenthesis = storeTypeName.IndexOf('(');
        return (openParenthesis < 0 ? storeTypeName : storeTypeName[..openParenthesis]).Trim();
    }

    private static int? TryParseSize(string storeTypeName)
    {
        var openParenthesis = storeTypeName.IndexOf('(');
        var closeParenthesis = storeTypeName.IndexOf(')', openParenthesis + 1);
        return openParenthesis >= 0
            && closeParenthesis > openParenthesis
            && int.TryParse(storeTypeName.AsSpan(openParenthesis + 1, closeParenthesis - openParenthesis - 1), out var size)
                ? size
                : null;
    }

}
