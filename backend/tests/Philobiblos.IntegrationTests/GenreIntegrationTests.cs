using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Philobiblos.Api.Features.Genres;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class GenreIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public GenreIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateGenre_returns_201_with_location_and_body()
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "Fantasy" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var genre = await response.Content.ReadFromJsonAsync<GenreResponse>(JsonOptions);
        genre.Should().NotBeNull();
        genre!.Name.Should().Be("Fantasy");
        response.Headers.Location!.ToString().Should().Contain($"/api/genres/{genre.Id}");
    }

    [Fact]
    public async Task ListGenres_returns_paged_envelope_with_metadata()
    {
        await CreateGenreAsync("Alpha");
        await CreateGenreAsync("Beta");
        await CreateGenreAsync("Gamma");

        var response = await _client.GetAsync("/api/genres?page=1&pageSize=2&sort=name&direction=asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<GenreResponse>>(JsonOptions);
        envelope.Should().NotBeNull();
        envelope!.Items.Should().HaveCount(2);
        envelope.Page.Should().Be(1);
        envelope.PageSize.Should().Be(2);
        envelope.TotalCount.Should().Be(3);
        envelope.Items.Select(g => g.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ListGenres_filters_by_name_case_insensitively()
    {
        await CreateGenreAsync("Science Fiction");
        await CreateGenreAsync("Historical Fiction");
        await CreateGenreAsync("Biography");

        var response = await _client.GetAsync("/api/genres?name=fiction");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<PagedResult<GenreResponse>>(JsonOptions);
        envelope!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGenre_returns_200_when_found()
    {
        var id = await CreateGenreAsync("Mystery");

        var response = await _client.GetAsync($"/api/genres/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var genre = await response.Content.ReadFromJsonAsync<GenreResponse>(JsonOptions);
        genre!.Name.Should().Be("Mystery");
    }

    [Fact]
    public async Task GetGenre_returns_404_when_missing()
    {
        var response = await _client.GetAsync($"/api/genres/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateGenre_returns_200_with_updated_body()
    {
        var id = await CreateGenreAsync("Old Name");

        var response = await _client.PutAsJsonAsync($"/api/genres/{id}", new { name = "New Name" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var genre = await response.Content.ReadFromJsonAsync<GenreResponse>(JsonOptions);
        genre!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateGenre_returns_404_when_missing()
    {
        var response = await _client.PutAsJsonAsync($"/api/genres/{Guid.NewGuid()}", new { name = "Name" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteGenre_returns_204_and_removes_resource()
    {
        var id = await CreateGenreAsync("ToDelete");

        var deleteResponse = await _client.DeleteAsync($"/api/genres/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/genres/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteGenre_returns_404_when_missing()
    {
        var response = await _client.DeleteAsync($"/api/genres/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateGenre_with_empty_name_returns_400_problem_details_with_errors_dictionary()
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "   " }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        var errors = document!.RootElement.GetProperty("errors");
        errors.TryGetProperty("name", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateGenre_with_duplicate_name_returns_409()
    {
        await _client.PostAsJsonAsync("/api/genres", new { name = "Duplicate" }, JsonOptions);

        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "duplicate" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task UpdateGenre_with_duplicate_name_returns_409()
    {
        await CreateGenreAsync("First");
        var secondId = await CreateGenreAsync("Second");

        var response = await _client.PutAsJsonAsync($"/api/genres/{secondId}", new { name = "first" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteGenre_in_use_returns_409()
    {
        var genreId = await CreateGenreAsync("InUse");
        var authorId = await CreateAuthorAsync("Author");
        await CreateBookAsync("Book", authorId, genreId);

        var response = await _client.DeleteAsync($"/api/genres/{genreId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ListGenres_with_invalid_pagination_returns_400()
    {
        var response = await _client.GetAsync("/api/genres?page=0&pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ListGenres_with_unsupported_sort_returns_400()
    {
        var response = await _client.GetAsync("/api/genres?sort=unknown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty("sort", out _).Should().BeTrue();
    }

    private async Task<Guid> CreateGenreAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var genre = await response.Content.ReadFromJsonAsync<GenreResponse>(JsonOptions);
        return genre!.Id;
    }

    private async Task<Guid> CreateAuthorAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/authors", new { name, bio = (string?)null }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var author = await response.Content.ReadFromJsonAsync<Philobiblos.Api.Features.Authors.AuthorResponse>(JsonOptions);
        return author!.Id;
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
