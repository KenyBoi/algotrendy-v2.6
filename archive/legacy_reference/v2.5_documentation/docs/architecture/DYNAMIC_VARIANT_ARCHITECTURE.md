# Dynamic Variant Trading System Architecture
## Consolidating 30+ Trader Files into ONE Configurable Template

**Status**: Architectural Design Document  
**Date**: 2025-10-16  
**Purpose**: Replace 90% duplicated trader files with dynamic variant system

---

## EXECUTIVE SUMMARY

Currently: **30+ `memgpt_*.py` files** (~1,150 lines each, 90% duplication)  
Goal: **1 unified trader template** with 4 dynamic variant dimensions

### Variant Dimensions

| Dimension | Current Files | Proposed Config | Examples |
|-----------|---------------|-----------------|----------|
| **Broker** | bybit, alpaca, binance, okx | `broker: "bybit"` | bybit, alpaca, binance, okx, kraken, deribit |
| **Strategy** | momentum, rsi, macd, mfi | `strategy: "momentum"` | momentum, rsi, macd, mfi, vwap |
| **Mode** | live, paper, futures | `mode: "live"` | live, paper, backtest, futures |
| **Asset Class** | crypto, stocks, futures | `asset_class: "crypto"` | crypto, stocks, futures, etf |

---

## CURRENT DUPLICATION ANALYSIS

### Bybit Live Trader vs Alpaca Paper Trader

#### IDENTICAL SECTIONS (90% of code)
```
✓ Signal generation structure
✓ Position tracking dictionary
✓ Trade logging system
✓ Learning model framework
✓ Main trading loop pattern
✓ Risk management calculations
✓ Performance metrics tracking
✓ Entry/exit condition logic
✓ Configuration initialization
```

#### DIFFERENT SECTIONS (10% of code)
```
✗ API authentication (Bybit HMAC vs Alpaca OAuth)
✗ REST endpoint URLs
✗ Order placement method
✗ Market data retrieval
✗ Signal parameters (momentum vs RSI thresholds)
✗ Position sizing logic
✗ Symbol list (crypto pairs vs stock tickers)
```

---

## PROPOSED ARCHITECTURE

### 1. Variant Configuration Schema

```json
{
  "trader_name": "unified_memgpt_trader",
  "version": "2.5.0",
  
  "variants": {
    "broker": "bybit",           // or: alpaca, binance, okx, kraken
    "strategy": "momentum",       // or: rsi, macd, mfi, vwap
    "mode": "live",              // or: paper, backtest, futures
    "asset_class": "crypto"      // or: stocks, futures, etf
  },

  "strategy_config": {
    "momentum": {
      "lookback_period": 20,
      "threshold_buy": 2.0,       // % change threshold
      "threshold_sell": -2.0,
      "volatility_filter": 0.15
    },
    "rsi": {
      "period": 14,
      "oversold_level": 30,
      "overbought_level": 70,
      "upper_threshold": 0.65,
      "lower_threshold": 0.35
    },
    "macd": {
      "fast_period": 12,
      "slow_period": 26,
      "signal_period": 9,
      "threshold": 0.0001
    }
  },

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
    "futures": ["BTC_USD_PERP", "ETH_USD_PERP"]
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

### 2. Broker Abstraction Layer (Already Exists!)

**Current**: `broker_abstraction.py` has `BrokerInterface` abstract base  
**Usage**: Leverage existing abstraction for variants

```python
class BrokerInterface(ABC):
    """Abstract broker interface for all variants"""
    
    @abstractmethod
    def connect(self): pass
    
    @abstractmethod
    def get_balance(self): pass
    
    @abstractmethod
    def get_market_price(self, symbol): pass
    
    @abstractmethod
    def place_order(self, symbol, qty, side): pass
    
    @abstractmethod
    def close_position(self, symbol): pass
```

**Variants Needed**:
- ✓ BybitBroker (exists)
- ✗ AlpacaBroker (create)
- ✗ BinanceBroker (upgrade stub)
- ✗ OKXBroker (upgrade stub)

### 3. Strategy Resolver System

```python
class StrategyResolver:
    """Dynamically load and apply trading strategies"""
    
    STRATEGIES = {
        'momentum': MomentumStrategy,
        'rsi': RSIStrategy,
        'macd': MACDStrategy,
        'mfi': MFIStrategy,
        'vwap': VWAPStrategy
    }
    
    @staticmethod
    def get_strategy(strategy_name: str) -> BaseStrategy:
        """Return strategy instance by name"""
        if strategy_name not in StrategyResolver.STRATEGIES:
            raise ValueError(f"Unknown strategy: {strategy_name}")
        return StrategyResolver.STRATEGIES[strategy_name]()
    
    @staticmethod
    def generate_signal(strategy: BaseStrategy, market_data):
        """Generate trading signal using strategy"""
        return strategy.analyze(market_data)
