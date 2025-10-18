using AlgoTrendy.API.Hubs;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace AlgoTrendy.API.Services;

/// <summary>
/// Background service that broadcasts market data updates to WebSocket clients
/// </summary>
public class MarketDataBroadcastService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<MarketDataHub> _hubContext;
    private readonly ILogger<MarketDataBroadcastService> _logger;
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromSeconds(1);

    // Symbols to monitor (in production, this would come from configuration or database)
    private readonly string[] _watchedSymbols = new[]
    {
        "BTCUSDT", "ETHUSDT", "BNBUSDT", "ADAUSDT", "DOGEUSDT",
        "XRPUSDT", "DOTUSDT", "SOLUSDT", "AVAXUSDT", "LINKUSDT"
    };

    public MarketDataBroadcastService(
        IServiceProvider serviceProvider,
        IHubContext<MarketDataHub> hubContext,
        ILogger<MarketDataBroadcastService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market Data Broadcast Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await BroadcastMarketDataAsync(stoppingToken);
                await Task.Delay(_broadcastInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Market Data Broadcast Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Market Data Broadcast Service");
            throw;
        }
    }

    private async Task BroadcastMarketDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var marketDataRepository = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

            // Fetch latest market data for all watched symbols
            var latestData = await marketDataRepository.GetLatestBatchAsync(
                _watchedSymbols,
                cancellationToken);

            if (!latestData.Any())
            {
                return;
            }

            _logger.LogDebug(
                "Broadcasting market data for {Count} symbols",
                latestData.Count);

            // Broadcast each symbol's data to its specific group
            foreach (var (symbol, marketData) in latestData)
            {
                await _hubContext.Clients
                    .Group($"market-{symbol}")
                    .SendAsync("MarketDataUpdate", marketData, cancellationToken);
            }

            // Also broadcast a summary to all connected clients
            var summary = new
            {
                Timestamp = DateTime.UtcNow,
                SymbolCount = latestData.Count,
                Symbols = latestData.Keys.ToArray()
            };

            await _hubContext.Clients.All.SendAsync(
                "MarketDataSummary",
                summary,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting market data");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Market Data Broadcast Service stopping");
        await base.StopAsync(cancellationToken);
    }
}
