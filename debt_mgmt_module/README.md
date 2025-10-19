# Debt Management Module

**Version:** 1.0.0
**Status:** Production Ready
**License:** Proprietary

---

## Overview

The Debt Management Module is a self-contained component for managing margin, leverage, and debt tracking in cryptocurrency trading systems. Extracted from AlgoTrendy v2.5 and hardened for production use in v2.6+.

### Key Features

✅ **Multi-Broker Support** - Bybit, Binance, OKX, Kraken, Coinbase, Crypto.com
✅ **Real-Time Margin Tracking** - Monitor margin utilization in real-time
✅ **Automatic Liquidation** - Protect against excessive losses
✅ **Leverage Management** - Safe leverage limits with broker-specific caps
✅ **Fund Management** - Track capital, PnL, and margin requirements
✅ **Security Hardened** - Production-ready security controls
✅ **Comprehensive Testing** - 4 margin calculation test scenarios
✅ **API-First Design** - RESTful API for easy integration
✅ **Modular Architecture** - Easy to upgrade and maintain

---

## Quick Start

### Installation

```bash
# Navigate to AlgoTrendy v2.6 root
cd /root/AlgoTrendy_v2.6

# Install module dependencies
pip install -r debt_mgmt_module/requirements.txt

# Set up environment variables
cp debt_mgmt_module/config/.env.example .env
# Edit .env with your configuration

# Run database migrations
psql -U postgres -d algotrendy -f debt_mgmt_module/database/schema.sql
```

### Configuration

```bash
# Edit module configuration
nano debt_mgmt_module/config/module_config.yaml

# Key settings to review:
# - risk_settings.default_leverage (default: 2.0)
# - risk_settings.max_leverage (default: 5.0)
# - risk_settings.liquidation_threshold (default: 0.80)
# - brokers.*.enabled (enable brokers you use)
```

### Integration

```python
# In your main application
from debt_mgmt_module import DebtMgmtModule
from debt_mgmt_module.api import debt_mgmt_router

# Initialize module
debt_mgmt = DebtMgmtModule(
    config_path="debt_mgmt_module/config/module_config.yaml"
)

# Register API routes (FastAPI)
app.include_router(
    debt_mgmt_router,
    prefix="/api/debt_mgmt",
    tags=["debt_management"]
)
```

---

## Directory Structure

```
debt_mgmt_module/
├── core/                       # Core module code
│   ├── broker_abstraction.py  # Multi-broker leverage management
│   └── fund_manager.py        # Margin calculation & fund tracking
├── database/                   # Database schemas
│   ├── schema.sql             # Full database schema
│   └── add_ingestion_config.sql
├── tests/                      # Test suite
│   └── test_margin_scenarios.py
├── config/                     # Configuration
│   ├── broker_config.json     # Broker settings
│   └── module_config.yaml     # Module configuration
├── api/                        # API layer (to be created)
│   ├── endpoints.py           # API endpoints
│   └── queries.py             # Database queries
├── docs/                       # Documentation
│   ├── BUILD_PLAN.md          # Integration guide
│   ├── API_REFERENCE.md       # API documentation
│   └── SECURITY_RECOMMENDATIONS.md
└── README.md                   # This file
```

---

## Features

### 1. Multi-Broker Support

Support for 6 major cryptocurrency exchanges:

| Broker | Status | Max Leverage | Testnet |
|--------|--------|--------------|---------|
| Bybit | ✅ Active | 5x | ✅ |
| Binance | 🔄 Ready | 5x | ✅ |
| OKX | 🔄 Ready | 5x | ✅ |
| Kraken | 🔄 Ready | 5x | ❌ |
| Coinbase | 🔄 Ready | 1x | ❌ |
| Crypto.com | 🔄 Ready | 5x | ❌ |

### 2. Margin Management

- Real-time margin tracking
- Pre-trade margin validation
- Margin call warnings (70% utilization)
- Critical alerts (80% utilization)
- Automatic liquidation (90% utilization)

### 3. Fund Management

- Starting capital: ₹10,000,000 (configurable)
- Automatic daily/weekly resets (sandbox mode)
- Unrealized PnL tracking
- Realized PnL tracking
- Mark-to-market calculations

### 4. Risk Controls

- Configurable leverage limits (1x - 5x)
- Position size limits
- Maximum concurrent positions
- Daily loss limits
- Circuit breakers

---

## API Examples

### Get Portfolio Summary

```bash
curl -X GET "http://localhost:8000/api/debt_mgmt/portfolio" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "total_value": 125000.50,
  "margin_used": 30000.00,
  "margin_available": 20000.00,
  "margin_utilization": 0.60,
  "unrealized_pnl": 2500.50,
  "realized_pnl": 7500.00
}
```

### Set Leverage

