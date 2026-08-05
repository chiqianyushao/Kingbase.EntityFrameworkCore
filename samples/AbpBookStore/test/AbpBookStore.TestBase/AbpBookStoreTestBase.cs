using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Uow;

namespace AbpBookStore.TestBase;

/// <summary>
/// Shared fixture + helpers for the real-Kingbase integration test classes.
///
/// Uses IClassFixture rather than a collection fixture: xunit v2 collection
/// definitions do not cross assemblies, and the test classes live in separate
/// assemblies from this one. Each test class therefore boots its own ABP
/// application (one per class). The non-parallel "Abp book store" collection
/// name still serializes the DB tests within each assembly.
/// </summary>
[Collection("Abp book store")]
public abstract class AbpBookStoreTestBase : IClassFixture<AbpAppFixture>
{
    protected AbpAppFixture Fixture { get; }

    protected ITestOutputHelper Output { get; }

    protected bool HasDatabase => Fixture.HasDatabase;

    protected AbpBookStoreTestBase(AbpAppFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
    }

    protected IServiceScope CreateScope()
        => Fixture.ServiceProvider!.CreateScope();

    protected static IUnitOfWorkManager UnitOfWorkManager(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

    protected static IUnitOfWork BeginUnitOfWork(IServiceScope scope)
        => UnitOfWorkManager(scope).Begin(new AbpUnitOfWorkOptions());
}
