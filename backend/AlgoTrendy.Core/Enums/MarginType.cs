namespace AlgoTrendy.Core.Enums;

/// <summary>
/// Margin type for leveraged positions
/// </summary>
public enum MarginType
{
    /// <summary>
    /// Cross margin - entire account is used as collateral (shared margin pool)
    /// </summary>
    Cross,

    /// <summary>
    /// Isolated margin - specific amount allocated per position (position-level collateral)
    /// </summary>
    Isolated
}