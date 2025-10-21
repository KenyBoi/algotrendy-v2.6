namespace AlgoTrendy.API.Controllers;

using AlgoTrendy.Core.Models;
using AlgoTrendy.TradingEngine.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Advanced Technical Indicators API
/// Provides professional-grade indicators for algorithmic trading
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdvancedIndicatorsController : ControllerBase
{
    private readonly AdvancedIndicatorService _advancedIndicators;
    private readonly ILogger<AdvancedIndicatorsController> _logger;

    public AdvancedIndicatorsController(
        AdvancedIndicatorService advancedIndicators,
        ILogger<AdvancedIndicatorsController> logger)
    {
        _advancedIndicators = advancedIndicators;
        _logger = logger;
    }

    /// <summary>
    /// Calculate all advanced indicators for a symbol and timeframe
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<AdvancedIndicatorsResponse>> CalculateAllIndicators(
        [FromBody] AdvancedIndicatorsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Calculating advanced indicators - Symbol: {Symbol}, Timeframe: {Timeframe}, DataPoints: {DataPoints}",
                request.Symbol, request.Timeframe, request.Data.Count);

            // Calculate all indicators in parallel for performance
            var tasks = new List<Task>();
            var response = new AdvancedIndicatorsResponse
            {
                Symbol = request.Symbol,
                Timeframe = request.Timeframe,
                Timestamp = DateTime.UtcNow
            };

            // Advanced Momentum
            var fisherTask = _advancedIndicators.CalculateFisherTransformAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.FisherTransform = t.Result);

            var laguerreTask = _advancedIndicators.CalculateLaguerreRSIAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.LaguerreRSI = t.Result);

            var connorsTask = _advancedIndicators.CalculateConnorsRSIAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.ConnorsRSI = t.Result);

            var squeezeTask = _advancedIndicators.CalculateSqueezeMomentumAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.SqueezeMomentum = t.Result);

            var waveTrendTask = _advancedIndicators.CalculateWaveTrendAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.WaveTrend = t.Result);

            var rviTask = _advancedIndicators.CalculateRVIAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.RVI = t.Result);

            var stcTask = _advancedIndicators.CalculateSchaffTrendCycleAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.SchaffTrendCycle = t.Result);

            // Volatility & Risk
            var hvTask = _advancedIndicators.CalculateHistoricalVolatilityAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.HistoricalVolatility = t.Result);

            var parkinsonTask = _advancedIndicators.CalculateParkinsonVolatilityAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.ParkinsonVolatility = t.Result);

            var gkTask = _advancedIndicators.CalculateGarmanKlassVolatilityAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.GarmanKlassVolatility = t.Result);

            var yzTask = _advancedIndicators.CalculateYangZhangVolatilityAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.YangZhangVolatility = t.Result);

            var chopTask = _advancedIndicators.CalculateChoppinessIndexAsync(
                request.Symbol, request.Data, cancellationToken: cancellationToken)
                .ContinueWith(t => response.ChoppinessIndex = t.Result);

            // Wait for all calculations
            await Task.WhenAll(
                fisherTask, laguerreTask, connorsTask, squeezeTask, waveTrendTask,
                rviTask, stcTask, hvTask, parkinsonTask, gkTask, yzTask, chopTask
            );

            // Calculate overall signal
            response.OverallSignal = DetermineOverallSignal(response);
            response.SignalStrength = CalculateSignalStrength(response);

            _logger.LogInformation(
                "Advanced indicators calculated - Symbol: {Symbol}, OverallSignal: {Signal}, Strength: {Strength}",
                request.Symbol, response.OverallSignal, response.SignalStrength);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating advanced indicators for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = "Failed to calculate indicators", details = ex.Message });
        }
    }

    /// <summary>
    /// Calculate Fisher Transform only
    /// </summary>
    [HttpPost("fisher")]
    public async Task<ActionResult<FisherTransformResult>> CalculateFisherTransform(
        [FromBody] IndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _advancedIndicators.CalculateFisherTransformAsync(
                request.Symbol, request.Data, request.Period ?? 10, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Fisher Transform for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate Squeeze Momentum only
    /// </summary>
    [HttpPost("squeeze")]
    public async Task<ActionResult<SqueezeMomentumResult>> CalculateSqueezeMomentum(
        [FromBody] IndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _advancedIndicators.CalculateSqueezeMomentumAsync(
                request.Symbol, request.Data, request.Period ?? 20, cancellationToken: cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Squeeze Momentum for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate Choppiness Index only
    /// </summary>
    [HttpPost("choppiness")]
    public async Task<ActionResult<ChoppinessResult>> CalculateChoppiness(
        [FromBody] IndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _advancedIndicators.CalculateChoppinessIndexAsync(
                request.Symbol, request.Data, request.Period ?? 14, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Choppiness Index for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate Multi-Timeframe RSI
    /// </summary>
    [HttpPost("mtf-rsi")]
    public async Task<ActionResult<MTFIndicatorResult>> CalculateMTFRSI(
        [FromBody] MTFIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _advancedIndicators.CalculateMTFRSIAsync(
                request.Symbol, request.TimeframeData, request.Period ?? 14, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating MTF RSI for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Calculate Multi-Timeframe Moving Average Trend
    /// </summary>
    [HttpPost("mtf-ma")]
    public async Task<ActionResult<MTFIndicatorResult>> CalculateMTFMovingAverage(
        [FromBody] MTFIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _advancedIndicators.CalculateMTFMovingAverageAsync(
                request.Symbol, request.TimeframeData, cancellationToken: cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating MTF MA for {Symbol}", request.Symbol);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get list of all available advanced indicators
    /// </summary>
    [HttpGet("list")]
    public ActionResult<IndicatorListResponse> GetIndicatorList()
    {
        var response = new IndicatorListResponse
        {
            Categories = new Dictionary<string, List<IndicatorInfo>>
            {
                ["Advanced Momentum"] = new()
                {
                    new() { Name = "Ehlers Fisher Transform", Description = "Gaussian price distribution", Priority = "⭐⭐" },
                    new() { Name = "Laguerre RSI", Description = "Low-lag RSI variant", Priority = "⭐⭐" },
                    new() { Name = "Connors RSI", Description = "Short-term mean reversion", Priority = "⭐⭐" },
                    new() { Name = "Squeeze Momentum", Description = "BBands + Keltner squeeze", Priority = "⭐⭐⭐" },
                    new() { Name = "Wave Trend Oscillator", Description = "TCI + MF combination", Priority = "⭐⭐" },
                    new() { Name = "Relative Vigor Index", Description = "Close vs open momentum", Priority = "⭐⭐" },
                    new() { Name = "Schaff Trend Cycle", Description = "Enhanced MACD", Priority = "⭐⭐" }
                },
                ["Volatility & Risk"] = new()
                {
                    new() { Name = "Historical Volatility", Description = "Realized volatility", Priority = "⭐⭐" },
                    new() { Name = "Parkinson Volatility", Description = "High-Low volatility estimator", Priority = "⭐⭐" },
                    new() { Name = "Garman-Klass Volatility", Description = "OHLC volatility estimator", Priority = "⭐⭐" },
                    new() { Name = "Yang-Zhang Volatility", Description = "Best OHLC estimator", Priority = "⭐⭐" },
                    new() { Name = "Choppiness Index", Description = "Trend vs range detection", Priority = "⭐⭐⭐" }
                },
                ["Multi-Timeframe"] = new()
                {
                    new() { Name = "MTF RSI", Description = "RSI across multiple timeframes", Priority = "⭐⭐⭐" },
                    new() { Name = "MTF Moving Averages", Description = "MAs from higher timeframes", Priority = "⭐⭐⭐" },
                    new() { Name = "MTF MACD", Description = "MACD on multiple timeframes", Priority = "⭐⭐" },
                    new() { Name = "HTF Trend Filter", Description = "Higher timeframe trend", Priority = "⭐⭐⭐" }
                }
            },
            TotalIndicators = 18
        };

        return Ok(response);
    }

    #region Helper Methods

    private string DetermineOverallSignal(AdvancedIndicatorsResponse response)
    {
        var signals = new List<string>();

        // Collect all signals
        if (response.FisherTransform != null)
            signals.Add(response.FisherTransform.Signal);
        if (response.SqueezeMomentum != null)
            signals.Add(response.SqueezeMomentum.Signal);
        if (response.WaveTrend != null)
            signals.Add(response.WaveTrend.Signal);
        if (response.RVI != null)
            signals.Add(response.RVI.Trend);

        // Count buy/sell signals
        var buyCount = signals.Count(s => s.Contains("BUY") || s.Contains("BULLISH"));
        var sellCount = signals.Count(s => s.Contains("SELL") || s.Contains("BEARISH"));

        if (buyCount > sellCount * 1.5) return "STRONG_BUY";
        if (buyCount > sellCount) return "BUY";
        if (sellCount > buyCount * 1.5) return "STRONG_SELL";
        if (sellCount > buyCount) return "SELL";

        return "NEUTRAL";
    }

    private decimal CalculateSignalStrength(AdvancedIndicatorsResponse response)
    {
        var signals = new List<string>();
        var maxSignals = 0m;

        if (response.FisherTransform != null) { signals.Add(response.FisherTransform.Signal); maxSignals++; }
        if (response.SqueezeMomentum != null) { signals.Add(response.SqueezeMomentum.Signal); maxSignals++; }
        if (response.WaveTrend != null) { signals.Add(response.WaveTrend.Signal); maxSignals++; }
        if (response.RVI != null) { signals.Add(response.RVI.Trend); maxSignals++; }
        if (response.ChoppinessIndex != null) maxSignals++;

        var agreementCount = signals.Count(s => s.Contains("BUY") || s.Contains("BULLISH"))
                           + signals.Count(s => s.Contains("SELL") || s.Contains("BEARISH"));

        return maxSignals > 0 ? (agreementCount / maxSignals) * 100m : 0m;
    }

    #endregion
}

#region Request/Response Models

public class AdvancedIndicatorsRequest
{
    public required string Symbol { get; set; }
    public required string Timeframe { get; set; }
    public required List<MarketData> Data { get; set; }
}

public class IndicatorRequest
{
    public required string Symbol { get; set; }
    public required List<MarketData> Data { get; set; }
    public int? Period { get; set; }
}

public class MTFIndicatorRequest
{
    public required string Symbol { get; set; }
    public required Dictionary<string, IEnumerable<MarketData>> TimeframeData { get; set; }
    public int? Period { get; set; }
}

public class AdvancedIndicatorsResponse
{
    public required string Symbol { get; set; }
    public required string Timeframe { get; set; }
    public DateTime Timestamp { get; set; }

    // Advanced Momentum
    public FisherTransformResult? FisherTransform { get; set; }
    public decimal? LaguerreRSI { get; set; }
    public decimal? ConnorsRSI { get; set; }
    public SqueezeMomentumResult? SqueezeMomentum { get; set; }
    public WaveTrendResult? WaveTrend { get; set; }
    public RVIResult? RVI { get; set; }
    public decimal? SchaffTrendCycle { get; set; }

    // Volatility & Risk
    public decimal? HistoricalVolatility { get; set; }
    public decimal? ParkinsonVolatility { get; set; }
    public decimal? GarmanKlassVolatility { get; set; }
    public decimal? YangZhangVolatility { get; set; }
    public ChoppinessResult? ChoppinessIndex { get; set; }

    // Overall Assessment
    public string OverallSignal { get; set; } = "NEUTRAL";
    public decimal SignalStrength { get; set; }
}

public class IndicatorListResponse
{
    public required Dictionary<string, List<IndicatorInfo>> Categories { get; set; }
    public int TotalIndicators { get; set; }
}

public class IndicatorInfo
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Priority { get; set; }
}

#endregion
