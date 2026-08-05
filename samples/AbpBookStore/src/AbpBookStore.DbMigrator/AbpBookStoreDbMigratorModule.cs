using AbpBookStore.Application;
using AbpBookStore.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpBookStore;

[DependsOn(
    typeof(AbpBookStoreEntityFrameworkCoreModule),
    typeof(AbpBookStoreApplicationModule),
    typeof(AbpAutofacModule))]
public class AbpBookStoreDbMigratorModule : AbpModule
{
}
