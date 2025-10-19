using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;
using CryptoExchangeModel = AlgoTrendy.Core.Models.CryptoExchange;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// API controller for cryptocurrency market data from Finnhub
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CryptoDataController : ControllerBase
{
    private readonly IFinnhubService _finnhubService;
    private readonly ILogger<CryptoDataController> _logger;

    public CryptoDataController(
        IFinnhubService finnhubService,
        ILogger<CryptoDataController> logger)
    {
        _finnhubService = finnhubService ?? throw new ArgumentNullException(nameof(finnhubService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets cryptocurrency candlestick (OHLCV) data
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BINANCE:BTCUSDT)</param>
    /// <param name="resolution">Candle resolution: 1, 5, 15, 30, 60, D, W, M (default: 1)</param>
    /// <param name="from">Start time (Unix timestamp in seconds)</param>
    /// <param name="to">End time (Unix timestamp in seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of cryptocurrency candles</returns>
    /// <response code="200">Returns candlestick data</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("candles")]
    [ProducesResponseType(typeof(IEnumerable<CryptoCandle>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CryptoCandle>>> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] string resolution = "1",
        [FromQuery] long? from = null,
        [FromQuery] long? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol parameter is required (e.g., BINANCE:BTCUSDT)");

            // Default to last 24 hours if not specified
            var toTimestamp = to ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fromTimestamp = from ?? toTimestamp - (24 * 60 * 60); // 24 hours ago

            if (fromTimestamp >= toTimestamp)
                return BadRequest("From timestamp must be before to timestamp");

            _logger.LogInformation(
                "Fetching crypto candles for {Symbol} with resolution {Resolution}",
                symbol, resolution);

            var candles = await _finnhubService.GetCryptoCandlesAsync(
                symbol, resolution, fromTimestamp, toTimestamp, cancellationToken);

            return Ok(candles);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for candle request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error fetching crypto candles");
            return StatusCode(500, new { error = "Failed to fetch cryptocurrency data" });
        }
    }

    /// <summary>
    /// Gets the latest quote/price for a cryptocurrency symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., BINANCE:BTCUSDT)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest price</returns>
    /// <response code="200">Returns latest price</response>
    /// <response code="400">Invalid symbol</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("quote")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuoteResponse>> GetQuote(
        [FromQuery] string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return BadRequest("Symbol parameter is required (e.g., BINANCE:BTCUSDT)");

            _logger.LogInformation("Fetching quote for {Symbol}", symbol);

            var price = await _finnhubService.GetCryptoQuoteAsync(symbol, cancellationToken);

            return Ok(new QuoteResponse
            {
                Symbol = symbol,
                Price = price,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid symbol for quote request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error fetching quote");
            return StatusCode(500, new { error = "Failed to fetch cryptocurrency quote" });
        }
    }

    /// <summary>
    /// Gets list of supported cryptocurrency exchanges
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of exchanges</returns>
    /// <response code="200">Returns list of exchanges</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("exchanges")]
    [ProducesResponseType(typeof(IEnumerable<CryptoExchangeModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CryptoExchangeModel>>> GetExchanges(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching list of crypto exchanges");

            var exchanges = await _finnhubService.GetCryptoExchangesAsync(cancellationToken);

            return Ok(exchanges);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error fetching exchanges");
            return StatusCode(500, new { error = "Failed to fetch cryptocurrency exchanges" });
        }
    }

    /// <summary>
    /// Gets list of cryptocurrency symbols for a specific exchange
    /// </summary>
    /// <param name="exchange">Exchange code (e.g., binance, coinbase, kraken)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of symbols</returns>
    /// <response code="200">Returns list of symbols</response>
    /// <response code="400">Invalid exchange</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("symbols")]
    [ProducesResponseType(typeof(IEnumerable<CryptoSymbol>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CryptoSymbol>>> GetSymbols(
        [FromQuery] string exchange,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(exchange))
                return BadRequest("Exchange parameter is required (e.g., binance, coinbase)");

            _logger.LogInformation("Fetching symbols for exchange {Exchange}", exchange);

            var symbols = await _finnhubService.GetCryptoSymbolsAsync(exchange, cancellationToken);

            return Ok(symbols);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid exchange for symbols request");
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error fetching symbols");
            return StatusCode(500, new { error = "Failed to fetch cryptocurrency symbols" });
        }
    }

    /// <summary>
    /// Gets candlestick data for multiple symbols (batch request)
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols (e.g., BINANCE:BTCUSDT,BINANCE:ETHUSDT)</param>
    /// <param name="resolution">Candle resolution: 1, 5, 15, 30, 60, D, W, M (default: 1)</param>
    /// <param name="from">Start time (Unix timestamp)</param>
    /// <param name="to">End time (Unix timestamp)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of symbol to candles</returns>
    [HttpGet("candles/batch")]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<CryptoCandle>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IDictionary<string, IEnumerable<CryptoCandle>>>> GetCandlesBatch(
        [FromQuery] string symbols,
        [FromQuery] string resolution = "1",
        [FromQuery] long? from = null,
        [FromQuery] long? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbols))
                return BadRequest("Symbols parameter is required (comma-separated list)");

            var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            if (!symbolList.Any())
                return BadRequest("At least one symbol is required");

            if (symbolList.Count > 10)
                return BadRequest("Maximum 10 symbols allowed per batch request");

            // Default to last 24 hours if not specified
            var toTimestamp = to ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fromTimestamp = from ?? toTimestamp - (24 * 60 * 60);

            _logger.LogInformation("Fetching candles for {Count} symbols", symbolList.Count);

            var result = new Dictionary<string, IEnumerable<CryptoCandle>>();

            foreach (var symbol in symbolList)
            {
                try
                {
                    var candles = await _finnhubService.GetCryptoCandlesAsync(
                        symbol, resolution, fromTimestamp, toTimestamp, cancellationToken);
                    result[symbol] = candles;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch candles for {Symbol}", symbol);
                    result[symbol] = new List<CryptoCandle>();
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch candles request");
            return StatusCode(500, new { error = "Failed to process batch request" });
        }
    }
}

/// <summary>
/// Quote response model
/// </summary>
public class QuoteResponse
{
    /// <summary>
    /// Trading symbol
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Current price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Timestamp of quote (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
}
