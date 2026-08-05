using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AbpBookStore.EntityFrameworkCore;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` / `dbcontext optimize`
/// work without booting the ABP module. Migrations generation never connects
/// to the database, so a placeholder connection string is safe here.
/// </summary>
public class BookStoreDbContextFactory : IDesignTimeDbContextFactory<BookStoreDbContext>
{
    public BookStoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KINGBASE_TEST_CONNECTION")
            ?? "Server=127.0.0.1;Port=54321;Database=abp_bookstore_dev;UID=system;PWD=changeit;SSL Mode=Disable";

        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseKdbndp(connectionString, kingbase => kingbase.SetOracleCompatibilityMode())
            .Options;

        return new BookStoreDbContext(options);
    }
}
