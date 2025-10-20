# AlgoTrendy v2.5 - MEM & ML Components Inventory

**Purpose:** Complete inventory of all MEM (Memory-Enhanced GPT) and ML (Machine Learning) components in v2.5
**Date:** October 19, 2025
**Status:** For integration into v2.6

---

## 📋 Executive Summary

### What's in v2.5 MEM/ML

**MEM System (Memory-Enhanced GPT Agent)**
- ✅ 9 core MemGPT modules (connector, manager, credentials, dashboard, etc.)
- ✅ TradingView integration bridge
- ✅ Persistent memory system (core updates, parameter tuning, learned strategies)
- ✅ Connection manager for multi-broker support
- ✅ Live dashboard for real-time monitoring

**ML System (Machine Learning Models)**
- ✅ Trend reversal detection model (Gradient Boosting)
- ✅ Model retraining pipeline with feature engineering
- ✅ Technical indicator calculations (RSI, MACD, Bollinger Bands, SMA/EMA)
- ✅ Model evaluation metrics (accuracy, precision, recall, F1)
- ✅ Joblib-based model persistence

**Missing in v2.6**
- ❌ All MEM modules (0% ported)
- ❌ All ML models (0% ported)
- ❌ Persistent memory system
- ❌ Model retraining pipeline

---

## 🧠 MEM Components (MemGPT Agent Modules)

### Location: `/root/algotrendy_v2.5/MEM/MEM_Modules_toolbox/`

#### 1. **mem_connector.py** (Core)
**Purpose:** Core connector between MemGPT agent and external systems

**Key Features:**
- Agent communication protocol
- Message serialization/deserialization
- Event routing
- Error handling and recovery
- Command sending to agents

**Dependencies:**
- asyncio (async operations)
- Custom configuration system

**Key Classes/Functions:**
- `MemGPTConnector` - Main connector class
- `initialize()` - Setup agent
- `send_command(command, params)` - Send commands to agent
- `analyze_signal(signal)` - Analyze trading signals
- `log_decision(decision, result)` - Log trading decisions

**Integration Points in v2.6:**
- Should integrate into `AlgoTrendy.TradingEngine` services
- Call during strategy signal generation
- Log to decision history

---

#### 2. **mem_connection_manager.py** (Connection Handler)
**Purpose:** Manages connections to external systems (brokers, data feeds, webhooks)

**Key Features:**
- Maintain connection pools
- Handle reconnection logic
- Monitor connection health
- Route messages to appropriate endpoints

**Dependencies:**
- asyncio
- Connection pooling

**Key Classes/Functions:**
- `ConnectionManager` - Main manager
- `add_broker_connection(name, api_key, api_secret)` - Add broker connection
- `add_data_feed(name, config)` - Add data feed
- `execute_on_broker(broker_name, command, params)` - Execute on specific broker
- `get_connection_status()` - Health check

**Integration Points in v2.6:**
- Already has similar functionality in `AlgoTrendy.TradingEngine/Brokers/`
- Could enhance with memory tracking

---

#### 3. **mem_credentials.py** (Credential Manager)
**Purpose:** Secure credential management for MemGPT agent

**Key Features:**
- Load credentials from environment
- Manage credential lifecycle
- Handle credential rotation
- Audit credential access

**Dependencies:**
- Environment variables
- Logging system

**Key Classes/Functions:**
- `CredentialManager` - Main manager
- `get_broker_credentials(broker_name)` - Get broker creds
- `has_credentials(broker_name)` - Check if creds exist
- `update_credentials()` - Rotate credentials
- `audit_access()` - Log credential access

**Integration Points in v2.6:**
- Could enhance existing credential handling in `AlgoTrendy.Core`
- Add audit logging capability

---

#### 4. **mem_live_dashboard.py** (Monitoring Dashboard)
**Purpose:** Real-time monitoring dashboard for MemGPT agent activity

**Key Features:**
- Live position tracking
- Trade history
- Performance metrics
- Agent decision logs
- Alert monitoring

**Endpoints:**
- `/dashboard` - Main dashboard
- `/metrics` - Performance metrics
- `/trades` - Trade history
- `/memory` - Memory state
- `/health` - System health

**Dependencies:**
- Flask/FastAPI (web framework)
- WebSocket support
- Real-time data streaming

**Integration Points in v2.6:**
- Could be integrated into existing SignalR hub
- Add to API layer for monitoring

---

#### 5. **mem_live_dashboard.py** (Support Files)
- Configuration files for dashboard
- Template rendering
- Real-time update logic

---

#### 6. **Tradingview Integration Suite**
**Location:** `/root/algotrendy_v2.5/MEM/MEM_Modules_toolbox/Tradingview_x_Algotrendy/`

**Components:**
- `memgpt_tradingview_companion.py` - TradingView webhook receiver
- `memgpt_tradingview_tradestation_bridge.py` - TradeStation bridge
- `memgpt_tradestation_integration.py` - Integration server

**Purpose:** Bridge MemGPT with TradingView alerts and TradeStation

---

#### 7. **singleton_decorator.py** (Utility)
**Purpose:** Singleton pattern decorator for persistent instances

