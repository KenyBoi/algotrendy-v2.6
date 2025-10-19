using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Npgsql;
using Serilog;

namespace AlgoTrendy.Infrastructure.Repositories;

/// <summary>
/// Repository for leverage and debt tracking operations against QuestDB
/// </summary>
public class LeverageRepository : ILeverageRepository
{
    private readonly string _connectionString;
    private const string LeverageHistoryTableName = "leverage_history";
    private const string DebtTrackingTableName = "debt_tracking";

    public LeverageRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Records a leverage change for tracking and audit purposes
    /// </summary>
    public async Task<bool> RecordLeverageChangeAsync(
        string symbol,
        decimal previousLeverage,
        decimal newLeverage,
        MarginType marginType,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO leverage_history (symbol, previous_leverage, new_leverage, margin_type, reason, changed_at)
                VALUES ($1, $2, $3, $4, $5, NOW());";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", symbol);
            cmd.Parameters.AddWithValue("$2", previousLeverage);
            cmd.Parameters.AddWithValue("$3", newLeverage);
            cmd.Parameters.AddWithValue("$4", marginType.ToString());
            cmd.Parameters.AddWithValue("$5", reason);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Information("Recorded leverage change for {Symbol} from {PrevLeverage} to {NewLeverage}: {Reason}",
                symbol, previousLeverage, newLeverage, reason);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording leverage change for {Symbol}", symbol);
            return false;
        }
    }

    /// <summary>
    /// Gets leverage history for a symbol
    /// </summary>
    public async Task<IEnumerable<LeverageHistoryRecord>> GetLeverageHistoryAsync(
        string symbol,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var records = new List<LeverageHistoryRecord>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT symbol, previous_leverage, new_leverage, margin_type, reason, changed_at
                FROM leverage_history
                WHERE symbol = $1
                ORDER BY changed_at DESC
                LIMIT $2;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", symbol);
            cmd.Parameters.AddWithValue("$2", limit);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(new LeverageHistoryRecord
                {
                    Symbol = reader.GetString(reader.GetOrdinal("symbol")),
                    PreviousLeverage = reader.GetDecimal(reader.GetOrdinal("previous_leverage")),
                    NewLeverage = reader.GetDecimal(reader.GetOrdinal("new_leverage")),
                    MarginType = (MarginType)Enum.Parse(typeof(MarginType), reader.GetString(reader.GetOrdinal("margin_type"))),
                    Reason = reader.GetString(reader.GetOrdinal("reason")),
                    ChangedAt = reader.GetDateTime(reader.GetOrdinal("changed_at"))
                });
            }

            return records;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching leverage history for {Symbol}", symbol);
            return Enumerable.Empty<LeverageHistoryRecord>();
        }
    }

    /// <summary>
    /// Gets current total leverage exposure
    /// </summary>
    public async Task<decimal> GetTotalLeverageExposureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(SUM(current_price * quantity * leverage), 0) as total_exposure
                FROM positions
                WHERE leverage > 1.0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating total leverage exposure");
            return 0m;
        }
    }

    /// <summary>
    /// Gets highest leverage level currently in use
    /// </summary>
    public async Task<decimal> GetMaxLeverageInUseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(MAX(leverage), 1.0) as max_leverage
                FROM positions
                WHERE leverage > 1.0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 1.0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching max leverage in use");
            return 1.0m;
        }
    }

    /// <summary>
    /// Gets average leverage across all positions
    /// </summary>
    public async Task<decimal> GetAverageLeverageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                SELECT COALESCE(AVG(leverage), 1.0) as avg_leverage
                FROM positions
                WHERE leverage > 1.0;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result != null ? Convert.ToDecimal(result) : 1.0m;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error calculating average leverage");
            return 1.0m;
        }
    }

    /// <summary>
    /// Gets positions using a specific leverage level
    /// </summary>
    public async Task<IEnumerable<Position>> GetPositionsByLeverageAsync(decimal leverage, CancellationToken cancellationToken = default)
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
                WHERE leverage = $1
                ORDER BY updated_at DESC;";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", leverage);
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
            Log.Error(ex, "Error fetching positions by leverage {Leverage}", leverage);
            return Enumerable.Empty<Position>();
        }
    }

    /// <summary>
    /// Records debt incurred from leveraged trading
    /// </summary>
    public async Task<bool> RecordDebtAsync(
        string positionId,
        decimal borrowedAmount,
        decimal interestRate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO debt_tracking (position_id, borrowed_amount, interest_rate, incurred_at, status)
                VALUES ($1, $2, $3, NOW(), 'ACTIVE');";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", positionId);
            cmd.Parameters.AddWithValue("$2", borrowedAmount);
            cmd.Parameters.AddWithValue("$3", interestRate);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Information("Recorded debt for position {PositionId}: ${BorrowedAmount} at {InterestRate}% daily",
                positionId, borrowedAmount, interestRate);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording debt for position {PositionId}", positionId);
            return false;
        }
    }

    /// <summary>
    /// Records debt repayment
    /// </summary>
    public async Task<bool> RecordDebtRepaymentAsync(
        string positionId,
        decimal repaymentAmount,
        decimal interestPaid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = @"
                INSERT INTO debt_repayment_history (position_id, repayment_amount, interest_paid, repaid_at)
                VALUES ($1, $2, $3, NOW());";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("$1", positionId);
            cmd.Parameters.AddWithValue("$2", repaymentAmount);
            cmd.Parameters.AddWithValue("$3", interestPaid);
            cmd.CommandTimeout = 30;

            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            Log.Information("Recorded debt repayment for position {PositionId}: ${RepaymentAmount} + ${InterestPaid} interest",
                positionId, repaymentAmount, interestPaid);
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error recording debt repayment for position {PositionId}", positionId);
            return false;
        }
    }

    /// <summary>
    /// Gets debt summary for all positions
    /// </summary>
    public async Task<DebtSummary> GetDebtSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get debt totals
            const string debtQuery = @"
                SELECT COALESCE(SUM(borrowed_amount), 0) as total_borrowed,
                       COUNT(DISTINCT position_id) as active_positions
                FROM positions
                WHERE borrowed_amount IS NOT NULL AND borrowed_amount > 0;";

            decimal totalBorrowed = 0m;
            int activePositions = 0;

            await using (var cmd = new NpgsqlCommand(debtQuery, connection))
            {
                cmd.CommandTimeout = 30;
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    totalBorrowed = Convert.ToDecimal(reader["total_borrowed"]);
                    activePositions = Convert.ToInt32(reader["active_positions"]);
                }
            }

            // Get collateral totals
            const string collateralQuery = @"
                SELECT COALESCE(SUM(collateral_amount), 0) as total_collateral
                FROM positions
                WHERE collateral_amount IS NOT NULL AND collateral_amount > 0;";

            decimal totalCollateral = 0m;

            await using (var cmd = new NpgsqlCommand(collateralQuery, connection))
            {
                cmd.CommandTimeout = 30;
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                totalCollateral = result != null ? Convert.ToDecimal(result) : 0m;
            }

            // Get margin health
            const string healthQuery = @"
                SELECT COALESCE(AVG(margin_health_ratio), 1.0) as avg_health
                FROM positions
                WHERE margin_health_ratio IS NOT NULL;";

            decimal marginHealth = 1.0m;

            await using (var cmd = new NpgsqlCommand(healthQuery, connection))
            {
                cmd.CommandTimeout = 30;
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                marginHealth = result != null ? Convert.ToDecimal(result) : 1.0m;
            }

            // Get leverage stats
            const string leverageQuery = @"
                SELECT COALESCE(AVG(leverage), 1.0) as avg_leverage,
                       COALESCE(MAX(leverage), 1.0) as max_leverage,
                       COALESCE(SUM(current_price * quantity * leverage), 0) as total_exposure
                FROM positions
                WHERE leverage > 1.0;";

            decimal avgLeverage = 1.0m;
            decimal maxLeverage = 1.0m;
            decimal totalExposure = 0m;

            await using (var cmd = new NpgsqlCommand(leverageQuery, connection))
            {
                cmd.CommandTimeout = 30;
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    avgLeverage = Convert.ToDecimal(reader["avg_leverage"]);
                    maxLeverage = Convert.ToDecimal(reader["max_leverage"]);
                    totalExposure = Convert.ToDecimal(reader["total_exposure"]);
                }
            }

            // Get PnL
            const string pnlQuery = @"
                SELECT COALESCE(SUM((current_price - entry_price) * quantity), 0) as total_pnl
                FROM positions;";

            decimal totalPnL = 0m;

            await using (var cmd = new NpgsqlCommand(pnlQuery, connection))
            {
                cmd.CommandTimeout = 30;
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                totalPnL = result != null ? Convert.ToDecimal(result) : 0m;
            }

            decimal interestCost = totalBorrowed > 0 ? totalBorrowed * 0.001m : 0m;  // Assuming 0.1% daily rate

            return new DebtSummary
            {
                TotalBorrowedAmount = totalBorrowed,
                TotalCollateralAmount = totalCollateral,
                TotalInterestAccrued = interestCost,
                ActiveLeveragedPositions = activePositions,
                MarginHealthRatio = marginHealth,
                TotalLeverageExposure = totalExposure,
                AverageLeverage = avgLeverage,
                MaxLeverageInUse = maxLeverage,
                TotalPnL = totalPnL,
                DailyInterestCost = interestCost
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating debt summary");
            return new DebtSummary
            {
                TotalBorrowedAmount = 0m,
                TotalCollateralAmount = 0m,
                ActiveLeveragedPositions = 0,
                MarginHealthRatio = 1.0m,
                AverageLeverage = 1.0m,
                MaxLeverageInUse = 1.0m
            };
        }
    }

    /// <summary>
    /// Helper method to read a Position from database reader
    /// </summary>
    private static Position ReadPositionFromReader(NpgsqlDataReader reader)
    {
        return new Position
        {
            PositionId = reader.GetString(reader.GetOrdinal("position_id")),
            Symbol = reader.GetString(reader.GetOrdinal("symbol")),
            Exchange = reader.GetString(reader.GetOrdinal("exchange")),
            Side = (OrderSide)Enum.Parse(typeof(OrderSide), reader.GetString(reader.GetOrdinal("side"))),
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
            MarginType = reader.IsDBNull(reader.GetOrdinal("margin_type")) ? null : (MarginType)Enum.Parse(typeof(MarginType), reader.GetString(reader.GetOrdinal("margin_type"))),
            CollateralAmount = reader.IsDBNull(reader.GetOrdinal("collateral_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("collateral_amount")),
            BorrowedAmount = reader.IsDBNull(reader.GetOrdinal("borrowed_amount")) ? null : reader.GetDecimal(reader.GetOrdinal("borrowed_amount")),
            InterestRate = reader.IsDBNull(reader.GetOrdinal("interest_rate")) ? null : reader.GetDecimal(reader.GetOrdinal("interest_rate")),
            LiquidationPrice = reader.IsDBNull(reader.GetOrdinal("liquidation_price")) ? null : reader.GetDecimal(reader.GetOrdinal("liquidation_price")),
            MarginHealthRatio = reader.IsDBNull(reader.GetOrdinal("margin_health_ratio")) ? null : reader.GetDecimal(reader.GetOrdinal("margin_health_ratio"))
        };
    }
}
