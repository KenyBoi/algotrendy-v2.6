# Phase 7A: Broker Implementation Plan

**Date:** October 19, 2025
**Status:** Ready to Start
**Estimated Duration:** 30-40 hours (1 week)
**Priority:** 🔴 CRITICAL

---

## 📋 Overview

Phase 7A focuses on completing broker integrations to enable multi-exchange trading. This is a critical phase that forms the foundation for the backtesting system (Phase 7B) and all downstream features.

### Current Status

| Broker | Status | Type | Progress |
|--------|--------|------|----------|
| **Binance** | ✅ Implemented | Crypto/Spot | ~80% |
| **Bybit** | ⚠️ Partial | Crypto/Derivatives | ~30% |
| **Alpaca** | ❌ Not Started | Stocks | 0% |
| **OKX** | ⚠️ Data-Only | Crypto/Derivatives | 10% |
| **Kraken** | ❌ Not Started | Crypto/Spot | 0% |

---

## 🎯 Phase 7A Goals

### Primary Objectives
1. **Complete Bybit Integration** (30-40 hours)
   - Full trading support (not just data)
   - Spot and derivatives support
   - Risk management integration
   - Comprehensive testing

2. **Implement Alpaca Broker** (15-20 hours)
   - Stock trading support
   - Market hours validation
   - Position management
   - Integration testing

3. **Upgrade OKX** (20-25 hours)
   - Convert from data provider to trading broker
   - Implement trading operations
   - Add derivatives support
   - Full test coverage

4. **Implement Kraken** (20-25 hours)
   - Complete broker implementation
   - Multi-asset support
   - Staking/margin features
   - Production-ready

### Secondary Objectives
- Comprehensive integration test suite
- Error handling and recovery
- Rate limiting implementation
- Performance monitoring
- Documentation

---

## 🔧 Implementation Strategy

### Week 1 Breakdown

#### Day 1-2: Bybit Broker (Complete)
**Time: 8-10 hours**

**Checklist:**
- [ ] Review existing BybitBroker partial implementation
- [ ] Study Bybit API documentation
- [ ] Implement remaining trading operations
- [ ] Add risk management hooks
- [ ] Create comprehensive integration tests
- [ ] Performance testing

**Deliverables:**
- Fully functional Bybit broker
- 10+ integration tests
- Complete documentation
- Performance baseline

**Files to Modify:**
- `backend/AlgoTrendy.Infrastructure/Brokers/Bybit/BybitBroker.cs`
- `backend/AlgoTrendy.Infrastructure/Brokers/Bybit/*.cs` (supporting files)
- `backend/AlgoTrendy.Tests/Integration/BybitBrokerIntegrationTests.cs`

---

#### Day 3: Alpaca Broker (MVP)
**Time: 6-8 hours**

**Checklist:**
- [ ] Create IBroker implementation for Alpaca
- [ ] Implement market hours validation
- [ ] Add position management
- [ ] Implement order operations
- [ ] Create integration tests
- [ ] Documentation

**Deliverables:**
- Alpaca broker MVP
- 5+ integration tests
- Market hours validator
- Documentation

**Files to Create:**
- `backend/AlgoTrendy.Infrastructure/Brokers/Alpaca/AlpacaBroker.cs`
- `backend/AlgoTrendy.Infrastructure/Brokers/Alpaca/AlpacaConfig.cs`
- `backend/AlgoTrendy.Tests/Integration/AlpacaBrokerIntegrationTests.cs`

---

#### Day 4-5: OKX Upgrade (MVP)
**Time: 8-10 hours**

**Checklist:**
- [ ] Review existing OKX data provider
- [ ] Implement trading operations
- [ ] Add derivative support (optional)
- [ ] Refactor from data provider to trading broker
- [ ] Create integration tests
- [ ] Migrate existing data code

**Deliverables:**
- OKX trading broker
- Maintained data functionality
- 6+ integration tests
- Upgrade documentation

