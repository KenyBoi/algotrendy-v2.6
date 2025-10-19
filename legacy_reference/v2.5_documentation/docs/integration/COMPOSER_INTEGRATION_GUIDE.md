# 🎼 Composer.Trade Integration Guide

**Version**: 1.0  
**Date**: October 16, 2025  
**Status**: ✅ Ready for Integration  

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Setup & Configuration](#setup--configuration)
4. [API Reference](#api-reference)
5. [Integration with MEM](#integration-with-mem)
6. [Examples](#examples)
7. [Testing](#testing)
8. [Troubleshooting](#troubleshooting)

---

## Overview

**Composer.Trade** is a multi-chain DEX aggregation platform that enables:

- **Multi-Chain Routing**: Execute swaps across Ethereum, Polygon, Arbitrum, Optimism, Base, Avalanche
- **Liquidity Aggregation**: Smart order routing across Uniswap, SushiSwap, Balancer, and other DEXes
- **Advanced Order Types**: Market, Limit, TWAP, VWAP, DCA
- **Portfolio Rebalancing**: Automated position management
- **Real-Time Data**: WebSocket feeds for prices and order updates

### Why Composer.Trade?

| Feature | Benefit |
|---------|---------|
| **Multi-Chain** | Diversify across protocols and chains |
| **Lower Slippage** | Smart routing finds best liquidity |
| **DeFi Native** | No KYC, full self-custody |
| **Composable** | Integrates with MEM's decision engine |
| **Open Data** | Real-time feeds for ML training |

---

## Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                      MEM Trading Engine                      │
│                   (Decision Making Layer)                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
    ┌────────────┐    ┌────────────┐    ┌────────────┐
    │   Bybit    │    │  Composer  │    │  Alpaca    │
    │  (Futures) │    │   (DeFi)   │    │  (Stocks)  │
    └────────────┘    └────────────┘    └────────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
                ┌──────────┴──────────┐
                │                     │
                ▼                     ▼
           ┌─────────┐          ┌─────────┐
           │  HTTP   │          │WebSocket│
           │ Client  │          │ Client  │
           └─────────┘          └─────────┘
                │                     │
                └──────────────────┬──┘
                                   │
                     ┌─────────────┴─────────────┐
                     │                           │
                     ▼                           ▼
          ┌──────────────────┐      ┌──────────────────┐
          │ Price Feeds      │      │ Order Execution  │
          │ Portfolio Data   │      │ Status Updates   │
          └──────────────────┘      └──────────────────┘
```

### Integration Pattern

```python
# MEM sees Composer as just another broker
class BrokerManager:
    def __init__(self):
        self.brokers = {
            'bybit': BybitBroker(config),
            'composer': ComposerBroker(config),  # <-- Same interface!
            'alpaca': AlpacaBroker(config)
        }
        
    async def execute_trade(self, broker_name, signal):
        broker = self.brokers[broker_name]
        return await broker.place_order(...)  # Works the same!
```

### Key Classes

| Class | Purpose |
|-------|---------|
| `ComposerTradeHTTP` | REST API client for order execution |
| `ComposerTradeWebSocket` | WebSocket for real-time price/order updates |
| `ComposerTradeAdapter` | Bridges MEM signals to Composer swaps |
| `ComposerToken` | Represents a blockchain token |
| `ComposerPosition` | Represents a portfolio position |
| `ComposerOrder` | Represents an executed order |

---

## Setup & Configuration

### 1. Prerequisites

```bash
# Python packages
pip install aiohttp websockets python-dotenv

# Optional: For advanced features
pip install numpy pandas scipy  # Analysis
pip install pytest pytest-asyncio  # Testing
```

### 2. Environment Variables

Create `.env` file:

```bash
# Composer API credentials
COMPOSER_API_KEY="your_api_key_here"
WALLET_ADDRESS="0x..."

# Optional: For different networks
COMPOSER_NETWORK="ethereum"  # ethereum, polygon, arbitrum, optimism, base, avalanche
```

### 3. Load Configuration

```bash
# Configuration is in composer_config.json
# MEM will auto-load via BrokerManager:

from broker_abstraction import BrokerManager

manager = BrokerManager()
composer_config = manager.load_config('composer')
```

### 4. Initialize Client

```python
from composer_trade_integration import ComposerTradeHTTP

composer = ComposerTradeHTTP({
    'api_url': 'https://api.composer.trade/v1',
    'api_key': os.getenv('COMPOSER_API_KEY'),
    'wallet_address': os.getenv('WALLET_ADDRESS'),
    'network': 'ethereum'
})

# Connect
if await composer.connect():
    print("✅ Connected!")
```

---

## API Reference

### ComposerTradeHTTP

#### `async connect() -> bool`
Connect to Composer API
```python
connected = await composer.connect()
```

#### `async disconnect() -> None`
Close connection
```python
await composer.disconnect()
```

#### `async get_portfolio() -> Dict`
Fetch current portfolio across all chains
```python
portfolio = await composer.get_portfolio()
# Returns:
# {
#   'total_value_usd': 10500.50,
#   'positions': [ComposerPosition(...), ...],
#   'chains': ['ethereum', 'arbitrum']
# }
```

#### `async get_token_price(token: ComposerToken) -> float`
Get real-time token price
```python
price = await composer.get_token_price(token)
```

#### `async swap(token_in, token_out, amount, order_type, slippage) -> ComposerOrder`
Execute a token swap

**Market Order**:
```python
order = await composer.swap(
    token_in=usdc_token,
    token_out=eth_token,
    amount=1000.0,
    order_type=ComposerOrderType.MARKET,
    slippage=0.01  # 1% max slippage
)
```

**TWAP Order** (Time-Weighted Average Price):
```python
order = await composer.swap(
    token_in=usdc_token,
    token_out=eth_token,
    amount=10000.0,
    order_type=ComposerOrderType.TWAP,
    duration=3600,     # 1 hour
    interval=60        # Execute every 60 seconds
)
```

**DCA Order** (Dollar-Cost Averaging):
```python
order = await composer.swap(
    token_in=usdc_token,
    token_out=eth_token,
    amount=10000.0,
    order_type=ComposerOrderType.DCA,
    buckets=10,        # Split into 10 purchases
    interval=3600      # Buy every hour
)
```

#### `async cancel_order(order_id: str) -> bool`
Cancel a pending order
```python
cancelled = await composer.cancel_order(order_id)
```

### ComposerTradeAdapter

#### `async execute_signal(signal_symbol, signal_direction, portfolio_value, risk_percent) -> Dict`
Convert MEM trading signal to Composer swap
```python
result = await adapter.execute_signal(
    signal_symbol='ETH',
    signal_direction='buy',
    portfolio_value=10000.0,
    risk_percent=0.05  # 5% of portfolio
)

# Returns:
# {
#   'success': True,
#   'order': ComposerOrder(...),
#   'position_size_usd': 500.0
# }
```

---

## Integration with MEM

### 1. Add Composer to BrokerManager

Edit `broker_abstraction.py`:

```python
from composer_trade_integration import ComposerTradeHTTP

class BrokerManager:
    def __init__(self):
        # Load configs
        with open('composer_config.json') as f:
            composer_config = json.load(f)
        
        # Initialize brokers
        self.brokers = {
            'bybit': BybitBroker(...),
            'composer': ComposerTradeHTTP(composer_config['composer']),
            'alpaca': AlpacaBroker(...)
        }
    
    async def switch_broker(self, broker_name: str) -> bool:
        """Switch active broker"""
        if broker_name not in self.brokers:
            return False
        self.active_broker = self.brokers[broker_name]
        return True
```

### 2. Use in Trading Logic

Edit `base_memgpt_trader.py`:

```python
from composer_trade_integration import ComposerTradeAdapter

class BaseMemGPTTrader:
    def __init__(self, broker_name='composer'):
        self.broker = BrokerManager().get_broker(broker_name)
        
        # If Composer, create adapter
        if broker_name == 'composer':
            self.adapter = ComposerTradeAdapter(self.broker)
    
    async def execute_trade(self, signal):
        """Execute trading signal"""
        
        if self.broker.name == 'Composer.Trade':
            # Use adapter for DeFi signals
            result = await self.adapter.execute_signal(
                signal_symbol=signal['symbol'],
                signal_direction=signal['direction'],
                portfolio_value=self.portfolio_value,
                risk_percent=self.risk_percent
            )
            return result
        else:
            # Use standard broker execution
            return await self.broker.place_order(...)
```

### 3. Runtime Selection

In your trading launcher:

```python
# Use via command line
python launch_menu.py

# Select broker: "composer"
# Select network: "ethereum"
# Select signal: "ETH" 
# Ready to trade!
```

---

## Examples

### Example 1: Simple Portfolio Query

```python
import asyncio
from composer_trade_integration import ComposerTradeHTTP

async def main():
    composer = ComposerTradeHTTP({
        'api_key': 'your-key',
        'wallet_address': '0x...'
    })
    
    if await composer.connect():
        portfolio = await composer.get_portfolio()
        
        print(f"Total Value: ${portfolio['total_value_usd']:,.2f}")
        for pos in portfolio['positions']:
            print(f"  {pos.token.symbol}: {pos.quantity} @ ${pos.current_price} | {pos.percentage:+.2f}%")
        
        await composer.disconnect()

asyncio.run(main())
```

**Output**:
```
Total Value: $25,500.00
  ETH: 5.2 @ $2,000 | +15.5%
  ARB: 1000 @ $15 | -2.3%
  USDC: 500 @ $1.00 | +0.0%
```

### Example 2: Execute a Buy Signal

```python
async def buy_eth_signal():
    composer = ComposerTradeHTTP({...})
    adapter = ComposerTradeAdapter(composer)
    
    if await composer.connect():
        result = await adapter.execute_signal(
            signal_symbol='ETH',
            signal_direction='buy',
            portfolio_value=10000,
            risk_percent=0.10  # 10% position size
        )
        
        if result['success']:
            order = result['order']
            print(f"✅ Bought {order.amount_out} ETH")
            print(f"   Execution price: ${order.fill_price}")
            print(f"   Slippage: {order.slippage_percent:.2f}%")
        else:
            print(f"❌ Failed: {result['error']}")
        
        await composer.disconnect()

asyncio.run(buy_eth_signal())
```

### Example 3: DCA Strategy

```python
async def dca_into_position():
    composer = ComposerTradeHTTP({...})
    
    if await composer.connect():
        # Buy $5,000 ETH split into 10 purchases over 10 hours
        order = await composer.swap(
            token_in=usdc_token,
            token_out=eth_token,
            amount=5000.0,
            order_type=ComposerOrderType.DCA,
            buckets=10,
            interval=3600  # 1 hour between purchases
        )
        
        print(f"DCA Order Created: {order.order_id}")
        print(f"Will execute {order.amount_in} USDC → ETH over 10 hours")

asyncio.run(dca_into_position())
```

### Example 4: TWAP Strategy

```python
async def large_order_with_twap():
    composer = ComposerTradeHTTP({...})
    
    if await composer.connect():
        # Execute $50,000 sell over 1 hour with TWAP
        order = await composer.swap(
            token_in=eth_token,
            token_out=usdc_token,
            amount=25.0,  # 25 ETH
            order_type=ComposerOrderType.TWAP,
            duration=3600,
            interval=60  # Execute every 60 seconds
        )
        
        print(f"TWAP Order: {order.order_id}")
        print(f"Will sell {order.amount_in} ETH over 1 hour")
        print(f"Target price: ${order.fill_price}")

asyncio.run(large_order_with_twap())
```

---

## Testing

### Unit Tests

Create `test_composer_integration.py`:

```python
import pytest
import asyncio
from composer_trade_integration import (
    ComposerTradeHTTP, ComposerToken, ComposerChain, ComposerOrderType
)

@pytest.mark.asyncio
async def test_composer_connect():
    composer = ComposerTradeHTTP({
        'api_url': 'https://api.composer.trade/v1',
        'api_key': 'test-key',
        'wallet_address': '0x...'
    })
    
    # Should not raise
    result = await composer.connect()
    assert isinstance(result, bool)
    
    if result:
        await composer.disconnect()

@pytest.mark.asyncio
async def test_get_portfolio():
    composer = ComposerTradeHTTP({...})
    
    if await composer.connect():
        portfolio = await composer.get_portfolio()
        
        assert 'total_value_usd' in portfolio
        assert 'positions' in portfolio
        assert 'chains' in portfolio
        
        await composer.disconnect()

@pytest.mark.asyncio
async def test_swap_execution():
    # Test in testnet first!
    composer = ComposerTradeHTTP({...})
    
    if await composer.connect():
        usdc = ComposerToken(
            address='0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48',
            symbol='USDC',
            chain=ComposerChain.ETHEREUM,
            decimals=6
        )
        
        eth = ComposerToken(
            address='0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2',
            symbol='ETH',
            chain=ComposerChain.ETHEREUM,
            decimals=18
        )
        
        # Execute small test order
        order = await composer.swap(
            token_in=usdc,
            token_out=eth,
            amount=100.0,  # $100
            order_type=ComposerOrderType.MARKET
        )
        
        assert order.order_id is not None
        assert order.status in ['pending', 'executed']
        
        await composer.disconnect()
```

### Integration Tests

```bash
# Run all tests
pytest test_composer_integration.py -v

# Run specific test
pytest test_composer_integration.py::test_swap_execution -v

# With coverage
pytest --cov=composer_trade_integration test_composer_integration.py
```

---

## Troubleshooting

### Issue: "Connection failed" 

**Solution**: Check API key and wallet address
```bash
# Verify credentials
echo $COMPOSER_API_KEY
echo $WALLET_ADDRESS
```

### Issue: "High slippage"

**Use TWAP for large orders**:
```python
# Instead of market order:
order = await composer.swap(
    ...,
    order_type=ComposerOrderType.TWAP,
    duration=1800  # 30 minutes
)
```

### Issue: "Order timeout"

**Increase timeout in config**:
```json
{
  "composer": {
    "order_timeout_seconds": 120  # Increase from 60
  }
}
```

### Issue: "Insufficient liquidity"

**Use aggregated routing**:
```python
# Composer automatically routes across:
# - Uniswap V2 & V3
# - SushiSwap
# - Balancer
# - Curve
# No manual selection needed!
```

---

## Next Steps

1. ✅ **Created**: Composer integration module
2. ✅ **Created**: Configuration file
3. ⏳ **Next**: Deploy to MEM's broker abstraction layer
4. ⏳ **Next**: Create Composer-specific signal types
5. ⏳ **Next**: Build portfolio optimizer using Composer data

---

## Resources

- **Composer Docs**: https://docs.composer.trade
- **API Reference**: https://api.composer.trade/docs
- **WebSocket Guide**: https://docs.composer.trade/websocket
- **Token Registry**: https://data.composer.trade/tokens

---

**Version**: 1.0  
**Last Updated**: October 16, 2025  
**Maintainer**: AlgoTrendy Development Team
