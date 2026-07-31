using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Common;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class AuthorIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public AuthorIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAuthor_returns_201_with_location_and_body()
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new { name = "Jane Doe", bio = "A writer." }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var author = await response.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        author.Should().NotBeNull();
        author!.Name.Should().Be("Jane Doe");
        author.Bio.Should().Be("A writer.");
    }

    [Fact]
    public async Task ListAuthors_returns_paged_envelope_with_metadata()
    {
        await CreateAuthorAsync("Alpha");
        await CreateAuthorAsync("Beta");
        await CreateAuthorAsync("Gamma");

        var response = await _client.GetAsync("/api/authors?page=2&pageSize=1&sort=name&direction=desc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<AuthorResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Items.Should().HaveCount(1);
        envelope.Page.Should().Be(2);
        envelope.PageSize.Should().Be(1);
        envelope.TotalCount.Should().Be(3);
        envelope.Items.Single().Name.Should().Be("Beta");
    }

    [Fact]
    public async Task ListAuthors_filters_by_name_case_insensitively()
    {
        await CreateAuthorAsync("Jane Doe");
        await CreateAuthorAsync("John Smith");
        await CreateAuthorAsync("Alice Walker");

        var response = await _client.GetAsync("/api/authors?name=jane");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<AuthorResponse>>(JsonOptions);
        envelope!.Items.Should().ContainSingle(author => author.Name == "Jane Doe");
    }

    [Fact]
    public async Task GetAuthor_returns_200_when_found()
    {
        var id = await CreateAuthorAsync("Author");

        var response = await _client.GetAsync($"/api/authors/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var author = await response.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        author!.Name.Should().Be("Author");
    }

    [Fact]
    public async Task GetAuthor_returns_404_when_missing()
    {
        var response = await _client.GetAsync($"/api/authors/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateAuthor_returns_200()
    {
        var id = await CreateAuthorAsync("Old");

        var response = await _client.PutAsJsonAsync($"/api/authors/{id}", new { name = "New", bio = "Updated bio." }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var author = await response.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        author!.Name.Should().Be("New");
        author.Bio.Should().Be("Updated bio.");
    }

    [Fact]
    public async Task DeleteAuthor_returns_204_and_removes_resource()
    {
        var id = await CreateAuthorAsync("ToDelete");

        var deleteResponse = await _client.DeleteAsync($"/api/authors/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/authors/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAuthor_with_empty_name_returns_400_with_errors_dictionary()
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new { name = "", bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAuthor_with_duplicate_name_returns_409()
    {
        await _client.PostAsJsonAsync("/api/authors", new { name = "Duplicate", bio = (string?)null }, JsonOptions);

        var response = await _client.PostAsJsonAsync("/api/authors", new { name = "DUPLICATE", bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAuthor_with_duplicate_name_returns_409()
    {
        await CreateAuthorAsync("First");
        var secondId = await CreateAuthorAsync("Second");

        var response = await _client.PutAsJsonAsync($"/api/authors/{secondId}", new { name = "first", bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAuthor_in_use_returns_409()
    {
        var authorId = await CreateAuthorAsync("InUse");
        var genreId = await CreateGenreAsync("Genre");
        await CreateBookAsync("Book", authorId, genreId);

        var response = await _client.DeleteAsync($"/api/authors/{authorId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ListAuthors_with_unsupported_sort_returns_400()
    {
        var response = await _client.GetAsync("/api/authors?sort=unknown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("sort", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAuthor_with_name_longer_than_150_characters_returns_400()
    {
        var name = new string('x', 151);

        var response = await _client.PostAsJsonAsync("/api/authors", new { name, bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAuthor_with_bio_longer_than_2000_characters_returns_400()
    {
        var bio = new string('x', 2001);

        var response = await _client.PostAsJsonAsync("/api/authors", new { name = "Valid", bio }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("bio", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ListAuthors_sorts_by_name_descending()
    {
        await CreateAuthorAsync("Alpha");
        await CreateAuthorAsync("Beta");
        await CreateAuthorAsync("Gamma");

        var response = await _client.GetAsync("/api/authors?sort=name&direction=desc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<AuthorResponse>>(JsonOptions);
        envelope!.Items.Select(author => author.Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListAuthors_without_sort_returns_default_order()
    {
        await CreateAuthorAsync("Gamma");
        await CreateAuthorAsync("Alpha");
        await CreateAuthorAsync("Beta");

        var response = await _client.GetAsync("/api/authors?pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<AuthorResponse>>(JsonOptions);
        envelope!.Items.Select(author => author.Name).Should().BeInAscendingOrder();
        envelope.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAuthor_with_empty_name_returns_400()
    {
        var id = await CreateAuthorAsync("Valid");

        var response = await _client.PutAsJsonAsync($"/api/authors/{id}", new { name = "", bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAuthor_with_name_too_long_returns_400()
    {
        var id = await CreateAuthorAsync("Valid");
        var name = new string('x', 151);

        var response = await _client.PutAsJsonAsync($"/api/authors/{id}", new { name, bio = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAuthor_with_bio_too_long_returns_400()
    {
        var id = await CreateAuthorAsync("Valid");
        var bio = new string('x', 2001);

        var response = await _client.PutAsJsonAsync($"/api/authors/{id}", new { name = "Valid", bio }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("bio", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAuthor_returns_404_when_missing()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/authors/{Guid.NewGuid()}",
            new { name = "Name", bio = (string?)null },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAuthor_returns_404_when_missing()
    {
        var response = await _client.DeleteAsync($"/api/authors/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListAuthors_with_invalid_pagination_returns_400()
    {
        var response = await _client.GetAsync("/api/authors?page=0&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<Guid> CreateAuthorAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new { name, bio = (string?)null }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var author = await response.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        return author!.Id;
    }

    private async Task<Guid> CreateGenreAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var genre = await response.Content.ReadFromJsonAsync<Philobiblos.Application.Genres.Dtos.GenreResponse>(JsonOptions);
        return genre!.Id;
    }

    private async Task CreateBookAsync(string title, Guid authorId, Guid genreId)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title,
            authorId,
            genreId,
            isbn = (string?)null,
            publishedYear = (int?)null
        }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
