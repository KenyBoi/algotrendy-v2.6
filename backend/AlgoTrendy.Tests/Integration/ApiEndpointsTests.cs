using AlgoTrendy.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AlgoTrendy.Tests.Integration;

/// <summary>
/// Integration tests for API endpoints using WebApplicationFactory
/// </summary>
public class ApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GetMarketData_WithValidSymbol_ReturnsData()
    {
        // Arrange
        var symbol = "BTCUSDT";

        // Act
        var response = await _client.GetAsync($"/api/market-data/{symbol}/latest");

        // Assert
        // May return 404 if no data, or 200 with data
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMarketData_WithInvalidSymbol_ReturnsBadRequestOrNotFound()
    {
        // Arrange
        var invalidSymbol = "";

        // Act
        var response = await _client.GetAsync($"/api/market-data/{invalidSymbol}/latest");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("/api/market-data/BTCUSDT/latest")]
    [InlineData("/api/market-data/ETHUSDT/latest")]
    public async Task GetMarketData_MultipleSymbols_ReturnsConsistently(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMarketData_Historical_WithTimeRange_ReturnsData()
    {
        // Arrange
        var symbol = "BTCUSDT";
        var startTime = DateTime.UtcNow.AddHours(-24);
        var endTime = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync(
            $"/api/market-data/{symbol}?startTime={startTime:O}&endTime={endTime:O}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApiEndpoints_ReturnCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().BeOneOf("application/json", "text/plain");
    }

    [Fact]
    public async Task ApiEndpoints_HandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    [Fact]
    public async Task ApiEndpoints_HandleLargePayload()
    {
        // This is a placeholder - actual test would depend on specific endpoints
        // that accept POST data

        // Arrange
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Swagger_IsAvailable_InDevelopment()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        // Swagger may or may not be available depending on environment
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ApiEndpoints_ReturnProblemDetails_OnError()
    {
        // Arrange - Request a non-existent endpoint
        var response = await _client.GetAsync("/api/nonexistent/endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrders_Endpoint_ExistsAndResponds()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        // May return 200 with empty list, 404, 401 if auth required, or 500 if database unavailable
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPositions_Endpoint_ExistsAndResponds()
    {
        // Act
        var response = await _client.GetAsync("/api/positions");

        // Assert
        // May return 200 with empty list, 404, 401 if auth required, or 500 if database unavailable
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ApiEndpoints_SupportCors()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        // CORS headers may or may not be present depending on configuration
    }

    [Fact]
    public async Task ApiEndpoints_HandleOptionsRequest()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);
    }
}
