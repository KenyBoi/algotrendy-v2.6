# ML Training Web Page - Data Connection Points Summary

**Quick Reference Guide** | **Version**: 2.6 | **Date**: October 20, 2025

---

## 🎯 Overview

This document organizes ALL data connection points needed for the ML Training Web Page.
Use this as your implementation checklist.

---

## 📊 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND (React/TypeScript)              │
│                    http://localhost:3000                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ML Training Dashboard                                      │
│    ├── TrainingVisualizer (real-time charts)              │
│    ├── DriftDetector (PSI scores)                         │
│    ├── OverfittingMonitor (gap analysis)                  │
│    └── PatternExplorer (opportunities)                    │
│                                                             │
└─────────┬─────────────────────────────────────────┬─────────┘
          │                                         │
          │ HTTP/REST                               │ WebSocket/SignalR
          │                                         │
┌─────────▼─────────────────┐           ┌──────────▼──────────┐
│   C# .NET API             │           │   SignalR Hub       │
│   http://localhost:5000   │           │   Real-time updates │
├───────────────────────────┤           ├─────────────────────┤
│                           │           │                     │
│ MLTrainingController      │           │ MLTrainingHub       │
│  ├─ GET /models          │           │  ├─ TrainingProgress│
│  ├─ POST /train          │           │  ├─ DriftAlert      │
│  ├─ GET /drift/{id}      │           │  └─ Metrics         │
│  └─ GET /performance     │           │                     │
│                           │           │                     │
└─────────┬─────────────────┘           └─────────────────────┘
          │
          │ HTTP calls to Python API
          │
┌─────────▼───────────────────────────────────────────────────┐
│            Python ML API (FastAPI)                          │
│            http://localhost:5050                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ├─ /models - List models                                  │
│  ├─ /train - Start training                                │
│  ├─ /predict - Get predictions                             │
│  ├─ /drift - Calculate drift                               │
│  └─ /overfitting - Analyze overfitting                     │
│                                                             │
└─────────┬────────────────────────┬──────────────────────────┘
          │                        │
          │                        │
┌─────────▼─────────┐    ┌────────▼─────────┐
│  ML Models        │    │  Market Data     │
│  /ml_models/      │    │  /data/          │
├───────────────────┤    ├──────────────────┤
│                   │    │                  │
│ - model.joblib    │    │ - QuestDB        │
│ - scaler.joblib   │    │ - YFinance       │
│ - config.json     │    │ - Pattern files  │
│ - metrics.json    │    │ - MEM knowledge  │
│                   │    │                  │
└───────────────────┘    └──────────────────┘
```

---

## 1. Frontend Data Sources

### 1.1 API Endpoints (HTTP)

**Base URL**: `http://localhost:5000/api/mltraining`

| Endpoint | Method | Purpose | Returns | Update Frequency |
|----------|--------|---------|---------|------------------|
| `/models` | GET | List all models | `MLModelInfo[]` | On page load |
| `/models/{id}` | GET | Model details | `MLModelDetails` | On selection |
| `/metrics/{id}` | GET | Model metrics | `ModelMetrics` | Real-time (polling) |
| `/train` | POST | Start training | `TrainingJobResult` | On demand |
| `/training/{jobId}` | GET | Training status | `TrainingStatus` | Poll every 1s |
| `/drift/{id}` | GET | Drift metrics | `DriftMetrics` | Every 5 minutes |
| `/performance/{id}` | GET | Performance history | `PerformanceHistory` | On page load |
| `/patterns` | GET | Latest patterns | `PatternAnalysis` | Every 1 minute |
| `/predict` | POST | Get prediction | `PredictionResult` | On demand |

### 1.2 SignalR Hub (WebSocket)

**Hub URL**: `ws://localhost:5000/hubs/mltraining`

