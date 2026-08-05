using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using Kdbndp;
using Kingbase.EntityFrameworkCore.Infrastructure.Internal;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

public sealed class KingbaseDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies)
    : RelationalDatabaseCreator(dependencies)
{
    public override bool Exists()
    {
        var connection = Dependencies.Connection.DbConnection;
        var shouldClose = connection.State == ConnectionState.Closed;

        try
        {
            if (shouldClose)
            {
                connection.Open();
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shouldClose && connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }
    }

    public override async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        var connection = Dependencies.Connection.DbConnection;
        var shouldClose = connection.State == ConnectionState.Closed;

        try
        {
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shouldClose && connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
        }
    }

    public override void Create()
    {
        var (databaseName, adminConnectionString) = GetDatabaseAdministrationInfo();
        using var connection = KingbaseConnectionFactory.Create(adminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName)}";
        command.ExecuteNonQuery();
    }

    public override async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        var (databaseName, adminConnectionString) = GetDatabaseAdministrationInfo();
        await using var connection = KingbaseConnectionFactory.Create(adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override void Delete()
    {
        Dependencies.Connection.Close();
        ClearTargetPool();
        var (databaseName, adminConnectionString) = GetDatabaseAdministrationInfo();
        using var connection = KingbaseConnectionFactory.Create(adminConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS {Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName)}";
        command.ExecuteNonQuery();
    }

    public override async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await Dependencies.Connection.CloseAsync();
        ClearTargetPool();
        var (databaseName, adminConnectionString) = GetDatabaseAdministrationInfo();
        await using var connection = KingbaseConnectionFactory.Create(adminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS {Dependencies.SqlGenerationHelper.DelimitIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public override bool HasTables()
    {
        var connection = Dependencies.Connection.DbConnection;
        var shouldClose = connection.State == ConnectionState.Closed;

        try
        {
            if (shouldClose)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = HasTablesSql;
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    public override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
    {
        var connection = Dependencies.Connection.DbConnection;
        var shouldClose = connection.State == ConnectionState.Closed;

        try
        {
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = HasTablesSql;
            return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private const string HasTablesSql =
        """
        SELECT COUNT(*)
        FROM sys_class AS c
        JOIN sys_namespace AS n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r', 'p')
          AND n.nspname NOT IN ('sys_catalog', 'pg_catalog', 'information_schema')
          AND n.nspname NOT LIKE 'sys%'
        """;

    private (string DatabaseName, string AdminConnectionString) GetDatabaseAdministrationInfo()
    {
        var connectionString = Dependencies.Connection.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("A connection string is required to create or delete a KingbaseES database.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var databaseKey = builder.Keys.Cast<string>()
            .FirstOrDefault(key => key.Equals("Database", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase));

        if (databaseKey is null || builder[databaseKey] is not { } databaseValue || string.IsNullOrWhiteSpace(Convert.ToString(databaseValue)))
        {
            throw new InvalidOperationException("The KingbaseES connection string must contain a Database value for create or delete operations.");
        }

        var databaseName = Convert.ToString(databaseValue)!;
        var adminDatabase = Dependencies.ContextOptions.FindExtension<KingbaseOptionsExtension>()?.AdminDatabase ?? "template1";
        builder[databaseKey] = adminDatabase;
        return (databaseName, builder.ConnectionString);
    }

    private void ClearTargetPool()
    {
        if (Dependencies.Connection.DbConnection is KdbndpConnection connection)
        {
            KdbndpConnection.ClearPool(connection);
        }
    }
}
