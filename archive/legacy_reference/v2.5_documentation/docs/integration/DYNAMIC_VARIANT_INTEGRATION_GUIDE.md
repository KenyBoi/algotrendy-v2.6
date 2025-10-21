# Dynamic Variant Trading System - Integration Guide

**Status**: Complete Architecture v2.5  
**Date**: 2025-10-16  
**Purpose**: Consolidate 30+ duplicated trader files into one configurable system

---

## EXECUTIVE SUMMARY

### Problem Solved
- **Before**: 30+ trader files (~1,150 lines each, 90% duplication)
- **After**: 1 unified trader + configuration-driven variants
- **Result**: 90% code reduction + complete flexibility

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│        UNIFIED MEMGPT TRADER (Single Template)          │
│                                                         │
│  • Loads configuration from JSON                        │
│  • Injects broker variant dynamically                   │
│  • Injects strategy variant dynamically                 │
│  • Uses indicators from separate engine                 │
│  • Fetches credentials from secure vault                │
└─────────────────────────────────────────────────────────┘
         ↑            ↑              ↑            ↑
         │            │              │            │
    ┌────┴───┐   ┌────┴──────┐  ┌───┴────┐   ┌──┴──────┐
    │ Broker │   │ Strategy  │  │Indicator│   │Secure   │
    │ Variant│   │ Variant   │  │Engine   │   │Creds    │
    │        │   │           │  │         │   │         │
    │ • Bybit│   │ • Momentum│  │ • RSI   │   │ • Vault │
    │ • Alpaca   │ • RSI     │  │ • MACD  │   │ • Audit │
    │ • Binance  │ • MACD    │  │ • MFI   │   │ • Rotate│
    │ • OKX      │ • MFI     │  │ • VWAP  │   │         │
    │ • Kraken   │ • VWAP    │  │ • BB    │   │         │
    └────────┘   └───────────┘  └─────────┘   └─────────┘
```

---

## SYSTEM COMPONENTS

### 1. VariantLoader (`algotrendy/variant_loader.py`)
**Purpose**: Load and parse configuration files with variant specifications

**Key Features**:
- Loads JSON configuration files
- Validates configuration schema
- Resolves environment variable references (e.g., `${BYBIT_API_KEY}`)
- Provides accessor methods for each variant type

**Usage**:
```python
from algotrendy.variant_loader import VariantLoader

loader = VariantLoader("algotrendy/configs/bybit_crypto_momentum_live.json")

# Access variants
broker = loader.get_broker_name()          # "bybit"
strategy = loader.get_strategy_name()      # "momentum"
symbols = loader.get_symbols()             # ["BTCUSDT", "ETHUSDT", ...]
params = loader.get_strategy_params()      # {period: 14, ...}
```

### 2. StrategyResolver (`algotrendy/strategy_resolver.py`)
**Purpose**: Dynamically load and instantiate trading strategies

**Included Strategies**:
- **MomentumStrategy**: Trades on price momentum
- **RSIStrategy**: Trades on RSI overbought/oversold
- **MACDStrategy**: Trades on MACD crossovers
- **MFIStrategy**: Trades on money flow index
- **VWAPStrategy**: Trades relative to VWAP

**Usage**:
```python
from algotrendy.strategy_resolver import StrategyResolver

# Get strategy instance
strategy = StrategyResolver.get_strategy("momentum", threshold_buy=2.0)

# Generate signal
signal = strategy.analyze(market_data)
# Returns: {'action': 'BUY', 'confidence': 0.85, 'entry_price': 45000, ...}

# Register custom strategy
StrategyResolver.register_strategy("custom", MyCustomStrategy)
```

### 3. IndicatorEngine (`algotrendy/indicator_engine.py`)
**Purpose**: Separate indicator calculations from strategy logic

**Included Indicators**:
- **RSI**: Relative Strength Index
- **MACD**: Moving Average Convergence Divergence
- **MFI**: Money Flow Index
- **VWAP**: Volume Weighted Average Price
- **BollingerBands**: Upper/middle/lower bands

**Key Feature**: Each indicator is calculated once and cached, allowing strategies to reuse values.

**Usage**:
```python
from algotrendy.indicator_engine import IndicatorEngine

