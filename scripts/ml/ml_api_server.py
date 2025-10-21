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

# Import visualization module
try:
    from ml_visualizations import MLVisualizer
    VISUALIZATIONS_AVAILABLE = True
except ImportError:
    VISUALIZATIONS_AVAILABLE = False
    print("⚠️  Visualization module not available")

# Import ensemble module
try:
    from ml_ensemble import EnsemblePredictor
    ENSEMBLE_AVAILABLE = True
except ImportError:
    ENSEMBLE_AVAILABLE = False
    print("⚠️  Ensemble module not available")

# Import A/B testing module
try:
    from ml_ab_testing import ABTestFramework
    AB_TESTING_AVAILABLE = True
    ab_framework = ABTestFramework()
except ImportError:
    AB_TESTING_AVAILABLE = False
    ab_framework = None
    print("⚠️  A/B testing module not available")

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

@app.post("/predict/ensemble")
async def get_ensemble_prediction(
    request: PredictionRequest,
    strategy: str = "weighted",
    model_version: str = "latest"
):
    """
    Get ensemble predictions combining multiple models

    Strategies:
    - voting: Simple majority vote
    - weighted: Performance-based weighting
    - confidence: Use only high-confidence models
    """
    if not ENSEMBLE_AVAILABLE:
        raise HTTPException(status_code=501, detail="Ensemble module not available")

    # Convert candles to DataFrame
    df = pd.DataFrame(request.recent_candles)

    # Calculate features
    df = calculate_features(df)

    # Get feature columns (from config or defaults)
    feature_cols = [
        'sma_5', 'sma_20', 'rsi', 'volume_ratio',
        'close_position', 'price_change', 'momentum_3', 'momentum_5',
        'volatility_3', 'volatility_5', 'range', 'body_size'
    ]

    # Prepare features for prediction
    df_clean = df[feature_cols].dropna()

    if len(df_clean) == 0:
        raise HTTPException(status_code=400, detail="Insufficient data for prediction")

    X = df_clean.iloc[-1].values  # Get last row as 1D array
    X = np.where(np.isinf(X), 0, X)
    X = np.nan_to_num(X, nan=0, posinf=1e6, neginf=-1e6)

    # Load ensemble and predict
    ensemble = EnsemblePredictor(ML_MODELS_DIR)
    ensemble.load_models(model_version)

    prediction = ensemble.predict_ensemble(X, strategy=strategy)

    # Add symbol and feature values
    prediction['symbol'] = request.symbol
    prediction['is_reversal'] = bool(prediction['prediction'])
    prediction['feature_values'] = {
        feature: float(value)
        for feature, value in zip(feature_cols, X)
    }

    return prediction

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
# A/B Testing Endpoints
# ============================================================================

@app.post("/ab-test/create")
async def create_ab_test(
    model_a: str,
    model_b: str,
    traffic_split: float = 0.5,
    min_samples: int = 100,
    confidence_level: float = 0.95
):
    """Create a new A/B test"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    test_id = ab_framework.create_test(
        model_a=model_a,
        model_b=model_b,
        traffic_split=traffic_split,
        min_samples=min_samples,
        confidence_level=confidence_level
    )

    return {
        "test_id": test_id,
        "status": "created",
        "model_a": model_a,
        "model_b": model_b,
        "traffic_split": traffic_split
    }

@app.get("/ab-test/list")
async def list_ab_tests():
    """List all active A/B tests"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    tests = ab_framework.list_tests()
    return {
        "tests": [
            {
                "test_id": test.test_id,
                "model_a": test.model_a,
                "model_b": test.model_b,
                "traffic_split": test.traffic_split,
                "status": test.status,
                "start_date": test.start_date,
                "end_date": test.end_date
            }
            for test in tests
        ]
    }

@app.get("/ab-test/{test_id}/assign")
async def assign_ab_variant(test_id: str, user_id: Optional[str] = None):
    """Assign a model variant for a prediction"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    try:
        model = ab_framework.assign_model(test_id, user_id)
        return {
            "test_id": test_id,
            "assigned_model": model,
            "user_id": user_id
        }
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))

@app.post("/ab-test/{test_id}/record")
async def record_ab_result(
    test_id: str,
    model_id: str,
    prediction: int,
    actual: Optional[int] = None,
    confidence: float = 0.5
):
    """Record a prediction result for A/B testing"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    ab_framework.record_prediction(test_id, model_id, prediction, actual, confidence)

    return {
        "test_id": test_id,
        "model_id": model_id,
        "recorded": True
    }

