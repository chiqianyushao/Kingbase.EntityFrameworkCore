using System.Net.Http.Json;
using AbpBookStore.Authors;
using AbpBookStore.Books;
using Volo.Abp.Application.Dtos;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

// 1. List books (paged).
Console.WriteLine("== GET /api/bookstore/books?MaxResultCount=10 ==");
var bookList = await http.GetFromJsonAsync<PagedResultDto<BookDto>>(
    "/api/bookstore/books?MaxResultCount=10");
foreach (var item in bookList?.Items ?? [])
{
    var authors = string.Join(", ", item.Authors.Select(a => a.Name));
    Console.WriteLine($"  {item.Name} [{item.Type}] {item.Price:C} — {authors}");
}

// 2. Create an author, then a book linked to it.
Console.WriteLine("== POST /api/bookstore/authors ==");
var authorResponse = await http.PostAsJsonAsync("/api/bookstore/authors", new CreateUpdateAuthorDto
{
    Name = "Frank Herbert",
    BirthDate = new DateTime(1920, 10, 8),
    ShortBio = "American science-fiction author."
});
authorResponse.EnsureSuccessStatusCode();
var createdAuthor = await authorResponse.Content.ReadFromJsonAsync<AuthorDto>();
Console.WriteLine($"  Created author: {createdAuthor!.Name} ({createdAuthor.Id})");

var bookResponse = await http.PostAsJsonAsync("/api/bookstore/books", new CreateUpdateBookDto
{
    Name = "Dune",
    Type = BookType.ScienceFiction,
    PublishDate = new DateTime(1965, 8, 1),
    Price = 24.99f,
    AuthorIds = [createdAuthor.Id]
});
bookResponse.EnsureSuccessStatusCode();
var createdBook = await bookResponse.Content.ReadFromJsonAsync<BookDto>();
Console.WriteLine($"  Created book: {createdBook!.Name} by {string.Join(", ", createdBook.Authors.Select(a => a.Name))}");

// 3. Delete it again.
Console.WriteLine("== DELETE the created book ==");
var deleted = await http.DeleteAsync($"/api/bookstore/books/{createdBook.Id}");
deleted.EnsureSuccessStatusCode();
Console.WriteLine("  Deleted.");
