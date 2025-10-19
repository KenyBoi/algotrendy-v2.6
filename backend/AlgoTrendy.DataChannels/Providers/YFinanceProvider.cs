using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlgoTrendy.DataChannels.Providers;

/// <summary>
/// yfinance data provider - FREE tier (unlimited, unofficial Yahoo Finance API)
/// Excellent for historical data, options chains, and fundamentals
/// Calls Python microservice running on port 5001
/// </summary>
public class YFinanceProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YFinanceProvider> _logger;
    private readonly string _serviceUrl;

    public string ProviderName => "yfinance";
    public bool IsFreeTier => true;
    public int? DailyRateLimit => null; // Unlimited (but has undocumented rate limiting)

    public YFinanceProvider(
        HttpClient httpClient,
        ILogger<YFinanceProvider> logger,
        string? serviceUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceUrl = serviceUrl ?? "http://localhost:5001";
    }

    public async Task<IEnumerable<MarketData>> FetchHistoricalAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        string interval = "1d",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate.ToString("yyyy-MM-dd");
            var end = endDate.ToString("yyyy-MM-dd");

            var url = $"{_serviceUrl}/historical?symbol={symbol}&start={start}&end={end}&interval={interval}";

            _logger.LogInformation("[yfinance] Fetching {Symbol} from {Start} to {End} (interval: {Interval})",
                symbol, start, end, interval);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<YFinanceResponse>(json);

            if (result == null)
            {
                _logger.LogWarning("[yfinance] Null response for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                _logger.LogError("[yfinance] Error from service: {Error}", result.Error);
                throw new InvalidOperationException($"yfinance error: {result.Error}");
            }

            if (result.Data == null || !result.Data.Any())
            {
                _logger.LogWarning("[yfinance] No data returned for {Symbol}", symbol);
                return Array.Empty<MarketData>();
            }

            _logger.LogInformation("[yfinance] Fetched {Count} bars for {Symbol}", result.Count, symbol);

            return result.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[yfinance] HTTP error fetching {Symbol}. Is yfinance service running on {Url}?",
                symbol, _serviceUrl);
            throw new InvalidOperationException(
                $"Cannot connect to yfinance service at {_serviceUrl}. " +
                "Start the service with: python yfinance_service.py", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[yfinance] Error fetching data for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<MarketData?> FetchLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_serviceUrl}/latest?symbol={symbol}";

            _logger.LogInformation("[yfinance] Fetching latest quote for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<MarketData>(json);

            if (result == null)
            {
                _logger.LogWarning("[yfinance] Null response for latest quote {Symbol}", symbol);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[yfinance] Error fetching latest quote for {Symbol}", symbol);
            throw;
        }
    }

    public Task<bool> SupportsSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        // yfinance supports all Yahoo Finance symbols (very broad coverage)
        return Task.FromResult(true);
    }

    public Task<int> GetCurrentUsageAsync()
    {
        // No usage tracking for yfinance (unlimited but unofficial)
        return Task.FromResult(0);
    }

    public Task<int?> GetRemainingCallsAsync()
    {
        // Unlimited (null)
        return Task.FromResult<int?>(null);
    }

    /// <summary>
    /// Get available option expiration dates
    /// </summary>
    public async Task<IEnumerable<string>> GetOptionsExpirationsAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_serviceUrl}/options/expirations?symbol={symbol}";

            _logger.LogInformation("[yfinance] Fetching option expirations for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<OptionsExpirationsResponse>(json);

            if (result == null || result.Expirations == null)
            {
                _logger.LogWarning("[yfinance] No expiration dates found for {Symbol}", symbol);
                return Array.Empty<string>();
            }

            _logger.LogInformation("[yfinance] Found {Count} expiration dates for {Symbol}",
                result.Count, symbol);

            return result.Expirations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[yfinance] Error fetching expirations for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Get options chain for a specific expiration
    /// </summary>
    public async Task<OptionsChain?> GetOptionsChainAsync(
        string symbol,
        string expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_serviceUrl}/options?symbol={symbol}&expiration={expiration}";

            _logger.LogInformation("[yfinance] Fetching options chain for {Symbol} expiring {Expiration}",
                symbol, expiration);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<OptionsChain>(json);

            if (result == null)
            {
                _logger.LogWarning("[yfinance] Null options chain for {Symbol}", symbol);
                return null;
            }

            _logger.LogInformation("[yfinance] Fetched {CallsCount} calls and {PutsCount} puts for {Symbol}",
                result.CallsCount, result.PutsCount, symbol);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[yfinance] Error fetching options chain for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Get company fundamentals
    /// </summary>
    public async Task<CompanyInfo?> GetCompanyInfoAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_serviceUrl}/info?symbol={symbol}";

            _logger.LogInformation("[yfinance] Fetching company info for {Symbol}", symbol);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<CompanyInfo>(json);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[yfinance] Error fetching company info for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Check if yfinance service is running
    /// </summary>
    public async Task<bool> IsServiceHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrl}/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// Response models
