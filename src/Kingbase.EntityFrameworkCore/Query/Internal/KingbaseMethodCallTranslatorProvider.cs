using Microsoft.EntityFrameworkCore.Query;

namespace Kingbase.EntityFrameworkCore.Query.Internal;

public sealed class KingbaseMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    public KingbaseMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
        AddTranslators(
        [
            new KingbaseStringMethodTranslator(dependencies.SqlExpressionFactory),
            new KingbaseDateTimeMethodTranslator(dependencies.SqlExpressionFactory),
            new KingbaseMathTranslator(dependencies.SqlExpressionFactory),
            new KingbaseGuidAndRegexTranslator(dependencies.SqlExpressionFactory, dependencies.RelationalTypeMappingSource),
            new KingbaseAdvancedTypeMethodTranslator(dependencies.SqlExpressionFactory)
        ]);
    }
}
