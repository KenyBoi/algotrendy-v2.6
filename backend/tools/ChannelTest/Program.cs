using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Infrastructure.Repositories;
using AlgoTrendy.DataChannels.Channels.REST;

Console.WriteLine("=== AlgoTrendy v2.6 Market Data Channels Test ===\n");

// Setup DI container
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add HttpClient factory
services.AddHttpClient();

// Mock repository (we won't save to DB in this test)
var questDbConnectionString = "Host=localhost;Port=8812;Database=qdb;Username=admin;Password=quest";
services.AddScoped<IMarketDataRepository>(sp =>
    new MarketDataRepository(questDbConnectionString));

// Register all channels
services.AddScoped<BinanceRestChannel>();
services.AddScoped<OKXRestChannel>();
services.AddScoped<CoinbaseRestChannel>();
services.AddScoped<KrakenRestChannel>();

var serviceProvider = services.BuildServiceProvider();

// Test each channel
var results = new Dictionary<string, (bool success, int recordCount, string error)>();

Console.WriteLine("Testing channels...\n");

// Test Binance
results["Binance"] = await TestChannelAsync<BinanceRestChannel>(serviceProvider, "Binance");

// Test OKX
results["OKX"] = await TestChannelAsync<OKXRestChannel>(serviceProvider, "OKX");

// Test Coinbase
results["Coinbase"] = await TestChannelAsync<CoinbaseRestChannel>(serviceProvider, "Coinbase");

// Test Kraken
results["Kraken"] = await TestChannelAsync<KrakenRestChannel>(serviceProvider, "Kraken");

// Summary
Console.WriteLine("\n=== Test Summary ===");
Console.WriteLine($"{"Channel",-15} {"Status",-10} {"Records",-10} {"Error",-50}");
Console.WriteLine(new string('-', 85));

var totalRecords = 0;
var successCount = 0;

foreach (var (channel, (success, recordCount, error)) in results)
{
    var status = success ? "PASS" : "FAIL";
    Console.WriteLine($"{channel,-15} {status,-10} {recordCount,-10} {error,-50}");

    if (success)
    {
        totalRecords += recordCount;
        successCount++;
    }
}

Console.WriteLine(new string('-', 85));
Console.WriteLine($"\nTotal: {successCount}/{results.Count} channels successful");
Console.WriteLine($"Total records fetched: {totalRecords}");

if (successCount == results.Count)
{
    Console.WriteLine("\nAll channels operational!");
    Environment.Exit(0);
}
else
{
    Console.WriteLine($"\n{results.Count - successCount} channel(s) failed");
    Environment.Exit(1);
}

static async Task<(bool success, int recordCount, string error)> TestChannelAsync<TChannel>(
    ServiceProvider serviceProvider,
    string channelName)
    where TChannel : class, IMarketDataChannel
{
    Console.WriteLine($"[{channelName}] Starting test...");

    try
    {
        using var scope = serviceProvider.CreateScope();
        var channel = scope.ServiceProvider.GetRequiredService<TChannel>();

        // Start the channel
        await channel.StartAsync();

        if (!channel.IsConnected)
        {
            return (false, 0, "Failed to connect");
        }

        Console.WriteLine($"[{channelName}] Connected successfully");

        // Fetch data using reflection (since FetchDataAsync is not in interface)
        var method = channel.GetType().GetMethod("FetchDataAsync");
        if (method == null)
        {
            return (false, 0, "FetchDataAsync method not found");
        }

        // Invoke with default parameters (fetch just 10 records for testing)
        var task = method.Invoke(channel, new object?[]
        {
            null,  // symbols (null = use defaults)
            "1m",  // interval
            10,    // limit (small for testing)
            CancellationToken.None
        });

        if (task is not Task<List<AlgoTrendy.Core.Models.MarketData>> dataTask)
        {
            return (false, 0, "Unexpected return type from FetchDataAsync");
        }

        var data = await dataTask;

        Console.WriteLine($"[{channelName}] Fetched {data.Count} records");

        // Display sample data
        if (data.Any())
        {
            var sample = data.First();
            Console.WriteLine($"[{channelName}] Sample: {sample.Symbol} @ {sample.Timestamp:yyyy-MM-dd HH:mm:ss} - O:{sample.Open} H:{sample.High} L:{sample.Low} C:{sample.Close} V:{sample.Volume}");
        }

        // Stop the channel
        await channel.StopAsync();

        return (true, data.Count, string.Empty);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{channelName}] Error: {ex.Message}");
        return (false, 0, ex.Message.Length > 50 ? ex.Message[..47] + "..." : ex.Message);
    }
}
