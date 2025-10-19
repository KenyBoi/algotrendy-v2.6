# AlgoTrendy v2.6 - API Credentials Setup Guide

**Document Version:** 1.0
**Created:** October 19, 2025
**Status:** Required for Production Deployment

---

## 🔑 Overview

This guide provides step-by-step instructions for obtaining and configuring exchange API credentials required for AlgoTrendy v2.6 to function.

**Current Status:**
- ✅ .env file created with secure system keys
- ⚠️ Exchange API credentials required (user must provide)

---

## 📋 Required API Credentials

### 1. Binance API Credentials (Primary Exchange)

**Status:** ⚠️ **REQUIRED** - System cannot trade without these

**Where to Get:**
1. Visit: https://www.binance.com/en/account/api-management
2. Log in to your Binance account
3. Click "Create API" button
4. Complete 2FA verification
5. Name your API key (e.g., "AlgoTrendy-Production")

**Required Permissions:**
- ✅ Enable Reading
- ✅ Enable Spot & Margin Trading
- ❌ Disable Withdrawals (for security)

**IP Whitelist (Recommended):**
- Add your server IP: `curl ifconfig.me` to get your IP
- In Binance API settings, click "Restrict access to trusted IPs"
- Add your VPS IP address

**Get Your Credentials:**
```
API Key: [32+ character alphanumeric string]
Secret Key: [64+ character string - shown only once, save it!]
```

**Add to .env file:**
```bash
cd /root/AlgoTrendy_v2.6
nano .env

# Find these lines and replace with your values:
BINANCE_API_KEY=your_actual_binance_api_key_here
BINANCE_API_SECRET=your_actual_binance_secret_here
BINANCE_TESTNET=false  # Set to 'true' for testing, 'false' for production
```

**Security Notes:**
- ⚠️ NEVER share your Secret Key
- ⚠️ Secret Key is shown ONLY ONCE - save it immediately
- ⚠️ If you lose it, you must delete the API key and create a new one
- ⚠️ Enable IP whitelist for maximum security

---

### 2. Binance Testnet (Optional but Recommended for Testing)

**Status:** ⚠️ **RECOMMENDED** - Test before using real funds

**Where to Get:**
1. Visit: https://testnet.binance.vision/
2. Log in with your GitHub or email
3. Click "Generate HMAC_SHA256 Key"
4. Copy both API Key and Secret Key

**Testnet Features:**
- Free virtual funds for testing
- No real money at risk
- Test all trading features
- Practice before production

**Add to .env file for testing:**
```bash
BINANCE_API_KEY=your_testnet_api_key
BINANCE_API_SECRET=your_testnet_secret
BINANCE_TESTNET=true  # IMPORTANT: Set to true for testnet
```

**Switch to Production:**
```bash
# Once tested, switch to real credentials:
BINANCE_API_KEY=your_production_api_key
BINANCE_API_SECRET=your_production_secret
BINANCE_TESTNET=false  # Set to false for real trading
```

---

### 3. Optional Exchange Credentials

The following exchanges are supported but NOT required for initial deployment:

#### OKX (Optional)

**Where to Get:** https://www.okx.com/account/my-api

**Add to .env:**
```bash
OKX_API_KEY=your_okx_api_key
OKX_API_SECRET=your_okx_secret
OKX_PASSPHRASE=your_okx_passphrase
OKX_TESTNET=false
```

#### Coinbase (Optional)

**Where to Get:** https://www.coinbase.com/settings/api

**Add to .env:**
```bash
COINBASE_API_KEY=your_coinbase_api_key
COINBASE_API_SECRET=your_coinbase_secret
COINBASE_SANDBOX=false
```

#### Kraken (Optional)

**Where to Get:** https://www.kraken.com/u/security/api

**Add to .env:**
```bash
KRAKEN_API_KEY=your_kraken_api_key
KRAKEN_API_SECRET=your_kraken_secret
```

---

## ✅ Pre-Configured System Credentials

The following have been automatically generated with secure random values:

### Security Keys ✅

```bash
JWT_SECRET_KEY=*** (Generated - 256-bit secure key)
ENCRYPTION_KEY=*** (Generated - 256-bit hex key)
QUESTDB_PASSWORD=*** (Generated - secure password)
```

### Application Settings ✅

```bash
ENVIRONMENT=production
DEBUG_MODE=false
LOG_LEVEL=Warning
```

**These do NOT need to be changed** - they are production-ready.

---

## 🔐 Security Best Practices

### DO ✅

1. **Enable IP Whitelist**
   - Restrict API access to your VPS IP only
   - Reduces risk of unauthorized access

2. **Limit Permissions**
   - Enable ONLY trading and reading
   - NEVER enable withdrawals

3. **Use Testnet First**
   - Test with Binance Testnet before production
   - Verify all features work correctly

4. **Secure .env File**
   - Permissions already set to 600 (read/write owner only)
   - Never commit .env to git (already in .gitignore)

5. **Regular Rotation**
   - Rotate API keys every 90 days
   - Delete unused API keys

6. **Monitor Activity**
   - Check Binance API logs regularly
   - Set up email alerts for API usage

### DON'T ❌

