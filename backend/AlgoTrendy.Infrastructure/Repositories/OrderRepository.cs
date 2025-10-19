using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Npgsql;
using System.Text.Json;

namespace AlgoTrendy.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of the order repository
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new order in the database
    /// </summary>
    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO orders (
                order_id, client_order_id, exchange_order_id, symbol, exchange,
                side, type, status, quantity, filled_quantity,
                price, stop_price, average_fill_price, strategy_id,
                created_at, updated_at, submitted_at, closed_at, metadata
            ) VALUES (
                @orderId, @clientOrderId, @exchangeOrderId, @symbol, @exchange,
                @side, @type, @status, @quantity, @filledQuantity,
                @price, @stopPrice, @averageFillPrice, @strategyId,
                @createdAt, @updatedAt, @submittedAt, @closedAt, @metadata::jsonb
            )
            RETURNING order_id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        AddOrderParameters(command, order);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return order;
    }

    /// <summary>
    /// Updates an existing order
    /// </summary>
    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE orders SET
                exchange_order_id = @exchangeOrderId,
                status = @status,
                filled_quantity = @filledQuantity,
                average_fill_price = @averageFillPrice,
                updated_at = @updatedAt,
                submitted_at = @submittedAt,
                closed_at = @closedAt,
                metadata = @metadata::jsonb
            WHERE order_id = @orderId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", order.OrderId);
        command.Parameters.AddWithValue("exchangeOrderId", (object?)order.ExchangeOrderId ?? DBNull.Value);
        command.Parameters.AddWithValue("status", order.Status.ToString());
        command.Parameters.AddWithValue("filledQuantity", order.FilledQuantity);
        command.Parameters.AddWithValue("averageFillPrice", (object?)order.AverageFillPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("updatedAt", order.UpdatedAt);
        command.Parameters.AddWithValue("submittedAt", (object?)order.SubmittedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("closedAt", (object?)order.ClosedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata", (object?)order.Metadata ?? DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Order {order.OrderId} not found for update");
        }

        return order;
    }

    /// <summary>
    /// Gets an order by its internal order ID
    /// </summary>
    public async Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE order_id = @orderId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOrder(reader);
        }

        return null;
    }

    /// <summary>
    /// Gets an order by its exchange order ID
    /// </summary>
    public async Task<Order?> GetByExchangeOrderIdAsync(string exchangeOrderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE exchange_order_id = @exchangeOrderId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("exchangeOrderId", exchangeOrderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOrder(reader);
        }

        return null;
    }

    /// <summary>
    /// Gets an order by its client order ID (for idempotency checks)
    /// </summary>
    public async Task<Order?> GetByClientOrderIdAsync(string clientOrderId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE client_order_id = @clientOrderId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("clientOrderId", clientOrderId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToOrder(reader);
        }

        return null;
    }

    /// <summary>
    /// Gets all orders for a specific symbol
    /// </summary>
    public async Task<IEnumerable<Order>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE symbol = @symbol
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("symbol", symbol);

        return await ReadOrdersAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets all active orders (Open or PartiallyFilled)
    /// </summary>
    public async Task<IEnumerable<Order>> GetActiveOrdersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE status IN ('Open', 'PartiallyFilled')
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        return await ReadOrdersAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets orders by status
    /// </summary>
    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE status = @status
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("status", status.ToString());

        return await ReadOrdersAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets orders created within a time range
    /// </summary>
    public async Task<IEnumerable<Order>> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE created_at >= @startTime AND created_at <= @endTime
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startTime", startTime);
        command.Parameters.AddWithValue("endTime", endTime);

        return await ReadOrdersAsync(command, cancellationToken);
    }

    /// <summary>
    /// Gets orders by strategy ID
    /// </summary>
    public async Task<IEnumerable<Order>> GetByStrategyAsync(string strategyId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT order_id, client_order_id, exchange_order_id, symbol, exchange,
                   side, type, status, quantity, filled_quantity,
                   price, stop_price, average_fill_price, strategy_id,
                   created_at, updated_at, submitted_at, closed_at, metadata
            FROM orders
            WHERE strategy_id = @strategyId
            ORDER BY created_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("strategyId", strategyId);

        return await ReadOrdersAsync(command, cancellationToken);
    }

    #region Helper Methods

    /// <summary>
    /// Adds all order parameters to a command
    /// </summary>
    private static void AddOrderParameters(NpgsqlCommand command, Order order)
    {
        command.Parameters.AddWithValue("orderId", order.OrderId);
        command.Parameters.AddWithValue("clientOrderId", order.ClientOrderId);
        command.Parameters.AddWithValue("exchangeOrderId", (object?)order.ExchangeOrderId ?? DBNull.Value);
        command.Parameters.AddWithValue("symbol", order.Symbol);
        command.Parameters.AddWithValue("exchange", order.Exchange);
        command.Parameters.AddWithValue("side", order.Side.ToString());
        command.Parameters.AddWithValue("type", order.Type.ToString());
        command.Parameters.AddWithValue("status", order.Status.ToString());
        command.Parameters.AddWithValue("quantity", order.Quantity);
        command.Parameters.AddWithValue("filledQuantity", order.FilledQuantity);
        command.Parameters.AddWithValue("price", (object?)order.Price ?? DBNull.Value);
        command.Parameters.AddWithValue("stopPrice", (object?)order.StopPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("averageFillPrice", (object?)order.AverageFillPrice ?? DBNull.Value);
        command.Parameters.AddWithValue("strategyId", (object?)order.StrategyId ?? DBNull.Value);
        command.Parameters.AddWithValue("createdAt", order.CreatedAt);
        command.Parameters.AddWithValue("updatedAt", order.UpdatedAt);
        command.Parameters.AddWithValue("submittedAt", (object?)order.SubmittedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("closedAt", (object?)order.ClosedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("metadata", (object?)order.Metadata ?? DBNull.Value);
    }

    /// <summary>
    /// Reads multiple orders from a data reader
    /// </summary>
    private static async Task<List<Order>> ReadOrdersAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var orders = new List<Order>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(MapToOrder(reader));
        }

        return orders;
    }

    /// <summary>
    /// Maps a data reader row to an Order object
    /// </summary>
    private static Order MapToOrder(NpgsqlDataReader reader)
    {
        return new Order
        {
            OrderId = reader.GetString(0),
            ClientOrderId = reader.GetString(1),
            ExchangeOrderId = reader.IsDBNull(2) ? null : reader.GetString(2),
            Symbol = reader.GetString(3),
            Exchange = reader.GetString(4),
            Side = Enum.Parse<OrderSide>(reader.GetString(5)),
            Type = Enum.Parse<OrderType>(reader.GetString(6)),
            Status = Enum.Parse<OrderStatus>(reader.GetString(7)),
            Quantity = reader.GetDecimal(8),
            FilledQuantity = reader.GetDecimal(9),
            Price = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
            StopPrice = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
            AverageFillPrice = reader.IsDBNull(12) ? null : reader.GetDecimal(12),
            StrategyId = reader.IsDBNull(13) ? null : reader.GetString(13),
            CreatedAt = reader.GetDateTime(14),
            UpdatedAt = reader.GetDateTime(15),
            SubmittedAt = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
            ClosedAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
            Metadata = reader.IsDBNull(18) ? null : reader.GetString(18)
        };
    }

    #endregion
}
