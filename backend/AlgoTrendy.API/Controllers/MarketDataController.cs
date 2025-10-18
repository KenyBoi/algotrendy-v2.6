using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API controller for market data operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(
        IMarketDataRepository marketDataRepository,
        ILogger<MarketDataController> logger)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
}
