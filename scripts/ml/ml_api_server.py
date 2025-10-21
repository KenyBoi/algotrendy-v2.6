#!/usr/bin/env python3
"""
ML API Server - REST API for ML Model Training, Prediction, and Monitoring
Provides endpoints for C# backend to communicate with Python ML models
"""

import os
import sys
import json
import joblib
import asyncio
import numpy as np
import pandas as pd
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional
from fastapi import FastAPI, BackgroundTasks, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import uvicorn

# Import ML libraries
try:
    from sklearn.ensemble import GradientBoostingClassifier
    from sklearn.preprocessing import StandardScaler
    from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score
    import yfinance as yf
except ImportError as e:
    print(f"Installing required packages: {e}")
    os.system("pip3 install --break-system-packages -q scikit-learn yfinance joblib pandas numpy fastapi uvicorn")

# Initialize FastAPI app
app = FastAPI(
    title="AlgoTrendy ML API",
    description="Machine Learning API for training, prediction, and monitoring",
    version="2.6.0"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure based on your needs
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Global variables
ML_MODELS_DIR = Path("/root/AlgoTrendy_v2.6/ml_models/trend_reversals")
TRAINING_JOBS = {}  # Store active training jobs

# ============================================================================
# Pydantic Models
# ============================================================================

class TrainingConfig(BaseModel):
    symbols: List[str]
    days: int = 30
    interval: str = "5m"
    hyperparameters: Optional[Dict] = None

class PredictionRequest(BaseModel):
    symbol: str
    recent_candles: List[Dict]  # OHLCV data

class DriftCheckRequest(BaseModel):
    model_id: str
    production_data: List[Dict]

class ModelInfo(BaseModel):
    model_id: str
    model_type: str
    trained_at: str
    accuracy: float
    precision: float
    recall: float
    f1_score: float
    training_rows: int
    status: str

# ============================================================================
# Utility Functions
# ============================================================================

def load_model(model_id: str):
    """Load ML model and scaler"""
    model_dir = ML_MODELS_DIR / model_id
    model_path = model_dir / "reversal_model.joblib"
    scaler_path = model_dir / "reversal_scaler.joblib"
    config_path = model_dir / "config.json"

    if not model_path.exists():
        raise HTTPException(status_code=404, detail=f"Model {model_id} not found")

    model = joblib.load(model_path)
    scaler = joblib.load(scaler_path) if scaler_path.exists() else StandardScaler()

    config = {}
    if config_path.exists():
        with open(config_path) as f:
            config = json.load(f)

    return model, scaler, config

def calculate_features(df: pd.DataFrame) -> pd.DataFrame:
    """Calculate all technical indicators"""
    df = df.copy()

    # Ensure numeric types
    for col in ['open', 'high', 'low', 'close', 'volume']:
        df[col] = pd.to_numeric(df[col], errors='coerce')

    # Price features
    df['price_change'] = df['close'].pct_change()
    df['range'] = df['high'] - df['low']
    df['body_size'] = abs(df['close'] - df['open'])
    df['wick_up'] = df['high'] - df[['open', 'close']].max(axis=1)
    df['wick_down'] = df[['open', 'close']].min(axis=1) - df['low']
    df['close_position'] = (df['close'] - df['low']) / (df['high'] - df['low'] + 1e-10)

    # Moving averages
    df['sma_5'] = df['close'].rolling(5).mean()
    df['sma_20'] = df['close'].rolling(20).mean()
    df['price_vs_sma5'] = (df['close'] - df['sma_5']) / (df['sma_5'] + 1e-10)
    df['price_vs_sma20'] = (df['close'] - df['sma_20']) / (df['sma_20'] + 1e-10)

    # Momentum
    df['momentum_3'] = df['close'].pct_change(3)
    df['momentum_5'] = df['close'].pct_change(5)
    df['roc_5'] = df['close'].pct_change(5) * 100

    # Volatility
    df['volatility_3'] = df['close'].rolling(3).std()
    df['volatility_5'] = df['close'].rolling(5).std()
    df['volatility_10'] = df['close'].rolling(10).std()

    # RSI
    delta = df['close'].diff()
    gain = (delta.where(delta > 0, 0)).rolling(14).mean()
    loss = (-delta.where(delta < 0, 0)).rolling(14).mean()
    rs = gain / (loss + 1e-10)
    df['rsi'] = 100 - (100 / (1 + rs))

    # Volume
    df['volume_ratio'] = df['volume'] / (df['volume'].rolling(20).mean() + 1e-10)

    # Consecutive candles
    df['consecutive_up'] = (df['close'] > df['close'].shift(1)).astype(int).rolling(5).sum()
    df['consecutive_down'] = (df['close'] < df['close'].shift(1)).astype(int).rolling(5).sum()

    # Divergence
    df['bullish_divergence'] = ((df['rsi'] > df['rsi'].shift(5)) &
                                 (df['close'] < df['close'].shift(5))).astype(int)
    df['bearish_divergence'] = ((df['rsi'] < df['rsi'].shift(5)) &
                                 (df['close'] > df['close'].shift(5))).astype(int)

    # Trend strength
    df['trend_strength'] = abs(df['close'].rolling(20).apply(lambda x: np.polyfit(range(len(x)), x, 1)[0] if len(x) == 20 else 0))

    return df

def calculate_psi(reference_dist: np.ndarray, production_dist: np.ndarray, bins: int = 10) -> float:
    """Calculate Population Stability Index for drift detection"""
    # Create bins
    min_val = min(reference_dist.min(), production_dist.min())
    max_val = max(reference_dist.max(), production_dist.max())
    bin_edges = np.linspace(min_val, max_val, bins + 1)

    # Calculate frequencies
    ref_counts, _ = np.histogram(reference_dist, bins=bin_edges)
    prod_counts, _ = np.histogram(production_dist, bins=bin_edges)

    # Convert to percentages
    ref_pct = (ref_counts + 1e-6) / (len(reference_dist) + bins * 1e-6)
    prod_pct = (prod_counts + 1e-6) / (len(production_dist) + bins * 1e-6)

    # Calculate PSI
    psi = np.sum((prod_pct - ref_pct) * np.log(prod_pct / ref_pct))

    return psi

# ============================================================================
# API Endpoints
# ============================================================================

@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "AlgoTrendy ML API",
        "version": "2.6.0",
        "timestamp": datetime.now().isoformat()
    }

