using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlgoTrendy.TradingEngine.Brokers;

/// <summary>
/// Freqtrade bot integration broker
/// Connects to Freqtrade REST API for automated trading
/// Supports multiple bot instances (Conservative RSI, MACD Hunter, Aggressive RSI)
/// </summary>
public class FreqtradeBroker : BrokerBase
{
    private readonly HttpClient _httpClient;
    private readonly FreqtradeOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public override string BrokerName => "freqtrade";
    protected override int MinRequestIntervalMs => 200; // 5 requests per second

    public FreqtradeBroker(
        ILogger<FreqtradeBroker> logger,
        IOptions<FreqtradeOptions> options,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClientFactory.CreateClient("Freqtrade");

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri($"http://127.0.0.1:{_options.Port}");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Add basic authentication
        var authBytes = Encoding.ASCII.GetBytes($"{_options.Username}:{_options.Password}");
        var authHeader = Convert.ToBase64String(authBytes);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        _logger.LogInformation($"Initialized FreqtradeBroker for '{_options.BotName}' on port {_options.Port}");
    }

    public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Connecting to Freqtrade bot '{_options.BotName}' at http://127.0.0.1:{_options.Port}");

            var response = await _httpClient.GetAsync("/api/v1/ping", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var pingResponse = JsonSerializer.Deserialize<FreqtradePingResponse>(content, _jsonOptions);

            _isConnected = pingResponse?.Status == "pong";

            if (_isConnected)
            {
                _logger.LogInformation($"Successfully connected to Freqtrade bot '{_options.BotName}'");
            }
            else
            {
                _logger.LogWarning($"Failed to connect to Freqtrade bot '{_options.BotName}' - Invalid response");
            }

            return _isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error connecting to Freqtrade bot '{_options.BotName}' on port {_options.Port}");
            _isConnected = false;
            return false;
        }
    }

    public override async Task<decimal> GetBalanceAsync(string currency = "USDT", CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            var response = await _httpClient.GetAsync("/api/v1/balance", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var balanceResponse = JsonSerializer.Deserialize<FreqtradeBalanceResponse>(content, _jsonOptions);

            return balanceResponse?.Total ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting balance from Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            var response = await _httpClient.GetAsync("/api/v1/status", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var positions = JsonSerializer.Deserialize<List<FreqtradePosition>>(content, _jsonOptions);

            if (positions == null || !positions.Any())
            {
                return Enumerable.Empty<Position>();
            }

            return positions.Select(p => new Position
            {
                Id = Guid.NewGuid(),
                Symbol = p.Pair ?? "UNKNOWN",
                Side = p.IsShort ? OrderSide.Sell : OrderSide.Buy,
                Quantity = p.Amount,
                EntryPrice = p.OpenRate,
                CurrentPrice = p.CurrentRate,
                UnrealizedPnL = p.ProfitAbs,
                Leverage = p.Leverage ?? 1m,
                Broker = BrokerName,
                OpenTime = DateTime.TryParse(p.OpenDate, out var openTime) ? openTime : DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                StopLoss = p.StopLoss,
                TakeProfit = p.StakingProfit
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting positions from Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override async Task<Order> PlaceOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            // Freqtrade handles orders automatically through strategies
            // This endpoint allows manual order forcing
            var orderPayload = new
            {
                pair = request.Symbol,
                side = request.Side == OrderSide.Buy ? "buy" : "sell",
                ordertype = request.OrderType == OrderType.Market ? "market" : "limit",
                price = request.Price,
                amount = request.Quantity
            };

            var json = JsonSerializer.Serialize(orderPayload, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/forcebuy", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var orderResponse = JsonSerializer.Deserialize<FreqtradeOrderResponse>(content, _jsonOptions);

            return new Order
            {
                Id = Guid.NewGuid(),
                OrderId = orderResponse?.TradeId.ToString() ?? Guid.NewGuid().ToString(),
                Symbol = request.Symbol,
                Side = request.Side,
                Type = request.OrderType,
                Quantity = request.Quantity,
                Price = request.Price ?? 0m,
                Status = OrderStatus.Filled,
                Broker = BrokerName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error placing order on Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override async Task<Order> CancelOrderAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            // Force exit a trade
            var payload = new { tradeid = orderId };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/forceexit", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            return new Order
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Symbol = symbol,
                Status = OrderStatus.Cancelled,
                Broker = BrokerName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error canceling order {orderId} on Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override async Task<Order> GetOrderStatusAsync(string orderId, string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            // Get trade status from trades endpoint
            var response = await _httpClient.GetAsync("/api/v1/trades", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tradesResponse = JsonSerializer.Deserialize<FreqtradeTradesResponse>(content, _jsonOptions);

            var trade = tradesResponse?.Trades?.FirstOrDefault(t => t.TradeId.ToString() == orderId);

            if (trade == null)
            {
                throw new InvalidOperationException($"Trade {orderId} not found");
            }

            return new Order
            {
                Id = Guid.NewGuid(),
                OrderId = trade.TradeId.ToString(),
                Symbol = trade.Pair ?? symbol,
                Side = trade.IsShort ? OrderSide.Sell : OrderSide.Buy,
                Type = OrderType.Market,
                Quantity = trade.Amount,
                Price = trade.OpenRate,
                FilledQuantity = trade.Amount,
                Status = trade.IsOpen ? OrderStatus.Filled : OrderStatus.Closed,
                Broker = BrokerName,
                CreatedAt = DateTime.TryParse(trade.OpenDate, out var openDate) ? openDate : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting order status for {orderId} on Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override async Task<decimal> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await EnsureRateLimitAsync();

        try
        {
            // Freqtrade doesn't have a direct price endpoint, get from current positions
            var positions = await GetPositionsAsync(cancellationToken);
            var position = positions.FirstOrDefault(p => p.Symbol == symbol);

            if (position != null)
            {
                return position.CurrentPrice;
            }

            // If no position, try to get from whitelist
            var response = await _httpClient.GetAsync("/api/v1/whitelist", cancellationToken);
            response.EnsureSuccessStatusCode();

            // Return 0 if symbol not found - caller should handle
            _logger.LogWarning($"No current price available for {symbol} on Freqtrade bot '{_options.BotName}'");
            return 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting current price for {symbol} on Freqtrade bot '{_options.BotName}'");
            throw;
        }
    }

    public override Task<bool> SetLeverageAsync(string symbol, decimal leverage, MarginType marginType = MarginType.Cross, CancellationToken cancellationToken = default)
    {
        // Freqtrade leverage is configured in the strategy, not via API
        _logger.LogWarning($"SetLeverageAsync not supported by Freqtrade - leverage is configured in strategy file");
        return Task.FromResult(false);
    }

    public override Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // Freqtrade doesn't expose leverage info via API
        // Return default leverage info
        return Task.FromResult(new LeverageInfo
        {
            Symbol = symbol,
            Leverage = 1m,
            MaxLeverage = 1m,
            MarginType = MarginType.Cross
        });
    }

    public override Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        // Freqtrade doesn't use margin health ratio concept
        // Return 100% indicating healthy account
        return Task.FromResult(1.0m);
    }

    #region Helper Classes for Freqtrade API Responses

    private class FreqtradePingResponse
    {
        public string? Status { get; set; }
    }

    private class FreqtradeBalanceResponse
    {
        public decimal Total { get; set; }
        public decimal Free { get; set; }
        public decimal Used { get; set; }
    }

    private class FreqtradePosition
    {
        public int TradeId { get; set; }
        public string? Pair { get; set; }
        public bool IsShort { get; set; }
        public decimal Amount { get; set; }
        public decimal OpenRate { get; set; }
        public decimal CurrentRate { get; set; }
        public decimal ProfitAbs { get; set; }
        public decimal ProfitRatio { get; set; }
        public string? OpenDate { get; set; }
        public decimal? Leverage { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? StakingProfit { get; set; }
    }

    private class FreqtradeOrderResponse
    {
        public int TradeId { get; set; }
        public string? Status { get; set; }
    }

    private class FreqtradeTradesResponse
    {
        public List<FreqtradeTrade>? Trades { get; set; }
    }

    private class FreqtradeTrade
    {
        public int TradeId { get; set; }
        public string? Pair { get; set; }
        public bool IsShort { get; set; }
        public bool IsOpen { get; set; }
        public decimal Amount { get; set; }
        public decimal OpenRate { get; set; }
        public string? OpenDate { get; set; }
    }

    #endregion
}

/// <summary>
/// Configuration options for Freqtrade broker
/// </summary>
public class FreqtradeOptions
{
    /// <summary>
    /// Bot name (e.g., "Conservative RSI", "MACD Hunter", "Aggressive RSI")
    /// </summary>
    public string BotName { get; set; } = "Freqtrade Bot";

    /// <summary>
    /// Port number where Freqtrade API is running
    /// Default: 8082 (Conservative RSI), 8083 (MACD Hunter), 8084 (Aggressive RSI)
    /// </summary>
    public int Port { get; set; } = 8082;

    /// <summary>
    /// API username for authentication
    /// </summary>
    public string Username { get; set; } = "memgpt";

    /// <summary>
    /// API password for authentication
    /// </summary>
    public string Password { get; set; } = "trading123";

    /// <summary>
    /// Strategy name (e.g., "RSI_Conservative", "MACD_Aggressive")
    /// </summary>
    public string Strategy { get; set; } = "RSI_Conservative";
}
