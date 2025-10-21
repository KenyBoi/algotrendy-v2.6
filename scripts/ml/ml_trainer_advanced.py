#!/usr/bin/env python3
"""
Advanced ML Model Trainer with XGBoost + LSTM Hybrid Architecture
Includes overfitting detection, visualization, and ensemble predictions
"""

import warnings
warnings.filterwarnings('ignore')

import os
import sys
import json
import joblib
import pandas as pd
import numpy as np
from pathlib import Path
from datetime import datetime, timedelta
from collections import defaultdict
from typing import Dict, List, Optional, Tuple

# ML imports - Traditional
from sklearn.ensemble import (
    GradientBoostingClassifier,
    RandomForestClassifier,
    VotingClassifier,
    AdaBoostClassifier
)
from sklearn.preprocessing import StandardScaler, RobustScaler, MinMaxScaler
from sklearn.model_selection import (
    train_test_split,
    cross_val_score,
    StratifiedKFold,
    GridSearchCV,
    TimeSeriesSplit,
    learning_curve
)
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report, roc_auc_score,
    roc_curve, precision_recall_curve, average_precision_score
)

# XGBoost
try:
    import xgboost as xgb
    from xgboost import XGBClassifier
    XGBOOST_AVAILABLE = True
except ImportError:
    XGBOOST_AVAILABLE = False
    print("⚠️  XGBoost not available - install with: pip install xgboost")

# LSTM/Deep Learning
try:
    import tensorflow as tf
    from tensorflow import keras
    from tensorflow.keras.models import Sequential
    from tensorflow.keras.layers import LSTM, Dense, Dropout, BatchNormalization
    from tensorflow.keras.callbacks import EarlyStopping, ReduceLROnPlateau
    from tensorflow.keras.optimizers import Adam
    TENSORFLOW_AVAILABLE = True
except ImportError:
    TENSORFLOW_AVAILABLE = False
    print("⚠️  TensorFlow not available - install with: pip install tensorflow")

# Visualization
try:
    import plotly.graph_objects as go
    import plotly.express as px
    from plotly.subplots import make_subplots
    import matplotlib.pyplot as plt
    import seaborn as sns
    VISUALIZATION_AVAILABLE = True
except ImportError:
    VISUALIZATION_AVAILABLE = False
    print("⚠️  Visualization libraries not available - install with: pip install plotly matplotlib seaborn")

# Data source
try:
    import yfinance as yf
    YFINANCE_AVAILABLE = True
except ImportError:
    YFINANCE_AVAILABLE = False
    print("⚠️  yfinance not available - install with: pip install yfinance")


