using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryCompilationContext : RelationalQueryCompilationContext
{
    public KingbaseQueryCompilationContext(
        QueryCompilationContextDependencies dependencies,
        RelationalQueryCompilationContextDependencies relationalDependencies,
        bool async,
        bool precompiling = false)
        : base(dependencies, relationalDependencies, async, precompiling)
    {
    }

    public override bool IsBuffering => true;
}
