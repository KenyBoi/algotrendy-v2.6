# 🚀 Quick Start: Bybit Testnet (5 Minutes)

## TL;DR - Fast Track

1. **Get testnet account**: https://testnet.bybit.com (2 min)
2. **Create API key**: Profile → API Management → Create (2 min)
3. **Configure**: Edit `.env` file (30 sec)
4. **Test**: Run `./test_bybit.sh` (30 sec)

**Done!** You're trading! 📈

---

## Step-by-Step

### 1. Get Bybit Testnet Account (2 minutes)

```bash
# Open in browser:
https://testnet.bybit.com

# Sign up with:
- Email
- Password
- Verify email (check spam)
```

💰 **You get 10,000 USDT FREE instantly!**

---

### 2. Create API Key (2 minutes)

1. Login → Profile (top right) → **API Management**
2. Click **"Create New Key"**
3. Configure:
   - Name: `AlgoTrendy`
   - Type: System-generated
   - Permissions: ✅ Read-Write, ✅ Spot, ✅ Contract
   - IP Restriction: No restriction (for testing)
4. Click **Confirm**
5. **COPY BOTH KEYS NOW** ⚠️ (you can't see the secret again!)

---

### 3. Configure AlgoTrendy (30 seconds)

Edit `/root/AlgoTrendy_v2.6/.env`:

```bash
# Find these lines (around line 92-95):
BYBIT_API_KEY=your_bybit_api_key_here
BYBIT_API_SECRET=your_bybit_secret_here
BYBIT_TESTNET=false

# Replace with YOUR keys:
BYBIT_API_KEY=YOUR_ACTUAL_API_KEY_HERE
BYBIT_API_SECRET=YOUR_ACTUAL_SECRET_KEY_HERE
BYBIT_TESTNET=true
```

**Or use this quick command:**
```bash
cd /root/AlgoTrendy_v2.6

# Edit the file
nano .env

# Or use sed (replace YOUR_KEY and YOUR_SECRET):
sed -i 's/BYBIT_API_KEY=.*/BYBIT_API_KEY=YOUR_ACTUAL_KEY/' .env
sed -i 's/BYBIT_API_SECRET=.*/BYBIT_API_SECRET=YOUR_ACTUAL_SECRET/' .env
sed -i 's/BYBIT_TESTNET=false/BYBIT_TESTNET=true/' .env
```

---

### 4. Test Connection (30 seconds)

```bash
cd /root/AlgoTrendy_v2.6

# Run the test script
./test_bybit.sh
```

**Expected output:**
```
✅ Bybit credentials found
✅ Build successful
✅ Successfully connected to Bybit
✅ Balance: 10000.00 USDT
✅ Test Complete!
```

---

## What You Can Do Now

### Test Trading
```bash
cd /root/AlgoTrendy_v2.6/backend

# Start the API
dotnet run --project AlgoTrendy.API/AlgoTrendy.API.csproj
```

Then open: http://localhost:5002/swagger

### Run Integration Tests
```bash
cd /root/AlgoTrendy_v2.6/backend

# Test Bybit connection
dotnet test --filter "Broker=Bybit"
```

### Place a Test Order
```bash
# Using the API (see Swagger UI at http://localhost:5002/swagger)
# Or use curl:

curl -X POST "http://localhost:5002/api/trading/order" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "side": "Buy",
    "type": "Limit",
    "quantity": 0.001,
    "price": 30000
  }'
```

---

## Troubleshooting

### ❌ "Invalid API key"
**Fix**: Double-check your API key and secret in `.env` - no extra spaces!

### ❌ "IP not whitelisted"
**Fix**: In Bybit API Management, set "IP Restriction" to "No restriction"

### ❌ "Insufficient permissions"
**Fix**: Edit API key in Bybit, enable "Spot Trading" and "Contract Trading"

### ❌ Test script not found
**Fix**: Make sure you're in `/root/AlgoTrendy_v2.6/` directory

---

## Next Steps

### 1. Explore the API
```bash
# Start the API server
cd /root/AlgoTrendy_v2.6/backend
dotnet run --project AlgoTrendy.API/AlgoTrendy.API.csproj

# Visit Swagger UI
# http://localhost:5002/swagger
```

### 2. Try Different Order Types
- Market orders (instant execution)
- Limit orders (set your price)
- Stop-loss orders (risk management)

### 3. Test with Leverage
- Default: 1x (no leverage)
- Can test up to 100x leverage
- Start small - even testnet can teach bad habits!

### 4. Monitor Your Trading
- Check positions
- View order history
- Monitor P&L (profit/loss)

---

## Moving to Production

**When ready for real money:**

⚠️ **WARNING: Real money is risky! Start SMALL!**

1. Create production account at https://www.bybit.com
2. Complete KYC verification
3. Deposit funds (start with amount you can afford to lose)
4. Create production API keys
5. Update `.env`:
   ```bash
   BYBIT_TESTNET=false
   BYBIT_API_KEY=production_key_here
   BYBIT_API_SECRET=production_secret_here
   ```
6. Enable IP restrictions
7. Test with minimal amounts first!

---

## Important Links

- **Testnet**: https://testnet.bybit.com
- **Production**: https://www.bybit.com
- **API Docs**: https://bybit-exchange.github.io/docs/v5/intro
- **Support**: https://www.bybit.com/en-US/help-center

---

## Security Checklist

- [x] Using testnet for testing
- [x] API keys in `.env` (not committed to git)
- [ ] IP restrictions enabled (production only)
- [ ] Read-only keys for monitoring (optional)
- [ ] 2FA enabled on account
- [ ] Different keys for testnet vs production
- [ ] Regular key rotation (every 3-6 months)

---

## Summary

**Total Time**: ~5 minutes
**Cost**: $0 (FREE testnet)
**Risk**: 0% (fake money)
**Learning**: Priceless! 🎓

**You're ready to trade! Happy testing! 🚀📈**

---

**Need help?** Check the full guide: `BYBIT_TESTNET_SETUP.md`
