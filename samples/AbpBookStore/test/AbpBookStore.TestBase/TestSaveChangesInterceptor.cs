using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AbpBookStore.TestBase;

/// <summary>
/// Counts SaveChanges invocations to prove the EF Core save-changes
/// interceptor pipeline runs through ABP + the Kingbase provider.
///
/// The tests save via the async path, and EF Core's SaveChangesInterceptor
/// does NOT forward SavingChangesAsync to SavingChanges — override both.
/// </summary>
public sealed class TestSaveChangesInterceptor : SaveChangesInterceptor
{
    public int SavingCalls { get; private set; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        SavingCalls++;
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SavingCalls++;
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
