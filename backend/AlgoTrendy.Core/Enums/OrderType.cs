namespace AlgoTrendy.Core.Enums;

/// <summary>
/// Represents the type of trading order
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Market order - executes at current market price
    /// </summary>
    Market,

    /// <summary>
    /// Limit order - executes at specified price or better
    /// </summary>
    Limit,

    /// <summary>
    /// Stop-loss order - triggers when price reaches stop price
    /// </summary>
    StopLoss,

    /// <summary>
    /// Stop-limit order - becomes limit order when stop price reached
    /// </summary>
    StopLimit,

    /// <summary>
    /// Take-profit order - closes position at profit target
    /// </summary>
    TakeProfit
}
