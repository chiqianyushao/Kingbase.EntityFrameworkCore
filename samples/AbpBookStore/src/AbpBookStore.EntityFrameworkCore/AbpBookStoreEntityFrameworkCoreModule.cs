using AbpBookStore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.DependencyInjection;
using Volo.Abp.Modularity;

namespace AbpBookStore.EntityFrameworkCore;

[DependsOn(
    typeof(AbpBookStoreDomainModule),
    typeof(AbpEntityFrameworkCoreModule))]
public class AbpBookStoreEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<BookStoreDbContext>(options =>
        {
            options.AddDefaultRepositories();
        });

        Configure<AbpDbContextOptions>(options =>
        {
            // ABP 10 stores a single Configure<T> action per DbContext type and
            // DbContextOptionsFactory.Create applies it to the options that are
            // handed to the context — so the provider must be set here, and no
            // other module may call Configure<BookStoreDbContext> again.
            options.Configure<BookStoreDbContext>(dbContext =>
            {
                dbContext.DbContextOptions.UseKdbndp(
                    ResolveConnectionString(context.Services),
                    kingbase => kingbase.SetOracleCompatibilityMode());
            });
        });
    }

    /// <summary>
    /// Connection resolution order:
    ///   1. IConfiguration["ConnectionStrings:Default"] (set by the test module / host)
    ///   2. KINGBASE_TEST_CONNECTION environment variable
    ///   3. placeholder (so the module still boots for offline model/DDL tests)
    /// </summary>
    private static string ResolveConnectionString(IServiceCollection services)
    {
        var fromConfiguration = services.GetConfiguration()["ConnectionStrings:Default"];
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
        {
            return fromConfiguration;
        }

        return Environment.GetEnvironmentVariable("KINGBASE_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=54321;Database=abp_bookstore_placeholder;UID=system;PWD=changeit;SSL Mode=Disable";
    }
}
