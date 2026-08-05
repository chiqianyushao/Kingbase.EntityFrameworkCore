using AbpBookStore.Application.Contracts;
using AbpBookStore.Domain;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AbpBookStore.Application;

[DependsOn(
    typeof(AbpBookStoreDomainModule),
    typeof(AbpBookStoreApplicationContractsModule),
    typeof(AbpAutoMapperModule))]
public class AbpBookStoreApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AbpBookStoreApplicationModule>();
        });
    }
}
