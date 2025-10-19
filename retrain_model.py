#!/usr/bin/env python3
"""
🤖 RETRAIN MODEL WITH FRESH DATA
Update the trend reversal detection model with latest market data
"""

import os
import sys
import json
import joblib
import pandas as pd
import numpy as np
from pathlib import Path
from datetime import datetime
from sklearn.ensemble import GradientBoostingClassifier
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix

class ModelRetrainer:
    """Retrain the trend reversal model with fresh data"""
    
    def __init__(self, data_dir="/root/algotrendy_v2.5/real_market_data",
                 model_dir="/root/algotrendy_v2.5/ml_models/trend_reversals/latest"):
        self.data_dir = Path(data_dir)
        self.model_dir = Path(model_dir)
        self.model_dir.mkdir(parents=True, exist_ok=True)
        
        self.symbols = ["BTCUSDT", "ETHUSDT", "BNBUSDT", "XRPUSDT"]
        self.model = None
        self.scaler = None
        
    def load_data(self):
        """Load all CSV files into one DataFrame"""
        print("\n" + "="*60)
        print("📂 LOADING DATA")
        print("="*60)
        
        dfs = []
        for symbol in self.symbols:
            # Get the latest file for each symbol
            files = sorted(self.data_dir.glob(f"{symbol}_5m_*.csv"))
            
            if not files:
                print(f"⚠️  No data for {symbol}")
                continue
            
            latest_file = files[-1]
            df = pd.read_csv(latest_file)
            df['symbol'] = symbol
            dfs.append(df)
            print(f"✅ Loaded {len(df)} rows from {latest_file.name}")
        
        if not dfs:
            print("❌ No data files found!")
            return None
        
        combined_df = pd.concat(dfs, ignore_index=True)
        print(f"\n📊 Total rows: {len(combined_df)}")
        return combined_df
    
    def engineer_features(self, df):
        """Create technical indicators and features"""
        print("\n" + "="*60)
        print("🔧 ENGINEERING FEATURES")
        print("="*60)
        
        df = df.copy()
        
        # Convert types
        df['open'] = pd.to_numeric(df['open'], errors='coerce')
        df['high'] = pd.to_numeric(df['high'], errors='coerce')
        df['low'] = pd.to_numeric(df['low'], errors='coerce')
        df['close'] = pd.to_numeric(df['close'], errors='coerce')
        df['volume'] = pd.to_numeric(df['volume'], errors='coerce')
        
        # Drop rows with NaN
        initial_rows = len(df)
        df = df.dropna()
        print(f"✅ Cleaned data: {initial_rows} → {len(df)} rows")
        
        # Technical indicators
        print("🔍 Computing indicators...")
        
        # Simple Moving Averages
        df['sma_5'] = df['close'].rolling(5).mean()
        df['sma_20'] = df['close'].rolling(20).mean()
        df['sma_50'] = df['close'].rolling(50).mean()
        
        # RSI
        delta = df['close'].diff()
        gain = (delta.where(delta > 0, 0)).rolling(14).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(14).mean()
        rs = gain / loss
        df['rsi'] = 100 - (100 / (1 + rs))
        
        # MACD
        ema_12 = df['close'].ewm(span=12).mean()
        ema_26 = df['close'].ewm(span=26).mean()
        df['macd'] = ema_12 - ema_26
        df['macd_signal'] = df['macd'].ewm(span=9).mean()
        df['macd_hist'] = df['macd'] - df['macd_signal']
        
        # Bollinger Bands
        bb_mid = df['close'].rolling(20).mean()
        bb_std = df['close'].rolling(20).std()
        df['bb_upper'] = bb_mid + (bb_std * 2)
        df['bb_lower'] = bb_mid - (bb_std * 2)
        df['bb_position'] = (df['close'] - df['bb_lower']) / (df['bb_upper'] - df['bb_lower'])
        
        # Volume features
        df['volume_sma'] = df['volume'].rolling(20).mean()
        df['volume_ratio'] = df['volume'] / df['volume_sma']
        
        # Price features
        df['hl_range'] = df['high'] - df['low']
        df['close_position'] = (df['close'] - df['low']) / (df['high'] - df['low'])
        df['oc_range'] = abs(df['close'] - df['open']) / (df['high'] - df['low'])
        
        # Drop initial NaN rows from indicators
        df = df.dropna()
        print(f"✅ After indicators: {len(df)} rows")
        
        return df
    
    def detect_reversals(self, df, threshold=0.02):
        """Detect trend reversals (peaks and troughs)"""
        print("\n" + "="*60)
        print("🔄 DETECTING REVERSALS")
        print("="*60)
        
        df = df.copy()
        df['reversal'] = 0
        
        for symbol in df['symbol'].unique():
            mask = df['symbol'] == symbol
            symbol_df = df[mask].copy()
            symbol_indices = df[mask].index
            
            # Find local peaks (price went up then down)
            for i in range(5, len(symbol_df) - 5):
                current = symbol_df['close'].iloc[i]
                prev_5 = symbol_df['close'].iloc[i-5:i].min()
                next_5 = symbol_df['close'].iloc[i+1:i+6].min()
                
                # Peak: current > previous window AND current > next window
                if current > prev_5 * (1 + threshold) and current > next_5 * (1 + threshold):
                    df.loc[symbol_indices[i], 'reversal'] = 1
            
            # Find local troughs (price went down then up)
            for i in range(5, len(symbol_df) - 5):
                current = symbol_df['close'].iloc[i]
                prev_5 = symbol_df['close'].iloc[i-5:i].max()
                next_5 = symbol_df['close'].iloc[i+1:i+6].max()
                
                # Trough: current < previous window AND current < next window
                if current < prev_5 * (1 - threshold) and current < next_5 * (1 - threshold):
                    df.loc[symbol_indices[i], 'reversal'] = 1
        
        reversal_count = df['reversal'].sum()
        reversal_pct = (reversal_count / len(df)) * 100
        
        print(f"✅ Found {reversal_count} reversals ({reversal_pct:.1f}% of data)")
        return df
    
    def prepare_training_data(self, df):
        """Prepare X and y for model training"""
        print("\n" + "="*60)
        print("📋 PREPARING TRAINING DATA")
        print("="*60)
        
        feature_cols = [
            'sma_5', 'sma_20', 'sma_50', 'rsi', 'macd', 'macd_signal', 'macd_hist',
            'bb_position', 'volume_ratio', 'hl_range', 'close_position', 'oc_range'
        ]
        
        # Remove rows with any NaN in features
        df_clean = df[feature_cols + ['reversal']].dropna()
        
        X = df_clean[feature_cols].values
        y = df_clean['reversal'].values
        
        # Replace inf and -inf with 0 or large finite values
        X = np.where(np.isinf(X), 0, X)
        X = np.nan_to_num(X, nan=0, posinf=1e6, neginf=-1e6)
        
        print(f"✅ Feature matrix shape: {X.shape}")
        print(f"✅ Target shape: {y.shape}")
        print(f"✅ Reversal distribution: {np.bincount(y.astype(int))}")
        
        return X, y, feature_cols
    
    def train_model(self, X, y):
        """Train Gradient Boosting model"""
        print("\n" + "="*60)
        print("🤖 TRAINING MODEL")
        print("="*60)
        
        # Scale features
        self.scaler = StandardScaler()
        X_scaled = self.scaler.fit_transform(X)
        
        # Train model
        print("Training Gradient Boosting Classifier...")
        self.model = GradientBoostingClassifier(
            n_estimators=200,
            max_depth=7,
            learning_rate=0.1,
            subsample=0.8,
            min_samples_split=5,
            min_samples_leaf=2,
            random_state=42,
            verbose=0
        )
        
        self.model.fit(X_scaled, y)
        
        # Evaluate
        y_pred = self.model.predict(X_scaled)
        accuracy = accuracy_score(y, y_pred)
        precision = precision_score(y, y_pred, zero_division=0)
        recall = recall_score(y, y_pred, zero_division=0)
        f1 = f1_score(y, y_pred, zero_division=0)
        
        print(f"\n📊 Training Metrics:")
        print(f"   Accuracy:  {accuracy:.2%}")
        print(f"   Precision: {precision:.2%}")
        print(f"   Recall:    {recall:.2%}")
        print(f"   F1-Score:  {f1:.2%}")
        
        return {
            'accuracy': accuracy,
            'precision': precision,
            'recall': recall,
            'f1': f1
        }
    
    def save_model(self, metrics):
        """Save model and scaler"""
        print("\n" + "="*60)
        print("💾 SAVING MODEL")
        print("="*60)
        
        # Save model
        model_path = self.model_dir / "reversal_model.joblib"
        joblib.dump(self.model, model_path)
        print(f"✅ Model saved: {model_path}")
        
        # Save scaler
        scaler_path = self.model_dir / "reversal_scaler.joblib"
        joblib.dump(self.scaler, scaler_path)
        print(f"✅ Scaler saved: {scaler_path}")
        
        # Save metrics
        metrics_path = self.model_dir / "model_metrics.json"
        with open(metrics_path, 'w') as f:
            json.dump({
                **metrics,
                'trained_at': datetime.now().isoformat(),
                'training_rows': len(self.X_train) if hasattr(self, 'X_train') else 0
            }, f, indent=2)
        print(f"✅ Metrics saved: {metrics_path}")
    
    def run(self):
        """Execute full training pipeline"""
        print("\n" + "█"*60)
        print("█ 🤖 TREND REVERSAL MODEL RETRAINING")
        print("█"*60)
        
        # Load data
        df = self.load_data()
        if df is None:
            return False
        
        # Engineer features
        df = self.engineer_features(df)
        
        # Detect reversals
        df = self.detect_reversals(df)
        
        # Prepare training data
        X, y, feature_cols = self.prepare_training_data(df)
        self.X_train = X
        
        # Train model
        metrics = self.train_model(X, y)
        
        # Save model
        self.save_model(metrics)
        
        print("\n" + "█"*60)
        print("█ ✅ RETRAINING COMPLETE!")
        print("█"*60)
        
        return True


def main():
    retrainer = ModelRetrainer()
    success = retrainer.run()
    
    if not success:
        print("\n❌ Model retraining failed!")
        sys.exit(1)
    
    print("\n✅ Model ready for inference!")


if __name__ == "__main__":
    main()
