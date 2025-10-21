using BenchmarkDotNet.Attributes;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Benchmarks;

/// <summary>
/// Benchmarks for monolith architecture (direct in-memory calls)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MonolithBenchmarks
{
    private OrderRequest _orderRequest = null!;
    private Position _position = null!;
    private List<MarketData> _marketDataBatch = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup test data
        _orderRequest = new OrderRequest
        {
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            Type = OrderType.Market,
            Quantity = 0.01m,
            StrategyId = "test-strategy"
        };

        _position = new Position
        {
            PositionId = Guid.NewGuid().ToString(),
            Symbol = "BTCUSDT",
            Side = OrderSide.Buy,
            Quantity = 0.01m,
            EntryPrice = 50000m,
            CurrentPrice = 51000m,
            UnrealizedPnL = 10m,
            Exchange = "bybit"
        };

        _marketDataBatch = Enumerable.Range(0, 100).Select(i => new MarketData
        {
            Symbol = "BTCUSDT",
            Price = 50000m + i,
            Volume = 1000m,
            Timestamp = DateTime.UtcNow.AddSeconds(-i),
            Source = "test"
        }).ToList();
    }

    [Benchmark(Description = "Order Validation (In-Process)")]
    public bool ValidateOrder()
    {
        // Simulate order validation logic
        return _orderRequest.Quantity > 0
            && !string.IsNullOrEmpty(_orderRequest.Symbol)
            && _orderRequest.Type != OrderType.Market || _orderRequest.Price.HasValue;
    }

    [Benchmark(Description = "Position PnL Calculation (In-Process)")]
    public decimal CalculatePositionPnL()
    {
        // Simulate PnL calculation
        var priceDiff = _position.CurrentPrice - _position.EntryPrice;
        var pnl = priceDiff * _position.Quantity;
        return _position.Side == OrderSide.Buy ? pnl : -pnl;
    }

    [Benchmark(Description = "Market Data Aggregation (In-Process)")]
    public decimal AggregateMarketData()
    {
        // Simulate market data aggregation
        return _marketDataBatch.Average(md => md.Price);
    }

    [Benchmark(Description = "Risk Check (In-Process)")]
    public bool RiskCheck()
    {
        // Simulate risk management check
        var maxPositionSize = 100m;
        var currentExposure = _position.Quantity * _position.CurrentPrice;
        var orderExposure = _orderRequest.Quantity * (_orderRequest.Price ?? 50000m);

        return currentExposure + orderExposure <= maxPositionSize;
    }

    [Benchmark(Description = "Object Serialization (In-Process)")]
    public string SerializeOrder()
    {
        // Simulate JSON serialization (common in APIs)
        return System.Text.Json.JsonSerializer.Serialize(_orderRequest);
    }

    [Benchmark(Description = "100 Orders Batch Processing")]
    public int ProcessOrderBatch()
    {
        int successCount = 0;
        for (int i = 0; i < 100; i++)
        {
            if (ValidateOrder())
            {
                successCount++;
            }
        }
        return successCount;
    }
}
