using System.Data.Common;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kingbase.EntityFrameworkCore.Migrations.Internal;

internal sealed class KingbaseMigrationsDatabaseLock(
    IHistoryRepository historyRepository,
    DbConnection connection,
    long lockId) : IMigrationsDatabaseLock
{
    private bool _released;

    public IHistoryRepository HistoryRepository { get; } = historyRepository;

    public void Dispose()
    {
        if (_released || connection.State != System.Data.ConnectionState.Open)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({lockId})";
        command.ExecuteNonQuery();
        _released = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_released || connection.State != System.Data.ConnectionState.Open)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT pg_advisory_unlock({lockId})";
        await command.ExecuteNonQueryAsync();
        _released = true;
    }
}
