using Volo.Abp.Modularity;

namespace AbpBookStore.Application.Contracts;

[DependsOn(typeof(AbpBookStoreDomainSharedModule))]
public class AbpBookStoreApplicationContractsModule : AbpModule
{
}
