using AlgoTrendy.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Webhook management and testing
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Test webhook connectivity
    /// </summary>
    /// <param name="request">Webhook test request</param>
    [HttpPost("test")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestWebhook([FromBody] WebhookTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "URL is required" });
        }

        try
        {
            var testPayload = new
            {
                eventType = "test",
                timestamp = DateTime.UtcNow,
                message = request.Message ?? "Test webhook from AlgoTrendy",
                data = request.Data
            };

            await _webhookService.SendWebhookAsync(request.Url, testPayload);

            return Ok(new
            {
                message = "Webhook test sent successfully",
                url = request.Url,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook test failed for {Url}", request.Url);
            return BadRequest(new
            {
                error = "Webhook test failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Receive TradingView webhook alerts
    /// </summary>
    /// <param name="alert">TradingView alert payload</param>
    [HttpPost("tradingview")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TradingViewWebhook([FromBody] TradingViewAlert alert)
    {
        _logger.LogInformation(
            "Received TradingView alert: {Symbol} {Action} at {Price}",
            alert.Symbol, alert.Action, alert.Price);

        // Process TradingView alert
        // This could trigger order placement, position management, etc.

        // For now, just log and acknowledge
        return Ok(new
        {
            message = "Alert received",
            symbol = alert.Symbol,
            action = alert.Action,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Webhook receiver endpoint for external integrations
    /// </summary>
    [HttpPost("receive")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ReceiveWebhook([FromBody] object payload)
    {
        _logger.LogInformation("Received webhook: {Payload}", payload);

        // Process incoming webhook
        // Implement your custom logic here

        return Ok(new
        {
            message = "Webhook received",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Webhook test request model
/// </summary>
public class WebhookTestRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Message { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// TradingView alert model
/// </summary>
public class TradingViewAlert
{
    public string Symbol { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // buy, sell, close_long, close_short
    public decimal Price { get; set; }
    public string? Strategy { get; set; }
    public string? Interval { get; set; }
    public decimal? Quantity { get; set; }
    public string? Message { get; set; }
}
