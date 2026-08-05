using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies)
    : IQuerySqlGeneratorFactory
{
    public QuerySqlGenerator Create()
        => new KingbaseQuerySqlGenerator(dependencies);
}
