using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a single brick in a Renko chart
/// Bricks are formed based on price movement only, ignoring time
/// </summary>
public class RenkoBrick
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp when the brick was completed
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Opening price of the brick
    /// </summary>
    public required decimal Open { get; init; }

    /// <summary>
    /// Closing price of the brick
    /// </summary>
    public required decimal Close { get; init; }

    /// <summary>
    /// High price (for traditional Renko, this equals Close for up bricks or Open for down bricks)
    /// For ATR-based Renko with wicks, this can differ
    /// </summary>
    public required decimal High { get; init; }

    /// <summary>
    /// Low price (for traditional Renko, this equals Open for up bricks or Close for down bricks)
    /// For ATR-based Renko with wicks, this can differ
    /// </summary>
    public required decimal Low { get; init; }

    /// <summary>
    /// Brick size in price units (e.g., $100 for BTC, or 1.5% of price)
    /// </summary>
    public required decimal BrickSize { get; init; }

    /// <summary>
    /// Direction of the brick: true = Up (bullish), false = Down (bearish)
    /// </summary>
    public required bool IsUpBrick { get; init; }

    /// <summary>
    /// Volume accumulated during this brick formation
    /// </summary>
    public decimal Volume { get; init; }

    /// <summary>
    /// Quote volume accumulated
    /// </summary>
    public decimal QuoteVolume { get; init; }

    /// <summary>
    /// Number of source candles/ticks that formed this brick
    /// </summary>
    public int SourceDataPoints { get; init; }

    /// <summary>
    /// Data source (binance, okx, bybit, etc.)
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Asset type classification
    /// </summary>
    public AssetType AssetType { get; set; } = AssetType.Cryptocurrency;

    /// <summary>
    /// Indicates if this brick represents a trend reversal
    /// </summary>
    public bool IsReversal { get; init; }

    /// <summary>
    /// Brick size calculation method (Fixed, ATR, Percentage)
    /// </summary>
    public string? SizingMethod { get; init; }

    /// <summary>
    /// Checks if the brick is bullish (upward)
    /// </summary>
    public bool IsBullish => IsUpBrick;

    /// <summary>
    /// Checks if the brick is bearish (downward)
    /// </summary>
    public bool IsBearish => !IsUpBrick;

    /// <summary>
    /// The actual price movement of this brick
    /// </summary>
    public decimal PriceMove => Math.Abs(Close - Open);
}
