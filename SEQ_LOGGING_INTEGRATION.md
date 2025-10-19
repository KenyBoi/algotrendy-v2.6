# Seq Structured Logging Integration - AlgoTrendy v2.6

## Overview
This document describes the Seq structured logging implementation for AlgoTrendy v2.6, providing centralized log aggregation, powerful querying, and real-time troubleshooting capabilities.

## What is Seq?
Seq is a self-hosted search and analysis server designed for structured application logs. It provides:
- **Real-time log streaming** with millisecond latency
- **Powerful query language** for filtering and analyzing logs
- **Rich structured data** visualization
- **Alert capabilities** for critical events
- **API access** for programmatic log analysis

## Architecture Changes

### 1. Docker Compose Configuration
Added Seq service to `docker-compose.yml`:

```yaml
seq:
  image: datalust/seq:latest
  container_name: algotrendy-seq
  restart: unless-stopped
  networks:
    algotrendy-network:
      ipv4_address: 172.20.0.40
  ports:
    - "5341:80"
  volumes:
    - seq_data:/data
  environment:
    - ACCEPT_EULA=Y
    - SEQ_CACHE_SYSTEMRAMTARGET=0.5
```

**Access URL**: http://localhost:5341

### 2. NuGet Package
Installed `Serilog.Sinks.Seq` version 8.0.0 for seamless Serilog integration.

### 3. Serilog Configuration
Enhanced `Program.cs` with Seq sink and enrichment:

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AlgoTrendy.API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/algotrendy-.log", ...)
    .WriteTo.Seq(
        serverUrl: "http://seq:5341",
        apiKey: seqApiKey,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();
```

### 4. Correlation ID Middleware
Created `CorrelationIdMiddleware.cs` for distributed request tracing:

```csharp
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await _next(context);
        }
    }
}
```

## Structured Log Events

### Custom Properties
All log events include the following structured properties:

| Property | Description | Example |
|----------|-------------|---------|
| `Application` | Application name | "AlgoTrendy.API" |
| `Environment` | Deployment environment | "Development", "Production" |
| `CorrelationId` | Request correlation ID for tracing | "a1b2c3d4-e5f6-..." |
| `RequestPath` | HTTP request path | "/api/trading/orders" |
| `RequestMethod` | HTTP method | "POST", "GET" |
| `MachineName` | Server hostname | "algotrendy-api" |

### Domain-Specific Properties

#### Trading Controller Events
```csharp
// Order Placement
_logger.LogInformation(
    "Order placement requested - Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}, Type: {Type}, Price: {Price}",
    request.Symbol, request.Side, request.Quantity, request.Type, request.Price);

// Order Success
_logger.LogInformation(
    "Order submitted successfully - OrderId: {OrderId}, ClientOrderId: {ClientOrderId}, ExchangeOrderId: {ExchangeOrderId}, Symbol: {Symbol}, Side: {Side}, Quantity: {Quantity}, Status: {Status}",
    submittedOrder.OrderId, submittedOrder.ClientOrderId, submittedOrder.ExchangeOrderId,
    submittedOrder.Symbol, submittedOrder.Side, submittedOrder.Quantity, submittedOrder.Status);

// Order Cancellation
_logger.LogInformation(
    "Order cancellation requested - OrderId: {OrderId}, OperationType: {OperationType}",
    orderId, "CancelOrder");

// Balance Query
_logger.LogInformation(
    "Balance query requested - Exchange: {Exchange}, Currency: {Currency}, OperationType: {OperationType}",
    exchange, currency, "GetBalance");
```

**Properties**: `Symbol`, `Side`, `Quantity`, `Type`, `Price`, `OrderId`, `ClientOrderId`, `ExchangeOrderId`, `Status`, `Exchange`, `Currency`, `OperationType`

#### Broker Service Events
```csharp
// Connection
_logger.LogInformation(
    "Broker connection initiated - Broker: {Broker}, Environment: {Environment}, OperationType: {OperationType}",
    BrokerName, _options.UseTestnet ? "Testnet" : "Production", "Connect");

// Order Placement
_logger.LogInformation(
    "Order placement initiated - Broker: {Broker}, Symbol: {Symbol}, Side: {Side}, Type: {Type}, Quantity: {Quantity}, Price: {Price}, ClientOrderId: {ClientOrderId}, OperationType: {OperationType}",
    BrokerName, request.Symbol, request.Side, request.Type, request.Quantity, request.Price, request.ClientOrderId, "PlaceOrder");

