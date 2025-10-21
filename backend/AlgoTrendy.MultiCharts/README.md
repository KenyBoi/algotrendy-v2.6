# AlgoTrendy.MultiCharts Integration

**Version:** 1.0.0
**Status:** Beta
**Created:** October 21, 2025

---

## 📊 Overview

The AlgoTrendy.MultiCharts integration provides seamless connectivity between AlgoTrendy v2.6 and MultiCharts .NET, enabling professional-grade backtesting, walk-forward optimization, Monte Carlo simulation, and market scanning capabilities.

### Key Features

✅ **Advanced Backtesting** - Institutional-grade backtest engine with accurate fills
✅ **Walk-Forward Optimization** - Prevent overfitting with out-of-sample testing
✅ **Monte Carlo Simulation** - Statistical analysis of strategy robustness
✅ **Market Scanning** - Real-time market scanner with custom formulas
✅ **Strategy Deployment** - Deploy C# strategies to MultiCharts
✅ **Historical Data** - Access MultiCharts data feeds
✅ **100+ Indicators** - Leverage MultiCharts indicator library
✅ **C# Integration** - Native .NET integration with code sharing

---

## 🎯 Why MultiCharts .NET?

### Fills Critical Gaps

| Feature | Before | With MultiCharts |
|---------|--------|------------------|
| **Walk-Forward Optimization** | ❌ | ✅ Built-in |
| **Monte Carlo Simulation** | ❌ | ✅ Built-in |
| **Market Scanning** | ❌ | ✅ Built-in |
| **Professional Charting** | ⚠️ | ✅ Advanced |
| **Strategy Language** | C# | C# (Shared!) |

### Integration Benefits

1. **Code Reusability** - Share C# code between AlgoTrendy and MultiCharts
2. **Professional Tools** - Access institutional-grade analysis features
3. **Complementary** - Works alongside QuantConnect and TradingView
4. **Native .NET** - No language barriers or conversion needed

---

## 📋 Prerequisites

### Software Requirements

- **.NET 8.0 SDK** - AlgoTrendy requirement
- **MultiCharts .NET 64** - Version 14.0 or higher
- **Windows OS** - MultiCharts requires Windows (or Wine for Linux)

### MultiCharts License

- Professional or higher tier
- .NET support enabled
- API access enabled

### Hardware Requirements

- **RAM:** 8GB minimum, 16GB recommended
- **Storage:** SSD recommended for historical data
- **CPU:** Multi-core for parallel optimization

---

## 🚀 Quick Start

### Step 1: Install Multi Charts

```bash
# Download from official website
https://www.multicharts.com/trading-software/

# Install with .NET support
# Activate your license
```

### Step 2: Configure AlgoTrendy

Add to `appsettings.json`:

```json
{
  "MultiCharts": {
    "Enabled": true,
    "ApiEndpoint": "http://localhost:8899",
    "DataPath": "C:\\MultiCharts\\Data",
    "StrategyPath": "C:\\MultiCharts\\Strategies",
    "TimeoutSeconds": 300
  }
}
```

### Step 3: Register Service

In `Program.cs` or `Startup.cs`:

```csharp
using AlgoTrendy.MultiCharts.Configuration;
using AlgoTrendy.MultiCharts.Interfaces;
using AlgoTrendy.MultiCharts.Services;

// Add configuration
builder.Services.Configure<MultiChartsOptions>(
    builder.Configuration.GetSection(MultiChartsOptions.SectionName));

// Register HTTP client
builder.Services.AddHttpClient<IMultiChartsClient, MultiChartsClient>();

// Register MultiCharts client
builder.Services.AddScoped<IMultiChartsClient, MultiChartsClient>();
```

### Step 4: Add Controller

```csharp
using AlgoTrendy.MultiCharts.Controllers;

// Controllers are auto-discovered
// API will be available at /api/multicharts/*
```

### Step 5: Test Connection

```bash
# Test health endpoint
curl http://localhost:5002/api/multicharts/health

# Expected response:
{
  "connected": true,
  "timestamp": "2025-10-21T12:00:00Z",
  "message": "MultiCharts is connected"
}
```

---

## 📖 API Documentation

### Health Check

**GET** `/api/multicharts/health`

Tests connection to MultiCharts platform.

```bash
curl http://localhost:5002/api/multicharts/health
```

**Response:**
```json
{
  "connected": true,
  "timestamp": "2025-10-21T12:00:00Z",
  "message": "MultiCharts is connected"
}
```

---

### Platform Status

**GET** `/api/multicharts/status`

Get MultiCharts platform information.

```bash
curl http://localhost:5002/api/multicharts/status
```

