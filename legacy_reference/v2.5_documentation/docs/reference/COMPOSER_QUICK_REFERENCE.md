# 🎼 COMPOSER.TRADE QUICK REFERENCE

**One-page integration guide for developers**

---

## 📦 INSTALLATION (2 minutes)

```bash
# 1. Set credentials
export COMPOSER_API_KEY="your-key"
export WALLET_ADDRESS="0x..."

# 2. Install dependencies
pip install aiohttp websockets python-dotenv

# 3. Run launcher
bash launch_composer_integration.sh
```

---

## 🔗 BASIC USAGE

### Initialize Client
```python
from composer_trade_integration import ComposerTradeHTTP

composer = ComposerTradeHTTP({
    'api_key': 'your-key',
    'wallet_address': '0x...'
})

await composer.connect()
```

### Get Portfolio
```python
portfolio = await composer.get_portfolio()
print(f"Value: ${portfolio['total_value_usd']}")

for pos in portfolio['positions']:
    print(f"  {pos.token.symbol}: {pos.quantity} @ ${pos.current_price}")
```

### Buy ETH
```python
from composer_trade_integration import ComposerOrderType

order = await composer.swap(
    token_in=usdc_token,
    token_out=eth_token,
    amount=1000.0,
    order_type=ComposerOrderType.MARKET
)
```

### DCA Strategy (Dollar-Cost Average)
```python
order = await composer.swap(
    token_in=usdc_token,
    token_out=eth_token,
    amount=10000.0,
    order_type=ComposerOrderType.DCA,
    buckets=10,       # 10 purchases
    interval=3600     # 1 hour apart
)
```

---

## 🚀 MEM INTEGRATION

### In broker_abstraction.py:
```python
from composer_trade_integration import ComposerTradeHTTP

class BrokerManager:
    def __init__(self):
        self.brokers = {
            'composer': ComposerTradeHTTP(config),  # ← Add this
            'bybit': BybitBroker(...),
        }
```

### In base_memgpt_trader.py:
```python
async def execute_trade(self, signal):
    if self.broker.name == 'Composer.Trade':
        return await self.adapter.execute_signal(
            signal['symbol'],
            signal['direction'],
            self.portfolio_value
        )
```

---

## 📊 DATA MODELS

### ComposerToken
```python
ComposerToken(
    address='0x...',      # Smart contract address
    symbol='ETH',         # Ticker symbol
    chain=ComposerChain.ETHEREUM,
    decimals=18,
    price_usd=2000.0
)
```

### ComposerPosition
```python
ComposerPosition(
    token=eth_token,
    quantity=5.2,
    entry_price=1732.0,
    current_price=2000.0,
    unrealized_pnl=1392.6,    # P&L in USD
    percentage=15.55          # P&L %
)
```

### ComposerOrder
```python
ComposerOrder(
    order_id='order-12345',
    token_in=usdc_token,
    token_out=eth_token,
    amount_in=1000.0,
    amount_out=0.5,
    order_type=ComposerOrderType.MARKET,
    status='executed',
    fill_price=2000.0,
    slippage_percent=0.05
)
```

---

## ⚙️ CONFIGURATION

### composer_config.json
```json
{
  "composer": {
    "api_url": "https://api.composer.trade/v1",
    "api_key": "${COMPOSER_API_KEY}",
    "wallet_address": "${WALLET_ADDRESS}",
    "chains": ["ethereum", "arbitrum", "polygon", ...],
    "default_slippage": 0.01,
    "token_registry": {...}
  }
}
```

### Advanced Orders
```json
{
  "twap": {
    "duration_seconds": 3600,
    "interval_seconds": 60
  },
  "dca": {
    "buckets": 10,
    "interval_seconds": 3600
  }
}
```

---

## 🧪 TESTING

```bash
# Run all tests
pytest test_composer_integration.py -v

# Run specific test
pytest test_composer_integration.py::TestComposerTradeHTTP::test_init -v

# With coverage
pytest --cov=composer_trade_integration test_composer_integration.py

# Run launcher menu
bash launch_composer_integration.sh
# → Option 1: Test Connection
# → Option 2: Query Portfolio
# → Option 3: Run Tests
```

---

## 📈 ORDER TYPES

| Type | Use Case | Example |
|------|----------|---------|
| **MARKET** | Quick execution | Buy now at current price |
| **LIMIT** | Price-sensitive | Buy only if ETH < $1900 |
| **TWAP** | Large orders | Sell 25 ETH over 1 hour |
| **VWAP** | Volume-weighted | Buy while averaging volume |
| **DCA** | Recurring | Buy $1000 ETH every day for 10 days |

