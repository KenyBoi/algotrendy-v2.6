using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kraken.Net;
using Kraken.Net.Clients;
using Kraken.Net.Enums;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Kraken broker implementation for cryptocurrency trading
/// Supports: Spot trading with optional margin (up to 5x leverage on select pairs)
/// NOTE: Kraken does not have a testnet - all testing must be done with production API using small orders
/// </summary>
public class KrakenBroker : IBroker
{
    private KrakenRestClient? _client;
    private readonly KrakenOptions _options;
    private readonly ILogger<KrakenBroker> _logger;
    private bool _isConnected = false;

    // Rate limiting (Kraken: Tier 2 = 15 orders/second base, variable by endpoint)
    private readonly SemaphoreSlim _rateLimiter = new(15, 15);
    private readonly Dictionary<string, DateTime> _lastRequestTime = new();
    private readonly object _requestTimeLock = new();
    private const int MinRequestIntervalMs = 70; // ~14 requests/second to stay safe

    // Kraken symbol mapping (Kraken uses unique formats like XXBTZUSD)
    private static readonly Dictionary<string, string> SymbolMapping = new()
    {
        ["BTCUSD"] = "XXBTZUSD",
        ["BTCUSDT"] = "XBTUSDT",
        ["ETHUSD"] = "XETHZUSD",
        ["ETHUSDT"] = "ETHUSDT",
        ["SOLUSD"] = "SOLUSD",
        ["SOLUSDT"] = "SOLUSDT",
        ["ADAUSD"] = "ADAUSD",
        ["ADAUSDT"] = "ADAUSDT",
        ["XRPUSD"] = "XXRPZUSD",
        ["XRPUSDT"] = "XRPUSDT"
    };

    public string BrokerName => "kraken";

    public KrakenBroker(
        IOptions<KrakenOptions> options,
        ILogger<KrakenBroker> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogWarning("Kraken broker configured for PRODUCTION (no testnet available). Use with caution.");
    }

    /// <summary>
    /// Gets or initializes the Kraken REST client (lazy initialization)
    /// </summary>
    private KrakenRestClient GetClient()
    {
        if (_client == null)
        {
            _client = new KrakenRestClient(opts =>
            {
                opts.ApiCredentials = new ApiCredentials(
                    _options.ApiKey,
                    _options.ApiSecret);
            });
        }
        return _client;
    }

    /// <summary>
    /// Connects to Kraken API and verifies credentials
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to Kraken...");

            // Test connection by getting account balance
            var balanceResult = await GetClient().SpotApi.Account.GetBalancesAsync(ct: cancellationToken);

            if (!balanceResult.Success)
            {
                _logger.LogError("Failed to connect to Kraken: {Error}", balanceResult.Error?.Message);
                return false;
            }

            _isConnected = true;
            _logger.LogInformation(
                "Connected to Kraken successfully. Account has {AssetCount} assets.",
                balanceResult.Data.Count());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Kraken connection");
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
            var balanceResult = await GetClient().SpotApi.Account.GetBalancesAsync(ct: cancellationToken);

