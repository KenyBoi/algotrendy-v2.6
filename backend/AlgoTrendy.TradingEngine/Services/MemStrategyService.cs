using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AlgoTrendy.Core.Models;

namespace AlgoTrendy.TradingEngine.Services
{
    /// <summary>
    /// Service for integrating with MEM Advanced Strategy API
    /// </summary>
    public class MemStrategyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MemStrategyService> _logger;
        private readonly string _apiBaseUrl;

        public MemStrategyService(HttpClient httpClient, ILogger<MemStrategyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiBaseUrl = Environment.GetEnvironmentVariable("MEM_STRATEGY_API_URL") ?? "http://localhost:5004";

            _logger.LogInformation("MemStrategyService initialized with API URL: {ApiUrl}", _apiBaseUrl);
        }

        /// <summary>
        /// Check if MEM Strategy API is healthy
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<HealthResponse>(
                    $"{_apiBaseUrl}/api/strategy/health");

                return response?.Status == "healthy";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MEM Strategy API health check failed");
                return false;
            }
        }

        /// <summary>
        /// Analyze market data and generate trading signal
        /// </summary>
        public async Task<MemTradingSignal?> AnalyzeAsync(
            string symbol,
            List<MarketData> data1h,
            List<MarketData>? data4h = null,
            List<MarketData>? data1d = null,
            decimal accountBalance = 10000m,
            StrategyConfig? config = null)
        {
            try
            {
                var request = new AnalyzeRequest
                {
                    Symbol = symbol,
                    Data1h = ConvertToApiFormat(data1h),
                    Data4h = data4h != null ? ConvertToApiFormat(data4h) : null,
                    Data1d = data1d != null ? ConvertToApiFormat(data1d) : null,
                    AccountBalance = accountBalance,
                    Config = config ?? new StrategyConfig()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/strategy/analyze",
                    request);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>();

                if (result?.Success == true && result.Signal != null)
                {
                    _logger.LogInformation(
                        "Generated {Action} signal for {Symbol} with {Confidence}% confidence",
                        result.Signal.Action,
                        symbol,
                        result.Signal.Confidence);

                    return result.Signal;
                }

                _logger.LogWarning("MEM Strategy API returned unsuccessful response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MEM Strategy API analyze endpoint");
                return null;
            }
        }

        /// <summary>
        /// Get comprehensive market analysis
        /// </summary>
        public async Task<MarketAnalysis?> GetMarketAnalysisAsync(List<MarketData> data)
        {
            try
            {
                var request = new
                {
                    data = ConvertToApiFormat(data)
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/strategy/market-analysis",
                    request);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<MarketAnalysisResponse>();

                if (result?.Success == true)
                {
                    return result.Analysis;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MEM Strategy API market-analysis endpoint");
                return null;
            }
        }

        /// <summary>
        /// Calculate specific indicators
        /// </summary>
        public async Task<Dictionary<string, object>?> CalculateIndicatorsAsync(
            List<MarketData> data,
            List<string> indicators)
        {
            try
            {
                var request = new
                {
                    data = ConvertToApiFormat(data),
                    indicators = indicators
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/strategy/indicators/calculate",
                    request);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<IndicatorsResponse>();

                return result?.Results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MEM Strategy API indicators/calculate endpoint");
                return null;
            }
        }

        /// <summary>
        /// Convert MarketData list to API format
        /// </summary>
        private object ConvertToApiFormat(List<MarketData> data)
        {
            var formattedData = new List<object>();

            foreach (var candle in data)
            {
                formattedData.Add(new
                {
                    timestamp = candle.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss"),
                    open = candle.Open,
                    high = candle.High,
                    low = candle.Low,
                    close = candle.Close,
                    volume = candle.Volume
                });
            }

            return new { data = formattedData };
        }

        #region Response Models

        private class HealthResponse
        {
            public string? Status { get; set; }
            public string? Service { get; set; }
            public string? Version { get; set; }
            public string? Timestamp { get; set; }
        }

        private class AnalyzeRequest
        {
            public string Symbol { get; set; } = string.Empty;
            public object Data1h { get; set; } = new { };
            public object? Data4h { get; set; }
            public object? Data1d { get; set; }
            public decimal AccountBalance { get; set; }
            public StrategyConfig Config { get; set; } = new();
        }

        private class AnalyzeResponse
        {
            public bool Success { get; set; }
            public string? Symbol { get; set; }
            public MemTradingSignal? Signal { get; set; }
            public string? Timestamp { get; set; }
            public string? Error { get; set; }
        }

        private class MarketAnalysisResponse
        {
            public bool Success { get; set; }
            public MarketAnalysis? Analysis { get; set; }
            public string? Timestamp { get; set; }
            public string? Error { get; set; }
        }

        private class IndicatorsResponse
        {
            public bool Success { get; set; }
            public Dictionary<string, object>? Results { get; set; }
            public string? Error { get; set; }
        }

        #endregion
    }

    #region Public Models

    /// <summary>
    /// Strategy configuration
    /// </summary>
    public class StrategyConfig
    {
        public decimal MinConfidence { get; set; } = 70.0m;
        public decimal MaxRiskPerTrade { get; set; } = 0.02m;
        public bool UseMultiTimeframe { get; set; } = true;
    }

    /// <summary>
    /// Trading signal from MEM Strategy
    /// </summary>
    public class MemTradingSignal
    {
        public string Action { get; set; } = string.Empty; // BUY, SELL, HOLD
        public string? Signal { get; set; } // BUY, SELL, NEUTRAL
        public decimal Confidence { get; set; }
        public decimal? EntryPrice { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? PositionSize { get; set; }
        public decimal? RiskAmount { get; set; }
        public decimal? RiskPercent { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public decimal? Atr { get; set; }
        public string? Trend { get; set; }
        public string? Volatility { get; set; }
        public string? RiskLevel { get; set; }
        public decimal? SharpeRatio { get; set; }
        public decimal? BbPosition { get; set; }
        public decimal? DistanceFromPivot { get; set; }
        public string? TimeframesAligned { get; set; }
        public string? Timestamp { get; set; }
        public List<string>? Reasoning { get; set; }
        public string? Reason { get; set; } // For HOLD signals
    }

    /// <summary>
    /// Comprehensive market analysis
    /// </summary>
    public class MarketAnalysis
    {
        public string? OverallSignal { get; set; }
        public decimal SignalStrength { get; set; }
        public string? TrendDirection { get; set; }
        public string? VolatilityLevel { get; set; }
        public decimal? TotalScore { get; set; }
        public List<string>? Reasoning { get; set; }
        public Dictionary<string, object>? Indicators { get; set; }
    }

    #endregion
}
