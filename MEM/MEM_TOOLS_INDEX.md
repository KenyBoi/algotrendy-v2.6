# MEM Tools & Modules Index

> **Complete list of all MEM components, tools, and utilities**
> **Last Updated**: October 20, 2025

---

## 🧰 Core MEM Modules

| Module | Size | Purpose | Status |
|--------|------|---------|--------|
| **mem_connector.py** | 13.3 KB | MemGPT agent connector - Core communication with AI agent | ✅ Production |
| **mem_connection_manager.py** | 5.9 KB | Multi-broker connection pool management | ✅ Production |
| **mem_credentials.py** | 6.7 KB | Secure credential handling with audit trail | ✅ Production |
| **mem_live_dashboard.py** | 15.2 KB | Real-time monitoring dashboard (Flask/FastAPI) | ✅ Production |
| **singleton_decorator.py** | 0.5 KB | Singleton pattern for instance management | ✅ Production |

**Total Core**: 5 modules, 41.6 KB

---

## 🤖 Machine Learning Models

| Model | Size | Accuracy | Purpose | Status |
|-------|------|----------|---------|--------|
| **reversal_model.joblib** | 111 KB | 78% | Trend reversal detection (Gradient Boosting) | ✅ Production |
| **reversal_scaler.joblib** | 0.9 KB | N/A | Feature scaler for normalization | ✅ Production |
| **scaler.joblib** | 1.7 KB | N/A | Alternative scaler | ✅ Production |
| **config.json** | 1.5 KB | N/A | Model configuration & hyperparameters | ✅ Production |
| **model_metrics.json** | 0.2 KB | N/A | Performance metrics (precision, recall, F1) | ✅ Production |

**Total ML**: 5 files, 115.3 KB | **Training Data**: 45,000+ trades | **Features**: 12 indicators

---

## 💾 Persistent Memory System

| File | Size | Purpose | Retention |
|------|------|---------|-----------|
| **core_memory_updates.txt** | ~0.5 KB | Decision history + learned patterns (timestamped logs) | Unlimited |
| **parameter_updates.json** | ~0.7 KB | Parameter tuning log with adjustments & reasoning | Unlimited |
| **strategy_modules.py** | ~2.3 KB | Auto-generated learned strategies (Python classes) | Unlimited |

**Total Memory**: 3 files, ~3.5 KB (grows with usage)

---

## 📊 Monitoring & Dashboard Tools

| Tool | Size | Purpose | Access |
|------|------|---------|--------|
| **memgpt_metrics_dashboard.py** | 17.8 KB | Real-time performance metrics & visualization | http://localhost:5001 |
| **mem_live_dashboard.py** | 15.2 KB | Live agent activity monitoring | http://localhost:5001/dashboard |

**Metrics Displayed**:
- Current positions & P&L
- Win rate, Sharpe ratio, drawdown
- Decision history with reasoning
- Learned patterns & strategies
- Memory insights & parameter changes

---

## 🔄 Training & Retraining Pipeline

| Script | Size | Purpose | Schedule |
|--------|------|---------|----------|
| **retrain_model.py** | 10.6 KB | Automated ML model retraining pipeline | Daily 2 AM UTC |

**Retraining Process**:
1. Fetch 30 days market data from QuestDB
2. Calculate 12 technical indicator features
3. Train new Gradient Boosting model
4. Validate on 20% holdout set
5. Deploy if accuracy ≥ existing + 2%
6. Archive old model & log metrics

---

## 🔗 TradingView Integration Suite

| Module | Size | Purpose | Status |
|--------|------|---------|--------|
| **memgpt_tradingview_companion.py** | 31.0 KB | TradingView webhook receiver & processor | ✅ Production |
| **memgpt_tradingview_plotter.py** | 9.3 KB | Alert visualization & charting | ✅ Production |
| **memgpt_tradingview_tradestation_bridge.py** | 9.9 KB | TradeStation integration bridge | ✅ Production |
| **memgpt_tradestation_integration.py** | ~8 KB | TradeStation server integration | ✅ Production |

**Total TradingView**: 4 modules, ~58 KB

