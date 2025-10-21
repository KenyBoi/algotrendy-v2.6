using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Alpaca.Markets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Alpaca broker implementation for stocks and crypto trading
/// Supports: Stocks (US equities), Options, Crypto (via Alpaca Crypto)
/// Note: Alpaca is primarily a cash account broker with limited margin support
/// </summary>
public class AlpacaBroker : BrokerBase
{
    private IAlpacaTradingClient? _tradingClient;
    private IAlpacaDataClient? _dataClient;
    private readonly AlpacaOptions _options;

    protected override int MinRequestIntervalMs => 300; // ~200 requests/minute

    public override string BrokerName => "alpaca";

    public AlpacaBroker(
        IOptions<AlpacaOptions> options,
        ILogger<AlpacaBroker> logger) : base(logger, 10)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets or initializes the Alpaca clients (lazy initialization)
    /// </summary>
    private void InitializeClients()
    {
        if (_tradingClient == null)
        {
            var environment = _options.UsePaper ? Environments.Paper : Environments.Live;

            var secretKey = new SecretKey(_options.ApiKey, _options.ApiSecret);

            _tradingClient = environment.GetAlpacaTradingClient(secretKey);
            _dataClient = environment.GetAlpacaDataClient(secretKey);

            _logger.LogInformation(
                "Alpaca clients initialized for {Environment} environment",
                _options.UsePaper ? "PAPER" : "LIVE");
        }
    }

