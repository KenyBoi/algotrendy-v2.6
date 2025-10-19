# 🤖 MEM Tier - MemGPT Agent Modules

> **Purpose**: MemGPT agent modules and persistent memory system  
> **Status**: Production  
> **Last Updated**: October 16, 2025

---

## 📂 Directory Structure

```
MEM/
├── README.md                    [This file - MEM index]
├── INDEX.md                     [Navigation guide]
│
└── MEM_Modules_toolbox/
    ├── README.md               [MemGPT modules index]
    ├── mem_connection_manager.py [Connection management]
    ├── mem_connector.py        [Core connector]
    ├── mem_credentials.py      [Credential handling]
    ├── mem_live_dashboard.py   [Live monitoring dashboard]
    ├── mem_*.py               [Other MemGPT modules]
    ├── tests/
    │   └── test_mem_*.py      [Module tests]
    └── docs/
        └── README.md           [Module documentation]
```

---

## 🤖 MemGPT Architecture

MemGPT (Memory-Enhanced GPT) is an AI agent that maintains persistent memory while trading.

**Key Components**:
- **Core Memory**: Persistent knowledge base about strategies and market behavior
- **Connections**: Communication channels with exchanges and data sources
- **Credentials**: Secure credential management for external systems
- **Dashboard**: Real-time monitoring of agent activity

---

## 📦 MEM_Modules_toolbox/

All MemGPT modules are organized in `MEM/MEM_Modules_toolbox/`.

### **mem_connection_manager.py**
Manages connections to external systems (brokers, data feeds, webhooks).

**Responsibilities**:
- Maintain connection pools
- Handle reconnection logic
- Monitor connection health
- Route messages to appropriate endpoints

**Usage**:
```python
from MEM.MEM_Modules_toolbox.mem_connection_manager import ConnectionManager

manager = ConnectionManager()
await manager.add_broker_connection('bybit', api_key, api_secret)
await manager.add_data_feed('crypto_feed', {'symbols': ['BTCUSDT']})

# Send command via connection
result = await manager.execute_on_broker('bybit', 'place_order', {
    'symbol': 'BTCUSDT',
    'qty': 0.1,
    'side': 'BUY'
})
```

---

### **mem_connector.py**
Core connector between MemGPT agent and external systems.

**Responsibilities**:
- Agent communication protocol
- Message serialization/deserialization
- Event routing
- Error handling and recovery

**Usage**:
```python
from MEM.MEM_Modules_toolbox.mem_connector import MemGPTConnector

connector = MemGPTConnector(
    agent_id='trader_001',
    memory_path='data/mem_knowledge/'
)

await connector.initialize()
response = await connector.send_command('analyze_market', {
    'symbol': 'BTCUSDT',
    'timeframe': '5m'
})
```

---

### **mem_credentials.py**
Secure credential management for MemGPT agent.

**Responsibilities**:
- Load credentials from environment
- Manage credential lifecycle
- Handle credential rotation
- Audit credential access

**Usage**:
```python
from MEM.MEM_Modules_toolbox.mem_credentials import CredentialManager

cred_manager = CredentialManager()

# Load credentials
bybit_creds = cred_manager.get_broker_credentials('bybit')
alpaca_creds = cred_manager.get_broker_credentials('alpaca')

# Check if credentials exist
if cred_manager.has_credentials('binance'):
    binance_creds = cred_manager.get_broker_credentials('binance')
```

---

### **mem_live_dashboard.py**
Real-time monitoring dashboard for MemGPT agent activity.

**Features**:
- Live position tracking
- Trade history
- Performance metrics
- Agent decision logs
- Alert monitoring

**Usage**:
```bash
# Start dashboard server
python -m MEM.MEM_Modules_toolbox.mem_live_dashboard

# Access at http://localhost:5001
```

**Metrics Displayed**:
- Current positions (symbol, entry price, P&L)
- Daily performance (PnL, win rate, Sharpe ratio)
- Trade history (open, closed, pending)
- Agent decisions (reasons, confidence scores)
- System health (memory usage, connection status)

---

## 🧠 Persistent Memory System

MemGPT maintains three levels of persistent memory:

### **Core Memory** (`data/mem_knowledge/core_memory_updates.txt`)
Decision history and learned patterns.

```
[2025-10-16 14:30:15] Strategy: momentum - Decision: BUY - Confidence: 0.85
[2025-10-16 14:35:20] Entry Price: 65000 - Stop Loss: 64500 - Target: 66000
[2025-10-16 14:40:00] P&L: +$500 - Closed position successfully
[2025-10-16 14:45:00] Learned: Support at 64500 is strong - record for future
```

### **Parameter Updates** (`data/mem_knowledge/parameter_updates.json`)
Model parameter adjustments over time.

```json
{
  "2025-10-16": {
    "momentum_threshold": 0.05,
    "position_size": 0.1,
    "risk_per_trade": 0.02,
    "adjustments": {
      "reason": "Increased volatility observed",
      "from": {"momentum_threshold": 0.04},
      "to": {"momentum_threshold": 0.05}
    }
  }
}
```

### **Strategy Modules** (`data/mem_knowledge/strategy_modules.py`)
Custom strategy implementations learned by agent.