@app.get("/ab-test/{test_id}/analyze")
async def analyze_ab_test(test_id: str):
    """Analyze A/B test results and determine winner"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    result = ab_framework.analyze_test(test_id)

    if not result:
        raise HTTPException(status_code=404, detail="Insufficient data for analysis")

    from dataclasses import asdict
    return asdict(result)

@app.post("/ab-test/{test_id}/stop")
async def stop_ab_test(test_id: str, reason: str = "Manual stop"):
    """Stop an active A/B test"""
    if not AB_TESTING_AVAILABLE or not ab_framework:
        raise HTTPException(status_code=501, detail="A/B testing not available")

    ab_framework.stop_test(test_id, reason)

    return {
        "test_id": test_id,
        "status": "stopped",
        "reason": reason
    }

# ============================================================================
# Visualization Endpoints
# ============================================================================

@app.get("/visualizations/learning-curves/{model_id}")
async def get_learning_curves(model_id: str):
    """Get learning curves visualization for overfitting detection"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    model_dir = ML_MODELS_DIR / model_id
    metrics_path = model_dir / "metrics.json"

    if not metrics_path.exists():
        raise HTTPException(status_code=404, detail=f"Metrics for model {model_id} not found")

    with open(metrics_path) as f:
        metrics = json.load(f)

    # Check if learning curve data exists
    if 'learning_curves' not in metrics:
        raise HTTPException(status_code=404, detail="Learning curves data not found for this model")

    lc_data = metrics['learning_curves']
    viz_json = MLVisualizer.create_learning_curves(
        train_sizes=np.array(lc_data['train_sizes']),
        train_scores=np.array(lc_data['train_scores']),
        val_scores=np.array(lc_data['val_scores']),
        title=f"Learning Curves - {model_id}"
    )

    return {
        "model_id": model_id,
        "visualization": json.loads(viz_json),
        "type": "learning_curves"
    }

@app.get("/visualizations/feature-importance/{model_id}")
async def get_feature_importance(model_id: str, top_n: int = 20):
    """Get feature importance visualization"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    model_dir = ML_MODELS_DIR / model_id
    config_path = model_dir / "config.json"

    if not config_path.exists():
        raise HTTPException(status_code=404, detail=f"Config for model {model_id} not found")

    with open(config_path) as f:
        config = json.load(f)

    # Get feature importance
    feature_importance = config.get('feature_importance', [])
    if not feature_importance:
        raise HTTPException(status_code=404, detail="Feature importance data not found")

    feature_names = [item['feature'] for item in feature_importance]
    importances = np.array([item['importance'] for item in feature_importance])

    viz_json = MLVisualizer.create_feature_importance(
        feature_names=feature_names,
        importances=importances,
        top_n=top_n,
        title=f"Feature Importance - {model_id}"
    )

    return {
        "model_id": model_id,
        "visualization": json.loads(viz_json),
        "type": "feature_importance"
    }

@app.post("/visualizations/roc-curve")
async def create_roc_curve(y_true: List[int], y_pred_proba: List[float]):
    """Generate ROC curve from predictions"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    if len(y_true) != len(y_pred_proba):
        raise HTTPException(status_code=400, detail="y_true and y_pred_proba must have same length")

    viz_json = MLVisualizer.create_roc_curve(
        y_true=np.array(y_true),
        y_pred_proba=np.array(y_pred_proba),
        title="ROC Curve"
    )

    return {
        "visualization": json.loads(viz_json),
        "type": "roc_curve"
    }

@app.post("/visualizations/confusion-matrix")
async def create_confusion_matrix_viz(y_true: List[int], y_pred: List[int]):
    """Generate confusion matrix visualization"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    if len(y_true) != len(y_pred):
        raise HTTPException(status_code=400, detail="y_true and y_pred must have same length")

    viz_json = MLVisualizer.create_confusion_matrix(
        y_true=np.array(y_true),
        y_pred=np.array(y_pred),
        title="Confusion Matrix"
    )

    return {
        "visualization": json.loads(viz_json),
        "type": "confusion_matrix"
    }

@app.get("/visualizations/training-history/{model_id}")
async def get_training_history(model_id: str):
    """Get training history visualization (for LSTM models)"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    model_dir = ML_MODELS_DIR / model_id
    metrics_path = model_dir / "metrics.json"

    if not metrics_path.exists():
        raise HTTPException(status_code=404, detail=f"Metrics for model {model_id} not found")

    with open(metrics_path) as f:
        metrics = json.load(f)

    # Check if this is an LSTM model with training history
    if 'lstm' not in metrics or 'history' not in metrics['lstm']:
        raise HTTPException(status_code=404, detail="Training history not found (not an LSTM model?)")

    history = metrics['lstm']['history']
    viz_json = MLVisualizer.create_training_history(
        history_dict=history,
        title=f"Training History - {model_id}"
    )

    return {
        "model_id": model_id,
        "visualization": json.loads(viz_json),
        "type": "training_history"
    }

