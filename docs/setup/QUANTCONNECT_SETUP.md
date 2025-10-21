# QuantConnect Setup Guide

## Overview

QuantConnect provides cloud-based backtesting with institutional-grade data and execution accuracy. This guide walks you through setting up QuantConnect integration with AlgoTrendy.

## Prerequisites

- AlgoTrendy v2.6 installed
- Active internet connection
- Web browser

## Step 1: Create QuantConnect Account

1. **Visit QuantConnect**
   - Go to https://www.quantconnect.com
   - Click "Sign Up" in the top right

2. **Register Account**
   - Enter email address
   - Create strong password
   - Verify email address

3. **Complete Profile**
   - Add basic information
   - Accept terms of service

**Cost:** FREE tier includes:
- Unlimited backtests
- 20 years historical data
- Cloud execution
- Community support

## Step 2: Get API Credentials

1. **Login to QuantConnect**
   - Go to https://www.quantconnect.com/account

2. **Navigate to Account Settings**
   - Click your profile icon (top right)
   - Select "Account"

3. **Generate API Token**
   - Scroll to "API Access" section
   - Click "Show API Access"
   - Copy your **User ID** (numeric, e.g., 123456)
   - Click "Create New Token"
   - Copy your **API Token** (long alphanumeric string)

   ```
   User ID: 123456
   API Token: abc123def456ghi789jkl012mno345pqr678stu901vwx234yz
   ```

4. **Save Credentials Securely**
   - Store in password manager
   - Never commit to source control
   - Keep backup copy

## Step 3: Configure AlgoTrendy

### Option A: User Secrets (Development - Recommended)

```bash
# Navigate to API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Set User ID
dotnet user-secrets set "QuantConnect:UserId" "YOUR_USER_ID"

# Set API Token
dotnet user-secrets set "QuantConnect:ApiToken" "YOUR_API_TOKEN"

# Verify secrets are set
dotnet user-secrets list | grep QuantConnect
```

### Option B: Environment Variables (Production)

```bash
# Add to ~/.bashrc or /etc/environment
export QUANTCONNECT_USER_ID="YOUR_USER_ID"
export QUANTCONNECT_API_TOKEN="YOUR_API_TOKEN"

# Reload environment
source ~/.bashrc
```

### Option C: Azure Key Vault (Enterprise)

```bash
# Store in Azure Key Vault
az keyvault secret set --vault-name algotrendy-vault \
  --name "QuantConnect-UserId" --value "YOUR_USER_ID"

az keyvault secret set --vault-name algotrendy-vault \
  --name "QuantConnect-ApiToken" --value "YOUR_API_TOKEN"
```

## Step 4: Test Connection

### Test via API

```bash
# Start the API
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run

# In another terminal, test authentication
curl -X GET "http://localhost:5002/api/backtest/quantconnect/status" \
  -H "Content-Type: application/json"

# Expected response:
# {
#   "status": "connected",
#   "userId": "123456",
#   "authenticated": true
# }
```

### Test via Swagger

1. Open browser: http://localhost:5002/swagger
2. Expand "Backtest" section
3. Try "GET /api/backtest/quantconnect/projects"
4. Click "Try it out" → "Execute"
5. Should return your QuantConnect projects

## Step 5: Create Your First Backtest

### Using AlgoTrendy API

```bash
curl -X POST "http://localhost:5002/api/backtest/quantconnect/run" \
  -H "Content-Type: application/json" \
  -d '{
    "algorithm": "EMA Cross Strategy",
    "symbols": ["BTCUSD"],
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "initialCapital": 10000
  }'

# Response:
# {
#   "backtestId": "abc123",
#   "status": "Running",
#   "estimatedDuration": "2-3 minutes"
# }
```

### Check Results

```bash
# Poll for results
curl -X GET "http://localhost:5002/api/backtest/results/abc123"

# Response when complete:
# {
#   "backtestId": "abc123",
#   "status": "Completed",
#   "totalReturn": 0.25,
#   "sharpeRatio": 1.85,
#   "maxDrawdown": -0.08,
#   "totalTrades": 150
# }
```

## Step 6: Integrate with MEM AI

AlgoTrendy automatically analyzes QuantConnect backtest results with MEM AI:

