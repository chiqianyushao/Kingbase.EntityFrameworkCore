using AbpBookStore.Authors;
using AbpBookStore.Books;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace AbpBookStore.EntityFrameworkCore;

/// <summary>
/// DbContext mirroring the ABP BookStore tutorial. ConfigureByConvention()
/// adds the audit/soft-delete/concurrency-stamp/extra-properties mapping and
/// the ABP soft-delete global query filter on top of the BaseEntity types.
/// </summary>
public class BookStoreDbContext : AbpDbContext<BookStoreDbContext>
{
    public DbSet<Book> Books { get; set; } = default!;

    public DbSet<Author> Authors { get; set; } = default!;

    public DbSet<BookAuthor> BookAuthors { get; set; } = default!;

    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Book>(b =>
        {
            b.ToTable("Books");
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(BookConsts.MaxNameLength);
            b.HasIndex(x => x.Name);
        });

        builder.Entity<Author>(a =>
        {
            a.ToTable("Authors");
            a.ConfigureByConvention();

            a.Property(x => x.Name).IsRequired().HasMaxLength(AuthorConsts.MaxNameLength);
            a.HasIndex(x => x.Name);
        });

        builder.Entity<BookAuthor>(ba =>
        {
            ba.ToTable("BookAuthors");
            ba.ConfigureByConvention();

            ba.HasKey(x => new { x.BookId, x.AuthorId });

            ba.HasOne(x => x.Book)
                .WithMany(x => x.BookAuthors)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            ba.HasOne(x => x.Author)
                .WithMany(x => x.Books)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
