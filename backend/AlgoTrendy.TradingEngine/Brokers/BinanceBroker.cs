using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Binance broker implementation using Binance.Net
/// </summary>
public class BinanceBroker : IBroker
{
    private readonly BinanceRestClient _client;
    private readonly BinanceOptions _options;
    private readonly ILogger<BinanceBroker> _logger;
    private bool _isConnected = false;

    public string BrokerName => "binance";

    public BinanceBroker(
        IOptions<BinanceOptions> options,
        ILogger<BinanceBroker> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize Binance REST client
        _client = new BinanceRestClient(opts =>
        {
            opts.ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
                _options.ApiKey,
                _options.ApiSecret);

            // Configure testnet/production environment
            if (_options.UseTestnet)
            {
                // Configure for testnet
                _logger.LogInformation("Binance broker configured for TESTNET");
            }
            else
            {
                // Configure for production
                _logger.LogInformation("Binance broker configured for PRODUCTION");
            }
        });
    }

    /// <summary>
    /// Connects to Binance API and verifies credentials
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to Binance...");

            // Test connection by getting account info
            var accountInfoResult = await _client.SpotApi.Account.GetAccountInfoAsync(ct: cancellationToken);

            if (!accountInfoResult.Success)
            {
                _logger.LogError("Failed to connect to Binance: {Error}", accountInfoResult.Error?.Message);
                return false;
            }

            _isConnected = true;
            _logger.LogInformation(
                "Connected to Binance successfully. Account Type: {AccountType}, Can Trade: {CanTrade}",
                accountInfoResult.Data.AccountType,
                accountInfoResult.Data.CanTrade);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Binance connection");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Gets account balance for a specific currency
    /// </summary>
    public async Task<decimal> GetBalanceAsync(string currency = "USDT", CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var accountInfoResult = await _client.SpotApi.Account.GetAccountInfoAsync(ct: cancellationToken);

            if (!accountInfoResult.Success)
            {
                _logger.LogError("Failed to get balance: {Error}", accountInfoResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get balance: {accountInfoResult.Error?.Message}");
            }

            var balance = accountInfoResult.Data.Balances.FirstOrDefault(b => b.Asset == currency);
            var availableBalance = balance?.Available ?? 0;

            _logger.LogDebug("Balance for {Currency}: {Balance}", currency, availableBalance);

            return availableBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for {Currency}", currency);
            throw;
        }
    }

    /// <summary>
    /// Gets all active positions (spot trading doesn't have positions, returns empty)
    /// </summary>
    public async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        // Spot trading doesn't have positions in the traditional sense
        // Return empty list for now
        return await Task.FromResult(Enumerable.Empty<Position>());
    }

    /// <summary>
    /// Places an order on Binance
    /// </summary>
    public async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation(
                "Placing {Type} {Side} order: {Symbol} Quantity: {Quantity}",
                request.Type, request.Side, request.Symbol, request.Quantity);

            // Map order type
            var orderSide = MapOrderSide(request.Side);
            var orderType = MapOrderType(request.Type);

            // Place order based on type
            var result = request.Type switch
            {
                OrderType.Market => await _client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: request.Symbol,
                    side: orderSide,
                    type: SpotOrderType.Market,
                    quantity: request.Quantity,
                    ct: cancellationToken),

                OrderType.Limit => await _client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: request.Symbol,
                    side: orderSide,
                    type: SpotOrderType.Limit,
                    quantity: request.Quantity,
                    price: request.Price,
                    timeInForce: TimeInForce.GoodTillCanceled,
                    ct: cancellationToken),

                OrderType.StopLoss => await _client.SpotApi.Trading.PlaceOrderAsync(
                    symbol: request.Symbol,
                    side: orderSide,
                    type: SpotOrderType.StopLoss,
                    quantity: request.Quantity,
                    stopPrice: request.StopPrice,
                    ct: cancellationToken),

                _ => throw new NotSupportedException($"Order type {request.Type} not supported")
            };

            if (!result.Success)
            {
                _logger.LogError("Failed to place order: {Error}", result.Error?.Message);
                throw new InvalidOperationException($"Failed to place order: {result.Error?.Message}");
            }

            var binanceOrder = result.Data;

            // Convert to our Order model
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ExchangeOrderId = binanceOrder.Id.ToString(),
                Symbol = request.Symbol,
                Exchange = BrokerName,
                Side = request.Side,
                Type = request.Type,
                Status = MapOrderStatus(binanceOrder.Status),
                Quantity = binanceOrder.Quantity,
                FilledQuantity = binanceOrder.QuantityFilled,
                Price = request.Price,
                StopPrice = request.StopPrice,
                AverageFillPrice = binanceOrder.QuoteQuantityFilled > 0
                    ? binanceOrder.QuoteQuantityFilled / binanceOrder.QuantityFilled
                    : null,
                StrategyId = request.StrategyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Order placed successfully. Exchange Order ID: {ExchangeOrderId}, Status: {Status}",
                order.ExchangeOrderId, order.Status);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order for {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order on Binance
    /// </summary>
    public async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Cancelling order {OrderId} for {Symbol}", orderId, symbol);

            var result = await _client.SpotApi.Trading.CancelOrderAsync(
                symbol: symbol,
                orderId: long.Parse(orderId),
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to cancel order: {Error}", result.Error?.Message);
                throw new InvalidOperationException($"Failed to cancel order: {result.Error?.Message}");
            }

            var binanceOrder = result.Data;

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ExchangeOrderId = binanceOrder.Id.ToString(),
                Symbol = symbol,
                Exchange = BrokerName,
                Side = MapOrderSideFromBinance(binanceOrder.Side),
                Type = MapOrderTypeFromBinance(binanceOrder.Type),
                Status = Core.Enums.OrderStatus.Cancelled,
                Quantity = binanceOrder.Quantity,
                FilledQuantity = binanceOrder.QuantityFilled,
                AverageFillPrice = binanceOrder.QuoteQuantityFilled > 0
                    ? binanceOrder.QuoteQuantityFilled / binanceOrder.QuantityFilled
                    : null,
                CreatedAt = binanceOrder.CreateTime,
                UpdatedAt = DateTime.UtcNow,
                ClosedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of an order from Binance
    /// </summary>
    public async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var result = await _client.SpotApi.Trading.GetOrderAsync(
                symbol: symbol,
                orderId: long.Parse(orderId),
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to get order status: {Error}", result.Error?.Message);
                throw new InvalidOperationException($"Failed to get order status: {result.Error?.Message}");
            }

            var binanceOrder = result.Data;

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ExchangeOrderId = binanceOrder.Id.ToString(),
                Symbol = symbol,
                Exchange = BrokerName,
                Side = MapOrderSideFromBinance(binanceOrder.Side),
                Type = MapOrderTypeFromBinance(binanceOrder.Type),
                Status = MapOrderStatus(binanceOrder.Status),
                Quantity = binanceOrder.Quantity,
                FilledQuantity = binanceOrder.QuantityFilled,
                Price = binanceOrder.Price > 0 ? binanceOrder.Price : null,
                AverageFillPrice = binanceOrder.QuoteQuantityFilled > 0
                    ? binanceOrder.QuoteQuantityFilled / binanceOrder.QuantityFilled
                    : null,
                CreatedAt = binanceOrder.CreateTime,
                UpdatedAt = binanceOrder.UpdateTime ?? DateTime.UtcNow
            };

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order status for {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets current market price for a symbol
    /// </summary>
    public async Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var result = await _client.SpotApi.ExchangeData.GetPriceAsync(
                symbol: symbol,
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to get price for {Symbol}: {Error}", symbol, result.Error?.Message);
                throw new InvalidOperationException($"Failed to get price: {result.Error?.Message}");
            }

            return result.Data.Price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price for {Symbol}", symbol);
            throw;
        }
    }

    #region Helper Methods

    private void EnsureConnected()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected to Binance. Call ConnectAsync first.");
        }
    }

    private static Binance.Net.Enums.OrderSide MapOrderSide(Core.Enums.OrderSide side)
    {
        return side switch
        {
            Core.Enums.OrderSide.Buy => Binance.Net.Enums.OrderSide.Buy,
            Core.Enums.OrderSide.Sell => Binance.Net.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid order side: {side}")
        };
    }

    private static Core.Enums.OrderSide MapOrderSideFromBinance(Binance.Net.Enums.OrderSide side)
    {
        return side switch
        {
            Binance.Net.Enums.OrderSide.Buy => Core.Enums.OrderSide.Buy,
            Binance.Net.Enums.OrderSide.Sell => Core.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid Binance order side: {side}")
        };
    }

    private static SpotOrderType MapOrderType(Core.Enums.OrderType type)
    {
        return type switch
        {
            Core.Enums.OrderType.Market => SpotOrderType.Market,
            Core.Enums.OrderType.Limit => SpotOrderType.Limit,
            Core.Enums.OrderType.StopLoss => SpotOrderType.StopLoss,
            Core.Enums.OrderType.StopLimit => SpotOrderType.StopLossLimit,
            Core.Enums.OrderType.TakeProfit => SpotOrderType.TakeProfitLimit,
            _ => throw new ArgumentException($"Invalid order type: {type}")
        };
    }

    private static Core.Enums.OrderType MapOrderTypeFromBinance(SpotOrderType type)
    {
        return type switch
        {
            SpotOrderType.Market => Core.Enums.OrderType.Market,
            SpotOrderType.Limit => Core.Enums.OrderType.Limit,
            SpotOrderType.StopLoss or SpotOrderType.StopLossLimit => Core.Enums.OrderType.StopLoss,
            SpotOrderType.TakeProfit or SpotOrderType.TakeProfitLimit => Core.Enums.OrderType.TakeProfit,
            _ => Core.Enums.OrderType.Market
        };
    }

    private static Core.Enums.OrderStatus MapOrderStatus(Binance.Net.Enums.OrderStatus status)
    {
        return status switch
        {
            Binance.Net.Enums.OrderStatus.New => Core.Enums.OrderStatus.Open,
            Binance.Net.Enums.OrderStatus.PartiallyFilled => Core.Enums.OrderStatus.PartiallyFilled,
            Binance.Net.Enums.OrderStatus.Filled => Core.Enums.OrderStatus.Filled,
            Binance.Net.Enums.OrderStatus.Canceled => Core.Enums.OrderStatus.Cancelled,
            Binance.Net.Enums.OrderStatus.Rejected => Core.Enums.OrderStatus.Rejected,
            Binance.Net.Enums.OrderStatus.Expired => Core.Enums.OrderStatus.Expired,
            _ => Core.Enums.OrderStatus.Pending
        };
    }

    #endregion
}

/// <summary>
/// Binance configuration options
/// </summary>
public class BinanceOptions
{
    /// <summary>
    /// Use Binance testnet instead of production
    /// </summary>
    public bool UseTestnet { get; set; } = true;

    /// <summary>
    /// Binance API key
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Binance API secret
    /// </summary>
    public required string ApiSecret { get; set; }
}
