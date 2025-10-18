using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AlgoTrendy.Infrastructure.Repositories;

/// <summary>
/// QuestDB implementation of the market data repository
/// Uses PostgreSQL wire protocol for querying
/// </summary>
public class MarketDataRepository : IMarketDataRepository
{
    private readonly string _connectionString;

    public MarketDataRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<bool> InsertAsync(MarketData marketData, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO market_data_1m (
                symbol, timestamp, open, high, low, close,
                volume, quote_volume, trades_count, source, metadata_json
            ) VALUES (
                @symbol, cast(@timestamp as timestamp), @open, @high, @low, @close,
                @volume, @quoteVolume, @tradesCount, @source, @metadata
            )";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", marketData.Symbol);
        command.Parameters.AddWithValue("timestamp", marketData.Timestamp);
        command.Parameters.AddWithValue("open", (double)marketData.Open);
        command.Parameters.AddWithValue("high", (double)marketData.High);
        command.Parameters.AddWithValue("low", (double)marketData.Low);
        command.Parameters.AddWithValue("close", (double)marketData.Close);
        command.Parameters.AddWithValue("volume", (double)marketData.Volume);
        command.Parameters.AddWithValue("quoteVolume", marketData.QuoteVolume.HasValue ? (double)marketData.QuoteVolume.Value : DBNull.Value);
        command.Parameters.AddWithValue("tradesCount", marketData.TradesCount.HasValue ? marketData.TradesCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("source", marketData.Source);
        command.Parameters.AddWithValue("metadata", (object?)marketData.Metadata ?? DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<int> InsertBatchAsync(IEnumerable<MarketData> marketDataList, CancellationToken cancellationToken = default)
    {
        var dataList = marketDataList.ToList();
        if (!dataList.Any())
            return 0;

        const string sql = @"
            INSERT INTO market_data_1m (
                symbol, timestamp, open, high, low, close,
                volume, quote_volume, trades_count, source, metadata_json
            ) VALUES (
                @symbol, cast(@timestamp as timestamp), @open, @high, @low, @close,
                @volume, @quoteVolume, @tradesCount, @source, @metadata
            )";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        int totalInserted = 0;

        foreach (var marketData in dataList)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("symbol", marketData.Symbol);
            command.Parameters.AddWithValue("timestamp", marketData.Timestamp);
            command.Parameters.AddWithValue("open", (double)marketData.Open);
            command.Parameters.AddWithValue("high", (double)marketData.High);
            command.Parameters.AddWithValue("low", (double)marketData.Low);
            command.Parameters.AddWithValue("close", (double)marketData.Close);
            command.Parameters.AddWithValue("volume", (double)marketData.Volume);
            command.Parameters.AddWithValue("quoteVolume", marketData.QuoteVolume.HasValue ? (double)marketData.QuoteVolume.Value : DBNull.Value);
            command.Parameters.AddWithValue("tradesCount", marketData.TradesCount.HasValue ? marketData.TradesCount.Value : DBNull.Value);
            command.Parameters.AddWithValue("source", marketData.Source);
            command.Parameters.AddWithValue("metadata", (object?)marketData.Metadata ?? DBNull.Value);

            totalInserted += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return totalInserted;
    }

    public async Task<IEnumerable<MarketData>> GetBySymbolAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT symbol, timestamp, open, high, low, close,
                   volume, quote_volume, trades_count, source, metadata_json
            FROM market_data_1m
            WHERE symbol = @symbol
              AND timestamp >= cast(@startTime as timestamp)
              AND timestamp <= cast(@endTime as timestamp)
            ORDER BY timestamp ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);
        command.Parameters.AddWithValue("startTime", startTime);
        command.Parameters.AddWithValue("endTime", endTime);

        var marketDataList = new List<MarketData>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            marketDataList.Add(MapToMarketData(reader));
        }

        return marketDataList;
    }

    public async Task<MarketData?> GetLatestAsync(string symbol, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT symbol, timestamp, open, high, low, close,
                   volume, quote_volume, trades_count, source, metadata_json
            FROM market_data_1m
            WHERE symbol = @symbol
            LATEST ON timestamp PARTITION BY symbol";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToMarketData(reader);
        }

        return null;
    }

    public async Task<IReadOnlyDictionary<string, MarketData>> GetLatestBatchAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.ToList();
        if (!symbolList.Any())
            return new Dictionary<string, MarketData>();

        const string sql = @"
            SELECT symbol, timestamp, open, high, low, close,
                   volume, quote_volume, trades_count, source, metadata_json
            FROM market_data_1m
            WHERE symbol = ANY(@symbols)
            LATEST ON timestamp PARTITION BY symbol";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbols", symbolList.ToArray());

        var result = new Dictionary<string, MarketData>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var marketData = MapToMarketData(reader);
            result[marketData.Symbol] = marketData;
        }

        return result;
    }

    public async Task<IEnumerable<MarketData>> GetAggregatedAsync(
        string symbol,
        string interval,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        // Convert interval to QuestDB sample by format
        var sampleBy = interval switch
        {
            "1h" => "1h",
            "1d" => "1d",
            "1w" => "7d",
            _ => throw new ArgumentException($"Unsupported interval: {interval}", nameof(interval))
        };

        var sql = $@"
            SELECT
                symbol,
                timestamp,
                first(open) as open,
                max(high) as high,
                min(low) as low,
                last(close) as close,
                sum(volume) as volume,
                sum(quote_volume) as quote_volume,
                sum(trades_count) as trades_count,
                first(source) as source,
                null as metadata_json
            FROM market_data_1m
            WHERE symbol = @symbol
              AND timestamp >= cast(@startTime as timestamp)
              AND timestamp <= cast(@endTime as timestamp)
            SAMPLE BY {sampleBy} ALIGN TO CALENDAR
            ORDER BY timestamp ASC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);
        command.Parameters.AddWithValue("startTime", startTime);
        command.Parameters.AddWithValue("endTime", endTime);

        var marketDataList = new List<MarketData>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            marketDataList.Add(MapToMarketData(reader));
        }

        return marketDataList;
    }

    public async Task<bool> ExistsAsync(string symbol, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM market_data_1m
            WHERE symbol = @symbol
              AND timestamp = cast(@timestamp as timestamp)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);
        command.Parameters.AddWithValue("timestamp", timestamp);

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return count > 0;
    }

    private static MarketData MapToMarketData(NpgsqlDataReader reader)
    {
        return new MarketData
        {
            Symbol = reader.GetString(0),
            Timestamp = reader.GetDateTime(1),
            Open = (decimal)reader.GetDouble(2),
            High = (decimal)reader.GetDouble(3),
            Low = (decimal)reader.GetDouble(4),
            Close = (decimal)reader.GetDouble(5),
            Volume = (decimal)reader.GetDouble(6),
            QuoteVolume = reader.IsDBNull(7) ? null : (decimal)reader.GetDouble(7),
            TradesCount = reader.IsDBNull(8) ? null : reader.GetInt64(8),
            Source = reader.GetString(9),
            Metadata = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
    }
}
