using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Kingbase.EntityFrameworkCore.Metadata.Conventions;

public sealed class KingbaseConventionSetBuilder(
    ProviderConventionSetBuilderDependencies dependencies,
    RelationalConventionSetBuilderDependencies relationalDependencies)
    : RelationalConventionSetBuilder(dependencies, relationalDependencies)
{
    public override ConventionSet CreateConventionSet()
    {
        var conventionSet = base.CreateConventionSet();
        conventionSet.ModelInitializedConventions.Add(
            new RelationalMaxIdentifierLengthConvention(63, Dependencies, RelationalDependencies));
        return conventionSet;
    }
}
