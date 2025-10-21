# AlgoTrendy Backtesting.py Microservice

**The 4th Engine in AlgoTrendy's QUAD-ENGINE Backtesting System**

This microservice provides professional-grade backtesting capabilities using the popular [Backtesting.py](https://kernc.github.io/backtesting.py/) library, bridging Python and C#/.NET through a REST API.

## Overview

- **Engine**: Backtesting.py v0.3.3
- **Language**: Python 3.11
- **Framework**: Flask
- **Port**: 5004
- **Architecture**: Microservice (REST API)

## Features

- ✅ Professional backtesting with Backtesting.py library
- ✅ Multiple strategies (SMA Crossover, RSI Mean Reversion)
- ✅ Comprehensive performance metrics
- ✅ RESTful API for C# integration
- ✅ Docker containerization
- ✅ Production-ready with Gunicorn
- ✅ Health checks and logging
- ✅ In-memory results storage

## API Endpoints

### Health Check
```bash
GET /health
```

### List Strategies
```bash
GET /strategies
```

### Run Backtest
```bash
POST /backtest/run
Content-Type: application/json

{
  "symbol": "BTCUSDT",
  "start_date": "2024-01-01",
  "end_date": "2024-10-01",
  "timeframe": "day",
  "strategy": "sma",
  "initial_capital": 10000,
  "commission": 0.001,
  "strategy_params": {
    "fast_period": 20,
    "slow_period": 50
  }
}
```

### Get Results
```bash
GET /backtest/results/<backtest_id>
```

### Get History
```bash
GET /backtest/history
```

### Delete Backtest
```bash
DELETE /backtest/<backtest_id>
```

## Strategies

### 1. SMA Crossover Strategy
- **Description**: Long when fast SMA crosses above slow SMA
- **Parameters**:
  - `fast_period` (default: 20, range: 5-50)
  - `slow_period` (default: 50, range: 20-200)

### 2. RSI Mean Reversion Strategy
- **Description**: Long when RSI crosses below oversold, exit when above overbought
- **Parameters**:
  - `rsi_period` (default: 14, range: 5-30)
  - `oversold` (default: 30, range: 10-40)
  - `overbought` (default: 70, range: 60-90)

## Performance Metrics

The service returns comprehensive performance metrics:

- **Return Metrics**: Total return, annual return
- **Risk Metrics**: Sharpe ratio, Sortino ratio, max drawdown
- **Trade Metrics**: Total trades, win rate, avg trade, best/worst trades
- **Other Metrics**: Profit factor, expectancy, SQN (System Quality Number)

## Local Development

### Install Dependencies
```bash
cd /root/AlgoTrendy_v2.6/backtesting-py-service
pip install -r requirements.txt
```

### Configure Database Connection
```bash
export QUESTDB_HOST=localhost
export QUESTDB_PORT=8812
export QUESTDB_USER=admin
export QUESTDB_PASSWORD=quest
export QUESTDB_DATABASE=qdb
```

### Run Service
```bash
python app.py
```

Service will be available at `http://localhost:5004`

### Test API
```bash
# Health check
curl http://localhost:5004/health

# List strategies
curl http://localhost:5004/strategies

# Run backtest
curl -X POST http://localhost:5004/backtest/run \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "start_date": "2024-01-01",
    "end_date": "2024-10-01",
    "strategy": "sma",
    "initial_capital": 10000
  }'
```

## Docker Deployment

### Build Image
```bash
docker build -t algotrendy-backtesting-py:latest .
```

### Run Container
```bash
docker run -d \
  --name backtesting-py-service \
  -p 5004:5004 \
  --restart unless-stopped \
  algotrendy-backtesting-py:latest
```

### Check Logs
```bash
docker logs -f backtesting-py-service
```

## Integration with v2.6

This service integrates with AlgoTrendy v2.6 C# backend through:

1. **BacktestingPyApiClient.cs** - HTTP client for API communication
2. **BacktestingPyEngine.cs** - Engine implementation for IBacktestEngine
3. **BacktestEngineFactory.cs** - Factory for engine selection

The service is automatically included in the QUAD-ENGINE system:
- Engine 1: Custom AlgoTrendy Engine (C#)
- Engine 2: QuantConnect Cloud (API)
- Engine 3: Local LEAN Docker (Docker)
- **Engine 4: Backtesting.py (Python/Flask)** ← This service

## Data Source

**Uses REAL market data from AlgoTrendy QuestDB database** via PostgreSQL protocol.

- Queries the `market_data` table for OHLCV data
- Supports all symbols in the database
- Auto-resamples data based on timeframe (minute, hour, day, week, month)
- Requires at least 100 bars for meaningful backtesting

## Production Considerations

- ✅ Run with Gunicorn (2+ workers)
- ✅ Use persistent storage (Redis/PostgreSQL) instead of in-memory dict
- ✅ Implement rate limiting
- ✅ Add authentication/API keys
- ✅ Set up monitoring and alerting
- ✅ Configure proper logging rotation

## Performance

- **Cold start**: ~2-3 seconds
- **Backtest execution**: 1-5 seconds (depends on data size)
- **Memory usage**: ~150-250 MB
- **Concurrent requests**: Supports 2-4 workers

## Future Enhancements

- [ ] Add more strategies (MACD, Bollinger Bands, etc.)
- [ ] Support custom strategy uploads
- [ ] Add optimization endpoints (parameter tuning)
- [ ] Implement walk-forward analysis
- [ ] Add Monte Carlo simulation
- [ ] Support portfolio backtesting

## Troubleshooting

**Service won't start:**
- Check port 5004 is not in use: `lsof -i :5004`
- Verify Python 3.11+ installed: `python --version`
- Install dependencies: `pip install -r requirements.txt`

**Backtests fail:**
- Check date range has sufficient data (100+ bars minimum)
- Verify strategy parameters are valid
- Check logs: `tail -f backtesting-py-service.log`

**Connection errors from C#:**
- Verify service is running: `curl http://localhost:5004/health`
- Check firewall settings
- Ensure correct URL in appsettings.json

## License

Part of AlgoTrendy v2.6 platform.

## References

- [Backtesting.py Documentation](https://kernc.github.io/backtesting.py/)
- [Flask Documentation](https://flask.palletsprojects.com/)
- [AlgoTrendy v2.6 Docs](/root/AlgoTrendy_v2.6/ai_context/)
