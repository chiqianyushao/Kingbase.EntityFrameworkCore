using Volo.Abp.Modularity;

namespace AbpBookStore.Domain;

[DependsOn(typeof(AbpBookStoreDomainSharedModule))]
public class AbpBookStoreDomainModule : AbpModule
{
}