**Files to Modify:**
- Existing OKX data provider
- Create: `backend/AlgoTrendy.Infrastructure/Brokers/OKX/OKXBroker.cs`

---

#### Day 6-7: Kraken Broker (MVP)
**Time: 8-10 hours**

**Checklist:**
- [ ] Study Kraken API
- [ ] Implement core broker interface
- [ ] Add position management
- [ ] Implement order operations
- [ ] Create integration tests
- [ ] Documentation

**Deliverables:**
- Kraken broker MVP
- 5+ integration tests
- Documentation
- Performance baseline

**Files to Create:**
- `backend/AlgoTrendy.Infrastructure/Brokers/Kraken/KrakenBroker.cs`
- `backend/AlgoTrendy.Infrastructure/Brokers/Kraken/KrakenConfig.cs`
- `backend/AlgoTrendy.Tests/Integration/KrakenBrokerIntegrationTests.cs`

---

## 📐 Architecture Reference

### IBroker Interface Pattern

```csharp
// Location: backend/AlgoTrendy.TradingEngine/Brokers/IBroker.cs
public interface IBroker
{
    // Connection Management
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected { get; }

    // Account & Balance
    Task<decimal> GetBalanceAsync(string currency);
    Task<Dictionary<string, decimal>> GetBalancesAsync();

    // Market Data
    Task<decimal> GetCurrentPriceAsync(string symbol);
    Task<List<Candle>> GetCandlesAsync(string symbol, string interval, int limit);

    // Position Management
    Task<List<Position>> GetPositionsAsync();
    Task<Position?> GetPositionAsync(string symbol);

    // Order Management
    Task<Order> PlaceOrderAsync(OrderRequest request);
    Task<Order> CancelOrderAsync(string exchangeOrderId, string symbol);
    Task<Order> GetOrderStatusAsync(string exchangeOrderId, string symbol);
    Task<List<Order>> GetOrdersAsync(string symbol);

    // Leverage & Margin (Optional)
    Task<LeverageInfo?> GetLeverageInfoAsync(string symbol);
    Task<bool> SetLeverageAsync(string symbol, decimal leverage);
}
```

### Key Implementation Patterns

#### 1. Connection Lifecycle
```csharp
public async Task<bool> ConnectAsync()
{
    try
    {
        // 1. Validate credentials
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
            throw new InvalidOperationException("Missing API credentials");

        // 2. Test API connection
        var response = await _httpClient.GetAsync("/api/account");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to connect to broker");

        // 3. Load initial data
        var balances = await GetBalancesAsync();
        var positions = await GetPositionsAsync();

        // 4. Set connected state
        _isConnected = true;
        _logger.LogInformation("Connected to {Broker}", BrokerName);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Connection failed");
        _isConnected = false;
        throw;
    }
}
```

#### 2. Order Error Handling
```csharp
try
{
    var order = await PlaceOrderAsync(request);
    return order;
}
catch (HttpRequestException ex) when (ex.Message.Contains("401"))
{
    // Authentication error
    throw new InvalidOperationException("Invalid API credentials");
}
catch (HttpRequestException ex) when (ex.Message.Contains("429"))
{
    // Rate limit error - implement exponential backoff
    _logger.LogWarning("Rate limit hit, backing off");
    await Task.Delay(TimeSpan.FromSeconds(exponentialBackoff));
    return await PlaceOrderAsync(request);
}
catch (HttpRequestException ex) when (ex.Message.Contains("Insufficient"))
{
    // Balance error
    throw new InvalidOperationException("Insufficient balance");
}
```

---

## 🧪 Testing Strategy

### Integration Test Template