            if (!balanceResult.Success)
            {
                _logger.LogError("Failed to get balance: {Error}", balanceResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get balance: {balanceResult.Error?.Message}");
            }

            // Kraken uses different currency codes (USD -> ZUSD, BTC -> XXBT, etc.)
            var krakenCurrency = MapCurrencyToKraken(currency);
            var balance = balanceResult.Data.FirstOrDefault(b => b.Asset == krakenCurrency);
            var availableBalance = balance?.Available ?? 0;

            _logger.LogDebug("Balance for {Currency} ({KrakenCurrency}): {Balance}",
                currency, krakenCurrency, availableBalance);

            return availableBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for {Currency}", currency);
            throw;
        }
    }

    /// <summary>
    /// Gets all active positions (margin positions only - spot doesn't have positions)
    /// </summary>
    public async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        // Spot trading doesn't have positions in the traditional sense
        // Margin positions would require additional implementation
        return await Task.FromResult(Enumerable.Empty<Position>());
    }

    /// <summary>
    /// Places an order on Kraken
    /// </summary>
    public async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        // Enforce rate limiting to prevent API bans
        await EnforceRateLimitAsync(request.Symbol, cancellationToken);

        try
        {
            _logger.LogInformation(
                "Placing {Type} {Side} order: {Symbol} Quantity: {Quantity}",
                request.Type, request.Side, request.Symbol, request.Quantity);

            // Map symbol to Kraken format
            var krakenSymbol = MapSymbolToKraken(request.Symbol);

            // Map order type and side
            var orderSide = MapOrderSide(request.Side);
            var orderType = MapOrderType(request.Type);

            // Place order based on type
            var result = request.Type switch
            {
                Core.Enums.OrderType.Market => await GetClient().SpotApi.Trading.PlaceOrderAsync(
                    symbol: krakenSymbol,
                    side: orderSide,
                    type: orderType,
                    quantity: request.Quantity,
                    ct: cancellationToken),

                Core.Enums.OrderType.Limit => await GetClient().SpotApi.Trading.PlaceOrderAsync(
                    symbol: krakenSymbol,
                    side: orderSide,
                    type: orderType,
                    quantity: request.Quantity,
                    price: request.Price,
                    ct: cancellationToken),

                Core.Enums.OrderType.StopLoss when request.StopPrice.HasValue =>
                    await GetClient().SpotApi.Trading.PlaceOrderAsync(
                        symbol: krakenSymbol,
                        side: orderSide,
                        type: Kraken.Net.Enums.OrderType.StopLoss,
                        quantity: request.Quantity,
                        price: request.StopPrice.Value,
                        ct: cancellationToken),

                Core.Enums.OrderType.TakeProfit when request.Price.HasValue =>
                    await GetClient().SpotApi.Trading.PlaceOrderAsync(
                        symbol: krakenSymbol,
                        side: orderSide,
                        type: Kraken.Net.Enums.OrderType.TakeProfit,
                        quantity: request.Quantity,
                        price: request.Price.Value,
                        ct: cancellationToken),

                _ => throw new NotSupportedException($"Order type {request.Type} not supported")
            };

            if (!result.Success)
            {
                _logger.LogError("Failed to place order: {Error}", result.Error?.Message);
                throw new InvalidOperationException($"Failed to place order: {result.Error?.Message}");
            }

            // Kraken returns order IDs as strings
            var krakenOrderId = result.Data.OrderIds.FirstOrDefault() ?? "unknown";

            // Convert to our Order model
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = krakenOrderId,
                Symbol = request.Symbol,
                Exchange = BrokerName,
                Side = request.Side,
                Type = request.Type,
                Status = Core.Enums.OrderStatus.Open, // Kraken doesn't return status immediately
                Quantity = request.Quantity,
                FilledQuantity = 0,
                Price = request.Price,
                StopPrice = request.StopPrice,
                AverageFillPrice = null,
                StrategyId = request.StrategyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Order placed successfully. Exchange Order ID: {ExchangeOrderId}",
                order.ExchangeOrderId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order for {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order on Kraken
    /// </summary>
    public async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Cancelling order {OrderId}", orderId);

            var result = await GetClient().SpotApi.Trading.CancelOrderAsync(
                orderId: orderId,
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to cancel order: {Error}", result.Error?.Message);
                throw new InvalidOperationException($"Failed to cancel order: {result.Error?.Message}");
            }

            // Fetch order details to get full info
            var orderDetailsResult = await GetClient().SpotApi.Trading.GetClosedOrdersAsync(ct: cancellationToken);

            var krakenOrder = orderDetailsResult.Success
                ? orderDetailsResult.Data.FirstOrDefault(o => o.Id == orderId)
                : null;

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = orderId,
                Symbol = symbol,
                Exchange = BrokerName,
                Side = krakenOrder != null ? MapOrderSideFromKraken(krakenOrder.Side) : Core.Enums.OrderSide.Buy,
                Type = krakenOrder != null ? MapOrderTypeFromKraken(krakenOrder.OrderType) : Core.Enums.OrderType.Market,
                Status = Core.Enums.OrderStatus.Cancelled,
                Quantity = krakenOrder?.Quantity ?? 0,
                FilledQuantity = krakenOrder?.QuantityFilled ?? 0,
                Price = krakenOrder?.Price,
                AverageFillPrice = krakenOrder?.AveragePrice,
                CreatedAt = krakenOrder?.CreateTime ?? DateTime.UtcNow,
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
    /// Gets the current status of an order from Kraken
    /// </summary>
    public async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            // Kraken requires checking both open and closed orders
            var openOrdersResult = await GetClient().SpotApi.Trading.GetOpenOrdersAsync(ct: cancellationToken);
            var krakenOrder = openOrdersResult.Success
                ? openOrdersResult.Data.FirstOrDefault(o => o.Id == orderId)
                : null;

            // If not in open orders, check closed orders
            if (krakenOrder == null)
            {
                var closedOrdersResult = await GetClient().SpotApi.Trading.GetClosedOrdersAsync(ct: cancellationToken);
                krakenOrder = closedOrdersResult.Success
                    ? closedOrdersResult.Data.FirstOrDefault(o => o.Id == orderId)
                    : null;
            }

            if (krakenOrder == null)
            {
                throw new InvalidOperationException($"Order {orderId} not found");
            }

            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                ExchangeOrderId = orderId,
                Symbol = symbol,
                Exchange = BrokerName,
                Side = MapOrderSideFromKraken(krakenOrder.Side),
                Type = MapOrderTypeFromKraken(krakenOrder.OrderType),
                Status = MapOrderStatus(krakenOrder.Status),
                Quantity = krakenOrder.Quantity,
                FilledQuantity = krakenOrder.QuantityFilled,
                Price = krakenOrder.Price,
                AverageFillPrice = krakenOrder.AveragePrice,
                CreatedAt = krakenOrder.CreateTime,
                UpdatedAt = DateTime.UtcNow
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
            var krakenSymbol = MapSymbolToKraken(symbol);
            var result = await GetClient().SpotApi.ExchangeData.GetTickerAsync(
                symbol: krakenSymbol,
                ct: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Failed to get price for {Symbol}: {Error}", symbol, result.Error?.Message);
                throw new InvalidOperationException($"Failed to get price: {result.Error?.Message}");
            }

            return result.Data.LastPrice;
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
            throw new InvalidOperationException("Not connected to Kraken. Call ConnectAsync first.");
        }
    }

    /// <summary>
    /// Enforces rate limiting to prevent Kraken API bans
    /// Kraken Tier 2 limits: 15 orders/second base rate
    /// </summary>
    private async Task EnforceRateLimitAsync(string symbol, CancellationToken cancellationToken)
    {
        // Acquire semaphore (limits concurrent requests to 15)
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            // Check last request time for this symbol and enforce minimum interval
            lock (_requestTimeLock)
            {
                var now = DateTime.UtcNow;
                if (_lastRequestTime.TryGetValue(symbol, out var lastTime))
                {
                    var elapsedMs = (now - lastTime).TotalMilliseconds;
                    if (elapsedMs < MinRequestIntervalMs)
                    {
                        var delayMs = (int)(MinRequestIntervalMs - elapsedMs);
                        _logger.LogDebug("Rate limiting: delaying {DelayMs}ms for {Symbol}", delayMs, symbol);
                        Task.Delay(delayMs, cancellationToken).Wait(cancellationToken);
                    }
                }
                _lastRequestTime[symbol] = DateTime.UtcNow;
            }
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    /// <summary>
    /// Maps standard symbol to Kraken format (BTCUSD -> XXBTZUSD)
    /// </summary>
    private string MapSymbolToKraken(string symbol)
    {
        if (SymbolMapping.TryGetValue(symbol, out var krakenSymbol))
        {
            return krakenSymbol;
        }
        // If no mapping exists, return as-is and let Kraken reject if invalid
        _logger.LogWarning("No symbol mapping for {Symbol}, using as-is", symbol);
        return symbol;
    }

    /// <summary>
    /// Maps standard currency to Kraken format (USD -> ZUSD, BTC -> XXBT)
    /// </summary>
    private string MapCurrencyToKraken(string currency)
    {
        return currency switch
        {
            "USD" => "ZUSD",
            "BTC" => "XXBT",
            "ETH" => "XETH",
            "XRP" => "XXRP",
            "USDT" => "USDT",
            "USDC" => "USDC",
            _ => currency
        };
    }

    private static Kraken.Net.Enums.OrderSide MapOrderSide(Core.Enums.OrderSide side)
    {
        return side switch
        {
            Core.Enums.OrderSide.Buy => Kraken.Net.Enums.OrderSide.Buy,
            Core.Enums.OrderSide.Sell => Kraken.Net.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid order side: {side}")
        };
    }

    private static Core.Enums.OrderSide MapOrderSideFromKraken(Kraken.Net.Enums.OrderSide side)
    {
        return side switch
        {
            Kraken.Net.Enums.OrderSide.Buy => Core.Enums.OrderSide.Buy,
            Kraken.Net.Enums.OrderSide.Sell => Core.Enums.OrderSide.Sell,
            _ => throw new ArgumentException($"Invalid Kraken order side: {side}")
        };
    }

    private static Kraken.Net.Enums.OrderType MapOrderType(Core.Enums.OrderType type)
    {
        return type switch
        {
            Core.Enums.OrderType.Market => Kraken.Net.Enums.OrderType.Market,
            Core.Enums.OrderType.Limit => Kraken.Net.Enums.OrderType.Limit,
            Core.Enums.OrderType.StopLoss => Kraken.Net.Enums.OrderType.StopLoss,
            Core.Enums.OrderType.StopLimit => Kraken.Net.Enums.OrderType.StopLossLimit,
            Core.Enums.OrderType.TakeProfit => Kraken.Net.Enums.OrderType.TakeProfit,
            _ => throw new ArgumentException($"Invalid order type: {type}")
        };
    }

    private static Core.Enums.OrderType MapOrderTypeFromKraken(Kraken.Net.Enums.OrderType type)
    {
        return type switch
        {
            Kraken.Net.Enums.OrderType.Market => Core.Enums.OrderType.Market,
            Kraken.Net.Enums.OrderType.Limit => Core.Enums.OrderType.Limit,
            Kraken.Net.Enums.OrderType.StopLoss => Core.Enums.OrderType.StopLoss,
            Kraken.Net.Enums.OrderType.StopLossLimit => Core.Enums.OrderType.StopLimit,
            Kraken.Net.Enums.OrderType.TakeProfit => Core.Enums.OrderType.TakeProfit,
            Kraken.Net.Enums.OrderType.TakeProfitLimit => Core.Enums.OrderType.TakeProfit,
            _ => Core.Enums.OrderType.Market
        };
    }

    private static Core.Enums.OrderStatus MapOrderStatus(Kraken.Net.Enums.OrderStatus status)
    {
        return status switch
        {
            Kraken.Net.Enums.OrderStatus.Pending => Core.Enums.OrderStatus.Pending,
            Kraken.Net.Enums.OrderStatus.Open => Core.Enums.OrderStatus.Open,
            Kraken.Net.Enums.OrderStatus.Closed => Core.Enums.OrderStatus.Filled,
            Kraken.Net.Enums.OrderStatus.Canceled => Core.Enums.OrderStatus.Cancelled,
            Kraken.Net.Enums.OrderStatus.Expired => Core.Enums.OrderStatus.Cancelled,
            _ => Core.Enums.OrderStatus.Pending
        };
    }

    /// <summary>
    /// Sets leverage for a symbol (Kraken margin trading)
    /// </summary>
    public async Task<bool> SetLeverageAsync(string symbol, decimal leverage, Core.Enums.MarginType marginType, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SetLeverageAsync not yet implemented for Kraken margin trading. Using default leverage 1x.");

        // Safe default: only allow 1x leverage (no leverage) for spot trading
        if (leverage != 1.0m)
        {
            throw new NotSupportedException($"Leverage modification not yet supported for Kraken spot trading. Default 1x leverage in use. Requested: {leverage}x. Kraken supports up to 5x on select margin pairs.");
        }

        await Task.CompletedTask; // Async placeholder
        return true;
    }

    /// <summary>
    /// Gets leverage information for a symbol
    /// </summary>
    public async Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GetLeverageInfoAsync not yet implemented for Kraken. Returning safe default (1x leverage).");

        await Task.CompletedTask; // Async placeholder

        // Return safe default: 1x leverage (no leverage)
        return new LeverageInfo
        {
            CurrentLeverage = 1.0m,
            MaxLeverage = 5.0m, // Kraken supports up to 5x on select margin pairs
            MarginType = Core.Enums.MarginType.Isolated,
            CollateralAmount = 0m,
            BorrowedAmount = 0m,
            InterestRate = 0m,
            LiquidationPrice = null,
            MarginHealthRatio = 1.0m // Perfect health (no leverage)
        };
    }

    /// <summary>
    /// Gets margin health ratio
    /// </summary>
    public async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GetMarginHealthRatioAsync not yet implemented for Kraken. Returning conservative estimate.");

        await Task.CompletedTask; // Async placeholder

        // Return conservative estimate: 50% health
        return 0.5m;
    }

    #endregion
}

/// <summary>
/// Kraken configuration options
/// </summary>
public class KrakenOptions
{
    /// <summary>
    /// Kraken API key
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Kraken API secret
    /// </summary>
    public required string ApiSecret { get; set; }
}
