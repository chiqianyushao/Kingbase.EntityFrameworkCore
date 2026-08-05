namespace AbpBookStore.TestBase;

/// <summary>
/// Non-parallel collection so the real-Kingbase tests serialize access to the
/// shared test database and share one booted ABP application.
/// </summary>
[CollectionDefinition("Abp book store", DisableParallelization = true)]
public sealed class AbpBookStoreCollection : ICollectionFixture<AbpAppFixture>
{
}
