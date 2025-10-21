namespace AlgoTrendy.TradingEngine.Models.MarketMaking;

/// <summary>
/// Avellaneda-Stoikov trading signal
/// Contains bid/ask prices and quantities calculated by the strategy
/// </summary>
public class ASSignal
{
    /// <summary>
    /// Symbol for this signal
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Timestamp when signal was generated
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Bid price (price we want to buy at)
    /// </summary>
    public required decimal BidPrice { get; init; }

    /// <summary>
    /// Ask price (price we want to sell at)
    /// </summary>
    public required decimal AskPrice { get; init; }

    /// <summary>
    /// Bid quantity (amount we want to buy)
    /// </summary>
    public required decimal BidQuantity { get; init; }

    /// <summary>
    /// Ask quantity (amount we want to sell)
    /// </summary>
    public required decimal AskQuantity { get; init; }

    /// <summary>
    /// Reservation price (optimal mid price based on inventory)
    /// </summary>
    public decimal? ReservationPrice { get; init; }

    /// <summary>
    /// Optimal spread calculated by AS formula
    /// </summary>
    public decimal? OptimalSpread { get; init; }

    /// <summary>
    /// Current inventory level when signal was generated
    /// </summary>
    public decimal? CurrentInventory { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// Higher = more confident in signal quality
    /// </summary>
    public decimal Confidence { get; init; } = 1.0m;

    /// <summary>
    /// Whether this signal is valid for execution
    /// </summary>
    public bool IsValid { get; init; } = true;

    /// <summary>
    /// Reason why signal is invalid (if IsValid = false)
    /// </summary>
    public string? InvalidReason { get; init; }

    // Computed properties

    /// <summary>
    /// Spread (ask - bid)
    /// </summary>
    public decimal Spread => AskPrice - BidPrice;

    /// <summary>
    /// Spread as percentage of mid price
    /// </summary>
    public decimal SpreadPercent => Spread / MidPrice;

    /// <summary>
    /// Mid price ((bid + ask) / 2)
    /// </summary>
    public decimal MidPrice => (BidPrice + AskPrice) / 2.0m;

    /// <summary>
    /// Spread in basis points (bps)
    /// </summary>
    public decimal SpreadBps => SpreadPercent * 10000.0m;

    /// <summary>
    /// Total notional value (bid qty * bid price + ask qty * ask price)
    /// </summary>
    public decimal TotalNotional => (BidQuantity * BidPrice) + (AskQuantity * AskPrice);

    /// <summary>
    /// Validates signal is executable
    /// </summary>
    /// <param name="minSpreadBps">Minimum allowed spread (bps)</param>
    /// <param name="maxSpreadBps">Maximum allowed spread (bps)</param>
    /// <returns>True if signal is valid for execution</returns>
    public bool ValidateForExecution(decimal minSpreadBps = 1.0m, decimal maxSpreadBps = 1000.0m)
    {
        if (!IsValid)
            return false;

        if (BidPrice <= 0 || AskPrice <= 0)
            return false;

        if (BidQuantity <= 0 || AskQuantity <= 0)
            return false;

        if (BidPrice >= AskPrice)
            return false; // Crossed market

        var spreadBps = SpreadBps;
        if (spreadBps < minSpreadBps || spreadBps > maxSpreadBps)
            return false;

        return true;
    }

    /// <summary>
    /// Creates an invalid signal with reason
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="reason">Reason for invalidity</param>
    /// <returns>Invalid ASSignal</returns>
    public static ASSignal CreateInvalid(string symbol, string reason)
    {
        return new ASSignal
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            BidPrice = 0,
            AskPrice = 0,
            BidQuantity = 0,
            AskQuantity = 0,
            IsValid = false,
            InvalidReason = reason,
            Confidence = 0.0m
        };
    }

    /// <summary>
    /// Deep clone
    /// </summary>
    public ASSignal Clone()
    {
        return new ASSignal
        {
            Symbol = Symbol,
            Timestamp = Timestamp,
            BidPrice = BidPrice,
            AskPrice = AskPrice,
            BidQuantity = BidQuantity,
            AskQuantity = AskQuantity,
            ReservationPrice = ReservationPrice,
            OptimalSpread = OptimalSpread,
            CurrentInventory = CurrentInventory,
            Confidence = Confidence,
            IsValid = IsValid,
            InvalidReason = InvalidReason
        };
    }

    public override string ToString()
    {
        if (!IsValid)
            return $"ASSignal[{Symbol}]: INVALID - {InvalidReason}";

        return $"ASSignal[{Symbol}]: " +
               $"Bid={BidPrice:F2}@{BidQuantity:F4}, Ask={AskPrice:F2}@{AskQuantity:F4}, " +
               $"Spread={SpreadBps:F1}bps, Conf={Confidence:P0}";
    }
}
