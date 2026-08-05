using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQueryTranslationPreprocessorFactory(
    QueryTranslationPreprocessorDependencies dependencies,
    RelationalQueryTranslationPreprocessorDependencies relationalDependencies)
    : IQueryTranslationPreprocessorFactory
{
    public QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
        => new KingbaseQueryTranslationPreprocessor(
            dependencies,
            relationalDependencies,
            queryCompilationContext);
}
