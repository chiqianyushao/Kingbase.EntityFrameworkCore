using System.Data.Common;
using Kdbndp;
using Kingbase.EntityFrameworkCore.Infrastructure;
using Kingbase.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

public static class KingbaseDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseKdbndp(
        this DbContextOptionsBuilder optionsBuilder,
        string? connectionString,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        var extension = GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        kingbaseOptionsAction?.Invoke(new KingbaseDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder UseKdbndp(
        this DbContextOptionsBuilder optionsBuilder,
        KdbndpDataSource dataSource,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(dataSource);
        var extension = ((KingbaseOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(dataSource.ConnectionString))
            .WithDataSource(dataSource);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        kingbaseOptionsAction?.Invoke(new KingbaseDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder UseKdbndp(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection = false,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(connection);
        var extension = GetOrCreateExtension(optionsBuilder).WithConnection(connection, contextOwnsConnection);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        kingbaseOptionsAction?.Invoke(new KingbaseDbContextOptionsBuilder(optionsBuilder));
        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseKdbndp<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string? connectionString,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseKdbndp((DbContextOptionsBuilder)optionsBuilder, connectionString, kingbaseOptionsAction);

    public static DbContextOptionsBuilder<TContext> UseKdbndp<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        KdbndpDataSource dataSource,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseKdbndp((DbContextOptionsBuilder)optionsBuilder, dataSource, kingbaseOptionsAction);

    public static DbContextOptionsBuilder<TContext> UseKdbndp<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection = false,
        Action<KingbaseDbContextOptionsBuilder>? kingbaseOptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseKdbndp((DbContextOptionsBuilder)optionsBuilder, connection, contextOwnsConnection, kingbaseOptionsAction);

    private static KingbaseOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.Options.FindExtension<KingbaseOptionsExtension>() ?? new KingbaseOptionsExtension();
}
