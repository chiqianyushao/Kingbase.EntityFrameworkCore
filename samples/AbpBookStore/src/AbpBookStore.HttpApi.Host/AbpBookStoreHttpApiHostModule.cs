using AbpBookStore.Application;
using AbpBookStore.EntityFrameworkCore;
using AbpBookStore.HttpApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpBookStore;

[DependsOn(
    typeof(AbpBookStoreHttpApiModule),
    typeof(AbpBookStoreApplicationModule),
    typeof(AbpBookStoreEntityFrameworkCoreModule),
    typeof(AbpAutofacModule))]
public class AbpBookStoreHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSwaggerGen();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseRouting();
        app.UseAuthorization();
        app.UseConfiguredEndpoints();
    }
}
