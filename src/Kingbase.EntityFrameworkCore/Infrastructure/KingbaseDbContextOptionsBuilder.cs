using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Kingbase.EntityFrameworkCore.Infrastructure.Internal;

namespace Kingbase.EntityFrameworkCore.Infrastructure;

public sealed class KingbaseDbContextOptionsBuilder
    : RelationalDbContextOptionsBuilder<KingbaseDbContextOptionsBuilder, Infrastructure.Internal.KingbaseOptionsExtension>
{
    public KingbaseDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        : base(optionsBuilder)
    {
    }

    public KingbaseDbContextOptionsBuilder SetPostgresCompatibilityMode()
        => SetCompatibilityMode(KingbaseCompatibilityMode.Postgres);

    public KingbaseDbContextOptionsBuilder SetOracleCompatibilityMode()
        => SetCompatibilityMode(KingbaseCompatibilityMode.Oracle);

    public KingbaseDbContextOptionsBuilder DetectCompatibilityMode()
        => SetCompatibilityMode(KingbaseCompatibilityMode.Auto);

    public KingbaseDbContextOptionsBuilder UseAdminDatabase(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        var extension = OptionsBuilder.Options.FindExtension<KingbaseOptionsExtension>() ?? new KingbaseOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder)
            .AddOrUpdateExtension(extension.WithAdminDatabase(databaseName));
        return this;
    }

    public KingbaseDbContextOptionsBuilder EnableRetryOnFailure(int maxRetryCount = 6, TimeSpan? maxRetryDelay = null, IEnumerable<string>? errorCodesToAdd = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetryCount);
        var retryDelay = maxRetryDelay ?? TimeSpan.FromSeconds(30);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.Zero);
        var extension = OptionsBuilder.Options.FindExtension<KingbaseOptionsExtension>() ?? new KingbaseOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension.WithRetryOnFailure(maxRetryCount, retryDelay, errorCodesToAdd));
        return this;
    }

    private KingbaseDbContextOptionsBuilder SetCompatibilityMode(KingbaseCompatibilityMode compatibilityMode)
    {
        var extension = OptionsBuilder.Options.FindExtension<KingbaseOptionsExtension>() ?? new KingbaseOptionsExtension();
        ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder)
            .AddOrUpdateExtension(extension.WithCompatibilityMode(compatibilityMode));
        return this;
    }
}
