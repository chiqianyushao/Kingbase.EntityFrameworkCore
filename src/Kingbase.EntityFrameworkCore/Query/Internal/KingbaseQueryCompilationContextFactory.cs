using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryCompilationContextFactory(
    QueryCompilationContextDependencies dependencies,
    RelationalQueryCompilationContextDependencies relationalDependencies)
    : RelationalQueryCompilationContextFactory(dependencies, relationalDependencies)
{
    public override QueryCompilationContext Create(bool async)
        => new KingbaseQueryCompilationContext(Dependencies, RelationalDependencies, async);

    public override QueryCompilationContext CreatePrecompiled(bool async)
        => new KingbaseQueryCompilationContext(Dependencies, RelationalDependencies, async, precompiling: true);
}
