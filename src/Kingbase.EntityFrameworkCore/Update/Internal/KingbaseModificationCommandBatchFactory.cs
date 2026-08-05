using Microsoft.EntityFrameworkCore.Update;

namespace Kingbase.EntityFrameworkCore.Update.Internal;

public sealed class KingbaseModificationCommandBatchFactory(ModificationCommandBatchFactoryDependencies dependencies)
    : IModificationCommandBatchFactory
{
    public ModificationCommandBatch Create()
        => new KingbaseModificationCommandBatch(dependencies);
}
