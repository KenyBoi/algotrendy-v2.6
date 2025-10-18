using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AlgoTrendy.DataChannels.Channels;

/// <summary>
/// Binance WebSocket market data channel
/// Connects to Binance Stream API and receives real-time 1-minute kline (candlestick) data
/// </summary>
public class BinanceMarketDataChannel : IMarketDataChannel
{
    private const string WebSocketUrl = "wss://stream.binance.com:9443/ws";
    private readonly IMarketDataRepository _marketDataRepository;
    private readonly ILogger<BinanceMarketDataChannel> _logger;
    private readonly List<string> _subscribedSymbols = new();
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;

    public string ExchangeName => "binance";
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    public IReadOnlyList<string> SubscribedSymbols => _subscribedSymbols.AsReadOnly();
    public DateTime? LastDataReceivedAt { get; private set; }
    public long TotalMessagesReceived { get; private set; }

    public BinanceMarketDataChannel(
        IMarketDataRepository marketDataRepository,
        ILogger<BinanceMarketDataChannel> logger)
    {
        _marketDataRepository = marketDataRepository ?? throw new ArgumentNullException(nameof(marketDataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogWarning("Binance channel is already connected");
            return;
        }

        _logger.LogInformation("Starting Binance market data channel");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _webSocket = new ClientWebSocket();

        try
        {
            await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);
            _logger.LogInformation("Connected to Binance WebSocket: {Url}", WebSocketUrl);

            // Start receiving messages
            _receiveTask = ReceiveMessagesAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Binance WebSocket");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Binance market data channel");

        _cancellationTokenSource?.Cancel();

        if (_webSocket?.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", cancellationToken);
        }

        if (_receiveTask != null)
        {
            await _receiveTask;
        }

        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();

        _logger.LogInformation("Binance market data channel stopped");
    }

    public async Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("Subscribing to {Count} symbols on Binance", symbolList.Count);

        // Binance requires lowercase symbols
        var streams = symbolList.Select(s => $"{s.ToLowerInvariant()}@kline_1m").ToArray();

        var subscribeMessage = new
        {
            method = "SUBSCRIBE",
            @params = streams,
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var json = JsonSerializer.Serialize(subscribeMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);

        _subscribedSymbols.AddRange(symbolList);

        _logger.LogInformation(
            "Subscribed to symbols: {Symbols}",
            string.Join(", ", symbolList));
    }

    public async Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Channel is not connected");
        }

        var symbolList = symbols.ToList();
        _logger.LogInformation("Unsubscribing from {Count} symbols on Binance", symbolList.Count);

        var streams = symbolList.Select(s => $"{s.ToLowerInvariant()}@kline_1m").ToArray();

        var unsubscribeMessage = new
        {
            method = "UNSUBSCRIBE",
            @params = streams,
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var json = JsonSerializer.Serialize(unsubscribeMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        await _webSocket!.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);

        foreach (var symbol in symbolList)
        {
            _subscribedSymbols.Remove(symbol);
        }

        _logger.LogInformation("Unsubscribed from symbols: {Symbols}", string.Join(", ", symbolList));
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 16]; // 16KB buffer

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var result = await _webSocket!.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by server");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                await ProcessMessageAsync(message, cancellationToken);

                TotalMessagesReceived++;
                LastDataReceivedAt = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Receive operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving messages from Binance");
        }
    }

    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // Check if this is a kline event
            if (root.TryGetProperty("e", out var eventType) && eventType.GetString() == "kline")
            {
                var kline = root.GetProperty("k");
                var symbol = root.GetProperty("s").GetString()!;

                // Only save completed candles
                if (kline.GetProperty("x").GetBoolean())
                {
                    var marketData = new MarketData
                    {
                        Symbol = symbol,
                        Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(
                            kline.GetProperty("t").GetInt64()).UtcDateTime,
                        Open = decimal.Parse(kline.GetProperty("o").GetString()!),
                        High = decimal.Parse(kline.GetProperty("h").GetString()!),
                        Low = decimal.Parse(kline.GetProperty("l").GetString()!),
                        Close = decimal.Parse(kline.GetProperty("c").GetString()!),
                        Volume = decimal.Parse(kline.GetProperty("v").GetString()!),
                        QuoteVolume = decimal.Parse(kline.GetProperty("q").GetString()!),
                        TradesCount = kline.GetProperty("n").GetInt64(),
                        Source = ExchangeName
                    };

                    await _marketDataRepository.InsertAsync(marketData, cancellationToken);

                    _logger.LogDebug(
                        "Saved market data: {Symbol} @ {Timestamp}, Close: {Close}",
                        marketData.Symbol,
                        marketData.Timestamp,
                        marketData.Close);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);
        }
    }
}
