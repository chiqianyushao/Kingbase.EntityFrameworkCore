using Kingbase.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Kingbase.EntityFrameworkCore.Tests.Storage;

public sealed class KingbaseTypeMappingSourceTests
{
    [Theory]
    [InlineData(typeof(bool), "boolean")]
    [InlineData(typeof(char), "character(1)")]
    [InlineData(typeof(uint), "bigint")]
    [InlineData(typeof(ulong), "numeric(20,0)")]
    [InlineData(typeof(int), "integer")]
    [InlineData(typeof(long), "bigint")]
    [InlineData(typeof(string), "text")]
    [InlineData(typeof(Guid), "uuid")]
    [InlineData(typeof(DateOnly), "date")]
    [InlineData(typeof(DateTime), "timestamp without time zone")]
    public void Maps_core_clr_types(Type clrType, string storeType)
    {
        using var services = new ServiceCollection()
            .AddEntityFrameworkKingbase()
            .BuildServiceProvider();

        var source = services.GetRequiredService<IRelationalTypeMappingSource>();
        var mapping = source.FindMapping(clrType);

        Assert.NotNull(mapping);
        Assert.Equal(storeType, mapping.StoreType);
    }

    [Theory]
    [InlineData("varchar(128)", typeof(string), 128)]
    [InlineData("character(8)", typeof(string), 8)]
    [InlineData("varchar2(64)", typeof(string), 64)]
    public void Maps_sized_string_store_types(string storeType, Type clrType, int size)
    {
        using var services = new ServiceCollection()
            .AddEntityFrameworkKingbase()
            .BuildServiceProvider();

        var source = services.GetRequiredService<IRelationalTypeMappingSource>();
        var mapping = source.FindMapping(storeType);

        Assert.NotNull(mapping);
        Assert.Equal(clrType, mapping.ClrType);
        Assert.Equal(size, mapping.Size);
    }
}
