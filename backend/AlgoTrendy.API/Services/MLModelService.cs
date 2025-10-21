using System.Text;
using System.Text.Json;

namespace AlgoTrendy.API.Services;

/// <summary>
/// Service for interacting with Python ML API for model training, prediction, and monitoring
/// </summary>
public class MLModelService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MLModelService> _logger;
    private readonly string _mlApiBaseUrl;

    public MLModelService(
        IHttpClientFactory httpClientFactory,
        ILogger<MLModelService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _mlApiBaseUrl = configuration["MLTraining:PythonApiUrl"] ?? "http://localhost:5050";
    }

    #region Model Management

    /// <summary>
    /// List all available ML models
    /// </summary>
    public async Task<List<MLModelInfo>?> ListModelsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_mlApiBaseUrl}/models");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MLModelInfo>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing ML models");
            return null;
        }
    }

    /// <summary>
    /// Get detailed information about a specific model
    /// </summary>
    public async Task<MLModelDetails?> GetModelDetailsAsync(string modelId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_mlApiBaseUrl}/models/{modelId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MLModelDetails>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model details for {ModelId}", modelId);
            return null;
        }
    }

    #endregion

    #region Training

    /// <summary>
    /// Start a new model training job
    /// </summary>
    public async Task<TrainingJobResult?> StartTrainingAsync(TrainingConfig config)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(config);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_mlApiBaseUrl}/train", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TrainingJobResult>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training job");
            return null;
        }
    }

    /// <summary>
    /// Get status of a training job
    /// </summary>
    public async Task<TrainingStatus?> GetTrainingStatusAsync(string jobId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_mlApiBaseUrl}/training/{jobId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TrainingStatus>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training status for {JobId}", jobId);
            return null;
        }
    }

    #endregion

    #region Prediction

    /// <summary>
    /// Get trend reversal prediction for given market data
    /// </summary>
    public async Task<ReversalPrediction?> PredictReversalAsync(string symbol, List<Dictionary<string, object>> recentCandles)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new
            {
                symbol,
                recent_candles = recentCandles
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_mlApiBaseUrl}/predict", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReversalPrediction>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prediction for {Symbol}", symbol);
            return null;
        }
    }

    #endregion

    #region Drift Detection

    /// <summary>
    /// Calculate drift metrics for a model
    /// </summary>
    public async Task<DriftMetrics?> GetDriftMetricsAsync(string modelId, List<Dictionary<string, object>> productionData)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new
            {
                model_id = modelId,
                production_data = productionData
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_mlApiBaseUrl}/drift", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DriftMetrics>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drift metrics for {ModelId}", modelId);
            return null;
        }
    }

    #endregion

    #region Pattern Analysis

    /// <summary>
    /// Get latest pattern analysis results
    /// </summary>
    public async Task<PatternAnalysis?> GetLatestPatternsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_mlApiBaseUrl}/patterns");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PatternAnalysis>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest patterns");
            return null;
        }
    }

    #endregion
}

#region DTOs

public record MLModelInfo(
    string ModelId,
    string ModelType,
    string TrainedAt,
    double Accuracy,
    double Precision,
    double Recall,
    double F1Score,
    int TrainingRows,
    string Status
);

public record MLModelDetails(
    string ModelId,
    string ModelType,
    string TrainedAt,
    List<string> Features,
    Dictionary<string, double> FeatureImportance,
    ModelMetrics Metrics,
    int TrainingRows
);

public record ModelMetrics(
    double Accuracy,
    double Precision,
    double Recall,
    double F1Score
);

public record TrainingConfig(
    List<string> Symbols,
    int Days = 30,
    string Interval = "5m",
    Dictionary<string, object>? Hyperparameters = null
);

public record TrainingJobResult(
    string JobId,
    string Status,
    string Message
);

public record TrainingStatus(
    string Status,
    int Progress,
    string? StartedAt = null,
    string? CompletedAt = null,
    string? Error = null
);

public record ReversalPrediction(
    string Symbol,
    bool IsReversal,
    double Confidence,
    Dictionary<string, double> FeatureValues,
    string Timestamp
);

public record DriftMetrics(
    string ModelId,
    string LastChecked,
    double DriftScore,
    bool IsDrifting,
    Dictionary<string, double> FeatureDrift,
    string RecommendedAction
);

public record PatternAnalysis(
    string Timestamp,
    List<OpportunityInfo> Opportunities,
    List<OpportunityInfo> AllResults
);

public record OpportunityInfo(
    string Symbol,
    double Price,
    double Rsi,
    double VolumeRatio,
    double ReversalConfidence,
    List<PatternInfo> Patterns
);

public record PatternInfo(
    string Type,
    double Confidence,
    string Signal,
    string Reason
);

#endregion