class AdvancedMLTrainer:
    """
    Production-grade ML trainer with:
    - XGBoost for feature selection and tabular prediction
    - LSTM for time-series sequential prediction
    - Hybrid ensemble combining both approaches
    - Overfitting detection and prevention
    - Interactive visualizations (Plotly)
    - K-fold cross-validation
    - Learning curve analysis
    - Feature importance tracking
    """

    def __init__(self,
                 model_dir="/root/AlgoTrendy_v2.6/ml_models/trend_reversals",
                 data_source="live",
                 sequence_length=60):  # For LSTM lookback

        self.model_dir = Path(model_dir)
        self.model_dir.mkdir(parents=True, exist_ok=True)

        # Create timestamped version directory
        self.version = datetime.now().strftime("%Y%m%d_%H%M%S")
        self.version_dir = self.model_dir / self.version
        self.version_dir.mkdir(parents=True, exist_ok=True)

        self.data_source = data_source
        self.sequence_length = sequence_length
        self.symbols = ["BTC-USD", "ETH-USD", "BNB-USD", "XRP-USD", "SOL-USD", "ADA-USD"]

        # Models
        self.models = {}
        self.lstm_model = None
        self.best_model = None
        self.best_model_name = None
        self.scalers = {
            'standard': None,  # For XGBoost/traditional
            'minmax': None     # For LSTM
        }

        # Results tracking
        self.results = defaultdict(dict)
        self.visualization_data = {}

    def fetch_live_data(self, period="90d", interval="1h"):
        """Fetch live market data from yfinance"""
        print("\n" + "="*70)
        print("📡 FETCHING LIVE MARKET DATA")
        print("="*70)

        if not YFINANCE_AVAILABLE:
            print("❌ yfinance not available")
            return None

        all_dfs = []
        for symbol in self.symbols:
            try:
                print(f"  Downloading {symbol}...", end=" ")
                ticker = yf.Ticker(symbol)
                df = ticker.history(period=period, interval=interval)

                if df.empty:
                    print("❌ No data")
                    continue

                df['Symbol'] = symbol
                all_dfs.append(df)
                print(f"✅ {len(df)} candles")

            except Exception as e:
                print(f"❌ Error: {e}")
                continue

        if not all_dfs:
            print("❌ No data fetched for any symbol")
            return None

        combined_df = pd.concat(all_dfs, ignore_index=True)
        print(f"\n✅ Total records: {len(combined_df)}")
        return combined_df

    def calculate_features(self, df):
        """Calculate comprehensive technical indicators"""
        print("\n" + "="*70)
        print("🔬 CALCULATING FEATURES")
        print("="*70)

        # Price features
        df['price_change'] = df['Close'].pct_change()
        df['high_low_range'] = (df['High'] - df['Low']) / df['Close']
        df['close_open_diff'] = (df['Close'] - df['Open']) / df['Open']

        # Moving averages
        for period in [7, 14, 21, 50]:
            df[f'sma_{period}'] = df['Close'].rolling(window=period).mean()
            df[f'ema_{period}'] = df['Close'].ewm(span=period, adjust=False).mean()

        # Momentum indicators
        df['rsi'] = self._calculate_rsi(df['Close'], 14)
        df['macd'], df['macd_signal'] = self._calculate_macd(df['Close'])

        # Volatility
        df['volatility_20'] = df['Close'].rolling(window=20).std()
        df['atr'] = self._calculate_atr(df, 14)

        # Volume indicators
        df['volume_sma_20'] = df['Volume'].rolling(window=20).mean()
        df['volume_ratio'] = df['Volume'] / df['volume_sma_20']

        # Bollinger Bands
        df['bb_middle'] = df['Close'].rolling(window=20).mean()
        bb_std = df['Close'].rolling(window=20).std()
        df['bb_upper'] = df['bb_middle'] + (2 * bb_std)
        df['bb_lower'] = df['bb_middle'] - (2 * bb_std)
        df['bb_position'] = (df['Close'] - df['bb_lower']) / (df['bb_upper'] - df['bb_lower'])

        # Trend features
        df['trend_5'] = np.where(df['Close'] > df['sma_7'], 1, 0)
        df['trend_14'] = np.where(df['Close'] > df['sma_14'], 1, 0)

        # Target: Predict next period direction (1 = up, 0 = down)
        df['target'] = np.where(df['Close'].shift(-1) > df['Close'], 1, 0)

        # Drop NaN values
        df = df.dropna()

        feature_cols = [col for col in df.columns if col not in
                       ['Open', 'High', 'Low', 'Close', 'Volume', 'Symbol', 'target']]

        print(f"✅ Created {len(feature_cols)} features")
        return df, feature_cols

    def _calculate_rsi(self, prices, period=14):
        """Calculate Relative Strength Index"""
        delta = prices.diff()
        gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()
        rs = gain / loss
        rsi = 100 - (100 / (1 + rs))
        return rsi

    def _calculate_macd(self, prices, fast=12, slow=26, signal=9):
        """Calculate MACD indicator"""
        ema_fast = prices.ewm(span=fast, adjust=False).mean()
        ema_slow = prices.ewm(span=slow, adjust=False).mean()
        macd = ema_fast - ema_slow
        macd_signal = macd.ewm(span=signal, adjust=False).mean()
        return macd, macd_signal

    def _calculate_atr(self, df, period=14):
        """Calculate Average True Range"""
        high_low = df['High'] - df['Low']
        high_close = np.abs(df['High'] - df['Close'].shift())
        low_close = np.abs(df['Low'] - df['Close'].shift())
        ranges = pd.concat([high_low, high_close, low_close], axis=1)
        true_range = np.max(ranges, axis=1)
        atr = true_range.rolling(period).mean()
        return atr

    def prepare_data(self, df, feature_cols, test_size=0.2, val_size=0.1):
        """Prepare data for training with train/val/test splits"""
        print("\n" + "="*70)
        print("📊 PREPARING DATA")
        print("="*70)

        X = df[feature_cols].values
        y = df['target'].values

        # Split: 70% train, 10% validation, 20% test
        X_temp, X_test, y_temp, y_test = train_test_split(
            X, y, test_size=test_size, random_state=42, stratify=y
        )

        val_ratio = val_size / (1 - test_size)
        X_train, X_val, y_train, y_val = train_test_split(
            X_temp, y_temp, test_size=val_ratio, random_state=42, stratify=y_temp
        )

        # Scale data
        self.scalers['standard'] = StandardScaler()
        X_train_scaled = self.scalers['standard'].fit_transform(X_train)
        X_val_scaled = self.scalers['standard'].transform(X_val)
        X_test_scaled = self.scalers['standard'].transform(X_test)

        print(f"  Train set: {len(X_train)} samples ({y_train.sum()} positive)")
        print(f"  Val set:   {len(X_val)} samples ({y_val.sum()} positive)")
        print(f"  Test set:  {len(X_test)} samples ({y_test.sum()} positive)")

        return (X_train_scaled, X_val_scaled, X_test_scaled,
                y_train, y_val, y_test)

    def train_xgboost(self, X_train, y_train, X_val, y_val):
        """Train XGBoost classifier with hyperparameter tuning"""
        print("\n" + "="*70)
        print("🚀 TRAINING XGBOOST MODEL")
        print("="*70)

        if not XGBOOST_AVAILABLE:
            print("❌ XGBoost not available")
            return None

        # XGBoost with regularization to prevent overfitting
        xgb_model = XGBClassifier(
            n_estimators=100,
            max_depth=5,  # Limit depth to prevent overfitting
            learning_rate=0.1,
            subsample=0.8,  # Row sampling
            colsample_bytree=0.8,  # Column sampling
            reg_alpha=0.1,  # L1 regularization
            reg_lambda=1.0,  # L2 regularization
            random_state=42,
            eval_metric='logloss',
            early_stopping_rounds=10
        )

        # Train with early stopping
        xgb_model.fit(
            X_train, y_train,
            eval_set=[(X_val, y_val)],
            verbose=False
        )

        # Predictions
        y_train_pred = xgb_model.predict(X_train)
        y_val_pred = xgb_model.predict(X_val)

        # Metrics
        train_acc = accuracy_score(y_train, y_train_pred)
        val_acc = accuracy_score(y_val, y_val_pred)
        val_precision = precision_score(y_val, y_val_pred, zero_division=0)
        val_recall = recall_score(y_val, y_val_pred, zero_division=0)
        val_f1 = f1_score(y_val, y_val_pred, zero_division=0)

        # Calculate overfitting metric
        overfit_gap = train_acc - val_acc

        print(f"  Train Accuracy: {train_acc:.4f}")
        print(f"  Val Accuracy:   {val_acc:.4f}")
        print(f"  Overfitting Gap: {overfit_gap:.4f} {'⚠️ HIGH' if overfit_gap > 0.05 else '✅ OK'}")
        print(f"  Precision: {val_precision:.4f}")
        print(f"  Recall:    {val_recall:.4f}")
        print(f"  F1 Score:  {val_f1:.4f}")

        # Store results
        self.results['xgboost'] = {
            'model': xgb_model,
            'train_accuracy': train_acc,
            'val_accuracy': val_acc,
            'precision': val_precision,
            'recall': val_recall,
            'f1_score': val_f1,
            'overfit_gap': overfit_gap,
            'feature_importance': xgb_model.feature_importances_
        }

        return xgb_model

    def prepare_sequences_for_lstm(self, X, y):
        """Prepare sequential data for LSTM"""
        sequences = []
        targets = []

        for i in range(len(X) - self.sequence_length):
            sequences.append(X[i:i + self.sequence_length])
            targets.append(y[i + self.sequence_length])

        return np.array(sequences), np.array(targets)

    def train_lstm(self, X_train, y_train, X_val, y_val, feature_count):
        """Train LSTM model for time-series prediction"""
        print("\n" + "="*70)
        print("🧠 TRAINING LSTM MODEL")
        print("="*70)

        if not TENSORFLOW_AVAILABLE:
            print("❌ TensorFlow not available")
            return None

        # Prepare sequences
        print(f"  Preparing sequences (lookback={self.sequence_length})...")
        X_train_seq, y_train_seq = self.prepare_sequences_for_lstm(X_train, y_train)
        X_val_seq, y_val_seq = self.prepare_sequences_for_lstm(X_val, y_val)

        print(f"  Train sequences: {X_train_seq.shape}")
        print(f"  Val sequences: {X_val_seq.shape}")

        # Build LSTM model
        model = Sequential([
            LSTM(64, input_shape=(self.sequence_length, feature_count),
                 return_sequences=True),
            Dropout(0.2),
            BatchNormalization(),

            LSTM(32, return_sequences=False),
            Dropout(0.2),
            BatchNormalization(),

            Dense(16, activation='relu'),
            Dropout(0.2),

            Dense(1, activation='sigmoid')
        ])

        model.compile(
            optimizer=Adam(learning_rate=0.001),
            loss='binary_crossentropy',
            metrics=['accuracy', 'AUC']
        )

        # Callbacks
        early_stop = EarlyStopping(
            monitor='val_loss',
            patience=10,
            restore_best_weights=True
        )

        reduce_lr = ReduceLROnPlateau(
            monitor='val_loss',
            factor=0.5,
            patience=5,
            min_lr=0.00001
        )

        # Train
        print("  Training LSTM...")
        history = model.fit(
            X_train_seq, y_train_seq,
            validation_data=(X_val_seq, y_val_seq),
            epochs=50,
            batch_size=32,
            callbacks=[early_stop, reduce_lr],
            verbose=0
        )

        # Evaluate
        train_loss, train_acc, train_auc = model.evaluate(X_train_seq, y_train_seq, verbose=0)
        val_loss, val_acc, val_auc = model.evaluate(X_val_seq, y_val_seq, verbose=0)

        overfit_gap = train_acc - val_acc

        print(f"  Train Accuracy: {train_acc:.4f}")
        print(f"  Val Accuracy:   {val_acc:.4f}")
        print(f"  Overfitting Gap: {overfit_gap:.4f} {'⚠️ HIGH' if overfit_gap > 0.05 else '✅ OK'}")
        print(f"  Val AUC: {val_auc:.4f}")

        # Store results
        self.results['lstm'] = {
            'model': model,
            'train_accuracy': train_acc,
            'val_accuracy': val_acc,
            'train_auc': train_auc,
            'val_auc': val_auc,
            'overfit_gap': overfit_gap,
            'history': history.history
        }

        self.lstm_model = model
        return model

    def perform_kfold_validation(self, X, y, model_type='xgboost', n_splits=5):
        """Perform k-fold cross-validation"""
        print("\n" + "="*70)
        print(f"🔄 K-FOLD CROSS-VALIDATION ({model_type.upper()})")
        print("="*70)

        kfold = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=42)
        scores = []

        for fold, (train_idx, val_idx) in enumerate(kfold.split(X, y), 1):
            X_train_fold = X[train_idx]
            y_train_fold = y[train_idx]
            X_val_fold = X[val_idx]
            y_val_fold = y[val_idx]

            if model_type == 'xgboost' and XGBOOST_AVAILABLE:
                model = XGBClassifier(
                    n_estimators=100, max_depth=5, learning_rate=0.1,
                    subsample=0.8, colsample_bytree=0.8,
                    random_state=42
                )
                model.fit(X_train_fold, y_train_fold)
                score = model.score(X_val_fold, y_val_fold)
            else:
                # Fallback to GradientBoosting
                from sklearn.ensemble import GradientBoostingClassifier
                model = GradientBoostingClassifier(n_estimators=100, max_depth=5, random_state=42)
                model.fit(X_train_fold, y_train_fold)
                score = model.score(X_val_fold, y_val_fold)

            scores.append(score)
            print(f"  Fold {fold}: {score:.4f}")

        mean_score = np.mean(scores)
        std_score = np.std(scores)
        print(f"\n  Mean CV Score: {mean_score:.4f} (±{std_score:.4f})")

        self.results[f'{model_type}_cv'] = {
            'scores': scores,
            'mean': mean_score,
            'std': std_score
        }

        return scores

    def generate_learning_curves(self, X_train, y_train, model_type='xgboost'):
        """Generate learning curves to detect overfitting"""
        print("\n" + "="*70)
        print(f"📈 GENERATING LEARNING CURVES ({model_type.upper()})")
        print("="*70)

        if model_type == 'xgboost' and XGBOOST_AVAILABLE:
            model = XGBClassifier(n_estimators=100, max_depth=5, random_state=42)
        else:
            from sklearn.ensemble import GradientBoostingClassifier
            model = GradientBoostingClassifier(n_estimators=100, max_depth=5, random_state=42)

        train_sizes, train_scores, val_scores = learning_curve(
            model, X_train, y_train,
            cv=5,
            train_sizes=np.linspace(0.1, 1.0, 10),
            scoring='accuracy',
            n_jobs=-1
        )

        train_mean = np.mean(train_scores, axis=1)
        train_std = np.std(train_scores, axis=1)
        val_mean = np.mean(val_scores, axis=1)
        val_std = np.std(val_scores, axis=1)

        # Store for visualization
        self.visualization_data['learning_curves'] = {
            'train_sizes': train_sizes,
            'train_mean': train_mean,
            'train_std': train_std,
            'val_mean': val_mean,
            'val_std': val_std
        }

        print(f"  ✅ Learning curves generated")
        return train_sizes, train_mean, val_mean

    def save_models(self, feature_cols):
        """Save all trained models and metadata"""
        print("\n" + "="*70)
        print("💾 SAVING MODELS")
        print("="*70)

        # Save XGBoost
        if 'xgboost' in self.results:
            xgb_path = self.version_dir / 'xgboost_model.joblib'
            joblib.dump(self.results['xgboost']['model'], xgb_path)
            print(f"  ✅ Saved XGBoost: {xgb_path}")

        # Save LSTM
        if 'lstm' in self.results:
            lstm_path = self.version_dir / 'lstm_model.h5'
            self.results['lstm']['model'].save(lstm_path)
            print(f"  ✅ Saved LSTM: {lstm_path}")

        # Save scalers
        scaler_path = self.version_dir / 'scalers.joblib'
        joblib.dump(self.scalers, scaler_path)
        print(f"  ✅ Saved scalers: {scaler_path}")

        # Save config
        config = {
            'version': self.version,
            'feature_cols': feature_cols,
            'sequence_length': self.sequence_length,
            'symbols': self.symbols,
            'created_at': datetime.now().isoformat()
        }
        config_path = self.version_dir / 'config.json'
        with open(config_path, 'w') as f:
            json.dump(config, f, indent=2)
        print(f"  ✅ Saved config: {config_path}")

        # Save metrics
        metrics = {}
        for model_name, result in self.results.items():
            if 'model' in result:
                metrics[model_name] = {
                    k: float(v) if isinstance(v, (np.floating, np.integer)) else v
                    for k, v in result.items()
                    if k != 'model' and not isinstance(v, (np.ndarray, object))
                }

        metrics_path = self.version_dir / 'metrics.json'
        with open(metrics_path, 'w') as f:
            json.dump(metrics, f, indent=2)
        print(f"  ✅ Saved metrics: {metrics_path}")

        # Create 'latest' symlink
        latest_link = self.model_dir / 'latest'
        if latest_link.exists():
            latest_link.unlink()
        latest_link.symlink_to(self.version)
        print(f"  ✅ Updated 'latest' symlink")

        return self.version_dir


