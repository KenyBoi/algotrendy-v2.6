using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlgoTrendy.Examples;

/// <summary>
/// AlgoTrendy C# API Client
/// Usage example for interacting with AlgoTrendy v2.6 API
/// </summary>
public class AlgoTrendyClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    /// <summary>
    /// Initialize AlgoTrendy client
    /// </summary>
    /// <param name="baseUrl">API base URL (e.g., "http://localhost:5002")</param>
    /// <param name="apiKey">Optional API key for authentication</param>
    public AlgoTrendyClient(string baseUrl, string? apiKey = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };

        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    }

    // Health & Status

    /// <summary>
    /// Get API health status
    /// </summary>
    public async Task<HealthResponse> GetHealthAsync()
    {
        return await _httpClient.GetFromJsonAsync<HealthResponse>("/health")
            ?? throw new Exception("Failed to get health status");
    }

    /// <summary>
    /// Get detailed health status with all checks
    /// </summary>
    public async Task<DetailedHealthResponse> GetDetailedHealthAsync()
    {
        return await _httpClient.GetFromJsonAsync<DetailedHealthResponse>("/api/health/detailed")
            ?? throw new Exception("Failed to get detailed health");
    }

    // Trading Operations

    /// <summary>
    /// Get account balance
    /// </summary>
    /// <param name="exchange">Exchange name (bybit, binance, etc.)</param>
    /// <param name="currency">Currency symbol (default: USDT)</param>
    public async Task<BalanceResponse> GetBalanceAsync(string exchange, string currency = "USDT")
    {
        var response = await _httpClient.GetFromJsonAsync<BalanceResponse>(
            $"/api/trading/balance?exchange={exchange}&currency={currency}");
        return response ?? throw new Exception("Failed to get balance");
    }

    /// <summary>
    /// Get all open positions
    /// </summary>
    /// <param name="exchange">Exchange name</param>
    public async Task<List<Position>> GetPositionsAsync(string exchange)
    {
        var response = await _httpClient.GetFromJsonAsync<List<Position>>(
            $"/api/trading/positions?exchange={exchange}");
        return response ?? new List<Position>();
    }

    /// <summary>
    /// Place a trading order
    /// </summary>
    /// <param name="order">Order request</param>
    /// <returns>Order result</returns>
    /// <example>
    /// // Market buy
    /// var order = await client.PlaceOrderAsync(new OrderRequest
    /// {
    ///     Exchange = "bybit",
    ///     Symbol = "BTCUSDT",
    ///     Side = "Buy",
    ///     Type = "Market",
    ///     Quantity = 0.001m
    /// });
    ///
    /// // Limit sell
    /// var order = await client.PlaceOrderAsync(new OrderRequest
    /// {
    ///     Exchange = "bybit",
    ///     Symbol = "ETHUSDT",
    ///     Side = "Sell",
    ///     Type = "Limit",
    ///     Quantity = 0.1m,
    ///     Price = 3000.00m
    /// });
    /// </example>
    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest order)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/trading/order", order);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderResponse>()
            ?? throw new Exception("Failed to place order");
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    public async Task<CancelOrderResponse> CancelOrderAsync(string exchange, string orderId)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/trading/order/cancel",
            new { Exchange = exchange, OrderId = orderId });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CancelOrderResponse>()
            ?? throw new Exception("Failed to cancel order");
    }

    // Market Data

    /// <summary>
    /// Get historical market data
    /// </summary>
    public async Task<List<Candle>> GetMarketDataAsync(
        string symbol,
        string exchange,
        string interval = "1h",
        int limit = 100)
    {
        var response = await _httpClient.GetFromJsonAsync<List<Candle>>(
            $"/api/marketdata?symbol={symbol}&exchange={exchange}&interval={interval}&limit={limit}");
        return response ?? new List<Candle>();
    }

    // Backtesting

    /// <summary>
    /// Run a backtest
    /// </summary>
    /// <example>
    /// var results = await client.RunBacktestAsync(new BacktestRequest
    /// {
    ///     Strategy = "EMA Cross",
    ///     Symbols = new[] { "BTCUSDT" },
    ///     StartDate = "2024-01-01",
    ///     EndDate = "2024-12-31",
    ///     InitialCapital = 10000
    /// });
    /// </example>
    public async Task<BacktestResponse> RunBacktestAsync(BacktestRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/backtest/run", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BacktestResponse>()
            ?? throw new Exception("Failed to run backtest");
    }

    /// <summary>
    /// Get backtest results by ID
    /// </summary>
    public async Task<BacktestResults> GetBacktestResultsAsync(string backtestId)
    {
        return await _httpClient.GetFromJsonAsync<BacktestResults>($"/api/backtest/results/{backtestId}")
            ?? throw new Exception("Failed to get backtest results");
    }

    // Metrics & Monitoring

    /// <summary>
    /// Get application metrics
    /// </summary>
    public async Task<MetricsResponse> GetMetricsAsync()
    {
        return await _httpClient.GetFromJsonAsync<MetricsResponse>("/api/metrics")
            ?? throw new Exception("Failed to get metrics");
    }

    /// <summary>
    /// Get metrics summary
    /// </summary>
    public async Task<MetricsSummaryResponse> GetMetricsSummaryAsync()
    {
        return await _httpClient.GetFromJsonAsync<MetricsSummaryResponse>("/api/metrics/summary")
            ?? throw new Exception("Failed to get metrics summary");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Response Models

public record HealthResponse(string Status, DateTime Timestamp, string Version);

public record DetailedHealthResponse(string Status, DateTime Timestamp, double Duration, object Checks, object Info);

public record BalanceResponse(decimal Balance, string Currency, string Exchange, bool Testnet);

public record Position(string Symbol, string Side, decimal Quantity, decimal EntryPrice, decimal CurrentPrice, decimal UnrealizedPnL);

public record OrderRequest
{
    public required string Exchange { get; init; }
    public required string Symbol { get; init; }
    public required string Side { get; init; }
    public required string Type { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopPrice { get; init; }
    public string? ClientOrderId { get; init; }
}

public record OrderResponse(string OrderId, string Status, string Message, DateTime Timestamp);

public record CancelOrderResponse(string OrderId, string Status, string Message);

public record Candle(DateTime Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);

public record BacktestRequest
{
    public required string Strategy { get; init; }
    public required string[] Symbols { get; init; }
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public decimal InitialCapital { get; init; } = 10000;
    public string Engine { get; init; } = "auto";
}

public record BacktestResponse(string BacktestId, string Status, string Message);

public record BacktestResults(
    string BacktestId,
    string Status,
    decimal TotalReturn,
    decimal SharpeRatio,
    decimal MaxDrawdown,
    int TotalTrades);

public record MetricsResponse(DateTime Timestamp, object Metrics);

public record MetricsSummaryResponse(DateTime Timestamp, MetricsSummary Summary);

public record MetricsSummary(
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    double AverageDurationMs);

// Example Usage
public class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize client
        using var client = new AlgoTrendyClient("http://localhost:5002");

        try
        {
            // Check health
            Console.WriteLine("Checking API health...");
            var health = await client.GetHealthAsync();
            Console.WriteLine($"✅ API Status: {health.Status}");

            // Get balance (requires credentials configured)
            try
            {
                Console.WriteLine("\nGetting balance...");
                var balance = await client.GetBalanceAsync("bybit");
                Console.WriteLine($"💰 Balance: {balance.Balance} {balance.Currency}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Balance check failed (credentials may not be configured): {e.Message}");
            }

            // Get market data
            Console.WriteLine("\nGetting market data...");
            var candles = await client.GetMarketDataAsync("BTCUSDT", "binance", "1h", 10);
            Console.WriteLine($"📊 Retrieved {candles.Count} candles");
            if (candles.Count > 0)
            {
                var latest = candles[^1];
                Console.WriteLine($"   Latest: Open={latest.Open}, Close={latest.Close}");
            }

            // Place market order (TESTNET ONLY)
            // Uncomment to test (requires testnet credentials)
            /*
            Console.WriteLine("\nPlacing test order...");
            var order = await client.PlaceOrderAsync(new OrderRequest
            {
                Exchange = "bybit",
                Symbol = "BTCUSDT",
                Side = "Buy",
                Type = "Market",
                Quantity = 0.001m
            });
            Console.WriteLine($"✅ Order placed: {order.OrderId}");
            Console.WriteLine($"   Status: {order.Status}");
            */

            // Run backtest
            Console.WriteLine("\nRunning backtest...");
            try
            {
                var backtest = await client.RunBacktestAsync(new BacktestRequest
                {
                    Strategy = "RSI",
                    Symbols = new[] { "BTCUSDT" },
                    StartDate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd"),
                    EndDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    InitialCapital = 10000
                });
                Console.WriteLine($"📈 Backtest ID: {backtest.BacktestId}");
                Console.WriteLine($"   Status: {backtest.Status}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Backtest failed: {e.Message}");
            }

            // Get metrics
            Console.WriteLine("\nGetting metrics...");
            try
            {
                var metrics = await client.GetMetricsSummaryAsync();
                Console.WriteLine($"📊 Total Requests: {metrics.Summary.TotalRequests}");
                Console.WriteLine($"   Error Rate: {metrics.Summary.ErrorRate}%");
                Console.WriteLine($"   Avg Duration: {metrics.Summary.AverageDurationMs}ms");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️ Metrics not available: {e.Message}");
            }

            Console.WriteLine("\n✅ Example completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}
