# Bybit Testnet Setup Guide

**Last Updated:** 2025-10-21
**Purpose:** Set up Bybit testnet credentials for safe testing of AlgoTrendy trading features

---

## 🎯 Overview

Bybit testnet provides a safe sandbox environment to test trading strategies without risking real funds. This guide walks you through:
1. Creating a Bybit testnet account
2. Generating API keys
3. Configuring AlgoTrendy to use testnet
4. Testing the connection

---

## 📋 Prerequisites

- ✅ AlgoTrendy v2.6 installed and configured
- ✅ Valid email address for testnet account
- ✅ Basic understanding of API keys and trading

---

## 🚀 Step 1: Create Bybit Testnet Account

### 1.1 Access Testnet Portal

Navigate to: **https://testnet.bybit.com/**

### 1.2 Register Account

1. Click **"Sign Up"** in the top right corner
2. Choose registration method:
   - **Email** (recommended)
   - Phone number
   - Social login (Google, Apple)
3. Enter your email address
4. Create a strong password
5. Complete the verification (email code or captcha)
6. Click **"Create Account"**

### 1.3 Verify Email

1. Check your email inbox for verification message
2. Click the verification link
3. Your testnet account is now active

### 1.4 Get Testnet Funds (Optional but Recommended)

1. Log in to https://testnet.bybit.com
2. Navigate to **"Assets" → "Testnet Faucet"** or similar
3. Request testnet funds (USDT, BTC, ETH, etc.)
4. Testnet funds are free and reset periodically

---

## 🔑 Step 2: Generate API Keys

### 2.1 Access API Management

1. Log in to https://testnet.bybit.com
2. Click your profile icon (top right)
3. Select **"API Keys"** or **"API Management"**
4. Click **"Create New Key"**

### 2.2 Configure API Key Permissions

**Recommended Permissions for AlgoTrendy:**

- ✅ **Read-Only** - View account info, positions, orders
- ✅ **Trade** - Place, modify, cancel orders
- ✅ **Withdraw** - ❌ **NOT RECOMMENDED** (disable for safety)
- ✅ **Transfer** - ❌ **NOT RECOMMENDED** (disable unless needed)

**IP Whitelist (Optional but Recommended):**
- Add your server/development machine IP address
- Leave blank for testing (less secure but more flexible)

### 2.3 Save API Credentials

After creating the API key, you'll see:
- **API Key** (Public key)
- **API Secret** (Private key - shown only once!)

⚠️ **IMPORTANT:**
- Copy both values immediately
- Store them securely (password manager recommended)
- **Never share or commit API secrets to git**

---

## ⚙️ Step 3: Configure AlgoTrendy

### 3.1 Option A: Using Environment Variables (Recommended)

Edit your `.env` file:

```bash
# Open .env file
nano /root/AlgoTrendy_v2.6/.env

# Add Bybit testnet credentials
BYBIT_API_KEY=your_actual_api_key_here
BYBIT_API_SECRET=your_actual_api_secret_here
BYBIT_TESTNET=true
```

### 3.2 Option B: Using .NET User Secrets (Development)

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Set API Key
dotnet user-secrets set "Bybit:ApiKey" "your_actual_api_key_here"

# Set API Secret
dotnet user-secrets set "Bybit:ApiSecret" "your_actual_api_secret_here"

# Enable testnet mode
dotnet user-secrets set "Bybit:UseTestnet" "true"
```

### 3.3 Option C: Using Azure Key Vault (Production)

For production deployments, store credentials in Azure Key Vault:

```bash
# Store in Azure Key Vault
az keyvault secret set --vault-name "algotrendy-vault" \
  --name "Bybit-ApiKey" \
  --value "your_actual_api_key_here"

az keyvault secret set --vault-name "algotrendy-vault" \
  --name "Bybit-ApiSecret" \
  --value "your_actual_api_secret_here"

az keyvault secret set --vault-name "algotrendy-vault" \
  --name "Bybit-UseTestnet" \
  --value "true"
```

---

## ✅ Step 4: Test Connection

### 4.1 Start AlgoTrendy API

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run
```

### 4.2 Test via Swagger UI

1. Open browser: http://localhost:5002/swagger
2. Navigate to **"Brokers" → "GET /api/brokers/bybit/balance"**
3. Click **"Try it out"**
4. Click **"Execute"**
5. Check response:
   - ✅ **200 OK** - Success! Connection working
   - ❌ **401 Unauthorized** - Check API keys
   - ❌ **403 Forbidden** - Check IP whitelist
   - ❌ **500 Internal Server Error** - Check logs

### 4.3 Test via cURL

```bash
# Test balance endpoint
curl -X GET "http://localhost:5002/api/brokers/bybit/balance" \
  -H "accept: application/json"

# Expected response (example):
{
  "success": true,
  "data": {
    "USDT": {
      "available": 10000.00,
      "locked": 0.00,
      "total": 10000.00
    }
  }
}
```

### 4.4 Test via AlgoTrendy CLI (if available)

