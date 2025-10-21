using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Npgsql;

namespace AlgoTrendy.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL/QuestDB implementation of the position repository
/// </summary>
public class PositionRepository : IPositionRepository
{
    private readonly string _connectionString;

    public PositionRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new position in the database
    /// </summary>
    public async Task<Position> CreateAsync(Position position, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO positions (
                position_id, symbol, exchange, side, quantity, entry_price,
                current_price, stop_loss, take_profit, strategy_id, open_order_id,
                opened_at, updated_at, leverage, margin_type, collateral_amount,
                borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            ) VALUES (
                @positionId, @symbol, @exchange, @side, @quantity, @entryPrice,
                @currentPrice, @stopLoss, @takeProfit, @strategyId, @openOrderId,
                @openedAt, @updatedAt, @leverage, @marginType, @collateralAmount,
                @borrowedAmount, @interestRate, @liquidationPrice, @marginHealthRatio
            )";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        AddPositionParameters(command, position);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return position;
    }

    /// <summary>
    /// Updates an existing position
    /// </summary>
    public async Task<Position> UpdateAsync(Position position, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE positions SET
                current_price = @currentPrice,
                stop_loss = @stopLoss,
                take_profit = @takeProfit,
                updated_at = @updatedAt,
                leverage = @leverage,
                margin_type = @marginType,
                collateral_amount = @collateralAmount,
                borrowed_amount = @borrowedAmount,
                interest_rate = @interestRate,
                liquidation_price = @liquidationPrice,
                margin_health_ratio = @marginHealthRatio
            WHERE position_id = @positionId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("positionId", position.PositionId);
        command.Parameters.AddWithValue("currentPrice", position.CurrentPrice);
        command.Parameters.AddWithValue("stopLoss", (object?)position.StopLoss ?? DBNull.Value);
        command.Parameters.AddWithValue("takeProfit", (object?)position.TakeProfit ?? DBNull.Value);
        command.Parameters.AddWithValue("updatedAt", position.UpdatedAt);
        command.Parameters.AddWithValue("leverage", position.Leverage);
        command.Parameters.AddWithValue("marginType", (object?)position.MarginType?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("collateralAmount", (object?)position.CollateralAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("borrowedAmount", (object?)position.BorrowedAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("interestRate", (object?)position.InterestRate ?? DBNull.Value);
        command.Parameters.AddWithValue("liquidationPrice", (object?)position.LiquidationPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("marginHealthRatio", (object?)position.MarginHealthRatio ?? DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Position {position.PositionId} not found for update");
        }

        return position;
    }

    /// <summary>
    /// Gets a position by its ID
    /// </summary>
    public async Task<Position?> GetByIdAsync(string positionId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position_id, symbol, exchange, side, quantity, entry_price,
                   current_price, stop_loss, take_profit, strategy_id, open_order_id,
                   opened_at, updated_at, leverage, margin_type, collateral_amount,
                   borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            FROM positions
            WHERE position_id = @positionId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("positionId", positionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToPosition(reader);
        }

        return null;
    }

    /// <summary>
    /// Gets a position by symbol
    /// </summary>
    public async Task<Position?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position_id, symbol, exchange, side, quantity, entry_price,
                   current_price, stop_loss, take_profit, strategy_id, open_order_id,
                   opened_at, updated_at, leverage, margin_type, collateral_amount,
                   borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            FROM positions
            WHERE symbol = @symbol
            ORDER BY opened_at DESC
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToPosition(reader);
        }

        return null;
    }

    /// <summary>
    /// Gets all active positions
    /// </summary>
    public async Task<IEnumerable<Position>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position_id, symbol, exchange, side, quantity, entry_price,
                   current_price, stop_loss, take_profit, strategy_id, open_order_id,
                   opened_at, updated_at, leverage, margin_type, collateral_amount,
                   borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            FROM positions
            ORDER BY opened_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        return await ReadPositionsAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets positions by exchange
    /// </summary>
    public async Task<IEnumerable<Position>> GetByExchangeAsync(string exchange, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position_id, symbol, exchange, side, quantity, entry_price,
                   current_price, stop_loss, take_profit, strategy_id, open_order_id,
                   opened_at, updated_at, leverage, margin_type, collateral_amount,
                   borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            FROM positions
            WHERE exchange = @exchange
            ORDER BY opened_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("exchange", exchange);

        return await ReadPositionsAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets positions by strategy ID
    /// </summary>
    public async Task<IEnumerable<Position>> GetByStrategyAsync(string strategyId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT position_id, symbol, exchange, side, quantity, entry_price,
                   current_price, stop_loss, take_profit, strategy_id, open_order_id,
                   opened_at, updated_at, leverage, margin_type, collateral_amount,
                   borrowed_amount, interest_rate, liquidation_price, margin_health_ratio
            FROM positions
            WHERE strategy_id = @strategyId
            ORDER BY opened_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("strategyId", strategyId);

        return await ReadPositionsAsync(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a position by ID
    /// </summary>
    public async Task DeleteAsync(string positionId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM positions WHERE position_id = @positionId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("positionId", positionId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Position {positionId} not found for deletion");
        }
    }

    /// <summary>
    /// Deletes a position by symbol
    /// </summary>
    public async Task DeleteBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM positions WHERE symbol = @symbol";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Position for symbol {symbol} not found for deletion");
        }
    }

    #region Helper Methods

    /// <summary>
    /// Adds all position parameters to a command
    /// </summary>
    private static void AddPositionParameters(NpgsqlCommand command, Position position)
    {
        command.Parameters.AddWithValue("positionId", position.PositionId);
        command.Parameters.AddWithValue("symbol", position.Symbol);
        command.Parameters.AddWithValue("exchange", position.Exchange);
        command.Parameters.AddWithValue("side", position.Side.ToString());
        command.Parameters.AddWithValue("quantity", position.Quantity);
        command.Parameters.AddWithValue("entryPrice", position.EntryPrice);
        command.Parameters.AddWithValue("currentPrice", position.CurrentPrice);
        command.Parameters.AddWithValue("stopLoss", (object?)position.StopLoss ?? DBNull.Value);
        command.Parameters.AddWithValue("takeProfit", (object?)position.TakeProfit ?? DBNull.Value);
        command.Parameters.AddWithValue("strategyId", (object?)position.StrategyId ?? DBNull.Value);
        command.Parameters.AddWithValue("openOrderId", (object?)position.OpenOrderId ?? DBNull.Value);
        command.Parameters.AddWithValue("openedAt", position.OpenedAt);
        command.Parameters.AddWithValue("updatedAt", position.UpdatedAt);
        command.Parameters.AddWithValue("leverage", position.Leverage);
        command.Parameters.AddWithValue("marginType", (object?)position.MarginType?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("collateralAmount", (object?)position.CollateralAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("borrowedAmount", (object?)position.BorrowedAmount ?? DBNull.Value);
        command.Parameters.AddWithValue("interestRate", (object?)position.InterestRate ?? DBNull.Value);
        command.Parameters.AddWithValue("liquidationPrice", (object?)position.LiquidationPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("marginHealthRatio", (object?)position.MarginHealthRatio ?? DBNull.Value);
    }

    /// <summary>
    /// Reads multiple positions from a data reader
    /// </summary>
    private static async Task<List<Position>> ReadPositionsAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var positions = new List<Position>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            positions.Add(MapToPosition(reader));
        }

        return positions;
    }

    /// <summary>
    /// Maps a data reader row to a Position object
    /// </summary>
    private static Position MapToPosition(NpgsqlDataReader reader)
    {
        return new Position
        {
            PositionId = reader.GetString(0),
            Symbol = reader.GetString(1),
            Exchange = reader.GetString(2),
            Side = Enum.Parse<OrderSide>(reader.GetString(3)),
            Quantity = reader.GetDecimal(4),
            EntryPrice = reader.GetDecimal(5),
            CurrentPrice = reader.GetDecimal(6),
            StopLoss = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            TakeProfit = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            StrategyId = reader.IsDBNull(9) ? null : reader.GetString(9),
            OpenOrderId = reader.IsDBNull(10) ? null : reader.GetString(10),
            OpenedAt = reader.GetDateTime(11),
            UpdatedAt = reader.GetDateTime(12),
            Leverage = reader.GetDecimal(13),
            MarginType = reader.IsDBNull(14) ? null : Enum.Parse<MarginType>(reader.GetString(14)),
            CollateralAmount = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
            BorrowedAmount = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
            InterestRate = reader.IsDBNull(17) ? null : reader.GetDecimal(17),
            LiquidationPrice = reader.IsDBNull(18) ? null : reader.GetDecimal(18),
            MarginHealthRatio = reader.IsDBNull(19) ? null : reader.GetDecimal(19)
        };
    }

    #endregion
}