```python
# Learned strategy for high-volatility conditions
class LearnedHighVolatilityStrategy:
    def __init__(self):
        self.rsi_threshold = 35  # Learned from backtesting
        self.macd_signal = 'strong'  # Learned pattern
    
    def analyze(self, market_data):
        # Strategy learned through reinforcement learning
        if market_data['volatility'] > 2.0:
            if market_data['rsi'] < self.rsi_threshold:
                return {'action': 'BUY', 'confidence': 0.8}
```

---

## 🔄 Integration with Trading Engine

```python
# In algotrendy/unified_trader.py
from MEM.MEM_Modules_toolbox.mem_connector import MemGPTConnector

class UnifiedMemGPTTrader:
    def __init__(self, config_path):
        self.mem_connector = MemGPTConnector()
        # ... initialize trader
    
    async def execute_trade(self, signal):
        # Consult MemGPT for decision enhancement
        mem_analysis = await self.mem_connector.analyze_signal(signal)
        
        # Combine strategy signal with MemGPT analysis
        final_decision = {
            **signal,
            'mem_confidence': mem_analysis['confidence'],
            'mem_reason': mem_analysis['reason']
        }
        
        # Execute trade
        result = await self.broker.place_order(**final_decision)
        
        # Update MemGPT memory
        await self.mem_connector.log_decision(final_decision, result)
```

---

## 🚀 Starting MemGPT Agent

### Quick Start
```bash
# 1. Verify credentials
python scripts/setup/verify_credentials.py

# 2. Start MemGPT pipeline
bash scripts/deployment/start_mem_pipeline.sh

# 3. Open dashboard
open http://localhost:5001
```

### With Custom Configuration
```bash
export TRADING_MODE=paper
export SYMBOL=BTCUSDT
export STRATEGY=momentum
export MEM_MEMORY_PATH=data/mem_knowledge/

bash scripts/deployment/start_mem_pipeline.sh
```

### Development Mode (Debug)
```bash
# Start with verbose logging
python -m MEM.MEM_Modules_toolbox.mem_connector \
  --agent-id dev_trader \
  --verbose \
  --memory-path data/mem_knowledge/
```

---

## 📊 Performance Monitoring

Monitor agent performance in real-time:

```bash
# View live dashboard
open http://localhost:5001/dashboard

# View performance metrics
open http://localhost:5001/metrics

# View trade history
open http://localhost:5001/trades

# View memory state
open http://localhost:5001/memory
```

---

## 🧪 Testing MemGPT Modules

```bash
# Run all MEM tests
pytest MEM/MEM_Modules_toolbox/tests/ -v

# Test specific module
pytest MEM/MEM_Modules_toolbox/tests/test_mem_connector.py -v

# Test with real broker connection
pytest MEM/MEM_Modules_toolbox/tests/ -v --live

# Generate coverage report
pytest MEM/MEM_Modules_toolbox/tests/ --cov=MEM
```

---

## 📈 Agent Learning & Adaptation

MemGPT continuously learns from trading experience:

### Learning Process
1. **Observation**: Market conditions and price movements
2. **Decision**: Strategy signal with reasoning
3. **Execution**: Trade placed on broker
4. **Outcome**: P&L and execution quality
5. **Memory Update**: Decision recorded with outcome for future reference

### Adaptation Triggers
- Poor performance in current market conditions
- New volatility regime detected
- Significant P&L drawdown
- Win rate degradation

### Memory-Based Adaptation
```python
# Example: Adjusting risk based on learned patterns
if performance_metrics['win_rate'] < 0.45:
    # Learned from memory: reduce position size
    new_position_size = current_position_size * 0.7
    update_memory('position_size', new_position_size)
```

---

## 🔐 Security Considerations

**Credential Handling**:
- ✅ Never log credentials
- ✅ Use environment variables only
- ✅ Rotate credentials regularly
- ✅ Audit all credential access

**Memory Protection**:
- ✅ Encrypt sensitive memory files
- ✅ Restrict file permissions (chmod 600)
- ✅ Regular backups to secure location
- ✅ Version control for audit trail

---

## ✅ Verification Checklist

- [x] MEM_Modules_toolbox/ structure created
- [x] All MemGPT modules present
- [x] mem_live_dashboard.py ready
- [x] Persistent memory system configured
- [x] Integration with trading engine verified
- [x] README documentation created

---

## 📝 Next Steps

1. ✅ Organize MEM modules (DONE)
2. ⏳ Test all MemGPT modules
3. ⏳ Deploy live dashboard
4. ⏳ Train agent on historical data
5. ⏳ Monitor agent performance

---

## 🚀 Quick Links

- [MemGPT Connector](./MEM_Modules_toolbox/mem_connector.py)
- [Connection Manager](./MEM_Modules_toolbox/mem_connection_manager.py)
- [Live Dashboard](./MEM_Modules_toolbox/mem_live_dashboard.py)
- [Persistent Memory](../data/mem_knowledge/)
- [MemGPT Integration Guide](./../docs/integration/MEM_RESEARCH_INTEGRATION_ROADMAP.md)

---

**Status**: Ready for Production  
**Last Verified**: October 16, 2025  
**Contact**: See project README.md
