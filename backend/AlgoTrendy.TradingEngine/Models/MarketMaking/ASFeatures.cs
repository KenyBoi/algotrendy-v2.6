namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Avellaneda-Stoikov 22 features for Reinforcement Learning agent
/// Features are categorized into 4 groups:
/// - Inventory (4 features)
/// - Order Book (9 features)
/// - Microstructure (4 features)
/// - Volatility/Candles (5 features)
/// </summary>
public class ASFeatures
{
    // ===== Inventory Features (4) =====

    /// <summary>
    /// Current inventory level in base currency
    /// </summary>
    public required decimal CurrentInventory { get; init; }

    /// <summary>
    /// Inventory as percentage of max position (0.0 to 1.0)
    /// </summary>
    public required decimal InventoryPct { get; init; }

    /// <summary>
    /// Distance from inventory target (usually 0)
    /// </summary>
    public required decimal InventoryDistanceFromTarget { get; init; }

    /// <summary>
    /// Rate of change in inventory
    /// </summary>
    public required decimal InventoryChangeRate { get; init; }

    // ===== Order Book Features (9) =====

    /// <summary>
    /// Best bid price
    /// </summary>
    public required decimal BestBid { get; init; }

    /// <summary>
    /// Best ask price
    /// </summary>
    public required decimal BestAsk { get; init; }

    /// <summary>
    /// Volume at best bid level
    /// </summary>
    public required decimal BidVolume { get; init; }

    /// <summary>
    /// Volume at best ask level
    /// </summary>
    public required decimal AskVolume { get; init; }

    /// <summary>
    /// Bid-ask spread (absolute)
    /// </summary>
    public required decimal Spread { get; init; }

    /// <summary>
    /// Spread as percentage of mid price
    /// </summary>
    public required decimal SpreadPct { get; init; }

    /// <summary>
    /// Order book imbalance: (BidVol - AskVol) / (BidVol + AskVol)
    /// Range: -1 to +1
    /// </summary>
    public required decimal OrderBookImbalance { get; init; }

    /// <summary>
    /// Microprice (volume-weighted mid price)
    /// </summary>
    public required decimal Microprice { get; init; }

    /// <summary>
    /// Weighted mid price across multiple levels
    /// </summary>
    public required decimal WeightedMidPrice { get; init; }

    // ===== Market Microstructure Features (4) =====

    /// <summary>
    /// Recent trade direction (buy vs sell pressure)
    /// Positive = more buying, Negative = more selling
    /// </summary>
    public required decimal RecentTradeDirection { get; init; }

    /// <summary>
    /// Trade flow imbalance
    /// </summary>
    public required decimal TradeFlowImbalance { get; init; }

    /// <summary>
    /// Quote update frequency (updates per second)
    /// </summary>
    public required decimal QuoteUpdateFrequency { get; init; }

    /// <summary>
    /// Time since last trade (seconds)
    /// </summary>
    public required decimal TimeSinceLastTrade { get; init; }

    // ===== Volatility & Price Action Features (5) =====

    /// <summary>
    /// 1-minute return volatility
    /// </summary>
    public required decimal Volatility1Min { get; init; }

    /// <summary>
    /// Price momentum (1-minute)
    /// </summary>
    public required decimal Momentum1Min { get; init; }

    /// <summary>
    /// Volume (1-minute)
    /// </summary>
    public required decimal Volume1Min { get; init; }

    /// <summary>
    /// Distance from VWAP
    /// </summary>
    public required decimal VWAPDistance { get; init; }

    /// <summary>
    /// High-low range (1-minute)
    /// </summary>
    public required decimal HighLowRange1Min { get; init; }

    /// <summary>
    /// Converts features to array for RL agent input
    /// Order must match Python implementation
    /// </summary>
    /// <returns>Array of 22 features</returns>
    public double[] ToArray()
    {
        return new[]
        {
            // Inventory (4)
            (double)CurrentInventory,
            (double)InventoryPct,
            (double)InventoryDistanceFromTarget,
            (double)InventoryChangeRate,

            // Order Book (9)
            (double)BestBid,
            (double)BestAsk,
            (double)BidVolume,
            (double)AskVolume,
            (double)Spread,
            (double)SpreadPct,
            (double)OrderBookImbalance,
            (double)Microprice,
            (double)WeightedMidPrice,

            // Microstructure (4)
            (double)RecentTradeDirection,
            (double)TradeFlowImbalance,
            (double)QuoteUpdateFrequency,
            (double)TimeSinceLastTrade,

            // Volatility/Candles (5)
            (double)Volatility1Min,
            (double)Momentum1Min,
            (double)Volume1Min,
            (double)VWAPDistance,
            (double)HighLowRange1Min
        };
    }

    /// <summary>
    /// Gets feature names in order
    /// </summary>
    public static string[] GetFeatureNames()
    {
        return new[]
        {
            // Inventory
            "CurrentInventory", "InventoryPct", "InventoryDistanceFromTarget", "InventoryChangeRate",

            // Order Book
            "BestBid", "BestAsk", "BidVolume", "AskVolume", "Spread", "SpreadPct",
            "OrderBookImbalance", "Microprice", "WeightedMidPrice",

            // Microstructure
            "RecentTradeDirection", "TradeFlowImbalance", "QuoteUpdateFrequency", "TimeSinceLastTrade",

            // Volatility/Candles
            "Volatility1Min", "Momentum1Min", "Volume1Min", "VWAPDistance", "HighLowRange1Min"
        };
    }

    /// <summary>
    /// Feature count (should always be 22)
    /// </summary>
    public const int FeatureCount = 22;

    public override string ToString()
    {
        return $"ASFeatures[22]: Inv={InventoryPct:P1}, Spread={SpreadPct:P2}, " +
               $"OBI={OrderBookImbalance:F2}, Vol={Volatility1Min:F4}";
    }
}
