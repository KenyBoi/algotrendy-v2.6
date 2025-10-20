# QuantConnect Integration Guide

**AlgoTrendy v2.6 - Complete Backtesting Solution**

## Overview

AlgoTrendy supports **THREE backtesting engines**, giving you flexibility based on your needs:

1. **QuantConnect Cloud** - Professional-grade cloud backtesting
2. **Local LEAN** - Run LEAN engine locally in Docker (free, private)
3. **Custom Engine** - AlgoTrendy's built-in backtesting (lightweight, fast)

---

## Quick Start

### Option A: QuantConnect Cloud (Recommended for Production)

**Pros:** Professional data, proven infrastructure, zero local resources
**Cons:** Requires account ($0-$20/month), external dependency

#### Setup Steps:

1. **Create QuantConnect Account**
   ```bash
   # Visit: https://www.quantconnect.com
   # Sign up for free account
   # Go to Account → API Access
   # Copy your User ID and API Token
   ```

2. **Configure Credentials**
   ```bash
   # Copy example file
   cp .env.quantconnect.example .env.quantconnect

   # Edit and add your credentials
   nano .env.quantconnect
   ```

   ```bash
   # .env.quantconnect
   QUANTCONNECT_USER_ID=123456
   QUANTCONNECT_API_TOKEN=your_api_token_here
   BACKTEST_ENGINE=cloud
   ```

3. **Load Environment Variables**
   ```bash
   source .env.quantconnect
   export $(cat .env.quantconnect | xargs)
   ```

4. **Test Authentication**
   ```bash
   curl http://localhost:5002/api/v1/quantconnect/auth/test

   # Expected response:
   # {"success":true,"message":"Successfully authenticated with QuantConnect API"}
   ```

5. **Run Your First Backtest**
   ```bash
   curl -X POST http://localhost:5002/api/v1/quantconnect/backtest \
     -H "Content-Type: application/json" \
     -d '{
       "symbol": "BTCUSD",
       "startDate": "2024-01-01",
       "endDate": "2024-10-01",
       "initialCapital": 100000,
       "timeframe": "Hour"
     }'
   ```

---

### Option B: Local LEAN Engine (Best for Development)

**Pros:** Free, private, no rate limits, full control
**Cons:** Requires Docker, uses local resources, data quality depends on sources

#### Setup Steps:

1. **Verify Docker is Installed**
   ```bash
   docker --version
   # Should show: Docker version 20.x+
   ```

2. **Build LEAN Docker Image**
   ```bash
   cd /root/AlgoTrendy_v2.6/docker/lean
   docker build -t algotrendy/lean:latest .
   ```

3. **Configure Local LEAN**
   ```bash
   # Set environment variables
   export LEAN_ALGORITHMS_PATH=/tmp/algotrendy/lean/algorithms
   export LEAN_RESULTS_PATH=/tmp/algotrendy/lean/results
   export LEAN_DATA_PATH=/tmp/algotrendy/lean/data
   export BACKTEST_ENGINE=local
   ```

4. **Enable LEAN Service in Docker Compose** (Optional)
   ```bash
   # Edit docker-compose.yml
   nano docker-compose.yml

   # Uncomment the "lean:" service section (lines 114-135)
   ```

5. **Test Local LEAN**
   ```bash
   # Check available engines
   curl http://localhost:5002/api/v1/backtesting/engines

   # Expected response includes:
   # {"name":"LocalLEAN","description":"Local LEAN engine...","available":true}
   ```

6. **Run Backtest with Local Engine**
   ```bash
   curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
     -H "Content-Type: application/json" \
     -d '{
       "config": {
         "symbol": "BTCUSD",
         "startDate": "2024-01-01",
         "endDate": "2024-10-01",
         "initialCapital": 100000,
         "timeframe": "Hour"
       },
       "engineType": "Local"
     }'
   ```

---

### Option C: Auto-Select (Best of Both Worlds)

The **Auto** engine intelligently selects the best available engine:

1. **Tries Local LEAN** first (if Docker available)
2. **Falls back to Custom** engine (if Local unavailable)
3. **Falls back to Cloud** (if configured)

#### Setup:

```bash
export BACKTEST_ENGINE=auto
```

#### Usage:

```bash
curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
  -H "Content-Type: application/json" \
  -d '{
    "config": {
      "symbol": "BTCUSD",
      "startDate": "2024-01-01",
      "endDate": "2024-10-01"
    },
    "engineType": "Auto"
  }'
```

---

## API Reference

### QuantConnect Cloud Endpoints

