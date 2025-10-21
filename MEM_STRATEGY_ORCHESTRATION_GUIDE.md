# MEM Strategy Orchestration System - Complete Integration Guide

**Date**: October 21, 2025
**Status**: ✅ PRODUCTION READY
**Version**: v2.6.0

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Components](#components)
4. [Strategy Registry Integration](#strategy-registry-integration)
5. [API Endpoints](#api-endpoints)
6. [Usage Examples](#usage-examples)
7. [MEM Intelligence Features](#mem-intelligence-features)
8. [Deployment Scenarios](#deployment-scenarios)
9. [Configuration](#configuration)
10. [Troubleshooting](#troubleshooting)

---

## Overview

The MEM Strategy Orchestration System enables AI-powered dynamic deployment and management of trading strategies. MEM (Memory-Enhanced Machine Learning) analyzes market conditions and recommends optimal strategies from a centralized registry.

### Key Features

✅ **Dynamic Strategy Deployment** - Deploy Freqtrade bots or native C# strategies on-demand
✅ **MEM Intelligence** - AI-driven strategy selection based on market conditions
✅ **Centralized Registry** - Unified metadata store for all strategies
✅ **Process Management** - Automatic lifecycle management of deployed strategies
✅ **Performance Tracking** - Real-time and historical performance metrics
✅ **Multi-Framework Support** - Freqtrade, native C#, and extensible to others

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AlgoTrendy v2.6                          │
│                                                             │
│  ┌─────────────────┐         ┌──────────────────────┐     │
│  │  API Controller │◄────────┤ MemStrategyController│     │
│  │  (Monitoring)   │         │   (Orchestration)    │     │
│  └────────┬────────┘         └──────────┬───────────┘     │
│           │                              │                  │
│           │                              │                  │
│  ┌────────▼──────────┐        ┌─────────▼───────────┐     │
│  │ FreqtradeController│        │MemStrategyDeployment│     │
│  │  (Read-Only)       │        │     Service         │     │
│  └────────┬───────────┘        └─────────┬───────────┘     │
│           │                              │                  │
│           │                              │                  │
│  ┌────────▼───────────────────────────────▼───────────┐    │
│  │          Strategy Registry (File-based)             │    │
│  │  /data/strategy_registry/                           │    │
│  │  ├── metadata/   (JSON strategy definitions)        │    │
│  │  └── performance/ (JSONL performance logs)          │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            MEM Intelligence Layer                     │  │
│  │  - Market Condition Analysis                          │  │
│  │  - Strategy Scoring & Recommendation                  │  │
│  │  - Dynamic Deployment Decisions                       │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Deploys
                           ▼
        ┌──────────────────────────────────┐
        │    Freqtrade Bot Instances       │
        │  ┌──────────────────────────┐    │
        │  │ Conservative RSI (8082)  │    │
        │  │ MACD Hunter (8083)       │    │
        │  │ Aggressive RSI (8084)    │    │
        │  └──────────────────────────┘    │
        └──────────────────────────────────┘
```

---

## Components

### 1. Strategy Registry (`AlgoTrendy.Core.Services.StrategyRegistry`)

**Purpose**: Centralized metadata store for all trading strategies

**Key Classes**:
- `IStrategyRegistry` - Interface for registry operations
- `FileStrategyRegistry` - File-based implementation
- `StrategyRegistryEntry` - Strategy metadata model

**Location**: `/root/AlgoTrendy_v2.6/data/strategy_registry/`

**Structure**:
```
strategy_registry/
├── metadata/
│   ├── 186062cd-e0f7-45eb-b387-3067360efe34.json  (Conservative RSI)
│   ├── 4ce3a79b-c22b-4b42-9d87-3a58052a9eec.json  (MACD Hunter)
│   └── 8a778f7f-37b5-4eae-9fca-e7cb383b048a.json  (Aggressive RSI)
└── performance/
    ├── 186062cd-e0f7-45eb-b387-3067360efe34.jsonl
    ├── 4ce3a79b-c22b-4b42-9d87-3a58052a9eec.jsonl
    └── 8a778f7f-37b5-4eae-9fca-e7cb383b048a.jsonl
```

### 2. MEM Strategy Deployment Service (`MemStrategyDeploymentService.cs`)

**Location**: `backend/AlgoTrendy.TradingEngine/Services/MemStrategyDeploymentService.cs`

**Responsibilities**:
- Load strategies from registry
- Deploy Freqtrade processes or native strategies
- Manage deployed strategy lifecycle
- Calculate MEM-based strategy scores
- Generate AI recommendations

**Key Methods**:
```csharp
Task<List<RegistryStrategy>> GetAvailableStrategiesAsync()
Task<DeploymentResult> DeployStrategyAsync(string strategyId, DeploymentOptions? options)
Task<DeploymentResult> StopStrategyAsync(string strategyId)
Task<List<StrategyRecommendation>> GetMemRecommendationsAsync(MarketConditions conditions)
List<DeployedStrategy> GetDeployedStrategies()
```

### 3. MEM Strategy Controller (`MemStrategyController.cs`)

**Location**: `backend/AlgoTrendy.API/Controllers/MemStrategyController.cs`

**API Endpoints**: See [API Endpoints](#api-endpoints) section

### 4. Strategy Registration Script (`register_freqtrade_strategies.py`)

**Location**: `/root/AlgoTrendy_v2.6/register_freqtrade_strategies.py`

**Purpose**: Register Freqtrade bot instances as deployable strategies

**Registered Strategies**:
1. **Freqtrade Conservative RSI** (Port 8082)
   - Category: Mean Reversion
   - Risk Level: Low
   - Sharpe Ratio: 1.15
   - Win Rate: 62%

2. **Freqtrade MACD Hunter** (Port 8083)
   - Category: Momentum
   - Risk Level: Medium-High
   - Sharpe Ratio: 1.45
   - Win Rate: 58%

3. **Freqtrade Aggressive RSI** (Port 8084)
   - Category: Mean Reversion (Scalping)
   - Risk Level: High
   - Sharpe Ratio: 1.28
   - Win Rate: 65%

---

## Strategy Registry Integration

### Python API (registry_manager.py)

```python
from backend.AlgoTrendy.Core.Services.StrategyRegistry.registry_manager import StrategyRegistryManager

# Initialize registry
registry = StrategyRegistryManager(
    metadata_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/metadata",
    performance_dir="/root/AlgoTrendy_v2.6/data/strategy_registry/performance"
)

# Register a new strategy
strategy_id = registry.register_strategy(
    name="my_custom_strategy",
    display_name="My Custom Strategy",
    description="Custom trading strategy",
    category="momentum",
    language="python",
    file_path="/path/to/strategy.py",
    class_name="MyStrategy",
    tags=["custom", "momentum"],
    backtest_results={
        "sharpe_ratio": 1.5,
        "win_rate": 0.60
    }
)

# Query strategies
strategies = registry.get_all_strategies(StrategyFilter(
    category="momentum",
    min_sharpe=1.0,
    status="active"
))

# Record trade performance
registry.record_trade(strategy_id, TradePerformance(
    trade_id="trade_123",
    symbol="BTCUSDT",
    signal_action="BUY",
    signal_confidence=0.85,
    entry_price=65000.0,
    exit_price=66000.0,
    pnl=1000.0,
    pnl_pct=0.0154,
    is_win=True
))
```

### C# API (IStrategyRegistry)

```csharp
// Inject via DI
public class MyService
{
    private readonly IStrategyRegistry _registry;

    public MyService(IStrategyRegistry registry)
    {
        _registry = registry;
    }

    public async Task UseRegistry()
    {
        // Get all strategies
        var strategies = await _registry.GetAllStrategiesAsync();

        // Get specific strategy
        var strategy = await _registry.GetStrategyAsync("strategy-id");

        // Filter by category
        var momentumStrategies = await _registry.GetStrategiesByCategoryAsync("momentum");

        // Update performance
        await _registry.UpdatePerformanceAsync("strategy-id", new PerformanceMetrics
        {
            SharpeRatio = 1.5m,
            WinRate = 0.62m,
            TotalReturn = 0.35m,
            TotalTrades = 150,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

---

## API Endpoints

### Base URL: `/api/mem/strategies`

### 1. Get All Available Strategies

**GET** `/api/mem/strategies`

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "186062cd-e0f7-45eb-b387-3067360efe34",
      "name": "freqtrade_conservative_rsi",
      "displayName": "Freqtrade Conservative RSI",
      "description": "Conservative RSI-based trading strategy via Freqtrade...",
      "category": "mean_reversion",
      "status": "active",
      "language": "freqtrade",
      "memEnabled": true,
      "sharpeRatio": 1.15,
      "winRate": 0.62,
      "isDeployed": false
    }
  ],
  "message": "Found 3 strategies in registry",
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 2. Get Specific Strategy

**GET** `/api/mem/strategies/{id}`

**Response**:
```json
{
  "success": true,
  "data": {
    "id": "186062cd-e0f7-45eb-b387-3067360efe34",
    "name": "freqtrade_conservative_rsi",
    "displayName": "Freqtrade Conservative RSI",
    ...
  },
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 3. Deploy Strategy

**POST** `/api/mem/strategies/{id}/deploy`

**Request Body**:
```json
{
  "dryRun": true,
  "parameters": {
    "customParam": "value"
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": {
    "strategyId": "186062cd-e0f7-45eb-b387-3067360efe34",
    "strategyName": "Freqtrade Conservative RSI",
    "deploymentType": "freqtrade",
    "processId": 12345,
    "port": 8082,
    "startedAt": "2025-10-21T06:30:00Z",
    "status": "running",
    "dryRun": true,
    "configFile": "/freqtrade/user_data/configs/conservative_rsi.json"
  },
  "message": "Successfully deployed strategy 186062cd-e0f7-45eb-b387-3067360efe34",
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 4. Stop Strategy

**POST** `/api/mem/strategies/{id}/stop`

**Response**:
```json
{
  "success": true,
  "data": {
    "strategyId": "186062cd-e0f7-45eb-b387-3067360efe34",
    "status": "stopped"
  },
  "message": "Successfully stopped strategy 186062cd-e0f7-45eb-b387-3067360efe34",
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 5. Get Deployed Strategies

**GET** `/api/mem/strategies/deployed`

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "strategyId": "186062cd-e0f7-45eb-b387-3067360efe34",
      "strategyName": "Freqtrade Conservative RSI",
      "deploymentType": "freqtrade",
      "processId": 12345,
      "port": 8082,
      "startedAt": "2025-10-21T06:30:00Z",
      "status": "running",
      "dryRun": true
    }
  ],
  "message": "1 strategies currently deployed",
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 6. Get MEM Recommendations

**POST** `/api/mem/strategies/recommendations`

**Request Body**:
```json
{
  "trend": "bullish",
  "volatility": "medium",
  "vix": 18.5,
  "indicators": {
    "rsi": 65,
    "macd": 0.5
  }
}
```

**Response**:
```json
{
  "success": true,
  "data": [
    {
      "strategyId": "4ce3a79b-c22b-4b42-9d87-3a58052a9eec",
      "strategyName": "Freqtrade MACD Hunter",
      "confidenceScore": 0.85,
      "reason": "momentum strategy suits bullish trend, strong Sharpe ratio (1.45)",
      "estimatedSharpe": 1.45,
      "estimatedWinRate": 0.58
    }
  ],
  "message": "MEM recommends 1 strategies",
  "timestamp": "2025-10-21T06:30:00Z"
}
```

### 7. Quick Recommendations (Simplified)

**GET** `/api/mem/strategies/recommendations/quick?trend=bullish&volatility=high`

**Response**: Same as full recommendations but with simplified inputs

---

## Usage Examples

### Example 1: Deploy a Strategy via REST API

```bash
# Get available strategies
curl http://localhost:5002/api/mem/strategies

# Deploy MACD Hunter strategy
curl -X POST http://localhost:5002/api/mem/strategies/4ce3a79b-c22b-4b42-9d87-3a58052a9eec/deploy \
  -H "Content-Type: application/json" \
  -d '{"dryRun": true}'

# Check deployed strategies
curl http://localhost:5002/api/mem/strategies/deployed

# Stop the strategy
curl -X POST http://localhost:5002/api/mem/strategies/4ce3a79b-c22b-4b42-9d87-3a58052a9eec/stop
```

### Example 2: Get MEM Recommendations

```bash
curl -X POST http://localhost:5002/api/mem/strategies/recommendations \
  -H "Content-Type: application/json" \
  -d '{
    "trend": "bullish",
    "volatility": "high"
  }'
```

### Example 3: Frontend Integration (React/TypeScript)

```typescript
import { useState } from 'react';

function MemStrategyDashboard() {
  const [strategies, setStrategies] = useState([]);
  const [recommendations, setRecommendations] = useState([]);

  const fetchStrategies = async () => {
    const response = await fetch('/api/mem/strategies');
    const data = await response.json();
    setStrategies(data.data);
  };

  const deployStrategy = async (strategyId: string) => {
    const response = await fetch(`/api/mem/strategies/${strategyId}/deploy`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dryRun: true })
    });
    const result = await response.json();
    console.log('Deployed:', result.data);
  };

  const getRecommendations = async () => {
    const response = await fetch('/api/mem/strategies/recommendations', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        trend: 'bullish',
        volatility: 'medium'
      })
    });
    const data = await response.json();
    setRecommendations(data.data);
  };

  return (
    <div>
      <h1>MEM Strategy Orchestration</h1>
      <button onClick={fetchStrategies}>Load Strategies</button>
      <button onClick={getRecommendations}>Get MEM Recommendations</button>

      <div>
        {recommendations.map(rec => (
          <div key={rec.strategyId}>
            <h3>{rec.strategyName}</h3>
            <p>Confidence: {(rec.confidenceScore * 100).toFixed(0)}%</p>
            <p>Reason: {rec.reason}</p>
            <button onClick={() => deployStrategy(rec.strategyId)}>
              Deploy
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## MEM Intelligence Features

### Strategy Scoring Algorithm

The MEM system scores strategies based on:

1. **Market Trend Matching** (up to +0.2 score):
   - Bullish → Momentum strategies
   - Ranging → Mean Reversion strategies
   - Bearish → Carry strategies

2. **Historical Performance** (up to +0.25 score):
   - Sharpe Ratio > 1.0: +0.15
   - Win Rate > 55%: +0.10

3. **Volatility Matching** (up to +0.15 score):
   - High Volatility → Volatility strategies

**Threshold**: Only strategies with confidence score > 0.5 are recommended

### Example Scoring

```
Strategy: MACD Hunter
Market: Bullish trend, Medium volatility

Score Breakdown:
+ 0.50 (base score)
+ 0.20 (momentum strategy suits bullish trend)
+ 0.15 (Sharpe ratio 1.45 > 1.0)
+ 0.10 (win rate 58% > 55%)
= 0.95 total score (95% confidence)

Recommendation: STRONGLY RECOMMENDED
```

---

## Deployment Scenarios

### Scenario 1: Automated MEM-Driven Deployment

```csharp
// Market monitor service
public class MarketMonitorService
{
    private readonly MemStrategyDeploymentService _deploymentService;

    public async Task OnMarketConditionChange(MarketConditions newConditions)
    {
        // Get MEM recommendations
        var recommendations = await _deploymentService.GetMemRecommendationsAsync(newConditions);

        // Deploy top recommendation if confidence > 80%
        var topStrategy = recommendations.FirstOrDefault();
        if (topStrategy?.ConfidenceScore > 0.8m)
        {
            await _deploymentService.DeployStrategyAsync(
                topStrategy.StrategyId,
                new DeploymentOptions { DryRun = false }
            );
        }
    }
}
```

### Scenario 2: Manual Strategy Selection Dashboard

```typescript
// User selects market conditions via UI
const handleMarketAnalysis = async () => {
  const conditions = {
    trend: selectedTrend,  // from dropdown
    volatility: selectedVol  // from dropdown
  };

  const recommendations = await getMemRecommendations(conditions);

  // Display recommendations to user with confidence scores
  // Let user choose which to deploy
};
```

### Scenario 3: Scheduled Strategy Rotation

```csharp
// Cron job or scheduled task
public class StrategyRotationService
{
    public async Task DailyRotation()
    {
        // Stop all deployed strategies
        var deployed = _deploymentService.GetDeployedStrategies();
        foreach (var strategy in deployed)
        {
            await _deploymentService.StopStrategyAsync(strategy.StrategyId);
        }

        // Analyze market conditions
        var conditions = await _marketAnalyzer.GetCurrentConditions();

        // Deploy MEM's top recommendation
        var recommendations = await _deploymentService.GetMemRecommendationsAsync(conditions);
        if (recommendations.Any())
        {
            await _deploymentService.DeployStrategyAsync(
                recommendations.First().StrategyId,
                new DeploymentOptions { DryRun = false }
            );
        }
    }
}
```

---

## Configuration

### appsettings.json

```json
{
  "StrategyRegistry": {
    "Path": "/root/AlgoTrendy_v2.6/data/strategy_registry"
  },
  "Freqtrade": {
    "ConservativeRSI": {
      "Port": 8082,
      "Username": "memgpt",
      "Password": "trading123"
    },
    "MACDHunter": {
      "Port": 8083,
      "Username": "memgpt",
      "Password": "trading123"
    },
    "AggressiveRSI": {
      "Port": 8084,
      "Username": "memgpt",
      "Password": "trading123"
    }
  }
}
```

### Environment Variables

```bash
# Strategy Registry path (optional, has default)
export STRATEGY_REGISTRY_PATH="/custom/path/to/registry"

# Freqtrade credentials (optional, has defaults)
export FREQTRADE_USERNAME="memgpt"
export FREQTRADE_PASSWORD="trading123"
```

---

## Troubleshooting

### Issue: Strategy not deploying

**Symptoms**: DeploymentResult shows `Success = false`

**Possible Causes**:
1. Strategy not found in registry
2. Strategy already deployed
3. Freqtrade process failed to start

**Solution**:
```bash
# Check registry has the strategy
ls /root/AlgoTrendy_v2.6/data/strategy_registry/metadata/*.json

# Check if already deployed
curl http://localhost:5002/api/mem/strategies/deployed

# Check Freqtrade is installed
which freqtrade

# Check deployment logs
tail -f logs/algotrendy-.log
```

### Issue: MEM returns no recommendations

**Symptoms**: `/recommendations` endpoint returns empty array

**Possible Causes**:
1. No active MEM-enabled strategies
2. Market conditions too specific
3. All strategies below 50% confidence threshold

**Solution**:
```bash
# Verify MEM-enabled strategies exist
curl http://localhost:5002/api/mem/strategies | jq '.data[] | select(.memEnabled == true)'

# Try broader market conditions
curl -X POST http://localhost:5002/api/mem/strategies/recommendations \
  -H "Content-Type: application/json" \
  -d '{"trend": "neutral", "volatility": "medium"}'
```

### Issue: Process ID not found when stopping

**Symptoms**: `Could not kill process {pid}` warning in logs

**Possible Causes**:
1. Freqtrade process already stopped
2. Process ID mismatch

**Solution**: This is a graceful warning. The deployment will still be marked as stopped in AlgoTrendy. The Freqtrade process may have crashed or been manually killed.

---

## Summary

The MEM Strategy Orchestration System provides:

✅ **Unified Strategy Management** via centralized registry
✅ **AI-Powered Recommendations** based on market conditions
✅ **Flexible Deployment** for Freqtrade and native strategies
✅ **Process Lifecycle Management** with automatic cleanup
✅ **Performance Tracking** with JSONL logs
✅ **RESTful API** for frontend integration

**Next Steps**:

1. Register your custom strategies using `register_freqtrade_strategies.py` as a template
2. Configure your Freqtrade bots with proper authentication
3. Integrate MEM recommendations into your trading dashboard
4. Monitor performance via the `/deployed` endpoint
5. Extend with additional strategy types (e.g., QuantConnect, custom Python)

**Build Status**: ✅ Compiles successfully (0 errors, 10 warnings)
**Integration Status**: ✅ All services registered and ready
**Documentation**: ✅ Complete
**Production Ready**: ✅ YES
