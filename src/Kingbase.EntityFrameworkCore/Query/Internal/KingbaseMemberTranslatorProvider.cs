using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseMemberTranslatorProvider : RelationalMemberTranslatorProvider
{
    public KingbaseMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        AddTranslators(
        [
            new KingbaseStringMemberTranslator(dependencies.SqlExpressionFactory),
            new KingbaseDateTimeMemberTranslator(dependencies.SqlExpressionFactory)
        ]);
    }
}
