using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// NinjaTrader broker implementation
/// Supports REST API mode (requires NinjaTrader 8 running)
/// </summary>
public class NinjaTraderBroker : BrokerBase
{
    private readonly HttpClient _httpClient;
    private readonly NinjaTraderOptions _options;

    protected override int MinRequestIntervalMs => 100;

    public override string BrokerName => "ninjatrader";

    public NinjaTraderBroker(
        IOptions<NinjaTraderOptions> options,
        ILogger<NinjaTraderBroker> logger,
        IHttpClientFactory httpClientFactory) : base(logger, 10)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClientFactory.CreateClient("NinjaTrader");

        // Configure base URL for REST API
        var baseUrl = $"http://{_options.Host}:{_options.Port}/";
        _httpClient.BaseAddress = new Uri(baseUrl);

        _logger.LogInformation(
            "NinjaTrader broker configured for {Host}:{Port} ({Mode} mode)",
            _options.Host, _options.Port, _options.ConnectionType);
    }

    /// <summary>
    /// Connects to NinjaTrader (verifies platform is running and accessible)
    /// </summary>
    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to NinjaTrader...");

            // Test connection by getting account info
            var response = await _httpClient.GetAsync("Account", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to connect to NinjaTrader: {StatusCode}", response.StatusCode);
                return false;
            }

            var accounts = await response.Content.ReadFromJsonAsync<List<NinjaTraderAccount>>(cancellationToken);

            if (accounts == null || accounts.Count == 0)
            {
                _logger.LogError("No NinjaTrader accounts found");
                return false;
            }

            _isConnected = true;
            _logger.LogInformation(
                "Connected to NinjaTrader successfully. Found {Count} account(s)",
                accounts.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during NinjaTrader connection");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Gets account balance
    /// </summary>
    public override async Task<decimal> GetBalanceAsync(string currency = "USD", CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var response = await _httpClient.GetAsync(
                $"Account/{_options.AccountId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var account = await response.Content.ReadFromJsonAsync<NinjaTraderAccountDetail>(cancellationToken);
            var balance = account?.CashValue ?? 0;

            _logger.LogDebug("NinjaTrader balance: ${Balance}", balance);

            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NinjaTrader balance");
            throw;
        }
    }

    /// <summary>
    /// Gets all active positions
    /// </summary>
    public override async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var response = await _httpClient.GetAsync(
                $"Positions/{_options.AccountId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ntPositions = await response.Content.ReadFromJsonAsync<List<NinjaTraderPosition>>(cancellationToken);
            var positions = new List<Position>();

            if (ntPositions != null)
            {
                foreach (var pos in ntPositions)
                {
                    var quantity = pos.Quantity;

                    positions.Add(new Position
                    {
                        PositionId = $"NT-{pos.Instrument}-{Guid.NewGuid()}",
                        Symbol = pos.Instrument,
                        Exchange = "NinjaTrader",
                        Side = quantity > 0 ? OrderSide.Buy : OrderSide.Sell,
                        Quantity = Math.Abs(quantity),
                        EntryPrice = pos.AveragePrice,
                        CurrentPrice = pos.MarketPrice,
                        MarginType = MarginType.Cross,
                        OpenedAt = DateTime.UtcNow // NinjaTrader doesn't provide open time in REST API
                        // Note: UnrealizedPnL is a computed property
                    });
                }
            }

            _logger.LogDebug("Retrieved {Count} NinjaTrader positions", positions.Count);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NinjaTrader positions");
            throw;
        }
    }

    /// <summary>
    /// Places an order on NinjaTrader
    /// </summary>
    public override async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Placing {Side} {OrderType} order: {Symbol} x {Quantity}",
                request.Side, request.Type, request.Symbol, request.Quantity);

            var orderRequest = new
            {
                Account = _options.AccountId,
                Instrument = request.Symbol,
                Action = request.Side == OrderSide.Buy ? "BUY" : "SELL",
                OrderType = MapOrderType(request.Type),
                Quantity = (int)request.Quantity,
                LimitPrice = request.Price,
                StopPrice = (decimal?)null,
                TimeInForce = "DAY",
                OrderId = request.ClientOrderId ?? Guid.NewGuid().ToString()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "Order",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var orderResponse = await response.Content.ReadFromJsonAsync<NinjaTraderOrderResponse>(cancellationToken);

            var order = new Order
            {
                OrderId = orderResponse?.OrderId ?? Guid.NewGuid().ToString(),
                ClientOrderId = request.ClientOrderId ?? orderResponse?.OrderId ?? Guid.NewGuid().ToString(),
                Symbol = request.Symbol,
                Exchange = BrokerName,
                Side = request.Side,
                Type = request.Type,
                Quantity = request.Quantity,
                Price = request.Price,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("NinjaTrader order placed successfully. OrderId: {OrderId}", order.OrderId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing NinjaTrader order for {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order
    /// </summary>
    public override async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Cancelling NinjaTrader order {OrderId}", orderId);

            var cancelRequest = new
            {
                OrderId = orderId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(cancelRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "CancelOrder",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var order = new Order
            {
                OrderId = orderId,
                ClientOrderId = orderId, // Use orderId as fallback
                Symbol = symbol,
                Exchange = BrokerName,
                Side = OrderSide.Buy, // Unknown, using default
                Type = OrderType.Limit, // Unknown, using default
                Quantity = 0, // Unknown
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow, // Unknown, using current time
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("NinjaTrader order cancelled successfully");

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling NinjaTrader order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of an order
    /// </summary>
    public override async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var response = await _httpClient.GetAsync(
                $"Orders/{_options.AccountId}/{orderId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ntOrder = await response.Content.ReadFromJsonAsync<NinjaTraderOrderDetail>(cancellationToken);

            var order = new Order
            {
                OrderId = ntOrder?.OrderId ?? orderId,
                ClientOrderId = ntOrder?.OrderId ?? orderId, // Use OrderId as ClientOrderId
                Symbol = ntOrder?.Instrument ?? symbol,
                Exchange = BrokerName,
                Side = ntOrder?.Action == "BUY" ? OrderSide.Buy : OrderSide.Sell,
                Type = ntOrder?.OrderType == "MARKET" ? OrderType.Market : OrderType.Limit,
                Quantity = ntOrder?.Quantity ?? 0,
                Price = ntOrder?.LimitPrice,
                FilledQuantity = ntOrder?.Filled ?? 0,
                Status = MapOrderStatus(ntOrder?.OrderState),
                CreatedAt = ntOrder?.Time ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NinjaTrader order status for {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets current market price for a symbol
    /// </summary>
    public override async Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var response = await _httpClient.GetAsync(
                $"MarketData/{symbol}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var marketData = await response.Content.ReadFromJsonAsync<NinjaTraderMarketData>(cancellationToken);
            var price = marketData?.Last ?? 0;

            _logger.LogDebug("NinjaTrader price for {Symbol}: ${Price}", symbol, price);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NinjaTrader price for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Sets leverage (not applicable for NinjaTrader)
    /// </summary>
    public override Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Leverage not directly applicable for NinjaTrader");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets leverage info (not directly applicable)
    /// </summary>
    public override Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LeverageInfo
        {
            CurrentLeverage = 1,
            MaxLeverage = 1,
            MarginType = MarginType.Cross,
            CollateralAmount = 0,
            BorrowedAmount = 0
        });
    }

    /// <summary>
    /// Gets margin health ratio
    /// </summary>
    public override async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        try
        {
            var response = await _httpClient.GetAsync(
                $"Account/{_options.AccountId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var account = await response.Content.ReadFromJsonAsync<NinjaTraderAccountDetail>(cancellationToken);

            var netLiquidation = account?.NetLiquidation ?? 0;
            var cashValue = account?.CashValue ?? 0;

            if (netLiquidation == 0) return 1.0m;

            var healthRatio = cashValue / netLiquidation;
            return healthRatio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NinjaTrader margin health");
            throw;
        }
    }

    #region Helper Methods

    private static string MapOrderType(OrderType type)
    {
        return type switch
        {
            OrderType.Market => "MARKET",
            OrderType.Limit => "LIMIT",
            _ => "MARKET"
        };
    }

    private static OrderStatus MapOrderStatus(string? status)
    {
        return status?.ToUpper() switch
        {
            "WORKING" or "ACCEPTED" => OrderStatus.Pending,
            "FILLED" => OrderStatus.Filled,
            "PARTFILLED" => OrderStatus.PartiallyFilled,
            "CANCELLED" or "CANCELED" => OrderStatus.Cancelled,
            "REJECTED" => OrderStatus.Rejected,
            _ => OrderStatus.Pending
        };
    }

    #endregion

    #region DTOs

    private class NinjaTraderAccount
    {
        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;
    }

    private class NinjaTraderAccountDetail
    {
        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;

        [JsonPropertyName("cashValue")]
        public decimal CashValue { get; set; }

        [JsonPropertyName("netLiquidation")]
        public decimal NetLiquidation { get; set; }

        [JsonPropertyName("realizedProfitLoss")]
        public decimal RealizedProfitLoss { get; set; }
    }

    private class NinjaTraderPosition
    {
        [JsonPropertyName("instrument")]
        public string Instrument { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("averagePrice")]
        public decimal AveragePrice { get; set; }

        [JsonPropertyName("marketPrice")]
        public decimal MarketPrice { get; set; }

        [JsonPropertyName("unrealizedProfitLoss")]
        public decimal UnrealizedProfitLoss { get; set; }
    }

    private class NinjaTraderOrderResponse
    {
        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    private class NinjaTraderOrderDetail
    {
        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("instrument")]
        public string? Instrument { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("orderType")]
        public string? OrderType { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("limitPrice")]
        public decimal? LimitPrice { get; set; }

        [JsonPropertyName("filled")]
        public decimal Filled { get; set; }

        [JsonPropertyName("orderState")]
        public string? OrderState { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }
    }

    private class NinjaTraderMarketData
    {
        [JsonPropertyName("instrument")]
        public string Instrument { get; set; } = string.Empty;

        [JsonPropertyName("last")]
        public decimal Last { get; set; }

        [JsonPropertyName("bid")]
        public decimal Bid { get; set; }

        [JsonPropertyName("ask")]
        public decimal Ask { get; set; }
    }

    #endregion
}

/// <summary>
/// NinjaTrader configuration options
/// </summary>
public class NinjaTraderOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "REST"; // REST or NinjaScript
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 36973; // Default ATI port
}
