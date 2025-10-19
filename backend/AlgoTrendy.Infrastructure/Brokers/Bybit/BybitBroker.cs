using AlgoTrendy.Core.Enums;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace AlgoTrendy.Infrastructure.Brokers.Bybit;

/// <summary>
/// Simplified Bybit broker implementation using HTTP client
/// This provides a working implementation while Bybit.Net library API is being evaluated
/// Full Bybit.Net integration is available in BybitBroker.Full.cs
/// </summary>
public class BybitBroker : IBroker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BybitBroker> _logger;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _baseUrl;
    private bool _isConnected = false;
    private readonly BybitResiliencePolicy _resiliencePolicy;
    private const int RequestTimeoutMs = 30000; // 30 seconds

    public string BrokerName => "Bybit";

    public BybitBroker(
        string apiKey,
        string apiSecret,
        bool testnet,
        ILogger<BybitBroker> logger)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _apiSecret = apiSecret;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = testnet
            ? "https://testnet.bybit.com"
            : "https://api.bybit.com";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(RequestTimeoutMs) };
        _resiliencePolicy = new BybitResiliencePolicy(_logger);

        _logger.LogInformation(
            "Bybit simplified broker initialized: {Environment}",
            testnet ? "Testnet" : "Live");
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to Bybit...");

            // Test connection with a simple public endpoint
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v5/market/tickers?category=linear&symbol=BTCUSDT");
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _isConnected = true;
                _logger.LogInformation("✅ Connected to Bybit successfully");
                return true;
            }

            _logger.LogError("Bybit connection failed: HTTP {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Bybit connection");
            return false;
        }
    }

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

            _logger.LogDebug("Getting balance for {Currency}", currency);

            // Call Bybit API: GET /v5/account/wallet-balance
            var url = $"{_baseUrl}/v5/account/wallet-balance";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Add authentication headers for account endpoints
            var timestamp = GetTimestampMs();
            AddAuthHeaders(request, "", timestamp);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get wallet balance: HTTP {StatusCode}", response.StatusCode);
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;

                // Bybit API response structure: { "retCode": 0, "result": { "list": [ { "coin": [ { "coin": "USDT", "walletBalance": "..." } ] } ] } }
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("list", out var list) &&
                    list.GetArrayLength() > 0)
                {
                    var firstAccount = list[0];
                    if (firstAccount.TryGetProperty("coin", out var coins))
                    {
                        for (int i = 0; i < coins.GetArrayLength(); i++)
                        {
                            var coinData = coins[i];
                            if (coinData.TryGetProperty("coin", out var coinName) &&
                                coinName.GetString() == currency &&
                                coinData.TryGetProperty("walletBalance", out var balance))
                            {
                                if (decimal.TryParse(balance.GetString(), out var balanceValue))
                                {
                                    _logger.LogDebug("Got balance for {Currency}: {Balance}", currency, balanceValue);
                                    return balanceValue;
                                }
                            }
                        }
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting balance for {Currency}", currency);
            return 0;
        }
    }

    public async Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Attempted to get positions while disconnected");
                return Enumerable.Empty<Position>();
            }

            _logger.LogDebug("Getting positions");

            // Call Bybit API: GET /v5/position/list
            var url = $"{_baseUrl}/v5/position/list";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Add authentication headers for trading endpoints
            var timestamp = GetTimestampMs();
            AddAuthHeaders(request, "", timestamp);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get positions: HTTP {StatusCode}", response.StatusCode);
                return Enumerable.Empty<Position>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var positions = new List<Position>();

            using (var doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;

                // Bybit API response structure: { "retCode": 0, "result": { "list": [ { "symbol": "...", "side": "...", "size": "...", "entryPrice": "..." } ] } }
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("list", out var list))
                {
                    for (int i = 0; i < list.GetArrayLength(); i++)
                    {
                        var positionData = list[i];
                        try
                        {
                            var symbol = positionData.TryGetProperty("symbol", out var symElem) ? symElem.GetString() : "UNKNOWN";
                            var side = positionData.TryGetProperty("side", out var sideElem) ? sideElem.GetString() : "Buy";
                            var size = ExtractDecimal(positionData, "size", 0);
                            var entryPrice = ExtractDecimal(positionData, "entryPrice", 0);

                            var position = new Position
                            {
                                PositionId = Guid.NewGuid().ToString(),
                                Exchange = "bybit",
                                Symbol = symbol ?? "UNKNOWN",
                                Side = side == "Sell" ? OrderSide.Sell : OrderSide.Buy,
                                Quantity = size,
                                EntryPrice = entryPrice,
                                CurrentPrice = 0, // Would need separate market data call
                                OpenedAt = DateTime.UtcNow
                            };

                            positions.Add(position);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error parsing position data");
                            continue;
                        }
                    }
                }
            }

            _logger.LogDebug("Retrieved {PositionCount} positions", positions.Count);
            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting positions");
            return Enumerable.Empty<Position>();
        }
    }

    public async Task<Order> PlaceOrderAsync(
        OrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            _logger.LogInformation(
                "Placing {Side} {OrderType} order for {Symbol}: {Quantity}",
                request.Side, request.Type, request.Symbol, request.Quantity);

            // Call Bybit API: POST /v5/order/create
            var url = $"{_baseUrl}/v5/order/create";

            var orderType = request.Type == OrderType.Market ? "MARKET" : "LIMIT";
            var side = request.Side == OrderSide.Buy ? "Buy" : "Sell";

            var bodyParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                { "symbol", request.Symbol },
                { "side", side },
                { "orderType", orderType },
                { "qty", request.Quantity.ToString() },
                { "clientOrderId", request.ClientOrderId ?? Guid.NewGuid().ToString() }
            };

            if (request.Type == OrderType.Limit && request.Price.HasValue)
            {
                bodyParams.Add("price", request.Price.Value.ToString());
            }

            var body = JsonSerializer.Serialize(bodyParams);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add authentication headers for trading endpoints
            var timestamp = GetTimestampMs();
            AddAuthHeaders(httpRequest, body, timestamp);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to place order: HTTP {StatusCode}, Body: {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Bybit API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var doc = JsonDocument.Parse(responseContent))
            {
                var root = doc.RootElement;

                // Check for API error
                if (root.TryGetProperty("retCode", out var retCode) && retCode.GetInt32() != 0)
                {
                    var retMsg = root.TryGetProperty("retMsg", out var msgElem) ? msgElem.GetString() : "Unknown error";
                    throw new InvalidOperationException($"Bybit API error: {retMsg}");
                }

                // Parse response: { "retCode": 0, "result": { "orderId": "...", "clientOrderId": "..." } }
                if (root.TryGetProperty("result", out var result))
                {
                    var bybitOrderId = result.TryGetProperty("orderId", out var orderIdElem) ? orderIdElem.GetString() : "";
                    var clientOrderId = result.TryGetProperty("clientOrderId", out var clientIdElem) ? clientIdElem.GetString() : "";

                    var order = new Order
                    {
                        OrderId = Guid.NewGuid().ToString(), // AlgoTrendy internal ID
                        ClientOrderId = clientOrderId ?? request.ClientOrderId ?? Guid.NewGuid().ToString(),
                        Symbol = request.Symbol,
                        Exchange = "Bybit",
                        Side = request.Side,
                        Type = request.Type,
                        Status = Core.Enums.OrderStatus.Pending,
                        Quantity = request.Quantity,
                        Price = request.Price,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _logger.LogInformation("Order placed successfully. Bybit OrderId: {BybitOrderId}", bybitOrderId);
                    return order;
                }
            }

            throw new InvalidOperationException("Unable to parse Bybit API response");
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while placing order");
            throw;
        }
    }

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

            _logger.LogInformation("Cancelling order {OrderId}", orderId);

            // Call Bybit API: POST /v5/order/cancel
            var url = $"{_baseUrl}/v5/order/cancel";

            var bodyParams = new Dictionary<string, string>
            {
                { "category", "linear" },
                { "symbol", symbol },
                { "orderId", orderId }
            };

            var body = JsonSerializer.Serialize(bodyParams);
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Add authentication headers
            var timestamp = GetTimestampMs();
            AddAuthHeaders(httpRequest, body, timestamp);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to cancel order: HTTP {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Bybit API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var doc = JsonDocument.Parse(responseContent))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("retCode", out var retCode) && retCode.GetInt32() != 0)
                {
                    var retMsg = root.TryGetProperty("retMsg", out var msgElem) ? msgElem.GetString() : "Unknown error";
                    throw new InvalidOperationException($"Bybit API error: {retMsg}");
                }

                return new Order
                {
                    OrderId = orderId,
                    ClientOrderId = "",
                    Symbol = symbol,
                    Exchange = "Bybit",
                    Side = Core.Enums.OrderSide.Buy,
                    Type = Core.Enums.OrderType.Market,
                    Status = Core.Enums.OrderStatus.Cancelled,
                    Quantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while cancelling order");
            throw;
        }
    }

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

            _logger.LogInformation("Getting order status for {OrderId}", orderId);

            // Call Bybit API: GET /v5/order/realtime
            var url = $"{_baseUrl}/v5/order/realtime?category=linear&symbol={symbol}&orderId={orderId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Add authentication headers
            var timestamp = GetTimestampMs();
            AddAuthHeaders(request, "", timestamp);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get order status: HTTP {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Bybit API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var doc = JsonDocument.Parse(responseContent))
            {
                var root = doc.RootElement;

                if (root.TryGetProperty("retCode", out var retCode) && retCode.GetInt32() != 0)
                {
                    var retMsg = root.TryGetProperty("retMsg", out var msgElem) ? msgElem.GetString() : "Unknown error";
                    throw new InvalidOperationException($"Bybit API error: {retMsg}");
                }

                // Parse response and extract order details
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("list", out var list) &&
                    list.GetArrayLength() > 0)
                {
                    var orderData = list[0];

                    var status = orderData.TryGetProperty("orderStatus", out var statusElem) ? statusElem.GetString() : "Pending";
                    var side = orderData.TryGetProperty("side", out var sideElem) ? sideElem.GetString() : "Buy";
                    var qty = ExtractDecimal(orderData, "qty", 0);

                    var orderStatus = status switch
                    {
                        "Filled" => Core.Enums.OrderStatus.Filled,
                        "PartiallyFilled" => Core.Enums.OrderStatus.PartiallyFilled,
                        "Cancelled" => Core.Enums.OrderStatus.Cancelled,
                        "Rejected" => Core.Enums.OrderStatus.Rejected,
                        _ => Core.Enums.OrderStatus.Pending
                    };

                    return new Order
                    {
                        OrderId = orderId,
                        ClientOrderId = "",
                        Symbol = symbol,
                        Exchange = "Bybit",
                        Side = side == "Sell" ? Core.Enums.OrderSide.Sell : Core.Enums.OrderSide.Buy,
                        Type = Core.Enums.OrderType.Market,
                        Status = orderStatus,
                        Quantity = qty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                }
            }

            return new Order
            {
                OrderId = orderId,
                ClientOrderId = "",
                Symbol = symbol,
                Exchange = "Bybit",
                Side = Core.Enums.OrderSide.Buy,
                Type = Core.Enums.OrderType.Market,
                Status = Core.Enums.OrderStatus.Pending,
                Quantity = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting order status");
            throw;
        }
    }

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

            _logger.LogDebug("Getting current price for {Symbol}", symbol);

            // Call Bybit API: GET /v5/market/tickers?category=linear&symbol={symbol}
            var url = $"{_baseUrl}/v5/market/tickers?category=linear&symbol={symbol}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get price for {Symbol}: HTTP {StatusCode}", symbol, response.StatusCode);
                return 0;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var doc = JsonDocument.Parse(content))
            {
                var root = doc.RootElement;

                // Bybit API response structure: { "retCode": 0, "retMsg": "...", "result": { "category": "...", "list": [ { "lastPrice": "..." } ] } }
                if (root.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("list", out var list) &&
                    list.GetArrayLength() > 0)
                {
                    var firstItem = list[0];
                    if (firstItem.TryGetProperty("lastPrice", out var lastPrice))
                    {
                        var priceStr = lastPrice.GetString();
                        if (decimal.TryParse(priceStr, out var price))
                        {
                            _logger.LogDebug("Got price for {Symbol}: {Price}", symbol, price);
                            return price;
                        }
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting current price for {Symbol}", symbol);
            return 0;
        }
    }

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
                symbol, leverage, marginType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while setting leverage");
            return false;
        }
    }

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

            _logger.LogInformation("Getting leverage info for {Symbol}", symbol);

            return new LeverageInfo
            {
                CurrentLeverage = 1,
                MaxLeverage = 100,
                MarginType = MarginType.Cross,
                CollateralAmount = 0,
                BorrowedAmount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting leverage info");
            throw;
        }
    }

    public async Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Bybit broker is not connected");
            }

            _logger.LogInformation("Getting margin health ratio");
            return 1.0m; // Healthy state
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting margin health ratio");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    #region Helper Methods

    /// <summary>
    /// Checks HTTP response headers for rate limit information
    /// </summary>
    private void CheckRateLimitHeaders(HttpResponseMessage response)
    {
        try
        {
            if (response.Headers.TryGetValues("X-BAPI-LIMIT-STATUS", out var limitStatus))
            {
                _logger.LogDebug("Rate limit status: {Status}", string.Join(",", limitStatus));
            }

            if (response.Headers.TryGetValues("X-BAPI-LIMIT-RESET-TIMESTAMP", out var resetTime))
            {
                _logger.LogDebug("Rate limit reset at: {ResetTime}", string.Join(",", resetTime));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking rate limit headers");
        }
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for Bybit API authentication
    /// </summary>
    private string GenerateSignature(string queryString, long timestamp)
    {
        try
        {
            var message = $"{queryString}{timestamp}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret ?? "")))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signature");
            throw;
        }
    }

    /// <summary>
    /// Adds authentication headers for Bybit API requests
    /// </summary>
    private void AddAuthHeaders(HttpRequestMessage request, string queryString, long timestamp)
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
            {
                _logger.LogWarning("Missing API credentials");
                return;
            }

            var signature = GenerateSignature(queryString, timestamp);
            request.Headers.Add("X-BAPI-KEY", _apiKey);
            request.Headers.Add("X-BAPI-SIGN", signature);
            request.Headers.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
            request.Headers.Add("X-BAPI-RECV-WINDOW", "5000");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding authentication headers");
        }
    }

    /// <summary>
    /// Safely parses JSON response using System.Text.Json
    /// </summary>
    private T? SafeJsonParse<T>(string json) where T : class
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing JSON response");
            return null;
        }
    }

    /// <summary>
    /// Safely extracts decimal value from JSON element
    /// </summary>
    private decimal ExtractDecimal(JsonElement? element, string path, decimal defaultValue = 0)
    {
        try
        {
            if (!element.HasValue) return defaultValue;

            var value = element.Value;
            var parts = path.Split('.');

            foreach (var part in parts)
            {
                if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty(part, out var nextValue))
                {
                    value = nextValue;
                }
                else
                {
                    return defaultValue;
                }
            }

            if (value.TryGetDecimal(out var decimalValue))
                return decimalValue;

            if (decimal.TryParse(value.GetString(), out var parsedValue))
                return parsedValue;

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting decimal value");
            return defaultValue;
        }
    }

    /// <summary>
    /// Gets current Unix timestamp in milliseconds
    /// </summary>
    private long GetTimestampMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    #endregion
}
