using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Storage.Internal;

public sealed class KingbaseSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
    : RelationalSqlGenerationHelper(dependencies)
{
}
