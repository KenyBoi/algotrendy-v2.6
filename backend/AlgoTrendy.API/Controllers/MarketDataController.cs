using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using AlgoTrendy.Core.Enums;
using AlgoTrendy.DataChannels.Channels.REST;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API controller for multi-asset market data operations
/// Supports crypto, stocks, futures, options, ETFs, and forex
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<MarketDataController> _logger;
    private readonly StockDataChannel? _stockDataChannel;
    private readonly FuturesDataChannel? _futuresDataChannel;

    public MarketDataController(
        IMarketDataRepository marketDataRepository,
        ILogger<MarketDataController> logger,
        IServiceProvider serviceProvider)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Optional dependencies (may not be registered in all configurations)
        _stockDataChannel = serviceProvider.GetService<StockDataChannel>();
        _futuresDataChannel = serviceProvider.GetService<FuturesDataChannel>();
    }

    /// <summary>
    /// Gets market data for a specific symbol within a time range
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
    /// <param name="startTime">Start time (UTC)</param>
    /// <param name="endTime">End time (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of market data candles</returns>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(IEnumerable<MarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MarketData>>> GetBySymbol(
        string symbol,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required");

            if (startTime >= endTime)
                return BadRequest("Start time must be before end time");

            _logger.LogInformation(
                "Fetching market data for {Symbol} from {StartTime} to {EndTime}",
                symbol, startTime, endTime);

            var marketData = await _marketDataRepository.GetBySymbolAsync(
                symbol, startTime, endTime, cancellationToken);

            return Ok(marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market data for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching market data");
        }
    }

    /// <summary>
    /// Gets the latest market data for a specific symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest market data candle</returns>
    [HttpGet("{symbol}/latest")]
    [ProducesResponseType(typeof(MarketData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarketData>> GetLatest(
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required");

            _logger.LogInformation("Fetching latest market data for {Symbol}", symbol);

            var marketData = await _marketDataRepository.GetLatestAsync(symbol, cancellationToken);

            if (marketData == null)
                return NotFound($"No market data found for symbol {symbol}");

            return Ok(marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest market data for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching market data");
        }
    }

    /// <summary>
    /// Gets the latest market data for multiple symbols
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of symbol to market data</returns>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, MarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyDictionary<string, MarketData>>> GetLatestBatch(
        [FromQuery] string symbols,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbols))
                return BadRequest("Symbols parameter is required");

            var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            if (!symbolList.Any())
                return BadRequest("At least one symbol is required");

            _logger.LogInformation("Fetching latest market data for {Count} symbols", symbolList.Count);

            var marketData = await _marketDataRepository.GetLatestBatchAsync(symbolList, cancellationToken);

            return Ok(marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching latest market data batch");
            return StatusCode(500, "An error occurred while fetching market data");
        }
    }

    /// <summary>
    /// Gets aggregated market data (hourly, daily, weekly) for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BTCUSDT)</param>
    /// <param name="interval">Aggregation interval (1h, 1d, 1w)</param>
    /// <param name="startTime">Start time (UTC)</param>
    /// <param name="endTime">End time (UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of aggregated market data candles</returns>
    [HttpGet("{symbol}/aggregated")]
    [ProducesResponseType(typeof(IEnumerable<MarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MarketData>>> GetAggregated(
        string symbol,
        [FromQuery] string interval,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol is required");

            if (string.IsNullOrWhiteSpace(interval))
                return BadRequest("Interval is required");

            if (!new[] { "1h", "1d", "1w" }.Contains(interval))
                return BadRequest("Interval must be one of: 1h, 1d, 1w");

            if (startTime >= endTime)
                return BadRequest("Start time must be before end time");

            _logger.LogInformation(
                "Fetching aggregated market data for {Symbol} with interval {Interval}",
                symbol, interval);

            var marketData = await _marketDataRepository.GetAggregatedAsync(
                symbol, interval, startTime, endTime, cancellationToken);

            return Ok(marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching aggregated market data for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching market data");
        }
    }

    /// <summary>
    /// Inserts new market data (admin/data channel use only)
    /// </summary>
    /// <param name="marketData">Market data to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Insert(
        [FromBody] MarketData marketData,
        CancellationToken cancellationToken)
    {
        try
        {
            if (marketData == null)
                return BadRequest("Market data is required");

            _logger.LogInformation(
                "Inserting market data for {Symbol} at {Timestamp}",
                marketData.Symbol, marketData.Timestamp);

            var success = await _marketDataRepository.InsertAsync(marketData, cancellationToken);

            if (!success)
                return StatusCode(500, "Failed to insert market data");

            return Created($"/api/v1/marketdata/{marketData.Symbol}/latest", marketData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting market data");
            return StatusCode(500, "An error occurred while inserting market data");
        }
    }

    /// <summary>
    /// Inserts multiple market data records in batch (admin/data channel use only)
    /// </summary>
    /// <param name="marketDataList">List of market data to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records inserted</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> InsertBatch(
        [FromBody] IEnumerable<MarketData> marketDataList,
        CancellationToken cancellationToken)
    {
        try
        {
            var dataList = marketDataList?.ToList();
            if (dataList == null || !dataList.Any())
                return BadRequest("Market data list is required and cannot be empty");

            _logger.LogInformation("Inserting batch of {Count} market data records", dataList.Count);

            var insertedCount = await _marketDataRepository.InsertBatchAsync(dataList, cancellationToken);

            _logger.LogInformation("Successfully inserted {Count} market data records", insertedCount);

            return Created("/api/v1/marketdata/batch", insertedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting market data batch");
            return StatusCode(500, "An error occurred while inserting market data");
        }
    }

    // ========== STOCK-SPECIFIC ENDPOINTS ==========

    /// <summary>
    /// Gets available option expiration dates for a stock symbol
    /// </summary>
    /// <param name="symbol">Stock symbol (e.g., AAPL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expiration dates</returns>
    [HttpGet("stocks/{symbol}/options/expirations")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IEnumerable<string>>> GetOptionsExpirations(
        string symbol,
        CancellationToken cancellationToken)
    {
        if (_stockDataChannel == null)
            return StatusCode(503, "Stock data channel is not available");

        try
        {
            _logger.LogInformation("Fetching option expirations for {Symbol}", symbol);

            var expirations = await _stockDataChannel.GetOptionsExpirationsAsync(symbol, cancellationToken);

            if (expirations == null || !expirations.Any())
                return NotFound($"No option expirations found for {symbol}");

            return Ok(expirations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching option expirations for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching option expirations");
        }
    }

    /// <summary>
    /// Gets options chain for a stock symbol and expiration date
    /// </summary>
    /// <param name="symbol">Stock symbol (e.g., AAPL)</param>
    /// <param name="expiration">Expiration date (YYYY-MM-DD format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Options chain with calls and puts</returns>
    [HttpGet("stocks/{symbol}/options/chain")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<object>> GetOptionsChain(
        string symbol,
        [FromQuery] string expiration,
        CancellationToken cancellationToken)
    {
        if (_stockDataChannel == null)
            return StatusCode(503, "Stock data channel is not available");

        if (string.IsNullOrWhiteSpace(expiration))
            return BadRequest("Expiration date is required");

        try
        {
            _logger.LogInformation(
                "Fetching options chain for {Symbol} expiring {Expiration}",
                symbol, expiration);

            var optionsChain = await _stockDataChannel.GetOptionsChainAsync(
                symbol, expiration, cancellationToken);

            if (optionsChain == null)
                return NotFound($"No options chain found for {symbol} expiring {expiration}");

            return Ok(optionsChain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching options chain for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching options chain");
        }
    }

    // ========== FUTURES-SPECIFIC ENDPOINTS ==========

    /// <summary>
    /// Gets open interest for a futures contract
    /// </summary>
    /// <param name="symbol">Futures symbol (e.g., BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Open interest value</returns>
    [HttpGet("futures/{symbol}/openinterest")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<decimal>> GetOpenInterest(
        string symbol,
        CancellationToken cancellationToken)
    {
        if (_futuresDataChannel == null)
            return StatusCode(503, "Futures data channel is not available");

        try
        {
            _logger.LogInformation("Fetching open interest for {Symbol}", symbol);

            var openInterest = await _futuresDataChannel.GetOpenInterestAsync(symbol, cancellationToken);

            if (openInterest == null)
                return NotFound($"No open interest data found for {symbol}");

            return Ok(openInterest.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching open interest for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching open interest");
        }
    }

    /// <summary>
    /// Gets funding rate for a perpetual futures contract
    /// </summary>
    /// <param name="symbol">Futures symbol (e.g., BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current funding rate</returns>
    [HttpGet("futures/{symbol}/fundingrate")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<decimal>> GetFundingRate(
        string symbol,
        CancellationToken cancellationToken)
    {
        if (_futuresDataChannel == null)
            return StatusCode(503, "Futures data channel is not available");

        try
        {
            _logger.LogInformation("Fetching funding rate for {Symbol}", symbol);

            var fundingRate = await _futuresDataChannel.GetFundingRateAsync(symbol, cancellationToken);

            if (fundingRate == null)
                return NotFound($"No funding rate data found for {symbol}");

            return Ok(fundingRate.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching funding rate for {Symbol}", symbol);
            return StatusCode(500, "An error occurred while fetching funding rate");
        }
    }

    // ========== MULTI-ASSET FILTERING ENDPOINTS ==========

    /// <summary>
    /// Gets latest market data filtered by asset type
    /// </summary>
    /// <param name="assetType">Asset type (Cryptocurrency, Stock, Futures, Options, ETF, Forex)</param>
    /// <param name="limit">Maximum number of records to return (default: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of market data for the specified asset type</returns>
    [HttpGet("by-asset-type/{assetType}")]
    [ProducesResponseType(typeof(IEnumerable<MarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MarketData>>> GetByAssetType(
        string assetType,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<AssetType>(assetType, true, out var parsedAssetType))
            {
                return BadRequest($"Invalid asset type. Valid values: {string.Join(", ", Enum.GetNames(typeof(AssetType)))}");
            }

            _logger.LogInformation("Fetching latest {Count} records for asset type {AssetType}", limit, assetType);

            // This would require a new repository method to filter by asset type
            // For now, return a meaningful message
            return StatusCode(501, "Asset type filtering requires database schema update. Coming in next phase.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching market data by asset type");
            return StatusCode(500, "An error occurred while fetching market data");
        }
    }

    /// <summary>
    /// Gets channel status for all data channels
    /// </summary>
    /// <returns>Status of all market data channels</returns>
    [HttpGet("channels/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult<object> GetChannelStatus()
    {
        var status = new
        {
            timestamp = DateTime.UtcNow,
            channels = new[]
            {
                new
                {
                    name = "stocks",
                    available = _stockDataChannel != null,
                    connected = _stockDataChannel?.IsConnected ?? false,
                    subscribedSymbols = _stockDataChannel?.SubscribedSymbols.Count ?? 0,
                    lastDataReceived = _stockDataChannel?.LastDataReceivedAt,
                    totalMessages = _stockDataChannel?.TotalMessagesReceived ?? 0
                },
                new
                {
                    name = "futures",
                    available = _futuresDataChannel != null,
                    connected = _futuresDataChannel?.IsConnected ?? false,
                    subscribedSymbols = _futuresDataChannel?.SubscribedSymbols.Count ?? 0,
                    lastDataReceived = _futuresDataChannel?.LastDataReceivedAt,
                    totalMessages = _futuresDataChannel?.TotalMessagesReceived ?? 0
                }
            }
        };

        return Ok(status);
    }
}
