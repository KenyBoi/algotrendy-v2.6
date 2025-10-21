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
    [HttpGet("models")]
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
    [HttpGet("models/{modelId}")]
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
    [HttpPost("train")]
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
    [HttpGet("training/{jobId}")]
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
    [HttpPost("predict")]
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
    [HttpPost("drift/{modelId}")]
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
    [HttpGet("patterns")]
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
    [HttpGet("health")]
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
