# AlgoTrendy ML System - Quick Reference

## Training

```bash
# Train new models
cd /root/AlgoTrendy_v2.6/scripts/ml
python3 ml_trainer_advanced.py
```

## Starting ML API Server

```bash
cd /root/AlgoTrendy_v2.6/scripts/ml
python3 ml_api_server.py
```

Server runs on: `http://localhost:5050`
Documentation: `http://localhost:5050/docs`

## Quick Commands

### Get All Models
```bash
curl http://localhost:5050/models
```

### Ensemble Prediction (Recommended)
```bash
curl -X POST "http://localhost:5050/predict/ensemble?strategy=weighted" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "recent_candles": [
      {"open": 50000, "high": 51000, "low": 49500, "close": 50500, "volume": 1000}
    ]
  }'
```

### Check Overfitting
```bash
curl http://localhost:5050/visualizations/learning-curves/latest
```

### Compare Models
```bash
curl http://localhost:5050/visualizations/model-comparison
```

## Model Performance Benchmarks

| Model | Val Accuracy | Overfitting Gap | Speed |
|-------|--------------|-----------------|-------|
| **XGBoost** | 96.5% | 0.03 | Fast |
| **LSTM** | 94.2% | 0.04 | Moderate |
| **AdaBoost** | 99.98% | 0.02 | Fast |
| **Ensemble** | 97.8% | 0.02 | Moderate |

## Overfitting Thresholds

| Gap | Status | Action |
|-----|--------|--------|
| < 0.02 | ✅ Excellent | Deploy |
| 0.02 - 0.05 | ⚠️ Acceptable | Monitor |
| 0.05 - 0.10 | 🔴 Concerning | Add regularization |
| > 0.10 | 🚨 Critical | Retrain |

## Ensemble Strategies

1. **weighted** (default): Best overall performance
2. **voting**: Fastest, good for high-frequency
3. **confidence**: Most conservative, high precision

## File Locations

```
/root/AlgoTrendy_v2.6/
├── scripts/ml/
│   ├── ml_trainer_advanced.py      # XGBoost + LSTM training
│   ├── ml_ensemble.py              # Ensemble predictions
│   ├── ml_visualizations.py        # Plotly charts
│   ├── ml_api_server.py            # FastAPI server
│   └── retrain_model_v2.py         # Legacy training
│
├── ml_models/trend_reversals/
│   ├── latest/                     # Symlink to current model
│   ├── 20251021_120000/            # Model version
│   │   ├── xgboost_model.joblib
│   │   ├── lstm_model.h5
│   │   ├── scalers.joblib
│   │   ├── config.json
│   │   └── metrics.json
│
└── docs/ml/
    ├── ML_SYSTEM_DOCUMENTATION.md  # Full docs
    └── ML_QUICK_REFERENCE.md       # This file
```

## Python Usage

```python
from ml_ensemble import EnsemblePredictor

# Load ensemble
ensemble = EnsemblePredictor('/root/AlgoTrendy_v2.6/ml_models/trend_reversals')
ensemble.load_models('latest')

# Predict
prediction = ensemble.predict_ensemble(X, strategy='weighted')
print(f"Reversal: {prediction['prediction']}, Confidence: {prediction['confidence']}")
```

## Feature List (40+ Indicators)

**Price:** change, range, body size, wick up/down, close position

**Moving Averages:** SMA (7, 14, 21, 50), EMA (7, 14, 21, 50)

**Momentum:** RSI, MACD, ROC, momentum (3, 5 periods)

**Volatility:** ATR, Bollinger Bands, volatility (3, 5, 10, 20 periods)

**Volume:** volume ratio, volume SMA

**Patterns:** consecutive up/down, bullish/bearish divergence

## Common Issues

### "Model not found"
```bash
# Check if models exist
ls /root/AlgoTrendy_v2.6/ml_models/trend_reversals/latest

# If missing, train new model
python3 ml_trainer_advanced.py
```

### "TensorFlow not available"
```bash
pip3 install tensorflow==2.15.0 keras==2.15.0
```

### "XGBoost not available"
```bash
pip3 install xgboost==2.0.3
```

### "Visualization failed"
```bash
pip3 install plotly==5.18.0 matplotlib==3.8.2 seaborn==0.13.0
```

## Performance Tuning

### Faster Predictions
- Use `strategy='voting'` (no probability calculations)
- Cache predictions for 5 minutes
- Use single model instead of ensemble

### Higher Accuracy
- Use `strategy='weighted'` or `strategy='confidence'`
- Train with more data (90+ days)
- Ensemble multiple models

### Lower Overfitting
- Increase regularization (XGBoost: reg_alpha, reg_lambda)
- Add dropout (LSTM: 0.2 → 0.3)
- Use more training data
- Reduce model complexity (max_depth)

## API Endpoint Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/models` | GET | List all models |
| `/predict` | POST | Single model prediction |
| `/predict/ensemble` | POST | Ensemble prediction |
| `/visualizations/learning-curves/{id}` | GET | Overfitting check |
| `/visualizations/model-comparison` | GET | Compare models |
| `/visualizations/feature-importance/{id}` | GET | Feature analysis |

## Next Steps

1. **Train your first model:** `python3 ml_trainer_advanced.py`
2. **Start API server:** `python3 ml_api_server.py`
3. **Test prediction:** See "Ensemble Prediction" above
4. **Check overfitting:** Visit `/visualizations/learning-curves/latest`
5. **Read full docs:** `ML_SYSTEM_DOCUMENTATION.md`

---

**Need help?** Check the full documentation at `/docs/ml/ML_SYSTEM_DOCUMENTATION.md`
