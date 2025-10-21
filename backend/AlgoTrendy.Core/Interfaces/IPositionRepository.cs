using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for position operations
/// </summary>
public interface IPositionRepository
{
    /// <summary>
    /// Creates a new position
    /// </summary>
    Task<Position> CreateAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing position
    /// </summary>
    Task<Position> UpdateAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a position by its ID
    /// </summary>
    Task<Position?> GetByIdAsync(string positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a position by symbol
    /// </summary>
    Task<Position?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active positions
    /// </summary>
    Task<IEnumerable<Position>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets positions by exchange
    /// </summary>
    Task<IEnumerable<Position>> GetByExchangeAsync(string exchange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets positions by strategy ID
    /// </summary>
    Task<IEnumerable<Position>> GetByStrategyAsync(string strategyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a position (closes it)
    /// </summary>
    Task DeleteAsync(string positionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a position by symbol
    /// </summary>
    Task DeleteBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}
