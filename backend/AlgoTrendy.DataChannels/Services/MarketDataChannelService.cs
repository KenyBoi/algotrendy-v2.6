using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.DataChannels.Channels.REST;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AlgoTrendy.DataChannels.Services;

/// <summary>
/// Background service that orchestrates data fetching from all market data channels
/// Manages Binance, OKX, Coinbase, and Kraken REST channels
/// Fetches data on configurable intervals and saves to QuestDB
/// </summary>
public class MarketDataChannelService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketDataChannelService> _logger;
    private readonly IConfiguration _configuration;
    private TimeSpan _fetchInterval;

    public MarketDataChannelService(
        IServiceProvider serviceProvider,
        ILogger<MarketDataChannelService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Get fetch interval from configuration, default to 60 seconds
        var intervalSeconds = _configuration.GetValue<int>("MarketData:FetchIntervalSeconds", 60);
        _fetchInterval = TimeSpan.FromSeconds(intervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketDataChannelService starting with fetch interval: {Interval}s",
            _fetchInterval.TotalSeconds);

        // Wait a bit before starting to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchFromAllChannelsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in market data fetch cycle");
            }

            // Wait for the configured interval before next fetch
            try
            {
                await Task.Delay(_fetchInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation("MarketDataChannelService stopping");
    }

    /// <summary>
    /// Fetch data from all channels and save to database
    /// Each channel runs independently - one failure doesn't stop others
    /// </summary>
    private async Task FetchFromAllChannelsAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting data fetch cycle at {Time}", startTime);

        using var scope = _serviceProvider.CreateScope();
        var totalRecords = 0;
        var successfulChannels = 0;
        var failedChannels = new List<string>();

        // Fetch from each channel independently
        var tasks = new List<Task<(string channelName, int recordCount, bool success)>>
        {
            FetchFromChannelAsync<BinanceRestChannel>(scope, "Binance", cancellationToken),
            FetchFromChannelAsync<OKXRestChannel>(scope, "OKX", cancellationToken),
            FetchFromChannelAsync<CoinbaseRestChannel>(scope, "Coinbase", cancellationToken),
            FetchFromChannelAsync<KrakenRestChannel>(scope, "Kraken", cancellationToken)
        };

        // Wait for all channels to complete
        var results = await Task.WhenAll(tasks);

        // Process results
        foreach (var (channelName, recordCount, success) in results)
        {
            if (success)
            {
                totalRecords += recordCount;
                successfulChannels++;
                _logger.LogInformation("{Channel}: Fetched and saved {Count} records",
                    channelName, recordCount);
            }
            else
            {
                failedChannels.Add(channelName);
            }
        }

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Fetch cycle completed in {Duration}ms: {Total} records from {Success}/{Total} channels",
            duration.TotalMilliseconds,
            totalRecords,
            successfulChannels,
            tasks.Count);

        if (failedChannels.Any())
        {
            _logger.LogWarning("Failed channels: {Channels}", string.Join(", ", failedChannels));
        }
    }

    /// <summary>
    /// Fetch data from a specific channel
    /// Handles errors gracefully without affecting other channels
    /// </summary>
    private async Task<(string channelName, int recordCount, bool success)> FetchFromChannelAsync<TChannel>(
        IServiceScope scope,
        string channelName,
        CancellationToken cancellationToken)
        where TChannel : class, IMarketDataChannel
    {
        try
        {
            var channel = scope.ServiceProvider.GetRequiredService<TChannel>();
            var repository = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();

            // Ensure channel is connected
            if (!channel.IsConnected)
            {
                _logger.LogInformation("Starting {Channel} channel", channelName);
                await channel.StartAsync(cancellationToken);
            }

            // Fetch data from the channel
            var data = await FetchDataFromChannelAsync(channel, cancellationToken);

            if (data.Count == 0)
            {
                _logger.LogWarning("{Channel}: No data fetched", channelName);
                return (channelName, 0, true);
            }

            // Save to database
            var savedCount = await repository.InsertBatchAsync(data, cancellationToken);

            return (channelName, savedCount, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from {Channel}", channelName);
            return (channelName, 0, false);
        }
    }

    /// <summary>
    /// Fetch data from a channel with proper type handling
    /// </summary>
    private async Task<List<Core.Models.MarketData>> FetchDataFromChannelAsync(
        IMarketDataChannel channel,
        CancellationToken cancellationToken)
    {
        // Call FetchDataAsync using reflection since it's not in the interface
        var method = channel.GetType().GetMethod("FetchDataAsync");

        if (method == null)
        {
            throw new InvalidOperationException($"Channel {channel.ExchangeName} does not have FetchDataAsync method");
        }

        // Invoke with default parameters
        var task = method.Invoke(channel, new object?[]
        {
            null,              // symbols (null = use defaults)
            "1m",              // interval
            100,               // limit
            cancellationToken  // cancellationToken
        });

        if (task is Task<List<Core.Models.MarketData>> dataTask)
        {
            return await dataTask;
        }

        throw new InvalidOperationException($"FetchDataAsync returned unexpected type for {channel.ExchangeName}");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping all market data channels");

        using var scope = _serviceProvider.CreateScope();

        // Stop all channels
        var stopTasks = new List<Task>
        {
            StopChannelAsync<BinanceRestChannel>(scope, "Binance", cancellationToken),
            StopChannelAsync<OKXRestChannel>(scope, "OKX", cancellationToken),
            StopChannelAsync<CoinbaseRestChannel>(scope, "Coinbase", cancellationToken),
            StopChannelAsync<KrakenRestChannel>(scope, "Kraken", cancellationToken)
        };

        await Task.WhenAll(stopTasks);

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Stop a specific channel gracefully
    /// </summary>
    private async Task StopChannelAsync<TChannel>(
        IServiceScope scope,
        string channelName,
        CancellationToken cancellationToken)
        where TChannel : class, IMarketDataChannel
    {
        try
        {
            var channel = scope.ServiceProvider.GetRequiredService<TChannel>();

            if (channel.IsConnected)
            {
                await channel.StopAsync(cancellationToken);
                _logger.LogInformation("{Channel} channel stopped", channelName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping {Channel} channel", channelName);
        }
    }
}
