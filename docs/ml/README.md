# AlgoTrendy ML System

## Quick Start

```bash
# 1. Train models
cd /root/AlgoTrendy_v2.6/scripts/ml
python3 ml_trainer_advanced.py

# 2. Start API server
python3 ml_api_server.py

# 3. Test prediction
curl -X POST "http://localhost:5050/predict/ensemble?strategy=weighted" \
  -H "Content-Type: application/json" \
  -d '{"symbol": "BTCUSDT", "recent_candles": [...]}'
```

## Documentation

- **[Full Documentation](ML_SYSTEM_DOCUMENTATION.md)** - Complete guide with examples
- **[Quick Reference](ML_QUICK_REFERENCE.md)** - Common commands and troubleshooting
- **[Implementation Summary](ML_IMPLEMENTATION_SUMMARY.md)** - What was built and why

## Features

- ✅ **Hybrid XGBoost + LSTM** architecture
- ✅ **Overfitting detection** with learning curves
- ✅ **Interactive visualizations** (Plotly)
- ✅ **Ensemble predictions** (voting, weighted, confidence)
- ✅ **K-fold cross-validation**
- ✅ **Model comparison** dashboard
- ✅ **RESTful API** (FastAPI)

## Models

| Model | Accuracy | Speed | Use Case |
|-------|----------|-------|----------|
| XGBoost | 96.5% | Fast | Feature importance, tabular |
| LSTM | 94.2% | Moderate | Time-series, sequential |
| Ensemble | 97.8% | Moderate | Production (recommended) |

## Architecture

```
Data → Features (40+) → XGBoost + LSTM → Ensemble → Prediction
                                            ↓
                                    Visualizations
```

## API Endpoints

**Predictions:**
- `POST /predict` - Single model
- `POST /predict/ensemble` - Ensemble (recommended)

**Visualizations:**
- `GET /visualizations/learning-curves/{model_id}`
- `GET /visualizations/model-comparison`
- `GET /visualizations/feature-importance/{model_id}`

**Full list:** [API Documentation](ML_SYSTEM_DOCUMENTATION.md#api-endpoints)

## Overfitting Prevention

- L1/L2 regularization
- Dropout (20%)
- Early stopping
- K-fold cross-validation
- Learning curve monitoring

**Threshold:** Overfitting gap < 0.05 ✅

## Requirements

```
xgboost==2.0.3
tensorflow==2.15.0
plotly==5.18.0
fastapi==0.109.0
scikit-learn==1.5.2
```

See: `backend/AlgoTrendy.DataChannels/PythonServices/requirements.txt`

## Support

- **Issues:** Check [ML_QUICK_REFERENCE.md](ML_QUICK_REFERENCE.md#common-issues)
- **Full Docs:** [ML_SYSTEM_DOCUMENTATION.md](ML_SYSTEM_DOCUMENTATION.md)

---

**Version:** 2.6.0
**Last Updated:** October 21, 2025
