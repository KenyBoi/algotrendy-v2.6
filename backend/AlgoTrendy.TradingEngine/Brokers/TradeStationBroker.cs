using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// TradeStation broker implementation for US equities trading
/// Supports both paper trading (sim-api) and production trading
/// </summary>
public class TradeStationBroker : BrokerBase
{
    private readonly HttpClient _httpClient;
    private readonly TradeStationOptions _options;
    private string? _accessToken = null;
    private DateTime _tokenExpiry = DateTime.MinValue;

    protected override int MinRequestIntervalMs => 100;

    public override string BrokerName => "tradestation";

    public TradeStationBroker(
        IOptions<TradeStationOptions> options,
        ILogger<TradeStationBroker> logger,
        IHttpClientFactory httpClientFactory) : base(logger, 10)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClientFactory.CreateClient("TradeStation");

        // Configure base URL based on paper trading flag
        var baseUrl = _options.UsePaperTrading
            ? "https://sim-api.tradestation.com/v3"
            : "https://api.tradestation.com/v3";

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation(
            "TradeStation broker configured for {Mode}",
            _options.UsePaperTrading ? "PAPER TRADING" : "PRODUCTION");
    }

    /// <summary>
    /// Connects to TradeStation API using OAuth 2.0
    /// </summary>
    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Connecting to TradeStation...");

            // Authenticate using OAuth 2.0
            var authSuccess = await AuthenticateAsync(cancellationToken);
            if (!authSuccess)
            {
                _logger.LogError("Failed to authenticate with TradeStation");
                return false;
            }

            // Test connection by getting account info
            var accountsResponse = await _httpClient.GetAsync("brokerage/accounts", cancellationToken);
            if (!accountsResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to verify TradeStation connection: {StatusCode}",
                    accountsResponse.StatusCode);
                return false;
            }

            _isConnected = true;
            _logger.LogInformation("Connected to TradeStation successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during TradeStation connection");
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
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            var response = await _httpClient.GetAsync(
                $"brokerage/accounts/{_options.AccountId}/balances",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var balances = await response.Content.ReadFromJsonAsync<TradeStationBalances>(cancellationToken);
            var cashBalance = balances?.CashBalance ?? 0;

            _logger.LogDebug("TradeStation balance: ${Balance}", cashBalance);

            return cashBalance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TradeStation balance");
            throw;
        }
    }

    /// <summary>
    /// Gets all active positions
    /// </summary>
    public override async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            var response = await _httpClient.GetAsync(
                $"brokerage/accounts/{_options.AccountId}/positions",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var tsPositions = await response.Content.ReadFromJsonAsync<TradeStationPositionsResponse>(cancellationToken);
            var positions = new List<Position>();

            if (tsPositions?.Positions != null)
            {
                foreach (var pos in tsPositions.Positions)
                {
                    positions.Add(new Position
                    {
                        PositionId = $"TS-{pos.Symbol}-{pos.TimeStamp.Ticks}",
                        Symbol = pos.Symbol,
                        Exchange = "TradeStation",
                        Side = pos.Quantity > 0 ? OrderSide.Buy : OrderSide.Sell,
                        Quantity = Math.Abs(pos.Quantity),
                        EntryPrice = pos.AveragePrice,
                        CurrentPrice = pos.Last,
                        MarginType = MarginType.Cross,
                        OpenedAt = pos.TimeStamp
                    });
                }
            }

            _logger.LogDebug("Retrieved {Count} TradeStation positions", positions.Count);

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TradeStation positions");
            throw;
        }
    }

    /// <summary>
    /// Places an order on TradeStation
    /// </summary>
    public override async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);
        await EnforceRateLimitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Placing {Side} {OrderType} order: {Symbol} x {Quantity}",
                request.Side, request.Type, request.Symbol, request.Quantity);

            var orderRequest = new
            {
                AccountID = _options.AccountId,
                Symbol = request.Symbol,
                Quantity = request.Quantity.ToString(),
                OrderType = MapOrderType(request.Type),
                TimeInForce = new { Duration = "DAY" },
                Route = "Intelligent",
                LimitPrice = request.Price?.ToString(),
                TradeAction = request.Side == OrderSide.Buy ? "BUY" : "SELL"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderRequest),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"orderexecution/orders",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var orderResponse = await response.Content.ReadFromJsonAsync<TradeStationOrderResponse>(cancellationToken);

            var order = new Order
            {
                OrderId = orderResponse?.Orders?[0]?.OrderID ?? Guid.NewGuid().ToString(),
                ClientOrderId = request.ClientOrderId ?? Guid.NewGuid().ToString(),
                Symbol = request.Symbol,
                Exchange = BrokerName,
                Side = request.Side,
                Type = request.Type,
                Quantity = request.Quantity,
                Price = request.Price,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("TradeStation order placed successfully. OrderId: {OrderId}", order.OrderId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing TradeStation order for {Symbol}", request.Symbol);
            throw;
        }
    }

    /// <summary>
    /// Cancels an active order
    /// </summary>
    public override async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Cancelling TradeStation order {OrderId}", orderId);

            var response = await _httpClient.DeleteAsync(
                $"orderexecution/orders/{orderId}",
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

            _logger.LogInformation("TradeStation order cancelled successfully");

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling TradeStation order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets the current status of an order
    /// </summary>
    public override async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            var response = await _httpClient.GetAsync(
                $"orderexecution/orders/{orderId}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var tsOrder = await response.Content.ReadFromJsonAsync<TradeStationOrderDetail>(cancellationToken);

            var order = new Order
            {
                OrderId = tsOrder?.OrderID ?? orderId,
                ClientOrderId = tsOrder?.OrderID ?? orderId, // Use OrderID as ClientOrderId
                Symbol = symbol,
                Exchange = BrokerName,
                Side = tsOrder?.TradeAction == "BUY" ? OrderSide.Buy : OrderSide.Sell,
                Type = tsOrder?.OrderType == "Market" ? OrderType.Market : OrderType.Limit,
                Quantity = decimal.Parse(tsOrder?.Quantity ?? "0"),
                Price = decimal.TryParse(tsOrder?.LimitPrice, out var price) ? price : null,
                FilledQuantity = decimal.Parse(tsOrder?.FilledQuantity ?? "0"),
                Status = MapOrderStatus(tsOrder?.Status),
                CreatedAt = tsOrder?.TimeStamp ?? DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TradeStation order status for {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Gets current market price for a symbol
    /// </summary>
    public override async Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            var response = await _httpClient.GetAsync(
                $"marketdata/quotes/{symbol}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var quote = await response.Content.ReadFromJsonAsync<TradeStationQuoteResponse>(cancellationToken);
            var price = quote?.Quotes?[0]?.Last ?? 0;

            _logger.LogDebug("TradeStation price for {Symbol}: ${Price}", symbol, price);

            return price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TradeStation price for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Sets leverage (not applicable for equities)
    /// </summary>
    public override Task<bool> SetLeverageAsync(
        string symbol,
        decimal leverage,
        MarginType marginType = MarginType.Cross,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Leverage not applicable for TradeStation equities trading");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets leverage info (not applicable for equities)
    /// </summary>
    public override Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LeverageInfo
        {
            CurrentLeverage = 1,
            MaxLeverage = 1,
            MarginType = MarginType.Cross,
            CollateralAmount = 0,
            BorrowedAmount = 0,
            InterestRate = 0,
            MarginHealthRatio = 1.0m
        });
    }

    /// <summary>
    /// Gets margin health ratio
    /// </summary>
    public override async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await EnsureValidTokenAsync(cancellationToken);

        try
        {
            var response = await _httpClient.GetAsync(
                $"brokerage/accounts/{_options.AccountId}/balances",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var balances = await response.Content.ReadFromJsonAsync<TradeStationBalances>(cancellationToken);

            var equity = balances?.Equity ?? 0;
            var cashBalance = balances?.CashBalance ?? 0;

            if (equity == 0) return 1.0m;

            var healthRatio = cashBalance / equity;
            return healthRatio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TradeStation margin health");
            throw;
        }
    }

    #region Helper Methods

    private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken == null || DateTime.UtcNow >= _tokenExpiry)
        {
            await AuthenticateAsync(cancellationToken);
        }
    }

    private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var authClient = new HttpClient();
            var authUrl = "https://signin.tradestation.com/oauth/token";

            var authData = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _options.ApiKey },
                { "client_secret", _options.ApiSecret },
                { "scope", "MarketData ReadAccount Trade" }
            };

            var content = new FormUrlEncodedContent(authData);
            var response = await authClient.PostAsync(authUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("TradeStation authentication failed: {Error}", error);
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<TradeStationAuthResponse>(cancellationToken);
            _accessToken = authResponse?.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse?.ExpiresIn ?? 3600);

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("TradeStation authentication successful");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TradeStation authentication");
            return false;
        }
    }

    private static string MapOrderType(OrderType type)
    {
        return type switch
        {
            OrderType.Market => "Market",
            OrderType.Limit => "Limit",
            _ => "Market"
        };
    }

    private static OrderStatus MapOrderStatus(string? status)
    {
        return status?.ToUpper() switch
        {
            "ACK" or "QUEUED" => OrderStatus.Pending,
            "FLL" or "FILLED" => OrderStatus.Filled,
            "FLP" or "PARTIALFILLED" => OrderStatus.PartiallyFilled,
            "CAN" or "CANCELLED" => OrderStatus.Cancelled,
            "REJ" or "REJECTED" => OrderStatus.Rejected,
            _ => OrderStatus.Pending
        };
    }

    #endregion

    #region DTOs

    private class TradeStationAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class TradeStationBalances
    {
        [JsonPropertyName("CashBalance")]
        public decimal CashBalance { get; set; }

        [JsonPropertyName("Equity")]
        public decimal Equity { get; set; }
    }

    private class TradeStationPositionsResponse
    {
        [JsonPropertyName("Positions")]
        public List<TradeStationPosition>? Positions { get; set; }
    }

    private class TradeStationPosition
    {
        [JsonPropertyName("Symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("Quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("AveragePrice")]
        public decimal AveragePrice { get; set; }

        [JsonPropertyName("Last")]
        public decimal Last { get; set; }

        [JsonPropertyName("UnrealizedProfitLoss")]
        public decimal UnrealizedProfitLoss { get; set; }

        [JsonPropertyName("UnrealizedProfitLossPercent")]
        public decimal UnrealizedProfitLossPercent { get; set; }

        [JsonPropertyName("TimeStamp")]
        public DateTime TimeStamp { get; set; }
    }

    private class TradeStationOrderResponse
    {
        [JsonPropertyName("Orders")]
        public List<TradeStationOrderInfo>? Orders { get; set; }
    }

    private class TradeStationOrderInfo
    {
        [JsonPropertyName("OrderID")]
        public string OrderID { get; set; } = string.Empty;
    }

    private class TradeStationOrderDetail
    {
        [JsonPropertyName("OrderID")]
        public string? OrderID { get; set; }

        [JsonPropertyName("Symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("TradeAction")]
        public string? TradeAction { get; set; }

        [JsonPropertyName("OrderType")]
        public string? OrderType { get; set; }

        [JsonPropertyName("Quantity")]
        public string? Quantity { get; set; }

        [JsonPropertyName("FilledQuantity")]
        public string? FilledQuantity { get; set; }

        [JsonPropertyName("LimitPrice")]
        public string? LimitPrice { get; set; }

        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        [JsonPropertyName("TimeStamp")]
        public DateTime TimeStamp { get; set; }
    }

    private class TradeStationQuoteResponse
    {
        [JsonPropertyName("Quotes")]
        public List<TradeStationQuote>? Quotes { get; set; }
    }

    private class TradeStationQuote
    {
        [JsonPropertyName("Last")]
        public decimal Last { get; set; }
    }

    #endregion
}

/// <summary>
/// TradeStation configuration options
/// </summary>
public class TradeStationOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public bool UsePaperTrading { get; set; } = true;
}