# Calculate single indicator
rsi_value = IndicatorEngine.calculate("rsi", market_data, period=14)

# Get indicator instance for reuse
indicator = IndicatorEngine.get_indicator("macd")
value = indicator.calculate(market_data)
history = indicator.get_value_history(limit=10)

# Register custom indicator
IndicatorEngine.register_indicator("custom", MyCustomIndicator)
```

### 4. AssetFactory (`algotrendy/asset_factory.py`)
**Purpose**: Manage different asset classes and symbol formatting

**Supported Asset Classes**:
- **Crypto**: BTCUSDT, ETHUSDT, SOLUSDT, ADAUSDT, DOTUSDT, etc.
- **Stocks**: AAPL, GOOGL, MSFT, TSLA, NVDA, etc.
- **Futures**: BTC_USD_PERP, ETH_USD_PERP, SOL_USD_PERP, etc.
- **ETFs**: SPY, QQQ, IWM, GLD, TLT, etc.

**Symbol Formatting**: Handles broker-specific symbol formats
- Bybit: BTCUSDT
- OKX: BTC-USDT
- Kraken: BTCUSD

**Usage**:
```python
from algotrendy.asset_factory import AssetFactory

# Get symbols for asset class
symbols = AssetFactory.get_symbols("crypto")         # ["BTCUSDT", "ETHUSDT", ...]

# Format symbol for broker
formatted = AssetFactory.format_symbol("BTCUSDT", "crypto", "okx")  # "BTC-USDT"

# Add custom symbol
AssetFactory.add_symbol("NEWTOKEN", "crypto", "New Token", min_quantity=0.1)

# Get statistics
stats = AssetFactory.get_statistics()  # {crypto: 10, stocks: 10, futures: 5, etf: 5, total: 30}
```

### 5. SecureCredentialManager (`algotrendy/secure_credentials.py`)
**Purpose**: Secure storage and management of API credentials

**Key Features**:
- Loads credentials from environment variables on startup
- Stores in encrypted vault
- Maintains immutable audit log of all access
- Supports multiple brokers simultaneously

**Credential Storage Priority**:
1. Encrypted vault (if exists)
2. Environment variables (fallback)

**Usage**:
```python
from algotrendy.secure_credentials import get_credential_manager, setup_credentials

manager = get_credential_manager()

# Setup credentials
setup_credentials("bybit", api_key="key123", api_secret="secret456")

# Retrieve credentials
creds = manager.get_broker_credentials("bybit")
api_key = manager.get_api_key("bybit")
api_secret = manager.get_api_secret("bybit")

# Audit trail
history = manager.get_audit_history("bybit")  # All access logged with timestamps
```

### 6. UnifiedMemGPTTrader (`algotrendy/unified_trader.py`)
**Purpose**: Main trading template that uses all variants

**Workflow**:
1. Load configuration (VariantLoader)
2. Initialize broker with credentials (SecureCredentialManager + BrokerFactory)
3. Load strategy with parameters (StrategyResolver)
4. Get trading symbols for asset class (AssetFactory)
5. Main trading loop:
   - Get market data
   - Calculate indicators (IndicatorEngine)
   - Generate signal (Strategy)
   - Place order (Broker)
   - Monitor positions
6. Generate session report

**Usage**:
```python
import asyncio
from algotrendy.unified_trader import UnifiedMemGPTTrader

async def main():
    trader = UnifiedMemGPTTrader("algotrendy/configs/bybit_crypto_momentum_live.json")
    await trader.run_trading_session(duration_hours=24)

