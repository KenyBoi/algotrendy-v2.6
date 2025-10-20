# AMP Global Clearing Broker Setup Guide

**Broker:** AMP Global Clearing (AMP Futures)
**Type:** Futures, Options on Futures
**Platforms:** CQG or Rithmic
**Status:** ✅ Implementation Complete

## Overview

AMP Global Clearing is a premier futures broker offering:
- **Low commission rates** ($0.19-$0.49 per side for futures)
- **CQG or Rithmic** professional trading platforms
- **24/6 futures trading** (Sunday-Friday)
- **Micro contracts** (MES, MNQ, M2K, MYM)
- **Full-size contracts** (ES, NQ, RTY, YM, CL, GC, etc.)

## Implementation Details

**File:** `AMPBroker.cs` (20 KB)
**Platform Support:** CQG WebAPI or Rithmic Protocol Buffers API
**Environment:** Sandbox (Demo) and Live trading

### Features Implemented
- ✅ CQG REST API integration
- ✅ Rithmic REST API integration
- ✅ Authentication and session management
- ✅ Account balance/equity retrieval
- ✅ Position management
- ✅ Order placement (Market, Limit, Stop)
- ✅ Order cancellation
- ✅ Order status tracking
- ✅ Real-time market data
- ✅ Rate limiting (10 req/sec)

## Configuration

### 1. Add to `appsettings.json`

```json
{
  "AMP": {
    "Username": "YOUR_AMP_USERNAME",
    "Password": "YOUR_AMP_PASSWORD",
    "Platform": "CQG",
    "UseSandbox": true,
    "AccountId": "YOUR_ACCOUNT_ID"
  }
}
```

### 2. Environment Variables (Recommended)

For production, use environment variables:

```bash
# .env file
AMP__Username=your_amp_username
AMP__Password=your_amp_password
AMP__Platform=CQG
AMP__UseSandbox=false
AMP__AccountId=your_account_id
```

### 3. Register in Dependency Injection

Add to `Program.cs`:

```csharp
// AMP Broker
builder.Services.Configure<AMPOptions>(
    builder.Configuration.GetSection("AMP"));

// Register HttpClientFactory for AMP
builder.Services.AddHttpClient("AMP");

// Register AMP Broker
builder.Services.AddTransient<IBroker, AMPBroker>();
```

## Platform Selection

### CQG WebAPI
**Recommended for:** Automated trading, REST API simplicity
**Pros:**
- REST API (easier to work with)
- Well-documented
- Stable and reliable
- Good for algorithmic trading

**Cons:**
- Slightly higher latency than Rithmic
- REST-based (not as fast as binary protocol)

**Base URLs:**
- Sandbox: `https://apitest.cqg.com`
- Live: `https://api.cqg.com`

### Rithmic
**Recommended for:** High-frequency trading, lowest latency
**Pros:**
- Ultra-low latency
- Binary protocol (faster)
- Preferred by professional traders
- Direct market access

**Cons:**
- More complex protocol
- Steeper learning curve
- Requires Rithmic license

**Base URLs:**
- Sandbox: `https://rituz01100.rithmic.com`
- Live: `https://api.rithmic.com`

## Getting Started

### Step 1: Open AMP Account

1. Visit https://www.ampclearing.com/
2. Complete account application
3. Fund account (minimum $500 for futures)
4. Choose platform: CQG or Rithmic

### Step 2: Get API Credentials

**For CQG:**
1. Contact AMP support to enable CQG WebAPI
2. Request API credentials (username/password)
3. Get demo account access for testing

**For Rithmic:**
1. Contact AMP support to enable Rithmic API
2. Purchase Rithmic license if needed
3. Get demo credentials for Rithmic Paper Trading

### Step 3: Configure AlgoTrendy

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Edit appsettings.json
nano appsettings.json

# Add AMP configuration (see Configuration section above)
```

### Step 4: Test Connection

```csharp
// Test AMP connection
var ampBroker = services.GetRequiredService<AMPBroker>();
var connected = await ampBroker.ConnectAsync();

if (connected)
{
    var balance = await ampBroker.GetBalanceAsync();
    Console.WriteLine($"AMP Account Balance: ${balance:N2}");
}
```

## Trading Examples

### Example 1: Place Market Order (ES Futures)

```csharp
var orderRequest = new OrderRequest
{
    Symbol = "ESZ4",  // E-mini S&P 500 December 2024
    Side = "buy",
    Type = OrderType.Market,
    Quantity = 1m  // 1 contract
};

var order = await ampBroker.PlaceOrderAsync(orderRequest);
Console.WriteLine($"Order placed: {order.OrderId}");
```

### Example 2: Place Limit Order (NQ Futures)

```csharp
var orderRequest = new OrderRequest
{
    Symbol = "NQZ4",  // E-mini Nasdaq December 2024
    Side = "sell",
    Type = OrderType.Limit,
    Quantity = 2m,  // 2 contracts
    Price = 16500m  // Limit price
};