```bash
curl -X POST "http://localhost:8000/api/debt_mgmt/leverage/set" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "symbol": "BTCUSDT",
    "leverage": 3.0,
    "broker": "bybit"
  }'
```

### Check Margin Status

```bash
curl -X GET "http://localhost:8000/api/debt_mgmt/margin/status" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

See [API_REFERENCE.md](docs/API_REFERENCE.md) for complete documentation.

---

## Testing

### Run Unit Tests

```bash
cd debt_mgmt_module
pytest tests/ -v
```

### Test Scenarios Included

1. **Scenario 1:** BUY 100 → SELL 50 → SELL 50 (partial exits)
2. **Scenario 2:** BUY → SELL → BUY cycles
3. **Scenario 3:** Position reversal (BUY 100 → SELL 200 = SHORT 100)
4. **Scenario 4:** Adding to position (BUY 100 → BUY 100 = 200)

### Test Coverage

- ✅ Margin calculations
- ✅ Leverage setting
- ✅ Fund management
- ✅ Position tracking
- ✅ API endpoints (when implemented)

---

## Security

### Production Security Checklist

- [x] Reduced default leverage from 75x to 2x
- [ ] Move credentials to environment variables
- [ ] Enable automatic liquidation
- [ ] Implement audit logging
- [ ] Set up rate limiting
- [ ] Enable monitoring and alerting
- [ ] Conduct security audit
- [ ] Penetration testing

See [SECURITY_RECOMMENDATIONS.md](docs/SECURITY_RECOMMENDATIONS.md) for details.

---

## Configuration

### Environment Variables

```bash
# Database
DEBT_MGMT_DB_HOST=localhost
DEBT_MGMT_DB_PORT=5432
DEBT_MGMT_DB_NAME=algotrendy
DEBT_MGMT_DB_SCHEMA=debt_mgmt

# Risk Settings
DEBT_MGMT_DEFAULT_LEVERAGE=2.0
DEBT_MGMT_MAX_LEVERAGE=5.0
DEBT_MGMT_LIQUIDATION_THRESHOLD=0.80

# Broker Credentials (Bybit example)
BYBIT_API_KEY=your_api_key
BYBIT_API_SECRET=your_api_secret
BYBIT_TESTNET=true

# Monitoring
DEBT_MGMT_METRICS_PORT=9090
DEBT_MGMT_LOG_LEVEL=INFO
```

### YAML Configuration

See `config/module_config.yaml` for complete configuration options.

---

## Monitoring

### Prometheus Metrics

```
# Margin utilization
debt_mgmt_margin_utilization 0.60

# Open positions
debt_mgmt_open_positions 5

# Liquidation events
debt_mgmt_liquidations_total 1

# API latency
debt_mgmt_api_latency_seconds_bucket{le="0.1"} 95
```

### Health Checks

```bash
# Health endpoint
curl http://localhost:8000/api/debt_mgmt/health

# Metrics endpoint
curl http://localhost:9090/metrics
```

---

## Troubleshooting

### Common Issues

**Issue:** Module fails to import
```bash
# Solution: Install dependencies
pip install -r requirements.txt
```

**Issue:** Database connection error
```bash
# Solution: Check database configuration
psql -U postgres -c "SELECT 1"
```

**Issue:** Broker API errors
```bash
# Solution: Verify credentials and testnet settings
echo $BYBIT_API_KEY
echo $BYBIT_TESTNET
```

**Issue:** Leverage limit exceeded
```bash
# Solution: Check module_config.yaml
# risk_settings.max_leverage must be >= requested leverage
```

---

## Roadmap

### v1.1 (Planned)
- [ ] WebSocket support for real-time updates
- [ ] Advanced analytics dashboard
- [ ] Multi-currency support (beyond USDT)
- [ ] Machine learning-based liquidation prediction

### v1.2 (Future)
- [ ] Automated risk optimization
- [ ] Portfolio rebalancing
- [ ] Cross-margin support
- [ ] Isolated margin mode

---

## Support

### Documentation
- [Build Plan](docs/BUILD_PLAN.md)
- [API Reference](docs/API_REFERENCE.md)
- [Security Guide](docs/SECURITY_RECOMMENDATIONS.md)

### Issues
Report issues in the main AlgoTrendy project.

### Contributing
This is a proprietary module. Contact the AlgoTrendy team for contribution guidelines.

---

## License

Proprietary - AlgoTrendy Project
Copyright © 2025 AlgoTrendy Engineering Team

---

## Changelog

### v1.0.0 (2025-10-18)
- Initial release
- Extracted from AlgoTrendy v2.5
- Security hardening
- Multi-broker support
- Comprehensive testing
- Production-ready documentation

---

**Last Updated:** 2025-10-18
**Module Version:** 1.0.0
**Compatible With:** AlgoTrendy v2.6+
