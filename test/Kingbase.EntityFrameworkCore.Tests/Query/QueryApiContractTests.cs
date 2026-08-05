using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Kingbase.EntityFrameworkCore.Tests.Query;

public sealed class QueryApiContractTests
{
    private static readonly HashSet<string> ExpectedQueryableMethods =
    [
        "Aggregate", "AggregateBy", "All", "Any", "Append", "AsQueryable", "Average", "Cast", "Chunk", "Concat",
        "Contains", "Count", "CountBy", "DefaultIfEmpty", "Distinct", "DistinctBy", "ElementAt", "ElementAtOrDefault",
        "Except", "ExceptBy", "First", "FirstOrDefault", "GroupBy", "GroupJoin", "Index", "Intersect", "IntersectBy",
        "Join", "Last", "LastOrDefault", "LeftJoin", "LongCount", "Max", "MaxBy", "Min", "MinBy", "OfType", "Order",
        "OrderBy", "OrderByDescending", "OrderDescending", "Prepend", "Reverse", "RightJoin", "Select", "SelectMany",
        "SequenceEqual", "Shuffle", "Single", "SingleOrDefault", "Skip", "SkipLast", "SkipWhile", "Sum", "Take",
        "TakeLast", "TakeWhile", "ThenBy", "ThenByDescending", "Union", "UnionBy", "Where", "Zip"
    ];

    private static readonly HashSet<string> ExpectedEfQueryableExtensions =
    [
        "AllAsync", "AnyAsync", "AsAsyncEnumerable", "AsNoTracking", "AsNoTrackingWithIdentityResolution", "AsTracking",
        "AverageAsync", "ContainsAsync", "CountAsync", "ElementAtAsync", "ElementAtOrDefaultAsync", "ExecuteDelete",
        "ExecuteDeleteAsync", "ExecuteUpdate", "ExecuteUpdateAsync", "FirstAsync", "FirstOrDefaultAsync", "ForEachAsync",
        "IgnoreAutoIncludes", "IgnoreQueryFilters", "Include", "LastAsync", "LastOrDefaultAsync", "Load", "LoadAsync",
        "LongCountAsync", "MaxAsync", "MinAsync", "SingleAsync", "SingleOrDefaultAsync", "SumAsync", "TagWith",
        "TagWithCallSite", "ThenInclude", "ToArrayAsync", "ToDictionaryAsync", "ToHashSetAsync", "ToListAsync", "ToQueryString"
    ];

    private static readonly HashSet<string> ExpectedRelationalQueryableExtensions =
    [
        "AsSingleQuery", "AsSplitQuery", "CreateDbCommand", "FromSql", "FromSqlInterpolated", "FromSqlRaw"
    ];

    private static readonly HashSet<string> EfCoreRelationalServerOperators =
    [
        "All", "Any", "Average", "Cast", "Concat", "Contains", "Count", "DefaultIfEmpty", "Distinct", "ElementAt",
        "ElementAtOrDefault", "Except", "First", "FirstOrDefault", "GroupBy", "GroupJoin", "Intersect", "Join", "Last",
        "LastOrDefault", "LeftJoin", "LongCount", "Max", "Min", "OfType", "Order", "OrderBy", "OrderByDescending",
        "OrderDescending", "Reverse", "RightJoin", "Select", "SelectMany", "Single", "SingleOrDefault", "Skip", "Sum",
        "Take", "ThenBy", "ThenByDescending", "Union", "Where"
    ];

    private static readonly HashSet<string> ClientOrIdentityOperators = ["AsQueryable"];

    private static readonly HashSet<string> NoEfCoreRelationalServerTranslation =
    [
        "Aggregate", "AggregateBy", "Append", "Chunk", "CountBy", "DistinctBy", "ExceptBy", "Index", "IntersectBy",
        "MaxBy", "MinBy", "Prepend", "SequenceEqual", "Shuffle", "SkipLast", "SkipWhile", "TakeLast", "TakeWhile",
        "UnionBy", "Zip"
    ];

    [Fact]
    public void Queryable_public_method_names_match_net10_contract()
        => Assert.Equal(ExpectedQueryableMethods, PublicMethodNames(typeof(Queryable)));

    [Fact]
    public void Ef_query_extension_names_match_efcore10_contract()
        => Assert.Equal(ExpectedEfQueryableExtensions, PublicMethodNames(typeof(EntityFrameworkQueryableExtensions)));

    [Fact]
    public void Relational_query_extension_names_match_efcore10_contract()
        => Assert.Equal(ExpectedRelationalQueryableExtensions, PublicMethodNames(typeof(RelationalQueryableExtensions)));

    [Fact]
    public void Every_queryable_name_has_an_explicit_server_semantics_classification()
    {
        var classified = EfCoreRelationalServerOperators
            .Concat(ClientOrIdentityOperators)
            .Concat(NoEfCoreRelationalServerTranslation)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ExpectedQueryableMethods, classified);
        Assert.Empty(EfCoreRelationalServerOperators.Intersect(NoEfCoreRelationalServerTranslation));
    }

    private static HashSet<string> PublicMethodNames(Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
}
