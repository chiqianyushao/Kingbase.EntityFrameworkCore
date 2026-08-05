using Kingbase.EntityFrameworkCore.Infrastructure.Internal;
using Kingbase.EntityFrameworkCore.Metadata.Conventions;
using Kingbase.EntityFrameworkCore.Migrations.Internal;
using Kingbase.EntityFrameworkCore.Query.Internal;
using Kingbase.EntityFrameworkCore.Storage.Internal;
using Kingbase.EntityFrameworkCore.Update.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Kingbase.EntityFrameworkCore.Extensions;

public static class KingbaseServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkKingbase(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        new EntityFrameworkRelationalServicesBuilder(services)
            .TryAdd<LoggingDefinitions, KingbaseLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<KingbaseOptionsExtension>>()
            .TryAdd<IProviderConventionSetBuilder, KingbaseConventionSetBuilder>()
            .TryAdd<IMethodCallTranslatorProvider, KingbaseMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, KingbaseMemberTranslatorProvider>()
            .TryAdd<IQueryTranslationPreprocessorFactory, KingbaseQueryTranslationPreprocessorFactory>()
            .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, KingbaseSqlTranslatingExpressionVisitorFactory>()
            .TryAdd<IQueryCompilationContextFactory, KingbaseQueryCompilationContextFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, KingbaseQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IQuerySqlGeneratorFactory, KingbaseQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalConnection, KingbaseRelationalConnection>()
            .TryAdd<IRelationalDatabaseCreator, KingbaseDatabaseCreator>()
            .TryAdd<IExecutionStrategyFactory, KingbaseExecutionStrategyFactory>()
            .TryAdd<IMigrationsSqlGenerator, KingbaseMigrationsSqlGenerator>()
            .TryAdd<IHistoryRepository, KingbaseHistoryRepository>()
            .TryAdd<IRelationalTypeMappingSource, KingbaseTypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, KingbaseSqlGenerationHelper>()
            .TryAdd<IUpdateSqlGenerator, KingbaseUpdateSqlGenerator>()
            .TryAdd<IModificationCommandBatchFactory, KingbaseModificationCommandBatchFactory>()
            .TryAddCoreServices();

        return services;
    }
}
