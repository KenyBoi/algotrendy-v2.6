# AlgoTrendy Latency Testing & Benchmarks

This directory contains tools for measuring and comparing performance between monolith and microservices architectures.

## Quick Start - Broker API Latency Test

**RECOMMENDED FIRST STEP:** Measure actual latency to broker APIs from your current VPS:

```bash
python3 benchmarks/broker_api_latency_test.py
```

This tests real latency to:
- Binance, Coinbase, Bybit, Alpaca APIs
- Stock data providers (Polygon, Tiingo, Twelve Data)
- Crypto data providers (CoinGecko, EODHD)

**Use this to determine if multi-region deployment is worth it!**

Results will show:
- Current latency from CDMX VPS to each broker
- Which brokers would benefit from New Jersey VPS
- Which brokers would benefit from Chicago VPS
- Potential latency improvements (50-70% for high-latency brokers)

See `.dev/planning/MULTI_REGION_DEPLOYMENT_STRATEGY.md` for deployment options.

---

## Quick Start - Theoretical Analysis

**No services needed!** Get instant theoretical latency estimates:

```bash
python3 benchmarks/quick_latency_estimate.py
```

This analyzes:
- Network overhead per request (~6.3ms)
- Impact on different operation types
- Percentage overhead compared to monolith
- Recommendations for your use case

## Theoretical Results Summary

Based on typical latencies for localhost HTTP requests:

| Operation | Monolith | Microservices | Overhead | Impact |
|-----------|----------|---------------|----------|--------|
| **Place Order** | 61.3 ms | 67.6 ms | +6.3 ms | ✅ 10% - Low |
| **Get Portfolio** | 5.8 ms | 12.1 ms | +6.3 ms | ⚠️ 108% - High |
| **Get Market Data** | 5.9 ms | 12.2 ms | +6.3 ms | ⚠️ 107% - High |
| **Run Backtest** | 552.8 ms | 559.1 ms | +6.3 ms | ✅ 1% - Negligible |
| **AVERAGE** | 156.5 ms | 162.8 ms | +6.3 ms | ✅ **4% - Acceptable** |

### Key Findings

**✅ Good News:**
- **Overall overhead: 4%** - Very acceptable for most use cases
- **Long operations** (backtesting, order execution) - overhead is negligible (1-10%)
- **I/O-bound operations** - network latency is small compared to database/API calls
- **Benefits outweigh costs** - Scalability + isolation > 6ms overhead

**⚠️ Watch Out For:**
- **Fast read operations** (portfolio, market data) - 100%+ overhead
  - **Solution**: Cache frequently accessed data in Redis
  - **Solution**: Use WebSocket streams for real-time data (bypass REST overhead)
- **High-frequency trading** - Every millisecond counts
  - **Recommendation**: Keep latency-critical paths in monolith

### Architecture Recommendation

**✅ MICROSERVICES ARE VIABLE** for AlgoTrendy because:

1. **Most operations are I/O-bound**
   - Database queries: 5-50ms
   - Broker API calls: 50-200ms
   - Network overhead (6ms) is small relative to these

2. **Long-running operations dominate**
   - Backtesting: 500-5000ms
   - ML training: seconds to minutes
   - 6ms overhead is negligible

3. **Scaling benefits**
   - Data service can scale independently for market data
   - Backtesting service can run on dedicated hardware
   - Trading service can have higher priority/resources

4. **Fault isolation**
   - ML training crash doesn't affect order execution
   - Data provider failure doesn't kill trading

## Real-World Testing

### Step 1: BenchmarkDotNet (In-Memory)

Test direct C# method call performance:

```bash
cd benchmarks/AlgoTrendy.Benchmarks
dotnet run -c Release
```

This measures:
- Order validation speed
- PnL calculation speed
- Market data aggregation
- Serialization overhead

**Use case:** Understand baseline performance without network

### Step 2: HTTP Latency Testing

Test real HTTP request latencies:

```bash
# Terminal 1: Start monolith
docker-compose up

# Terminal 2: Run latency tests
python3 benchmarks/latency_test.py
# Follow prompts, press Enter when ready

# Terminal 1: Stop monolith, start microservices
docker-compose down
docker-compose -f docker-compose.modular.yml up

# Terminal 2: Continue test (press Enter)
```

This measures:
- End-to-end HTTP latencies
- Gateway routing overhead
- Service-to-service communication
- Real database query times

**Results saved to:** `latency_test_results.json`

## Optimization Strategies

If latency becomes an issue:

### 1. **Caching** (Biggest win)
```csharp
// Add Redis caching for frequently accessed data
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Cache portfolio data for 1 second
[ResponseCache(Duration = 1)]
public async Task<Portfolio> GetPortfolio() { ... }
```

**Impact:** Reduces database queries from 5ms → <1ms

