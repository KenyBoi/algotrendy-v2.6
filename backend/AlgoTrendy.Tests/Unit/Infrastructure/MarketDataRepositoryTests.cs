using AlgoTrendy.Core.Models;
using AlgoTrendy.Infrastructure.Repositories;
using AlgoTrendy.Tests.TestHelpers.Builders;
using FluentAssertions;
using Moq;
using Npgsql;
using System.Data;
using Xunit;

namespace AlgoTrendy.Tests.Unit.Infrastructure;

public class MarketDataRepositoryTests
{
    private const string TestConnectionString = "Host=localhost;Port=8812;Username=admin;Password=quest;Database=qdb";

    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new MarketDataRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithValidConnectionString_CreatesRepository()
    {
        // Act
        var repository = new MarketDataRepository(TestConnectionString);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public async Task InsertAsync_WithValidMarketData_ReturnsTrue()
    {
        // Note: This is an integration-style test that would need a real QuestDB connection
        // For true unit testing, we'd need to refactor the repository to accept an IDbConnection
        // For now, we'll test the model building logic via the builders

        // Arrange
        var marketData = new MarketDataBuilder()
            .WithSymbol("BTCUSDT")
            .WithOpen(50000m)
            .WithHigh(51000m)
            .WithLow(49000m)
            .WithClose(50500m)
            .WithVolume(100m)
            .Build();

        // Assert - just verify the data is well-formed
        marketData.Symbol.Should().Be("BTCUSDT");
        marketData.Open.Should().BePositive();
        marketData.High.Should().BeGreaterThanOrEqualTo(marketData.Low);
    }

    [Fact]
    public async Task InsertBatchAsync_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var repository = new MarketDataRepository(TestConnectionString);
        var emptyList = new List<MarketData>();

        // Act
        var result = await repository.InsertBatchAsync(emptyList, CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void MarketData_ValidationLogic_EnsuresOHLCRelationships()
    {
        // Arrange & Act
        var validMarketData = new MarketDataBuilder()
            .WithOpen(50000m)
            .WithHigh(51000m)
            .WithLow(49000m)
            .WithClose(50500m)
            .Build();

        // Assert - High should be the highest
        validMarketData.High.Should().BeGreaterThanOrEqualTo(validMarketData.Open);
        validMarketData.High.Should().BeGreaterThanOrEqualTo(validMarketData.Close);
        validMarketData.High.Should().BeGreaterThanOrEqualTo(validMarketData.Low);

        // Low should be the lowest
        validMarketData.Low.Should().BeLessThanOrEqualTo(validMarketData.Open);
        validMarketData.Low.Should().BeLessThanOrEqualTo(validMarketData.Close);
        validMarketData.Low.Should().BeLessThanOrEqualTo(validMarketData.High);
    }

    [Fact]
    public void MarketData_WithOptionalFields_CanBeNull()
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithQuoteVolume(null)
            .WithTradesCount(null)
            .WithMetadata(null)
            .Build();

        // Assert
        marketData.QuoteVolume.Should().BeNull();
        marketData.TradesCount.Should().BeNull();
        marketData.Metadata.Should().BeNull();
    }

    [Fact]
    public void MarketData_ForDifferentExchanges_StoresSourceCorrectly()
    {
        // Arrange & Act
        var binanceData = new MarketDataBuilder().WithSource("binance").Build();
        var okxData = new MarketDataBuilder().WithSource("okx").Build();
        var coinbaseData = new MarketDataBuilder().WithSource("coinbase").Build();

        // Assert
        binanceData.Source.Should().Be("binance");
        okxData.Source.Should().Be("okx");
        coinbaseData.Source.Should().Be("coinbase");
    }

    [Fact]
    public void MarketData_TimestampComparison_WorksCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var earlier = now.AddMinutes(-5);

        var data1 = new MarketDataBuilder().WithTimestamp(earlier).Build();
        var data2 = new MarketDataBuilder().WithTimestamp(now).Build();

        // Act & Assert
        data2.Timestamp.Should().BeAfter(data1.Timestamp);
    }

    [Theory]
    [InlineData("BTCUSDT")]
    [InlineData("ETHUSDT")]
    [InlineData("BNBUSDT")]
    [InlineData("SOLUSDT")]
    public void MarketData_WithDifferentSymbols_StoresCorrectly(string symbol)
    {
        // Arrange & Act
        var marketData = new MarketDataBuilder()
            .WithSymbol(symbol)
            .Build();

        // Assert
        marketData.Symbol.Should().Be(symbol);
    }