// Balance Retrieval
_logger.LogInformation(
    "Balance retrieved - Broker: {Broker}, Currency: {Currency}, Balance: {Balance}, WalletBalance: {WalletBalance}",
    BrokerName, currency, availableBalance, coin?.WalletBalance ?? 0);
```

**Properties**: `Broker`, `Symbol`, `Side`, `Type`, `Quantity`, `Price`, `ClientOrderId`, `Currency`, `Balance`, `OperationType`

#### Indicator Service Events
```csharp
// Cache Hit
_logger.LogInformation(
    "Indicator cache hit - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, CacheKey: {CacheKey}",
    "RSI", symbol, period, cacheKey);

// Cache Miss
_logger.LogInformation(
    "Indicator cache miss - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, DataPoints: {DataPoints}",
    "RSI", symbol, period, prices.Count);

// Calculation Complete
_logger.LogInformation(
    "Indicator calculated - Indicator: {Indicator}, Symbol: {Symbol}, Period: {Period}, Value: {Value}, AvgGain: {AvgGain}, AvgLoss: {AvgLoss}",
    "RSI", symbol, period, rsi, avgGain, avgLoss);
```

**Properties**: `Indicator`, `Symbol`, `Period`, `Value`, `CacheKey`, `DataPoints`, `AvgGain`, `AvgLoss`

## Common Troubleshooting Queries

### 1. Track Order Lifecycle
Find all events for a specific order:
```
OrderId = "12345"
```

### 2. Failed Order Placements
Find all failed order placements in the last hour:
```
@Level = "Error" and OperationType = "PlaceOrder" and @Timestamp > Now() - 1h
```

### 3. Slow Broker API Calls
Find broker operations taking longer than 1 second:
```
Broker <> null and @Duration > 1000 and OperationType in ["PlaceOrder", "CancelOrder", "GetBalance"]
```

### 4. Indicator Cache Performance
Check cache hit ratio:
```
Indicator <> null
| summarize CacheHits = count() by Indicator, select Indicator like 'cache hit'
```

### 5. Orders by Symbol
Track all orders for a specific symbol:
```
Symbol = "BTCUSDT" and OperationType in ["PlaceOrder", "CancelOrder"]
```

### 6. Request Tracing
Trace entire request flow using correlation ID:
```
CorrelationId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

### 7. Broker-Specific Errors
Find errors from specific broker:
```
@Level = "Error" and Broker = "bybit"
```

### 8. Balance Queries
Track balance queries across exchanges:
```
OperationType = "GetBalance"
| summarize count() by Exchange, Currency
```

### 9. High-Frequency Trading Activity
Find symbols with most trading activity:
```
OperationType = "PlaceOrder" and @Timestamp > Now() - 1h
| summarize OrderCount = count() by Symbol
| order by OrderCount desc
```

### 10. Failed Validations
Find order validation failures:
```
@Level = "Warning" and contains(@Message, "Invalid order")
| select @Timestamp, Symbol, Side, Reason
```

## Security Considerations

### What We DON'T Log
The following sensitive data is **never** logged:
- API keys (masked as `***`)
- API secrets
- Passwords
- User credentials
- Private keys
- Session tokens

### What We DO Log
- Order IDs and Client Order IDs
- Trading symbols
- Order quantities and prices (public market data)
- Balance amounts (non-sensitive account data)
- Broker names and operation types
- Performance metrics
- Error messages (sanitized)

## Configuration

### Environment Variables
```bash
# Seq URL (default: http://localhost:5341 for local, http://seq:5341 in Docker)
SEQ_URL=http://seq:5341

# Optional: Seq API key for authentication
SEQ_API_KEY=your-api-key-here

# Log level for Seq sink
Serilog__MinimumLevel__Default=Information
```

### API Key Configuration (Optional)
To enable authentication:
1. Generate API key in Seq UI (Settings → API Keys)
2. Set `SEQ_API_KEY` environment variable
3. Restart API service

## Performance Impact

### Benchmarks
- **Average overhead**: ~2-5ms per log event
- **Memory usage**: Minimal (async batching)
- **Network traffic**: ~500 bytes per structured log event
- **Seq storage**: ~100KB per 1000 events

