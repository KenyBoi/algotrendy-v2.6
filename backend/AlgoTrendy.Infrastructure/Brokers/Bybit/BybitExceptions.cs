namespace AlgoTrendy.Infrastructure.Brokers.Bybit;

/// <summary>
/// Base exception for all Bybit broker errors
/// </summary>
public class BybitException : Exception
{
    public BybitException(string message) : base(message) { }
    public BybitException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when balance is insufficient for operation
/// </summary>
public class InsufficientBalanceException : BybitException
{
    public decimal RequiredAmount { get; }
    public decimal AvailableAmount { get; }

    public InsufficientBalanceException(decimal required, decimal available)
        : base($"Insufficient balance. Required: {required}, Available: {available}")
    {
        RequiredAmount = required;
        AvailableAmount = available;
    }
}

/// <summary>
/// Exception thrown when leverage configuration is invalid
/// </summary>
public class InvalidLeverageException : BybitException
{
    public decimal RequestedLeverage { get; }
    public decimal MaximumLeverage { get; }

    public InvalidLeverageException(decimal requested, decimal maximum)
        : base($"Invalid leverage. Requested: {requested}, Maximum allowed: {maximum}")
    {
        RequestedLeverage = requested;
        MaximumLeverage = maximum;
    }
}

/// <summary>
/// Exception thrown when order is rejected by the exchange
/// </summary>
public class OrderRejectedException : BybitException
{
    public string OrderId { get; }
    public string Reason { get; }

    public OrderRejectedException(string orderId, string reason)
        : base($"Order {orderId} rejected: {reason}")
    {
        OrderId = orderId;
        Reason = reason;
    }
}

/// <summary>
/// Exception thrown when a position is not found
/// </summary>
public class PositionNotFoundException : BybitException
{
    public string Symbol { get; }

    public PositionNotFoundException(string symbol)
        : base($"Position not found for symbol: {symbol}")
    {
        Symbol = symbol;
    }
}

/// <summary>
/// Exception thrown when API rate limit is exceeded
/// </summary>
public class RateLimitExceededException : BybitException
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfter = 60)
        : base($"Rate limit exceeded. Retry after {retryAfter} seconds")
    {
        RetryAfterSeconds = retryAfter;
    }
}

/// <summary>
/// Exception thrown when API connection fails
/// </summary>
public class ApiConnectionException : BybitException
{
    public int? HttpStatusCode { get; }

    public ApiConnectionException(string message, int? statusCode = null)
        : base(message)
    {
        HttpStatusCode = statusCode;
    }

    public ApiConnectionException(string message, Exception innerException, int? statusCode = null)
        : base(message, innerException)
    {
        HttpStatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when API response cannot be parsed
/// </summary>
public class ApiResponseParseException : BybitException
{
    public string ResponseBody { get; }

    public ApiResponseParseException(string responseBody, Exception innerException)
        : base("Failed to parse API response", innerException)
    {
        ResponseBody = responseBody;
    }
}