```bash
# Test Bybit connection
algotrendy broker test bybit

# Expected output:
✅ Bybit connection successful
✅ Account balance retrieved: 10000 USDT
✅ API key permissions: READ, TRADE
```

---

## 🔒 Security Best Practices

### DO ✅

- ✅ Use testnet for development and testing
- ✅ Store credentials in environment variables or secrets manager
- ✅ Use IP whitelisting when possible
- ✅ Enable only necessary API permissions
- ✅ Rotate API keys regularly (every 30-90 days)
- ✅ Monitor API usage in Bybit dashboard
- ✅ Use separate API keys for different environments

### DON'T ❌

- ❌ Commit API keys to git repositories
- ❌ Share API secrets with others
- ❌ Enable withdrawal permissions unless absolutely necessary
- ❌ Use testnet keys in production
- ❌ Use production keys in testnet
- ❌ Leave API keys in code comments
- ❌ Store API keys in plaintext files

---

## 🐛 Troubleshooting

### Issue: "Unauthorized" Error (401)

**Possible Causes:**
- Invalid API key or secret
- API key not yet activated (wait 5-10 minutes after creation)
- Incorrect testnet mode setting

**Solution:**
```bash
# Verify credentials in .env
cat .env | grep BYBIT

# Check testnet mode is enabled
BYBIT_TESTNET=true

# Regenerate API keys if needed
```

### Issue: "Forbidden" Error (403)

**Possible Causes:**
- IP address not whitelisted
- Insufficient API permissions

**Solution:**
1. Go to Bybit testnet → API Management
2. Check IP whitelist (add your IP or remove restriction)
3. Verify permissions include "Read" and "Trade"

### Issue: "Invalid Signature" Error

**Possible Causes:**
- API secret mismatch
- System time drift
- Special characters in secret not properly encoded

**Solution:**
```bash
# Check system time
date

# Sync system time if needed
sudo ntpdate time.nist.gov

# Regenerate API keys if time sync doesn't help
```

### Issue: Connection Timeout

**Possible Causes:**
- Firewall blocking outbound connections
- DNS resolution issues
- Bybit testnet under maintenance

**Solution:**
```bash
# Test connectivity
ping testnet.bybit.com

# Check if port 443 is accessible
nc -zv testnet-api.bybit.com 443

# Try alternative DNS
sudo systemctl restart systemd-resolved
```

---

## 📊 Testing Checklist

Before using Bybit in AlgoTrendy, verify:

- [ ] Testnet account created and verified
- [ ] API keys generated with correct permissions
- [ ] Credentials configured in AlgoTrendy (.env or user secrets)
- [ ] `BYBIT_TESTNET=true` is set
- [ ] Balance endpoint returns 200 OK
- [ ] Account info endpoint works
- [ ] Testnet funds received (optional)
- [ ] Can view open positions
- [ ] Can place test orders (optional)
- [ ] API usage shows in Bybit dashboard

---

## 🔗 Additional Resources

### Official Documentation
- Bybit Testnet: https://testnet.bybit.com
- Bybit API Docs: https://bybit-exchange.github.io/docs/v5/intro
- Bybit API Guide: https://learn.bybit.com/bybit-guide/how-to-create-a-bybit-api-key/

### AlgoTrendy Documentation
- [Security Policy](../SECURITY.md) - Credential management best practices
- [Quick Setup Guide](../scripts/setup/quick_setup_credentials.sh) - Interactive setup script
- [Broker Integration Tests](../backend/AlgoTrendy.Tests/Integration/Brokers/) - Test examples

### Support
- GitHub Issues: https://github.com/KenyBoi/algotrendy-v2.6/issues
- Security Issues: See [SECURITY.md](../SECURITY.md)

---

## ✅ Next Steps

Once Bybit testnet is configured:

1. ✅ Test with small orders to verify functionality
2. ✅ Review [Trading Strategy Guide](TRADING_STRATEGIES.md) (if available)
3. ✅ Set up additional brokers (Binance, MEXC, etc.)
4. ✅ Configure backtesting with historical data
5. ✅ Run integration tests
6. ✅ Deploy to staging environment

---

## 📝 Notes

### Testnet vs Production

| Feature | Testnet | Production |
|---------|---------|------------|
| **URL** | testnet.bybit.com | www.bybit.com |
| **Funds** | Fake (free) | Real money |
| **API Endpoint** | api-testnet.bybit.com | api.bybit.com |
| **Data** | May differ from production | Real-time market data |
| **Order Execution** | Simulated | Actual trades |
| **Risk** | Zero | Financial risk |

### When to Use Testnet

- ✅ Development and testing
- ✅ Learning trading strategies
- ✅ Verifying new features
- ✅ Integration testing
- ✅ CI/CD pipelines
- ✅ Demo environments

### When to Use Production

- ✅ Live trading (only after extensive testing!)
- ✅ Real portfolio management
- ✅ Actual profit/loss tracking

---

**⚠️ IMPORTANT REMINDER:**

Always test thoroughly on testnet before using production credentials. Never risk real funds without proper testing and validation.

---

**Document Version:** 1.0
**Last Updated:** 2025-10-21
**Maintainer:** AlgoTrendy Development Team