---

## 🔒 SECURITY

```python
# API key in environment
export COMPOSER_API_KEY="..."  # ← Never hardcode!

# Use .env file
COMPOSER_API_KEY=your-key-here

# In code
import os
api_key = os.getenv('COMPOSER_API_KEY')
```

---

## 🌐 SUPPORTED CHAINS

```python
ComposerChain.ETHEREUM    # Ethereum Mainnet
ComposerChain.POLYGON     # Polygon
ComposerChain.ARBITRUM    # Arbitrum One
ComposerChain.OPTIMISM    # Optimism
ComposerChain.BASE        # Base
ComposerChain.AVALANCHE   # Avalanche C-Chain
```

---

## 📱 EXAMPLE: COMPLETE BUY FLOW

```python
import asyncio
from composer_trade_integration import (
    ComposerTradeHTTP,
    ComposerTradeAdapter,
    ComposerToken,
    ComposerChain
)

async def main():
    # Initialize
    composer = ComposerTradeHTTP({
        'api_key': 'your-key',
        'wallet_address': '0x...'
    })
    
    # Connect
    if not await composer.connect():
        print("❌ Failed to connect")
        return
    
    # Get portfolio
    portfolio = await composer.get_portfolio()
    print(f"💼 Portfolio: ${portfolio['total_value_usd']:.2f}")
    
    # Create adapter
    adapter = ComposerTradeAdapter(composer)
    
    # Execute buy signal
    result = await adapter.execute_signal(
        signal_symbol='ETH',
        signal_direction='buy',
        portfolio_value=portfolio['total_value_usd'],
        risk_percent=0.05  # 5% of portfolio
    )
    
    if result['success']:
        order = result['order']
        print(f"✅ Executed: {order.amount_out} {order.token_out.symbol}")
        print(f"   Price: ${order.fill_price:.2f}")
        print(f"   Slippage: {order.slippage_percent:.2f}%")
    else:
        print(f"❌ Failed: {result['error']}")
    
    # Disconnect
    await composer.disconnect()

# Run
asyncio.run(main())
```

---

## 🐛 DEBUGGING

```python
# Enable logging
import logging
logging.basicConfig(level=logging.DEBUG)

# Check connection
if composer.connected:
    print("✅ Connected")
else:
    print("❌ Not connected")

# Check metrics
print(composer.metrics)

# Print configuration
import json
with open('composer_config.json') as f:
    config = json.load(f)
    print(json.dumps(config, indent=2))
```

---

## 💾 COMMON PATTERNS

### Polling Portfolio
```python
import asyncio

async def monitor_portfolio():
    while True:
        portfolio = await composer.get_portfolio()
        print(f"Value: ${portfolio['total_value_usd']:.2f}")
        await asyncio.sleep(30)  # Every 30 seconds
```

### Error Handling
```python
try:
    order = await composer.swap(...)
except Exception as e:
    print(f"❌ Order failed: {e}")
    # Log error
    # Try alternative execution
    # Alert user
```

### Multi-Token Swaps
```python
# Swap multiple tokens to a single token
results = []
for symbol in ['ARB', 'OP', 'USDC']:
    result = await adapter.execute_signal(
        signal_symbol='ETH',  # All to ETH
        signal_direction='buy',
        portfolio_value=10000
    )
    results.append(result)
```

---

## 📚 LEARN MORE

- **Full Guide**: `COMPOSER_INTEGRATION_GUIDE.md`
- **Deployment Summary**: `COMPOSER_DEPLOYMENT_SUMMARY.md`
- **Configuration**: `composer_config.json`
- **Tests**: `test_composer_integration.py`
- **Launcher**: `bash launch_composer_integration.sh`

---

## ⚡ PERFORMANCE TIPS

1. **Connection Pooling**: Reuse client instances
2. **Batch Queries**: Get portfolio once, use multiple times
3. **WebSocket**: Use for real-time price feeds
4. **Rate Limiting**: Respect API rate limits (check headers)
5. **Async**: Use `asyncio` for concurrent operations

---

## 🆘 TROUBLESHOOTING

| Error | Solution |
|-------|----------|
| `API key invalid` | Check COMPOSER_API_KEY in .env |
| `High slippage` | Use TWAP/VWAP for large orders |
| `Connection timeout` | Check network, increase timeout |
| `Insufficient liquidity` | Composer routes optimally, but LP may be limited |
| `Order not found` | Verify order_id, may need time to settle |

---

**Quick Reference v1.0** | October 16, 2025 | Ready to use!