```csharp
[Collection("BrokerIntegration")]
public class NewBrokerIntegrationTests : IAsyncLifetime
{
    private readonly INewBroker _broker;
    private readonly ILogger<NewBrokerIntegrationTests> _logger;
    private bool _skipTests;

    public NewBrokerIntegrationTests()
    {
        // Initialize broker with credentials from environment
        _skipTests = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BROKER_API_KEY"));
    }

    public async Task InitializeAsync()
    {
        if (!_skipTests)
        {
            await _broker.ConnectAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (!_skipTests)
        {
            await _broker.DisconnectAsync();
        }
    }

    [SkippableFact]
    public async Task ConnectAsync_WithValidCredentials_Succeeds()
    {
        Skip.If(_skipTests, "Credentials not configured");

        var result = await _broker.ConnectAsync();

        Assert.True(result);
        Assert.True(_broker.IsConnected);
    }

    [SkippableFact]
    public async Task GetBalanceAsync_ReturnsValidBalance()
    {
        Skip.If(_skipTests, "Credentials not configured");

        var balance = await _broker.GetBalanceAsync("USDT");

        Assert.True(balance >= 0);
    }
}
```

### Required Test Cases per Broker

For each broker, implement these test suites:

1. **Connection Tests** (2-3 tests)
   - Valid credentials connect
   - Invalid credentials fail gracefully
   - Reconnection handling

2. **Account Tests** (3-4 tests)
   - Get balance
   - Get positions
   - Account info retrieval

3. **Order Tests** (4-5 tests)
   - Place market order
   - Place limit order
   - Cancel order
   - Get order status
   - List orders

4. **Position Tests** (2-3 tests)
   - Get positions
   - Calculate PnL
   - Check leverage (if applicable)

5. **Error Handling Tests** (3-4 tests)
   - Network timeout recovery
   - Rate limit handling
   - Invalid order handling
   - Insufficient balance

**Total per broker: 14-19 integration tests**

---

## 📝 Documentation Requirements

### For Each Broker

1. **README.md**
   - Overview and features
   - API requirements
   - Configuration instructions
   - Known limitations

2. **Implementation Notes**
   - Architecture decisions
   - Error handling strategy
   - Rate limiting approach
   - Performance considerations

3. **Testing Guide**
   - How to set up testnet
   - Running integration tests
   - Interpreting results

---

## ⚠️ Common Pitfalls to Avoid

### 1. **Inconsistent Error Handling**
❌ **Bad:**
```csharp
try { ... }
catch (Exception) { return null; }
```

✅ **Good:**
```csharp
try { ... }
catch (HttpRequestException ex) when (ex.StatusCode == 401)
{
    throw new UnauthorizedAccessException("Invalid API key");
}
catch (HttpRequestException ex) when (ex.StatusCode == 429)
{
    // Implement retry logic
}
```

### 2. **Missing Connection State Checks**
❌ **Bad:**
```csharp
public async Task<decimal> GetBalanceAsync(string currency)
{
    return await _httpClient.GetAsync(...);
}
```

✅ **Good:**
```csharp
public async Task<decimal> GetBalanceAsync(string currency)
{
    if (!_isConnected)
        throw new InvalidOperationException("Not connected to broker");

    return await _httpClient.GetAsync(...);
}
```

### 3. **Inconsistent DateTime Handling**
❌ **Bad:** Mix of local time and UTC
✅ **Good:** Always use UTC with DateTime.UtcNow

### 4. **Missing Rate Limit Headers**
Always check response headers for rate limit info:
```csharp
if (response.Headers.Contains("X-RateLimit-Remaining"))
{
    var remaining = response.Headers.GetValues("X-RateLimit-Remaining").First();
    _logger.LogDebug("Rate limit remaining: {Remaining}", remaining);
}
```

---

## 🚀 Dependency Injection Setup

### Register Brokers in Program.cs

