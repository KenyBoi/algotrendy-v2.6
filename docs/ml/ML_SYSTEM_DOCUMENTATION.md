# AlgoTrendy ML System Documentation

## Overview

AlgoTrendy's machine learning system combines state-of-the-art models (XGBoost + LSTM) with comprehensive overfitting detection and interactive visualizations to predict market trend reversals.

**Version:** 2.6.0
**Last Updated:** October 21, 2025

---

## Table of Contents

1. [Architecture](#architecture)
2. [Models](#models)
3. [Training](#training)
4. [Predictions](#predictions)
5. [Overfitting Detection](#overfitting-detection)
6. [Visualizations](#visualizations)
7. [API Endpoints](#api-endpoints)
8. [Usage Examples](#usage-examples)
9. [Best Practices](#best-practices)

---

## Architecture

### Hybrid XGBoost + LSTM Design

```
┌─────────────────────────────────────────────────────────────┐
│                     Data Sources                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │  Binance    │  │   Bybit     │  │    MEXC     │        │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘        │
└─────────┼─────────────────┼─────────────────┼──────────────┘
          │                 │                 │
          └─────────────────┴─────────────────┘
                            │
                ┌───────────▼───────────┐
                │  Feature Engineering  │
                │   40+ Indicators      │
                └───────────┬───────────┘
                            │
          ┌─────────────────┴──────────────────┐
          │                                    │
    ┌─────▼──────┐                    ┌───────▼────────┐
    │  XGBoost   │                    │     LSTM       │
    │  Tabular   │                    │  Sequential    │
    │  Features  │                    │  Time-Series   │
    └─────┬──────┘                    └───────┬────────┘
          │                                    │
          └─────────────────┬──────────────────┘
                            │
                  ┌─────────▼─────────┐
                  │ Ensemble Predictor│
                  │  Weighted Voting  │
                  └─────────┬─────────┘
                            │
                  ┌─────────▼─────────┐
                  │   Final Prediction │
                  │  + Confidence      │
                  └────────────────────┘
```

### Key Components

| Component | Purpose | Technology |
|-----------|---------|------------|
| **ml_trainer_advanced.py** | Training XGBoost + LSTM models | scikit-learn, XGBoost, TensorFlow |
| **ml_visualizations.py** | Interactive overfitting detection | Plotly, Matplotlib |
| **ml_ensemble.py** | Ensemble predictions | Custom voting/weighting |
| **ml_api_server.py** | REST API for ML operations | FastAPI, Uvicorn |

---

## Models

### 1. XGBoost (Tabular Data)

**Purpose:** Feature importance and tabular prediction

**Configuration:**
```python
XGBClassifier(
    n_estimators=100,
    max_depth=5,           # Prevent overfitting
    learning_rate=0.1,
    subsample=0.8,         # Row sampling
    colsample_bytree=0.8,  # Column sampling
    reg_alpha=0.1,         # L1 regularization
    reg_lambda=1.0,        # L2 regularization
    early_stopping_rounds=10
)
```

**Features:**
- Fast training and inference
- Built-in feature importance
- Handles missing values
- Regularization prevents overfitting

### 2. LSTM (Time-Series)

**Purpose:** Sequential pattern recognition

**Architecture:**
```python
Sequential([
    LSTM(64, return_sequences=True),
    Dropout(0.2),
    BatchNormalization(),

    LSTM(32, return_sequences=False),
    Dropout(0.2),
    BatchNormalization(),

    Dense(16, activation='relu'),
    Dropout(0.2),

    Dense(1, activation='sigmoid')
])
```

**Features:**
- Captures temporal dependencies
- Batch normalization for stability
- Dropout for regularization
- Early stopping to prevent overfitting

### 3. Traditional Models (Baseline)

- **AdaBoost:** Current production model (99.98% val accuracy)
- **GradientBoosting:** Strong baseline
- **RandomForest:** Ensemble of decision trees

---

## Training

### Quick Start

```bash
# Train advanced models (XGBoost + LSTM)
cd /root/AlgoTrendy_v2.6/scripts/ml
python3 ml_trainer_advanced.py
```

### Training Process

1. **Data Fetching** (90 days, 1h interval)
   ```python
   df = trainer.fetch_live_data(period="90d", interval="1h")
   ```

2. **Feature Engineering** (40+ indicators)
   - Price features: change, range, body size
   - Moving averages: SMA, EMA (7, 14, 21, 50)
   - Momentum: RSI, MACD, ROC
   - Volatility: ATR, Bollinger Bands
   - Volume: Volume ratio, SMA

3. **Data Split**
   - Training: 70%
   - Validation: 10%
   - Test: 20%

4. **Model Training**
   - XGBoost with early stopping
   - LSTM with callbacks (EarlyStopping, ReduceLROnPlateau)
   - K-fold cross-validation (5 folds)

5. **Evaluation**
   - Accuracy, Precision, Recall, F1
   - Overfitting gap calculation
   - Learning curves generation

6. **Model Saving**
   - Models: `{version}/xgboost_model.joblib`, `lstm_model.h5`
   - Scalers: `{version}/scalers.joblib`
   - Config: `{version}/config.json`
   - Metrics: `{version}/metrics.json`

### Advanced Training Options

```python
trainer = AdvancedMLTrainer(
    model_dir="/path/to/models",
    data_source="live",       # or "csv"
    sequence_length=60        # LSTM lookback window
)

# Fetch data
df = trainer.fetch_live_data(period="90d", interval="1h")

# Calculate features
df, feature_cols = trainer.calculate_features(df)

# Prepare data
X_train, X_val, X_test, y_train, y_val, y_test = trainer.prepare_data(df, feature_cols)

# Train XGBoost
xgb_model = trainer.train_xgboost(X_train, y_train, X_val, y_val)

# K-fold validation
scores = trainer.perform_kfold_validation(X_train, y_train, model_type='xgboost', n_splits=5)

# Learning curves
trainer.generate_learning_curves(X_train, y_train, model_type='xgboost')

# Train LSTM
lstm_model = trainer.train_lstm(X_train, y_train, X_val, y_val, feature_count=len(feature_cols))

# Save all models
version_dir = trainer.save_models(feature_cols)
```

---

## Predictions

### Single Model Prediction

```bash
curl -X POST "http://localhost:5050/predict" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "recent_candles": [
      {"open": 50000, "high": 51000, "low": 49500, "close": 50500, "volume": 1000},
      ...
    ]
  }'
```

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "is_reversal": true,
  "confidence": 0.85,
  "feature_values": {...},
  "timestamp": "2025-10-21T12:00:00Z"
}
```

### Ensemble Prediction (Recommended)

```bash
curl -X POST "http://localhost:5050/predict/ensemble?strategy=weighted" \
  -H "Content-Type: application/json" \
  -d '{...}'
```

**Strategies:**
- `voting`: Simple majority vote
- `weighted`: Performance-based weighting (default)
- `confidence`: Use only high-confidence models

**Response:**
```json
{
  "symbol": "BTCUSDT",
  "prediction": 1,
  "is_reversal": true,
  "confidence": 0.92,
  "strategy": "weighted",
  "model_breakdown": {
    "xgboost": {
      "prediction": 1,
      "confidence": 0.88,
      "weight": 0.35
    },
    "adaboost": {
      "prediction": 1,
      "confidence": 0.95,
      "weight": 0.40
    }
  },
  "timestamp": "2025-10-21T12:00:00Z"
}
```

---

## Overfitting Detection

### Metrics

**Overfitting Gap:**
```
gap = train_accuracy - val_accuracy
```

**Interpretation:**
- `< 0.02`: ✅ Well-fit model
- `0.02 - 0.05`: ⚠️ Mild overfitting
- `0.05 - 0.10`: 🔴 Moderate overfitting
- `> 0.10`: 🚨 Severe overfitting

### Detection Methods

1. **Learning Curves**
   - Train vs. validation accuracy over training set sizes
   - Converging curves = good fit
   - Diverging curves = overfitting

2. **K-Fold Cross-Validation**
   - 5-fold validation with different data splits
   - Low variance = stable model
   - High variance = overfitting

3. **Validation Loss Monitoring**
   - Early stopping when validation loss increases
   - ReduceLROnPlateau for adaptive learning rate

4. **Regularization**
   - XGBoost: L1/L2 regularization, subsample, colsample
   - LSTM: Dropout (20%), BatchNormalization

---

## Visualizations

### Available Visualizations

All visualizations are interactive (Plotly) and can be embedded in dashboards.

#### 1. Learning Curves

**Endpoint:**
```
GET /visualizations/learning-curves/{model_id}
```

**Purpose:** Detect overfitting via train/val gap

![Learning Curves Example](https://via.placeholder.com/800x400?text=Learning+Curves)

#### 2. ROC Curve

**Endpoint:**
```
POST /visualizations/roc-curve
```

**Purpose:** Evaluate classifier performance

**Usage:**
```python
{
  "y_true": [0, 1, 1, 0, 1, ...],
  "y_pred_proba": [0.2, 0.8, 0.9, 0.3, 0.7, ...]
}
```

#### 3. Feature Importance

**Endpoint:**
```
GET /visualizations/feature-importance/{model_id}?top_n=20
```

**Purpose:** Understand model drivers

#### 4. Confusion Matrix

**Endpoint:**
```
POST /visualizations/confusion-matrix
```

**Purpose:** Detailed prediction breakdown

#### 5. Training History (LSTM)

**Endpoint:**
```
GET /visualizations/training-history/{model_id}
```

**Purpose:** Track LSTM training progression

#### 6. Model Comparison

**Endpoint:**
```
GET /visualizations/model-comparison
```

**Purpose:** Compare all models side-by-side

#### 7. Overfitting Dashboard

**Endpoint:**
```
GET /visualizations/overfitting-dashboard/{model_id}
```

**Purpose:** Comprehensive overfitting analysis

---

## API Endpoints

### Core Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/models` | List all models |
| GET | `/models/{id}` | Get model details |
| POST | `/train` | Start training job |
| GET | `/training/{id}` | Get training status |
| POST | `/predict` | Single model prediction |
| POST | `/predict/ensemble` | Ensemble prediction |
| POST | `/drift` | Check model drift |
| POST | `/overfitting` | Analyze overfitting |

### Visualization Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/visualizations/learning-curves/{id}` | Overfitting detection |
| GET | `/visualizations/feature-importance/{id}` | Feature analysis |
| POST | `/visualizations/roc-curve` | ROC curve |
| POST | `/visualizations/confusion-matrix` | Confusion matrix |
| GET | `/visualizations/training-history/{id}` | LSTM training |
| GET | `/visualizations/model-comparison` | Compare models |
| GET | `/visualizations/overfitting-dashboard/{id}` | Full dashboard |

---

## Usage Examples

### Python

```python
import requests

# Single prediction
response = requests.post("http://localhost:5050/predict", json={
    "symbol": "BTCUSDT",
    "recent_candles": candles_data
})
prediction = response.json()
print(f"Reversal: {prediction['is_reversal']}, Confidence: {prediction['confidence']}")

# Ensemble prediction (recommended)
response = requests.post(
    "http://localhost:5050/predict/ensemble",
    params={"strategy": "weighted"},
    json={"symbol": "BTCUSDT", "recent_candles": candles_data}
)
ensemble_pred = response.json()
print(f"Ensemble Confidence: {ensemble_pred['confidence']}")

# Get visualizations
response = requests.get("http://localhost:5050/visualizations/model-comparison")
viz_data = response.json()
# viz_data['visualization'] contains Plotly figure JSON
```

### C#

```csharp
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5050") };

// Ensemble prediction
var request = new {
    symbol = "BTCUSDT",
    recent_candles = candlesData
};

var response = await client.PostAsJsonAsync(
    "/predict/ensemble?strategy=weighted",
    request
);

var prediction = await response.Content.ReadFromJsonAsync<EnsemblePrediction>();
Console.WriteLine($"Confidence: {prediction.Confidence}");
```

### JavaScript

```javascript
// Fetch ensemble prediction
const response = await fetch('http://localhost:5050/predict/ensemble?strategy=weighted', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    symbol: 'BTCUSDT',
    recent_candles: candlesData
  })
});

const prediction = await response.json();
console.log(`Confidence: ${prediction.confidence}`);

// Get visualization
const vizResponse = await fetch('http://localhost:5050/visualizations/learning-curves/latest');
const vizData = await vizResponse.json();
// Use Plotly.js to render: Plotly.newPlot('chart', vizData.visualization);
```

---

## Best Practices

### Training

1. **Use Sufficient Data**
   - Minimum: 30 days of 1h candles (~720 samples)
   - Recommended: 90 days (~2,160 samples)

2. **Monitor Overfitting**
   - Always check learning curves
   - Validate with k-fold cross-validation
   - Keep overfitting gap < 0.05

3. **Regularization**
   - XGBoost: Use subsample and colsample_bytree
   - LSTM: Use dropout and batch normalization

4. **Feature Engineering**
   - Normalize all features with StandardScaler
   - Remove highly correlated features (correlation > 0.95)
   - Use domain knowledge (technical indicators)

### Prediction

1. **Use Ensemble**
   - More robust than single models
   - Reduces prediction variance
   - Higher confidence signals

2. **Check Confidence**
   - Only act on high-confidence predictions (> 0.7)
   - Use confidence-based ensemble strategy

3. **Monitor Drift**
   - Check model drift weekly
   - Retrain if drift score > 0.25

### Production

1. **Model Versioning**
   - Keep last 5 versions
   - Tag production model
   - Enable rollback capability

2. **A/B Testing**
   - Test new models against production
   - Gradual rollout (10% → 50% → 100%)

3. **Monitoring**
   - Track prediction accuracy daily
   - Alert on performance degradation
   - Automate retraining when needed

---

## Troubleshooting

### Model Not Loading

```bash
# Check model directory
ls -la /root/AlgoTrendy_v2.6/ml_models/trend_reversals/

# Verify latest symlink
ls -l /root/AlgoTrendy_v2.6/ml_models/trend_reversals/latest
```

### Low Accuracy

1. Check overfitting gap
2. Increase training data
3. Add regularization
4. Try different hyperparameters

### High Latency

1. Use single model instead of ensemble
2. Cache predictions (5-minute TTL)
3. Reduce sequence length for LSTM

---

## References

- **XGBoost Documentation:** https://xgboost.readthedocs.io/
- **TensorFlow/Keras:** https://www.tensorflow.org/api_docs/python/tf/keras
- **Plotly Python:** https://plotly.com/python/
- **FastAPI:** https://fastapi.tiangolo.com/

---

**Support:** For ML system issues, check `/docs/ml/` or contact the development team.

**Last Updated:** October 21, 2025
**Version:** 2.6.0
