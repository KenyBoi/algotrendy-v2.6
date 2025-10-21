# AlgoTrendy v2.6 - Credentials & Integration Setup Guide

**Generated:** 2025-10-20
**Purpose:** Complete guide for setting up API credentials and third-party integrations

---

## Table of Contents

1. [Overview](#overview)
2. [Credential Storage Methods](#credential-storage-methods)
3. [Required Integrations](#required-integrations)
4. [Migration from v2.5](#migration-from-v25)
5. [Security Best Practices](#security-best-practices)
6. [Verification & Testing](#verification--testing)

---

## Overview

AlgoTrendy v2.6 supports multiple credential storage methods:
- ✅ **User Secrets** (Recommended for development)
- ✅ **Environment Variables** (Recommended for production)
- ✅ **Azure Key Vault** (Enterprise production)
- ❌ **Hardcoded in appsettings.json** (NEVER do this for production)

---

## Credential Storage Methods

### Method 1: .NET User Secrets (Development)

**Recommended for local development**

```bash
# Navigate to API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Initialize user secrets
dotnet user-secrets init

# Set QuantConnect credentials
dotnet user-secrets set "QuantConnect:UserId" "your-user-id"
dotnet user-secrets set "QuantConnect:ApiToken" "your-api-token"

# Set broker credentials (example: Bybit)
dotnet user-secrets set "Brokers:Bybit:ApiKey" "your-bybit-api-key"
dotnet user-secrets set "Brokers:Bybit:ApiSecret" "your-bybit-api-secret"

# Set PostgreSQL credentials
dotnet user-secrets set "ConnectionStrings:AlgoTrendyDb" "Host=localhost;Database=algotrendy_v26;Username=algotrendy;Password=your-password"

# Set QuestDB credentials
dotnet user-secrets set "QuestDB:Username" "admin"
dotnet user-secrets set "QuestDB:Password" "quest"
```

### Method 2: Environment Variables (Production)

**Add to ~/.bashrc or system environment:**

```bash
# QuantConnect
export QUANTCONNECT_USERID="your-user-id"
export QUANTCONNECT_APITOKEN="your-api-token"

# Bybit
export BYBIT_API_KEY="your-api-key"
export BYBIT_API_SECRET="your-api-secret"
export BYBIT_TESTNET="false"

# MEXC
export MEXC_API_KEY="your-api-key"
export MEXC_API_SECRET="your-api-secret"

# Binance
export BINANCE_API_KEY="your-api-key"
export BINANCE_API_SECRET="your-api-secret"

# OKX
export OKX_API_KEY="your-api-key"
export OKX_API_SECRET="your-api-secret"
export OKX_PASSPHRASE="your-passphrase"

# Alpaca
export ALPACA_API_KEY="your-api-key"
export ALPACA_API_SECRET="your-api-secret"

# Database
export DB_PASSWORD="your-postgres-password"
export QUESTDB_PASSWORD="quest"

# Apply changes
source ~/.bashrc
```

### Method 3: Azure Key Vault (Enterprise Production)

```csharp
// Already configured in Program.cs
// Add secrets to Azure Key Vault:
// - QuantConnect--UserId
// - QuantConnect--ApiToken
// - Brokers--Bybit--ApiKey
// - Brokers--Bybit--ApiSecret
```

---

## Required Integrations

### 1. QuantConnect (Backtesting)

**Status:** ✅ Fully integrated
**Purpose:** Advanced backtesting platform
**Files:**
- `backend/AlgoTrendy.Backtesting/Services/QuantConnectApiClient.cs`
- `backend/AlgoTrendy.Backtesting/Engines/QuantConnectBacktestEngine.cs`
- `backend/AlgoTrendy.API/Controllers/QuantConnectController.cs`

**Setup:**

1. Create account at https://www.quantconnect.com
2. Get API credentials from https://www.quantconnect.com/account
3. Add credentials using one of the methods above

**Configuration in appsettings.json:**
```json
"QuantConnect": {
  "UserId": "USE_USER_SECRETS_IN_DEVELOPMENT",
  "ApiToken": "USE_USER_SECRETS_IN_DEVELOPMENT",
  "BaseUrl": "https://www.quantconnect.com/api/v2",
  "DefaultProjectId": null,
  "TimeoutSeconds": 300
}
```

---

### 2. Broker APIs

#### Bybit (Primary Broker)

**Status:** ✅ Fully integrated
**Files:**
- `backend/AlgoTrendy.TradingEngine/Brokers/BybitBroker.cs`
- `backend/AlgoTrendy.TradingEngine/Brokers/BybitClient.cs`

**Setup:**

1. Create account at https://www.bybit.com
2. Enable API access in account settings
3. Generate API Key and Secret
4. Set IP whitelist (recommended)
5. Add credentials via user secrets or environment variables

**Testnet Setup:**
```bash
# For testnet testing
export BYBIT_TESTNET="true"
export BYBIT_API_KEY="testnet-api-key"
export BYBIT_API_SECRET="testnet-api-secret"
```

#### MEXC

**Status:** ✅ Fully integrated
**Files:**
- `backend/AlgoTrendy.TradingEngine/Brokers/MEXCBroker.cs`
- `backend/AlgoTrendy.TradingEngine/Brokers/MEXCClient.cs`

**Setup:**

1. Create account at https://www.mexc.com
2. Complete KYC verification
3. Generate API credentials
4. Add to user secrets or environment variables

#### Binance, OKX, Alpaca, Kraken

**Status:** ⚠️ Partially integrated
**Action Required:** Complete implementation or use existing interfaces

**Configuration in appsettings.json:**
```json
"Brokers": {
  "Bybit": {
    "ApiKey": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "ApiSecret": "USE_USER_SECRETS_IN_DEVELOPMENT"
  },
  "MEXC": {
    "ApiKey": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "ApiSecret": "USE_USER_SECRETS_IN_DEVELOPMENT"
  },
  "Binance": {
    "ApiKey": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "ApiSecret": "USE_USER_SECRETS_IN_DEVELOPMENT"
  }
}
```

---

### 3. Database Connections

#### PostgreSQL (Primary Database)

**Configuration:**
```json
"ConnectionStrings": {
  "AlgoTrendyDb": "Host=localhost;Database=algotrendy_v26;Username=algotrendy;Password=USE_USER_SECRETS_IN_DEVELOPMENT"
}
```

**User Secrets Setup:**
```bash
dotnet user-secrets set "ConnectionStrings:AlgoTrendyDb" "Host=localhost;Database=algotrendy_v26;Username=algotrendy;Password=your-password"
```

#### QuestDB (Time-Series Data)

**Configuration:**
```json
"QuestDB": {
  "HttpUrl": "http://localhost:9000",
  "InfluxUrl": "http://localhost:9009",
  "Username": "admin",
  "Password": "USE_USER_SECRETS_IN_DEVELOPMENT"
}
```

**User Secrets Setup:**
```bash
dotnet user-secrets set "QuestDB:Password" "quest"
```

---

### 4. Market Data Providers

#### Alpha Vantage

**Free tier:** 25 requests/day
**Get key:** https://www.alphavantage.co/support/#api-key

```bash
dotnet user-secrets set "MarketData:AlphaVantage:ApiKey" "your-api-key"
```

#### Finnhub

**Free tier:** 60 calls/minute
**Get key:** https://finnhub.io/register

```bash
dotnet user-secrets set "MarketData:Finnhub:ApiKey" "your-api-key"
```

#### Financial Modeling Prep

**Get key:** https://site.financialmodelingprep.com/

```bash
dotnet user-secrets set "MarketData:FinancialModelingPrep:ApiKey" "your-api-key"
```

#### Twelve Data

**Get key:** https://twelvedata.com/

```bash
dotnet user-secrets set "MarketData:TwelveData:ApiKey" "your-api-key"
```

---

## Migration from v2.5

### What Changed

v2.5 used a Python-based credential system (`secure_credentials.py`) with environment variables.
v2.6 uses .NET User Secrets and Azure Key Vault for better .NET integration.

### Migration Steps

#### Step 1: Export v2.5 Environment Variables

```bash
# If you have credentials in v2.5, export them
env | grep -E "BYBIT|MEXC|QUANTCONNECT|BINANCE" > /tmp/v25_credentials.txt
```

#### Step 2: Import to v2.6 User Secrets

```bash
# Navigate to v2.6 API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Set credentials from v2.5
# Example for Bybit:
dotnet user-secrets set "Brokers:Bybit:ApiKey" "$BYBIT_API_KEY"
dotnet user-secrets set "Brokers:Bybit:ApiSecret" "$BYBIT_API_SECRET"
```

#### Step 3: Update Configuration Files

v2.6 broker configuration is located at:
- `/root/AlgoTrendy_v2.6/debt_mgmt_module/config/broker_config.json`
- `/root/AlgoTrendy_v2.6/backend/AlgoTrendy.API/appsettings.json`

**Note:** Do NOT put actual credentials in these JSON files. Use user secrets or environment variables.

#### Step 4: Verify Migration

```bash
# Build the project
cd /root/AlgoTrendy_v2.6/backend
dotnet build

# Test credential loading (requires custom test)
dotnet run --project AlgoTrendy.API --urls="http://localhost:5000"
```

---

## Security Best Practices

### ✅ DO

1. **Use User Secrets for development**
   ```bash
   dotnet user-secrets set "key" "value"
   ```

2. **Use Environment Variables for production**
   ```bash
   export KEY="value"
   ```

3. **Use Azure Key Vault for enterprise**
   - Already integrated in `Program.cs`
   - Set KeyVault URL in environment

4. **Rotate credentials regularly**
   - Bybit: Every 90 days
   - QuantConnect: Every 180 days

5. **Use IP whitelisting**
   - Configure in broker account settings
   - Restrict API access to known IPs

6. **Enable 2FA on all accounts**
   - Broker accounts
   - QuantConnect account
   - Database admin accounts

### ❌ DON'T

1. **Never commit credentials to Git**
   ```bash
   # Already in .gitignore:
   appsettings.*.json
   *.env
   secrets.json
   ```

2. **Never hardcode in source files**
   ```csharp
   // ❌ BAD
   var apiKey = "hardcoded-key";

   // ✅ GOOD
   var apiKey = _configuration["Brokers:Bybit:ApiKey"];
   ```

3. **Never share credentials in Slack/Discord**
   - Use secure password managers
   - Share via encrypted channels only

4. **Never use production credentials in tests**
   - Use testnet/sandbox environments
   - Use mock credentials in unit tests

---

## Verification & Testing

### Check User Secrets

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet user-secrets list
```

### Check Environment Variables

```bash
env | grep -E "BYBIT|MEXC|QUANTCONNECT|DB_PASSWORD"
```

### Test QuantConnect Connection

```bash
# Run the API and check logs
cd /root/AlgoTrendy_v2.6/backend
dotnet run --project AlgoTrendy.API

# Check startup logs for configuration validation
```

### Test Broker Connections

```csharp
// Use the built-in health checks
GET /health
GET /api/v1/brokers/health
```

### Verify Build

```bash
cd /root/AlgoTrendy_v2.6/backend
dotnet build
# Should show: Build succeeded. 0 Error(s)
```

---

## Quick Setup Script

```bash
#!/bin/bash
# quick_setup_credentials.sh

cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Initialize user secrets
dotnet user-secrets init

# QuantConnect (get from https://www.quantconnect.com/account)
read -p "QuantConnect User ID: " qc_user
read -s -p "QuantConnect API Token: " qc_token
echo
dotnet user-secrets set "QuantConnect:UserId" "$qc_user"
dotnet user-secrets set "QuantConnect:ApiToken" "$qc_token"

# Bybit (get from https://www.bybit.com)
read -p "Bybit API Key: " bybit_key
read -s -p "Bybit API Secret: " bybit_secret
echo
dotnet user-secrets set "Brokers:Bybit:ApiKey" "$bybit_key"
dotnet user-secrets set "Brokers:Bybit:ApiSecret" "$bybit_secret"

# Database
read -s -p "PostgreSQL Password: " db_pass
echo
dotnet user-secrets set "ConnectionStrings:AlgoTrendyDb" "Host=localhost;Database=algotrendy_v26;Username=algotrendy;Password=$db_pass"

echo "✅ Credentials configured successfully!"
echo "Verify with: dotnet user-secrets list"
```

---

## Current Integration Status

| Integration | Status | Config Location | Credentials Required |
|------------|--------|-----------------|---------------------|
| **QuantConnect** | ✅ Complete | appsettings.json:195-202 | UserId, ApiToken |
| **Bybit** | ✅ Complete | appsettings.json:76-79 | ApiKey, ApiSecret |
| **MEXC** | ✅ Complete | appsettings.json:81-84 | ApiKey, ApiSecret |
| **Binance** | ⚠️ Partial | appsettings.json:85-88 | ApiKey, ApiSecret |
| **OKX** | ⚠️ Partial | appsettings.json:90-93 | ApiKey, ApiSecret, Passphrase |
| **Alpaca** | ⚠️ Partial | appsettings.json:96-99 | ApiKey, ApiSecret |
| **PostgreSQL** | ✅ Complete | appsettings.json:4-6 | Connection String |
| **QuestDB** | ✅ Complete | appsettings.json:106-108 | Username, Password |
| **Redis** | ✅ Complete | appsettings.json:121-124 | Password (optional) |

---

## Support & Troubleshooting

### Common Issues

1. **"Configuration not found" errors**
   - Check user secrets are set: `dotnet user-secrets list`
   - Verify environment variables: `env | grep KEY_NAME`

2. **"Authentication failed" errors**
   - Verify credentials are correct
   - Check API key permissions in broker account
   - Ensure IP whitelist includes your server IP

3. **Build errors**
   - Run: `dotnet build` and check output
   - Ensure all packages restored: `dotnet restore`

### Getting Help

- Check logs: `/root/AlgoTrendy_v2.6/logs/`
- Review documentation: `/root/AlgoTrendy_v2.6/docs/`
- API documentation: https://docs.quantconnect.com (for QuantConnect)

---

## Files Reference

### v2.6 Configuration Files
```
/root/AlgoTrendy_v2.6/
├── backend/AlgoTrendy.API/
│   ├── appsettings.json                    # Main config (NO SECRETS!)
│   ├── appsettings.Development.json         # Dev overrides
│   └── appsettings.Compliance.json          # Compliance settings
├── debt_mgmt_module/config/
│   └── broker_config.json                   # Broker settings (NO SECRETS!)
└── CREDENTIALS_SETUP_GUIDE.md              # This file

User Secrets Location:
~/.microsoft/usersecrets/<project-id>/secrets.json
```

### v2.5 Legacy Files (Reference Only)
```
/root/algotrendy_v2.5/
├── algotrendy/secure_credentials.py         # Python credential manager
├── broker_config.json                       # Old broker config
└── algotrendy-api/.env                      # Python API env vars
```

---

**Last Updated:** 2025-10-20
**Version:** 2.6.0
**Author:** AlgoTrendy Development Team