**Key Features:**
- Ensure single instance of MemGPT agent
- Thread-safe implementation
- Memory efficiency

---

### Data Files (Persistent Memory)

**Location:** `/root/algotrendy_v2.5/data/mem_knowledge/`

#### **core_memory_updates.txt**
**Purpose:** Decision history and learned patterns

**Content:**
```
[timestamp] Strategy: [strategy] - Decision: [BUY/SELL/HOLD] - Confidence: [0.0-1.0]
[timestamp] Entry Price: [price] - Stop Loss: [price] - Target: [price]
[timestamp] P&L: [amount] - Closed position successfully
[timestamp] Learned: [pattern] - record for future
```

**Integration:** Append logs during trading operations

---

#### **parameter_updates.json**
**Purpose:** Model parameter adjustments over time

**Content:**
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

**Integration:** Update during parameter tuning

---

#### **strategy_modules.py**
**Purpose:** Custom strategy implementations learned by agent

**Content:**
```python
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

**Integration:** Load and execute learned strategies

---

---

## 🤖 ML Components (Machine Learning Models)

### Location: `/root/algotrendy_v2.5/ml_models/`

#### **Trend Reversal Model**
**Location:** `/root/algotrendy_v2.5/ml_models/trend_reversals/20251016_234123/`

**Model Type:** Gradient Boosting Classifier (scikit-learn)

**Files:**
- `reversal_model.joblib` - Trained model (111 KB)
- `reversal_scaler.joblib` - Feature scaler (0.9 KB)
- `scaler.joblib` - Alternative scaler (1.7 KB)
- `config.json` - Model configuration
- `model_metrics.json` - Training metrics

**Configuration (config.json):**
```json
{
  "model_type": "GradientBoostingClassifier",
  "n_estimators": 200,
  "max_depth": 7,
  "learning_rate": 0.1,
  "subsample": 0.8,
  "min_samples_split": 5,
  "min_samples_leaf": 2,
  "random_state": 42,
  "symbols": ["BTCUSDT", "ETHUSDT", "BNBUSDT", "XRPUSDT"],
  "trained_at": "2025-10-17T01:29:00"
}
```

**Metrics (model_metrics.json):**
```json
{
  "accuracy": 0.78,
  "precision": 0.72,
  "recall": 0.68,
  "f1": 0.70,
  "trained_at": "2025-10-17T01:29:00",
  "training_rows": 45000
}
```

**Input Features (12 technical indicators):**
1. `sma_5` - 5-period Simple Moving Average
2. `sma_20` - 20-period SMA
3. `sma_50` - 50-period SMA
4. `rsi` - Relative Strength Index (14-period)
5. `macd` - MACD line (12-26)
6. `macd_signal` - MACD signal line (9-period)
7. `macd_hist` - MACD histogram
8. `bb_position` - Bollinger Band position (0-1)
9. `volume_ratio` - Volume vs SMA ratio
10. `hl_range` - High-Low range
11. `close_position` - Close position in range
12. `oc_range` - Open-Close range

**Output:** Binary classification
- `0` - No reversal detected
- `1` - Reversal detected (peak or trough)

**Performance:**
- Accuracy: 78%
- Precision: 72%
- Recall: 68%
- F1-Score: 70%
- Training samples: 45,000+

---

### Model Retraining Script
**Location:** `/root/algotrendy_v2.5/retrain_model.py`

**Purpose:** Retrain trend reversal model with fresh market data

**Pipeline Steps:**
1. Load data from CSV files (BTCUSDT, ETHUSDT, BNBUSDT, XRPUSDT)
2. Engineer features (technical indicators)
3. Detect reversals (peaks and troughs with 2% threshold)
4. Prepare training data (scale and normalize)
5. Train Gradient Boosting model
6. Evaluate on training data
7. Save model, scaler, and metrics
8. Generate JSON config

**Key Classes:**
- `ModelRetrainer` - Main retraining orchestrator

**Key Methods:**
- `load_data()` - Load market data
- `engineer_features(df)` - Create indicators
- `detect_reversals(df)` - Identify reversals
- `prepare_training_data(df)` - Format for ML
- `train_model(X, y)` - Train classifier
- `save_model(metrics)` - Persist model artifacts
- `run()` - Execute full pipeline

**Integration Points in v2.6:**
- Call periodically (daily/weekly) to retrain
- Store new models versioned by date
- Compare metrics with previous version
- Switch to new model if performance improves

---

### ML Deployment Scripts
**Location:** `/root/algotrendy_v2.5/scripts/deployment/`

#### **memgpt_tradingview_plotter.py**
- Plots alerts on TradingView
- Sends alert data to MemGPT
- Visualizes model predictions

#### **memgpt_tradingview_companion.py**
- Webhook receiver for TradingView alerts
- Routes alerts to MemGPT agent
- Executes trades based on alerts

#### **memgpt_tradingview_tradestation_bridge.py**
- Bridge between TradingView and TradeStation
- Integrates with MemGPT agent
- Two-way communication

#### **start_mem_pipeline.sh**
- Bash script to start MEM pipeline
- Sets up environment variables
- Starts all services

---

### MemGPT Metrics Dashboard
**Location:** `/root/algotrendy_v2.5/memgpt_metrics_dashboard.py`

**Purpose:** Dashboard for monitoring MemGPT agent performance

**Displays:**
- Real-time positions
- Trade P&L
- Agent decisions with confidence scores
- Performance metrics (Sharpe, win rate, etc.)
- Memory state and decision history

---

---

## 📊 Technical Stack Summary

### Python Libraries Used (v2.5 MEM/ML)
```
scikit-learn==1.4.0  # ML models and preprocessing
pandas==2.0.0        # Data manipulation
numpy==1.24.0        # Numerical computing
joblib==1.3.0        # Model serialization
asyncio               # Async operations (built-in)
json                  # Config and metrics (built-in)
```

### Architecture Patterns
- **Observer Pattern** - Decision logging
- **Singleton Pattern** - Persistent agent instance
- **Factory Pattern** - Model creation
- **Strategy Pattern** - Learned strategies
- **Decorator Pattern** - Singleton wrapper

---

---

## 🔄 Porting Strategy for v2.6

### High Priority (Critical for Trading)
1. **ML Model Loading & Prediction**
   - Load joblib model files in C#
   - Create predictor service
   - Integrate with signal generation
   - **Estimate:** 4-6 hours

2. **Persistent Memory System**
   - Create decision logging
   - Parameter tracking
   - Memory updates
   - **Estimate:** 3-4 hours

3. **MemGPT Connector**
   - Agent communication protocol
   - Decision enhancement
   - Learning from outcomes
   - **Estimate:** 6-8 hours

### Medium Priority (Enhances Functionality)
4. **Live Dashboard**
   - Agent decision display
   - Real-time metrics
   - **Estimate:** 4-5 hours

5. **Connection Manager**
   - Multi-broker management
   - Health monitoring
   - **Estimate:** 3-4 hours

6. **TradingView Integration**
   - Webhook receiver
   - Alert processing
   - **Estimate:** 4-5 hours

### Low Priority (Nice-to-Have)
7. **Model Retraining Pipeline**
   - Data pipeline
   - Feature engineering
   - Model evaluation
   - **Estimate:** 8-10 hours

---

## 🚀 Files to Copy from v2.5

### Core MEM/ML Files (Must Copy)
```
/root/algotrendy_v2.5/
├── MEM/
│   └── MEM_Modules_toolbox/
│       ├── mem_connector.py ✅
│       ├── mem_connection_manager.py ✅
│       ├── mem_credentials.py ✅
│       ├── mem_live_dashboard.py ✅
│       ├── singleton_decorator.py ✅
│       └── Tradingview_x_Algotrendy/ ✅
│
├── ml_models/
│   └── trend_reversals/20251016_234123/
│       ├── reversal_model.joblib ✅
│       ├── reversal_scaler.joblib ✅
│       ├── scaler.joblib ✅
│       ├── config.json ✅
│       └── model_metrics.json ✅
│
├── data/mem_knowledge/
│   ├── core_memory_updates.txt ✅
│   ├── parameter_updates.json ✅
│   └── strategy_modules.py ✅
│
├── retrain_model.py ✅
│
├── memgpt_metrics_dashboard.py ✅
│
└── scripts/deployment/
    ├── memgpt_tradingview_plotter.py ✅
    ├── memgpt_tradingview_companion.py ✅
    ├── memgpt_tradingview_tradestation_bridge.py ✅
    └── start_mem_pipeline.sh ✅