var order = await ampBroker.PlaceOrderAsync(orderRequest);
```

### Example 3: Get Positions

```csharp
var positions = await ampBroker.GetPositionsAsync();

foreach (var pos in positions)
{
    Console.WriteLine($"{pos.Symbol}: {pos.Size} contracts @ ${pos.EntryPrice}");
    Console.WriteLine($"  Unrealized P&L: ${pos.UnrealizedPnl:N2}");
}
```

### Example 4: Get Account Balance

```csharp
var balance = await ampBroker.GetBalanceAsync();
Console.WriteLine($"Account Equity: ${balance:N2}");
```

## Popular Futures Symbols

### Equity Index Futures
- **ES** - E-mini S&P 500 ($50/point)
- **MES** - Micro E-mini S&P 500 ($5/point)
- **NQ** - E-mini Nasdaq-100 ($20/point)
- **MNQ** - Micro E-mini Nasdaq-100 ($2/point)
- **RTY** - E-mini Russell 2000 ($50/point)
- **M2K** - Micro E-mini Russell 2000 ($5/point)
- **YM** - E-mini Dow ($5/point)
- **MYM** - Micro E-mini Dow ($0.50/point)

### Energy Futures
- **CL** - Crude Oil ($1,000/point)
- **NG** - Natural Gas ($10,000/point)
- **RB** - RBOB Gasoline ($42,000/point)

### Metals Futures
- **GC** - Gold ($100/point)
- **MGC** - Micro Gold ($10/point)
- **SI** - Silver ($5,000/point)
- **HG** - Copper ($25,000/point)

### Agricultural Futures
- **ZC** - Corn ($50/point)
- **ZS** - Soybeans ($50/point)
- **ZW** - Wheat ($50/point)

## Commission Rates (AMP)

| Contract Type | Commission per Side |
|---------------|---------------------|
| E-mini (ES, NQ, RTY, YM) | $0.49 |
| Micro E-mini (MES, MNQ, M2K, MYM) | $0.29 |
| Crude Oil (CL) | $0.49 |
| Gold (GC) | $0.49 |
| Micro Gold (MGC) | $0.29 |

**Data Fees:**
- CME Market Data: $1-5/month (varies)
- Rithmic Platform: $0-120/month (varies by package)

## Risk Management

### Margin Requirements

**Day Trading Margins (Intraday):**
- ES: $500-1,000
- MES: $50-100
- NQ: $1,000-2,000
- MNQ: $100-200

**Overnight Margins:**
- ES: $13,200
- MES: $1,320
- NQ: $18,700
- MNQ: $1,870

**Important:** Always check current margin requirements on AMP's website.

### Risk Controls

```csharp
// Set maximum position size
const decimal MAX_CONTRACTS = 5m;

// Set maximum loss per day
const decimal MAX_DAILY_LOSS = 500m;

// Implement in your trading logic
if (request.Quantity > MAX_CONTRACTS)
{
    throw new Exception($"Position size {request.Quantity} exceeds max {MAX_CONTRACTS}");
}
```

## Troubleshooting

### Issue: Authentication Failed

**Solution:**
1. Verify username/password are correct
2. Ensure API access is enabled for your account
3. Check if using correct platform (CQG vs Rithmic)
4. Confirm account is funded and active

### Issue: Order Rejected

**Possible Causes:**
- Insufficient margin
- Invalid symbol (check expiration month)
- Market closed
- Position limit exceeded

### Issue: Connection Timeout

**Solution:**
1. Check internet connection
2. Verify base URL is correct
3. Ensure firewall allows HTTPS traffic
4. Try sandbox environment first

## Support

**AMP Support:**
- Phone: 1-312-884-1960
- Email: support@ampclearing.com
- Hours: 24/6 (Sunday 5pm - Friday 5pm CT)

**Platform Support:**
- CQG: https://www.cqg.com/support
- Rithmic: https://yyy3.rithmicplatform.com/support

## Next Steps

1. ✅ AMP broker implementation complete
2. ⏭️ Open AMP account and get API credentials
3. ⏭️ Configure credentials in `appsettings.json`
4. ⏭️ Test connection in sandbox environment
5. ⏭️ Create integration tests
6. ⏭️ Go live with small position sizes

## Additional Resources

- **AMP Website:** https://www.ampclearing.com/
- **CQG WebAPI Docs:** https://www.cqg.com/partners/webapi
- **Rithmic Docs:** https://yyy3.rithmicplatform.com/api
- **CME Group:** https://www.cmegroup.com/

---

**Status:** ✅ Implementation Complete
**Ready for:** Configuration and testing
**Estimated Setup Time:** 1-2 hours