| Event | Direction | Data | Purpose |
|-------|-----------|------|---------|
| `TrainingProgress` | Server → Client | `TrainingProgress` | Real-time training updates |
| `TrainingComplete` | Server → Client | `ModelMetrics` | Training completed notification |
| `DriftAlert` | Server → Client | `DriftAlert` | Drift threshold exceeded |
| `ModelUpdated` | Server → Client | `MLModelInfo` | Model was retrained |
| `SubscribeToTraining` | Client → Server | `jobId` | Subscribe to training job |
| `SubscribeToDrift` | Client → Server | `modelId` | Subscribe to drift alerts |

### 1.3 File-Based Data (for visualization)

| File | Path | Format | Purpose | How to Access |
|------|------|--------|---------|---------------|
| ML Model | `/ml_models/trend_reversals/{id}/reversal_model.joblib` | Binary | Trained model | Via API: GET `/models/{id}` |
| Config | `/ml_models/trend_reversals/{id}/config.json` | JSON | Model config | Direct file read or API |
| Metrics | `/ml_models/trend_reversals/{id}/model_metrics.json` | JSON | Performance metrics | Direct file read or API |
| Pattern Report | `/pattern_analysis_report.json` | JSON | Latest patterns | GET `/api/mltraining/patterns` |
| MEM Memory | `/data/mem_knowledge/core_memory_updates.txt` | Text | Learned patterns | Parse and display |
| Parameters | `/data/mem_knowledge/parameter_updates.json` | JSON | Parameter history | Parse JSON |

---

## 2. Backend Data Connections

### 2.1 C# API to Python ML API

**Communication**: HTTP REST calls from C# to Python FastAPI

```csharp
// In MLTrainingService.cs

private readonly HttpClient _httpClient;
private const string PYTHON_API_URL = "http://localhost:5050";

// Example: Start training
public async Task<TrainingJobResult> StartTrainingAsync(TrainingConfig config)
{
    var response = await _httpClient.PostAsJsonAsync(
        $"{PYTHON_API_URL}/train",
        config
    );
    return await response.Content.ReadFromJsonAsync<TrainingJobResult>();
}

// Example: Check drift
public async Task<DriftMetrics> GetDriftMetricsAsync(string modelId)
{
    var response = await _httpClient.GetAsync($"{PYTHON_API_URL}/drift/{modelId}");
    return await response.Content.ReadFromJsonAsync<DriftMetrics>();
}
```

### 2.2 Python ML API to Model Files

```python
# In ml_api_server.py

ML_MODELS_DIR = Path("/root/AlgoTrendy_v2.6/ml_models/trend_reversals")

# Load model
def load_model(model_id: str):
    model_path = ML_MODELS_DIR / model_id / "reversal_model.joblib"
    scaler_path = ML_MODELS_DIR / model_id / "reversal_scaler.joblib"
    config_path = ML_MODELS_DIR / model_id / "config.json"

    model = joblib.load(model_path)
    scaler = joblib.load(scaler_path)

    with open(config_path) as f:
        config = json.load(f)

    return model, scaler, config
```

### 2.3 Data Provider Connections

**For training data retrieval**:

| Provider | Connection String | Purpose | Rate Limit |
|----------|------------------|---------|------------|
| YFinance | `yf.Ticker(symbol)` | Historical OHLCV | Unlimited |
| Tiingo | `https://api.tiingo.com/tiingo/crypto/prices` | Crypto data | 1000/hour |
| Polygon | `https://api.polygon.io/v2/aggs/ticker` | Professional data | 5/minute |
| QuestDB | `jdbc:postgresql://localhost:8812/qdb` | Time-series storage | Local |

---

## 3. Component Data Requirements

### 3.1 TrainingVisualizer Component

**Input Data**:
```typescript
interface TrainingProgress {
  jobId: string;
  epoch: number;
  totalEpochs: number;
  trainAccuracy: number;
  valAccuracy: number;
  trainLoss: number;
  valLoss: number;
  learningRate: number;
  timePerEpoch: number;
  eta: number; // seconds
}
```

