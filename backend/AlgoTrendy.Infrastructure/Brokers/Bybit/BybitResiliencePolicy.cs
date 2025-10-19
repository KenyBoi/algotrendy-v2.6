using Microsoft.Extensions.Logging;

namespace AlgoTrendy.Infrastructure.Brokers.Bybit;

/// <summary>
/// Implements resilience policies for Bybit API calls (retry, rate limiting, backoff)
/// </summary>
public class BybitResiliencePolicy
{
    private readonly ILogger _logger;
    private readonly int _maxRetries;
    private readonly int _initialBackoffMs;
    private readonly double _backoffMultiplier;
    private readonly int _maxBackoffMs;
    private DateTime _lastRateLimitTime = DateTime.MinValue;
    private int _rateLimitWaitMs = 0;

    public BybitResiliencePolicy(
        ILogger logger,
        int maxRetries = 3,
        int initialBackoffMs = 100,
        double backoffMultiplier = 2.0,
        int maxBackoffMs = 5000)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _initialBackoffMs = initialBackoffMs;
        _backoffMultiplier = backoffMultiplier;
        _maxBackoffMs = maxBackoffMs;
    }

    /// <summary>
    /// Executes an async operation with automatic retry logic
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        int backoffMs = _initialBackoffMs;

        while (true)
        {
            try
            {
                // Check if we need to wait due to rate limiting
                if (_rateLimitWaitMs > 0)
                {
                    var timeSinceLimit = (DateTime.UtcNow - _lastRateLimitTime).TotalMilliseconds;
                    if (timeSinceLimit < _rateLimitWaitMs)
                    {
                        var waitTime = (int)(_rateLimitWaitMs - timeSinceLimit);
                        _logger.LogWarning(
                            "Rate limited. Waiting {WaitTimeMs}ms before retrying {OperationName}",
                            waitTime, operationName);
                        await Task.Delay(waitTime, cancellationToken);
                    }
                    _rateLimitWaitMs = 0;
                }

                // Execute the operation
                attempt++;
                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, _maxRetries + 1);

                return await operation();
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogWarning("Rate limit hit for {OperationName}. Retry after {RetryAfter}s",
                    operationName, ex.RetryAfterSeconds);
                _lastRateLimitTime = DateTime.UtcNow;
                _rateLimitWaitMs = ex.RetryAfterSeconds * 1000;

                if (attempt <= _maxRetries)
                {
                    await Task.Delay(_rateLimitWaitMs, cancellationToken);
                    continue;
                }

                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation {OperationName} was cancelled", operationName);
                throw;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt <= _maxRetries)
            {
                _logger.LogWarning(ex,
                    "Transient error during {OperationName}, attempt {Attempt}/{MaxRetries}. " +
                    "Retrying in {BackoffMs}ms",
                    operationName, attempt, _maxRetries + 1, backoffMs);

                await Task.Delay(backoffMs, cancellationToken);
                backoffMs = Math.Min(
                    (int)(backoffMs * _backoffMultiplier),
                    _maxBackoffMs);
                continue;
            }
        }
    }

    /// <summary>
    /// Executes an async operation with automatic retry logic
    /// </summary>
    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(
            async () =>
            {
                await operation();
                return true;
            },
            operationName,
            cancellationToken);
    }

    /// <summary>
    /// Determines if an error is transient and should be retried
    /// </summary>
    private bool IsTransientError(Exception ex)
    {
        // Network errors
        if (ex is HttpRequestException)
            return true;

        // Timeout errors
        if (ex is TimeoutException)
            return true;

        // Connection reset
        if (ex is IOException && ex.InnerException is SocketException)
            return true;

        // API connection errors
        if (ex is ApiConnectionException apiEx)
            return apiEx.HttpStatusCode >= 500 || apiEx.HttpStatusCode == 408 || apiEx.HttpStatusCode == 429;

        return false;
    }

    /// <summary>
    /// Records that a rate limit was hit
    /// </summary>
    public void RecordRateLimit(int retryAfterSeconds = 60)
    {
        _lastRateLimitTime = DateTime.UtcNow;
        _rateLimitWaitMs = retryAfterSeconds * 1000;
        _logger.LogWarning("Rate limit recorded. Will wait {WaitTimeS}s before next request",
            retryAfterSeconds);
    }

    /// <summary>
    /// Gets the current rate limit wait time in milliseconds
    /// </summary>
    public int GetRateLimitWaitMs()
    {
        if (_rateLimitWaitMs <= 0)
            return 0;

        var timeSinceLimit = (DateTime.UtcNow - _lastRateLimitTime).TotalMilliseconds;
        return Math.Max(0, (int)(_rateLimitWaitMs - timeSinceLimit));
    }
}

/// <summary>
/// Socket exception for detecting network-level issues
/// </summary>
internal class SocketException : Exception
{
    public SocketException(string message) : base(message) { }
}
