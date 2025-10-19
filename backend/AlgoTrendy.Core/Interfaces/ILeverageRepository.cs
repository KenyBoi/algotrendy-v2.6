using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for leverage and debt tracking
/// </summary>
public interface ILeverageRepository
{
    /// <summary>
    /// Records a leverage change for tracking and audit purposes
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="previousLeverage">Previous leverage value</param>
    /// <param name="newLeverage">New leverage value</param>
    /// <param name="marginType">Margin type used</param>
    /// <param name="reason">Reason for the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordLeverageChangeAsync(
        string symbol,
        decimal previousLeverage,
        decimal newLeverage,
        Enums.MarginType marginType,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leverage history for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leverage change history</returns>
    Task<IEnumerable<LeverageHistoryRecord>> GetLeverageHistoryAsync(
        string symbol,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current total leverage exposure
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total exposure value</returns>
    Task<decimal> GetTotalLeverageExposureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets highest leverage level currently in use
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Maximum leverage in use</returns>
    Task<decimal> GetMaxLeverageInUseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average leverage across all positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Average leverage value</returns>
    Task<decimal> GetAverageLeverageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets positions using a specific leverage level
    /// </summary>
    /// <param name="leverage">Leverage level to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of positions at that leverage level</returns>
    Task<IEnumerable<Position>> GetPositionsByLeverageAsync(decimal leverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records debt incurred from leveraged trading
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="borrowedAmount">Amount borrowed</param>
    /// <param name="interestRate">Daily interest rate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordDebtAsync(
        string positionId,
        decimal borrowedAmount,
        decimal interestRate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records debt repayment
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="repaymentAmount">Amount repaid</param>
    /// <param name="interestPaid">Interest paid</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordDebtRepaymentAsync(
        string positionId,
        decimal repaymentAmount,
        decimal interestPaid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets debt summary for all positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debt summary</returns>
    Task<DebtSummary> GetDebtSummaryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Record of a leverage change event
/// </summary>
public class LeverageHistoryRecord
{
    public required string Symbol { get; init; }
    public required decimal PreviousLeverage { get; init; }
    public required decimal NewLeverage { get; init; }
    public required Enums.MarginType MarginType { get; init; }
    public required string Reason { get; init; }
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
