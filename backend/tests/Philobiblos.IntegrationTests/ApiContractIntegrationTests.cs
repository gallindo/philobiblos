using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Philobiblos.Api.Features.Authors;
using Philobiblos.Api.Features.Books;
using Philobiblos.Api.Features.Genres;
using Philobiblos.Api.Infrastructure;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class ApiContractIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public ApiContractIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task NotFound_response_returns_problem_details_without_internal_information()
    {
        var response = await _client.GetAsync($"/api/genres/{Guid.NewGuid()}");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BadRequest_response_returns_problem_details_without_internal_information()
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "" }, JsonOptions);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Conflict_response_returns_problem_details_without_internal_information()
    {
        await _client.PostAsJsonAsync("/api/genres", new { name = "Duplicate" }, JsonOptions);
        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "duplicate" }, JsonOptions);

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task InternalServerError_response_returns_problem_details_with_correlationId()
    {
        var response = await _client.GetAsync("/api/test/throw");

        await AssertProblemDetailsAsync(response, HttpStatusCode.InternalServerError, expectCorrelationId: true);
    }

    [Fact]
    public async Task Validation_failure_reports_all_failing_fields_together()
    {
        var futureYear = DateTime.UtcNow.Year + 1;

        var response = await _client.PostAsJsonAsync("/api/books", new
        {
            title = "   ",
            authorId = Guid.Empty,
            genreId = Guid.Empty,
            isbn = (string?)null,
            publishedYear = futureYear
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        var errors = document!.RootElement.GetProperty("errors");
        errors.TryGetProperty("title", out _).Should().BeTrue();
        errors.TryGetProperty("authorId", out _).Should().BeTrue();
        errors.TryGetProperty("genreId", out _).Should().BeTrue();
        errors.TryGetProperty("publishedYear", out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/authors?page=0")]
    [InlineData("/api/authors?pageSize=101")]
    [InlineData("/api/books?page=0")]
    [InlineData("/api/books?pageSize=0")]
    public async Task Out_of_range_pagination_returns_400_problem_details(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Theory]
    [InlineData("/api/authors?sort=unknown", "sort")]
    [InlineData("/api/books?sort=unknown", "sort")]
    public async Task Unsupported_sort_returns_400_with_field_key(string url, string fieldKey)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(JsonOptions);
        document!.RootElement.GetProperty("errors").TryGetProperty(fieldKey, out _).Should().BeTrue();
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        bool expectCorrelationId = false)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("StackTrace");
        body.Should().NotContain(" at ");

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.TryGetProperty("type", out _).Should().BeTrue();
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.GetProperty("status").GetInt32().Should().Be((int)expectedStatus);
        root.TryGetProperty("detail", out _).Should().BeTrue();

        if (expectCorrelationId)
        {
            root.TryGetProperty("correlationId", out var correlationId).Should().BeTrue();
            correlationId.GetString().Should().NotBeNullOrWhiteSpace();
        }
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
        var author = await response.Content.ReadFromJsonAsync<AuthorResponse>(JsonOptions);
        return author!.Id;
    }
}
