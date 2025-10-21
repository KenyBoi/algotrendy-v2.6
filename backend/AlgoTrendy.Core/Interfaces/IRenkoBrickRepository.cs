using AlgoTrendy.Core.Models;

namespace AlgoTrendy.Core.Interfaces;

/// <summary>
/// Repository interface for Renko brick operations
/// Handles storage and retrieval of Renko chart bricks
/// </summary>
public interface IRenkoBrickRepository
{
    /// <summary>
    /// Inserts a single Renko brick record
    /// </summary>
    Task<bool> InsertAsync(RenkoBrick brick, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple Renko bricks in batch
    /// </summary>
    Task<int> InsertBatchAsync(IEnumerable<RenkoBrick> bricks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Renko bricks for a specific symbol and brick size within a time range
    /// </summary>
    Task<IEnumerable<RenkoBrick>> GetBySymbolAsync(
        string symbol,
        decimal brickSize,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest Renko bricks for a symbol and brick size
    /// </summary>
    Task<IEnumerable<RenkoBrick>> GetLatestAsync(
        string symbol,
        decimal brickSize,
        int count = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest Renko brick for a specific symbol and brick size
    /// </summary>
    Task<RenkoBrick?> GetLatestSingleAsync(
        string symbol,
        decimal brickSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available brick sizes for a symbol
    /// </summary>
    Task<IEnumerable<decimal>> GetAvailableBrickSizesAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Renko trend information (count of consecutive up/down bricks)
    /// </summary>
    Task<RenkoTrendInfo> GetTrendInfoAsync(
        string symbol,
        decimal brickSize,
        int lookbackCount = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about Renko trend
/// </summary>
public class RenkoTrendInfo
{
    public required string Symbol { get; init; }
    public required decimal BrickSize { get; init; }
    public int ConsecutiveUpBricks { get; init; }
    public int ConsecutiveDownBricks { get; init; }
    public bool IsUpTrend => ConsecutiveUpBricks > ConsecutiveDownBricks;
    public bool IsDownTrend => ConsecutiveDownBricks > ConsecutiveUpBricks;
    public int TrendStrength => Math.Max(ConsecutiveUpBricks, ConsecutiveDownBricks);
}