asyncio.run(main())
```

---

## CONFIGURATION SCHEMA

### Complete Configuration Structure
```json
{
  "trader_name": "unified_memgpt_trader",
  "version": "2.5.0",
  "description": "Description of configuration",

  "variants": {
    "broker": "bybit",              // or: alpaca, binance, okx, kraken, deribit
    "strategy": "momentum",          // or: rsi, macd, mfi, vwap
    "mode": "live",                 // or: paper, backtest, futures
    "asset_class": "crypto"         // or: stocks, futures, etf
  },

  "polling_interval_seconds": 30,

  "strategy_config": {
    "momentum": {
      "lookback_period": 20,
      "threshold_buy": 2.0,
      "threshold_sell": -2.0,
      "volatility_filter": 0.15
    },
    "rsi": {
      "period": 14,
      "oversold_level": 30,
      "overbought_level": 70,
      "upper_threshold": 0.65,
      "lower_threshold": 0.35
    }
  },

  "indicators": ["momentum", "rsi"],

  "broker_config": {
    "active_broker": "bybit",
    "test_mode": false,
    "credentials": {
      "api_key": "${BYBIT_API_KEY}",
      "api_secret": "${BYBIT_API_SECRET}"
    }
  },

  "trading_symbols": {
    "crypto": ["BTCUSDT", "ETHUSDT", "SOLUSDT"],
    "stocks": ["AAPL", "GOOGL", "TSLA"],
    "futures": ["BTC_USD_PERP"],
    "etf": ["SPY"]
  },

  "risk_settings": {
    "max_position_size": 750,
    "max_total_exposure": 3000,
    "max_concurrent_positions": 8,
    "default_leverage": 75,
    "risk_per_trade": 0.02
  }
}
```

---

## QUICK START EXAMPLES

### Example 1: Bybit Crypto Momentum Live Trading
```bash
# Set environment variables
export BYBIT_API_KEY="your_api_key"
export BYBIT_API_SECRET="your_api_secret"

# Run
python -m algotrendy.unified_trader algotrendy/configs/bybit_crypto_momentum_live.json
```

### Example 2: Alpaca Paper Trading with RSI
```bash
# Set environment variables
export ALPACA_API_KEY="your_key"
export ALPACA_API_SECRET="your_secret"

# Run
python -m algotrendy.unified_trader algotrendy/configs/alpaca_stocks_paper_rsi.json
```

### Example 3: Create Custom Configuration
```python
import json

config = {
    "trader_name": "unified_memgpt_trader",
    "version": "2.5.0",
    "variants": {
        "broker": "binance",
        "strategy": "macd",
        "mode": "paper",
        "asset_class": "crypto"
    },
    "polling_interval_seconds": 60,
    "strategy_config": {
        "macd": {
            "fast_period": 12,
            "slow_period": 26,
            "signal_period": 9,
            "threshold": 0.0001
        }
    },
    "indicators": ["macd"],
    "broker_config": {
        "active_broker": "binance",
        "test_mode": True,
        "credentials": {
            "api_key": "${BINANCE_API_KEY}",
            "api_secret": "${BINANCE_API_SECRET}"
        }
    },
    "trading_symbols": {
        "crypto": ["BTCUSDT", "ETHUSDT", "BNBUSDT"],
        "stocks": [],
        "futures": [],
        "etf": []
    },
    "risk_settings": {
        "max_position_size": 500,
        "max_total_exposure": 2000,
        "max_concurrent_positions": 5,
        "default_leverage": 1,
        "risk_per_trade": 0.01
    }
}

with open("custom_config.json", "w") as f:
    json.dump(config, f, indent=2)
```

---

## MIGRATION FROM LEGACY SYSTEM

### Old System (30+ files)
```
memgpt_bybit_live_trader.py         (1,150 lines)
memgpt_alpaca_paper_trader.py       (1,150 lines)
memgpt_binance_futures_trader.py    (1,150 lines)
memgpt_okx_demo_trader.py           (1,150 lines)
... (25+ more similar files)

Total: ~35 MB, 90% duplicated
```

### New System (Unified)
```
algotrendy/
├── unified_trader.py               (550 lines)
├── variant_loader.py               (180 lines)
├── strategy_resolver.py            (420 lines)
├── indicator_engine.py             (380 lines)
├── asset_factory.py                (350 lines)
├── secure_credentials.py           (320 lines)
└── configs/
    ├── bybit_crypto_momentum_live.json
    ├── alpaca_stocks_paper_rsi.json
    └── binance_futures_macd_paper.json