    /// <summary>
    /// Connects to Alpaca API and verifies credentials
    /// </summary>
    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to Alpaca ({Environment})...",
                _options.UsePaper ? "Paper" : "Live");

            InitializeClients();

            // Test connection by getting account info
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);

            _isConnected = true;
            _logger.LogInformation(
                "Connected to Alpaca successfully. Account: {Status}, Equity: ${Equity:F2}, Buying Power: ${BuyingPower:F2}",
                account.Status,
                account.Equity,
                account.BuyingPower);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Alpaca");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Gets current balance in the specified currency
    /// </summary>
    public override async Task<decimal> GetBalanceAsync(string currency = "USD", CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);

            // Alpaca uses USD, return equity (total account value)
            return account.Equity ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance from Alpaca");
            return 0m;
        }
    }

    /// <summary>
    /// Gets all current positions
    /// </summary>
    public override async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var alpacaPositions = await _tradingClient!.ListPositionsAsync(cancellationToken);
            var positions = new List<Position>();

            foreach (var pos in alpacaPositions)
            {
                positions.Add(new Position
                {
                    Symbol = pos.Symbol,
                    Size = pos.Quantity,
                    Side = pos.Side == PositionSide.Long ? "buy" : "sell",
                    EntryPrice = pos.AverageEntryPrice,
                    CurrentPrice = pos.AssetCurrentPrice ?? pos.AverageEntryPrice,
                    UnrealizedPnl = pos.UnrealizedProfitLoss ?? 0m,
                    RealizedPnl = 0m, // Alpaca doesn't provide this per position
                    Leverage = 1m, // Alpaca is primarily cash account (1x leverage)
                    Broker = BrokerName
                });
            }

            _logger.LogInformation("Retrieved {Count} positions from Alpaca", positions.Count);
            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get positions from Alpaca");
            return Array.Empty<Position>();
        }
    }

    /// <summary>
    /// Places a new order
    /// </summary>
    public override async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(request.Symbol, cancellationToken);

        try
        {
            _logger.LogInformation(
                "Placing {OrderType} {Side} order: {Symbol} x {Quantity}",
                request.Type, request.Side, request.Symbol, request.Quantity);

            var orderSide = request.Side.ToLowerInvariant() == "buy"
                ? OrderSide.Buy
                : OrderSide.Sell;

            // Create order request based on type
            NewOrderRequest alpacaOrderRequest;

            switch (request.Type)
            {
                case Core.Enums.OrderType.Market:
                    alpacaOrderRequest = orderSide == OrderSide.Buy
                        ? MarketOrder.Buy(request.Symbol, OrderQuantity.Fractional(request.Quantity))
                        : MarketOrder.Sell(request.Symbol, OrderQuantity.Fractional(request.Quantity));
                    break;

                case Core.Enums.OrderType.Limit:
                    if (!request.Price.HasValue)
                        throw new ArgumentException("Limit orders require a price");

                    alpacaOrderRequest = orderSide == OrderSide.Buy
                        ? LimitOrder.Buy(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.Price.Value)
                        : LimitOrder.Sell(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.Price.Value);
                    break;

                case Core.Enums.OrderType.StopLoss:
                    if (!request.StopPrice.HasValue)
                        throw new ArgumentException("Stop loss orders require a stop price");

                    alpacaOrderRequest = orderSide == OrderSide.Buy
                        ? StopOrder.Buy(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.StopPrice.Value)
                        : StopOrder.Sell(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.StopPrice.Value);
                    break;

                case Core.Enums.OrderType.StopLimit:
                    if (!request.Price.HasValue || !request.StopPrice.HasValue)
                        throw new ArgumentException("Stop limit orders require both price and stop price");

                    alpacaOrderRequest = orderSide == OrderSide.Buy
                        ? StopLimitOrder.Buy(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.StopPrice.Value, request.Price.Value)
                        : StopLimitOrder.Sell(request.Symbol, OrderQuantity.Fractional(request.Quantity), request.StopPrice.Value, request.Price.Value);
                    break;

                default:
                    throw new ArgumentException($"Unsupported order type: {request.Type}");
            }

            // Set time in force (default to day order)
            alpacaOrderRequest.Duration = TimeInForce.Day;

            // Submit the order
            var alpacaOrder = await _tradingClient!.PostOrderAsync(alpacaOrderRequest, cancellationToken);

            _logger.LogInformation("Order placed successfully: {OrderId}", alpacaOrder.OrderId);

            return new Order
            {
                OrderId = alpacaOrder.OrderId.ToString(),
                Symbol = alpacaOrder.Symbol,
                Side = alpacaOrder.OrderSide == OrderSide.Buy ? "buy" : "sell",
                Type = MapOrderType(alpacaOrder.OrderType),
                Quantity = alpacaOrder.Quantity ?? 0m,
                Price = alpacaOrder.LimitPrice,
                Status = MapOrderStatus(alpacaOrder.OrderStatus),
                FilledQuantity = alpacaOrder.FilledQuantity ?? 0m,
                Timestamp = alpacaOrder.CreatedAtUtc ?? DateTime.UtcNow,
                Broker = BrokerName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place order for {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Cancels an existing order
    /// </summary>
    public override async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(symbol, cancellationToken);

        try
        {
            var orderGuid = Guid.Parse(orderId);

            // Get order details before cancelling
            var alpacaOrder = await _tradingClient!.GetOrderAsync(orderGuid, cancellationToken);

            // Cancel the order
            var success = await _tradingClient.DeleteOrderAsync(orderGuid, cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled: {Success}", orderId, success);

            // Fetch the updated order status
            var cancelledOrder = await _tradingClient.GetOrderAsync(orderGuid, cancellationToken);

            return new Order
            {
                OrderId = cancelledOrder.OrderId.ToString(),
                Symbol = cancelledOrder.Symbol,
                Side = cancelledOrder.OrderSide == OrderSide.Buy ? "buy" : "sell",
                Type = MapOrderType(cancelledOrder.OrderType),
                Quantity = cancelledOrder.Quantity ?? 0m,
                Price = cancelledOrder.LimitPrice,
                Status = Core.Enums.OrderStatus.Cancelled,
                FilledQuantity = cancelledOrder.FilledQuantity ?? 0m,
                Timestamp = cancelledOrder.CreatedAtUtc ?? DateTime.UtcNow,
                Broker = BrokerName
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
        EnsureConnected();
        await EnforceRateLimitAsync(symbol, cancellationToken);

        try
        {
            var orderGuid = Guid.Parse(orderId);
            var alpacaOrder = await _tradingClient!.GetOrderAsync(orderGuid, cancellationToken);

            return new Order
            {
                OrderId = alpacaOrder.OrderId.ToString(),
                Symbol = alpacaOrder.Symbol,
                Side = alpacaOrder.OrderSide == OrderSide.Buy ? "buy" : "sell",
                Type = MapOrderType(alpacaOrder.OrderType),
                Quantity = alpacaOrder.Quantity ?? 0m,
                Price = alpacaOrder.LimitPrice,
                Status = MapOrderStatus(alpacaOrder.OrderStatus),
                FilledQuantity = alpacaOrder.FilledQuantity ?? 0m,
                Timestamp = alpacaOrder.CreatedAtUtc ?? DateTime.UtcNow,
                Broker = BrokerName
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
        EnsureConnected();
        await EnforceRateLimitAsync(symbol, cancellationToken);

        try
        {
            var request = new LatestMarketDataRequest(symbol);
            var quote = await _dataClient!.GetLatestQuoteAsync(request, cancellationToken);

            // Use bid price for consistency
            return quote.BidPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current price for {Symbol}", symbol);
            return 0m;
        }
    }

    /// <summary>
    /// Sets leverage for a symbol
    /// NOTE: Alpaca is primarily a cash account broker with limited margin support.
    /// Most accounts don't support leverage trading. This method will return false for unsupported operations.
    /// </summary>
    public override async Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(symbol, cancellationToken);

        try
        {
            // Get account info to check if margin is enabled
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);

            if (!account.IsTradingBlocked && account.BuyingPower > account.Equity)
            {
                // Account has margin, but Alpaca doesn't allow per-symbol leverage configuration
                _logger.LogWarning(
                    "Alpaca does not support per-symbol leverage configuration. " +
                    "Leverage is determined by account type and overall buying power.");
                return false;
            }

            _logger.LogWarning(
                "Account does not have margin enabled. Alpaca leverage requires a margin account.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set leverage for {Symbol}", symbol);
            return false;
        }
    }

    /// <summary>
    /// Gets current leverage information for a symbol
    /// NOTE: Alpaca doesn't provide per-symbol leverage. Returns default 1x leverage.
    /// </summary>
    public override async Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(symbol, cancellationToken);

        try
        {
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);

            // Calculate effective leverage based on buying power vs equity
            var equity = account.Equity ?? 0m;
            var buyingPower = account.BuyingPower ?? 0m;
            var effectiveLeverage = equity > 0
                ? buyingPower / equity
                : 1m;

            return new LeverageInfo
            {
                Symbol = symbol,
                Leverage = effectiveLeverage,
                MaxLeverage = effectiveLeverage, // Alpaca doesn't provide max leverage per symbol
                MarginType = MarginType.Cross, // Alpaca uses cross margin for margin accounts
                Broker = BrokerName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leverage info for {Symbol}", symbol);

            // Return default values on error
            return new LeverageInfo
            {
                Symbol = symbol,
                Leverage = 1m,
                MaxLeverage = 1m,
                MarginType = MarginType.Cross,
                Broker = BrokerName
            };
        }
    }

    /// <summary>
    /// Gets margin health ratio for the account
    /// Returns a ratio where 1.0 = healthy, values approaching 0 indicate risk of margin call
    /// </summary>
    public override async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            var account = await _tradingClient!.GetAccountAsync(cancellationToken);

            // Check if account is blocked from trading (margin call scenario)
            if (account.IsTradingBlocked)
            {
                _logger.LogWarning("Account is blocked from trading - possible margin call");
                return 0m;
            }

            // Calculate health ratio based on equity vs buying power
            // For cash accounts, this will be 1.0
            // For margin accounts, buying power > equity indicates healthy margin
            if (account.BuyingPower <= 0)
                return 0m;

            // Maintenance margin requirement (Alpaca uses ~25% for stocks)
            var maintenanceMargin = account.InitialMargin * 0.25m;

            if (maintenanceMargin <= 0)
                return 1m; // Cash account, always healthy

            // Health ratio = (Equity - Maintenance Margin) / Maintenance Margin
            var healthRatio = (account.Equity - maintenanceMargin) / maintenanceMargin;

            return Math.Max(0m, Math.Min(1m, healthRatio)); // Clamp between 0 and 1
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get margin health ratio");
            return 1m; // Return healthy by default to avoid false alarms
        }
    }

    #region Helper Methods

    /// <summary>
    /// Maps Alpaca order type to AlgoTrendy order type
    /// </summary>
    private Core.Enums.OrderType MapOrderType(Alpaca.Markets.OrderType alpacaType)
    {
        return alpacaType switch
        {
            Alpaca.Markets.OrderType.Market => Core.Enums.OrderType.Market,
            Alpaca.Markets.OrderType.Limit => Core.Enums.OrderType.Limit,
            Alpaca.Markets.OrderType.Stop => Core.Enums.OrderType.StopLoss,
            Alpaca.Markets.OrderType.StopLimit => Core.Enums.OrderType.StopLimit,
            Alpaca.Markets.OrderType.TrailingStop => Core.Enums.OrderType.StopLoss, // Map to stop loss
            _ => Core.Enums.OrderType.Market
        };
    }

    /// <summary>
    /// Maps Alpaca order status to AlgoTrendy order status
    /// </summary>
    private Core.Enums.OrderStatus MapOrderStatus(Alpaca.Markets.OrderStatus alpacaStatus)
    {
        return alpacaStatus switch
        {
            Alpaca.Markets.OrderStatus.New => Core.Enums.OrderStatus.Open,
            Alpaca.Markets.OrderStatus.PartiallyFilled => Core.Enums.OrderStatus.PartiallyFilled,
            Alpaca.Markets.OrderStatus.Filled => Core.Enums.OrderStatus.Filled,
            Alpaca.Markets.OrderStatus.Canceled => Core.Enums.OrderStatus.Cancelled,
            Alpaca.Markets.OrderStatus.Expired => Core.Enums.OrderStatus.Expired,
            Alpaca.Markets.OrderStatus.Rejected => Core.Enums.OrderStatus.Rejected,
            Alpaca.Markets.OrderStatus.PendingCancel => Core.Enums.OrderStatus.Open,
            Alpaca.Markets.OrderStatus.PendingNew => Core.Enums.OrderStatus.Open,
            Alpaca.Markets.OrderStatus.Accepted => Core.Enums.OrderStatus.Open,
            Alpaca.Markets.OrderStatus.Stopped => Core.Enums.OrderStatus.Rejected,
            Alpaca.Markets.OrderStatus.Suspended => Core.Enums.OrderStatus.Rejected,
            Alpaca.Markets.OrderStatus.Calculated => Core.Enums.OrderStatus.Open,
            _ => Core.Enums.OrderStatus.Open
        };
    }

    #endregion
}

/// <summary>
/// Configuration options for Alpaca broker
/// </summary>
public class AlpacaOptions
{
    /// <summary>
    /// Alpaca API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Alpaca API Secret
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Use paper trading environment (default: true for safety)
    /// </summary>
    public bool UsePaper { get; set; } = true;
}
