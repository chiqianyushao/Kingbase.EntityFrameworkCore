using AbpBookStore.Authors;
using AbpBookStore.Books;
using Microsoft.Extensions.DependencyInjection;

namespace AbpBookStore.Application.Tests.Books;

/// <summary>
/// Exercises the real ABP application service stack (ApplicationService +
/// ObjectMapper/AutoMapper + IRepository + unit of work) against Kingbase.
/// </summary>
public sealed class BookAppServiceTests : AbpBookStoreTestBase
{
    public BookAppServiceTests(AbpAppFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Create_get_list_and_delete_via_app_service()
    {
        if (!HasDatabase)
        {
            return;
        }

        await TestDatabase.ResetSchemaAsync(Fixture.ConnectionString!);

        using var scope = CreateScope();
        using var uow = BeginUnitOfWork(scope);

        var bookAppService = scope.ServiceProvider.GetRequiredService<IBookAppService>();
        var authorAppService = scope.ServiceProvider.GetRequiredService<IAuthorAppService>();

        var author = await authorAppService.CreateAsync(new CreateUpdateAuthorDto
        {
            Name = "Isaac Asimov",
            BirthDate = new DateTime(1920, 1, 2),
            ShortBio = "American writer and professor of biochemistry."
        });

        var created = await bookAppService.CreateAsync(new CreateUpdateBookDto
        {
            Name = "Foundation",
            Type = BookType.ScienceFiction,
            PublishDate = new DateTime(1951, 1, 1),
            Price = 19.99f,
            AuthorIds = [author.Id]
        });

        Assert.Equal("Foundation", created.Name);
        Assert.Equal(BookType.ScienceFiction, created.Type);
        Assert.Single(created.Authors);
        Assert.Equal("Isaac Asimov", created.Authors[0].Name);

        var list = await bookAppService.GetListAsync(new GetBookListDto { MaxResultCount = 10 });
        Assert.Equal(1, list.TotalCount);
        Assert.Equal("Foundation", list.Items[0].Name);

        var updated = await bookAppService.UpdateAsync(created.Id, new CreateUpdateBookDto
        {
            Name = "Foundation (Revised)",
            Type = BookType.ScienceFiction,
            PublishDate = new DateTime(1951, 1, 1),
            Price = 21.99f,
            AuthorIds = [author.Id]
        });
        Assert.Equal("Foundation (Revised)", updated.Name);

        await bookAppService.DeleteAsync(created.Id);

        var afterDelete = await bookAppService.GetListAsync(new GetBookListDto { MaxResultCount = 10 });
        Assert.Equal(0, afterDelete.TotalCount);

        await uow.CompleteAsync();
    }
}