```

---

## ✅ Integration Checklist for v2.6

- [ ] **Phase 1: Model Loading**
  - [ ] Create C# wrapper for scikit-learn model loading
  - [ ] Load joblib files with Python.NET bridge or ml.net
  - [ ] Create predictor service

- [ ] **Phase 2: Decision Logging**
  - [ ] Create decision history storage
  - [ ] Implement core_memory_updates logging
  - [ ] Track decision outcomes

- [ ] **Phase 3: MemGPT Integration**
  - [ ] Port mem_connector.py to C#
  - [ ] Integrate with strategy resolver
  - [ ] Add confidence scores to signals

- [ ] **Phase 4: Persistent Memory**
  - [ ] Implement parameter update tracking
  - [ ] Create learned strategy storage
  - [ ] Add memory serialization

- [ ] **Phase 5: Monitoring**
  - [ ] Add MemGPT metrics to SignalR
  - [ ] Create dashboard components
  - [ ] Real-time decision display

---

## 📝 Notes

- **v2.5 Preservation:** All v2.5 files remain intact, only copies will be made
- **Model Versioning:** ML models should be versioned by timestamp
- **Memory Persistence:** Consider database storage vs file-based for production
- **Python-C# Bridge:** For ML models, consider:
  - Option 1: ML.NET (recommended for pure .NET)
  - Option 2: Python.NET bridge for joblib models
  - Option 3: Re-implement in C# with ML.NET

---

**Status:** Inventory Complete - Ready for Integration
**Next Step:** Begin porting in phases based on priority

