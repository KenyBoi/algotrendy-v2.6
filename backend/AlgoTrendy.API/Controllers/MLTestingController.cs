using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API for ML model testing (Backtest, Walk-Forward, Gap Analysis)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MLTestingController : ControllerBase
{
    private readonly ILogger<MLTestingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _pythonPath;
    private readonly string _testingScriptPath;

    public MLTestingController(
        ILogger<MLTestingController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _pythonPath = configuration["Python:ExecutablePath"] ?? "python3";
        _testingScriptPath = configuration["Testing:ScriptPath"] ??
            "/root/AlgoTrendy_v2.6/MEM/comprehensive_testing_framework.py";
    }

    /// <summary>
    /// Run comprehensive testing (Backtest, Walk-Forward, Gap Analysis)
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<TestingResults>> RunTests([FromBody] TestingConfig config)
    {
        try
        {
            _logger.LogInformation(
                "Starting comprehensive testing for {Symbol} with config: {@Config}",
                config.Symbol,
                config
            );

            // Save config to temp file
            var configPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");
            await System.IO.File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(config));

            // Execute Python testing script
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"{_testingScriptPath} --config {configPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            // Clean up temp file
            if (System.IO.File.Exists(configPath))
            {
                System.IO.File.Delete(configPath);
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("Testing failed with exit code {ExitCode}: {Error}",
                    process.ExitCode, error);
                return StatusCode(500, new { error = error });
            }

            // Parse results from output
            var results = ParseTestingResults(output);

            _logger.LogInformation("Testing completed successfully for {Symbol}", config.Symbol);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running comprehensive tests");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get available test configurations
    /// </summary>
    [HttpGet("presets")]
    public ActionResult<List<TestingPreset>> GetPresets()
    {
        var presets = new List<TestingPreset>
        {
            new TestingPreset
            {
                Name = "Quick Test (Fast)",
                Description = "Fast validation for development",
                Config = new TestingConfig
                {
                    EnableBacktest = true,
                    EnableWalkForward = true,
                    EnableGapAnalysis = true,
                    BacktestConfig = new MLBacktestConfig
                    {
                        NCVSplits = 3,
                        EmbargoPct = 0.01m,
                        TestSize = 0.2m
                    },
                    WalkForwardConfig = new WalkForwardConfig
                    {
                        TrainWindowDays = 90,
                        TestWindowDays = 30,
                        StepDays = 15
                    }
                }
            },
            new TestingPreset
            {
                Name = "Standard Test (Recommended)",
                Description = "Balanced speed and accuracy",
                Config = new TestingConfig
                {
                    EnableBacktest = true,
                    EnableWalkForward = true,
                    EnableGapAnalysis = true,
                    BacktestConfig = new MLBacktestConfig
                    {
                        NCVSplits = 5,
                        EmbargoPct = 0.01m,
                        TestSize = 0.2m
                    },
                    WalkForwardConfig = new WalkForwardConfig
                    {
                        TrainWindowDays = 365,
                        TestWindowDays = 90,
                        StepDays = 30
                    }
                }
            },
            new TestingPreset
            {
                Name = "Production Test (Thorough)",
                Description = "Comprehensive validation for production",
                Config = new TestingConfig
                {
                    EnableBacktest = true,
                    EnableWalkForward = true,
                    EnableGapAnalysis = true,
                    BacktestConfig = new MLBacktestConfig
                    {
                        NCVSplits = 10,
                        EmbargoPct = 0.02m,
                        TestSize = 0.2m
                    },
                    WalkForwardConfig = new WalkForwardConfig
                    {
                        TrainWindowDays = 1095, // 3 years
                        TestWindowDays = 90,
                        StepDays = 30
                    }
                }
            },
            new TestingPreset
            {
                Name = "Backtest Only",
                Description = "Quick backtest with CPCV",
                Config = new TestingConfig
                {
                    EnableBacktest = true,
                    EnableWalkForward = false,
                    EnableGapAnalysis = false,
                    BacktestConfig = new MLBacktestConfig
                    {
                        NCVSplits = 5,
                        EmbargoPct = 0.01m,
                        TestSize = 0.2m
                    }
                }
            },
            new TestingPreset
            {
                Name = "Walk-Forward Only",
                Description = "Rolling validation without gap analysis",
                Config = new TestingConfig
                {
                    EnableBacktest = false,
                    EnableWalkForward = true,
                    EnableGapAnalysis = false,
                    WalkForwardConfig = new WalkForwardConfig
                    {
                        TrainWindowDays = 365,
                        TestWindowDays = 90,
                        StepDays = 30
                    }
                }
            }
        };

        return Ok(presets);
    }

    private TestingResults ParseTestingResults(string output)
    {
        // Simple parsing - in production, use structured output from Python
        var results = new TestingResults
        {
            Timestamp = DateTime.UtcNow,
            RawOutput = output
        };

        // Parse backtest metrics
        if (output.Contains("Backtest complete"))
        {
            results.BacktestResults = ParseBacktestMetrics(output);
        }

        // Parse walk-forward metrics
        if (output.Contains("Walk-forward complete"))
        {
            results.WalkForwardResults = ParseWalkForwardMetrics(output);
        }

        // Parse gap analysis
        if (output.Contains("Gap Analysis Complete"))
        {
            results.GapAnalysis = ParseGapAnalysis(output);
        }

        return results;
    }

    private BacktestMetrics ParseBacktestMetrics(string output)
    {
        // Extract metrics from output
        return new BacktestMetrics
        {
            Accuracy = ExtractMetric(output, "Accuracy:", 0.0m),
            Precision = ExtractMetric(output, "Precision:", 0.0m),
            Recall = ExtractMetric(output, "Recall:", 0.0m),
            SharpeRatio = ExtractMetric(output, "Sharpe:", 0.0m),
            MaxDrawdown = ExtractMetric(output, "Max DD:", 0.0m),
            WinRate = ExtractMetric(output, "Win Rate:", 0.0m)
        };
    }

    private WalkForwardMetrics ParseWalkForwardMetrics(string output)
    {
        return new WalkForwardMetrics
        {
            NumPeriods = ExtractIntMetric(output, "periods tested", 0),
            AvgAccuracy = ExtractMetric(output, "Accuracy:", 0.0m),
            AvgSharpe = ExtractMetric(output, "Sharpe:", 0.0m),
            Efficiency = ExtractMetric(output, "WF Efficiency:", 0.0m)
        };
    }

    private GapAnalysisResult ParseGapAnalysis(string output)
    {
        var accuracyGap = ExtractMetric(output, "Accuracy Gap:", 0.0m);
        var sharpeGap = ExtractMetric(output, "Sharpe Gap:", 0.0m);
        var overfittingScore = ExtractMetric(output, "Overfitting:", 0.0m);
        var degradation = ExtractMetric(output, "Predicted Degradation:", 0.0m);

        var trend = "stable";
        if (output.Contains("INCREASING"))
            trend = "increasing";
        else if (output.Contains("DECREASING"))
            trend = "decreasing";

        var recommendation = ExtractRecommendation(output);

        return new GapAnalysisResult
        {
            AccuracyGap = accuracyGap,
            SharpeGap = sharpeGap,
            Trend = trend,
            OverfittingScore = overfittingScore,
            DegradationPrediction = degradation,
            Recommendation = recommendation,
            IsSafeToDeply = overfittingScore < 50
        };
    }

    private decimal ExtractMetric(string output, string label, decimal defaultValue)
    {
        try
        {
            var index = output.IndexOf(label);
            if (index == -1) return defaultValue;

            var valueStart = index + label.Length;
            var valueEnd = output.IndexOfAny(new[] { '\n', ',', ' ' }, valueStart);
            if (valueEnd == -1) valueEnd = output.Length;

            var valueStr = output.Substring(valueStart, valueEnd - valueStart).Trim();
            valueStr = valueStr.TrimEnd('%');

            if (decimal.TryParse(valueStr, out var value))
                return value;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private int ExtractIntMetric(string output, string label, int defaultValue)
    {
        return (int)ExtractMetric(output, label, defaultValue);
    }

    private string ExtractRecommendation(string output)
    {
        var startMarkers = new[] { "✅", "⚠️", "⛔" };
        foreach (var marker in startMarkers)
        {
            var index = output.IndexOf(marker);
            if (index != -1)
            {
                var endIndex = output.IndexOf('\n', index);
                if (endIndex == -1) endIndex = output.Length;
                return output.Substring(index, endIndex - index).Trim();
            }
        }
        return "No recommendation available";
    }
}

#region DTOs

public record TestingConfig
{
    public string Symbol { get; set; } = "BTCUSDT";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool EnableBacktest { get; set; } = true;
    public bool EnableWalkForward { get; set; } = true;
    public bool EnableGapAnalysis { get; set; } = true;
    public MLBacktestConfig? BacktestConfig { get; set; }
    public WalkForwardConfig? WalkForwardConfig { get; set; }
}

public record MLBacktestConfig
{
    public int NCVSplits { get; set; } = 5;
    public decimal EmbargoPct { get; set; } = 0.01m;
    public decimal TestSize { get; set; } = 0.2m;
    public decimal MinTrainSize { get; set; } = 0.5m;
}

public record WalkForwardConfig
{
    public int TrainWindowDays { get; set; } = 365;
    public int TestWindowDays { get; set; } = 90;
    public int StepDays { get; set; } = 30;
}

public record TestingResults
{
    public DateTime Timestamp { get; set; }
    public BacktestMetrics? BacktestResults { get; set; }
    public WalkForwardMetrics? WalkForwardResults { get; set; }
    public GapAnalysisResult? GapAnalysis { get; set; }
    public string RawOutput { get; set; } = string.Empty;
}

public record BacktestMetrics
{
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal WinRate { get; set; }
    public int TotalTrades { get; set; }
}

public record WalkForwardMetrics
{
    public int NumPeriods { get; set; }
    public decimal AvgAccuracy { get; set; }
    public decimal AvgSharpe { get; set; }
    public decimal BestAccuracy { get; set; }
    public decimal WorstAccuracy { get; set; }
    public decimal Efficiency { get; set; }
}

public record GapAnalysisResult
{
    public decimal AccuracyGap { get; set; }
    public decimal SharpeGap { get; set; }
    public string Trend { get; set; } = "stable";
    public decimal OverfittingScore { get; set; }
    public decimal DegradationPrediction { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public bool IsSafeToDeply { get; set; }
}

public record TestingPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestingConfig Config { get; set; } = new();
}

#endregion
