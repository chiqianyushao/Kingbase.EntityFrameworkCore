using AbpBookStore.Application;
using AbpBookStore.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace AbpBookStore.TestBase;

[DependsOn(
    typeof(AbpBookStoreEntityFrameworkCoreModule),
    typeof(AbpBookStoreApplicationModule),
    typeof(AbpAutofacModule))]
public class AbpBookStoreTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register a real EF Core save-changes interceptor through the ABP DI
        // stack and attach it to the BookStoreDbContext, to verify the
        // interceptor pipeline survives ABP + the Kingbase provider.
        var saveChangesInterceptor = new TestSaveChangesInterceptor();
        context.Services.AddSingleton(saveChangesInterceptor);

        Configure<AbpDbContextOptions>(options =>
        {
            // Do NOT call Configure<BookStoreDbContext> here — ABP stores a
            // SINGLE Configure action per DbContext type (Dictionary<Type, object>)
            // and the EF module owns it for the provider. Use the additive
            // PreConfigure hook instead: it runs in DbContextOptionsFactory.Create
            // (before Configure) where LazyServiceProvider is available, unlike
            // ConfigureOnConfiguring which fires inside AbpDbContext.OnConfiguring
            // during construction — LazyServiceProvider is property-injected by
            // Autofac AFTER construction, so those actions never run.
            options.PreConfigure<BookStoreDbContext>(ctx =>
            {
                ctx.DbContextOptions.AddInterceptors(saveChangesInterceptor);
            });
        });
    }
}