**Pine Scripts Available**:
- `memgpt_basic_companion.pine` - Basic signal companion
- `memgpt_companion_enhanced.pine` - Enhanced with ML predictions

---

## 🚀 Deployment & Infrastructure

| Script | Size | Purpose | Usage |
|--------|------|---------|-------|
| **start_mem_pipeline.sh** | 4.7 KB | Launch complete MEM pipeline | `bash scripts/deployment/start_mem_pipeline.sh` |
| **launch_project.sh** | ~2 KB | TradingView project launcher | `bash launch_project.sh` |

**Environment Variables Required**:
- `TRADING_MODE=paper|live`
- `SYMBOL=BTCUSDT`
- `STRATEGY=momentum`
- `MEM_MEMORY_PATH=data/mem_knowledge/`

---

## 📚 Documentation

| Document | Lines | Purpose |
|----------|-------|---------|
| **README.md** | 615 | Complete MEM overview & capabilities |
| **MEM_ARCHITECTURE.md** | 850 | Deep technical architecture dive |
| **MEM_CAPABILITIES.md** | 720 | Full capability matrix & use cases |
| **MEM_TOOLS_INDEX.md** | ~150 | This file - tools index |

**Integration Docs**:
- `docs/implementation/integrations/MEM_ML_INTEGRATION_ROADMAP.md` (22 KB)
- `docs/implementation/integrations/MEM_ML_INVENTORY_V2.5.md` (15 KB)
- `ai_context/MEM_ML_HANDOFF_CHECKLIST.md` (8 KB)

---

## 🛠️ Utility Tools

| Tool | Purpose | Location |
|------|---------|----------|
| **dynamic_timeframe_demo.py** | TradingView dynamic timeframe demo | `integrations/tradingview/` |
| **tradingview_data_publisher.py** | Publish data to TradingView | `integrations/tradingview/` |
| **tradingview_integration_strategy.py** | Strategy integration | `integrations/tradingview/` |
| **tradingview_paper_trading_dashboard.py** | Paper trading dashboard | `integrations/tradingview/` |

---

## 📁 Directory Structure

```
/root/AlgoTrendy_v2.6/
├── MEM/                                          # Core MEM Directory
│   ├── README.md                                 # Main documentation
│   ├── MEM_ARCHITECTURE.md                       # Architecture deep dive
│   ├── MEM_CAPABILITIES.md                       # Capabilities overview
│   ├── MEM_TOOLS_INDEX.md                        # This file
│   └── MEM_Modules_toolbox/                      # Core modules
│       ├── mem_connector.py                      # (13.3 KB)
│       ├── mem_connection_manager.py             # (5.9 KB)
│       ├── mem_credentials.py                    # (6.7 KB)
│       ├── mem_live_dashboard.py                 # (15.2 KB)
│       ├── singleton_decorator.py                # (0.5 KB)
│       └── Tradingview_x_Algotrendy/
│           └── memgpt_tradingview_project/
│               ├── servers/                      # Integration servers
│               ├── pine_scripts/                 # TradingView indicators
│               ├── templates/                    # Webhook templates
│               └── README.md
│
├── ml_models/                                    # ML Models
│   └── trend_reversals/
│       └── 20251016_234123/                      # Model version
│           ├── reversal_model.joblib             # (111 KB)
│           ├── reversal_scaler.joblib            # (0.9 KB)
│           ├── scaler.joblib                     # (1.7 KB)
│           ├── config.json                       # (1.5 KB)
│           └── model_metrics.json                # (0.2 KB)
│
├── data/mem_knowledge/                           # Persistent Memory
│   ├── core_memory_updates.txt                   # Decision history
│   ├── parameter_updates.json                    # Parameter tuning log
│   └── strategy_modules.py                       # Learned strategies
│
├── retrain_model.py                              # (10.6 KB)
├── memgpt_metrics_dashboard.py                   # (17.8 KB)
│
└── integrations/tradingview/                     # TradingView Integration
    ├── memgpt_tradingview_companion.py           # (31 KB)
    ├── memgpt_tradingview_plotter.py             # (9.3 KB)
    ├── memgpt_tradingview_tradestation_bridge.py # (9.9 KB)
    ├── servers/
    │   ├── memgpt_tradingview_companion.py
    │   ├── memgpt_tradingview_tradestation_bridge.py
    │   └── memgpt_tradestation_integration.py
    ├── pine_scripts/
    │   ├── memgpt_basic_companion.pine
    │   └── memgpt_companion_enhanced.pine
    └── templates/
        └── webhook_template.json
```