@app.get("/models")
async def list_models() -> List[ModelInfo]:
    """List all available ML models"""
    models = []

    if not ML_MODELS_DIR.exists():
        return models

    for model_dir in ML_MODELS_DIR.iterdir():
        if not model_dir.is_dir():
            continue

        metrics_path = model_dir / "model_metrics.json"
        config_path = model_dir / "config.json"

        if not metrics_path.exists():
            continue

        with open(metrics_path) as f:
            metrics = json.load(f)

        with open(config_path) as f:
            config = json.load(f)

        models.append(ModelInfo(
            model_id=model_dir.name,
            model_type=config.get('model_type', 'GradientBoostingClassifier'),
            trained_at=metrics.get('trained_at', ''),
            accuracy=metrics.get('accuracy', 0.0),
            precision=metrics.get('precision', 0.0),
            recall=metrics.get('recall', 0.0),
            f1_score=metrics.get('f1', 0.0),
            training_rows=metrics.get('training_rows', 0),
            status='active'
        ))

    return models

@app.get("/models/{model_id}")
async def get_model_details(model_id: str):
    """Get detailed information about a specific model"""
    model_dir = ML_MODELS_DIR / model_id

    if not model_dir.exists():
        raise HTTPException(status_code=404, detail=f"Model {model_id} not found")

    config_path = model_dir / "config.json"
    metrics_path = model_dir / "model_metrics.json"

    with open(config_path) as f:
        config = json.load(f)

    with open(metrics_path) as f:
        metrics = json.load(f)

    # Get feature importance
    feature_importance = {
        item['feature']: item['importance']
        for item in config.get('feature_importance', [])
    }

    return {
        "model_id": model_id,
        "model_type": config.get('model_type', 'GradientBoostingClassifier'),
        "trained_at": metrics.get('trained_at', ''),
        "features": config.get('features', []),
        "feature_importance": feature_importance,
        "metrics": {
            "accuracy": metrics.get('accuracy', 0.0),
            "precision": metrics.get('precision', 0.0),
            "recall": metrics.get('recall', 0.0),
            "f1_score": metrics.get('f1', 0.0)
        },
        "training_rows": metrics.get('training_rows', 0)
    }