@app.get("/visualizations/model-comparison")
async def get_model_comparison():
    """Compare all available models"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    if not ML_MODELS_DIR.exists():
        raise HTTPException(status_code=404, detail="No models directory found")

    models_metrics = {}

    for model_dir in ML_MODELS_DIR.iterdir():
        if not model_dir.is_dir():
            continue

        metrics_path = model_dir / "metrics.json"
        if not metrics_path.exists():
            # Try old format
            metrics_path = model_dir / "model_metrics.json"
            if not metrics_path.exists():
                continue

        with open(metrics_path) as f:
            metrics = json.load(f)

        # Extract metrics for each model type
        for model_type in ['xgboost', 'lstm', 'adaboost', 'gradient_boosting', 'random_forest']:
            if model_type in metrics:
                model_name = f"{model_dir.name}_{model_type}"
                models_metrics[model_name] = metrics[model_type]
            elif model_type == 'adaboost' and 'accuracy' in metrics:
                # Old format
                models_metrics[model_dir.name] = metrics
                break

    if not models_metrics:
        raise HTTPException(status_code=404, detail="No model metrics found")

    viz_json = MLVisualizer.create_model_comparison(
        models_metrics=models_metrics,
        title="Model Performance Comparison"
    )

    return {
        "visualization": json.loads(viz_json),
        "type": "model_comparison",
        "models_count": len(models_metrics)
    }

@app.get("/visualizations/overfitting-dashboard/{model_id}")
async def get_overfitting_dashboard(model_id: str):
    """Get comprehensive overfitting detection dashboard"""
    if not VISUALIZATIONS_AVAILABLE:
        raise HTTPException(status_code=501, detail="Visualization module not available")

    model_dir = ML_MODELS_DIR / model_id
    metrics_path = model_dir / "metrics.json"

    if not metrics_path.exists():
        raise HTTPException(status_code=404, detail=f"Metrics for model {model_id} not found")

    with open(metrics_path) as f:
        metrics = json.load(f)

    # Ensure we have all required data
    required_keys = ['learning_curves', 'train_predictions', 'val_predictions']
    missing = [k for k in required_keys if k not in metrics]

    if missing:
        return {
            "error": f"Missing data for dashboard: {missing}",
            "available_visualizations": [
                "/visualizations/learning-curves/{model_id}",
                "/visualizations/feature-importance/{model_id}",
                "/visualizations/model-comparison"
            ]
        }

    # Extract data
    lc_data = metrics['learning_curves']
    train_pred = metrics['train_predictions']
    val_pred = metrics['val_predictions']

    viz_json = MLVisualizer.create_overfitting_dashboard(
        learning_curve_data=lc_data,
        train_scores=np.array(lc_data['train_scores']),
        val_scores=np.array(lc_data['val_scores']),
        y_true=np.array(val_pred['y_true']),
        y_pred=np.array(val_pred['y_pred']),
        y_pred_proba=np.array(val_pred['y_pred_proba'])
    )

    return {
        "model_id": model_id,
        "visualization": json.loads(viz_json),
        "type": "overfitting_dashboard"
    }

# ============================================================================
# Main
# ============================================================================

if __name__ == "__main__":
    print("""
    ╔══════════════════════════════════════════════════════════════╗
    ║                                                              ║
    ║         🤖 AlgoTrendy ML API Server v2.6                    ║
    ║         With XGBoost + LSTM + Visualizations                ║
    ║                                                              ║
    ╚══════════════════════════════════════════════════════════════╝

    Starting ML API server on http://localhost:5050

    📊 Core Endpoints:
    - GET  /models                          - List all models
    - GET  /models/{id}                     - Get model details
    - POST /train                           - Start training job
    - GET  /training/{id}                   - Get training status
    - POST /predict                         - Get predictions (single model)
    - POST /predict/ensemble                - Get ensemble predictions
    - POST /drift                           - Check model drift
    - POST /overfitting                     - Analyze overfitting
    - GET  /patterns                        - Get latest patterns

    🧪 A/B Testing Endpoints (NEW):
    - POST /ab-test/create                  - Create A/B test
    - GET  /ab-test/list                    - List active tests
    - GET  /ab-test/{id}/assign             - Assign variant
    - POST /ab-test/{id}/record             - Record result
    - GET  /ab-test/{id}/analyze            - Analyze test
    - POST /ab-test/{id}/stop               - Stop test

    📈 Visualization Endpoints:
    - GET  /visualizations/learning-curves/{model_id}     - Overfitting detection
    - GET  /visualizations/feature-importance/{model_id}  - Feature analysis
    - POST /visualizations/roc-curve                      - ROC curve
    - POST /visualizations/confusion-matrix               - Confusion matrix
    - GET  /visualizations/training-history/{model_id}    - LSTM training
    - GET  /visualizations/model-comparison               - Compare models
    - GET  /visualizations/overfitting-dashboard/{id}     - Full dashboard

    📚 Documentation: http://localhost:5050/docs
    """)

    uvicorn.run(app, host="0.0.0.0", port=5050, log_level="info")
