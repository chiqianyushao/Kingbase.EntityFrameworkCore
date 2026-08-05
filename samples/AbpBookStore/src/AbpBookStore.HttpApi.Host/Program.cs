using AbpBookStore;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ABP's EF Core layer relies on property injection (LazyServiceProvider) for the
// DbContext, so the host must use Autofac.
builder.Host.UseAutofac();

// Bridge the configured connection string into the environment variable that
// AbpBookStoreEntityFrameworkCoreModule reads, so the host uses appsettings.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Environment.SetEnvironmentVariable("KINGBASE_TEST_CONNECTION", connectionString);
}

builder.Services.AddApplication<AbpBookStoreHttpApiHostModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
app.Run();
