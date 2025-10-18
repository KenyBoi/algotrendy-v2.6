namespace AlgoTrendy.Core.Enums;

/// <summary>
/// Represents the current status of a trading order
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created but not yet submitted to exchange
    /// </summary>
    Pending,

    /// <summary>
    /// Order has been submitted to exchange and is active
    /// </summary>
    Open,

    /// <summary>
    /// Order has been partially filled
    /// </summary>
    PartiallyFilled,

    /// <summary>
    /// Order has been completely filled
    /// </summary>
    Filled,

    /// <summary>
    /// Order has been cancelled by user or system
    /// </summary>
    Cancelled,

    /// <summary>
    /// Order was rejected by exchange (insufficient funds, invalid params, etc.)
    /// </summary>
    Rejected,

    /// <summary>
    /// Order has expired (time-in-force limit reached)
    /// </summary>
    Expired
}
