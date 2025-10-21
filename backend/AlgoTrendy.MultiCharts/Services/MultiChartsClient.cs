using AlgoTrendy.MultiCharts.Configuration;
using AlgoTrendy.MultiCharts.Interfaces;
using AlgoTrendy.MultiCharts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;

namespace AlgoTrendy.MultiCharts.Services;

/// <summary>
/// Client for MultiCharts .NET platform integration
/// </summary>
public class MultiChartsClient : IMultiChartsClient
{
    private readonly HttpClient _httpClient;
    private readonly MultiChartsOptions _options;
    private readonly ILogger<MultiChartsClient> _logger;

    public MultiChartsClient(
        HttpClient httpClient,
        IOptions<MultiChartsOptions> options,
        ILogger<MultiChartsClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.ApiEndpoint);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to MultiCharts at {Endpoint}", _options.ApiEndpoint);

            var response = await _httpClient.GetAsync("/api/health", cancellationToken);
            var isConnected = response.IsSuccessStatusCode;

            _logger.LogInformation("MultiCharts connection test: {Status}", isConnected ? "SUCCESS" : "FAILED");

            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MultiCharts");
            return false;
        }
    }

    public async Task<MultiChartsPlatformStatus> GetPlatformStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching MultiCharts platform status");

            var response = await _httpClient.GetAsync("/api/status", cancellationToken);
            response.EnsureSuccessStatusCode();

            var status = await response.Content.ReadFromJsonAsync<MultiChartsPlatformStatus>(cancellationToken);

            _logger.LogInformation("Platform status retrieved: Version={Version}, ActiveStrategies={Count}",
                status?.Version, status?.ActiveStrategies);

            return status ?? new MultiChartsPlatformStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get platform status");
            throw;
        }
    }

    public async Task<BacktestResult> RunBacktestAsync(BacktestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting backtest for {Strategy} on {Symbol} from {From} to {To}",
                request.StrategyName, request.Symbol, request.FromDate, request.ToDate);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/backtest", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BacktestResult>(cancellationToken);

            _logger.LogInformation("Backtest completed: NetProfit={Profit}, TotalTrades={Trades}, WinRate={WinRate}%",
                result?.NetProfit, result?.TotalTrades, result?.WinRate * 100);

            return result ?? new BacktestResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtest failed for {Strategy}", request.StrategyName);
            throw;
        }
    }

    public async Task<WalkForwardResult> RunWalkForwardOptimizationAsync(WalkForwardRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting walk-forward optimization for {Strategy} on {Symbol}",
                request.StrategyName, request.Symbol);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/optimization/walk-forward", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WalkForwardResult>(cancellationToken);

            _logger.LogInformation("Walk-forward optimization completed: Windows={Count}, IsRobust={Robust}, Score={Score}",
                result?.Windows.Count, result?.IsRobust, result?.RobustnessScore);

            return result ?? new WalkForwardResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Walk-forward optimization failed for {Strategy}", request.StrategyName);
            throw;
        }
    }

    public async Task<MonteCarloResult> RunMonteCarloSimulationAsync(MonteCarloRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Monte Carlo simulation for {Strategy} with {Runs} runs",
                request.StrategyName, request.NumberOfRuns);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/simulation/monte-carlo", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MonteCarloResult>(cancellationToken);

            _logger.LogInformation("Monte Carlo simulation completed: MeanReturn={Mean}, ProbabilityOfProfit={Prob}%",
                result?.MeanReturn, result?.ProbabilityOfProfit * 100);

            return result ?? new MonteCarloResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monte Carlo simulation failed for {Strategy}", request.StrategyName);
            throw;
        }
    }

    public async Task<StrategyDeploymentResult> DeployStrategyAsync(StrategyDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deploying strategy {Strategy}", request.StrategyName);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/strategy/deploy", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StrategyDeploymentResult>(cancellationToken);

            if (result?.Success == true)
            {
                _logger.LogInformation("Strategy {Strategy} deployed successfully with ID {Id}",
                    result.StrategyName, result.StrategyId);
            }
            else
            {
                _logger.LogWarning("Strategy {Strategy} deployment failed: {Message}",
                    request.StrategyName, result?.Message);
            }

            return result ?? new StrategyDeploymentResult { Success = false, Message = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strategy deployment failed for {Strategy}", request.StrategyName);
            throw;
        }
    }

    public async Task<List<StrategyInfo>> GetStrategiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching strategies list");

            var response = await _httpClient.GetAsync("/api/strategy/list", cancellationToken);
            response.EnsureSuccessStatusCode();

            var strategies = await response.Content.ReadFromJsonAsync<List<StrategyInfo>>(cancellationToken);

            _logger.LogInformation("Retrieved {Count} strategies", strategies?.Count ?? 0);

            return strategies ?? new List<StrategyInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch strategies list");
            throw;
        }
    }

    public async Task<ScanResult> RunMarketScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running market scan: {ScanName} on {Count} symbols",
                request.ScanName, request.Symbols.Count);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/scanner/run", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ScanResult>(cancellationToken);

            _logger.LogInformation("Market scan completed: {Matches}/{Total} symbols matched",
                result?.MatchingSymbols, result?.TotalSymbolsScanned);

            return result ?? new ScanResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market scan failed for {ScanName}", request.ScanName);
            throw;
        }
    }

    public async Task<List<IndicatorInfo>> GetIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching indicators list");

            var response = await _httpClient.GetAsync("/api/indicator/list", cancellationToken);
            response.EnsureSuccessStatusCode();

            var indicators = await response.Content.ReadFromJsonAsync<List<IndicatorInfo>>(cancellationToken);

            _logger.LogInformation("Retrieved {Count} indicators", indicators?.Count ?? 0);

            return indicators ?? new List<IndicatorInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch indicators list");
            throw;
        }
    }

    public async Task<List<OHLCVData>> GetHistoricalDataAsync(DataRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching historical data for {Symbol} from {From} to {To}",
                request.Symbol, request.FromDate, request.ToDate);

            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/data/historical", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<List<OHLCVData>>(cancellationToken);

            _logger.LogInformation("Retrieved {Count} data points for {Symbol}",
                data?.Count ?? 0, request.Symbol);

            return data ?? new List<OHLCVData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch historical data for {Symbol}", request.Symbol);
            throw;
        }
    }
}
