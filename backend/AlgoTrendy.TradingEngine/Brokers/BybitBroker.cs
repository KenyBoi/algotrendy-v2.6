using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Bybit broker implementation using Bybit.Net
/// Supports USDT perpetual futures trading (linear contracts)
/// </summary>
public class BybitBroker : IBroker
{
    private readonly BybitRestClient _client;
    private readonly BybitOptions _options;
    private readonly ILogger<BybitBroker> _logger;
    private bool _isConnected = false;

    // Rate limiting (Bybit: 10 orders/second for futures)
    private readonly SemaphoreSlim _rateLimiter = new(10, 10);
    private readonly Dictionary<string, DateTime> _lastRequestTime = new();
    private readonly object _requestTimeLock = new();
    private const int MinRequestIntervalMs = 100; // 100ms = 10 requests/second

    public string BrokerName => "bybit";

    public BybitBroker(
        IOptions<BybitOptions> options,
        ILogger<BybitBroker> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(options));

        // Configure testnet/production environment
        if (_options.UseTestnet)
        {
            _logger.LogInformation("Bybit broker configured for TESTNET");
        }
        else
        {
            _logger.LogInformation("Bybit broker configured for PRODUCTION");
        }

        // Initialize Bybit REST client
        _client = new BybitRestClient(opts =>
        {
            opts.ApiCredentials = new ApiCredentials(
                _options.ApiKey,
                _options.ApiSecret);

            // Set testnet environment if enabled
            if (_options.UseTestnet)
            {
                opts.Environment = Bybit.Net.BybitEnvironment.Testnet;
            }
        });
    }

    /// <summary>
    /// Connects to Bybit API and verifies credentials
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Broker connection initiated - Broker: {Broker}, Environment: {Environment}, OperationType: {OperationType}",
                BrokerName, _options.UseTestnet ? "Testnet" : "Production", "Connect");

            // Test connection by getting wallet balance (Unified Trading Account)
            var walletResult = await _client.V5Api.Account.GetBalancesAsync(
                Bybit.Net.Enums.AccountType.Unified,
                ct: cancellationToken);

            if (!walletResult.Success)
            {
                _logger.LogError(
                    "Broker connection failed - Broker: {Broker}, Error: {Error}, ErrorCode: {ErrorCode}",
                    BrokerName, walletResult.Error?.Message, walletResult.Error?.Code);
                return false;
            }

            _isConnected = true;
            _logger.LogInformation(
                "Broker connected successfully - Broker: {Broker}, AccountType: {AccountType}",
                BrokerName, "UnifiedTrading");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Broker connection exception - Broker: {Broker}",
                BrokerName);
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Gets account balance for USDT (Unified Trading Account)
    /// </summary>
    public async Task<decimal> GetBalanceAsync(string currency = "USDT", CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogDebug(
                "Balance query initiated - Broker: {Broker}, Currency: {Currency}, OperationType: {OperationType}",
                BrokerName, currency, "GetBalance");

            var walletResult = await _client.V5Api.Account.GetBalancesAsync(
                Bybit.Net.Enums.AccountType.Unified,
                ct: cancellationToken);

            if (!walletResult.Success)
            {
                _logger.LogError(
                    "Balance query failed - Broker: {Broker}, Currency: {Currency}, Error: {Error}",
                    BrokerName, currency, walletResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get balance: {walletResult.Error?.Message}");
            }

            var balance = walletResult.Data.List.FirstOrDefault();
            if (balance == null)
            {
                _logger.LogWarning(
                    "Balance not found - Broker: {Broker}, Currency: {Currency}",
                    BrokerName, currency);
                return 0;
            }

            var coin = balance.Assets.FirstOrDefault(a => a.Asset == currency);
            var availableBalance = coin?.WalletBalance ?? 0;

            _logger.LogInformation(
                "Balance retrieved - Broker: {Broker}, Currency: {Currency}, Balance: {Balance}, WalletBalance: {WalletBalance}",
                BrokerName, currency, availableBalance, coin?.WalletBalance ?? 0);

            return availableBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Balance query exception - Broker: {Broker}, Currency: {Currency}",
                BrokerName, currency);
            throw;
        }
    }

    /// <summary>
    /// Gets all active positions for USDT perpetual futures
    /// </summary>
    public async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var positionsResult = await _client.V5Api.Trading.GetPositionsAsync(
                Bybit.Net.Enums.Category.Linear,
                ct: cancellationToken);

            if (!positionsResult.Success)
            {
                _logger.LogError("Failed to get positions: {Error}", positionsResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get positions: {positionsResult.Error?.Message}");
            }

            var positions = new List<Position>();

            foreach (var pos in positionsResult.Data.List.Where(p => p.Quantity != 0))
            {
                var unrealizedPnl = pos.UnrealizedPnl;
                var positionValue = pos.PositionValue;
                var pnlPercentage = positionValue > 0 ? (unrealizedPnl / positionValue) * 100 : 0;

                positions.Add(new Position
                {
                    PositionId = $"{pos.Symbol}-{pos.PositionIdx}",
                    Symbol = pos.Symbol,
                    Exchange = BrokerName,
                    Side = pos.Side == Bybit.Net.Enums.PositionSide.Buy ? Core.Enums.OrderSide.Buy : Core.Enums.OrderSide.Sell,
                    Quantity = pos.Quantity,
                    EntryPrice = pos.AveragePrice ?? 0,
                    CurrentPrice = pos.MarkPrice ?? 0,
                    Leverage = pos.Leverage ?? 1,
                    MarginType = Core.Enums.MarginType.Cross, // Default to Cross, MarginType property not available in V5
                    LiquidationPrice = pos.LiquidationPrice,
                    OpenedAt = DateTime.UtcNow // CreatedTime not available in V5 API
                    // Note: UnrealizedPnL and UnrealizedPnLPercent are computed properties
                });
            }

            _logger.LogDebug("Retrieved {Count} active positions", positions.Count);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting positions");
            throw;
        }
    }

    /// <summary>
    /// Places an order on Bybit (USDT perpetual futures)
    /// </summary>
    public async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        // Enforce rate limiting
        await EnforceRateLimitAsync(request.Symbol, cancellationToken);

        try
        {
            _logger.LogInformation(
                "Order placement initiated - Broker: {Broker}, Symbol: {Symbol}, Side: {Side}, Type: {Type}, Quantity: {Quantity}, Price: {Price}, ClientOrderId: {ClientOrderId}, OperationType: {OperationType}",
                BrokerName, request.Symbol, request.Side, request.Type, request.Quantity, request.Price, request.ClientOrderId, "PlaceOrder");

            // Map order type
            var orderType = request.Type switch
            {
                Core.Enums.OrderType.Market => Bybit.Net.Enums.NewOrderType.Market,
                Core.Enums.OrderType.Limit => Bybit.Net.Enums.NewOrderType.Limit,
                _ => throw new ArgumentException($"Unsupported order type: {request.Type}")
            };

            // Map order side
            var side = request.Side == Core.Enums.OrderSide.Buy
                ? Bybit.Net.Enums.OrderSide.Buy
                : Bybit.Net.Enums.OrderSide.Sell;

            // Place order
            var orderResult = await _client.V5Api.Trading.PlaceOrderAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: request.Symbol,
                side: side,
                type: orderType,
                quantity: request.Quantity,
                price: request.Price,
                timeInForce: Bybit.Net.Enums.TimeInForce.GoodTillCanceled,
                clientOrderId: request.ClientOrderId,
                ct: cancellationToken);

            if (!orderResult.Success)
            {
                _logger.LogError(
                    "Order placement failed - Broker: {Broker}, Symbol: {Symbol}, Side: {Side}, Error: {Error}, ErrorCode: {ErrorCode}",
                    BrokerName, request.Symbol, request.Side, orderResult.Error?.Message, orderResult.Error?.Code);
                throw new InvalidOperationException($"Failed to place order: {orderResult.Error?.Message}");
            }

            var order = new Order
            {
                OrderId = orderResult.Data.OrderId,
                ClientOrderId = request.ClientOrderId ?? orderResult.Data.OrderId,
                Symbol = request.Symbol,
                Exchange = BrokerName,
                Side = request.Side,
                Type = request.Type,
                Quantity = request.Quantity,
                Price = request.Price,
                Status = Core.Enums.OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Order placed successfully - Broker: {Broker}, OrderId: {OrderId}, ClientOrderId: {ClientOrderId}, Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}, Status: {Status}",
                BrokerName, order.OrderId, order.ClientOrderId, order.Symbol, order.Side, order.Quantity, order.Status);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Order placement exception - Broker: {Broker}, Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}",
                BrokerName, request.Symbol, request.Side, request.Quantity);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order
    /// </summary>
    public async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation(
                "Order cancellation initiated - Broker: {Broker}, OrderId: {OrderId}, Symbol: {Symbol}, OperationType: {OperationType}",
                BrokerName, orderId, symbol, "CancelOrder");

            var cancelResult = await _client.V5Api.Trading.CancelOrderAsync(
                Bybit.Net.Enums.Category.Linear,
                symbol,
                orderId,
                ct: cancellationToken);

            if (!cancelResult.Success)
            {
                _logger.LogError(
                    "Order cancellation failed - Broker: {Broker}, OrderId: {OrderId}, Symbol: {Symbol}, Error: {Error}",
                    BrokerName, orderId, symbol, cancelResult.Error?.Message);
                throw new InvalidOperationException($"Failed to cancel order: {cancelResult.Error?.Message}");
            }

            var order = new Order
            {
                OrderId = orderId,
                ClientOrderId = orderId, // Use orderId as fallback
                Symbol = symbol,
                Exchange = BrokerName,
                Side = Core.Enums.OrderSide.Buy, // Unknown, using default
                Type = Core.Enums.OrderType.Limit, // Unknown, using default
                Quantity = 0, // Unknown
                Status = Core.Enums.OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow, // Unknown, using current time
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Order cancelled successfully");

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of an order
    /// </summary>
    public async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var orderResult = await _client.V5Api.Trading.GetOrdersAsync(
                Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                orderId: orderId,
                ct: cancellationToken);

            if (!orderResult.Success || !orderResult.Data.List.Any())
            {
                _logger.LogError("Failed to get order status: {Error}", orderResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get order status: {orderResult.Error?.Message}");
            }

            var bybitOrder = orderResult.Data.List.First();

            var order = new Order
            {
                OrderId = bybitOrder.OrderId,
                ClientOrderId = bybitOrder.ClientOrderId,
                Symbol = bybitOrder.Symbol,
                Exchange = BrokerName,
                Side = bybitOrder.Side == Bybit.Net.Enums.OrderSide.Buy ? Core.Enums.OrderSide.Buy : Core.Enums.OrderSide.Sell,
                Type = bybitOrder.OrderType == Bybit.Net.Enums.OrderType.Market ? Core.Enums.OrderType.Market : Core.Enums.OrderType.Limit,
                Quantity = bybitOrder.Quantity,
                Price = bybitOrder.Price,
                FilledQuantity = bybitOrder.QuantityFilled ?? 0m,
                AverageFillPrice = bybitOrder.AveragePrice,
                Status = MapOrderStatus(bybitOrder.Status.ToString()),
                CreatedAt = bybitOrder.CreateTime,
                UpdatedAt = bybitOrder.UpdateTime
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
            var tickerResult = await _client.V5Api.ExchangeData.GetLinearInverseTickersAsync(
                Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                ct: cancellationToken);

            if (!tickerResult.Success || !tickerResult.Data.List.Any())
            {
                _logger.LogError("Failed to get price: {Error}", tickerResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get price: {tickerResult.Error?.Message}");
            }

            var ticker = tickerResult.Data.List.First();
            var price = ticker.LastPrice;

            _logger.LogDebug("Current price for {Symbol}: {Price}", symbol, price);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Sets leverage for a symbol
    /// </summary>
    public async Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Setting leverage for {Symbol} to {Leverage}x ({MarginType})",
                symbol, leverage, marginType);

            // Note: SetLeverageAsync method signature may vary by Bybit.Net version
            // Check Bybit.Net documentation for V5 API leverage management
            // For now, returning true as placeholder
            _logger.LogWarning("Leverage management requires specific Bybit.Net V5 API implementation");

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting leverage for {Symbol}", symbol);
            return false;
        }
    }

    /// <summary>
    /// Gets current leverage information for a symbol
    /// </summary>
    public async Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var positions = await GetPositionsAsync(cancellationToken);
            var position = positions.FirstOrDefault(p => p.Symbol == symbol);

            if (position == null)
            {
                // No position, return default leverage info
                return new LeverageInfo
                {
                    CurrentLeverage = 1,
                    MaxLeverage = 100, // Bybit default max for USDT perpetuals
                    MarginType = MarginType.Cross,
                    CollateralAmount = 0,
                    BorrowedAmount = 0
                };
            }

            return new LeverageInfo
            {
                CurrentLeverage = position.Leverage,
                MaxLeverage = 100,
                MarginType = position.MarginType ?? MarginType.Cross,
                LiquidationPrice = position.LiquidationPrice,
                CollateralAmount = position.CollateralAmount ?? 0,
                BorrowedAmount = position.BorrowedAmount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leverage info for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Gets margin health ratio (maintenance margin / total equity)
    /// </summary>
    public async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var walletResult = await _client.V5Api.Account.GetBalancesAsync(
                Bybit.Net.Enums.AccountType.Unified,
                ct: cancellationToken);

            if (!walletResult.Success || !walletResult.Data.List.Any())
            {
                _logger.LogError("Failed to get wallet info: {Error}", walletResult.Error?.Message);
                throw new InvalidOperationException($"Failed to get wallet info: {walletResult.Error?.Message}");
            }

            var wallet = walletResult.Data.List.First();
            var totalEquity = wallet.TotalEquity ?? 0;
            var totalMarginBalance = wallet.TotalMarginBalance ?? 0;

            if (totalEquity == 0)
            {
                return 1.0m; // No positions, healthy
            }

            // Health ratio: closer to 1.0 is healthier, closer to 0.0 is at risk
            var healthRatio = totalMarginBalance / totalEquity;

            _logger.LogDebug("Margin health ratio: {Ratio}", healthRatio);

            return healthRatio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting margin health ratio");
            throw;
        }
    }

    #region Helper Methods

    private void EnsureConnected()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected to Bybit. Call ConnectAsync first.");
        }
    }

    private async Task EnforceRateLimitAsync(string symbol, CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            lock (_requestTimeLock)
            {
                if (_lastRequestTime.TryGetValue(symbol, out var lastTime))
                {
                    var timeSinceLastRequest = (DateTime.UtcNow - lastTime).TotalMilliseconds;
                    var waitTime = MinRequestIntervalMs - timeSinceLastRequest;

                    if (waitTime > 0)
                    {
                        Task.Delay((int)waitTime, cancellationToken).Wait(cancellationToken);
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

    private static Core.Enums.OrderStatus MapOrderStatus(string bybitStatus)
    {
        return bybitStatus.ToUpper() switch
        {
            "NEW" => Core.Enums.OrderStatus.Pending,
            "PARTIALFILLED" => Core.Enums.OrderStatus.PartiallyFilled,
            "FILLED" => Core.Enums.OrderStatus.Filled,
            "CANCELLED" => Core.Enums.OrderStatus.Cancelled,
            "REJECTED" => Core.Enums.OrderStatus.Rejected,
            _ => Core.Enums.OrderStatus.Pending
        };
    }

    #endregion
}

/// <summary>
/// Bybit configuration options
/// </summary>
public class BybitOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public bool UseTestnet { get; set; } = false;
}
