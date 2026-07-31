using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class ObservabilityIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public ObservabilityIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_endpoint_returns_healthy_status_anonymously()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Metrics_endpoint_returns_prometheus_metrics_anonymously()
    {
        await _client.GetAsync("/health");

        var response = await _client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("# TYPE");
        body.Should().Contain("dotnet_");
        body.Should().Contain("http_server_request_duration");
    }

    [Fact]
    public async Task Correlation_id_is_attached_to_current_open_telemetry_activity()
    {
        var response = await _client.GetAsync("/api/test/activity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var correlationId = body.GetProperty("correlationId").GetString();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }
}
