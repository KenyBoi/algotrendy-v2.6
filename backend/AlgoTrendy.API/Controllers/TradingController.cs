using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Trading operations controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TradingController : ControllerBase
{
    private readonly ITradingEngine _tradingEngine;
    private readonly ILogger<TradingController> _logger;

    public TradingController(
        ITradingEngine tradingEngine,
        ILogger<TradingController> logger)
    {
        _tradingEngine = tradingEngine ?? throw new ArgumentNullException(nameof(tradingEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Places a new order
    /// </summary>
    /// <param name="request">Order request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Submitted order with exchange order ID</returns>
    /// <response code="200">Order submitted successfully</response>
    /// <response code="400">Invalid order request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Order>> PlaceOrderAsync(
        [FromBody] OrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Order placement requested - Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}, Type: {Type}, Price: {Price}",
                request.Symbol, request.Side, request.Quantity, request.Type, request.Price);

            // Create Order from request (ClientOrderId auto-generated if not provided)
            var order = OrderFactory.FromRequest(request);

            // Submit to trading engine (includes idempotency check)
            var submittedOrder = await _tradingEngine.SubmitOrderAsync(order, cancellationToken);

            _logger.LogInformation(
                "Order submitted successfully - OrderId: {OrderId}, ClientOrderId: {ClientOrderId}, ExchangeOrderId: {ExchangeOrderId}, Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}, Status: {Status}",
                submittedOrder.OrderId, submittedOrder.ClientOrderId, submittedOrder.ExchangeOrderId,
                submittedOrder.Symbol, submittedOrder.Side, submittedOrder.Quantity, submittedOrder.Status);

            return Ok(submittedOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Invalid order request - Symbol: {Symbol}, Side: {Side}, Reason: {Reason}",
                request.Symbol, request.Side, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to place order - Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}",
                request.Symbol, request.Side, request.Quantity);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels an active order
    /// </summary>
    /// <param name="orderId">Order ID to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled order</returns>
    /// <response code="200">Order cancelled successfully</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("orders/{orderId}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Order>> CancelOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Order cancellation requested - OrderId: {OrderId}, OperationType: {OperationType}",
                orderId, "CancelOrder");

            var cancelledOrder = await _tradingEngine.CancelOrderAsync(orderId, cancellationToken);

            _logger.LogInformation(
                "Order cancelled successfully - OrderId: {OrderId}, Symbol: {Symbol}, Status: {Status}",
                orderId, cancelledOrder.Symbol, cancelledOrder.Status);

            return Ok(cancelledOrder);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex,
                "Order cancellation failed - OrderId: {OrderId}, Reason: {Reason}",
                orderId, "OrderNotFound");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to cancel order - OrderId: {OrderId}",
                orderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the current status of an order
    /// </summary>
    /// <param name="orderId">Order ID to query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order with current status</returns>
    /// <response code="200">Order status retrieved successfully</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("orders/{orderId}")]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Order>> GetOrderStatusAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting status for order {OrderId}", orderId);

            var order = await _tradingEngine.GetOrderStatusAsync(orderId, cancellationToken);

            return Ok(order);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Order {OrderId} not found", orderId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order status for {OrderId}", orderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validates an order before submission
    /// </summary>
    /// <param name="request">Order request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    /// <response code="200">Order is valid</response>
    /// <response code="400">Order validation failed</response>
    [HttpPost("orders/validate")]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidationResult>> ValidateOrderAsync(
        [FromBody] OrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = OrderFactory.FromRequest(request);
            var (isValid, errorMessage) = await _tradingEngine.ValidateOrderAsync(order, cancellationToken);

            if (isValid)
            {
                return Ok(new ValidationResult { IsValid = true, Message = "Order is valid" });
            }

            return BadRequest(new ValidationResult { IsValid = false, Message = errorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order");
            return StatusCode(500, new ValidationResult
            {
                IsValid = false,
                Message = "Internal server error during validation"
            });
        }
    }

    /// <summary>
    /// Gets account balance for a specific currency
    /// </summary>
    /// <param name="exchange">Exchange name (e.g., "binance")</param>
    /// <param name="currency">Currency symbol (e.g., "USDT")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available balance</returns>
    [HttpGet("balance/{exchange}/{currency}")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceResponse>> GetBalanceAsync(
        string exchange,
        string currency,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Balance query requested - Exchange: {Exchange}, Currency: {Currency}, OperationType: {OperationType}",
                exchange, currency, "GetBalance");

            var balance = await _tradingEngine.GetBalanceAsync(exchange, currency, cancellationToken);

            _logger.LogInformation(
                "Balance retrieved - Exchange: {Exchange}, Currency: {Currency}, Balance: {Balance}",
                exchange, currency, balance);

            return Ok(new BalanceResponse
            {
                Exchange = exchange,
                Currency = currency,
                Balance = balance,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get balance - Exchange: {Exchange}, Currency: {Currency}",
                exchange, currency);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Order validation result
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the order passed validation
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation message describing the result or error details
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Balance query response
/// </summary>
public class BalanceResponse
{
    /// <summary>
    /// Exchange name (e.g., "binance", "okx", "coinbase")
    /// </summary>
    public required string Exchange { get; set; }

    /// <summary>
    /// Currency symbol (e.g., "USDT", "BTC", "ETH")
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Available balance amount
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Timestamp when balance was retrieved (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }
}
