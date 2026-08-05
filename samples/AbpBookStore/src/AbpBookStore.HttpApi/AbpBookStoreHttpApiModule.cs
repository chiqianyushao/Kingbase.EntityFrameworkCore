using AbpBookStore.Application.Contracts;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace AbpBookStore.HttpApi;

[DependsOn(
    typeof(AbpBookStoreApplicationContractsModule),
    typeof(AbpAspNetCoreMvcModule))]
public class AbpBookStoreHttpApiModule : AbpModule
{
}
