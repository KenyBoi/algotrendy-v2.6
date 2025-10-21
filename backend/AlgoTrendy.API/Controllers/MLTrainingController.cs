using Microsoft.AspNetCore.Mvc;
using AlgoTrendy.API.Services;

namespace AlgoTrendy.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MLTrainingController : ControllerBase
{
    private readonly MLModelService _mlModelService;
    private readonly ILogger<MLTrainingController> _logger;

    public MLTrainingController(
        MLModelService mlModelService,
        ILogger<MLTrainingController> logger)
    {
        _mlModelService = mlModelService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available ML models
    /// </summary>
    /// <returns>List of all trained ML models with basic information</returns>
    /// <response code="200">Returns the list of available models</response>
    /// <response code="500">If there was an error communicating with the ML service</response>
    [HttpGet("models")]
    [ProducesResponseType(typeof(List<MLModelInfo>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<MLModelInfo>>> GetModels()
    {
        var models = await _mlModelService.ListModelsAsync();

        if (models == null)
        {
            return StatusCode(500, "Error retrieving models from ML API");
        }

        return Ok(models);
    }

    /// <summary>
    /// Get detailed information about a specific model
    /// </summary>
    /// <param name="modelId">The unique identifier of the model</param>
    /// <returns>Detailed model information including metrics, parameters, and training history</returns>
    /// <response code="200">Returns the model details</response>
    /// <response code="404">If the model with the specified ID is not found</response>
    [HttpGet("models/{modelId}")]
    [ProducesResponseType(typeof(MLModelDetails), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MLModelDetails>> GetModelDetails(string modelId)
    {
        var details = await _mlModelService.GetModelDetailsAsync(modelId);

        if (details == null)
        {
            return NotFound($"Model {modelId} not found");
        }

        return Ok(details);
    }

    /// <summary>
    /// Start a new model training job
    /// </summary>
    /// <param name="config">Training configuration including symbols, timeframe, and model parameters</param>
    /// <returns>Training job information with job ID and status</returns>
    /// <response code="200">Training job successfully started</response>
    /// <response code="500">If there was an error starting the training job</response>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/mltraining/train
    ///     {
    ///         "symbols": ["BTCUSDT", "ETHUSDT"],
    ///         "timeframe": "1h",
    ///         "lookbackDays": 90
    ///     }
    /// </remarks>
    [HttpPost("train")]
    [ProducesResponseType(typeof(TrainingJobResult), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<TrainingJobResult>> StartTraining([FromBody] TrainingConfig config)
    {
        _logger.LogInformation("Starting ML training with {SymbolCount} symbols", config.Symbols.Count);

        var result = await _mlModelService.StartTrainingAsync(config);

        if (result == null)
        {
            return StatusCode(500, "Error starting training job");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get status of a training job
    /// </summary>
    /// <param name="jobId">The unique identifier of the training job</param>
    /// <returns>Current status of the training job including progress, metrics, and completion time</returns>
    /// <response code="200">Returns the training job status</response>
    /// <response code="404">If the training job with the specified ID is not found</response>
    [HttpGet("training/{jobId}")]
    [ProducesResponseType(typeof(TrainingStatus), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<TrainingStatus>> GetTrainingStatus(string jobId)
    {
        var status = await _mlModelService.GetTrainingStatusAsync(jobId);

        if (status == null)
        {
            return NotFound($"Training job {jobId} not found");
        }

        return Ok(status);
    }

    /// <summary>
    /// Get trend reversal prediction for a symbol
    /// </summary>
    /// <param name="request">Prediction request containing symbol and recent candle data</param>
    /// <returns>Reversal prediction with probability, confidence, and recommended action</returns>
    /// <response code="200">Returns the prediction result</response>
    /// <response code="500">If there was an error generating the prediction</response>
    /// <remarks>
    /// The prediction uses the latest trained model to analyze market patterns
    /// and predict potential trend reversals based on technical indicators.
    /// </remarks>
    [HttpPost("predict")]
    [ProducesResponseType(typeof(ReversalPrediction), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ReversalPrediction>> GetPrediction([FromBody] PredictionRequest request)
    {
        var prediction = await _mlModelService.PredictReversalAsync(request.Symbol, request.RecentCandles);

        if (prediction == null)
        {
            return StatusCode(500, "Error getting prediction");
        }

        return Ok(prediction);
    }

    /// <summary>
    /// Check model drift metrics
    /// </summary>
    /// <param name="modelId">The unique identifier of the model to check for drift</param>
    /// <param name="productionData">Recent production data to compare against training distribution</param>
    /// <returns>Drift metrics including statistical measures and drift score</returns>
    /// <response code="200">Returns the drift analysis results</response>
    /// <response code="500">If there was an error calculating drift metrics</response>
    /// <remarks>
    /// Model drift detection helps identify when a model's performance may degrade
    /// due to changes in the underlying data distribution. Regular drift monitoring
    /// is essential for maintaining model accuracy in production.
    /// </remarks>
    [HttpPost("drift/{modelId}")]
    [ProducesResponseType(typeof(DriftMetrics), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<DriftMetrics>> CheckDrift(string modelId, [FromBody] List<Dictionary<string, object>> productionData)
    {
        var drift = await _mlModelService.GetDriftMetricsAsync(modelId, productionData);

        if (drift == null)
        {
            return StatusCode(500, "Error calculating drift metrics");
        }

        return Ok(drift);
    }

    /// <summary>
    /// Get latest pattern analysis results
    /// </summary>
    /// <returns>Latest pattern analysis including identified patterns and trading opportunities</returns>
    /// <response code="200">Returns the latest pattern analysis</response>
    /// <response code="404">If no pattern analysis is available</response>
    /// <remarks>
    /// Pattern analysis identifies chart patterns such as head and shoulders,
    /// double tops/bottoms, triangles, and other technical formations that
    /// may indicate potential trading opportunities.
    /// </remarks>
    [HttpGet("patterns")]
    [ProducesResponseType(typeof(PatternAnalysis), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PatternAnalysis>> GetLatestPatterns()
    {
        var patterns = await _mlModelService.GetLatestPatternsAsync();

        if (patterns == null)
        {
            return NotFound("No pattern analysis available");
        }

        return Ok(patterns);
    }

    /// <summary>
    /// Health check for ML API connectivity
    /// </summary>
    /// <returns>Health status of the ML API service</returns>
    /// <response code="200">ML API is healthy and responding</response>
    /// <response code="503">ML API is unhealthy or not responding</response>
    /// <remarks>
    /// This endpoint checks connectivity to the ML API service running on port 5050.
    /// Use this to verify the ML service is available before making training or prediction requests.
    /// </remarks>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<ActionResult> CheckHealth()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5050/");

            if (response.IsSuccessStatusCode)
            {
                return Ok(new {
                    status = "healthy",
                    mlApiConnected = true,
                    message = "ML API is accessible"
                });
            }

            return StatusCode(503, new {
                status = "degraded",
                mlApiConnected = false,
                message = "ML API is not responding"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ML API health");
            return StatusCode(503, new {
                status = "unhealthy",
                mlApiConnected = false,
                message = $"ML API connection failed: {ex.Message}"
            });
        }
    }
}

public record PredictionRequest(
    string Symbol,
    List<Dictionary<string, object>> RecentCandles
);
