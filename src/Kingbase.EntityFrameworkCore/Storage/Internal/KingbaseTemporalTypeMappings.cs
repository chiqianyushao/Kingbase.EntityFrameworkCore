using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal sealed class KingbaseDateOnlyTypeMapping : RelationalTypeMapping
{
    private static readonly MethodInfo GetDateTimeMethod = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue))!.MakeGenericMethod(typeof(DateTime));
    private static readonly MethodInfo FromDateTimeMethod = typeof(DateOnly).GetMethod(nameof(DateOnly.FromDateTime), [typeof(DateTime)])!;

    public KingbaseDateOnlyTypeMapping(string storeType = "date") : base(storeType, typeof(DateOnly), System.Data.DbType.Date) { }
    private KingbaseDateOnlyTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new KingbaseDateOnlyTypeMapping(parameters);
    public override MethodInfo GetDataReaderMethod() => GetDateTimeMethod;
    public override Expression CustomizeDataReaderExpression(Expression expression) => Expression.Call(FromDateTimeMethod, expression);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter.Value is DateOnly value) parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
    protected override string GenerateNonNullSqlLiteral(object value) => $"DATE '{((DateOnly)value):yyyy-MM-dd}'";
}

internal sealed class KingbaseTimeOnlyTypeMapping : RelationalTypeMapping
{
    private static readonly MethodInfo GetTimeSpanMethod = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue))!.MakeGenericMethod(typeof(TimeSpan));
    private static readonly MethodInfo FromTimeSpanMethod = typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromTimeSpan), [typeof(TimeSpan)])!;

    public KingbaseTimeOnlyTypeMapping(string storeType = "time without time zone") : base(storeType, typeof(TimeOnly), System.Data.DbType.Time) { }
    private KingbaseTimeOnlyTypeMapping(RelationalTypeMappingParameters parameters) : base(parameters) { }
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new KingbaseTimeOnlyTypeMapping(parameters);
    public override MethodInfo GetDataReaderMethod() => GetTimeSpanMethod;
    public override Expression CustomizeDataReaderExpression(Expression expression) => Expression.Call(FromTimeSpanMethod, expression);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        base.ConfigureParameter(parameter);
        if (parameter.Value is TimeOnly value) parameter.Value = value.ToTimeSpan();
    }
    protected override string GenerateNonNullSqlLiteral(object value) => $"TIME '{((TimeOnly)value):HH:mm:ss.ffffff}'";
}
