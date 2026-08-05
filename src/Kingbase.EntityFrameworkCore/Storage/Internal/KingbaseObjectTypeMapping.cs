using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Kdbndp;
using KdbndpTypes;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal sealed class KingbaseObjectTypeMapping : RelationalTypeMapping
{
    private readonly KdbndpDbType? _kdbndpDbType;

    public KingbaseObjectTypeMapping(
        string storeType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type clrType,
        KdbndpDbType? kdbndpDbType = null)
        : base(storeType, clrType, System.Data.DbType.Object)
    {
        _kdbndpDbType = kdbndpDbType;
    }

    private KingbaseObjectTypeMapping(RelationalTypeMappingParameters parameters, KdbndpDbType? kdbndpDbType)
        : base(parameters)
    {
        _kdbndpDbType = kdbndpDbType;
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new KingbaseObjectTypeMapping(parameters, _kdbndpDbType);

    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (_kdbndpDbType is { } kdbndpDbType && parameter is KdbndpParameter kdbndpParameter)
        {
            kdbndpParameter.KdbndpDbType = kdbndpDbType;
        }
    }

    protected override string GenerateNonNullSqlLiteral(object value)
        => StoreType switch
        {
            "json" or "jsonb" => $"'{Escape(Json(value))}'::{StoreType}",
            "integer[]" => $"ARRAY[{string.Join(",", ((IEnumerable<int>)value).Select(number => number.ToString(CultureInfo.InvariantCulture)))}]::integer[]",
            "int4range" => $"'{Escape(value.ToString()!)}'::int4range",
            "tsvector" => $"'{Escape(value.ToString()!)}'::tsvector",
            _ => $"'{Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}'::{StoreType}"
        };

    private static string Json(object value)
        => value switch
        {
            JsonDocument document => document.RootElement.GetRawText(),
            JsonElement element => element.GetRawText(),
            _ => throw new InvalidOperationException($"Unsupported JSON literal CLR type '{value.GetType()}'.")
        };

    private static string Escape(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);
}
