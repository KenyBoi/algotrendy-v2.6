namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Represents a Level 2 order book snapshot with bid and ask levels
/// Used for market making strategies (Avellaneda-Stoikov)
/// </summary>
public class OrderBookSnapshot
{
    /// <summary>
    /// Trading symbol (e.g., "BTCUSDT")
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Exchange providing the order book data
    /// </summary>
    public required string Exchange { get; init; }

    /// <summary>
    /// Timestamp of the snapshot (UTC)
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Bid levels (sorted by price descending)
    /// Index 0 = Best bid (highest price)
    /// </summary>
    public required List<OrderBookLevel> Bids { get; init; }

    /// <summary>
    /// Ask levels (sorted by price ascending)
    /// Index 0 = Best ask (lowest price)
    /// </summary>
    public required List<OrderBookLevel> Asks { get; init; }

    /// <summary>
    /// Best bid price (highest buy price)
    /// </summary>
    public decimal BestBid => Bids.Any() ? Bids[0].Price : 0;

    /// <summary>
    /// Best ask price (lowest sell price)
    /// </summary>
    public decimal BestAsk => Asks.Any() ? Asks[0].Price : 0;

    /// <summary>
    /// Bid-ask spread (absolute)
    /// </summary>
    public decimal Spread => BestAsk - BestBid;

    /// <summary>
    /// Bid-ask spread as percentage of mid price
    /// </summary>
    public decimal SpreadPercent => Spread / MidPrice;

    /// <summary>
    /// Mid price (simple average of best bid and ask)
    /// </summary>
    public decimal MidPrice => (BestBid + BestAsk) / 2;

    /// <summary>
    /// Microprice (volume-weighted mid price)
    /// More accurate than simple mid price for predicting next trade price
    /// Formula: (BestBid * AskVolume + BestAsk * BidVolume) / (BidVolume + AskVolume)
    /// </summary>
    public decimal Microprice
    {
        get
        {
            if (!Bids.Any() || !Asks.Any()) return MidPrice;

            var bidVolume = Bids[0].Quantity;
            var askVolume = Asks[0].Quantity;

            if (bidVolume + askVolume == 0) return MidPrice;

            return (BestBid * askVolume + BestAsk * bidVolume) / (bidVolume + askVolume);
        }
    }

    /// <summary>
    /// Total bid depth (dollar value) at N levels
    /// </summary>
    /// <param name="levels">Number of levels to include (default 5)</param>
    public decimal GetBidDepth(int levels = 5)
    {
        return Bids
            .Take(levels)
            .Sum(b => b.Price * b.Quantity);
    }

    /// <summary>
    /// Total ask depth (dollar value) at N levels
    /// </summary>
    /// <param name="levels">Number of levels to include (default 5)</param>
    public decimal GetAskDepth(int levels = 5)
    {
        return Asks
            .Take(levels)
            .Sum(a => a.Price * a.Quantity);
    }

    /// <summary>
    /// Total depth (bid + ask) at N levels
    /// </summary>
    /// <param name="levels">Number of levels to include (default 5)</param>
    public decimal GetTotalDepth(int levels = 5)
    {
        return GetBidDepth(levels) + GetAskDepth(levels);
    }

    /// <summary>
    /// Order book imbalance (OBI) at N levels
    /// OBI = (BidVolume - AskVolume) / (BidVolume + AskVolume)
    /// Range: -1 (all asks) to +1 (all bids)
    /// Positive = buy pressure, Negative = sell pressure
    /// </summary>
    /// <param name="levels">Number of levels to include (default 5)</param>
    public decimal GetOrderBookImbalance(int levels = 5)
    {
        var bidVolume = Bids.Take(levels).Sum(b => b.Quantity);
        var askVolume = Asks.Take(levels).Sum(a => a.Quantity);

        if (bidVolume + askVolume == 0) return 0;

        return (bidVolume - askVolume) / (bidVolume + askVolume);
    }

    /// <summary>
    /// Weighted mid price (volume-weighted across multiple levels)
    /// </summary>
    /// <param name="levels">Number of levels to include</param>
    public decimal GetWeightedMidPrice(int levels = 5)
    {
        var bidVolume = Bids.Take(levels).Sum(b => b.Quantity);
        var askVolume = Asks.Take(levels).Sum(a => a.Quantity);

        if (bidVolume + askVolume == 0) return MidPrice;

        var bidPrice = Bids.Take(levels).Sum(b => b.Price * b.Quantity) / bidVolume;
        var askPrice = Asks.Take(levels).Sum(a => a.Price * a.Quantity) / askVolume;

        return (bidPrice * askVolume + askPrice * bidVolume) / (bidVolume + askVolume);
    }

    /// <summary>
    /// Validates the order book snapshot
    /// </summary>
    public bool IsValid()
    {
        // Must have at least one bid and one ask
        if (!Bids.Any() || !Asks.Any()) return false;

        // Best bid must be less than best ask (no crossed market)
        if (BestBid >= BestAsk) return false;

        // Bids must be sorted descending
        for (int i = 1; i < Bids.Count; i++)
        {
            if (Bids[i].Price >= Bids[i - 1].Price) return false;
        }

        // Asks must be sorted ascending
        for (int i = 1; i < Asks.Count; i++)
        {
            if (Asks[i].Price <= Asks[i - 1].Price) return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a deep copy of this order book snapshot
    /// </summary>
    public OrderBookSnapshot Clone()
    {
        return new OrderBookSnapshot
        {
            Symbol = Symbol,
            Exchange = Exchange,
            Timestamp = Timestamp,
            Bids = Bids.Select(b => b.Clone()).ToList(),
            Asks = Asks.Select(a => a.Clone()).ToList()
        };
    }

    public override string ToString()
    {
        return $"{Symbol} @ {Exchange} | Spread: {Spread:F8} ({SpreadPercent:P2}) | " +
               $"Bid: {BestBid:F8} | Ask: {BestAsk:F8} | Micro: {Microprice:F8} | " +
               $"Depth: {GetTotalDepth():C2} | OBI: {GetOrderBookImbalance():F2}";
    }
}