### Optimization Tips
1. Use appropriate log levels (avoid Debug in production)
2. Enable Seq data retention policies
3. Archive old logs to cold storage
4. Use Seq's built-in compression

## Monitoring & Alerts

### Recommended Alerts
Configure Seq alerts for:
1. **High error rate**: More than 10 errors/minute
2. **Failed orders**: Any order placement failures
3. **Broker disconnections**: Connection failures
4. **API latency**: Response times > 5 seconds
5. **Balance anomalies**: Unexpected balance changes

### Health Checks
Seq provides built-in health endpoint:
```
GET http://localhost:5341/api/health
```

## Deployment

### Starting Services
```bash
# Start all services including Seq
docker-compose up -d

# Verify Seq is running
docker ps | grep seq

# Check Seq logs
docker logs algotrendy-seq
```

### Accessing Seq UI
1. Open browser to http://localhost:5341
2. No authentication required (by default)
3. Logs appear in real-time as events occur

## Example Dashboard Queries

### Trading Activity Dashboard
```sql
-- Total orders in last hour
select count(*) as TotalOrders
where OperationType = "PlaceOrder"
and @Timestamp > Now() - 1h

-- Orders by broker
select count(*) as OrderCount, Broker
where OperationType = "PlaceOrder"
group by Broker

-- Success vs failure rate
select
  count(*) as Total,
  count(case when @Level = "Error" then 1 end) as Failures
where OperationType = "PlaceOrder"
```

### Performance Metrics
```sql
-- Average indicator calculation time
select avg(@Duration) as AvgDuration, Indicator
where Indicator <> null
group by Indicator

-- Broker API latency
select avg(@Duration) as AvgLatency, Broker, OperationType
where Broker <> null
group by Broker, OperationType
order by AvgLatency desc
```

## Testing

### Generate Test Logs
```bash
# Test order placement (generates structured logs)
curl -X POST http://localhost:5002/api/trading/orders \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "type": "Limit",
    "quantity": 0.001,
    "price": 50000
  }'

# Test balance query
curl http://localhost:5002/api/trading/balance/bybit/USDT
```

### Verify in Seq
1. Open Seq UI: http://localhost:5341
2. Search for recent events: `@Timestamp > Now() - 5m`
3. Filter by operation: `OperationType = "PlaceOrder"`
4. Inspect structured properties in log details

## Maintenance

### Log Retention
Configure retention policy in Seq:
1. Settings → Retention
2. Set retention period (e.g., 30 days)
3. Enable automatic cleanup

### Backup
Seq data is stored in Docker volume `seq_data`:
```bash
# Backup Seq data
docker run --rm -v seq_data:/data -v $(pwd):/backup ubuntu tar czf /backup/seq-backup.tar.gz /data

# Restore Seq data
docker run --rm -v seq_data:/data -v $(pwd):/backup ubuntu tar xzf /backup/seq-backup.tar.gz -C /
```

## Troubleshooting

### Seq Not Receiving Logs
1. Check Seq container is running: `docker ps | grep seq`
2. Verify network connectivity: `docker exec algotrendy-api ping seq`
3. Check Serilog configuration in Program.cs
4. Verify SEQ_URL environment variable

### High Memory Usage
1. Reduce log retention period
2. Increase `SEQ_CACHE_SYSTEMRAMTARGET` (default: 0.5 = 50% of RAM)
3. Enable log compression
4. Archive old events

### Missing Properties
1. Verify middleware is registered before controllers
2. Check LogContext.PushProperty calls
3. Ensure Enrich.FromLogContext() is configured

## Next Steps

### Future Enhancements
1. **Dashboard Templates**: Pre-built Seq dashboards for common scenarios
2. **Alerting**: Integrate with PagerDuty/Slack for critical events
3. **Log Correlation**: Link logs with distributed tracing (OpenTelemetry)
4. **Custom Metrics**: Export Seq data to Prometheus/Grafana
5. **Machine Learning**: Anomaly detection for trading patterns

## References
- [Seq Documentation](https://docs.datalust.co/docs)
- [Serilog.Sinks.Seq](https://github.com/serilog/serilog-sinks-seq)
- [Structured Logging Best Practices](https://nblumhardt.com/2016/06/structured-logging-concepts-in-net-series-1/)