#### 1. Test Authentication
```bash
GET /api/v1/quantconnect/auth/test
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully authenticated with QuantConnect API"
}
```

---

#### 2. Run Backtest on Cloud
```bash
POST /api/v1/quantconnect/backtest
```

**Request Body:**
```json
{
  "symbol": "BTCUSD",
  "startDate": "2024-01-01",
  "endDate": "2024-10-01",
  "initialCapital": 100000,
  "timeframe": "Hour",
  "assetClass": "Crypto"
}
```

**Response:**
```json
{
  "backtestId": "abc-123-def",
  "status": "Completed",
  "metrics": {
    "totalReturn": 25.5,
    "sharpeRatio": 1.8,
    "maxDrawdown": -12.3,
    "totalTrades": 45,
    "winRate": 62.2
  }
}
```

---

#### 3. Run Backtest with MEM Analysis
```bash
POST /api/v1/quantconnect/backtest/with-analysis
```

Runs backtest + AI-powered analysis from Machine Learning service.

---

#### 4. Export Strategy to QuantConnect
```bash
POST /api/v1/quantconnect/strategy/export
```

**Request Body:**
```json
{
  "strategyCode": "using QuantConnect...",
  "projectName": "MyStrategy"
}
```

---

### Backtest Engine Selection Endpoints

#### 1. Get Available Engines
```bash
GET /api/v1/backtesting/engines
```

**Response:**
```json
[
  {
    "name": "QuantConnect",
    "description": "Professional-grade cloud backtesting",
    "available": true
  },
  {
    "name": "LocalLEAN",
    "description": "Local LEAN engine running in Docker",
    "available": true
  },
  {
    "name": "Custom",
    "description": "AlgoTrendy built-in engine",
    "available": true
  }
]
```

---

#### 2. Run Backtest with Specific Engine
```bash
POST /api/v1/backtesting/run/with-engine
```

**Request Body:**
```json
{
  "config": {
    "symbol": "BTCUSD",
    "startDate": "2024-01-01",
    "endDate": "2024-10-01",
    "initialCapital": 100000,
    "timeframe": "Hour"
  },
  "engineType": "Cloud"
}
```

**Engine Types:**
- `"Cloud"` - QuantConnect Cloud
- `"Local"` - Local LEAN Docker
- `"Custom"` - AlgoTrendy engine
- `"Auto"` - Auto-select best available

---

## Configuration Reference

### Environment Variables

```bash
# QuantConnect Cloud
QUANTCONNECT_USER_ID=your_user_id
QUANTCONNECT_API_TOKEN=your_api_token
QUANTCONNECT_BASE_URL=https://www.quantconnect.com/api/v2
QUANTCONNECT_TIMEOUT_SECONDS=300

# Local LEAN Engine
LEAN_ALGORITHMS_PATH=/tmp/algotrendy/lean/algorithms
LEAN_RESULTS_PATH=/tmp/algotrendy/lean/results
LEAN_DATA_PATH=/tmp/algotrendy/lean/data
LEAN_IMAGE_NAME=algotrendy/lean:latest
LEAN_MAX_EXECUTION_MINUTES=30

# Engine Selection
BACKTEST_ENGINE=auto  # Options: cloud, local, custom, auto
BACKTEST_ALLOW_CLOUD_FALLBACK=true
```

---

### appsettings.json

```json
{
  "QuantConnect": {
    "UserId": "",
    "ApiToken": "",
    "BaseUrl": "https://www.quantconnect.com/api/v2",
    "TimeoutSeconds": 300
  },
  "LocalLEAN": {
    "AlgorithmsPath": "/tmp/algotrendy/lean/algorithms",
    "ResultsPath": "/tmp/algotrendy/lean/results",
    "DataPath": "/tmp/algotrendy/lean/data",
    "ContainerName": "algotrendy-lean",
    "ImageName": "algotrendy/lean:latest",
    "MaxExecutionMinutes": 30,
    "Enabled": true
  },
  "Backtest": {
    "DefaultEngine": "auto",
    "AllowCloudFallback": true
  }
}
```

---

## Architecture

### How It Works

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│           AlgoTrendy API                            │
│                                                     │
│  ┌─────────────────────────────────────────────┐  │
│  │  BacktestEngineFactory                       │  │
│  │  (Auto-selects based on config)              │  │
│  └──────────┬────────┬────────┬─────────────────┘  │
│             │        │        │                     │
│      ┌──────▼─┐  ┌──▼───┐  ┌─▼──────┐              │
│      │ Cloud  │  │Local │  │Custom  │              │
│      │Engine  │  │LEAN  │  │Engine  │              │
│      └──┬─────┘  └──┬───┘  └────────┘              │
│         │           │                               │
└─────────┼───────────┼───────────────────────────────┘
          │           │
          │           │
     ┌────▼────┐  ┌──▼────────────┐
     │ QuantCon│  │ Docker        │
     │ Cloud   │  │ (Local LEAN)  │
     └─────────┘  └───────────────┘
