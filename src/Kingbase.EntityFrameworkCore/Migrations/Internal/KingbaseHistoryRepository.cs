using System.Data;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kingbase.EntityFrameworkCore.Migrations.Internal;

public sealed class KingbaseHistoryRepository(HistoryRepositoryDependencies dependencies)
    : HistoryRepository(dependencies)
{
    private const long MigrationLockId = 742136531903L;

    public override LockReleaseBehavior LockReleaseBehavior => LockReleaseBehavior.Explicit;

    protected override string ExistsSql
        => TableSchema is null
            ? $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = {Literal(TableName)})"
            : $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = {Literal(TableSchema)} AND table_name = {Literal(TableName)})";

    protected override bool InterpretExistsResult(object? value)
        => Convert.ToBoolean(value);

    public override string GetCreateIfNotExistsScript()
        => GetCreateScript().Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ", StringComparison.Ordinal);

    public override string GetBeginIfNotExistsScript(string migrationId)
        => $"DO $EF$ BEGIN IF NOT EXISTS (SELECT 1 FROM {SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema)} WHERE {SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName)} = {Literal(migrationId)}) THEN{Environment.NewLine}";

    public override string GetBeginIfExistsScript(string migrationId)
        => $"DO $EF$ BEGIN IF EXISTS (SELECT 1 FROM {SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema)} WHERE {SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName)} = {Literal(migrationId)}) THEN{Environment.NewLine}";

    public override string GetEndIfScript()
        => $"{Environment.NewLine}END IF; END $EF${SqlGenerationHelper.StatementTerminator}{Environment.NewLine}";

    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        var connection = Dependencies.Connection.DbConnection;
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({MigrationLockId})";
        command.ExecuteNonQuery();
        return new KingbaseMigrationsDatabaseLock(this, connection, MigrationLockId);
    }

    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        var connection = Dependencies.Connection.DbConnection;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_lock({MigrationLockId})";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return new KingbaseMigrationsDatabaseLock(this, connection, MigrationLockId);
    }

    private static string Literal(string value)
        => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