```csharp
// Get broker from environment
var brokerType = builder.Configuration["TradingEngine:DefaultBroker"] ?? "Binance";

builder.Services.AddScoped<IBroker>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IBroker>>();
    var config = sp.GetRequiredService<IConfiguration>();

    return brokerType switch
    {
        "Bybit" => new BybitBroker(
            config["Brokers:Bybit:ApiKey"],
            config["Brokers:Bybit:ApiSecret"],
            config.GetValue<bool>("Brokers:Bybit:UseTestnet"),
            logger),

        "Alpaca" => new AlpacaBroker(
            config["Brokers:Alpaca:ApiKey"],
            config["Brokers:Alpaca:BaseUrl"],
            logger),

        "OKX" => new OKXBroker(
            config["Brokers:OKX:ApiKey"],
            config["Brokers:OKX:ApiSecret"],
            logger),

        "Kraken" => new KrakenBroker(
            config["Brokers:Kraken:ApiKey"],
            config["Brokers:Kraken:ApiSecret"],
            logger),

        _ => throw new InvalidOperationException($"Unknown broker: {brokerType}")
    };
});
```

---

## 📊 Success Criteria

### Code Quality
- [ ] All SOLID principles followed
- [ ] No code duplication
- [ ] Comprehensive error handling
- [ ] Clear logging at all levels
- [ ] Well-documented interfaces

### Testing
- [ ] 14+ integration tests per broker
- [ ] All tests passing in CI/CD
- [ ] Code coverage > 80%
- [ ] Performance benchmarks established

### Documentation
- [ ] README for each broker
- [ ] Implementation notes
- [ ] API reference
- [ ] Configuration guide
- [ ] Testing guide

### Functionality
- [ ] All required operations implemented
- [ ] Rate limiting handled
- [ ] Connection lifecycle managed
- [ ] Error recovery implemented
- [ ] Idempotency verified (uses existing TradingEngine system)

---

## 📅 Timeline & Effort

| Broker | Effort | Days | Status |
|--------|--------|------|--------|
| **Bybit** | 30-40h | 3 | 🟡 Ready |
| **Alpaca** | 15-20h | 2 | 🟢 Ready |
| **OKX** | 20-25h | 2.5 | 🟢 Ready |
| **Kraken** | 20-25h | 2.5 | 🟢 Ready |
| **Testing/Docs** | 5-10h | 1 | 🟢 Ongoing |
| **TOTAL** | 90-120h | 7-10 | ✅ Ready |

---

## 🔗 Related Documentation

- **ADVANCED_IMPLEMENTATION_NOTES.md** - Detailed implementation guidance
- **SESSION_IDEMPOTENCY_IMPROVEMENTS.md** - Order idempotency system (USE THIS!)
- **AUDIT_SUMMARY.md** - Missing components analysis
- **MISSING_COMPONENTS_COMPREHENSIVE_AUDIT.md** - Detailed effort estimates

---

## 📞 Getting Started

### Prerequisites
1. Clone repository and install dependencies
2. Set up test credentials for each broker
3. Configure environment variables
4. Run existing broker tests as reference

### First Task
1. **Review BinanceBroker** (`backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs`)
   - Study the pattern
   - Understand error handling
   - Review test structure

2. **Review BybitBroker** (`backend/AlgoTrendy.Infrastructure/Brokers/Bybit/BybitBroker.cs`)
   - Identify what's missing
   - Plan completion strategy
   - Set up test environment

3. **Create Alpaca Broker**
   - Use BinanceBroker as template
   - Implement market hours validation
   - Add comprehensive tests

---

## ✅ Phase 7A Completion Checklist

- [ ] Bybit broker 100% complete with tests
- [ ] Alpaca broker 100% complete with tests
- [ ] OKX broker upgraded with tests
- [ ] Kraken broker 100% complete with tests
- [ ] All 4 brokers integrated into DI
- [ ] 60+ integration tests passing
- [ ] Documentation complete
- [ ] Performance baselines established
- [ ] Ready for Phase 7B (Backtesting)

---

**Status:** ✅ READY TO BEGIN

**Next Phase:** Phase 7B - Backtesting System (after broker integration complete)

**Questions?** See ADVANCED_IMPLEMENTATION_NOTES.md for detailed patterns and examples.

