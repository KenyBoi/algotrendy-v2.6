using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Bybit.Net.Clients;
using BybitOrderStatus = Bybit.Net.Enums.OrderStatus;
using Bybit.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Infrastructure.Brokers.Bybit;

/// <summary>
/// Bybit broker implementation for trading on Bybit exchange
/// Supports spot and linear (futures) trading with leverage
/// </summary>
public class BybitBroker : IBroker
{
    private readonly BybitClient _client;
    private readonly BybitSocketClient _socketClient;
    private readonly ILogger<BybitBroker> _logger;
    private bool _isConnected = false;

    /// <summary>
    /// Broker name identifier
    /// </summary>
    public string BrokerName => "Bybit";

    /// <summary>
    /// Initialize Bybit broker with API credentials
    /// </summary>
    public BybitBroker(
        string apiKey,
        string apiSecret,
        bool testnet,
        ILogger<BybitBroker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var clientOptions = new BybitClientOptions
        {
            ApiCredentials = new ApiCredentials(apiKey, apiSecret),
            BaseAddress = testnet
                ? BybitApiAddresses.TestNet
                : BybitApiAddresses.LiveAddress
        };

        _client = new BybitClient(clientOptions);
        _socketClient = new BybitSocketClient();

        _logger.LogInformation(
            "Bybit broker initialized: {Environment}",
            testnet ? "Testnet" : "Live");
    }

    /// <summary>
    /// Connect to Bybit API and verify credentials
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to Bybit...");

            // Test connection by getting account info
            var accountResult = await _client.V5Api.Account.GetAccountInfoAsync(
                cancellationToken: cancellationToken);

            if (!accountResult.Success)
            {
                _logger.LogError(
                    "Bybit connection failed: {ErrorMessage}",
                    accountResult.Error?.Message ?? "Unknown error");
                return false;
            }

