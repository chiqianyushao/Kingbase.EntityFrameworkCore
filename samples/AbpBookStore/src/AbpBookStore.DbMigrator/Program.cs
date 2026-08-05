using AbpBookStore;
using AbpBookStore.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Data;

var connectionString = Environment.GetEnvironmentVariable("KINGBASE_TEST_CONNECTION")
    ?? throw new InvalidOperationException(
        "Set the KINGBASE_TEST_CONNECTION environment variable before running the migrator.");

Environment.SetEnvironmentVariable("KINGBASE_TEST_CONNECTION", connectionString);

using var application = await AbpApplicationFactory.CreateAsync<AbpBookStoreDbMigratorModule>(
    options => options.UseAutofac());
await application.InitializeAsync();

using (var scope = application.ServiceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
    await dbContext.Database.MigrateAsync();

    var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await dataSeeder.SeedAsync();
}

Console.WriteLine("Migrations applied and data seeded successfully.");
await application.ShutdownAsync();
