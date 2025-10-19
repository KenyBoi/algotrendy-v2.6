# Bybit Broker Porting Plan: v2.5 → v2.6

**Status:** Planning Phase
**Estimated Duration:** 30-40 hours
**Complexity:** High (Financial APIs, Leverage Management, Error Handling)

---

## 📋 Analysis Summary

### v2.5 Implementation Overview
**File:** `/root/AlgoTrendy_v2.6/legacy_reference/v2.5_brokers/broker_abstraction.py`
**Lines:** 61-179 (BybitBroker class)
**Library:** `pybit` (Bybit Unified Trading API)

### v2.6 Interface Requirements
**File:** `AlgoTrendy.Core/Interfaces/IBroker.cs`
**Interface Methods:**
- ConnectAsync()
- GetBalanceAsync()
- GetPositionsAsync()
- PlaceOrderAsync()
- CancelOrderAsync()
- GetOrderStatusAsync()
- GetCurrentPriceAsync()
- SetLeverageAsync()
- GetLeverageInfoAsync()
- GetMarginHealthRatioAsync()

### Feature Mapping: v2.5 → v2.6

| v2.5 Method | v2.6 Method | Status | Complexity |
|-------------|-------------|--------|-----------|
| connect() | ConnectAsync() | ✅ Direct mapping | Low |
| get_balance() | GetBalanceAsync() | ✅ Direct mapping | Low |
| get_positions() | GetPositionsAsync() | ✅ Direct mapping | Medium |
| place_order() | PlaceOrderAsync() | ✅ Direct mapping | High |
| close_position() | N/A (handled by PlaceOrderAsync with opposite) | ✅ Different approach | Medium |
| get_market_price() | GetCurrentPriceAsync() | ⚠️ Requires refactoring | Low |
| set_leverage() | SetLeverageAsync() + GetLeverageInfoAsync() | ✅ Extended | High |

---

## 🔧 Implementation Phases

### Phase 1: Project Setup & Dependencies (2 hours)

**Tasks:**
1. Choose C# Bybit SDK
   - **Option A:** Bybit.Net (CoinEx/Bybit community library)
   - **Option B:** Official Bybit REST API with HttpClient
   - **Recommendation:** Bybit.Net (well-maintained, type-safe)

2. Add NuGet package to csproj
   ```xml
   <PackageReference Include="Bybit.Net" Version="latest" />
   ```

3. Create folder structure
   ```
   AlgoTrendy.Infrastructure/
   └── Brokers/
       ├── BybitBroker.cs
       ├── BybitAuthenticator.cs
       └── BybitModels.cs
   ```

### Phase 2: Core Connection & Authentication (4 hours)

**File:** `AlgoTrendy.Infrastructure/Brokers/BybitBroker.cs`

**Implement:**
```csharp
public class BybitBroker : IBroker
{
    public string BrokerName => "Bybit";

    private BybitClient? _client;
    private BybitSocketClient? _socketClient;
    private readonly BybitAuthenticator _auth;
    private readonly ILogger<BybitBroker> _logger;

    public BybitBroker(BybitAuthenticator auth, ILogger<BybitBroker> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        // Initialize Bybit client with credentials
        // Test connection with account info call
        // Return connection status
    }
}
```

**Key Points:**
- Initialize BybitClient with API key/secret
- Test connection by calling get_account_info equivalent
- Handle testnet vs. production mode
- Log connection success/failure

### Phase 3: Account Operations (6 hours)

**Methods to Implement:**
1. **GetBalanceAsync()**
   - Call: `GET /v5/account/wallet-balance`
   - Return: Decimal balance for specified currency
   - Error handling: Null checks, exception logging

2. **GetMarginHealthRatioAsync()**
   - Call: `GET /v5/account/wallet-balance`
   - Calculate: `walletBalance / totalWalletBalance`
   - Return: Decimal ratio (0.0 to 1.0)

3. **GetLeverageInfoAsync()**
   - Call: `GET /v5/position/list`
   - Extract: Symbol-specific leverage data
   - Return: LeverageInfo model with:
     - Symbol
     - CurrentLeverage
     - MaxLeverage
     - MarginType (Cross/Isolated)

**Bybit API Endpoints:**
```
GET /v5/account/wallet-balance          - Get wallet balance
GET /v5/position/list                   - Get positions with leverage
GET /v5/account/info                    - Get account info
```

### Phase 4: Position Management (8 hours)

**Methods to Implement:**
1. **GetPositionsAsync()**
   - Call: `GET /v5/position/list`
   - Parse response: Category="linear", settleCoin="USDT"
   - Map to Position model:
     - Symbol
     - Side (Buy/Sell)
     - Size
     - EntryPrice
     - UnrealizedPnL
     - etc.

2. **SetLeverageAsync()**
   - Call: `POST /v5/position/set-leverage`
   - Parameters: symbol, buyLeverage, sellLeverage, marginType
   - Validate: Max 100x leverage on Bybit
   - Return: Success/failure

**Key Challenges:**
- Handling multiple positions per symbol
- Convert Bybit response format to Position model
- Proper error handling for non-existent positions

### Phase 5: Order Management (10 hours)

**Methods to Implement:**
1. **PlaceOrderAsync(OrderRequest request)**
   - Call: `POST /v5/order/create`
   - Parameters from OrderRequest:
     - Symbol
     - Side (Buy/Sell)
     - OrderType (Market/Limit)
     - Quantity
     - Price (for limit orders)
     - TakeProfit/StopLoss (if specified)
   - Return: Order model with:
     - OrderId (exchange ID)
     - Status
     - FilledQuantity
     - AveragePrice
     - Timestamp

