using AbpBookStore.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AbpBookStore.EntityFrameworkCore.Tests;

/// <summary>
/// Offline model/DDL validation — never touches the database. GenerateCreateScript
/// and relational type mappings are computed client-side, so these tests run
/// without KINGBASE_TEST_CONNECTION and are the earliest possible signal of a
/// provider problem with the ABP BookStore model (ExtraProperties jsonb DDL,
/// Guid keys, composite m2m keys, cascade FKs).
/// </summary>
public sealed class ModelAndDdlTests(ITestOutputHelper output)
{
    private const string PlaceholderConnection =
        "Server=127.0.0.1;Port=54321;Database=abp_bookstore_placeholder;UID=system;PWD=changeit;SSL Mode=Disable";

    private static BookStoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseKdbndp(PlaceholderConnection, kingbase => kingbase.SetOracleCompatibilityMode())
            .Options;

        return new BookStoreDbContext(options);
    }

    [Fact]
    public void Create_script_contains_abp_bookstore_tables()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE", script);
        Assert.Contains("\"Books\"", script);
        Assert.Contains("\"Authors\"", script);
        Assert.Contains("\"BookAuthors\"", script);
    }

    [Fact]
    public void Create_script_contains_abp_convention_columns_and_m2m_keys()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();

        // ABP convention columns on the audited/soft-delete entities.
        Assert.Contains("\"ExtraProperties\"", script);
        Assert.Contains("\"ConcurrencyStamp\"", script);
        Assert.Contains("\"IsDeleted\"", script);
        Assert.Contains("\"CreationTime\"", script);
        Assert.Contains("\"DeletionTime\"", script);

        // Book fields.
        Assert.Contains("\"PublishDate\"", script);
        Assert.Contains("\"Type\"", script);

        // BookAuthor composite key + cascade foreign keys + Book.Name index.
        Assert.Contains("\"BookId\"", script);
        Assert.Contains("\"AuthorId\"", script);
        Assert.Contains("PRIMARY KEY", script);
        Assert.Contains("CASCADE", script);
        Assert.Contains("CREATE INDEX", script);
    }

    [Fact]
    public void Books_table_uses_uuid_not_identity_for_guid_key()
    {
        using var context = CreateContext();
        var script = context.Database.GenerateCreateScript();
        var booksTable = ExtractCreateTable(script, "Books");

        output.WriteLine($"--- CREATE TABLE \"Books\" ---{Environment.NewLine}{booksTable}");

        Assert.Contains("\"Id\" uuid NOT NULL", booksTable);
        Assert.DoesNotContain("IDENTITY", booksTable);
    }

    [Fact]
    public void Relational_store_types_are_recorded_for_abp_surface()
    {
        using var context = CreateContext();
        _ = context.Database.GenerateCreateScript(); // forces model finalization

        var bookType = context.Model.FindEntityType(typeof(Book))!;

        var columns = new (string Name, IProperty Property)[]
        {
            ("Id", bookType.FindProperty(nameof(Book.Id))!),
            ("Name", bookType.FindProperty(nameof(Book.Name))!),
            ("Type(enum)", bookType.FindProperty(nameof(Book.Type))!),
            ("PublishDate", bookType.FindProperty(nameof(Book.PublishDate))!),
            ("Price(float)", bookType.FindProperty(nameof(Book.Price))!),
            ("ExtraProperties", bookType.FindProperty("ExtraProperties")!),
            ("ConcurrencyStamp", bookType.FindProperty("ConcurrencyStamp")!),
            ("IsDeleted", bookType.FindProperty("IsDeleted")!)
        };

        foreach (var (name, property) in columns)
        {
            var storeType = property.GetRelationalTypeMapping()?.StoreType ?? "<unknown>";
            output.WriteLine($"Book.{name} -> {storeType} (CLR {property.ClrType.Name})");
        }

        // ExtraProperties was a flagged risk area. Finding: ABP 10.6 maps
        // ExtraProperties through a value converter to a JSON *string* column
        // (text), not jsonb — so the provider's scalar JsonElement->jsonb
        // support is not even exercised here. Assert text and record it.
        var extraPropertiesStoreType =
            bookType.FindProperty("ExtraProperties")!.GetRelationalTypeMapping()!.StoreType;

        Assert.Equal("text", extraPropertiesStoreType);
    }

    private static string ExtractCreateTable(string script, string tableName)
    {
        var marker = $"CREATE TABLE \"{tableName}\"";
        var start = script.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0, $"Expected '{marker}' in generated script.");

        var end = script.IndexOf(';', start);
        return script[start..end];
    }
}
