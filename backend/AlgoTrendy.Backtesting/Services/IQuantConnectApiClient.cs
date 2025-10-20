using AlgoTrendy.Backtesting.Models.QuantConnect;

namespace AlgoTrendy.Backtesting.Services;

/// <summary>
/// Interface for QuantConnect REST API client
/// </summary>
public interface IQuantConnectApiClient
{
    /// <summary>
    /// Authenticate with QuantConnect API to verify credentials
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authentication successful</returns>
    Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new QuantConnect project
    /// </summary>
    /// <param name="name">Project name</param>
    /// <param name="language">Programming language (CSharp or Python)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project creation response</returns>
    Task<QCProjectResponse> CreateProjectAsync(
        string name,
        string language = "CSharp",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read project details
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project details</returns>
    Task<QCProjectResponse> ReadProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update a file in a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="fileName">File name</param>
    /// <param name="fileContent">File content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> CreateOrUpdateFileAsync(
        int projectId,
        string fileName,
        string fileContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compile a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compile response with compile ID</returns>
    Task<QCCompileResponse> CompileProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read compilation results
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="compileId">Compile ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compilation results</returns>
    Task<QCCompileResponse> ReadCompileAsync(
        int projectId,
        string compileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a backtest
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="compileId">Compile ID from successful compilation</param>
    /// <param name="backtestName">Name for the backtest</param>
    /// <param name="parameters">Optional backtest parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest response with backtest ID</returns>
    Task<QCBacktestResponse> CreateBacktestAsync(
        int projectId,
        string compileId,
        string backtestName,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read backtest results
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="backtestId">Backtest ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backtest results</returns>
    Task<QCBacktestResponse> ReadBacktestAsync(
        int projectId,
        string backtestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all backtests for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of backtest summaries</returns>
    Task<QCBacktestListResponse> ListBacktestsAsync(
        int projectId,
        CancellationToken cancellationToken = default);
}
