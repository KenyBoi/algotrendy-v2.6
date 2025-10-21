using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Coinbase.AdvancedTrade;
using Coinbase.AdvancedTrade.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Coinbase Advanced Trade broker implementation for cryptocurrency trading
/// Supports: Spot trading only (no margin/leverage)
/// Uses: Coinbase.AdvancedTrade v1.4.0 package
/// </summary>
public class CoinbaseBroker : BrokerBase, IDisposable
{
    private CoinbaseClient? _client;
    private readonly CoinbaseOptions _options;
    private readonly SemaphoreSlim _rateLimiterLocal = new(10, 10); // For local rate limiting methods

    protected override int MinRequestIntervalMs => 100; // ~10 requests/second

    public override string BrokerName => "coinbase";

    public CoinbaseBroker(
        IOptions<CoinbaseOptions> options,
        ILogger<CoinbaseBroker> logger) : base(logger, 10)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets or initializes the Coinbase Advanced Trade REST client (lazy initialization)
    /// </summary>
    private CoinbaseClient GetClient()
    {
        if (_client == null)
        {
            #pragma warning disable CS0618 // Suppress obsolete warning for Legacy API key type
            _client = new CoinbaseClient(
                _options.ApiKey,
                _options.ApiSecret,
                websocketBufferSize: 5 * 1024 * 1024, // 5 MB default
                apiKeyType: ApiKeyType.Legacy // Using Legacy for backward compatibility
            );
            #pragma warning restore CS0618

            _logger.LogInformation("Coinbase Advanced Trade client initialized");
        }
        return _client;
    }

    /// <summary>
    /// Connects to Coinbase API and verifies credentials
    /// </summary>
    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to Coinbase Advanced Trade...");

            // Test connection by listing accounts
            var accounts = await GetClient().Accounts.ListAccountsAsync(limit: 10, cursor: null);

            if (accounts != null && accounts.Any())
            {
                _isConnected = true;
                _logger.LogInformation(
                    "Connected to Coinbase successfully. Found {AccountCount} accounts",
                    accounts.Count());
                return true;
            }

