using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Service interface for managing debt, margin, and leverage operations
/// Module is designed to be independently upgradeable without affecting other system components
/// </summary>
public interface IDebtManagementService
{
    /// <summary>
    /// Sets leverage for a specific position or symbol
    /// </summary>
    /// <param name="symbol">Trading symbol (e.g., "BTCUSDT")</param>
    /// <param name="leverage">Leverage multiplier (e.g., 3.0 for 3x)</param>
    /// <param name="marginType">Type of margin (Cross or Isolated)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>LeverageInfo with updated leverage details</returns>
    Task<LeverageInfo> SetLeverageAsync(
        string symbol,
        decimal leverage,
        Enums.MarginType marginType = Enums.MarginType.Cross,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current leverage information for a symbol
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current leverage information</returns>
    Task<LeverageInfo> GetLeverageInfoAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets margin health ratio for the entire account
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Margin health ratio (0.0 to 1.0)</returns>
    Task<decimal> GetMarginHealthRatioAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total leverage exposure across all positions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sum of all position exposure values</returns>
    Task<decimal> GetTotalLeverageExposureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates liquidation price for a position
    /// </summary>
    /// <param name="position">Position to calculate for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Liquidation price</returns>
    Task<decimal> CalculateLiquidationPriceAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions at risk of liquidation
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of positions in liquidation risk</returns>
    Task<IEnumerable<Position>> GetLiquidationRiskPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a new leverage setting complies with risk limits
    /// </summary>
    /// <param name="symbol">Trading symbol</param>
    /// <param name="newLeverage">Proposed leverage multiplier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateLeverageAsync(string symbol, decimal newLeverage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets debt summary for the account
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debt summary information</returns>
    Task<DebtSummary> GetDebtSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a leveraged position and settles debt
    /// </summary>
    /// <param name="position">Position to close</param>
    /// <param name="reason">Reason for closure (e.g., "Margin Call", "Manual", "Liquidation")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully closed</returns>
    Task<bool> CloseLeveragedPositionAsync(Position position, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets margin configuration limits for the broker
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Margin configuration limits</returns>
    Task<MarginConfiguration> GetMarginConfigurationAsync(CancellationToken cancellationToken = default);
}