```

---

## Comparison: Cloud vs Local vs Custom

| Feature | QuantConnect Cloud | Local LEAN | Custom Engine |
|---------|-------------------|------------|---------------|
| **Cost** | $0-$20/month | Free | Free |
| **Data Quality** | Institutional-grade | Depends on source | Your data |
| **Speed** | Medium (network) | Fast (local) | Very Fast |
| **Privacy** | Cloud-hosted | Local/private | Local/private |
| **Setup** | Easy (just creds) | Medium (Docker) | Built-in |
| **Resources** | None | CPU/RAM needed | Minimal |
| **Indicators** | Full LEAN library | Full LEAN library | 8 custom |
| **Languages** | C#, Python | C#, Python | C# only |
| **Best For** | Production | Development | Quick tests |

---

## Troubleshooting

### QuantConnect Cloud Issues

**Problem:** Authentication failed
```bash
# Solution: Check credentials
curl http://localhost:5002/api/v1/quantconnect/auth/test

# Verify environment variables
echo $QUANTCONNECT_USER_ID
echo $QUANTCONNECT_API_TOKEN
```

**Problem:** Backtest timeout
```bash
# Solution: Increase timeout in config
export QUANTCONNECT_TIMEOUT_SECONDS=600
```

---

### Local LEAN Issues

**Problem:** Docker not found
```bash
# Solution: Install Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
```

**Problem:** LEAN image not found
```bash
# Solution: Build the image
cd /root/AlgoTrendy_v2.6/docker/lean
docker build -t algotrendy/lean:latest .
```

**Problem:** Permission denied on directories
```bash
# Solution: Create directories with correct permissions
sudo mkdir -p /tmp/algotrendy/lean/{algorithms,results,data}
sudo chmod -R 777 /tmp/algotrendy/lean
```

---

## Examples

### Example 1: Test All Engines

```bash
# Get available engines
curl http://localhost:5002/api/v1/backtesting/engines

# Test Cloud
curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
  -H "Content-Type: application/json" \
  -d '{"config":{"symbol":"BTCUSD","startDate":"2024-01-01","endDate":"2024-02-01"},"engineType":"Cloud"}'

# Test Local
curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
  -H "Content-Type: application/json" \
  -d '{"config":{"symbol":"BTCUSD","startDate":"2024-01-01","endDate":"2024-02-01"},"engineType":"Local"}'

# Test Auto
curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
  -H "Content-Type: application/json" \
  -d '{"config":{"symbol":"BTCUSD","startDate":"2024-01-01","endDate":"2024-02-01"},"engineType":"Auto"}'
```

---

### Example 2: Production Workflow

```bash
# 1. Quick test with Local LEAN
curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \
  -d '{"config":{...},"engineType":"Local"}'

# 2. If successful, validate with Cloud (high-quality data)
curl -X POST http://localhost:5002/api/v1/quantconnect/backtest \
  -d '{...}'

# 3. Deploy strategy if metrics are good
```

---

## Security Best Practices

1. **Never commit credentials** to git
   ```bash
   # Add to .gitignore
   echo ".env.quantconnect" >> .gitignore
   ```

2. **Use environment variables** in production
   ```bash
   # Not in code
   export QUANTCONNECT_API_TOKEN=xxx
   ```

3. **Rotate API tokens** regularly
   ```bash
   # Regenerate in QuantConnect account
   # Update .env file
   ```

4. **Use User Secrets** for development
   ```bash
   dotnet user-secrets set "QuantConnect:ApiToken" "your_token"
   ```

---

## Next Steps

1. **Choose your engine** (Cloud, Local, or Auto)
2. **Configure credentials** (if using Cloud)
3. **Run test backtest** to verify setup
4. **Review results** and metrics
5. **Deploy to production** when ready

---

## Support

- **QuantConnect Docs:** https://www.quantconnect.com/docs
- **LEAN GitHub:** https://github.com/QuantConnect/Lean
- **AlgoTrendy Docs:** `/root/AlgoTrendy_v2.6/ai_context/`

---

**Status:** ✅ Fully Integrated
**Version:** v2.6
**Last Updated:** October 20, 2025
**Integration Level:** Cloud + Local LEAN + Custom (Triple Engine)
