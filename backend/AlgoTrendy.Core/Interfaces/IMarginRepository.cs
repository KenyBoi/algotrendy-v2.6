using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for margin-related data persistence
/// </summary>
public interface IMarginRepository
{
    /// <summary>
    /// Gets all active margin positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of margin positions</returns>
    Task<IEnumerable<Position>> GetAllMarginPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets margin positions for a specific symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of positions for the symbol</returns>
    Task<IEnumerable<Position>> GetMarginPositionsBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets positions at risk of liquidation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of liquidation risk positions</returns>
    Task<IEnumerable<Position>> GetLiquidationRiskPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates margin health for a position
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="marginHealthRatio">New margin health ratio</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateMarginHealthAsync(string positionId, decimal marginHealthRatio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a margin call event
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="currentHealthRatio">Current health ratio at time of margin call</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordMarginCallAsync(string positionId, decimal currentHealthRatio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a liquidation event
    /// </summary>
    /// <param name="positionId">Position ID</param>
    /// <param name="liquidationPrice">Price at which liquidation occurred</param>
    /// <param name="pnl">PnL from liquidation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordLiquidationAsync(string positionId, decimal liquidationPrice, decimal pnl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total collateral amount across all margin positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total collateral amount</returns>
    Task<decimal> GetTotalCollateralAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total borrowed amount across all margin positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total borrowed amount</returns>
    Task<decimal> GetTotalBorrowedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accrued interest for all margin positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total accrued interest</returns>
    Task<decimal> GetTotalAccruedInterestAsync(CancellationToken cancellationToken = default);
}