```

### 4. Asset Factory System

```python
class AssetFactory:
    """Handle different asset classes and symbols"""
    
    @staticmethod
    def get_symbols(asset_class: str) -> List[str]:
        """Return symbols for asset class"""
        symbols = {
            'crypto': ['BTCUSDT', 'ETHUSDT', 'SOLUSDT', 'ADAUSDT'],
            'stocks': ['AAPL', 'GOOGL', 'TSLA', 'MSFT'],
            'futures': ['BTC_USD_PERP', 'ETH_USD_PERP']
        }
        return symbols.get(asset_class, [])
    
    @staticmethod
    def format_symbol(symbol: str, asset_class: str, broker: str) -> str:
        """Format symbol according to broker requirements"""
        # Different brokers expect different symbol formats
        formatters = {
            ('crypto', 'bybit'): lambda s: f"{s}",
            ('crypto', 'binance'): lambda s: f"{s}",
            ('stocks', 'alpaca'): lambda s: f"{s}",
        }
        formatter = formatters.get((asset_class, broker), lambda s: s)
        return formatter(symbol)
```

### 5. Variant Loader Class

```python
class VariantLoader:
    """Load and apply variants at runtime"""
    
    def __init__(self, config_path: str):
        self.config = self._load_config(config_path)
        self.broker = self._load_broker()
        self.strategy = self._load_strategy()
        self.symbols = self._load_symbols()
    
    def _load_broker(self) -> BrokerInterface:
        """Load broker variant"""
        broker_name = self.config['variants']['broker']
        broker_class = BrokerFactory.get_broker(broker_name)
        credentials = self.config['broker_config']['credentials']
        return broker_class(**credentials)
    
    def _load_strategy(self) -> BaseStrategy:
        """Load strategy variant"""
        strategy_name = self.config['variants']['strategy']
        strategy_params = self.config['strategy_config'].get(strategy_name, {})
        return StrategyResolver.get_strategy(strategy_name, **strategy_params)
    
    def _load_symbols(self) -> List[str]:
        """Load symbols for asset class"""
        asset_class = self.config['variants']['asset_class']
        return AssetFactory.get_symbols(asset_class)
    
    def _load_config(self, config_path: str) -> Dict:
        """Load configuration from JSON"""
        with open(config_path, 'r') as f:
            return json.load(f)
```

### 6. Unified Trader Template

```python
class UnifiedMemGPTTrader:
    """Single configurable trader template using variants"""
    
    def __init__(self, config_path: str):
        self.variant_loader = VariantLoader(config_path)
        self.config = self.variant_loader.config
        
        # Load variants
        self.broker = self.variant_loader.broker
        self.strategy = self.variant_loader.strategy
        self.symbols = self.variant_loader.symbols
        
        # State management
        self.active_positions = {}
        self.trade_log = []
        self.performance_history = []
        self.market_data = {}
    
    async def run_trading_session(self):
        """Main trading loop - same regardless of variants"""
        
        print(f"🧠 MEMGPT UNIFIED TRADER")
        print(f"   Broker: {self.config['variants']['broker']}")
        print(f"   Strategy: {self.config['variants']['strategy']}")
        print(f"   Mode: {self.config['variants']['mode']}")
        print(f"   Asset Class: {self.config['variants']['asset_class']}")
        
        cycle = 1
        while True:
            print(f"\n⏰ Trading Cycle #{cycle}")
            
            # This logic is IDENTICAL regardless of variants!
            for symbol in self.symbols:
                try:
                    # Get market data (broker-specific via variant)
                    market_data = self.broker.get_market_data(symbol)
                    if not market_data:
                        continue
                    
                    # Generate signal (strategy-specific via variant)
                    signal = self.strategy.generate_signal(symbol, market_data)
                    if not signal or signal['action'] == 'HOLD':
                        continue
                    
                    # Place order (broker & mode-specific via variant)
                    if symbol not in self.active_positions:
                        order = self.broker.place_order(signal)
                        if order:
                            self._track_position(symbol, signal, order)
                    
                except Exception as e:
                    logger.error(f"Error processing {symbol}: {e}")
            
            # Monitor positions (identical logic)
            self._monitor_positions()
            
            cycle += 1
            await asyncio.sleep(30)  # Polling interval
    
    def _track_position(self, symbol, signal, order):
        """Track open position"""
        self.active_positions[symbol] = {
            'order_id': order['id'],
            'symbol': symbol,
            'entry_price': signal['entry_price'],
            'position_size': signal['position_size'],
            'entry_time': datetime.now()
        }
    
    def _monitor_positions(self):
        """Monitor and manage open positions"""
        for symbol, position in list(self.active_positions.items()):
            current_price = self.broker.get_market_price(symbol)
            pnl_pct = (current_price - position['entry_price']) / position['entry_price']
            
            # Exit logic (identical)
            if pnl_pct > 0.05 or pnl_pct < -0.02:  # 5% TP or 2% SL
                self.broker.close_position(symbol)
                del self.active_positions[symbol]