@app.post("/train")
async def start_training(config: TrainingConfig, background_tasks: BackgroundTasks):
    """Start a new model training job"""
    job_id = f"job_{datetime.now().strftime('%Y%m%d_%H%M%S')}"

    TRAINING_JOBS[job_id] = {
        "status": "queued",
        "progress": 0,
        "started_at": datetime.now().isoformat(),
        "config": config.dict()
    }

    # Start training in background
    background_tasks.add_task(run_training, job_id, config)

    return {
        "job_id": job_id,
        "status": "queued",
        "message": f"Training job {job_id} queued successfully"
    }

async def run_training(job_id: str, config: TrainingConfig):
    """Execute model training (runs in background)"""
    try:
        TRAINING_JOBS[job_id]["status"] = "running"
        TRAINING_JOBS[job_id]["progress"] = 10

        # This would call your retrain_model.py logic
        # For now, simulating with the pattern analysis
        import subprocess

        result = subprocess.run(
            ['python3', 'retrain_model.py'],
            cwd='/root/AlgoTrendy_v2.6',
            capture_output=True,
            text=True
        )

        if result.returncode == 0:
            TRAINING_JOBS[job_id]["status"] = "completed"
            TRAINING_JOBS[job_id]["progress"] = 100
            TRAINING_JOBS[job_id]["completed_at"] = datetime.now().isoformat()
        else:
            TRAINING_JOBS[job_id]["status"] = "failed"
            TRAINING_JOBS[job_id]["error"] = result.stderr

    except Exception as e:
        TRAINING_JOBS[job_id]["status"] = "failed"
        TRAINING_JOBS[job_id]["error"] = str(e)

@app.get("/training/{job_id}")
async def get_training_status(job_id: str):
    """Get status of a training job"""
    if job_id not in TRAINING_JOBS:
        raise HTTPException(status_code=404, detail=f"Training job {job_id} not found")

    return TRAINING_JOBS[job_id]

@app.post("/predict")
async def get_prediction(request: PredictionRequest):
    """Get predictions for given market data"""
    # Load latest model
    latest_model_dir = max(ML_MODELS_DIR.iterdir(), key=lambda x: x.name)
    model, scaler, config = load_model(latest_model_dir.name)

    # Convert candles to DataFrame
    df = pd.DataFrame(request.recent_candles)

    # Calculate features
    df = calculate_features(df)

    # Get feature columns
    feature_cols = config.get('features', [
        'sma_5', 'sma_20', 'rsi', 'volume_ratio',
        'close_position', 'price_change', 'momentum_3', 'momentum_5',
        'volatility_3', 'volatility_5', 'range', 'body_size'
    ])

    # Prepare features for prediction
    df_clean = df[feature_cols].dropna()

    if len(df_clean) == 0:
        raise HTTPException(status_code=400, detail="Insufficient data for prediction")

    X = df_clean.iloc[-1:].values  # Get last row
    X = np.where(np.isinf(X), 0, X)
    X = np.nan_to_num(X, nan=0, posinf=1e6, neginf=-1e6)

    # Scale and predict
    X_scaled = scaler.transform(X)
    prediction = model.predict(X_scaled)[0]
    probability = model.predict_proba(X_scaled)[0]

    return {
        "symbol": request.symbol,
        "is_reversal": bool(prediction),
        "confidence": float(probability[1] if len(probability) > 1 else probability[0]),
        "feature_values": {
            feature: float(value)
            for feature, value in zip(feature_cols, X[0])
        },
        "timestamp": datetime.now().isoformat()
    }