internal class YFinanceResponse
{
    [JsonProperty("data")]
    public List<MarketData>? Data { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
}

internal class OptionsExpirationsResponse
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonProperty("expirations")]
    public List<string>? Expirations { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; }

    [JsonProperty("error")]
    public string? Error { get; set; }
}

public class OptionsChain
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonProperty("expiration")]
    public string Expiration { get; set; } = string.Empty;

    [JsonProperty("calls")]
    public List<OptionContract>? Calls { get; set; }

    [JsonProperty("puts")]
    public List<OptionContract>? Puts { get; set; }

    [JsonProperty("calls_count")]
    public int CallsCount { get; set; }

    [JsonProperty("puts_count")]
    public int PutsCount { get; set; }
}

public class OptionContract
{
    [JsonProperty("contractSymbol")]
    public string? ContractSymbol { get; set; }

    [JsonProperty("strike")]
    public decimal Strike { get; set; }

    [JsonProperty("lastPrice")]
    public decimal? LastPrice { get; set; }

    [JsonProperty("bid")]
    public decimal? Bid { get; set; }

    [JsonProperty("ask")]
    public decimal? Ask { get; set; }

    [JsonProperty("volume")]
    public long? Volume { get; set; }

    [JsonProperty("openInterest")]
    public long? OpenInterest { get; set; }

    [JsonProperty("impliedVolatility")]
    public decimal? ImpliedVolatility { get; set; }

    [JsonProperty("inTheMoney")]
    public bool InTheMoney { get; set; }
}

public class CompanyInfo
{
    [JsonProperty("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonProperty("company_name")]
    public string? CompanyName { get; set; }

    [JsonProperty("sector")]
    public string? Sector { get; set; }

    [JsonProperty("industry")]
    public string? Industry { get; set; }

    [JsonProperty("market_cap")]
    public long? MarketCap { get; set; }

    [JsonProperty("pe_ratio")]
    public decimal? PeRatio { get; set; }

    [JsonProperty("forward_pe")]
    public decimal? ForwardPe { get; set; }

    [JsonProperty("peg_ratio")]
    public decimal? PegRatio { get; set; }

    [JsonProperty("dividend_yield")]
    public decimal? DividendYield { get; set; }

    [JsonProperty("52w_high")]
    public decimal? FiftyTwoWeekHigh { get; set; }

    [JsonProperty("52w_low")]
    public decimal? FiftyTwoWeekLow { get; set; }

    [JsonProperty("beta")]
    public decimal? Beta { get; set; }

    [JsonProperty("price")]
    public decimal? Price { get; set; }

    [JsonProperty("previous_close")]
    public decimal? PreviousClose { get; set; }

    [JsonProperty("volume")]
    public long? Volume { get; set; }

    [JsonProperty("average_volume")]
    public long? AverageVolume { get; set; }

    [JsonProperty("employees")]
    public int? Employees { get; set; }

    [JsonProperty("website")]
    public string? Website { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }
}
