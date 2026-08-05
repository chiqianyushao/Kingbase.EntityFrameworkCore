using System.Data.Common;
using Kdbndp;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

internal static class KingbaseConnectionFactory
{
    public static DbConnection Create(string? connectionString)
        => new KdbndpConnection(connectionString ?? string.Empty);
}