### 2. **gRPC Instead of REST** (For service-to-service)
```csharp
// Replace HTTP calls with gRPC
// Faster serialization + HTTP/2 multiplexing
```

**Impact:** Reduces service-to-service latency by ~50%

### 3. **WebSockets for Real-Time Data**
```csharp
// Use SignalR for market data streaming
// Avoids repeated HTTP request overhead
```

**Impact:** Eliminates 6ms HTTP overhead for real-time updates

### 4. **Database Connection Pooling**
```csharp
// Reuse database connections
services.AddDbContextPool<TradingContext>(options => ...);
```

**Impact:** Reduces connection overhead by 2-3ms

### 5. **Async All The Way**
```csharp
// Never block on async calls
var result = await service.GetDataAsync();  // ✅ Good
var result = service.GetDataAsync().Result;  // ❌ Bad (blocks thread)
```

**Impact:** Better throughput, lower latency under load

## When to Use Each Architecture

### Use Monolith (main branch) When:
- ⚡ Ultra-low latency required (<10ms)
- 🏎️ High-frequency trading (microseconds matter)
- 👤 Small team (<5 developers)
- 🚀 Rapid iteration more important than scalability

### Use Microservices (modular branch) When:
- 📈 Need independent scaling (e.g., data service gets 10x traffic)
- 🛡️ Fault isolation critical (ML crash shouldn't affect trading)
- 👥 Multiple teams working on different services
- 🌍 Global deployment (services in different regions)
- 🔧 Different technologies per service (Python ML, .NET trading)

## Benchmark Files

```
benchmarks/
├── README.md (this file)
├── broker_api_latency_test.py    # 🌍 Broker API latency from current VPS (RECOMMENDED FIRST!)
├── quick_latency_estimate.py     # Theoretical analysis (no services needed)
├── latency_test.py                # Real HTTP latency testing (requires services running)
└── AlgoTrendy.Benchmarks/         # BenchmarkDotNet C# benchmarks
    ├── AlgoTrendy.Benchmarks.csproj
    ├── Program.cs
    └── MonolithBenchmarks.cs      # In-memory performance tests
```

**Test Order:**
1. **broker_api_latency_test.py** - Measure real latency to broker APIs (determines multi-region value)
2. **quick_latency_estimate.py** - Theoretical microservices overhead analysis
3. **latency_test.py** - Real HTTP testing (after deploying services)
4. **AlgoTrendy.Benchmarks/** - In-memory C# performance benchmarks

## Interpreting Results

### Latency Metrics

- **Min:** Best case (usually cached or warm)
- **Avg:** Typical performance
- **P95:** 95% of requests faster than this (recommended SLA metric)
- **P99:** 99% of requests faster than this (worst case most users see)

### Acceptable Latencies (General Guidelines)

| Operation Type | Acceptable Latency | User Perception |
|---------------|-------------------|-----------------|
| **Interactive** (portfolio, market data) | <100ms | Instant |
| **Submit** (place order) | <500ms | Responsive |
| **Analysis** (backtest) | <5s | Acceptable |
| **Report** (ML training) | <30s | Patient |

### AlgoTrendy Specific

Based on theoretical analysis:

- **Portfolio fetch:** 12ms (microservices) ✅ Well under 100ms
- **Order placement:** 68ms (microservices) ✅ Well under 500ms
- **Market data:** 12ms (microservices) ✅ Can add caching if needed
- **Backtesting:** 559ms (microservices) ✅ Under 5s, can optimize further

## Next Steps

1. **Review theoretical results** (already done!)
2. **Run BenchmarkDotNet** for baseline metrics
3. **Deploy both architectures** locally
4. **Run real latency tests** with latency_test.py
5. **Make informed decision** based on your specific use case

## Questions to Consider

1. **What's your typical traffic?**
   - <100 requests/sec → Monolith is fine
   - >1000 requests/sec → Microservices help with scaling

2. **What's your growth plan?**
   - Stable user base → Monolith simpler
   - Rapid growth expected → Microservices future-proof

3. **What's your team size?**
   - 1-3 developers → Monolith easier to manage
   - 5+ developers → Microservices allow parallel work

4. **What's your infrastructure budget?**
   - Limited → Monolith (single server)
   - Flexible → Microservices (can optimize cost per service)

## Conclusion

**For AlgoTrendy, microservices add only 4% average latency overhead** while providing:
- ✅ Independent scaling
- ✅ Fault isolation
- ✅ Technology flexibility
- ✅ Parallel development

**Recommendation:** Start with **modular branch** (microservices) for new features, keep **main branch** (monolith) as fallback.

## Support

Questions? Check:
- `PARALLEL_ARCHITECTURE_STRATEGY.md` - Overall strategy
- `VERSION_MANAGEMENT_TOOLING.md` - CI/CD automation
- `services/README.md` - Microservices architecture details