1. **Never Enable Withdrawals**
   - Trading bot should not be able to withdraw funds
   - Manual withdrawals only

2. **Never Share Credentials**
   - Don't share in chat/email
   - Don't commit to version control
   - Don't store in plain text elsewhere

3. **Don't Use Same Keys Across Systems**
   - Create separate API keys for each bot/system
   - Easier to track and revoke if needed

4. **Don't Skip IP Whitelist**
   - Always restrict to known IPs
   - Adds critical security layer

---

## 🧪 Testing Your Credentials

After adding Binance credentials to .env:

### 1. Verify .env File

```bash
# Check file exists and has correct permissions
ls -la /root/AlgoTrendy_v2.6/.env
# Should show: -rw------- (600 permissions)

# Verify credentials are set (shows first few characters only)
grep "BINANCE_API_KEY" /root/AlgoTrendy_v2.6/.env | cut -c1-30
# Should show: BINANCE_API_KEY=<your_key>
```

### 2. Test with Binance API (Optional)

```bash
# Quick test that API key is valid (read-only test)
API_KEY="your_api_key_here"
curl -H "X-MBX-APIKEY: $API_KEY" \
  https://api.binance.com/api/v3/account

# Should return account info or error message
```

### 3. Start AlgoTrendy and Check Logs

```bash
# Start services
cd /root/AlgoTrendy_v2.6
docker-compose -f docker-compose.prod.yml up -d

# Check API logs for Binance connection
docker-compose logs api | grep -i binance

# Expected output:
# "Binance broker configured for PRODUCTION"
# or
# "Binance broker configured for TESTNET"
```

---

## 📝 Quick Setup Checklist

- [ ] Visit Binance API management page
- [ ] Create new API key with trading permissions
- [ ] Save API Key and Secret Key (Secret shown only once!)
- [ ] Configure IP whitelist (recommended)
- [ ] Add credentials to /root/AlgoTrendy_v2.6/.env file
- [ ] Set BINANCE_TESTNET=true for testing or false for production
- [ ] Verify .env has 600 permissions
- [ ] Test connection by starting AlgoTrendy
- [ ] Check logs for successful Binance connection

---

## 🔄 Credential Rotation

Rotate your API keys every 90 days:

### Rotation Procedure

1. **Create New API Key** on Binance
2. **Update .env file** with new credentials
3. **Restart services:**
   ```bash
   docker-compose -f docker-compose.prod.yml restart
   ```
4. **Verify new credentials work** (check logs)
5. **Delete old API key** on Binance
6. **Document rotation** in maintenance log

### Set Reminder

```bash
# Add calendar reminder
echo "Rotate Binance API keys" | at now + 90 days
```

---

## 🆘 Troubleshooting

### Problem: "Invalid API Key"

**Causes:**
- API key copied incorrectly (extra spaces, missing characters)
- API key not enabled on Binance
- IP whitelist doesn't include server IP

**Solutions:**
```bash
# 1. Verify no extra spaces
grep "BINANCE_API_KEY" .env | cat -A
# Should not show spaces before/after the key

# 2. Check your server IP
curl ifconfig.me
# Add this IP to Binance API whitelist

# 3. Regenerate API key if needed
```

### Problem: "Insufficient Permissions"

**Cause:** API key doesn't have trading permissions

**Solution:**
- Go to Binance API management
- Edit API key settings
- Enable "Spot & Margin Trading"
- Save changes

### Problem: "Timestamp Error"

**Cause:** Server time not synchronized

**Solution:**
```bash
# Sync server time
sudo timedatectl set-ntp true
sudo timedatectl status
```

### Problem: Credentials Work in Testnet but Not Production

**Cause:** Forgot to change BINANCE_TESTNET setting

**Solution:**
```bash
# Edit .env file
nano .env

# Change:
BINANCE_TESTNET=true

# To:
BINANCE_TESTNET=false

# Restart services
docker-compose restart
```

---

## 📞 Support Resources

### Binance Resources

- **API Documentation:** https://binance-docs.github.io/apidocs/
- **API Management:** https://www.binance.com/en/account/api-management
- **Testnet:** https://testnet.binance.vision/
- **Support:** https://www.binance.com/en/support

### AlgoTrendy Resources

- **Deployment Guide:** `/root/AlgoTrendy_v2.6/DEPLOYMENT_DOCKER.md`
- **Configuration:** `/root/AlgoTrendy_v2.6/.env.example`
- **Logs:** `docker-compose logs api`

---

## 🎯 Current Status

### ✅ Completed

- Docker Compose installed (v2.40.1)
- .env file created from template
- System security keys generated
- Production environment configured
- File permissions secured (600)

### ⚠️ Required: User Action Needed

**You must obtain and add Binance API credentials before deployment can proceed.**

**Estimated Time:** 10-15 minutes

**Steps:**
1. Visit Binance API page (5 min)
2. Create API key (2 min)
3. Configure IP whitelist (3 min)
4. Add to .env file (2 min)
5. Test connection (3 min)

**After credentials are added, you can proceed with deployment!**

---

**Last Updated:** October 19, 2025
**Status:** Awaiting User Credentials
**Next Step:** Add Binance API credentials to /root/AlgoTrendy_v2.6/.env
