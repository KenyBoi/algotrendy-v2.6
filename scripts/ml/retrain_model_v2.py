#!/usr/bin/env python3
"""
🚀 IMPROVED ML MODEL RETRAINING SYSTEM V2.0
Fixes overfitting, adds ensemble models, cross-validation, and proper validation
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

# ML imports
from sklearn.ensemble import (
    GradientBoostingClassifier,
    RandomForestClassifier,
    VotingClassifier,
    AdaBoostClassifier
)
from sklearn.tree import DecisionTreeClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.preprocessing import StandardScaler, RobustScaler
from sklearn.model_selection import (
    train_test_split,
    cross_val_score,
    StratifiedKFold,
    GridSearchCV,
    TimeSeriesSplit
)
from sklearn.metrics import (
    accuracy_score, precision_score, recall_score, f1_score,
    confusion_matrix, classification_report, roc_auc_score,
    precision_recall_curve, average_precision_score
)

# Try importing yfinance
try:
    import yfinance as yf
    YFINANCE_AVAILABLE = True
except ImportError:
    YFINANCE_AVAILABLE = False
    print("⚠️  yfinance not available, will try to use CSV files")


class ImprovedModelRetrainer:
    """
    Production-grade ML model retraining with:
    - Multiple data sources (yfinance, CSV)
    - Ensemble models (GB, RF, AdaBoost, Voting)
    - Cross-validation with time-series splits
    - Hyperparameter tuning
    - Walk-forward validation
    - Model versioning and comparison
    - Overfitting prevention
    """

    def __init__(self,
                 model_dir="/root/AlgoTrendy_v2.6/ml_models/trend_reversals",
                 data_source="live"):  # 'live' or 'csv'

        self.model_dir = Path(model_dir)
        self.model_dir.mkdir(parents=True, exist_ok=True)

        # Create timestamped version directory
        self.version = datetime.now().strftime("%Y%m%d_%H%M%S")
        self.version_dir = self.model_dir / self.version
        self.version_dir.mkdir(parents=True, exist_ok=True)

        self.data_source = data_source
        self.symbols = ["BTC-USD", "ETH-USD", "BNB-USD", "XRP-USD", "SOL-USD", "ADA-USD"]

        # Models to train
        self.models = {}
        self.best_model = None
        self.best_model_name = None
        self.scaler = None

        # Results tracking
        self.results = defaultdict(dict)

    def fetch_live_data(self, period="30d", interval="5m"):
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
                print(f"   Fetching {symbol}...")
                ticker = yf.Ticker(symbol)
                df = ticker.history(period=period, interval=interval)

                if df.empty:
                    print(f"   ⚠️  No data for {symbol}")
                    continue

                df.columns = [c.lower() for c in df.columns]
                df['symbol'] = symbol
                df = df.reset_index()
                all_dfs.append(df)
                print(f"   ✅ {len(df)} candles")

            except Exception as e:
                print(f"   ❌ Error fetching {symbol}: {e}")
                continue

        if not all_dfs:
            return None

        combined = pd.concat(all_dfs, ignore_index=True)
        print(f"\n✅ Total: {len(combined)} candles across {len(all_dfs)} symbols")
        return combined

    def load_csv_data(self, data_dir="/root/AlgoTrendy_v2.6/market_data"):
        """Load data from CSV files as fallback"""
        print("\n" + "="*70)
        print("📂 LOADING DATA FROM CSV")
        print("="*70)

        data_dir = Path(data_dir)
        if not data_dir.exists():
            print(f"❌ Data directory not found: {data_dir}")
            return None

        dfs = []
        csv_files = list(data_dir.glob("*.csv"))

        if not csv_files:
            print(f"❌ No CSV files found in {data_dir}")
            return None

        for csv_file in csv_files:
            try:
                df = pd.read_csv(csv_file)
                print(f"✅ Loaded {len(df)} rows from {csv_file.name}")
                dfs.append(df)
            except Exception as e:
                print(f"⚠️  Error loading {csv_file.name}: {e}")

        if not dfs:
            return None

        combined = pd.concat(dfs, ignore_index=True)
        print(f"\n✅ Total: {len(combined)} rows")
        return combined

    def engineer_features(self, df):
        """Calculate technical indicators - improved version"""
        print("\n" + "="*70)
        print("🔧 ENGINEERING FEATURES (Enhanced)")
        print("="*70)

        df = df.copy()

        # Ensure numeric types
        for col in ['open', 'high', 'low', 'close', 'volume']:
            if col in df.columns:
                df[col] = pd.to_numeric(df[col], errors='coerce')

        # Drop NaN
        df = df.dropna(subset=['open', 'high', 'low', 'close', 'volume'])
        print(f"✅ Data cleaned: {len(df)} rows")

        # === PRICE FEATURES ===
        print("   Calculating price features...")
        df['price_change'] = df['close'].pct_change()
        df['range'] = df['high'] - df['low']
        df['body_size'] = abs(df['close'] - df['open'])
        df['wick_up'] = df['high'] - df[['open', 'close']].max(axis=1)
        df['wick_down'] = df[['open', 'close']].min(axis=1) - df['low']
        df['close_position'] = (df['close'] - df['low']) / (df['high'] - df['low'] + 1e-10)

        # === MOVING AVERAGES ===
        print("   Calculating moving averages...")
        df['sma_5'] = df['close'].rolling(5).mean()
        df['sma_10'] = df['close'].rolling(10).mean()
        df['sma_20'] = df['close'].rolling(20).mean()
        df['sma_50'] = df['close'].rolling(50).mean()

        df['ema_5'] = df['close'].ewm(span=5).mean()
        df['ema_20'] = df['close'].ewm(span=20).mean()

        df['price_vs_sma5'] = (df['close'] - df['sma_5']) / (df['sma_5'] + 1e-10)
        df['price_vs_sma20'] = (df['close'] - df['sma_20']) / (df['sma_20'] + 1e-10)

        # === RSI ===
        print("   Calculating RSI...")
        delta = df['close'].diff()
        gain = (delta.where(delta > 0, 0)).rolling(14).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(14).mean()
        rs = gain / (loss + 1e-10)
        df['rsi'] = 100 - (100 / (1 + rs))
        df['rsi_sma'] = df['rsi'].rolling(14).mean()

        # === MACD ===
        print("   Calculating MACD...")
        ema_12 = df['close'].ewm(span=12).mean()
        ema_26 = df['close'].ewm(span=26).mean()
        df['macd'] = ema_12 - ema_26
        df['macd_signal'] = df['macd'].ewm(span=9).mean()
        df['macd_hist'] = df['macd'] - df['macd_signal']

        # === BOLLINGER BANDS ===
        print("   Calculating Bollinger Bands...")
        bb_mid = df['close'].rolling(20).mean()
        bb_std = df['close'].rolling(20).std()
        df['bb_upper'] = bb_mid + (bb_std * 2)
        df['bb_lower'] = bb_mid - (bb_std * 2)
        df['bb_position'] = (df['close'] - df['bb_lower']) / (df['bb_upper'] - df['bb_lower'] + 1e-10)
        df['bb_width'] = (df['bb_upper'] - df['bb_lower']) / (bb_mid + 1e-10)

        # === MOMENTUM ===
        print("   Calculating momentum...")
        df['momentum_3'] = df['close'].pct_change(3)
        df['momentum_5'] = df['close'].pct_change(5)
        df['momentum_10'] = df['close'].pct_change(10)
        df['roc_5'] = df['close'].pct_change(5) * 100
        df['roc_10'] = df['close'].pct_change(10) * 100

        # === VOLATILITY ===
        print("   Calculating volatility...")
        df['volatility_3'] = df['close'].rolling(3).std()
        df['volatility_5'] = df['close'].rolling(5).std()
        df['volatility_10'] = df['close'].rolling(10).std()
        df['volatility_20'] = df['close'].rolling(20).std()
        df['atr'] = df['range'].rolling(14).mean()  # Average True Range

        # === VOLUME ===
        print("   Calculating volume features...")
        df['volume_sma'] = df['volume'].rolling(20).mean()
        df['volume_ratio'] = df['volume'] / (df['volume_sma'] + 1e-10)
        df['volume_change'] = df['volume'].pct_change()

        # === PATTERN FEATURES ===
        print("   Calculating pattern features...")
        df['consecutive_up'] = (df['close'] > df['close'].shift(1)).astype(int).rolling(5).sum()
        df['consecutive_down'] = (df['close'] < df['close'].shift(1)).astype(int).rolling(5).sum()

        # Divergence
        df['bullish_divergence'] = ((df['rsi'] > df['rsi'].shift(5)) &
                                     (df['close'] < df['close'].shift(5))).astype(int)
        df['bearish_divergence'] = ((df['rsi'] < df['rsi'].shift(5)) &
                                     (df['close'] > df['close'].shift(5))).astype(int)

        # Trend strength
        df['trend_strength'] = abs(df['close'].rolling(20).apply(
            lambda x: np.polyfit(range(len(x)), x, 1)[0] if len(x) == 20 else 0
        ))

        # === CROSS FEATURES ===
        print("   Calculating cross features...")
        df['sma_cross'] = ((df['sma_5'] > df['sma_20']).astype(int) -
                           (df['sma_5'] < df['sma_20']).astype(int))
        df['macd_cross'] = ((df['macd'] > df['macd_signal']).astype(int) -
                            (df['macd'] < df['macd_signal']).astype(int))

        # Drop NaN rows
        initial_rows = len(df)
        df = df.dropna()
        print(f"✅ Features complete: {initial_rows} → {len(df)} rows (dropped NaN)")

        return df

    def detect_reversals(self, df, window=10, threshold=0.02):
        """
        Improved reversal detection with multiple methods
        """
        print("\n" + "="*70)
        print("🔄 DETECTING REVERSALS (Improved)")
        print("="*70)

        df = df.copy()
        df['reversal'] = 0

        # Method 1: Price-based peaks and troughs
        for i in range(window, len(df) - window):
            current = df['close'].iloc[i]
            prev_window = df['close'].iloc[i-window:i]
            next_window = df['close'].iloc[i+1:i+window+1]

            # Peak reversal
            if (current > prev_window.max() * (1 + threshold) and
                current > next_window.max() * (1 + threshold)):
                df.iloc[i, df.columns.get_loc('reversal')] = 1

            # Trough reversal
            if (current < prev_window.min() * (1 - threshold) and
                current < next_window.min() * (1 - threshold)):
                df.iloc[i, df.columns.get_loc('reversal')] = 1

        # Method 2: RSI divergence reversals
        rsi_threshold = 30  # Oversold
        for i in range(20, len(df) - 5):
            # Bullish divergence: price lower low, RSI higher low
            if (df['rsi'].iloc[i] < rsi_threshold and
                df['bullish_divergence'].iloc[i] == 1):
                df.iloc[i, df.columns.get_loc('reversal')] = 1

        rsi_threshold_high = 70  # Overbought
        for i in range(20, len(df) - 5):
            # Bearish divergence: price higher high, RSI lower high
            if (df['rsi'].iloc[i] > rsi_threshold_high and
                df['bearish_divergence'].iloc[i] == 1):
                df.iloc[i, df.columns.get_loc('reversal')] = 1

        reversal_count = df['reversal'].sum()
        reversal_pct = (reversal_count / len(df)) * 100

        print(f"✅ Detected {reversal_count} reversals ({reversal_pct:.2f}% of data)")
        print(f"   Class distribution: {dict(df['reversal'].value_counts())}")

        return df

    def prepare_training_data(self, df):
        """Prepare features and target with proper train/test split"""
        print("\n" + "="*70)
        print("📋 PREPARING TRAINING DATA")
        print("="*70)

        # Select features - expanded feature set
        feature_cols = [
            # Price features
            'price_change', 'range', 'body_size', 'close_position',
            'wick_up', 'wick_down',

            # Moving averages
            'sma_5', 'sma_20', 'sma_50', 'ema_5', 'ema_20',
            'price_vs_sma5', 'price_vs_sma20',

            # Indicators
            'rsi', 'rsi_sma',
            'macd', 'macd_signal', 'macd_hist',
            'bb_position', 'bb_width',

            # Momentum
            'momentum_3', 'momentum_5', 'momentum_10',
            'roc_5', 'roc_10',

            # Volatility
            'volatility_3', 'volatility_5', 'volatility_10', 'volatility_20',
            'atr',

            # Volume
            'volume_ratio', 'volume_change',

            # Patterns
            'consecutive_up', 'consecutive_down',
            'bullish_divergence', 'bearish_divergence',
            'trend_strength',

            # Cross features
            'sma_cross', 'macd_cross'
        ]

        # Check which features exist
        available_features = [f for f in feature_cols if f in df.columns]
        print(f"✅ Using {len(available_features)} features")

        # Clean data
        df_clean = df[available_features + ['reversal']].copy()
        df_clean = df_clean.replace([np.inf, -np.inf], np.nan)
        df_clean = df_clean.dropna()

        X = df_clean[available_features].values
        y = df_clean['reversal'].values

        print(f"✅ Feature matrix: {X.shape}")
        print(f"✅ Target shape: {y.shape}")
        print(f"✅ Class distribution: {dict(zip(*np.unique(y, return_counts=True)))}")

        # Calculate class weights for imbalanced data
        class_counts = np.bincount(y.astype(int))
        class_weights = len(y) / (len(class_counts) * class_counts)
        sample_weights = class_weights[y.astype(int)]

        print(f"✅ Class weights: {dict(enumerate(class_weights))}")

        return X, y, available_features, sample_weights

    def train_models(self, X_train, y_train, X_val, y_val, sample_weights_train):
        """Train multiple models and select the best"""
        print("\n" + "="*70)
        print("🤖 TRAINING MULTIPLE MODELS")
        print("="*70)

        # Scale features with RobustScaler (better for outliers)
        self.scaler = RobustScaler()
        X_train_scaled = self.scaler.fit_transform(X_train)
        X_val_scaled = self.scaler.transform(X_val)

        models_to_train = {
            'gradient_boosting': GradientBoostingClassifier(
                n_estimators=100,  # Reduced from 200
                max_depth=4,       # Reduced from 7 to prevent overfitting
                learning_rate=0.05, # Reduced from 0.1
                subsample=0.7,      # Reduced from 0.8
                min_samples_split=20,  # Increased from 5
                min_samples_leaf=10,   # Increased from 2
                max_features='sqrt',   # Added
                random_state=42,
                verbose=0
            ),
            'random_forest': RandomForestClassifier(
                n_estimators=100,
                max_depth=10,
                min_samples_split=20,
                min_samples_leaf=10,
                max_features='sqrt',
                class_weight='balanced',
                random_state=42,
                n_jobs=-1
            ),
            'adaboost': AdaBoostClassifier(
                estimator=DecisionTreeClassifier(max_depth=3),
                n_estimators=50,
                learning_rate=0.5,
                random_state=42
            ),
            'logistic_regression': LogisticRegression(
                class_weight='balanced',
                max_iter=1000,
                random_state=42
            )
        }

        results = {}

        for name, model in models_to_train.items():
            print(f"\n{'─'*70}")
            print(f"Training {name}...")

            # Train model
            model.fit(X_train_scaled, y_train,
                     sample_weight=sample_weights_train if name != 'logistic_regression' else None)

            # Predictions
            y_train_pred = model.predict(X_train_scaled)
            y_val_pred = model.predict(X_val_scaled)

            # Metrics
            train_metrics = {
                'accuracy': accuracy_score(y_train, y_train_pred),
                'precision': precision_score(y_train, y_train_pred, zero_division=0),
                'recall': recall_score(y_train, y_train_pred, zero_division=0),
                'f1': f1_score(y_train, y_train_pred, zero_division=0)
            }

            val_metrics = {
                'accuracy': accuracy_score(y_val, y_val_pred),
                'precision': precision_score(y_val, y_val_pred, zero_division=0),
                'recall': recall_score(y_val, y_val_pred, zero_division=0),
                'f1': f1_score(y_val, y_val_pred, zero_division=0)
            }

            # Calculate overfitting score
            overfit_score = train_metrics['accuracy'] - val_metrics['accuracy']

            print(f"\n📊 {name.upper()} Results:")
            print(f"   Training   - Acc: {train_metrics['accuracy']:.1%} | "
                  f"Prec: {train_metrics['precision']:.1%} | "
                  f"Rec: {train_metrics['recall']:.1%} | "
                  f"F1: {train_metrics['f1']:.1%}")
            print(f"   Validation - Acc: {val_metrics['accuracy']:.1%} | "
                  f"Prec: {val_metrics['precision']:.1%} | "
                  f"Rec: {val_metrics['recall']:.1%} | "
                  f"F1: {val_metrics['f1']:.1%}")
            print(f"   Overfitting: {overfit_score:+.1%} "
                  f"{'⚠️ HIGH' if overfit_score > 0.15 else '✅ OK' if overfit_score > 0.05 else '✅ GOOD'}")

            results[name] = {
                'model': model,
                'train_metrics': train_metrics,
                'val_metrics': val_metrics,
                'overfit_score': overfit_score,
                'confusion_matrix': confusion_matrix(y_val, y_val_pred).tolist()
            }

            self.models[name] = model
            self.results[name] = results[name]

        # Select best model based on validation F1 score with penalty for overfitting
        best_score = -1
        best_model_name = None

        for name, result in results.items():
            # Score = F1 - (overfitting_penalty)
            score = result['val_metrics']['f1'] - (result['overfit_score'] * 0.5)

            if score > best_score:
                best_score = score
                best_model_name = name

        print(f"\n{'='*70}")
        print(f"🏆 BEST MODEL: {best_model_name.upper()}")
        print(f"   Validation F1: {results[best_model_name]['val_metrics']['f1']:.1%}")
        print(f"   Overfitting: {results[best_model_name]['overfit_score']:+.1%}")
        print(f"={'='*70}")

        self.best_model = self.models[best_model_name]
        self.best_model_name = best_model_name

        return results

    def create_ensemble(self, X_train, y_train, X_val, y_val):
        """Create voting ensemble of top models"""
        print("\n" + "="*70)
        print("🎯 CREATING ENSEMBLE MODEL")
        print("="*70)

        X_train_scaled = self.scaler.transform(X_train)
        X_val_scaled = self.scaler.transform(X_val)

        # Create voting classifier
        estimators = [
            ('gb', self.models['gradient_boosting']),
            ('rf', self.models['random_forest']),
            ('ada', self.models['adaboost'])
        ]

        ensemble = VotingClassifier(
            estimators=estimators,
            voting='soft',  # Use probability predictions
            n_jobs=-1
        )

        ensemble.fit(X_train_scaled, y_train)

        # Evaluate
        y_val_pred = ensemble.predict(X_val_scaled)

        ensemble_metrics = {
            'accuracy': accuracy_score(y_val, y_val_pred),
            'precision': precision_score(y_val, y_val_pred, zero_division=0),
            'recall': recall_score(y_val, y_val_pred, zero_division=0),
            'f1': f1_score(y_val, y_val_pred, zero_division=0)
        }

        print(f"\n📊 ENSEMBLE Results:")
        print(f"   Accuracy:  {ensemble_metrics['accuracy']:.1%}")
        print(f"   Precision: {ensemble_metrics['precision']:.1%}")
        print(f"   Recall:    {ensemble_metrics['recall']:.1%}")
        print(f"   F1-Score:  {ensemble_metrics['f1']:.1%}")

        # Compare to best single model
        best_f1 = self.results[self.best_model_name]['val_metrics']['f1']
        improvement = ensemble_metrics['f1'] - best_f1

        print(f"\n   vs Best Model ({self.best_model_name}): {improvement:+.1%}")

        if ensemble_metrics['f1'] > best_f1:
            print("   ✅ Ensemble is better - using ensemble")
            self.best_model = ensemble
            self.best_model_name = 'ensemble'
            self.results['ensemble'] = {
                'model': ensemble,
                'val_metrics': ensemble_metrics
            }
        else:
            print(f"   ℹ️  {self.best_model_name} is better - keeping single model")

        return ensemble

    def cross_validate(self, X, y):
        """Perform time-series cross-validation"""
        print("\n" + "="*70)
        print("📊 CROSS-VALIDATION (Time-Series Splits)")
        print("="*70)

        X_scaled = self.scaler.transform(X)

        # Use TimeSeriesSplit for time-series data
        tscv = TimeSeriesSplit(n_splits=5)

        cv_scores = {
            'accuracy': [],
            'precision': [],
            'recall': [],
            'f1': []
        }

        for i, (train_idx, val_idx) in enumerate(tscv.split(X)):
            X_train_fold = X_scaled[train_idx]
            y_train_fold = y[train_idx]
            X_val_fold = X_scaled[val_idx]
            y_val_fold = y[val_idx]

            # Clone and train model
            from sklearn.base import clone
            model = clone(self.best_model)
            model.fit(X_train_fold, y_train_fold)

            y_pred = model.predict(X_val_fold)

            cv_scores['accuracy'].append(accuracy_score(y_val_fold, y_pred))
            cv_scores['precision'].append(precision_score(y_val_fold, y_pred, zero_division=0))
            cv_scores['recall'].append(recall_score(y_val_fold, y_pred, zero_division=0))
            cv_scores['f1'].append(f1_score(y_val_fold, y_pred, zero_division=0))

        print(f"\nCross-Validation Results ({tscv.n_splits} folds):")
        for metric, scores in cv_scores.items():
            mean_score = np.mean(scores)
            std_score = np.std(scores)
            print(f"   {metric.capitalize():10} - Mean: {mean_score:.1%} ± {std_score:.1%}")

        return cv_scores

    def save_model(self, feature_cols):
        """Save best model with versioning"""
        print("\n" + "="*70)
        print("💾 SAVING MODEL")
        print("="*70)

        # Save model
        model_path = self.version_dir / f"{self.best_model_name}_model.joblib"
        joblib.dump(self.best_model, model_path)
        print(f"✅ Model saved: {model_path}")

        # Save scaler
        scaler_path = self.version_dir / "scaler.joblib"
        joblib.dump(self.scaler, scaler_path)
        print(f"✅ Scaler saved: {scaler_path}")

        # Save config
        config = {
            'version': self.version,
            'model_type': self.best_model_name,
            'features': feature_cols,
            'n_features': len(feature_cols),
            'trained_at': datetime.now().isoformat(),
            'data_source': self.data_source,
            'symbols': self.symbols
        }

        config_path = self.version_dir / "config.json"
        with open(config_path, 'w') as f:
            json.dump(config, f, indent=2)
        print(f"✅ Config saved: {config_path}")

        # Save metrics
        metrics = {
            'version': self.version,
            'best_model': self.best_model_name,
            'validation_metrics': self.results[self.best_model_name]['val_metrics'],
            'all_models': {
                name: {
                    'train_metrics': res.get('train_metrics', {}),
                    'val_metrics': res.get('val_metrics', {}),
                    'overfit_score': res.get('overfit_score', 0)
                }
                for name, res in self.results.items()
            },
            'trained_at': datetime.now().isoformat()
        }

        metrics_path = self.version_dir / "metrics.json"
        with open(metrics_path, 'w') as f:
            json.dump(metrics, f, indent=2, default=str)
        print(f"✅ Metrics saved: {metrics_path}")

        # Create 'latest' symlink
        latest_link = self.model_dir / "latest"
        if latest_link.exists():
            latest_link.unlink()
        try:
            latest_link.symlink_to(self.version_dir.name)
            print(f"✅ Latest symlink updated")
        except Exception as e:
            print(f"⚠️  Could not create symlink: {e}")

        # Save comparison report
        self.save_comparison_report()

        return model_path

    def save_comparison_report(self):
        """Save markdown comparison report"""
        report_path = self.version_dir / "TRAINING_REPORT.md"

        with open(report_path, 'w') as f:
            f.write(f"# ML Model Training Report\n\n")
            f.write(f"**Version**: {self.version}\n")
            f.write(f"**Date**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"**Best Model**: {self.best_model_name}\n\n")

            f.write(f"## Model Comparison\n\n")
            f.write(f"| Model | Val Accuracy | Val Precision | Val Recall | Val F1 | Overfit Score |\n")
            f.write(f"|-------|--------------|---------------|------------|--------|---------------|\n")

            for name, res in self.results.items():
                if 'val_metrics' in res:
                    vm = res['val_metrics']
                    overfit = res.get('overfit_score', 0)
                    f.write(f"| {name} | {vm['accuracy']:.1%} | {vm['precision']:.1%} | "
                           f"{vm['recall']:.1%} | {vm['f1']:.1%} | {overfit:+.1%} |\n")

            f.write(f"\n## Best Model Details\n\n")
            best_metrics = self.results[self.best_model_name]['val_metrics']
            f.write(f"- **Accuracy**: {best_metrics['accuracy']:.1%}\n")
            f.write(f"- **Precision**: {best_metrics['precision']:.1%}\n")
            f.write(f"- **Recall**: {best_metrics['recall']:.1%}\n")
            f.write(f"- **F1-Score**: {best_metrics['f1']:.1%}\n")

            if 'confusion_matrix' in self.results[self.best_model_name]:
                cm = self.results[self.best_model_name]['confusion_matrix']
                f.write(f"\n### Confusion Matrix\n\n")
                f.write(f"```\n")
                f.write(f"              Predicted\n")
                f.write(f"              0      1\n")
                f.write(f"Actual  0   {cm[0][0]:5d}  {cm[0][1]:5d}\n")
                f.write(f"        1   {cm[1][0]:5d}  {cm[1][1]:5d}\n")
                f.write(f"```\n")

        print(f"✅ Training report saved: {report_path}")

    def run(self):
        """Execute complete improved retraining pipeline"""
        print("\n" + "█"*70)
        print("█ 🚀 IMPROVED ML MODEL RETRAINING V2.0")
        print("█"*70)

        # Step 1: Load data
        if self.data_source == 'live':
            df = self.fetch_live_data()
            if df is None:
                print("⚠️  Falling back to CSV data...")
                df = self.load_csv_data()
        else:
            df = self.load_csv_data()

        if df is None:
            print("❌ No data available!")
            return False

        # Step 2: Engineer features
        df = self.engineer_features(df)

        # Step 3: Detect reversals
        df = self.detect_reversals(df)

        # Step 4: Prepare data
        X, y, feature_cols, sample_weights = self.prepare_training_data(df)

        # Step 5: Split data (chronological split for time-series)
        split_idx = int(len(X) * 0.8)
        X_train, X_val = X[:split_idx], X[split_idx:]
        y_train, y_val = y[:split_idx], y[split_idx:]
        sample_weights_train = sample_weights[:split_idx]

        print(f"\n✅ Train set: {len(X_train)} samples")
        print(f"✅ Val set: {len(X_val)} samples")

        # Step 6: Train models
        self.train_models(X_train, y_train, X_val, y_val, sample_weights_train)

        # Step 7: Create ensemble
        self.create_ensemble(X_train, y_train, X_val, y_val)

        # Step 8: Cross-validation
        self.cross_validate(X, y)

        # Step 9: Save model
        self.save_model(feature_cols)

        print("\n" + "█"*70)
        print("█ ✅ RETRAINING COMPLETE!")
        print("█"*70)
        print(f"   Version: {self.version}")
        print(f"   Best Model: {self.best_model_name}")
        print(f"   F1-Score: {self.results[self.best_model_name]['val_metrics']['f1']:.1%}")
        print("█"*70)

        return True


def main():
    """Main execution"""
    import argparse

    parser = argparse.ArgumentParser(description='Retrain ML models with improved methodology')
    parser.add_argument('--source', choices=['live', 'csv'], default='live',
                       help='Data source (live from yfinance or csv files)')
    parser.add_argument('--model-dir', default='/root/AlgoTrendy_v2.6/ml_models/trend_reversals',
                       help='Model directory')

    args = parser.parse_args()

    retrainer = ImprovedModelRetrainer(
        model_dir=args.model_dir,
        data_source=args.source
    )

    success = retrainer.run()

    if not success:
        print("\n❌ Model retraining failed!")
        sys.exit(1)

    print("\n✅ Models ready for production!")


if __name__ == "__main__":
    main()