```

---

## BASE STRATEGY CLASSES

```python
class BaseStrategy(ABC):
    """Abstract base for all trading strategies"""
    
    def __init__(self, **params):
        self.params = params
    
    @abstractmethod
    def analyze(self, market_data: Dict) -> Dict:
        """Generate trading signal from market data"""
        pass

class MomentumStrategy(BaseStrategy):
    """Momentum-based strategy"""
    
    def analyze(self, market_data):
        price_change = market_data.get('change_pct', 0)
        threshold_buy = self.params.get('threshold_buy', 2.0)
        
        if price_change > threshold_buy:
            return {'action': 'BUY', 'confidence': 0.8}
        elif price_change < -threshold_buy:
            return {'action': 'SELL', 'confidence': 0.8}
        return {'action': 'HOLD', 'confidence': 0.3}

class RSIStrategy(BaseStrategy):
    """RSI-based strategy"""
    
    def analyze(self, market_data):
        rsi = market_data.get('rsi', 50)
        oversold = self.params.get('oversold_level', 30)
        overbought = self.params.get('overbought_level', 70)
        
        if rsi < oversold:
            return {'action': 'BUY', 'confidence': 0.75}
        elif rsi > overbought:
            return {'action': 'SELL', 'confidence': 0.75}
        return {'action': 'HOLD', 'confidence': 0.3}
```

---

## MIGRATION PLAN

### Phase 1: Foundation (Week 1)
- [x] Broker abstraction layer exists
- [ ] Create AlpacaBroker implementation
- [ ] Create UnifiedMemGPTTrader base class
- [ ] Create VariantLoader class

### Phase 2: Strategies (Week 2)
- [ ] Extract MomentumStrategy from bybit trader
- [ ] Extract RSIStrategy from alpaca trader
- [ ] Create MACD, MFI, VWAP strategies
- [ ] Implement StrategyResolver

### Phase 3: Assets (Week 3)
- [ ] Create AssetFactory for symbol management
- [ ] Implement crypto symbol resolution
- [ ] Implement stock symbol resolution
- [ ] Implement futures symbol resolution

### Phase 4: Integration (Week 4)
- [ ] Create unified config schema
- [ ] Integrate all variants
- [ ] Create launcher with variant selection
- [ ] Archive legacy files

---

## FILE CONSOLIDATION MAPPING

### FROM (Legacy - 30+ files)
```
memgpt_bybit_live_trader.py          → CONSOLIDATE
memgpt_alpaca_paper_trader.py        → CONSOLIDATE
memgpt_binance_futures_trader.py     → CONSOLIDATE
memgpt_okx_demo_trader.py            → CONSOLIDATE
memgpt_coaiusdt_futures_trader.py    → CONSOLIDATE
... (25+ more files)
```

### TO (New - 1 file + configs)
```
v2.5/
├── unified_memgpt_trader.py         (NEW - configurable)
├── strategy/
│   ├── momentum.py
│   ├── rsi.py
│   ├── macd.py
│   └── base.py
├── configs/
│   ├── bybit_crypto_live.json
│   ├── alpaca_stocks_paper.json
│   ├── binance_futures_live.json
│   └── template.json
└── brokers/
    ├── alpaca.py                     (NEW - implement)
    ├── binance.py                    (NEW - upgrade stub)
    └── ... (existing adapters)
```

---

## USAGE EXAMPLES

### Example 1: Bybit Crypto Live Trading
```python
trader = UnifiedMemGPTTrader("configs/bybit_crypto_live.json")
await trader.run_trading_session()
```

### Example 2: Alpaca Stocks Paper Trading
```python
trader = UnifiedMemGPTTrader("configs/alpaca_stocks_paper.json")
await trader.run_trading_session()
```

### Example 3: Binance Futures RSI Strategy
```python
trader = UnifiedMemGPTTrader("configs/binance_futures_rsi.json")
await trader.run_trading_session()
```

### Example 4: Creating New Variant at Runtime
```python
config = {
    "variants": {
        "broker": "kraken",
        "strategy": "macd",
        "mode": "paper",
        "asset_class": "crypto"
    },
    "strategy_config": { ... },
    "broker_config": { ... }
}

with open("kraken_crypto_macd.json", "w") as f:
    json.dump(config, f)

trader = UnifiedMemGPTTrader("kraken_crypto_macd.json")
```

---

## BENEFITS

✅ **Code Deduplication**: 30+ files → 1 template (90% reduction)  
✅ **Easier Maintenance**: Changes in one place, not 30+  
✅ **Configuration-Driven**: No code changes to switch brokers/strategies  
✅ **Scalable**: Add new brokers/strategies without modifying core  
✅ **Testable**: Each variant can be tested independently  
✅ **Backward Compatible**: Legacy files can coexist during migration  

---

## NEXT STEPS

1. Review this architecture with user
2. Create AlpacaBroker adapter (if approved)
3. Implement VariantLoader
4. Extract strategies into separate classes
5. Create unified trader and integrate
6. Test with each broker/strategy combination
7. Archive legacy files