```json
{
  "backtestId": "abc123",
  "memAnalysis": {
    "accuracy": 0.78,
    "confidence": "high",
    "recommendation": "Strategy shows strong momentum signals",
    "riskLevel": "medium",
    "suggestions": [
      "Consider tightening stop loss to 3%",
      "Increase position size on high-confidence signals"
    ]
  }
}
```

## Configuration Reference

### appsettings.json

```json
{
  "QuantConnect": {
    "UserId": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "ApiToken": "USE_USER_SECRETS_IN_DEVELOPMENT",
    "BaseUrl": "https://www.quantconnect.com/api/v2",
    "DefaultProjectId": null,
    "TimeoutSeconds": 300
  }
}
```

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `QUANTCONNECT_USER_ID` | Your QuantConnect User ID | `123456` |
| `QUANTCONNECT_API_TOKEN` | Your API token | `abc123...` |
| `QUANTCONNECT_BASE_URL` | API endpoint (optional) | `https://www.quantconnect.com/api/v2` |

## Troubleshooting

### Error: "Invalid API credentials"

**Solution:**
```bash
# Verify credentials are set
dotnet user-secrets list | grep QuantConnect

# Check format (User ID should be numeric)
echo $QUANTCONNECT_USER_ID

# Re-set if needed
dotnet user-secrets set "QuantConnect:UserId" "123456"
dotnet user-secrets set "QuantConnect:ApiToken" "your_token_here"
```

### Error: "Rate limit exceeded"

**Solution:**
- QuantConnect free tier: 20 backtests/day
- Wait 1 hour between backtests
- Upgrade to paid tier for higher limits

### Error: "Backtest failed to compile"

**Solution:**
- Check algorithm syntax
- Verify symbol format (use QuantConnect symbols)
- Review logs: `/root/AlgoTrendy_v2.6/logs/algotrendy-*.log`

### Connection timeout

**Solution:**
```bash
# Increase timeout in appsettings.json
"QuantConnect": {
  "TimeoutSeconds": 600  // 10 minutes
}
```

## API Rate Limits

| Tier | Backtests/Day | Cloud Nodes | Storage |
|------|---------------|-------------|---------|
| Free | 20 | 1 | 1 GB |
| Quant Researcher | 100 | 2 | 10 GB |
| Professional | Unlimited | 4 | 50 GB |

## Security Best Practices

1. **Never commit credentials** to source control
2. **Use user secrets** in development
3. **Use Azure Key Vault** in production
4. **Rotate API tokens** every 90 days
5. **Monitor API usage** for anomalies

### Check for leaked credentials

```bash
# Scan codebase for hardcoded tokens
grep -r "QUANTCONNECT_API_TOKEN" . --exclude-dir=node_modules --exclude-dir=bin --exclude-dir=obj

# Should return: 0 results
```

## Advanced Features

### Custom Algorithms

Upload custom C# algorithms to QuantConnect:

```csharp
public class MyStrategy : QCAlgorithm
{
    public override void Initialize()
    {
        SetStartDate(2024, 1, 1);
        SetEndDate(2024, 12, 31);
        SetCash(10000);
        AddCrypto("BTCUSD", Resolution.Hour);
    }

    public override void OnData(Slice data)
    {
        // Your strategy logic
    }
}
```

### Parameter Optimization

Run parameter sweeps:

```bash
curl -X POST "http://localhost:5002/api/backtest/quantconnect/optimize" \
  -H "Content-Type: application/json" \
  -d '{
    "algorithm": "EMA Cross",
    "parameters": {
      "fastPeriod": [10, 20, 30],
      "slowPeriod": [50, 100, 200]
    }
  }'
```

## Support Resources

- **QuantConnect Docs:** https://www.quantconnect.com/docs
- **Community Forum:** https://www.quantconnect.com/forum
- **API Reference:** https://www.quantconnect.com/docs/v2
- **AlgoTrendy Docs:** `/docs/README.md`

## Next Steps

1. ✅ Set up credentials
2. ✅ Test connection
3. ✅ Run first backtest
4. 📊 Review MEM AI analysis
5. 🚀 Deploy winning strategies to live trading

## Updates

**Last Updated:** October 21, 2025
**Tested With:** QuantConnect API v2
**AlgoTrendy Version:** 2.6.0