            _isConnected = true;
            _logger.LogInformation("✅ Connected to Bybit successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Bybit connection");
            return false;
        }
    }

    /// <summary>
    /// Get account balance for specified currency
    /// </summary>
    public async Task<decimal> GetBalanceAsync(
        string currency = "USDT",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Attempted to get balance while disconnected");
                return 0;
            }

            var result = await _client.V5Api.Account.GetWalletBalanceAsync(
                accountType: Bybit.Net.Enums.AccountType.Unified,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to get wallet balance: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");
                return 0;
            }

            var wallet = result.Data?.FirstOrDefault();
            if (wallet == null)
            {
                _logger.LogWarning("No wallet data returned from Bybit");
                return 0;
            }

            var coinBalance = wallet.Coins?.FirstOrDefault(c =>
                c.Coin.Equals(currency, StringComparison.OrdinalIgnoreCase));

            if (coinBalance == null)
            {
                _logger.LogWarning(
                    "Currency {Currency} not found in wallet",
                    currency);
                return 0;
            }

            return coinBalance.AvailableToWithdraw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting balance for {Currency}", currency);
            return 0;
        }
    }

    /// <summary>
    /// Get all active positions
    /// </summary>
    public async Task<IEnumerable<Position>> GetPositionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Attempted to get positions while disconnected");
                return Enumerable.Empty<Position>();
            }

            var result = await _client.V5Api.Trading.GetPositionsAsync(
                category: Bybit.Net.Enums.Category.Linear,
                settleCoin: "USDT",
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to get positions: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");
                return Enumerable.Empty<Position>();
            }

            var positions = new List<Position>();

            if (result.Data?.List != null)
            {
                foreach (var pos in result.Data.List)
                {
                    if (pos.Size > 0)
                    {
                        var position = new Position
                        {
                            Symbol = pos.Symbol,
                            Side = pos.Side.ToString().ToLower() == "buy"
                                ? OrderSide.Buy
                                : OrderSide.Sell,
                            Size = pos.Size,
                            EntryPrice = pos.AveragePrice ?? 0,
                            MarkPrice = pos.MarkPrice ?? 0,
                            LiquidationPrice = pos.LiquidationPrice ?? 0,
                            UnrealizedPnL = pos.UnrealizedPnl ?? 0,
                            UnrealizedPnLPercent = (pos.UnrealizedPnlPercent ?? 0) * 100,
                            Leverage = pos.Leverage ?? 1,
                            MarginMode = (MarginType)Enum.Parse(
                                typeof(MarginType),
                                pos.IsIsolated ? "Isolated" : "Cross")
                        };

                        positions.Add(position);
                        _logger.LogDebug(
                            "Position loaded: {Symbol}, Size: {Size}, Leverage: {Leverage}x",
                            pos.Symbol,
                            pos.Size,
                            pos.Leverage);
                    }
                }
            }

            _logger.LogInformation(
                "Loaded {PositionCount} active positions",
                positions.Count);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting positions");
            return Enumerable.Empty<Position>();
        }
    }

    /// <summary>
    /// Place an order on Bybit
    /// </summary>
    public async Task<Order> PlaceOrderAsync(
        OrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            _logger.LogInformation(
                "Placing {Side} {OrderType} order for {Symbol}: {Quantity} @ {Price}",
                request.Side,
                request.OrderType,
                request.Symbol,
                request.Quantity,
                request.Price ?? 0);

            var side = request.Side == OrderSide.Buy
                ? Bybit.Net.Enums.OrderSide.Buy
                : Bybit.Net.Enums.OrderSide.Sell;

            var orderType = request.OrderType == OrderType.Market
                ? Bybit.Net.Enums.OrderType.Market
                : Bybit.Net.Enums.OrderType.Limit;

            var result = await _client.V5Api.Trading.PlaceOrderAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: request.Symbol,
                side: side,
                type: orderType,
                quantity: request.Quantity,
                price: request.Price,
                clientOrderId: request.ClientOrderId,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Order placement failed: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");

                throw new InvalidOperationException(
                    $"Order placement failed: {result.Error?.Message}");
            }

            var bybitOrder = result.Data;

            var order = new Order
            {
                OrderId = bybitOrder.OrderId.ToString(),
                ClientOrderId = request.ClientOrderId,
                Symbol = request.Symbol,
                Side = request.Side,
                OrderType = request.OrderType,
                Quantity = request.Quantity,
                Price = request.Price ?? 0,
                FilledQuantity = 0,
                AveragePrice = 0,
                Status = ConvertOrderStatus(bybitOrder.Status),
                CreateTime = DateTime.UtcNow,
                UpdateTime = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Order placed successfully: {OrderId}",
                order.OrderId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while placing order");
            throw;
        }
    }

    /// <summary>
    /// Cancel an active order
    /// </summary>
    public async Task<Order> CancelOrderAsync(
        string orderId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            _logger.LogInformation(
                "Cancelling order {OrderId} for {Symbol}",
                orderId,
                symbol);

            var result = await _client.V5Api.Trading.CancelOrderAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                orderId: long.TryParse(orderId, out var id) ? id : 0,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Order cancellation failed: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");

                throw new InvalidOperationException(
                    $"Order cancellation failed: {result.Error?.Message}");
            }

            // Fetch updated order status
            return await GetOrderStatusAsync(orderId, symbol, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while cancelling order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Get current status of an order
    /// </summary>
    public async Task<Order> GetOrderStatusAsync(
        string orderId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            var result = await _client.V5Api.Trading.GetOrderHistoryAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to get order status: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");

                throw new InvalidOperationException(
                    $"Failed to get order status: {result.Error?.Message}");
            }

            var bybitOrder = result.Data?.List?.FirstOrDefault(o =>
                o.OrderId.ToString() == orderId);

            if (bybitOrder == null)
            {
                throw new InvalidOperationException(
                    $"Order {orderId} not found");
            }

            var order = new Order
            {
                OrderId = bybitOrder.OrderId.ToString(),
                Symbol = bybitOrder.Symbol,
                Side = bybitOrder.Side.ToString().ToLower() == "buy"
                    ? OrderSide.Buy
                    : OrderSide.Sell,
                OrderType = bybitOrder.OrderType.ToString().ToLower() == "market"
                    ? OrderType.Market
                    : OrderType.Limit,
                Quantity = bybitOrder.Quantity ?? 0,
                Price = bybitOrder.Price ?? 0,
                FilledQuantity = bybitOrder.QuantityFilled ?? 0,
                AveragePrice = bybitOrder.AverageFilledPrice ?? 0,
                Status = ConvertOrderStatus(bybitOrder.Status),
                CreateTime = bybitOrder.CreateTime ?? DateTime.UtcNow,
                UpdateTime = bybitOrder.UpdateTime ?? DateTime.UtcNow
            };

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting order status for {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Get current market price for a symbol
    /// </summary>
    public async Task<decimal> GetCurrentPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Attempted to get price while disconnected");
                return 0;
            }

            var result = await _client.V5Api.ExchangeData.GetTickersAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to get price for {Symbol}: {ErrorMessage}",
                    symbol,
                    result.Error?.Message ?? "Unknown error");
                return 0;
            }

            var ticker = result.Data?.List?.FirstOrDefault();
            return ticker?.LastPrice ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting current price for {Symbol}", symbol);
            return 0;
        }
    }

    /// <summary>
    /// Set leverage for a symbol
    /// </summary>
    public async Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            _logger.LogInformation(
                "Setting leverage for {Symbol}: {Leverage}x ({MarginType})",
                symbol,
                leverage,
                marginType);

            // Bybit max leverage is 100x
            if (leverage > 100)
            {
                _logger.LogWarning(
                    "Requested leverage {Leverage}x exceeds maximum 100x, capping at 100x",
                    leverage);
                leverage = 100;
            }

            var result = await _client.V5Api.Account.SetLeverageAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                buyLeverage: leverage,
                sellLeverage: leverage,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to set leverage: {ErrorMessage}",
                    result.Error?.Message ?? "Unknown error");
                return false;
            }

            _logger.LogInformation(
                "Leverage set successfully for {Symbol}: {Leverage}x",
                symbol,
                leverage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while setting leverage for {Symbol}", symbol);
            return false;
        }
    }

    /// <summary>
    /// Get leverage information for a symbol
    /// </summary>
    public async Task<LeverageInfo> GetLeverageInfoAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            var result = await _client.V5Api.Trading.GetPositionsAsync(
                category: Bybit.Net.Enums.Category.Linear,
                symbol: symbol,
                settleCoin: "USDT",
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to get leverage info: {result.Error?.Message}");
            }

            var position = result.Data?.List?.FirstOrDefault();
            if (position == null)
            {
                throw new InvalidOperationException($"No position found for {symbol}");
            }

            return new LeverageInfo
            {
                Symbol = symbol,
                CurrentLeverage = position.Leverage ?? 1,
                MaxLeverage = 100, // Bybit max leverage
                MarginType = position.IsIsolated ? MarginType.Isolated : MarginType.Cross,
                MaintenanceMarginRate = position.MaintenanceMarginRate ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting leverage info for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Get margin health ratio for the account
    /// </summary>
    public async Task<decimal> GetMarginHealthRatioAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            var result = await _client.V5Api.Account.GetWalletBalanceAsync(
                accountType: Bybit.Net.Enums.AccountType.Unified,
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to get margin health: {result.Error?.Message}");
            }

            var wallet = result.Data?.FirstOrDefault();
            if (wallet == null)
            {
                return 0;
            }

            // Calculate margin health ratio
            var totalWalletBalance = wallet.TotalWalletBalance;
            var totalMarginBalance = wallet.TotalMarginBalance;

            if (totalMarginBalance <= 0)
            {
                return 1; // Healthy if no margin used
            }

            return totalWalletBalance / totalMarginBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting margin health ratio");
            throw;
        }
    }

    /// <summary>
    /// Convert Bybit order status to AlgoTrendy OrderStatus
    /// </summary>
    private OrderStatus ConvertOrderStatus(BybitOrderStatus? bybitStatus)
    {
        return bybitStatus?.ToString() switch
        {
            "Created" or "New" => OrderStatus.Pending,
            "PartiallyFilled" => OrderStatus.PartiallyFilled,
            "Filled" => OrderStatus.Filled,
            "Cancelled" or "Rejected" => OrderStatus.Cancelled,
            "Expired" => OrderStatus.Expired,
            _ => OrderStatus.Pending
        };
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
        _socketClient?.Dispose();
    }
}