**Response:**
```json
{
  "isConnected": true,
  "version": "14.5.0",
  "serverTime": "2025-10-21T12:00:00Z",
  "activeStrategies": 3
}
```

---

### Run Backtest

**POST** `/api/multicharts/backtest`

Execute a strategy backtest.

```bash
curl -X POST http://localhost:5002/api/multicharts/backtest \
  -H "Content-Type: application/json" \
  -d '{
    "strategyName": "SMA_Crossover",
    "symbol": "BTCUSDT",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2025-01-01T00:00:00Z",
    "timeframe": "1D",
    "initialCapital": 10000,
    "parameters": {
      "FastPeriod": 10,
      "SlowPeriod": 30
    }
  }'
```

**Response:**
```json
{
  "strategyName": "SMA_Crossover",
  "symbol": "BTCUSDT",
  "netProfit": 2456.78,
  "totalTrades": 45,
  "winRate": 0.62,
  "sharpeRatio": 1.85,
  "maxDrawdown": -15.3,
  "profitFactor": 1.92,
  "completedAt": "2025-10-21T12:05:00Z"
}
```

---

### Walk-Forward Optimization

**POST** `/api/multicharts/optimization/walk-forward`

Run walk-forward optimization to prevent overfitting.

```bash
curl -X POST http://localhost:5002/api/multicharts/optimization/walk-forward \
  -H "Content-Type: application/json" \
  -d '{
    "strategyName": "SMA_Crossover",
    "symbol": "BTCUSDT",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2025-01-01T00:00:00Z",
    "inSamplePeriodDays": 180,
    "outOfSamplePeriodDays": 60,
    "stepDays": 30,
    "parametersToOptimize": {
      "FastPeriod": { "start": 5, "stop": 20, "step": 1 },
      "SlowPeriod": { "start": 20, "stop": 50, "step": 2 }
    }
  }'
```

---

### Monte Carlo Simulation

**POST** `/api/multicharts/simulation/monte-carlo`

Run Monte Carlo simulation for risk analysis.

```bash
curl -X POST http://localhost:5002/api/multicharts/simulation/monte-carlo \
  -H "Content-Type: application/json" \
  -d '{
    "strategyName": "SMA_Crossover",
    "symbol": "BTCUSDT",
    "fromDate": "2024-01-01T00:00:00Z",
    "toDate": "2025-01-01T00:00:00Z",
    "numberOfRuns": 1000,
    "parameters": {
      "FastPeriod": 10,
      "SlowPeriod": 30
    }
  }'
```

**Response:**
```json
{
  "meanReturn": 24.5,
  "medianReturn": 22.3,
  "stdDeviation": 8.9,
  "probabilityOfProfit": 0.68,
  "meanMaxDrawdown": -18.2,
  "returnConfidenceIntervalLower": 15.2,
  "returnConfidenceIntervalUpper": 33.8
}
```

---

### Market Scanner

**POST** `/api/multicharts/scanner/run`

Run market scanner with custom formula.

```bash
curl -X POST http://localhost:5002/api/multicharts/scanner/run \
  -H "Content-Type: application/json" \
  -d '{
    "scanName": "RSI_Oversold",
    "symbols": ["BTCUSDT", "ETHUSDT", "BNBUSDT"],
    "scanFormula": "RSI(14) < 30",
    "timeframe": "1D"
  }'
```

---

## 🔧 Configuration Options

### appsettings.json

```json
{
  "MultiCharts": {
    "Enabled": true,                    // Enable/disable integration
    "ApiEndpoint": "http://localhost:8899",  // MultiCharts API URL
    "DataPath": "C:\\MultiCharts\\Data",     // Data directory
    "StrategyPath": "C:\\MultiCharts\\Strategies", // Strategy directory
    "TimeoutSeconds": 300,              // API timeout
    "MaxBacktestDurationMinutes": 60,   // Max backtest time
    "EnableRetry": true,                // Enable retry on failure
    "MaxRetryAttempts": 3,              // Max retry attempts
    "RetryDelayMilliseconds": 1000,     // Delay between retries
    "EnableLogging": true,              // Enable detailed logging
    "ApiKey": null,                     // Optional API key
    "ApiSecret": null                   // Optional API secret
  }
}
```

---

## 📊 Sample Strategies

### SMA Crossover

```csharp
using PowerLanguage.Strategy;

public class SMA_Crossover : SignalObject
{
    [Input]
    public int FastPeriod { get; set; } = 10;

    [Input]
    public int SlowPeriod { get; set; } = 30;

    protected override void CalcBar()
    {
        var fastMA = Bars.Close.Average(FastPeriod);
        var slowMA = Bars.Close.Average(SlowPeriod);

        if (fastMA > slowMA)
            buyMarket.Send();
        else if (fastMA < slowMA)
            sellMarket.Send();
    }
}
```

