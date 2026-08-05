using System.Threading;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AbpBookStore.TestBase;

/// <summary>
/// Boots the real ABP module (AbpApplicationFactory) once per test collection,
/// targeting the configured Kingbase database. When no connection is
/// configured the fixture does not boot the module and the database tests
/// early-return (skip) — so `dotnet test` stays green offline.
///
/// Because `dotnet test` on a solution runs test assemblies in parallel, and
/// several assemblies share the same dedicated test database (dropping and
/// recreating its tables), a named cross-process gate serializes the whole
/// DB-touching lifetime of each fixture. Without it, one assembly's
/// ResetSchemaAsync can drop the tables another assembly is mid-test on.
///
/// A named Semaphore (not a Mutex) is used because the test lifecycle is
/// async: InitializeAsync and DisposeAsync can run on different threads, and
/// Mutex.ReleaseMutex is thread-affine ("Object synchronization method was
/// called from an unsynchronized block"). A Semaphore has no owner thread, so
/// acquire/release from different threads is safe and it still serializes
/// across processes.
/// </summary>
public sealed class AbpAppFixture : IAsyncLifetime
{
    private const string DatabaseGateName = "AbpBookStore.Kingbase.Database";

    private Semaphore? _databaseGate;

    public string? ConnectionString { get; private set; }

    public IAbpApplicationWithInternalServiceProvider? Application { get; private set; }

    public IServiceProvider? ServiceProvider => Application?.ServiceProvider;

    public bool HasDatabase => ConnectionString is not null;

    public async Task InitializeAsync()
    {
        ConnectionString = await TestDatabase.ResolveAsync();
        if (ConnectionString is null)
        {
            return;
        }

        // Serialize with other test assemblies that share this database. Held
        // for the whole fixture lifetime so no other assembly drops the tables
        // while this one is mid-test. Released in DisposeAsync (or here if the
        // boot below throws).
        _databaseGate = new Semaphore(initialCount: 1, maximumCount: 1, DatabaseGateName);
        _databaseGate.WaitOne(TimeSpan.FromMinutes(10));
        try
        {
            // The EF module resolves the connection from the KINGBASE_TEST_CONNECTION
            // environment variable (config-first, then env). Setting it before the
            // module boots keeps the bootstrap independent of the ABP configuration
            // builder surface.
            Environment.SetEnvironmentVariable(TestDatabase.TestConnectionVariable, ConnectionString);

            // ABP's EF Core layer relies on property injection (LazyServiceProvider)
            // for the DbContext, so the app must boot on Autofac — the built-in MS DI
            // does not property-inject and the DbContext would NRE on audit/UOW.
            Application = await AbpApplicationFactory.CreateAsync<AbpBookStoreTestBaseModule>(
                options => options.UseAutofac());
            await Application.InitializeAsync();
        }
        catch
        {
            ReleaseDatabaseGate();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Application is not null)
            {
                await Application.ShutdownAsync();
            }
        }
        finally
        {
            ReleaseDatabaseGate();
        }
    }

    private void ReleaseDatabaseGate()
    {
        _databaseGate?.Release(); // return the single count (Semaphore is thread-agnostic)
        _databaseGate?.Dispose();
        _databaseGate = null;
    }
}