**Data Source**: SignalR Hub → `TrainingProgress` event

**Update Frequency**: Every 100ms during training

### 3.2 DriftDetector Component

**Input Data**:
```typescript
interface DriftMetrics {
  modelId: string;
  lastChecked: string; // ISO date
  driftScore: number; // 0-1
  isDrifting: boolean;
  featureDrift: Record<string, number>; // feature → PSI
  history: DriftHistory[];
  recommendedAction: string;
}
```

**Data Source**: HTTP GET `/api/mltraining/drift/{modelId}`

**Update Frequency**: Every 5 minutes (auto-refresh)

### 3.3 OverfittingMonitor Component

**Input Data**:
```typescript
interface PerformanceHistory {
  modelId: string;
  trainingPerformance: PerformancePoint[];
  validationPerformance: PerformancePoint[];
}

interface PerformancePoint {
  epoch: number;
  accuracy: number;
  loss: number;
  timestamp: string;
}
```

**Data Source**: HTTP GET `/api/mltraining/performance/{modelId}`

**Update Frequency**: On page load, then on training completion

### 3.4 PatternExplorer Component

**Input Data**:
```typescript
interface PatternAnalysis {
  timestamp: string;
  opportunities: OpportunityInfo[];
  patternDistribution: Record<string, number>;
  symbolsAnalyzed: number;
}

interface OpportunityInfo {
  symbol: string;
  price: number;
  rsi: number;
  volumeRatio: number;
  reversalConfidence: number;
  patterns: PatternInfo[];
  opportunityScore: number;
}
```

**Data Source**: HTTP GET `/api/mltraining/patterns`

**Update Frequency**: Every 1 minute (auto-refresh)

---

## 4. Database Connections

### 4.1 QuestDB (Time-Series Data)

**Connection**:
```csharp
// In C# backend
var connectionString = "Host=localhost;Port=8812;Database=qdb;Username=admin;Password=quest";
```

**Tables**:
- `market_data` - OHLCV candles
- `trades` - Executed trades
- `model_metrics` - Historical performance
- `drift_scores` - Drift tracking

### 4.2 File-Based Storage

**MEM Knowledge Files**:
```bash
/root/AlgoTrendy_v2.6/data/mem_knowledge/
├── core_memory_updates.txt         # Decision logs
├── parameter_updates.json          # Parameter history
└── strategy_modules.py             # Learned strategies
```

**ML Model Files**:
```bash
/root/AlgoTrendy_v2.6/ml_models/trend_reversals/
└── 20251016_234123/                # Model version
    ├── reversal_model.joblib       # Trained model (111 KB)
    ├── reversal_scaler.joblib      # Feature scaler (0.9 KB)
    ├── config.json                 # Configuration (1.5 KB)
    └── model_metrics.json          # Metrics (0.2 KB)
```

---

## 5. Real-Time Data Flow

### 5.1 Training Process

```
User clicks "Start Training"
    ↓
Frontend: POST /api/mltraining/train
    ↓
C# API: Forward to Python API POST /train
    ↓
Python API: Queue training job, return jobId
    ↓
C# API: Return jobId to Frontend
    ↓
Frontend: Subscribe to SignalR hub for training updates
    ↓
Python API: Execute training in background
    ↓ (every 100ms)
Python API → C# API → SignalR Hub → Frontend
    ↓
Frontend: Update TrainingVisualizer charts
    ↓
Python API: Training complete, save model
    ↓
SignalR Hub: Broadcast "TrainingComplete" event
    ↓
Frontend: Show success notification, refresh model list
```

### 5.2 Drift Detection Process

```
Background Service (runs every 5 minutes)
    ↓
C# DriftDetectionService: Calculate drift
    ↓
Call Python API: POST /drift with production data
    ↓
Python API: Calculate PSI for each feature
    ↓
Python API: Return drift metrics
    ↓
C# Service: Check if drift > threshold
    ↓
If drifting: SignalR Hub broadcast "DriftAlert"
    ↓
Frontend: Show drift alert banner
    ↓
Frontend: Update DriftDetector visualization
```

