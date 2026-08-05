using Kingbase.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kingbase.EntityFrameworkCore.Design.Internal;

public sealed class KingbaseDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddEntityFrameworkKingbase();
        serviceCollection.TryAddSingleton<IDatabaseModelFactory, KingbaseDatabaseModelFactory>();
        serviceCollection.TryAddSingleton<IProviderConfigurationCodeGenerator, KingbaseProviderCodeGenerator>();
        serviceCollection.TryAddSingleton<IAnnotationCodeGenerator>(provider =>
            new KingbaseAnnotationCodeGenerator(new AnnotationCodeGeneratorDependencies(
                provider.GetRequiredService<IRelationalTypeMappingSource>())));
        serviceCollection.TryAddSingleton<ICSharpRuntimeAnnotationCodeGenerator>(provider =>
            new KingbaseCSharpRuntimeAnnotationCodeGenerator(
                new CSharpRuntimeAnnotationCodeGeneratorDependencies(provider.GetRequiredService<ICSharpHelper>()),
                new RelationalCSharpRuntimeAnnotationCodeGeneratorDependencies()));
    }
}
