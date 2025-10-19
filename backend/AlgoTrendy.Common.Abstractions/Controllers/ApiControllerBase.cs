using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Common.Abstractions.Controllers;

/// <summary>
/// Base class for API controllers with standardized error handling and logging.
/// Eliminates ~80 lines of duplicate error handling code across controllers.
///
/// Note: This class references Microsoft.AspNetCore.Mvc which is only available
/// in the API project. Controllers should inherit from this when using the new pattern.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ILogger Logger;

    protected ApiControllerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an async action with standardized error handling and logging.
    /// </summary>
    /// <typeparam name="T">The return type of the action</typeparam>
    /// <param name="operation">Description of the operation for logging</param>
    /// <param name="action">The async action to execute</param>
    /// <returns>ActionResult with appropriate status codes and error messages</returns>
    protected async Task<ActionResult<T>> ExecuteAsync<T>(string operation, Func<Task<T>> action)
    {
        try
        {
            Logger.LogInformation("Starting operation: {Operation}", operation);
            var result = await action();
            Logger.LogInformation("Completed operation: {Operation}", operation);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Validation error in {Operation}: {Message}", operation, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning(ex, "Resource not found in {Operation}: {Message}", operation, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Invalid operation in {Operation}: {Message}", operation, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access in {Operation}", operation);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled error in {Operation}", operation);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Executes an async action that returns no content with standardized error handling.
    /// </summary>
    protected async Task<IActionResult> ExecuteAsync(string operation, Func<Task> action)
    {
        try
        {
            Logger.LogInformation("Starting operation: {Operation}", operation);
            await action();
            Logger.LogInformation("Completed operation: {Operation}", operation);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Validation error in {Operation}: {Message}", operation, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning(ex, "Resource not found in {Operation}: {Message}", operation, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogWarning(ex, "Invalid operation in {Operation}: {Message}", operation, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Unauthorized access in {Operation}", operation);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled error in {Operation}", operation);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Validates required string parameters.
    /// Returns BadRequest if validation fails, null if validation passes.
    /// </summary>
    protected IActionResult? ValidateRequired(params (string name, string? value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Logger.LogWarning("Validation failed: {Parameter} is required", name);
                return BadRequest(new { error = $"{name} is required" });
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that a date range is valid (start before end).
    /// Returns BadRequest if validation fails, null if validation passes.
    /// </summary>
    protected IActionResult? ValidateDateRange(DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime)
        {
            Logger.LogWarning("Validation failed: Start time {Start} must be before end time {End}", startTime, endTime);
            return BadRequest(new { error = "Start time must be before end time" });
        }

        return null;
    }

    /// <summary>
    /// Validates that a numeric value is positive.
    /// Returns BadRequest if validation fails, null if validation passes.
    /// </summary>
    protected IActionResult? ValidatePositive(string name, decimal value)
    {
        if (value <= 0)
        {
            Logger.LogWarning("Validation failed: {Parameter} must be positive, got {Value}", name, value);
            return BadRequest(new { error = $"{name} must be positive" });
        }

        return null;
    }

    /// <summary>
    /// Validates that a numeric value is within a specific range.
    /// Returns BadRequest if validation fails, null if validation passes.
    /// </summary>
    protected IActionResult? ValidateRange(string name, decimal value, decimal min, decimal max)
    {
        if (value < min || value > max)
        {
            Logger.LogWarning("Validation failed: {Parameter} must be between {Min} and {Max}, got {Value}", name, min, max, value);
            return BadRequest(new { error = $"{name} must be between {min} and {max}" });
        }

        return null;
    }

    /// <summary>
    /// Combines multiple validation results. Returns the first error found, or null if all pass.
    /// </summary>
    protected IActionResult? CombineValidations(params IActionResult?[] validations)
    {
        foreach (var validation in validations)
        {
            if (validation != null)
                return validation;
        }

        return null;
    }
}