2. **CancelOrderAsync(string orderId, string symbol)**
   - Call: `POST /v5/order/cancel`
   - Parameters: symbol, orderId
   - Return: Updated Order with "Cancelled" status

3. **GetOrderStatusAsync(string orderId, string symbol)**
   - Call: `GET /v5/order/realtime`
   - Return: Order with latest status

4. **GetCurrentPriceAsync(string symbol)**
   - Call: `GET /v5/market/tickers`
   - Return: Decimal (last price)

**Key Challenges:**
- Mapping OrderRequest to Bybit-specific order types
- Handling partial fills
- Post-only orders
- Reduce-only orders (for closing positions)
- Proper error messages for insufficient balance, invalid leverage, etc.

### Phase 6: Error Handling & Resilience (4 hours)

**Implement:**
- Graceful handling of network timeouts
- Rate limit handling (Bybit: 10 req/sec for most endpoints)
- Retry logic with exponential backoff
- Logging of all API calls and responses
- Custom exception types:
  - InsufficientBalanceException
  - InvalidLeverageException
  - OrderRejectedException
  - PositionNotFoundException

### Phase 7: Testing (6 hours)

**Unit Tests:**
- Connection tests (mocked client)
- Balance retrieval
- Position parsing
- Order creation validation
- Error scenarios

**Integration Tests (optional):**
- Testnet connection
- Paper trading order placement
- Real leverage operations
- Rate limiting behavior

---

## 📊 API Endpoint Reference

### Core Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/v5/account/wallet-balance` | GET | Account balance |
| `/v5/account/info` | GET | Account info |
| `/v5/position/list` | GET | Active positions |
| `/v5/position/set-leverage` | POST | Set leverage |
| `/v5/order/create` | POST | Place order |
| `/v5/order/cancel` | POST | Cancel order |
| `/v5/order/realtime` | GET | Order status |
| `/v5/market/tickers` | GET | Market price |

### Authentication
- **Header:** `X-BAPI-KEY: {api_key}`
- **Header:** `X-BAPI-SIGN: {signature}`
- **Header:** `X-BAPI-TIMESTAMP: {timestamp}`
- **Algorithm:** HMAC-SHA256

---

## 🔄 Integration Points

### 1. Dependency Injection
```csharp
// In Program.cs
builder.Services.AddScoped<BybitAuthenticator>();
builder.Services.AddScoped<IBroker, BybitBroker>();
```

### 2. Configuration
```json
{
  "Brokers": {
    "Bybit": {
      "ApiKey": "...",
      "ApiSecret": "...",
      "Testnet": true
    }
  }
}
```

### 3. Usage in TradingEngine
```csharp
public class TradingEngine
{
    public async Task ExecuteOrderAsync(OrderRequest request)
    {
        var order = await _broker.PlaceOrderAsync(request);
        // Process order...
    }
}
```

---

## ⚠️ Known Challenges & Solutions

| Challenge | Solution |
|-----------|----------|
| Rate limiting | Implement request throttling + queue |
| Network timeouts | Retry logic with exponential backoff |
| Testnet/Production switching | Configuration-based environment selection |
| Decimal precision | Use `decimal` type consistently, not `double` |
| Timestamp synchronization | Use NTP-synced server time |
| Order partial fills | Track filled quantity separately |
| Leverage limits by symbol | Validate against Bybit's per-symbol limits |

---

## 📝 Deliverables

### Code Files
1. `BybitBroker.cs` - Main implementation (~400-500 lines)
2. `BybitAuthenticator.cs` - HMAC signature generation (~50 lines)
3. `BybitModels.cs` - DTO/response models (~100-150 lines)
4. `BybitBrokerTests.cs` - Unit tests (~300-400 lines)

### Documentation
1. `BYBIT_IMPLEMENTATION.md` - Implementation guide
2. API response examples
3. Test scenarios

### Integration
1. Update `IBroker` implementations list
2. Add Bybit configuration to appsettings.json
3. Update DI container in Program.cs

---

## 🚀 Success Criteria

- ✅ All IBroker interface methods implemented
- ✅ Successful connection to Bybit testnet
- ✅ All unit tests passing (80%+ coverage)
- ✅ Order placement and cancellation working
- ✅ Position tracking and leverage management functional
- ✅ Proper error handling and logging
- ✅ No hardcoded credentials or test data

---

## 📅 Timeline

| Phase | Duration | Cumulative |
|-------|----------|-----------|
| Setup | 2h | 2h |
| Connection | 4h | 6h |
| Accounts | 6h | 12h |
| Positions | 8h | 20h |
| Orders | 10h | 30h |
| Error Handling | 4h | 34h |
| Testing | 6h | 40h |
| **TOTAL** | **40h** | **40h** |

---

## 📚 References

- Bybit Unified API Docs: https://bybit-exchange.github.io/docs/v5/intro
- Bybit.Net GitHub: https://github.com/JKorf/Bybit.Net
- AlgoTrendy v2.5 Reference: `/root/AlgoTrendy_v2.6/legacy_reference/v2.5_brokers/broker_abstraction.py`
- v2.6 IBroker Interface: `AlgoTrendy.Core/Interfaces/IBroker.cs`