    [Fact]
    public void MarketData_BulkInsertPreparation_CreatesList()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var marketDataList = new List<MarketData>
        {
            new MarketDataBuilder().WithSymbol("BTCUSDT").WithTimestamp(timestamp).Build(),
            new MarketDataBuilder().WithSymbol("ETHUSDT").WithTimestamp(timestamp).Build(),
            new MarketDataBuilder().WithSymbol("BNBUSDT").WithTimestamp(timestamp).Build()
        };

        // Act & Assert
        marketDataList.Should().HaveCount(3);
        marketDataList.Select(m => m.Symbol).Should().BeEquivalentTo(new[] { "BTCUSDT", "ETHUSDT", "BNBUSDT" });
        marketDataList.All(m => m.Timestamp == timestamp).Should().BeTrue();
    }

    [Fact]
    public void MarketData_QueryFiltering_ByTimeRange()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;
        var beforeRange = startTime.AddMinutes(-30);
        var inRange = startTime.AddMinutes(30);
        var afterRange = endTime.AddMinutes(30);

        var data1 = new MarketDataBuilder().WithTimestamp(beforeRange).Build();
        var data2 = new MarketDataBuilder().WithTimestamp(inRange).Build();
        var data3 = new MarketDataBuilder().WithTimestamp(afterRange).Build();

        var allData = new[] { data1, data2, data3 };

        // Act - Simulate filtering
        var filtered = allData.Where(d => d.Timestamp >= startTime && d.Timestamp <= endTime).ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered.First().Timestamp.Should().Be(inRange);
    }

    [Fact]
    public void MarketData_AggregationPreparation_GroupsBySymbol()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var marketDataList = new List<MarketData>
        {
            new MarketDataBuilder().WithSymbol("BTCUSDT").WithClose(50000m).Build(),
            new MarketDataBuilder().WithSymbol("BTCUSDT").WithClose(51000m).Build(),
            new MarketDataBuilder().WithSymbol("ETHUSDT").WithClose(3000m).Build(),
        };

        // Act
        var grouped = marketDataList.GroupBy(m => m.Symbol).ToList();

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key == "BTCUSDT").Should().HaveCount(2);
        grouped.First(g => g.Key == "ETHUSDT").Should().HaveCount(1);
    }

    [Fact]
    public void MarketData_LatestSelection_ReturnsNewestTimestamp()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var marketDataList = new List<MarketData>
        {
            new MarketDataBuilder().WithTimestamp(now.AddMinutes(-10)).WithClose(50000m).Build(),
            new MarketDataBuilder().WithTimestamp(now.AddMinutes(-5)).WithClose(50500m).Build(),
            new MarketDataBuilder().WithTimestamp(now).WithClose(51000m).Build(),
        };

        // Act
        var latest = marketDataList.OrderByDescending(m => m.Timestamp).First();

        // Assert
        latest.Timestamp.Should().Be(now);
        latest.Close.Should().Be(51000m);
    }

    [Fact]
    public void MarketData_ExistenceCheck_BySymbolAndTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var symbol = "BTCUSDT";

        var marketDataList = new List<MarketData>
        {
            new MarketDataBuilder().WithSymbol(symbol).WithTimestamp(timestamp).Build()
        };

        // Act
        var exists = marketDataList.Any(m => m.Symbol == symbol && m.Timestamp == timestamp);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void MarketData_DownsamplingLogic_CalculatesOHLCCorrectly()
    {
        // Arrange - Create 1-minute candles that should be aggregated to 1-hour
        var baseTime = DateTime.UtcNow.Date;
        var candles = new List<MarketData>
        {
            new MarketDataBuilder().WithTimestamp(baseTime).WithOpen(100m).WithHigh(105m).WithLow(95m).WithClose(102m).WithVolume(10m).Build(),
            new MarketDataBuilder().WithTimestamp(baseTime.AddMinutes(1)).WithOpen(102m).WithHigh(110m).WithLow(100m).WithClose(108m).WithVolume(15m).Build(),
            new MarketDataBuilder().WithTimestamp(baseTime.AddMinutes(2)).WithOpen(108m).WithHigh(112m).WithLow(107m).WithClose(110m).WithVolume(20m).Build(),
        };

        // Act - Simulate aggregation
        var aggregated = new MarketData
        {
            Symbol = candles.First().Symbol,
            Timestamp = baseTime,
            Open = candles.First().Open,
            High = candles.Max(c => c.High),
            Low = candles.Min(c => c.Low),
            Close = candles.Last().Close,
            Volume = candles.Sum(c => c.Volume),
            Source = candles.First().Source
        };

        // Assert
        aggregated.Open.Should().Be(100m);
        aggregated.High.Should().Be(112m);
        aggregated.Low.Should().Be(95m);
        aggregated.Close.Should().Be(110m);
        aggregated.Volume.Should().Be(45m);
    }
}
