using AlgoTrendy.Core.Enums;

namespace AlgoTrendy.Core.Models;

/// <summary>
/// Represents a single trade execution (tick) from an exchange
/// Used for high-frequency data collection and tick chart generation
/// </summary>
public class TickData
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exact timestamp of the trade execution in UTC
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Trade execution price
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Quantity traded in base currency
    /// </summary>
    public required decimal Quantity { get; init; }

    /// <summary>
    /// Quote volume (Price * Quantity)
    /// </summary>
    public decimal QuoteVolume => Price * Quantity;

    /// <summary>
    /// Side of the trade (buy/sell from taker perspective)
    /// True = Buy (taker was buyer), False = Sell (taker was seller)
    /// </summary>
    public required bool IsBuyerMaker { get; init; }

    /// <summary>
    /// Exchange trade ID (unique per exchange)
    /// </summary>
    public long? TradeId { get; init; }

    /// <summary>
    /// Data source (binance, okx, bybit, etc.)
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Asset type classification
    /// </summary>
    public AssetType AssetType { get; set; } = AssetType.Cryptocurrency;

    /// <summary>
    /// Indicates if this was a market buy (bullish pressure)
    /// </summary>
    public bool IsMarketBuy => !IsBuyerMaker;

    /// <summary>
    /// Indicates if this was a market sell (bearish pressure)
    /// </summary>
    public bool IsMarketSell => IsBuyerMaker;
}