See `Strategies/SampleStrategies.cs` for more examples.

---

## 🧪 Testing

### Unit Tests

```bash
cd backend/AlgoTrendy.MultiCharts.Tests
dotnet test
```

### Integration Tests

```bash
# Ensure MultiCharts is running first
dotnet test --filter Category=Integration
```

### Manual Testing

```bash
# Test connection
curl http://localhost:5002/api/multicharts/health

# List strategies
curl http://localhost:5002/api/multicharts/strategy/list

# List indicators
curl http://localhost:5002/api/multicharts/indicator/list
```

---

## 📈 Performance Metrics

### Typical Performance

- **Backtest Execution:** 30-120 seconds (depending on data size)
- **Walk-Forward Optimization:** 5-30 minutes (depending on parameter space)
- **Monte Carlo Simulation (1000 runs):** 2-10 minutes
- **Market Scan:** 1-5 seconds per 100 symbols

### Optimization Tips

1. **Use SSD** - Faster data access for large backtests
2. **Multi-core CPU** - Parallel optimization
3. **Limit Data Range** - Test on smaller date ranges first
4. **Cache Results** - Store backtest results for reuse

---

## 🐛 Troubleshooting

### Connection Issues

**Problem:** Cannot connect to MultiCharts

**Solutions:**
```bash
# 1. Check if MultiCharts is running
# 2. Verify API endpoint in appsettings.json
# 3. Check firewall settings
# 4. Test with curl:
curl http://localhost:8899/api/health
```

### Backtest Failures

**Problem:** Backtest returns error

**Solutions:**
- Verify strategy code is valid
- Check symbol exists in data feed
- Ensure date range has data
- Check MultiCharts logs

### Performance Issues

**Problem:** Backtests running slowly

**Solutions:**
- Reduce date range
- Use larger timeframes (daily vs minute)
- Limit number of parameters in optimization
- Check system resources (CPU, RAM)

---

## 🔗 Integration with Other AlgoTrendy Features

### With QuantConnect

```csharp
// Compare backtest results
var qcResult = await quantConnectClient.RunBacktestAsync(...);
var mcResult = await multiChartsClient.RunBacktestAsync(...);

// Validate consistency
if (Math.Abs(qcResult.NetProfit - mcResult.NetProfit) / qcResult.NetProfit < 0.05)
{
    Console.WriteLine("Results are consistent within 5%");
}
```

### With Risk Management

```csharp
// Use Monte Carlo for position sizing
var monteCarloResult = await multiChartsClient.RunMonteCarloSimulationAsync(...);
var maxDrawdown = monteCarloResult.WorstDrawdown;

// Adjust position size based on risk
var positionSize = riskManager.CalculatePositionSize(
    capital: 10000,
    maxDrawdown: maxDrawdown
);
```

### With TradingView

```csharp
// Deploy strategy tested in MultiCharts to TradingView
var backtestResult = await multiChartsClient.RunBacktestAsync(...);

if (backtestResult.SharpeRatio > 1.5)
{
    // Deploy to TradingView for live alerts
    await tradingViewClient.DeployStrategyAsync(...);
}
```

---

## 📚 Additional Resources

### Official Documentation

- MultiCharts .NET: https://www.multicharts.com/net/
- PowerLanguage .NET Reference: https://www.multicharts.com/trading-software/index.php?title=PowerLanguage_.NET

### AlgoTrendy Documentation

- [Integration Plan](../../../planning/MULTICHARTS_INTEGRATION_PLAN.md)
- [Main README](../../../README.md)
- [API Documentation](../../../docs/API_USAGE_EXAMPLES.md)

### Community

- MultiCharts Forum: https://www.multicharts.com/discussion/
- AlgoTrendy GitHub: https://github.com/KenyBoi/algotrendy-v2.6

---

## 🎯 Next Steps

1. ✅ Install and configure MultiCharts
2. ✅ Test connection with health endpoint
3. ✅ Deploy a sample strategy
4. ✅ Run your first backtest
5. ✅ Try walk-forward optimization
6. ✅ Run Monte Carlo simulation
7. ✅ Create your own strategies

---

## 📝 License

Proprietary - AlgoTrendy Project
Copyright © 2025 AlgoTrendy Engineering Team

---

**Version:** 1.0.0
**Last Updated:** October 21, 2025
**Status:** Beta - Ready for testing

---

*For questions or issues, please refer to the main AlgoTrendy documentation or create an issue on GitHub.*
