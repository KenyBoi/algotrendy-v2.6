using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Npgsql;
using Serilog;

namespace AlgoTrendy.Infrastructure.Repositories;

/// <summary>
/// Repository for margin-related operations against QuestDB
/// </summary>
public class MarginRepository : IMarginRepository
{
    private readonly string _connectionString;
    private const string TableName = "margin_positions";
    private const string MarginCallHistoryTableName = "margin_call_history";

    public MarginRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Gets all active margin positions
    /// </summary>
    public async Task<IEnumerable<Position>> GetAllMarginPositionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var positions = new List<Position>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT symbol, quantity, entry_price, current_price, leverage, margin_type,
                       collateral_amount, borrowed_amount, interest_rate, liquidation_price,
                       margin_health_ratio, position_id, exchange, side, stop_loss, take_profit,
                       strategy_id, open_order_id, opened_at, updated_at
                FROM positions
                WHERE leverage > 1.0 AND margin_type IS NOT NULL
                ORDER BY updated_at DESC;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                positions.Add(ReadPositionFromReader(reader));
            }

            return positions;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching all margin positions");
            return Enumerable.Empty<Position>();
        }
    }

    /// <summary>
    /// Gets margin positions for a specific symbol
    /// </summary>
    public async Task<IEnumerable<Position>> GetMarginPositionsBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var positions = new List<Position>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT symbol, quantity, entry_price, current_price, leverage, margin_type,
                       collateral_amount, borrowed_amount, interest_rate, liquidation_price,
                       margin_health_ratio, position_id, exchange, side, stop_loss, take_profit,
                       strategy_id, open_order_id, opened_at, updated_at
                FROM positions
                WHERE symbol = $1 AND leverage > 1.0 AND margin_type IS NOT NULL
                ORDER BY updated_at DESC;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", symbol);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                positions.Add(ReadPositionFromReader(reader));
            }

            return positions;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching margin positions for symbol {Symbol}", symbol);
            return Enumerable.Empty<Position>();
        }
    }

    /// <summary>
    /// Gets positions at risk of liquidation
    /// </summary>
    public async Task<IEnumerable<Position>> GetLiquidationRiskPositionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var positions = new List<Position>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT symbol, quantity, entry_price, current_price, leverage, margin_type,
                       collateral_amount, borrowed_amount, interest_rate, liquidation_price,
                       margin_health_ratio, position_id, exchange, side, stop_loss, take_profit,
                       strategy_id, open_order_id, opened_at, updated_at
                FROM positions
                WHERE margin_health_ratio IS NOT NULL AND margin_health_ratio < 0.05
                ORDER BY margin_health_ratio ASC;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                positions.Add(ReadPositionFromReader(reader));
            }

            return positions;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching liquidation risk positions");
            return Enumerable.Empty<Position>();
        }
    }

    /// <summary>
    /// Updates margin health for a position
    /// </summary>
    public async Task<bool> UpdateMarginHealthAsync(string positionId, decimal marginHealthRatio, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                UPDATE positions
                SET margin_health_ratio = $1, updated_at = NOW()
                WHERE position_id = $2;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", marginHealthRatio);
            cmd.Parameters.AddWithValue("$2", positionId);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Information("Updated margin health for position {PositionId} to {HealthRatio}", positionId, marginHealthRatio);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating margin health for position {PositionId}", positionId);
            return false;
        }
    }

    /// <summary>
    /// Records a margin call event
    /// </summary>
    public async Task<bool> RecordMarginCallAsync(string positionId, decimal currentHealthRatio, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO margin_call_history (position_id, health_ratio_at_call, called_at)
                VALUES ($1, $2, NOW());";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", positionId);
            cmd.Parameters.AddWithValue("$2", currentHealthRatio);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Warning("Recorded margin call for position {PositionId} with health ratio {HealthRatio}", positionId, currentHealthRatio);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording margin call for position {PositionId}", positionId);
            return false;
        }
    }

    /// <summary>
    /// Records a liquidation event
    /// </summary>
    public async Task<bool> RecordLiquidationAsync(string positionId, decimal liquidationPrice, decimal pnl, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO liquidation_history (position_id, liquidation_price, pnl, liquidated_at)
                VALUES ($1, $2, $3, NOW());";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", positionId);
            cmd.Parameters.AddWithValue("$2", liquidationPrice);
            cmd.Parameters.AddWithValue("$3", pnl);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Warning("Recorded liquidation for position {PositionId} at price {Price} with PnL {PnL}", positionId, liquidationPrice, pnl);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording liquidation for position {PositionId}", positionId);
            return false;
        }
    }

    /// <summary>
    /// Gets total collateral amount across all margin positions
    /// </summary>
    public async Task<decimal> GetTotalCollateralAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(SUM(collateral_amount), 0) as total_collateral
                FROM positions
                WHERE collateral_amount IS NOT NULL AND collateral_amount > 0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total collateral");
            return 0m;
        }
    }

    /// <summary>
    /// Gets total borrowed amount across all margin positions
    /// </summary>
    public async Task<decimal> GetTotalBorrowedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(SUM(borrowed_amount), 0) as total_borrowed
                FROM positions
                WHERE borrowed_amount IS NOT NULL AND borrowed_amount > 0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total borrowed");
            return 0m;
        }
    }

    /// <summary>
    /// Gets accrued interest for all margin positions
    /// </summary>
    public async Task<decimal> GetTotalAccruedInterestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(SUM(borrowed_amount * interest_rate * EXTRACT(DAY FROM (NOW() - opened_at)) / 365), 0) as accrued_interest
                FROM positions
                WHERE borrowed_amount IS NOT NULL AND interest_rate IS NOT NULL AND borrowed_amount > 0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total accrued interest");
            return 0m;
        }
    }

    /// <summary>
    /// Helper method to read a Position from a database reader
    /// </summary>
    private static Position ReadPositionFromReader(NpgsqlDataReader reader)
    {
        return new Position
        {
            PositionId = reader.GetString(reader.GetOrdinal("position_id")),
            Symbol = reader.GetString(reader.GetOrdinal("symbol")),
            Exchange = reader.GetString(reader.GetOrdinal("exchange")),
            Side = (Core.Enums.OrderSide)Enum.Parse(typeof(Core.Enums.OrderSide), reader.GetString(reader.GetOrdinal("side"))),
            Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
            EntryPrice = reader.GetDecimal(reader.GetOrdinal("entry_price")),
            CurrentPrice = reader.GetDecimal(reader.GetOrdinal("current_price")),
            StopLoss = reader.IsDBNull(reader.GetOrdinal("stop_loss")) ? null : reader.GetDecimal(reader.GetOrdinal("stop_loss")),
            TakeProfit = reader.IsDBNull(reader.GetOrdinal("take_profit")) ? null : reader.GetDecimal(reader.GetOrdinal("take_profit")),
            StrategyId = reader.IsDBNull(reader.GetOrdinal("strategy_id")) ? null : reader.GetString(reader.GetOrdinal("strategy_id")),
            OpenOrderId = reader.IsDBNull(reader.GetOrdinal("open_order_id")) ? null : reader.GetString(reader.GetOrdinal("open_order_id")),
            OpenedAt = reader.GetDateTime(reader.GetOrdinal("opened_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at")),
            Leverage = reader.GetDecimal(reader.GetOrdinal("leverage")),
            MarginType = reader.IsDBNull(reader.GetOrdinal("margin_type")) ? null : (Core.Enums.MarginType)Enum.Parse(typeof(Core.Enums.MarginType), reader.GetString(reader.GetOrdinal("margin_type"))),
            CollateralAmount = reader.IsDBNull(reader.GetOrdinal("collateral_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("collateral_amount")),
            BorrowedAmount = reader.IsDBNull(reader.GetOrdinal("borrowed_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("borrowed_amount")),
            InterestRate = reader.IsDBNull(reader.GetOrdinal("interest_rate")) ? null : reader.GetDecimal(reader.GetOrdinal("interest_rate")),
            LiquidationPrice = reader.IsDBNull(reader.GetOrdinal("liquidation_price")) ? null : reader.GetDecimal(reader.GetOrdinal("liquidation_price")),
            MarginHealthRatio = reader.IsDBNull(reader.GetOrdinal("margin_health_ratio")) ? null : reader.GetDecimal(reader.GetOrdinal("margin_health_ratio"))
        };
    }
}
