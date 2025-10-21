using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.API.Services;

/// <summary>
/// Service for sending webhook notifications
/// </summary>
public interface IWebhookService
{
    Task SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default);
    Task SendOrderNotificationAsync(string orderId, string status, CancellationToken cancellationToken = default);
    Task SendPriceAlertAsync(string symbol, decimal price, string condition, CancellationToken cancellationToken = default);
    Task SendSystemEventAsync(string eventType, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of webhook notification service
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;
    private readonly IConfiguration _configuration;

    public WebhookService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a generic webhook notification
    /// </summary>
    public async Task SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Webhook URL is null or empty, skipping notification");
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending webhook to {Url}", url);

            var response = await httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to {Url}", url);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook failed with status {StatusCode}: {Url}",
                    response.StatusCode, url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to {Url}", url);
        }
    }

    /// <summary>
    /// Send order status notification
    /// </summary>
    public async Task SendOrderNotificationAsync(
        string orderId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var webhookUrl = _configuration["Webhooks:OrderNotifications"];

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        var payload = new
        {
            eventType = "order_status_changed",
            timestamp = DateTime.UtcNow,
            data = new
            {
                orderId,
                status,
                source = "AlgoTrendy.API"
            }
        };

        await SendWebhookAsync(webhookUrl, payload, cancellationToken);
    }

    /// <summary>
    /// Send price alert notification
    /// </summary>
    public async Task SendPriceAlertAsync(
        string symbol,
        decimal price,
        string condition,
        CancellationToken cancellationToken = default)
    {
        var webhookUrl = _configuration["Webhooks:PriceAlerts"];

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        var payload = new
        {
            eventType = "price_alert",
            timestamp = DateTime.UtcNow,
            data = new
            {
                symbol,
                price,
                condition,
                source = "AlgoTrendy.API"
            }
        };

        await SendWebhookAsync(webhookUrl, payload, cancellationToken);
    }

    /// <summary>
    /// Send system event notification
    /// </summary>
    public async Task SendSystemEventAsync(
        string eventType,
        string message,
        CancellationToken cancellationToken = default)
    {
        var webhookUrl = _configuration["Webhooks:SystemEvents"];

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        var payload = new
        {
            eventType = "system_event",
            timestamp = DateTime.UtcNow,
            data = new
            {
                type = eventType,
                message,
                source = "AlgoTrendy.API"
            }
        };

        await SendWebhookAsync(webhookUrl, payload, cancellationToken);
    }
}
