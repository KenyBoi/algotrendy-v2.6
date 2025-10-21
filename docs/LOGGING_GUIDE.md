# AlgoTrendy - Structured Logging Guide

This guide explains how to implement and use structured logging in AlgoTrendy using Serilog.

## Table of Contents

- [Overview](#overview)
- [Configuration](#configuration)
- [Logging Levels](#logging-levels)
- [Logging Best Practices](#logging-best-practices)
- [Code Examples](#code-examples)
- [Log Sinks](#log-sinks)
- [Querying Logs](#querying-logs)
- [Performance Considerations](#performance-considerations)

---

## Overview

AlgoTrendy uses **Serilog** for structured logging with multiple sinks:

- **Console** - Development and debugging
- **File** - Local log files with rotation
- **Seq** - Centralized log aggregation and querying

### Why Structured Logging?

✅ **Searchable** - Query logs by properties  
✅ **Contextual** - Rich metadata attached to each log  
✅ **Type-safe** - Strongly-typed log properties  
✅ **Performant** - Minimal overhead with async sinks  

---

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning",
        "AlgoTrendy": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/algotrendy-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:5341",
          "apiKey": "${SEQ_API_KEY}",
          "compact": true
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "AlgoTrendy",
      "Environment": "Production"
    }
  }
}
```

### Program.cs Setup

```csharp
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AlgoTrendy")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting AlgoTrendy API");
    
    var app = builder.Build();
    
    // Configure middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress);
        };
    });
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

---

## Logging Levels

### Level Hierarchy

```
Verbose < Debug < Information < Warning < Error < Fatal
```

### When to Use Each Level

#### Verbose

**Use for:** Detailed tracing information

```csharp
_logger.LogTrace("Entering {MethodName} with parameters: {Parameters}", 
    nameof(ProcessOrder), 
    new { symbol, quantity, price });
```

**Examples:**
- Method entry/exit
- Variable values at specific points
- Loop iterations

#### Debug

**Use for:** Debugging information useful during development

```csharp
_logger.LogDebug("Order validation passed for {Symbol}: {Quantity} @ {Price}", 
    symbol, quantity, price);
```

**Examples:**
- Intermediate calculation results
- State changes
- Cache hits/misses

#### Information

**Use for:** General informational messages

```csharp
_logger.LogInformation("Order {OrderId} placed successfully for {Symbol}", 
    orderId, symbol);
```

**Examples:**
- Successful operations
- State transitions
- Business events

#### Warning

**Use for:** Unexpected but recoverable situations

```csharp
_logger.LogWarning("Market data delayed for {Symbol}, using cached data from {CacheAge} seconds ago", 
    symbol, cacheAge);
```

**Examples:**
- Degraded performance
- Deprecated API usage
- Retry attempts
- Business rule violations

#### Error

**Use for:** Errors that prevented operation completion

```csharp
_logger.LogError(ex, "Failed to place order {OrderId} for {Symbol}: {ErrorMessage}", 
    orderId, symbol, ex.Message);
```

**Examples:**
- Caught exceptions
- Failed operations
- Integration errors

#### Fatal

**Use for:** Catastrophic failures requiring immediate attention

```csharp
_logger.LogCritical(ex, "Database connection lost. Application cannot continue.");
```

**Examples:**
- Database unavailable
- Critical system failures
- Unrecoverable errors

---

## Logging Best Practices

### 1. Use Structured Logging

❌ **Don't:**
```csharp
_logger.LogInformation($"Order {orderId} placed for {symbol} at {price}");
```

✅ **Do:**
```csharp
_logger.LogInformation("Order {OrderId} placed for {Symbol} at {Price}", 
    orderId, symbol, price);
```

**Why:** Structured properties can be queried in Seq.

### 2. Include Contextual Information

```csharp
_logger.LogInformation("Trade executed: {Symbol} {Side} {Quantity} @ {Price} | Broker: {Broker}, Account: {Account}", 
    symbol, side, quantity, price, broker, accountId);
```

### 3. Use Meaningful Property Names

❌ **Don't:**
```csharp
_logger.LogInformation("Processing {Item}", orderId);
```

✅ **Do:**
```csharp
_logger.LogInformation("Processing order {OrderId}", orderId);
```

### 4. Log Exceptions Properly

❌ **Don't:**
```csharp
_logger.LogError($"Error: {ex.Message}");
```

✅ **Do:**
```csharp
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);
```

### 5. Use Log Context for Common Properties

```csharp
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("SessionId", sessionId))
{
    _logger.LogInformation("User action: {Action}", "PlaceOrder");
    // All logs in this scope will include UserId and SessionId
}
```

### 6. Don't Log Sensitive Information

❌ **Never log:**
- API keys/secrets
- Passwords
- Full credit card numbers
- Personal identification numbers

✅ **Do log masked versions:**
```csharp
_logger.LogInformation("API request to {Broker} with key {ApiKey}", 
    broker, 
    MaskApiKey(apiKey)); // e.g., "abc***xyz"
```

### 7. Performance-Conscious Logging

Use guards for expensive operations:

```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    _logger.LogDebug("Complex object: {ComplexData}", 
        SerializeComplexObject(data));
}
```

---

## Code Examples

### Service Layer Logging

```csharp
public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;
    private readonly IBroker _broker;

    public OrderService(
        ILogger<OrderService> logger,
        IBroker broker)
    {
        _logger = logger;
        _broker = broker;
    }

    public async Task<OrderResult> PlaceMarketOrderAsync(OrderRequest request)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderType"] = "Market",
            ["Symbol"] = request.Symbol,
            ["Broker"] = request.BrokerId
        }))
        {
            _logger.LogInformation("Placing market order: {Side} {Quantity} {Symbol}",
                request.Side, request.Quantity, request.Symbol);

            try
            {
                // Validate
                ValidateOrder(request);
                _logger.LogDebug("Order validation passed");

                // Execute
                var result = await _broker.PlaceOrderAsync(request);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Order placed successfully: {OrderId} filled {FilledQty} @ {AvgPrice}",
                        result.OrderId, result.FilledQuantity, result.AveragePrice);
                }
                else
                {
                    _logger.LogWarning(
                        "Order placement failed: {Reason}",
                        result.ErrorMessage);
                }

                return result;
            }
            catch (BrokerException ex)
            {
                _logger.LogError(ex,
                    "Broker error placing order: {ErrorCode} - {ErrorMessage}",
                    ex.ErrorCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error placing market order");
                throw;
            }
        }
    }

    private void ValidateOrder(OrderRequest request)
    {
        if (request.Quantity <= 0)
        {
            _logger.LogWarning(
                "Invalid order quantity: {Quantity}",
                request.Quantity);
            throw new ValidationException("Quantity must be positive");
        }
    }
}
```

### Controller Logging

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderService _orderService;

    public OrdersController(
        ILogger<OrdersController> logger,
        IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    [HttpPost("market")]
    public async Task<ActionResult<OrderResponse>> PlaceMarketOrder(
        [FromBody] MarketOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        using (_logger.BeginScope("UserId: {UserId}", userId))
        {
            _logger.LogInformation(
                "Market order request: {Symbol} {Side} {Quantity}",
                request.Symbol, request.Side, request.Quantity);

            try
            {
                var result = await _orderService.PlaceMarketOrderAsync(request);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Order validation failed");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process market order");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
```

### Background Service Logging

```csharp
public class MarketDataFetcherService : BackgroundService
{
    private readonly ILogger<MarketDataFetcherService> _logger;
    private readonly IMarketDataService _marketDataService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market data fetcher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _marketDataService.FetchAndStoreDataAsync();

                _logger.LogInformation(
                    "Market data fetch completed in {ElapsedMs}ms",
                    sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching market data after {ElapsedMs}ms",
                    sw.ElapsedMilliseconds);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Market data fetcher stopped");
    }
}
```

---

## Log Sinks

### Console Sink

**Purpose:** Development and debugging

**Configuration:**
```json
{
  "Name": "Console",
  "Args": {
    "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
  }
}
```

### File Sink

**Purpose:** Local log persistence

**Features:**
- Daily rolling
- Size-based rolling
- Retention policy (30 days)
- Compression (optional)

**Configuration:**
```json
{
  "Name": "File",
  "Args": {
    "path": "logs/algotrendy-.log",
    "rollingInterval": "Day",
    "retainedFileCountLimit": 30,
    "fileSizeLimitBytes": 104857600,
    "rollOnFileSizeLimit": true,
    "shared": true
  }
}
```

### Seq Sink

**Purpose:** Centralized log aggregation and analysis

**Features:**
- Full-text search
- Structured querying
- Dashboards
- Alerts

**Configuration:**
```json
{
  "Name": "Seq",
  "Args": {
    "serverUrl": "http://seq:5341",
    "apiKey": "${SEQ_API_KEY}",
    "compact": true,
    "bufferBaseFilename": "./logs/seq-buffer"
  }
}
```

---

## Querying Logs

### Seq Query Examples

**Find errors in the last hour:**
```
level = 'Error' and @Timestamp > Now()-1h
```

**Find orders for a specific symbol:**
```
Symbol = 'BTCUSDT' and @Message like '%order%'
```

**Find slow API requests:**
```
Elapsed > 1000 and RequestPath like '/api/%'
```

**Group errors by exception type:**
```
level = 'Error' 
| group by @Exception 
| select count(*) as Count
```

**Find failed orders:**
```
@Message like '%order%failed%' 
| select @Timestamp, OrderId, Symbol, ErrorMessage
```

---

## Performance Considerations

### 1. Async Logging

Configure async sinks for better performance:

```csharp
.WriteTo.Async(a => a.File("logs/app.log"))
.WriteTo.Async(a => a.Seq("http://seq:5341"))
```

### 2. Filtering

Filter logs before they hit expensive sinks:

```csharp
.WriteTo.Logger(lc => lc
    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Warning)
    .WriteTo.Seq("http://seq:5341"))
```

### 3. Sampling

Log only a sample of high-volume events:

```csharp
.WriteTo.Logger(lc => lc
    .Filter.With(new SamplingEnricher(sampleRate: 0.1)) // 10% sample
    .WriteTo.File("logs/sample.log"))
```

### 4. Buffering

Use buffered sinks for remote logging:

```csharp
.WriteTo.Seq(
    serverUrl: "http://seq:5341",
    bufferBaseFilename: "./logs/seq-buffer",
    retainedInvalidPayloadsLimitBytes: 100_000_000)
```

---

## Monitoring & Alerts

### Critical Alerts

Set up Seq alerts for:

1. **Error Rate Spike**
   ```
   count(level = 'Error') > 10 within 5m
   ```

2. **Failed Orders**
   ```
   @Message like '%order%failed%' 
   and Symbol in ['BTCUSDT', 'ETHUSDT']
   ```

3. **Slow Performance**
   ```
   avg(Elapsed) > 500 within 1m
   ```

4. **Database Errors**
   ```
   @Exception like '%database%' or @Exception like '%connection%'
   ```

---

## Troubleshooting

### Logs Not Appearing in Seq

```bash
# Check Seq is running
docker ps | grep seq

# Check Seq logs
docker logs algotrendy-seq

# Test connection
curl http://localhost:5341/api

# Verify API key
echo $SEQ_API_KEY
```

### Log Files Growing Too Large

Adjust retention in appsettings.json:

```json
{
  "retainedFileCountLimit": 7,  // Keep only 7 days
  "fileSizeLimitBytes": 52428800  // 50 MB per file
}
```

### Performance Issues

```csharp
// Use minimum level overrides
"MinimumLevel": {
  "Default": "Warning",  // Less verbose in production
  "Override": {
    "AlgoTrendy.Orders": "Information"  // Specific namespaces
  }
}
```

---

## Resources

- [Serilog Documentation](https://github.com/serilog/serilog/wiki)
- [Seq Documentation](https://docs.datalust.co/docs)
- [Best Practices](https://benfoster.io/blog/serilog-best-practices/)

---

*Last Updated: October 21, 2025*
