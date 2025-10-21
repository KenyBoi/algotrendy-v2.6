# Bybit Testnet Setup Guide

## Overview

Bybit testnet allows risk-free testing of trading strategies with virtual funds. This guide shows you how to set up Bybit testnet integration with AlgoTrendy.

**Why Use Testnet?**
- 🆓 Free virtual trading ($100,000 USDT)
- 🔄 Unlimited resets
- 📊 Real market data
- ⚡ Same API as production
- 🛡️ Zero risk

## Prerequisites

- AlgoTrendy v2.6 installed
- Email address
- Web browser

## Step 1: Create Bybit Testnet Account

1. **Visit Bybit Testnet**
   - Go to https://testnet.bybit.com
   - Click "Sign Up" (top right)

2. **Register Account**
   - Enter email address
   - Create password (min 8 characters, 1 uppercase, 1 number)
   - Verify email (check spam folder)

3. **Login**
   - Go to https://testnet.bybit.com
   - Login with your credentials

4. **Get Free Testnet Funds**
   - Navigate to Assets → Overview
   - Click "Get Testnet Funds"
   - Receive 100,000 USDT (free, unlimited resets!)

## Step 2: Generate API Keys

1. **Navigate to API Management**
   - Click profile icon (top right)
   - Select "API"
   - Or visit: https://testnet.bybit.com/app/user/api-management

2. **Create New API Key**
   - Click "Create New Key"
   - Choose "System-generated API Keys"
   - Name: `AlgoTrendy-Dev`

3. **Set Permissions**
   - ✅ **Read-Write** (for trading)
   - ✅ Contract Trading
   - ✅ Spot Trading
   - ❌ Withdraw (keep disabled for security)
   - ❌ Affiliate (not needed)

4. **Set IP Restrictions (Optional but Recommended)**
   - Click "Edit" next to IP
   - Add your server IP
   - Example: `203.0.113.5`
   - Click "Confirm"

   ```bash
   # Find your IP address
   curl https://ipinfo.io/ip
   ```

5. **Generate Key**
   - Click "Submit"
   - Enter 2FA code (if enabled)
   - **IMPORTANT:** Copy and save immediately!
     - API Key: `xxxxxxxxxxxxxxxxxxxxxx`
     - Secret Key: `yyyyyyyyyyyyyyyyyyyyyy`
   - ⚠️ Secret key shown only once!

## Step 3: Configure AlgoTrendy

### Option A: User Secrets (Development - Recommended)

```bash
# Navigate to API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Set API Key
dotnet user-secrets set "BYBIT_API_KEY" "YOUR_API_KEY"

# Set API Secret
dotnet user-secrets set "BYBIT_API_SECRET" "YOUR_API_SECRET"

# Enable testnet mode
dotnet user-secrets set "BYBIT_TESTNET" "true"

# Verify secrets are set
dotnet user-secrets list | grep BYBIT
```

### Option B: Environment Variables (Production)

```bash
# Add to ~/.bashrc
export BYBIT_API_KEY="YOUR_API_KEY"
export BYBIT_API_SECRET="YOUR_API_SECRET"
export BYBIT_TESTNET="true"

# Reload environment
source ~/.bashrc
```

### Option C: appsettings.Development.json (Local Only)

⚠️ **Never commit to git!**

```json
{
  "Bybit": {
    "ApiKey": "YOUR_API_KEY",
    "ApiSecret": "YOUR_API_SECRET",
    "UseTestnet": true
  }
}
```

## Step 4: Test Connection

### Start AlgoTrendy API

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run
```

### Test via cURL

```bash
# Test authentication
curl -X GET "http://localhost:5002/api/trading/balance?exchange=bybit" \
  -H "Content-Type: application/json"

# Expected response:
# {
#   "balance": 100000.00,
#   "currency": "USDT",
#   "exchange": "bybit",
#   "testnet": true
# }
```

### Test via Swagger UI

1. Open browser: http://localhost:5002/swagger
2. Expand "Trading" section
3. Try "GET /api/trading/balance"
4. Parameters:
   - `exchange`: `bybit`
5. Click "Execute"
6. Should show 100,000 USDT balance

## Step 5: Place Your First Test Order

### Market Order (Buy BTC)

```bash
curl -X POST "http://localhost:5002/api/trading/order" \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "bybit",
    "symbol": "BTCUSDT",
    "side": "Buy",
    "type": "Market",
    "quantity": 0.001
  }'

# Response:
# {
#   "orderId": "550e8400-e29b-41d4-a716-446655440000",
#   "status": "Filled",
#   "averageFillPrice": 43250.50,
#   "message": "Order placed successfully"
# }
```

### Limit Order (Sell ETH)

```bash
curl -X POST "http://localhost:5002/api/trading/order" \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "bybit",
    "symbol": "ETHUSDT",
    "side": "Sell",
    "type": "Limit",
    "quantity": 0.1,
    "price": 3000.00
  }'
```

## Step 6: Monitor Positions

```bash
# Get all positions
curl -X GET "http://localhost:5002/api/trading/positions?exchange=bybit"

