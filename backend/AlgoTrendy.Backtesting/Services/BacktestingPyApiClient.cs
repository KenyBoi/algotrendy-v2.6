using System.Net.Http.Json;
using System.Text.Json;
using AlgoTrendy.Backtesting.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// HTTP client for communicating with Backtesting.py microservice
/// </summary>
public class BacktestingPyApiClient : IBacktestingPyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BacktestingPyApiClient> _logger;
    private readonly string _serviceUrl;

    public BacktestingPyApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BacktestingPyApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceUrl = configuration["BacktestingPyService:Url"] ?? "http://localhost:5004";

        _httpClient.BaseAddress = new Uri(_serviceUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Backtests can take longer
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
            _logger.LogWarning(ex, "Backtesting.py service health check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<StrategyInfo>?> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/strategies", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get strategies: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<StrategyListResponse>(cancellationToken: cancellationToken);
            return result?.Strategies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting strategies from Backtesting.py service");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<BacktestResults?> RunBacktestAsync(BacktestConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert config to Backtesting.py format
            var request = new
            {
                symbol = config.Symbol,
                start_date = config.StartDate.ToString("yyyy-MM-dd"),
                end_date = config.EndDate.ToString("yyyy-MM-dd"),
                timeframe = MapTimeframe(config.Timeframe),
                strategy = "sma", // Default to SMA, can be made configurable
                initial_capital = (double)config.InitialCapital,
                commission = (double)config.Commission,
                strategy_params = ExtractStrategyParams(config.Indicators)
            };

            _logger.LogInformation(
                "Running backtest on Backtesting.py: {Symbol} from {Start} to {End}",
                config.Symbol, config.StartDate, config.EndDate);

            var response = await _httpClient.PostAsJsonAsync("/backtest/run", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Backtest failed: {StatusCode} - {Error}", response.StatusCode, error);
                return CreateErrorResult(config, $"Backtest failed: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<BacktestingPyResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                _logger.LogError("Received null response from Backtesting.py service");
                return CreateErrorResult(config, "Null response from service");
            }

            // Convert to AlgoTrendy format
            return ConvertToBacktestResults(result, config);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Backtesting.py service");
            return CreateErrorResult(config, $"Connection error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Backtesting.py service request timed out");
            return CreateErrorResult(config, "Request timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during backtest");
            return CreateErrorResult(config, $"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BacktestResults?> GetResultsAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/backtest/results/{backtestId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get results for backtest {Id}: {StatusCode}", backtestId, response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<BacktestingPyResponse>(cancellationToken: cancellationToken);
            return result != null ? ConvertToBacktestResults(result, null) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting results for backtest {Id}", backtestId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<BacktestSummary>?> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/backtest/history", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get backtest history: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<HistoryResponse>(cancellationToken: cancellationToken);
            return result?.Backtests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backtest history");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteBacktestAsync(string backtestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/backtest/{backtestId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backtest {Id}", backtestId);
            return false;
        }
    }

    private static string MapTimeframe(TimeframeType timeframe)
    {
        return timeframe switch
        {
            TimeframeType.Minute => "minute",
            TimeframeType.Hour => "hour",
            TimeframeType.Day => "day",
            TimeframeType.Week => "week",
            TimeframeType.Month => "month",
            _ => "day"
        };
    }

    private static Dictionary<string, object> ExtractStrategyParams(Dictionary<string, IndicatorConfig> indicators)
    {
        var params_ = new Dictionary<string, object>();

        // Extract SMA parameters
        if (indicators.TryGetValue("SMA", out var sma) && sma.Enabled)
        {
            if (sma.Parameters.TryGetValue("fast_period", out var fast))
                params_["fast_period"] = Convert.ToInt32(fast);
            if (sma.Parameters.TryGetValue("slow_period", out var slow))
                params_["slow_period"] = Convert.ToInt32(slow);
        }

        // Extract RSI parameters
        if (indicators.TryGetValue("RSI", out var rsi) && rsi.Enabled)
        {
            if (rsi.Parameters.TryGetValue("period", out var period))
                params_["rsi_period"] = Convert.ToInt32(period);
            if (rsi.Parameters.TryGetValue("oversold", out var oversold))
                params_["oversold"] = Convert.ToInt32(oversold);
            if (rsi.Parameters.TryGetValue("overbought", out var overbought))
                params_["overbought"] = Convert.ToInt32(overbought);
        }

        return params_;
    }

    private BacktestResults ConvertToBacktestResults(BacktestingPyResponse response, BacktestConfig? config)
    {
        return new BacktestResults
        {
            BacktestId = response.BacktestId,
            Status = response.Status == "completed" ? BacktestStatus.Completed : BacktestStatus.Failed,
            Config = config!,
            StartedAt = DateTime.UtcNow, // Approximate
            CompletedAt = DateTime.TryParse(response.Timestamp, out var ts) ? ts : DateTime.UtcNow,
            ExecutionTimeSeconds = 0, // Not provided by Python service
            Metrics = new BacktestMetrics
            {
                TotalReturn = (decimal)response.Metrics.TotalReturn,
                AnnualReturn = (decimal)response.Metrics.AnnualReturn,
                SharpeRatio = (decimal)response.Metrics.SharpeRatio,
                SortinoRatio = (decimal)response.Metrics.SortinoRatio,
                MaxDrawdown = (decimal)response.Metrics.MaxDrawdown,
                WinRate = (decimal)response.Metrics.WinRate,
                ProfitFactor = (decimal)response.Metrics.ProfitFactor,
                TotalTrades = response.Metrics.TotalTrades,
                WinningTrades = 0, // Calculate from win rate
                LosingTrades = 0,
                AverageWin = 0,
                AverageLoss = 0,
                LargestWin = (decimal)response.Metrics.BestTrade,
                LargestLoss = (decimal)response.Metrics.WorstTrade,
                AverageTradeDuration = 0
            },
            EquityCurve = new List<EquityPoint>(), // Simplified for now
            Trades = new List<TradeResult>(), // Simplified for now
            IndicatorsUsed = new List<string>(),
            Metadata = new Dictionary<string, object>
            {
                ["engine"] = response.Engine,
                ["data_points"] = response.DataPoints.ToString(),
                ["symbol"] = response.Symbol,
                ["strategy"] = response.Strategy,
                ["final_equity"] = response.FinalEquity.ToString()
            }
        };
    }

    private BacktestResults CreateErrorResult(BacktestConfig config, string errorMessage)
    {
        return new BacktestResults
        {
            BacktestId = Guid.NewGuid().ToString(),
            Status = BacktestStatus.Failed,
            Config = config,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            ErrorDetails = new Dictionary<string, object>
            {
                ["source"] = "BacktestingPyApiClient"
            }
        };
    }

    // Response DTOs for JSON deserialization
    private class StrategyListResponse
    {
        public List<StrategyInfo>? Strategies { get; set; }
    }

    private class BacktestingPyResponse
    {
        public string BacktestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string Timeframe { get; set; } = string.Empty;
        public double InitialCapital { get; set; }
        public double FinalEquity { get; set; }
        public MetricsDto Metrics { get; set; } = new();
        public string Timestamp { get; set; } = string.Empty;
        public int DataPoints { get; set; }
        public string Engine { get; set; } = string.Empty;
    }

    private class MetricsDto
    {
        public double TotalReturn { get; set; }
        public double AnnualReturn { get; set; }
        public double SharpeRatio { get; set; }
        public double SortinoRatio { get; set; }
        public double MaxDrawdown { get; set; }
        public double WinRate { get; set; }
        public int TotalTrades { get; set; }
        public double AvgTrade { get; set; }
        public double BestTrade { get; set; }
        public double WorstTrade { get; set; }
        public double ProfitFactor { get; set; }
    }

    private class HistoryResponse
    {
        public int Count { get; set; }
        public List<BacktestSummary>? Backtests { get; set; }
    }
}