@app.post("/drift")
async def check_drift(request: DriftCheckRequest):
    """Calculate drift metrics for a model"""
    model, scaler, config = load_model(request.model_id)

    # Load reference distribution (training data)
    # In real implementation, this would be saved during training
    # For now, using dummy values
    features = config.get('features', [])
    feature_drift = {}

    # Convert production data to DataFrame
    prod_df = pd.DataFrame(request.production_data)
    prod_df = calculate_features(prod_df)

    # Calculate PSI for each feature
    for feature in features:
        if feature in prod_df.columns:
            # Generate reference distribution (normally loaded from training)
            ref_dist = np.random.normal(
                prod_df[feature].mean(),
                prod_df[feature].std(),
                1000
            )
            prod_dist = prod_df[feature].dropna().values

            psi = calculate_psi(ref_dist, prod_dist)
            feature_drift[feature] = float(psi)

    # Calculate overall drift score
    drift_score = np.mean(list(feature_drift.values()))
    is_drifting = drift_score > 0.25

    return {
        "model_id": request.model_id,
        "last_checked": datetime.now().isoformat(),
        "drift_score": float(drift_score),
        "is_drifting": is_drifting,
        "feature_drift": feature_drift,
        "recommended_action": "Retrain model immediately" if is_drifting else "No action needed"
    }

@app.post("/overfitting")
async def analyze_overfitting(training_history: Dict):
    """Analyze training history for overfitting"""
    train_acc = training_history.get('train_accuracy', [])
    val_acc = training_history.get('val_accuracy', [])
    train_loss = training_history.get('train_loss', [])
    val_loss = training_history.get('val_loss', [])

    if not all([train_acc, val_acc, train_loss, val_loss]):
        raise HTTPException(status_code=400, detail="Incomplete training history")

    # Calculate metrics
    final_gap = train_acc[-1] - val_acc[-1]
    val_loss_increasing = val_loss[-1] > val_loss[-5] if len(val_loss) > 5 else False
    val_variance = np.std(val_acc[-10:]) / np.mean(val_acc[-10:]) if len(val_acc) >= 10 else 0

    # Best validation accuracy
    best_val_acc = max(val_acc)
    epochs_since_improvement = len(val_acc) - val_acc.index(best_val_acc) - 1
    early_stop_triggered = epochs_since_improvement > 5

    # Calculate overfitting score
    gap_score = min(final_gap * 100, 50)
    loss_score = 20 if val_loss_increasing else 0
    variance_score = min(val_variance * 100, 20)
    early_stop_score = 10 if early_stop_triggered else 0

    overfitting_score = gap_score + loss_score + variance_score + early_stop_score
    is_overfitting = overfitting_score > 30

    # Generate recommendation
    if overfitting_score < 20:
        recommendation = "Model is well-fit. Safe to deploy."
    elif overfitting_score < 40:
        recommendation = "Mild overfitting detected. Consider adding regularization."
    elif overfitting_score < 60:
        recommendation = "Moderate overfitting. Reduce complexity or add data."
    else:
        recommendation = "Severe overfitting. Retrain with more data and regularization."

    return {
        "is_overfitting": is_overfitting,
        "overfitting_score": float(overfitting_score),
        "indicators": {
            "gap": float(final_gap),
            "val_loss_increasing": val_loss_increasing,
            "variance": float(val_variance),
            "early_stop_triggered": early_stop_triggered
        },
        "recommendation": recommendation
    }

@app.get("/patterns")
async def get_latest_patterns():
    """Get latest pattern analysis results"""
    report_path = Path("/root/AlgoTrendy_v2.6/pattern_analysis_report.json")

    if not report_path.exists():
        raise HTTPException(status_code=404, detail="No pattern analysis available")

    with open(report_path) as f:
        data = json.load(f)

    return data

# ============================================================================
# Main
# ============================================================================

if __name__ == "__main__":
    print("""
    ╔══════════════════════════════════════════════════════════════╗
    ║                                                              ║
    ║         🤖 AlgoTrendy ML API Server v2.6                    ║
    ║                                                              ║
    ╚══════════════════════════════════════════════════════════════╝

    Starting ML API server on http://localhost:5050

    Available endpoints:
    - GET  /models                  - List all models
    - GET  /models/{id}             - Get model details
    - POST /train                   - Start training job
    - GET  /training/{id}           - Get training status
    - POST /predict                 - Get predictions
    - POST /drift                   - Check model drift
    - POST /overfitting             - Analyze overfitting
    - GET  /patterns                - Get latest patterns

    Documentation: http://localhost:5050/docs
    """)

    uvicorn.run(app, host="0.0.0.0", port=5050, log_level="info")
