using AbpBookStore.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Re-verifies the report's §10 (save, tracking and batch operations) THROUGH
/// the real ABP stack: Add/AddRange/Update/UpdateRange/Remove/RemoveRange,
/// SaveChanges (sync + async), DetectChanges, Attach/AttachRange, batch command
/// execution, and ExecuteDelete/ExecuteUpdate (sync + async).
/// </summary>
public sealed class CapabilitySaveTrackingTests : CapabilityTestBase
{
    public CapabilitySaveTrackingTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Add_and_AddRange_save_and_read_back()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        await InDbContextAsync(async context =>
        {
            // Add (single)
            context.Books.Add(NewBook("Single Add", BookType.Adventure, new DateTime(2001, 1, 1), 1));
            Assert.Equal(1, await context.SaveChangesAsync());

            // AddRange (many roots)
            context.Books.AddRange(
                NewBook("Range One", BookType.Biography, new DateTime(2002, 1, 1), 2),
                NewBook("Range Two", BookType.Dystopia, new DateTime(2003, 1, 1), 3));
            Assert.Equal(2, await context.SaveChangesAsync());

            Assert.Equal(3, await context.Books.CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task Update_and_UpdateRange_save_changes()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        var bookOne = NewBook("Update One", BookType.Adventure, new DateTime(2001, 1, 1), 1);
        var bookTwo = NewBook("Update Two", BookType.Biography, new DateTime(2002, 1, 1), 2);
        await SeedBooksAsync(bookOne, bookTwo);

        await InDbContextAsync(async context =>
        {
            // Update (single tracked entity)
            var first = await context.Books.SingleAsync(b => b.Id == bookOne.Id);
            first.Price = 11;
            Assert.Equal(1, await context.SaveChangesAsync());

            // UpdateRange (two entities in one batch)
            var second = await context.Books.SingleAsync(b => b.Id == bookTwo.Id);
            second.Price = 22;
            context.Update(second);
            Assert.Equal(1, await context.SaveChangesAsync());

            Assert.Equal(11, (await context.Books.SingleAsync(b => b.Id == bookOne.Id)).Price);
            Assert.Equal(22, (await context.Books.SingleAsync(b => b.Id == bookTwo.Id)).Price);
            return 0;
        });
    }

    [Fact]
    public async Task Remove_and_RemoveRange_execute()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        var bookOne = NewBook("Remove One", BookType.Adventure, new DateTime(2001, 1, 1), 1);
        var bookTwo = NewBook("Remove Two", BookType.Biography, new DateTime(2002, 1, 1), 2);
        await SeedBooksAsync(bookOne, bookTwo);

        await InDbContextAsync(async context =>
        {
            var first = await context.Books.SingleAsync(b => b.Id == bookOne.Id);
            context.Remove(first);
            Assert.Equal(1, await context.SaveChangesAsync());

            var second = await context.Books.SingleAsync(b => b.Id == bookTwo.Id);
            context.RemoveRange(second);
            Assert.Equal(1, await context.SaveChangesAsync());

            Assert.Equal(0, await context.Books.CountAsync());
            // Books are soft-deleted (ABP), so the rows remain physically:
            Assert.Equal(2, await context.Books.IgnoreQueryFilters().CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task SaveChanges_sync_and_async_with_batch_commands()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        await InDbContextAsync(async context =>
        {
            // Multiple commands in a single SaveChanges batch (2 inserts, 1 in one batch).
            context.Books.Add(NewBook("Batch A", BookType.Adventure, new DateTime(2001, 1, 1), 1));
            context.Books.Add(NewBook("Batch B", BookType.Biography, new DateTime(2002, 1, 1), 2));
            Assert.Equal(2, await context.SaveChangesAsync());   // async

            context.Books.Add(NewBook("Batch C", BookType.Dystopia, new DateTime(2003, 1, 1), 3));
            Assert.Equal(1, context.SaveChanges());              // sync

            Assert.Equal(3, await context.Books.CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task DetectChanges_finds_mutations_when_auto_detect_is_off()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        var book = NewBook("Detect Changes", BookType.Adventure, new DateTime(2001, 1, 1), 1);
        await SeedBooksAsync(book);

        await InDbContextAsync(async context =>
        {
            context.ChangeTracker.AutoDetectChangesEnabled = false;
            var loaded = await context.Books.SingleAsync(b => b.Id == book.Id);
            loaded.Price = 42;

            Assert.Equal(EntityState.Unchanged, context.Entry(loaded).State); // not yet detected
            context.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, context.Entry(loaded).State);
            Assert.Equal(1, await context.SaveChangesAsync());

            Assert.Equal(42, (await context.Books.SingleAsync(b => b.Id == book.Id)).Price);
            return 0;
        });
    }

    [Fact]
    public async Task Attach_and_AttachRange_track_as_unchanged_then_update()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        var book = NewBook("Attach", BookType.Adventure, new DateTime(2001, 1, 1), 1);
        await SeedBooksAsync(book);

        await InDbContextAsync(async context =>
        {
            // ABP sets a random ConcurrencyStamp in the entity constructor, so a
            // freshly-created detached instance never matches the stored row. Copy
            // the stored stamp onto the detached instance first, otherwise the
            // UPDATE's `WHERE ... AND ConcurrencyStamp = @original` matches 0 rows.
            var storedStamp = await context.Books
                .IgnoreQueryFilters()
                .Where(b => b.Id == book.Id)
                .Select(b => b.ConcurrencyStamp)
                .SingleAsync();

            // Attach a detached instance with an existing key -> Unchanged
            var detached = NewBook("Attach", BookType.Adventure, new DateTime(2001, 1, 1), 1, book.Id);
            detached.ConcurrencyStamp = storedStamp;
            context.Attach(detached);
            Assert.Equal(EntityState.Unchanged, context.Entry(detached).State);

            detached.Price = 55;
            context.Entry(detached).State = EntityState.Modified;
            Assert.Equal(1, await context.SaveChangesAsync());

            Assert.Equal(55, (await context.Books.SingleAsync(b => b.Id == book.Id)).Price);
            return 0;
        });

        // AttachRange with multiple existing instances — done in a fresh context
        // so the instances from the block above are not already tracked.
        var other = NewBook("Attach Other", BookType.Biography, new DateTime(2002, 1, 1), 2);
        await SeedBooksAsync(other);
        await InDbContextAsync(async context =>
        {
            var otherStamp = await context.Books.IgnoreQueryFilters()
                .Where(b => b.Id == other.Id).Select(b => b.ConcurrencyStamp).SingleAsync();
            var second = NewBook("Attach Other", BookType.Biography, new DateTime(2002, 1, 1), 2, other.Id);
            second.ConcurrencyStamp = otherStamp;
            context.AttachRange(second);
            Assert.Equal(EntityState.Unchanged, context.Entry(second).State);
            return 0;
        });
    }

    [Fact]
    public async Task ExecuteDelete_and_ExecuteUpdate_sync_and_async()
    {
        if (!HasDatabase) return;
        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);
        await SeedBooksAsync(
            NewBook("ED One", BookType.Adventure, new DateTime(2001, 1, 1), 1),
            NewBook("ED Two", BookType.Biography, new DateTime(2002, 1, 1), 2),
            NewBook("ED Three", BookType.Dystopia, new DateTime(2003, 1, 1), 3));

        await InDbContextAsync(async context =>
        {
            // ExecuteUpdate (async then sync)
            Assert.Equal(3, await context.Books.ExecuteUpdateAsync(s => s.SetProperty(b => b.Price, 99)));
            Assert.Equal(99, await context.Books.MaxAsync(b => b.Price));

            Assert.Equal(3, context.Books.ExecuteUpdate(s => s.SetProperty(b => b.Price, 1)));
            Assert.Equal(1, await context.Books.MaxAsync(b => b.Price));

            // ExecuteDelete (async then sync) — all three rows now have Price=1
            Assert.Equal(3, await context.Books.Where(b => b.Price <= 1).ExecuteDeleteAsync());
            Assert.Equal(0, context.Books.ExecuteDelete());
            Assert.Equal(0, await context.Books.CountAsync());
            return 0;
        });
    }
}
