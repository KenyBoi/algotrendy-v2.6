using AlgoTrendy.MultiCharts.Models;
using Newtonsoft.Json;

namespace AlgoTrendy.MultiCharts.Tests.Models;

public class ModelSerializationTests
{
    [Fact]
    public void BacktestRequest_SerializesAndDeserializes()
    {
        // Arrange
        var request = new BacktestRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            Timeframe = "1D",
            InitialCapital = 10000m,
            Parameters = new Dictionary<string, object>
            {
                { "FastPeriod", 10 },
                { "SlowPeriod", 30 }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<BacktestRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.StrategyName, deserialized.StrategyName);
        Assert.Equal(request.Symbol, deserialized.Symbol);
        Assert.Equal(request.InitialCapital, deserialized.InitialCapital);
        Assert.Equal(request.Parameters.Count, deserialized.Parameters.Count);
    }

    [Fact]
    public void BacktestResult_SerializesAndDeserializes()
    {
        // Arrange
        var result = new BacktestResult
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            NetProfit = 2456.78m,
            TotalTrades = 45,
            WinRate = 0.62m,
            SharpeRatio = 1.85m,
            MaxDrawdown = -15.3m,
            ProfitFactor = 1.92m,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var json = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<BacktestResult>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(result.StrategyName, deserialized.StrategyName);
        Assert.Equal(result.Symbol, deserialized.Symbol);
        Assert.Equal(result.NetProfit, deserialized.NetProfit);
        Assert.Equal(result.TotalTrades, deserialized.TotalTrades);
        Assert.Equal(result.WinRate, deserialized.WinRate);
    }

    [Fact]
    public void WalkForwardRequest_SerializesAndDeserializes()
    {
        // Arrange
        var request = new WalkForwardRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            InSamplePeriodDays = 180,
            OutOfSamplePeriodDays = 60,
            StepDays = 30,
            ParametersToOptimize = new Dictionary<string, ParameterRange>
            {
                { "FastPeriod", new ParameterRange { Start = 5m, Stop = 20m, Step = 1m } }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<WalkForwardRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.StrategyName, deserialized.StrategyName);
        Assert.Equal(request.InSamplePeriodDays, deserialized.InSamplePeriodDays);
        Assert.Equal(request.OutOfSamplePeriodDays, deserialized.OutOfSamplePeriodDays);
    }

    [Fact]
    public void MonteCarloRequest_SerializesAndDeserializes()
    {
        // Arrange
        var request = new MonteCarloRequest
        {
            StrategyName = "SMA_Crossover",
            Symbol = "BTCUSDT",
            FromDate = DateTime.Parse("2024-01-01"),
            ToDate = DateTime.Parse("2025-01-01"),
            NumberOfRuns = 1000,
            Parameters = new Dictionary<string, object>
            {
                { "FastPeriod", 10 }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<MonteCarloRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.StrategyName, deserialized.StrategyName);
        Assert.Equal(request.NumberOfRuns, deserialized.NumberOfRuns);
    }

    [Fact]
    public void MonteCarloResult_SerializesAndDeserializes()
    {
        // Arrange
        var result = new MonteCarloResult
        {
            MeanReturn = 24.5m,
            MedianReturn = 22.3m,
            StdDeviation = 8.9m,
            ProbabilityOfProfit = 0.68m,
            MeanMaxDrawdown = -18.2m
        };

        // Act
        var json = JsonConvert.SerializeObject(result);
        var deserialized = JsonConvert.DeserializeObject<MonteCarloResult>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(result.MeanReturn, deserialized.MeanReturn);
        Assert.Equal(result.MedianReturn, deserialized.MedianReturn);
        Assert.Equal(result.ProbabilityOfProfit, deserialized.ProbabilityOfProfit);
    }

    [Fact]
    public void Trade_SerializesAndDeserializes()
    {
        // Arrange
        var trade = new Trade
        {
            EntryTime = DateTime.Parse("2024-06-15 10:30:00"),
            ExitTime = DateTime.Parse("2024-06-15 14:30:00"),
            Side = "Long",
            EntryPrice = 65000m,
            ExitPrice = 66500m,
            Quantity = 0.1m,
            ProfitLoss = 150m
        };

        // Act
        var json = JsonConvert.SerializeObject(trade);
        var deserialized = JsonConvert.DeserializeObject<Trade>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(trade.Side, deserialized.Side);
        Assert.Equal(trade.EntryPrice, deserialized.EntryPrice);
        Assert.Equal(trade.ExitPrice, deserialized.ExitPrice);
        Assert.Equal(trade.ProfitLoss, deserialized.ProfitLoss);
    }

    [Fact]
    public void ScanRequest_SerializesAndDeserializes()
    {
        // Arrange
        var request = new ScanRequest
        {
            ScanName = "RSI_Oversold",
            Symbols = new List<string> { "BTCUSDT", "ETHUSDT" },
            ScanFormula = "RSI(14) < 30",
            Timeframe = "1D"
        };

        // Act
        var json = JsonConvert.SerializeObject(request);
        var deserialized = JsonConvert.DeserializeObject<ScanRequest>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(request.ScanName, deserialized.ScanName);
        Assert.Equal(request.Symbols.Count, deserialized.Symbols.Count);
        Assert.Equal(request.ScanFormula, deserialized.ScanFormula);
    }

    [Fact]
    public void OHLCVData_SerializesAndDeserializes()
    {
        // Arrange
        var data = new OHLCVData
        {
            Time = DateTime.Parse("2024-06-15"),
            Open = 65000m,
            High = 66000m,
            Low = 64500m,
            Close = 65800m,
            Volume = 1500000m
        };

        // Act
        var json = JsonConvert.SerializeObject(data);
        var deserialized = JsonConvert.DeserializeObject<OHLCVData>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(data.Open, deserialized.Open);
        Assert.Equal(data.High, deserialized.High);
        Assert.Equal(data.Low, deserialized.Low);
        Assert.Equal(data.Close, deserialized.Close);
        Assert.Equal(data.Volume, deserialized.Volume);
    }
}