            _logger.LogWarning("Connected to Coinbase but no accounts found");
            _isConnected = true; // Still consider it connected
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Coinbase");
            return false;
        }
    }

    /// <summary>
    /// Gets current balance in the specified currency
    /// </summary>
    public override async Task<decimal> GetBalanceAsync(string currency = "USD", CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            var accounts = await GetClient().Accounts.ListAccountsAsync(limit: 250, cursor: null);

            if (accounts == null || !accounts.Any())
            {
                _logger.LogWarning("No accounts found");
                return 0m;
            }

            var account = accounts.FirstOrDefault(a =>
                a.Currency != null && a.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase));

            if (account?.AvailableBalance?.Value != null &&
                decimal.TryParse(account.AvailableBalance.Value, out var balance))
            {
                return balance;
            }

            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for {Currency}", currency);
            return 0m;
        }
    }

    /// <summary>
    /// Gets all current positions
    /// Note: Coinbase doesn't have traditional "positions" - we track non-zero balances
    /// </summary>
    public override async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            var accounts = await GetClient().Accounts.ListAccountsAsync(limit: 250, cursor: null);

            if (accounts == null || !accounts.Any())
            {
                return Array.Empty<Position>();
            }

            var positions = new List<Position>();

            foreach (var account in accounts)
            {
                if (account.AvailableBalance?.Value != null &&
                    decimal.TryParse(account.AvailableBalance.Value, out var balance) &&
                    balance > 0)
                {
                    positions.Add(new Position
                    {
                        PositionId = account.Uuid ?? Guid.NewGuid().ToString(),
                        Symbol = account.Currency ?? "UNKNOWN",
                        Exchange = "coinbase",
                        Side = AlgoTrendy.Core.Enums.OrderSide.Buy, // Holding balance = long position
                        Quantity = balance,
                        EntryPrice = 0, // Not available from balance data
                        CurrentPrice = 0, // Would need separate price fetch
                        OpenedAt = DateTime.UtcNow, // Not available, use current time
                        UpdatedAt = DateTime.UtcNow
                        // Note: UnrealizedPnL is a computed property based on CurrentPrice and EntryPrice
                    });
                }
            }

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get positions");
            return Array.Empty<Position>();
        }
    }

    /// <summary>
    /// Places a new order
    /// </summary>
    public override async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            _logger.LogInformation(
                "Placing {OrderType} {Side} order: {Symbol} x {Quantity}",
                request.Type, request.Side, request.Symbol, request.Quantity);

            var side = request.Side.ToString().ToLower() == "buy"
                ? Coinbase.AdvancedTrade.Enums.OrderSide.BUY
                : Coinbase.AdvancedTrade.Enums.OrderSide.SELL;

            string? orderId;

            if (request.Type == AlgoTrendy.Core.Enums.OrderType.Market)
            {
                // Market order: CreateMarketOrderAsync(productId, side, quantity)
                orderId = await GetClient().Orders.CreateMarketOrderAsync(
                    request.Symbol,
                    side,
                    request.Quantity.ToString());
            }
            else if (request.Type == AlgoTrendy.Core.Enums.OrderType.Limit && request.Price.HasValue)
            {
                // Limit order: CreateLimitOrderGTCAsync(productId, side, baseSize, limitPrice, postOnly)
                orderId = await GetClient().Orders.CreateLimitOrderGTCAsync(
                    request.Symbol,
                    side,
                    request.Quantity.ToString(),
                    request.Price.Value.ToString(),
                    postOnly: false);
            }
            else
            {
                throw new ArgumentException("Invalid order type or missing price for limit order");
            }

            if (string.IsNullOrEmpty(orderId))
            {
                throw new Exception("Order creation failed - no order ID returned");
            }

            _logger.LogInformation("Order placed successfully. Order ID: {OrderId}", orderId);

            // Return order with basic info
            return new Order
            {
                OrderId = orderId,
                ClientOrderId = request.ClientOrderId ?? OrderFactory.GenerateClientOrderId(),
                Symbol = request.Symbol,
                Exchange = "coinbase",
                Side = request.Side,
                Type = request.Type,
                Quantity = request.Quantity,
                Price = request.Price,
                Status = AlgoTrendy.Core.Enums.OrderStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order");
            throw;
        }
    }

    /// <summary>
    /// Cancels an existing order
    /// </summary>
    public override async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            // CancelOrdersAsync takes array of order IDs
            var result = await GetClient().Orders.CancelOrdersAsync(new[] { orderId });

            _logger.LogInformation("Order {OrderId} cancelled", orderId);

            return new Order
            {
                OrderId = orderId,
                ClientOrderId = OrderFactory.GenerateClientOrderId(),
                Symbol = symbol,
                Exchange = "coinbase",
                Side = AlgoTrendy.Core.Enums.OrderSide.Buy, // Unknown, default to Buy
                Type = AlgoTrendy.Core.Enums.OrderType.Market, // Unknown, default to Market
                Quantity = 0m, // Unknown
                Status = AlgoTrendy.Core.Enums.OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the status of an existing order
    /// </summary>
    public override async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            var coinbaseOrder = await GetClient().Orders.GetOrderAsync(orderId);

            if (coinbaseOrder == null)
            {
                throw new Exception($"Order {orderId} not found");
            }

            return new Order
            {
                OrderId = coinbaseOrder.OrderId ?? orderId,
                ClientOrderId = coinbaseOrder.ClientOrderId ?? "",
                Symbol = coinbaseOrder.ProductId ?? symbol,
                Exchange = "coinbase",
                Side = coinbaseOrder.Side?.ToUpper() == "BUY"
                    ? AlgoTrendy.Core.Enums.OrderSide.Buy
                    : AlgoTrendy.Core.Enums.OrderSide.Sell,
                Type = MapCoinbaseOrderType(coinbaseOrder.OrderType),
                Quantity = 0m, // OrderConfiguration structure varies, would need specific parsing
                Price = null, // Not always available
                Status = MapCoinbaseOrderStatus(coinbaseOrder.Status),
                CreatedAt = coinbaseOrder.CreatedTime ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order status for {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current market price for a symbol
    /// </summary>
    public override async Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        await RateLimitAsync();

        try
        {
            var product = await GetClient().Products.GetProductAsync(symbol);

            if (product?.Price != null && decimal.TryParse(product.Price, out var price))
            {
                return price;
            }

            _logger.LogWarning("Price not available for {Symbol}", symbol);
            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get price for {Symbol}", symbol);
            return 0m;
        }
    }

    /// <summary>
    /// Sets leverage for a symbol
    /// Note: Coinbase Advanced Trade does NOT support leverage/margin trading
    /// </summary>
    public override async Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        AlgoTrendy.Core.Enums.MarginType marginType = AlgoTrendy.Core.Enums.MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        // Coinbase Advanced Trade is spot trading only
        if (leverage != 1m)
        {
            _logger.LogWarning(
                "Coinbase does not support leverage trading. Symbol: {Symbol}, Requested: {Leverage}x",
                symbol, leverage);
            return false;
        }

        await Task.CompletedTask; // Suppress async warning
        return true;
    }

    /// <summary>
    /// Gets leverage information for a symbol
    /// Note: Coinbase is spot trading only - always returns 1x leverage
    /// </summary>
    public override async Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Suppress async warning

        return new LeverageInfo
        {
            CurrentLeverage = 1m,
            MaxLeverage = 1m,
            MarginType = AlgoTrendy.Core.Enums.MarginType.Cross,
            CollateralAmount = 0m,
            BorrowedAmount = 0m
        };
    }

    /// <summary>
    /// Gets margin health ratio for the account
    /// Note: Coinbase is spot trading only - no margin to track
    /// </summary>
    public override async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Suppress async warning
        return 1.0m; // Fully healthy (no leverage risk)
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            await ConnectAsync(cancellationToken);
        }
    }

    private async Task RateLimitAsync()
    {
        await _rateLimiterLocal.WaitAsync();
        try
        {
            await Task.Delay(MinRequestIntervalMs);
        }
        finally
        {
            _rateLimiterLocal.Release();
        }
    }

    private AlgoTrendy.Core.Enums.OrderType MapCoinbaseOrderType(string? coinbaseType)
    {
        if (string.IsNullOrEmpty(coinbaseType))
            return AlgoTrendy.Core.Enums.OrderType.Market;

        return coinbaseType.ToLowerInvariant() switch
        {
            "market" => AlgoTrendy.Core.Enums.OrderType.Market,
            "limit" => AlgoTrendy.Core.Enums.OrderType.Limit,
            "stop" => AlgoTrendy.Core.Enums.OrderType.StopLoss,
            "stop_limit" => AlgoTrendy.Core.Enums.OrderType.StopLimit,
            _ => AlgoTrendy.Core.Enums.OrderType.Market
        };
    }

    private AlgoTrendy.Core.Enums.OrderStatus MapCoinbaseOrderStatus(string? coinbaseStatus)
    {
        if (string.IsNullOrEmpty(coinbaseStatus))
            return AlgoTrendy.Core.Enums.OrderStatus.Open;

        return coinbaseStatus.ToLowerInvariant() switch
        {
            "open" => AlgoTrendy.Core.Enums.OrderStatus.Open,
            "pending" => AlgoTrendy.Core.Enums.OrderStatus.Open,
            "active" => AlgoTrendy.Core.Enums.OrderStatus.Open,
            "filled" => AlgoTrendy.Core.Enums.OrderStatus.Filled,
            "cancelled" => AlgoTrendy.Core.Enums.OrderStatus.Cancelled,
            "expired" => AlgoTrendy.Core.Enums.OrderStatus.Expired,
            "failed" => AlgoTrendy.Core.Enums.OrderStatus.Rejected,
            _ => AlgoTrendy.Core.Enums.OrderStatus.Open
        };
    }

    public void Dispose()
    {
        _client = null; // CoinbaseClient doesn't implement IDisposable
        _rateLimiterLocal.Dispose();
    }
}

/// <summary>
/// Configuration options for Coinbase Advanced Trade broker
/// </summary>
public class CoinbaseOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