---

## 6. Configuration Files

### 6.1 appsettings.json (C# Backend)

```json
{
  "MLTraining": {
    "PythonApiUrl": "http://localhost:5050",
    "DriftCheckInterval": "00:05:00",
    "DriftThreshold": 0.25,
    "AccuracyDropThreshold": 0.05,
    "RetrainThreshold": 0.30,
    "ModelsDirectory": "/root/AlgoTrendy_v2.6/ml_models/trend_reversals"
  },
  "SignalR": {
    "EnableDetailedErrors": true,
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30"
  }
}
```

### 6.2 Frontend Environment Variables

```bash
# .env
REACT_APP_API_URL=http://localhost:5000
REACT_APP_SIGNALR_HUB=http://localhost:5000/hubs/mltraining
REACT_APP_PYTHON_API_URL=http://localhost:5050
```

---

## 7. Quick Start Commands

### Start Python ML API
```bash
cd /root/AlgoTrendy_v2.6
python3 ml_api_server.py
# Runs on http://localhost:5050
```

### Start C# Backend
```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API
dotnet run
# Runs on http://localhost:5000
```

### Start Frontend
```bash
cd /root/AlgoTrendy_v2.6/frontend
npm install
npm start
# Runs on http://localhost:3000
```

### Test Python API
```bash
curl http://localhost:5050/models
curl http://localhost:5050/patterns
```

---

## 8. Data Connection Checklist

Use this checklist when implementing:

**Backend Setup**:
- [ ] Python ML API server running (port 5050)
- [ ] C# API running (port 5000)
- [ ] SignalR hub configured
- [ ] HTTP client configured for Python API calls
- [ ] Background services for drift detection
- [ ] QuestDB connection working

**Frontend Setup**:
- [ ] API service configured (axios/fetch)
- [ ] SignalR connection established
- [ ] Environment variables set
- [ ] Real-time hooks implemented
- [ ] Auto-refresh intervals configured
- [ ] Error handling for connection failures

**Data Sources**:
- [ ] ML model files accessible
- [ ] QuestDB tables created
- [ ] Market data providers configured
- [ ] MEM knowledge files readable
- [ ] Pattern analysis reports generated

**Testing**:
- [ ] Can list models via API
- [ ] Can start training job
- [ ] Real-time updates working
- [ ] Drift alerts triggering
- [ ] Charts rendering correctly
- [ ] WebSocket reconnection working

---

## 9. Troubleshooting

### Python API Not Responding
```bash
# Check if running
ps aux | grep ml_api_server

# Check port
netstat -tuln | grep 5050

# Restart
python3 ml_api_server.py
```

### SignalR Connection Issues
```typescript
// In frontend, add connection logging
connection.onclose(() => {
  console.error('SignalR connection closed. Reconnecting...');
  startConnection();
});

connection.onreconnecting(() => {
  console.log('SignalR reconnecting...');
});
```

### Model Files Not Found
```bash
# Verify model directory
ls -la /root/AlgoTrendy_v2.6/ml_models/trend_reversals/

# Check permissions
chmod -R 755 /root/AlgoTrendy_v2.6/ml_models/
```

---

## 10. Performance Optimization

**Caching Strategy**:
- Cache model list for 1 minute
- Cache drift metrics for 5 minutes
- Cache performance history for 10 minutes
- Clear cache on training completion

**Connection Pooling**:
- Reuse HTTP client instances
- Keep SignalR connection alive
- Connection timeout: 30 seconds
- Retry on failure: 3 attempts

**Data Fetching**:
- Lazy load model details
- Paginate model list if > 50 items
- Debounce API calls (300ms)
- Use React Query for automatic caching

---

**Document Version**: 1.0
**Last Updated**: October 20, 2025
**Status**: ✅ Ready for Implementation
