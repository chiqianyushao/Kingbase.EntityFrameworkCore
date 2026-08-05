using System.Data.Common;
using Kingbase.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

public sealed class KingbaseRelationalConnection(RelationalConnectionDependencies dependencies)
    : RelationalConnection(dependencies)
{
    private readonly KingbaseOptionsExtension? _options = dependencies.ContextOptions.FindExtension<KingbaseOptionsExtension>();

    protected override bool SupportsAmbientTransactions => true;

    protected override DbConnection CreateDbConnection()
        => _options?.DataSource?.CreateConnection() ?? KingbaseConnectionFactory.Create(ConnectionString);
}
