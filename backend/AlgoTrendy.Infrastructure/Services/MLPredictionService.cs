using System.Net.Http.Json;
using System.Text.Json;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Infrastructure.Services;

/// <summary>
/// Implementation of ML prediction service that communicates with Python Flask microservice
/// </summary>
public class MLPredictionService : IMLPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MLPredictionService> _logger;
    private readonly string _mlServiceUrl;

    public MLPredictionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MLPredictionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mlServiceUrl = configuration["MLService:Url"] ?? "http://localhost:5003";

        _httpClient.BaseAddress = new Uri(_mlServiceUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    public async Task<ReversalPrediction?> PredictReversalAsync(
        double[] features,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (features == null || features.Length == 0)
            {
                _logger.LogWarning("Cannot predict reversal with null or empty features");
                return null;
            }

            _logger.LogDebug("Sending prediction request with {FeatureCount} features", features.Length);

            var request = new { features };
            var response = await _httpClient.PostAsJsonAsync(
                "/predict/reversal",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "ML service returned error status: {StatusCode}",
                    response.StatusCode);
                return new ReversalPrediction
                {
                    Error = $"ML service error: {response.StatusCode}",
                    Timestamp = DateTime.UtcNow
                };
            }

            var prediction = await response.Content.ReadFromJsonAsync<ReversalPrediction>(
                cancellationToken: cancellationToken);

            if (prediction != null)
            {
                prediction.Timestamp = DateTime.UtcNow;
                _logger.LogInformation(
                    "Received prediction: IsReversal={IsReversal}, Confidence={Confidence:F3}",
                    prediction.IsReversal,
                    prediction.Confidence);
            }

            return prediction;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling ML service");
            return new ReversalPrediction
            {
                Error = $"Connection error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "ML service request timed out");
            return new ReversalPrediction
            {
                Error = "Request timeout",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during ML prediction");
            return new ReversalPrediction
            {
                Error = $"Unexpected error: {ex.Message}",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ML service health check failed");
            return false;
        }
    }
}