# Response:
# [
#   {
#     "symbol": "BTCUSDT",
#     "side": "Long",
#     "quantity": 0.001,
#     "entryPrice": 43250.50,
#     "currentPrice": 43500.00,
#     "unrealizedPnL": 0.25
#   }
# ]
```

## Configuration Reference

### appsettings.json

```json
{
  "Bybit": {
    "UseTestnet": true,
    "ApiKey": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "ApiSecret": "USE_USER_SECRETS_IN_DEVELOPMENT"
  }
}
```

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `BYBIT_API_KEY` | Your Bybit API key | `abc123...` |
| `BYBIT_API_SECRET` | Your API secret | `xyz789...` |
| `BYBIT_TESTNET` | Enable testnet mode | `true` |

## Testnet vs Mainnet

| Feature | Testnet | Mainnet |
|---------|---------|---------|
| **Cost** | Free | Real money |
| **Funds** | Virtual (100K USDT) | Real funds |
| **Data** | Real market data | Real market data |
| **Orders** | Simulated execution | Real execution |
| **API** | testnet.bybit.com | api.bybit.com |
| **Risk** | Zero | Real |

### Switching to Mainnet

When ready for live trading:

1. Create mainnet account at https://www.bybit.com
2. Complete KYC verification
3. Deposit real funds
4. Generate mainnet API keys
5. Update configuration:
   ```bash
   dotnet user-secrets set "BYBIT_TESTNET" "false"
   ```

⚠️ **Test thoroughly on testnet first!**

## Troubleshooting

### Error: "Invalid API key"

**Solution:**
```bash
# Verify key is set correctly
dotnet user-secrets list | grep BYBIT_API_KEY

# Check for extra spaces
echo "$BYBIT_API_KEY" | wc -c

# Re-generate key on Bybit testnet if needed
```

### Error: "Signature verification failed"

**Causes:**
- Incorrect API secret
- System time not synchronized
- IP restriction mismatch

**Solution:**
```bash
# Sync system time
sudo ntpdate pool.ntp.org

# Verify secret
dotnet user-secrets list | grep BYBIT_API_SECRET

# Check IP matches whitelist
curl https://ipinfo.io/ip
```

### Error: "Insufficient balance"

**Solution:**
```bash
# Reset testnet funds
# Visit: https://testnet.bybit.com/app/user/assets/exchange
# Click "Get Testnet Funds"
# Receive fresh 100,000 USDT
```

### Connection refused

**Solution:**
```bash
# Verify testnet mode is enabled
grep "UseTestnet" backend/AlgoTrendy.API/appsettings.json

# Should show: "UseTestnet": true
```

## API Rate Limits

| Endpoint Type | Testnet Limit | Mainnet Limit |
|---------------|---------------|---------------|
| Public | 120/min | 120/min |
| Private | 100/min | 100/min |
| Order Placement | 10/sec | 10/sec |

## Security Best Practices

1. **Enable 2FA** on Bybit testnet account
2. **Use IP whitelist** for API keys
3. **Never commit secrets** to source control
4. **Rotate keys** every 90 days
5. **Disable withdraw** permission
6. **Monitor API usage** regularly

### Security Checklist

- [x] 2FA enabled on account
- [x] IP whitelist configured
- [x] Withdraw permission disabled
- [x] Secrets stored in user-secrets
- [x] .gitignore includes appsettings.Development.json
- [x] Regular key rotation scheduled

## Advanced Features

### Leverage Trading

```bash
# Set leverage to 10x
curl -X POST "http://localhost:5002/api/trading/leverage" \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "bybit",
    "symbol": "BTCUSDT",
    "leverage": 10
  }'
```

### Stop Loss Orders

```bash
curl -X POST "http://localhost:5002/api/trading/order" \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "bybit",
    "symbol": "BTCUSDT",
    "side": "Sell",
    "type": "StopLoss",
    "quantity": 0.001,
    "stopPrice": 42000.00
  }'
```

## Testing Strategies

### Recommended Testing Workflow

1. **Start Small**
   - Begin with 0.001 BTC positions
   - Test order types: Market, Limit, Stop

2. **Test Edge Cases**
   - Insufficient balance
   - Invalid symbols
   - Out-of-range prices

3. **Monitor Performance**
   - Track execution latency
   - Monitor fill rates
   - Check slippage

4. **Stress Test**
   - Rapid order placement
   - Concurrent positions
   - Market volatility

5. **Validate Results**
   - Compare with Bybit UI
   - Verify P&L calculations
   - Check order history

## Support Resources

- **Bybit Testnet:** https://testnet.bybit.com
- **API Documentation:** https://bybit-exchange.github.io/docs/v5/intro
- **Community:** https://www.bybit.com/en-US/help-center
- **AlgoTrendy Docs:** `/docs/README.md`

## Next Steps

1. ✅ Create testnet account
2. ✅ Generate API keys
3. ✅ Configure AlgoTrendy
4. ✅ Test connection
5. ✅ Place test orders
6. 📊 Run backtest with testnet execution
7. 🚀 Graduate to mainnet when ready

## Common Testing Scenarios

### Scenario 1: Buy and Hold

```bash
# Buy BTC
curl -X POST ".../trading/order" -d '{"side":"Buy","quantity":0.1}'

# Wait 1 hour

# Check P&L
curl -X GET ".../trading/positions?exchange=bybit"

# Sell
curl -X POST ".../trading/order" -d '{"side":"Sell","quantity":0.1}'
```

### Scenario 2: Scalping Strategy

```bash
# Place multiple small orders
for i in {1..10}; do
  curl -X POST ".../trading/order" \
    -d '{"side":"Buy","quantity":0.001}'
  sleep 60
done
```

### Scenario 3: Risk Management

```bash
# Place order with stop loss and take profit
curl -X POST ".../trading/order" \
  -d '{
    "side": "Buy",
    "quantity": 0.01,
    "stopLoss": 42000,
    "takeProfit": 45000
  }'
```

## Updates

**Last Updated:** October 21, 2025
**Tested With:** Bybit API v5
**AlgoTrendy Version:** 2.6.0
