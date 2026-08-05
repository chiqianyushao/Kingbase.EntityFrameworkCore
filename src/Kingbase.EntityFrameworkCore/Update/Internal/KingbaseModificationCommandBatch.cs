using Microsoft.EntityFrameworkCore.Update;

namespace Kingbase.EntityFrameworkCore.Update.Internal;

public sealed class KingbaseModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies)
    : AffectedCountModificationCommandBatch(dependencies)
{
}