---

## 🔧 Integration Tools (C# .NET)

**To Be Implemented** (See `MEM_ML_INTEGRATION_ROADMAP.md`):

| Service | Purpose | Status |
|---------|---------|--------|
| **MLModelService.cs** | ML model loading & inference | ⏳ Planned |
| **MLPredictionService.cs** | Prediction orchestration | ⏳ Planned |
| **DecisionLoggerService.cs** | Decision persistence | ⏳ Planned |
| **MemGPTConnectorService.cs** | MemGPT agent connector | ⏳ Planned |
| **DashboardService.cs** | Metrics aggregation | ⏳ Planned |
| **ModelRetrainingService.cs** | Automated retraining | ⏳ Planned |

---

## 📊 Quick Stats

| Category | Count | Total Size |
|----------|-------|------------|
| **Core Modules** | 5 | 41.6 KB |
| **ML Models** | 5 files | 115.3 KB |
| **Memory Files** | 3 | 3.5 KB |
| **TradingView Integration** | 4+ modules | 58 KB |
| **Dashboards** | 2 | 33 KB |
| **Documentation** | 4+ docs | 50+ KB |
| **Total MEM Components** | 25+ | ~275 KB |

---

## 🚀 Quick Access Commands

### Start MEM Pipeline
```bash
bash scripts/deployment/start_mem_pipeline.sh
```

### Access Dashboard
```bash
# Open in browser
open http://localhost:5001/dashboard
```

### Retrain ML Model
```bash
python retrain_model.py --symbols BTCUSDT,ETHUSDT --days 30
```

### View Memory
```bash
# Recent decisions
tail -n 50 data/mem_knowledge/core_memory_updates.txt

# Parameter history
cat data/mem_knowledge/parameter_updates.json | jq '.'

# Learned strategies
cat data/mem_knowledge/strategy_modules.py
```

### Test Modules
```bash
# Test MEM connector
python -m MEM.MEM_Modules_toolbox.mem_connector

# Test dashboard
python -m MEM.MEM_Modules_toolbox.mem_live_dashboard
```

---

## 📦 Dependencies

### Python Packages Required
```
scikit-learn==1.4.0      # ML models
pandas==2.0.0            # Data processing
numpy==1.24.0            # Numerical computing
joblib==1.3.0            # Model serialization
asyncio                  # Async operations
websockets               # WebSocket support
aiohttp                  # Async HTTP
flask/fastapi            # Dashboard server
```

### .NET Packages Required (Future)
```xml
<PackageReference Include="pythonnet" Version="3.20" />
<PackageReference Include="Microsoft.ML" Version="2.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0" />
```

---

## ✅ Tool Status Summary

| Status | Count | Percentage |
|--------|-------|------------|
| ✅ **Production Ready** | 18 tools | 85% |
| ⏳ **Planned (C# Integration)** | 6 services | 15% |
| 🔄 **Continuous Updates** | Memory files | Ongoing |
| 📈 **Active Learning** | ML models | Daily retraining |

---

## 🎯 Next Steps

1. **Use MEM**: `bash scripts/deployment/start_mem_pipeline.sh`
2. **Monitor**: Open `http://localhost:5001/dashboard`
3. **Review Decisions**: `tail -f data/mem_knowledge/core_memory_updates.txt`
4. **Check Performance**: Review dashboard metrics
5. **Integrate C#**: Follow `MEM_ML_INTEGRATION_ROADMAP.md`

---

**MEM Status**: Production-Ready | **Tools**: 25+ components | **Size**: ~275 KB
**ML Accuracy**: 78% | **Win Rate**: 62.5% | **Active Strategies**: 15

**Last Updated**: October 20, 2025 | **Maintained By**: AlgoTrendy Development Team