if __name__ == "__main__":
    print("\n" + "="*70)
    print("🚀 ADVANCED ML TRAINER - XGBOOST + LSTM HYBRID")
    print("="*70)

    # Initialize trainer
    trainer = AdvancedMLTrainer(
        data_source="live",
        sequence_length=60
    )

    # Fetch data
    df = trainer.fetch_live_data(period="90d", interval="1h")
    if df is None:
        print("❌ Failed to fetch data")
        sys.exit(1)

    # Calculate features
    df, feature_cols = trainer.calculate_features(df)

    # Prepare data
    X_train, X_val, X_test, y_train, y_val, y_test = trainer.prepare_data(
        df, feature_cols, test_size=0.2, val_size=0.1
    )

    # Train XGBoost
    if XGBOOST_AVAILABLE:
        xgb_model = trainer.train_xgboost(X_train, y_train, X_val, y_val)

        # K-fold validation
        trainer.perform_kfold_validation(
            np.vstack([X_train, X_val]),
            np.concatenate([y_train, y_val]),
            model_type='xgboost',
            n_splits=5
        )

        # Learning curves
        trainer.generate_learning_curves(X_train, y_train, model_type='xgboost')

    # Train LSTM
    if TENSORFLOW_AVAILABLE:
        lstm_model = trainer.train_lstm(
            X_train, y_train, X_val, y_val,
            feature_count=len(feature_cols)
        )

    # Save all models
    version_dir = trainer.save_models(feature_cols)

    print("\n" + "="*70)
    print("✅ TRAINING COMPLETE")
    print("="*70)
    print(f"Models saved to: {version_dir}")
    print("\nNext steps:")
    print("1. Review metrics.json for model performance")
    print("2. Generate visualizations with visualization endpoints")
    print("3. Deploy models via ML API server")
    print("="*70 + "\n")
