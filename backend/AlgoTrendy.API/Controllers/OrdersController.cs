using AlgoTrendy.Core.Interfaces;
using AlgoTrendy.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrendy.API.Controllers;

/// <summary>
/// Orders management controller
/// Provides endpoints for querying and managing orders
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderRepository _orderRepository;

    public OrdersController(
        ILogger<OrdersController> logger,
        IOrderRepository orderRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    /// <summary>
    /// Gets all orders
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of orders</returns>
    /// <response code="200">Returns the list of orders</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders(
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving all active orders");

            var orders = await _orderRepository.GetActiveOrdersAsync(cancellationToken);

            _logger.LogInformation("Retrieved {Count} orders", orders.Count());

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve orders");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