Total: ~2 MB, 0% duplicated
```

### Migration Steps
1. Set environment variables: `export BYBIT_API_KEY="..."`, etc.
2. Create configuration JSON for desired variant combination
3. Run unified trader: `python -m algotrendy.unified_trader config.json`
4. Archive legacy trader files

---

## SECURITY BEST PRACTICES

### 1. Credential Management
✅ **DO**:
- Use `SecureCredentialManager` for all credentials
- Load credentials from environment variables
- Maintain audit logs of credential access
- Rotate credentials regularly

❌ **DON'T**:
- Hardcode credentials in config files
- Commit credentials to git
- Share credential manager instances across processes
- Access credentials directly from environment

### 2. Configuration Files
✅ **DO**:
- Use environment variable references: `${BYBIT_API_KEY}`
- Keep config files in `.gitignore`
- Version control config templates without secrets
- Separate risk settings per environment

❌ **DON'T**:
- Store actual API keys in config files
- Commit config files with credentials
- Share config files across environments
- Use same risk settings for paper and live

### 3. Access Auditing
- All credential access is logged with timestamp
- Access logs stored in `secure_vault/audit.log`
- Review audit logs regularly: `manager.get_audit_history()`

---

## EXTENDING THE SYSTEM

### Add New Strategy
```python
from algotrendy.strategy_resolver import BaseStrategy, StrategyResolver

class BollingerBandsStrategy(BaseStrategy):
    def analyze(self, market_data: Dict) -> Dict:
        # Implementation
        return {'action': 'BUY', 'confidence': 0.8, ...}

# Register
StrategyResolver.register_strategy("bollinger_bands", BollingerBandsStrategy)
```

### Add New Indicator
```python
from algotrendy.indicator_engine import BaseIndicator, IndicatorEngine

class StochasticIndicator(BaseIndicator):
    def calculate(self, market_data: Dict) -> float:
        # Implementation
        return 0.75

# Register
IndicatorEngine.register_indicator("stochastic", StochasticIndicator)
```

### Add New Asset Class
```python
from algotrendy.asset_factory import AssetFactory

AssetFactory.add_symbol("SYMBOL", "crypto", "Display Name", min_quantity=0.1)
```

### Add New Broker
```python
from broker_abstraction import BrokerInterface, BrokerFactory

class NewBrokerAdapter(BrokerInterface):
    # Implement interface
    pass

BrokerFactory.register("newbroker", NewBrokerAdapter)
```

---

## MONITORING & DEBUGGING

### Session Reports
Automatically generated after each session:
- `session_report_YYYYMMDD_HHMMSS.json`
- Contains trade history, win rate, PnL metrics

### Credential Audit Log
Access via:
```python
manager = get_credential_manager()
history = manager.get_audit_history()  # All access
history_bybit = manager.get_audit_history("bybit")  # Specific broker
```

### Indicator History
Each indicator maintains value history:
```python
indicator = IndicatorEngine.get_indicator("rsi")
history = indicator.get_value_history(limit=100)
```

---

## TROUBLESHOOTING

### Issue: "No credentials found"
```
Solution:
1. Verify environment variables: env | grep BYBIT
2. Run: python -c "from algotrendy.secure_credentials import get_credential_manager; print(get_credential_manager().list_available_brokers())"
3. Set credentials: export BYBIT_API_KEY="..." && export BYBIT_API_SECRET="..."
```

### Issue: "Unknown strategy"
```
Solution:
1. Check available: python -c "from algotrendy.strategy_resolver import StrategyResolver; print(StrategyResolver.get_available_strategies())"
2. Verify config "strategy" field matches available strategy
```

### Issue: "Configuration validation failed"
```
Solution:
1. Verify all required fields present: variants, broker_config, risk_settings
2. Check for typos in variant names
3. Run: python -c "from algotrendy.variant_loader import VariantLoader; VariantLoader('config.json')"
```

---

## PERFORMANCE NOTES

- **Indicator Caching**: Indicators are cached by name + parameters
- **Symbol Formatting**: Cached per broker/asset combination
- **Credential Access**: Logged to disk (slight I/O overhead)
- **Typical Polling**: 30-60 seconds per cycle

---

## NEXT STEPS

1. ✅ Configure environment variables
2. ✅ Create configuration JSON for your strategy
3. ✅ Test with paper trading first
4. ✅ Review audit logs and trade reports
5. ✅ Adjust parameters based on performance
6. ✅ Deploy to live trading when confident

---

## SUPPORT

For issues or questions:
1. Check troubleshooting section above
2. Review audit logs: `secure_vault/audit.log`
3. Review session reports: `session_report_*.json`
4. Inspect configuration: Validate JSON schema
