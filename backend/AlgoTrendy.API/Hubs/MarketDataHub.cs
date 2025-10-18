using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace AlgoTrendy.API.Hubs;

/// <summary>
/// SignalR hub for real-time market data streaming
/// </summary>
public class MarketDataHub : Hub
{
    private readonly ILogger<MarketDataHub> _logger;

    public MarketDataHub(ILogger<MarketDataHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId,
            exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to market data updates for specific symbols
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols to subscribe to</param>
    public async Task SubscribeToSymbols(string symbols)
    {
        var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .ToList();

        foreach (var symbol in symbolList)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"market-{symbol}");
            _logger.LogInformation(
                "Client {ConnectionId} subscribed to {Symbol}",
                Context.ConnectionId,
                symbol);
        }

        await Clients.Caller.SendAsync("Subscribed", symbolList);
    }

    /// <summary>
    /// Unsubscribe from market data updates for specific symbols
    /// </summary>
    /// <param name="symbols">Comma-separated list of symbols to unsubscribe from</param>
    public async Task UnsubscribeFromSymbols(string symbols)
    {
        var symbolList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .ToList();

        foreach (var symbol in symbolList)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"market-{symbol}");
            _logger.LogInformation(
                "Client {ConnectionId} unsubscribed from {Symbol}",
                Context.ConnectionId,
                symbol);
        }

        await Clients.Caller.SendAsync("Unsubscribed", symbolList);
    }

    /// <summary>
    /// Get list of currently subscribed symbols for this client
    /// </summary>
    public Task<string> GetSubscribedSymbols()
    {
        // Note: SignalR doesn't provide a built-in way to get group memberships
        // In production, you'd track this in a distributed cache (Redis)
        return Task.FromResult("Check your client-side subscription state");
    }

    /// <summary>
    /// Ping to test connection (useful for keepalive)
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }
}
