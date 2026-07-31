using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Philobiblos.Application.Books.Dtos;
using Philobiblos.Application.Common;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class BookIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public BookIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await AuthenticateAsync();
    }

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsync("/api/auth/test-login", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateBook_returns_201_with_author_and_genre_summaries()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "The Book",
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        book.Should().NotBeNull();
        book!.Title.Should().Be("The Book");
        book.Author.Id.Should().Be(authorId);
        book.Author.Name.Should().Be("Author");
        book.Genre.Id.Should().Be(genreId);
        book.Genre.Name.Should().Be("Genre");
    }

    [Fact]
    public async Task CreateBook_with_valid_isbn_returns_201()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "The Book",
            authorId,
            genreId,
            isbn = "978-0-306-40615-7",
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        book!.Isbn.Should().Be("9780306406157");
    }

    [Fact]
    public async Task ListBooks_supports_combined_filters_and_sorting()
    {
        var authorA = await CreateAuthorAsync("Author A");
        var authorB = await CreateAuthorAsync("Author B");
        var genreA = await CreateGenreAsync("Genre A");
        var genreB = await CreateGenreAsync("Genre B");

        await CreateBookAsync("Alpha Book", authorA, genreA);
        await CreateBookAsync("Beta Book", authorA, genreB);
        await CreateBookAsync("Gamma Book", authorB, genreB);

        var response = await _client.GetAsync($"/api/books?title=book&authorId={authorA}&genreId={genreA}&sort=title&direction=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<BookResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Items.Should().ContainSingle(book => book.Title == "Alpha Book");
        envelope.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListBooks_returns_paged_envelope_with_total_count()
    {
        var author = await CreateAuthorAsync("Author");
        var genre = await CreateGenreAsync("Genre");
        await CreateBookAsync("Book A", author, genre, publishedYear: 2020);
        await CreateBookAsync("Book B", author, genre, publishedYear: 2021);
        await CreateBookAsync("Book C", author, genre, publishedYear: 2022);

        var response = await _client.GetAsync("/api/books?page=2&pageSize=2&sort=publishedYear&direction=desc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<BookResponse>>(JsonOptions);
        envelope!.Page.Should().Be(2);
        envelope.PageSize.Should().Be(2);
        envelope.TotalCount.Should().Be(3);
        envelope.Items.Should().ContainSingle(book => book.PublishedYear == 2020);
    }

    [Fact]
    public async Task GetBook_returns_200_with_author_and_genre()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var bookId = await CreateBookAsync("Book", authorId, genreId);

        var response = await _client.GetAsync($"/api/books/{bookId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        book!.Author.Name.Should().Be("Author");
        book.Genre.Name.Should().Be("Genre");
    }

    [Fact]
    public async Task GetBook_returns_404_when_missing()
    {
        var response = await _client.GetAsync($"/api/books/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBook_can_change_author_genre_and_title()
    {
        var author1 = await CreateAuthorAsync("Author One");
        var genre1 = await CreateGenreAsync("Genre One");
        var author2 = await CreateAuthorAsync("Author Two");
        var genre2 = await CreateGenreAsync("Genre Two");
        var bookId = await CreateBookAsync("Original", author1, genre1);

        var response = await _client.PutAsJsonAsync($"/api/books/{bookId}", new
        {
            title = "Updated",
            authorId = author2,
            genreId = genre2,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        book!.Title.Should().Be("Updated");
        book.Author.Id.Should().Be(author2);
        book.Genre.Id.Should().Be(genre2);
    }

    [Fact]
    public async Task UpdateBook_returns_404_when_missing()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PutAsJsonAsync($"/api/books/{Guid.NewGuid()}", new
        {
            title = "Title",
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBook_returns_204_and_preserves_author_and_genre()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var bookId = await CreateBookAsync("Book", authorId, genreId);

        var deleteResponse = await _client.DeleteAsync($"/api/books/{bookId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/books/{bookId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var authorResponse = await _client.GetAsync($"/api/authors/{authorId}");
        authorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var genreResponse = await _client.GetAsync($"/api/genres/{genreId}");
        genreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateBook_with_missing_references_returns_400_with_errors_dictionary()
    {
        var missingAuthor = Guid.NewGuid();
        var missingGenre = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "Title",
            authorId = missingAuthor,
            genreId = missingGenre,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        var errors = document!.RootElement.GetProperty("errors");
        errors.TryGetProperty("authorId", out _).Should().BeTrue();
        errors.TryGetProperty("genreId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_with_invalid_year_returns_400()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var futureYear = DateTime.UtcNow.Year + 1;

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "Title",
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = futureYear
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("publishedYear", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_with_duplicate_isbn_returns_409()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var isbn = "978-0-306-40615-7";
        await CreateBookAsync("First", authorId, genreId, isbn);

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "Second",
            authorId,
            genreId,
            isbn,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ListBooks_with_unsupported_sort_returns_400()
    {
        var response = await _client.GetAsync("/api/books?sort=author");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("sort", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_with_empty_title_returns_400()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "   ",
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("title", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_with_title_too_long_returns_400()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var title = new string('x', 201);

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title,
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("title", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_with_invalid_isbn_returns_400()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "Title",
            authorId,
            genreId,
            isbn = "not-an-isbn",
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("isbn", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateBook_without_isbn_returns_201_with_null_isbn()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "Title",
            authorId,
            genreId,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        book!.Isbn.Should().BeNull();
    }

    [Fact]
    public async Task ListBooks_filters_by_authorId_alone()
    {
        var authorA = await CreateAuthorAsync("Author A");
        var authorB = await CreateAuthorAsync("Author B");
        var genre = await CreateGenreAsync("Genre");

        await CreateBookAsync("Book A", authorA, genre);
        await CreateBookAsync("Book B", authorB, genre);

        var response = await _client.GetAsync($"/api/books?authorId={authorA}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<BookResponse>>(JsonOptions);
        envelope!.Items.Should().ContainSingle(book => book.Title == "Book A");
        envelope.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListBooks_filters_by_genreId_alone()
    {
        var author = await CreateAuthorAsync("Author");
        var genreA = await CreateGenreAsync("Genre A");
        var genreB = await CreateGenreAsync("Genre B");

        await CreateBookAsync("Book A", author, genreA);
        await CreateBookAsync("Book B", author, genreB);

        var response = await _client.GetAsync($"/api/books?genreId={genreA}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<BookResponse>>(JsonOptions);
        envelope!.Items.Should().ContainSingle(book => book.Title == "Book A");
        envelope.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListBooks_without_sort_returns_default_order()
    {
        var author = await CreateAuthorAsync("Author");
        var genre = await CreateGenreAsync("Genre");

        await CreateBookAsync("Gamma Book", author, genre);
        await CreateBookAsync("Alpha Book", author, genre);
        await CreateBookAsync("Beta Book", author, genre);

        var response = await _client.GetAsync("/api/books?pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<BookResponse>>(JsonOptions);
        envelope!.Items.Select(book => book.Title).Should().BeInAscendingOrder();
        envelope.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task UpdateBook_with_duplicate_isbn_returns_409()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");

        var bookA = await CreateBookAsync("Book A", authorId, genreId, isbn: "978-0-306-40615-7");
        var bookB = await CreateBookAsync("Book B", authorId, genreId, isbn: "978-1-4028-9462-6");

        var response = await _client.PutAsJsonAsync($"/api/books/{bookB}", new
        {
            title = "Book B Updated",
            authorId,
            genreId,
            isbn = "9780306406157",
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateBook_with_missing_references_returns_400()
    {
        var authorId = await CreateAuthorAsync("Author");
        var genreId = await CreateGenreAsync("Genre");
        var bookId = await CreateBookAsync("Book", authorId, genreId);
        var missingAuthorId = Guid.NewGuid();
        var missingGenreId = Guid.NewGuid();

        var response = await _client.PutAsJsonAsync($"/api/books/{bookId}", new
        {
            title = "Updated",
            authorId = missingAuthorId,
            genreId = missingGenreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        var errors = document!.RootElement.GetProperty("errors");
        errors.TryGetProperty("authorId", out _).Should().BeTrue();
        errors.TryGetProperty("genreId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBook_returns_404_when_missing()
    {
        var response = await _client.DeleteAsync($"/api/books/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListBooks_with_invalid_pagination_returns_400()
    {
        var response = await _client.GetAsync("/api/books?page=0&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<Guid> CreateAuthorAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new { name, bio = (string?)null }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var author = await response.Content.ReadFromJsonAsync<Philobiblos.Application.Authors.Dtos.AuthorResponse>(JsonOptions);
        return author!.Id;
    }

    private async Task<Guid> CreateGenreAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var genre = await response.Content.ReadFromJsonAsync<Philobiblos.Application.Genres.Dtos.GenreResponse>(JsonOptions);
        return genre!.Id;
    }

    private async Task<Guid> CreateBookAsync(
        string title,
        Guid authorId,
        Guid genreId,
        string? isbn = null,
        int? publishedYear = null)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title,
            authorId,
            genreId,
            isbn,
            publishedYear
        }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var book = await response.Content.ReadFromJsonAsync<BookResponse>(JsonOptions);
        return book!.Id;
    }
}
