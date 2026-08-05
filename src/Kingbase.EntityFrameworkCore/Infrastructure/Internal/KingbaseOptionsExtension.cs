using System.Globalization;
using System.Runtime.CompilerServices;
using Kdbndp;
using Kingbase.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kingbase.EntityFrameworkCore.Infrastructure.Internal;

public sealed class KingbaseOptionsExtension : RelationalOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public KingbaseOptionsExtension()
    {
    }

    private KingbaseOptionsExtension(KingbaseOptionsExtension copyFrom)
        : base(copyFrom)
    {
        CompatibilityMode = copyFrom.CompatibilityMode;
        AdminDatabase = copyFrom.AdminDatabase;
        MaxRetryCount = copyFrom.MaxRetryCount;
        MaxRetryDelay = copyFrom.MaxRetryDelay;
        AdditionalTransientErrorCodes = copyFrom.AdditionalTransientErrorCodes;
        DataSource = copyFrom.DataSource;
    }

    public KingbaseCompatibilityMode CompatibilityMode { get; private set; } = KingbaseCompatibilityMode.Auto;
    public string AdminDatabase { get; private set; } = "template1";
    public int? MaxRetryCount { get; private set; }
    public TimeSpan? MaxRetryDelay { get; private set; }
    public IReadOnlyCollection<string> AdditionalTransientErrorCodes { get; private set; } = Array.Empty<string>();
    public KdbndpDataSource? DataSource { get; private set; }

    public KingbaseOptionsExtension WithDataSource(KdbndpDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        var clone = (KingbaseOptionsExtension)Clone();
        clone.DataSource = dataSource;
        return clone;
    }

    public KingbaseOptionsExtension WithCompatibilityMode(KingbaseCompatibilityMode compatibilityMode)
    {
        var clone = (KingbaseOptionsExtension)Clone();
        clone.CompatibilityMode = compatibilityMode;
        return clone;
    }

    public KingbaseOptionsExtension WithAdminDatabase(string adminDatabase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminDatabase);
        var clone = (KingbaseOptionsExtension)Clone();
        clone.AdminDatabase = adminDatabase;
        return clone;
    }

    public KingbaseOptionsExtension WithRetryOnFailure(int maxRetryCount, TimeSpan maxRetryDelay, IEnumerable<string>? errorCodes)
    {
        var clone = (KingbaseOptionsExtension)Clone();
        clone.MaxRetryCount = maxRetryCount;
        clone.MaxRetryDelay = maxRetryDelay;
        clone.AdditionalTransientErrorCodes = errorCodes?.Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        return clone;
    }

    protected override RelationalOptionsExtension Clone()
        => new KingbaseOptionsExtension(this);

    public override void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkKingbase();

    public override DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : RelationalExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => true;

        private KingbaseOptionsExtension KingbaseExtension
            => (KingbaseOptionsExtension)Extension;

        public override string LogFragment
            => base.LogFragment + $"KingbaseESCompatibilityMode={KingbaseExtension.CompatibilityMode} AdminDatabase={KingbaseExtension.AdminDatabase} ";

        public override int GetServiceProviderHashCode()
            => HashCode.Combine(
                KingbaseExtension.CompatibilityMode,
                KingbaseExtension.AdminDatabase,
                KingbaseExtension.MaxRetryCount,
                KingbaseExtension.MaxRetryDelay,
                KingbaseExtension.DataSource is null ? 0 : RuntimeHelpers.GetHashCode(KingbaseExtension.DataSource));

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo otherInfo
                && KingbaseExtension.CompatibilityMode == otherInfo.KingbaseExtension.CompatibilityMode
                && KingbaseExtension.AdminDatabase == otherInfo.KingbaseExtension.AdminDatabase
                && KingbaseExtension.MaxRetryCount == otherInfo.KingbaseExtension.MaxRetryCount
                && KingbaseExtension.MaxRetryDelay == otherInfo.KingbaseExtension.MaxRetryDelay
                && ReferenceEquals(KingbaseExtension.DataSource, otherInfo.KingbaseExtension.DataSource)
                && KingbaseExtension.AdditionalTransientErrorCodes.SequenceEqual(otherInfo.KingbaseExtension.AdditionalTransientErrorCodes);

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Kingbase.EntityFrameworkCore:CompatibilityMode"] =
                ((int)KingbaseExtension.CompatibilityMode).ToString(CultureInfo.InvariantCulture);
            debugInfo["Kingbase.EntityFrameworkCore:AdminDatabase"] = KingbaseExtension.AdminDatabase;
            debugInfo["Kingbase.EntityFrameworkCore:Retry"] = KingbaseExtension.MaxRetryCount?.ToString(CultureInfo.InvariantCulture) ?? "Disabled";
            debugInfo["Kingbase.EntityFrameworkCore:DataSource"] = KingbaseExtension.DataSource is null ? "None" : "Configured";
        }
    }
}
