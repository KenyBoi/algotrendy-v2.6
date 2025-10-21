# ML Research-Backed Enhancements

**Based on**: Latest 2024-2025 Research Papers & Open Source Projects
**Status**: Implementation Roadmap
**Last Updated**: October 20, 2025

---

## Executive Summary

This document outlines cutting-edge enhancements to AlgoTrendy's ML system based on the latest academic research and open-source projects. Current implementation uses GradientBoosting/RandomForest. Research suggests LSTM-Transformer hybrids and advanced feature engineering can improve accuracy by 15-25%.

---

## Research Findings

### 1. State-of-the-Art Models (2024-2025)

#### LSTM-Transformer Hybrid Model ⭐⭐⭐⭐⭐
**Source**: MDPI Computers Journal, January 2025

**Architecture**: LSTM → Modified Transformer → MLP
- LSTM layer: Captures long-term dependencies
- Modified Transformer: Multi-head attention mechanism
- MLP: Final prediction layer

**Performance** (Google stock):
- MAE: 0.642
- RMSE: 0.815
- MSE: 0.664
- ~20% improvement over standalone LSTM

**Recommendation**: HIGH PRIORITY - Implement for next version

---

#### XGBoost-LSTM Hybrid ⭐⭐⭐⭐
**Source**: arXiv 2506.22055v1, 2024

**Method**:
1. XGBoost extracts non-linear feature interactions
2. LSTM captures temporal patterns
3. Ensemble prediction weighted by confidence

**Benefits**:
- Combines tree-based and sequence modeling
- Handles both tabular and time-series features
- More robust than single approach

**Recommendation**: MEDIUM PRIORITY - Consider for ensemble

---

#### Temporal Fusion Transformer (TFT) ⭐⭐⭐⭐⭐
**Source**: Multiple GitHub projects, 2024

**Features**:
- Multi-horizon forecasting
- Variable selection networks
- Interpretable attention mechanisms
- Handles multiple time-series simultaneously

**Use Case**: Perfect for multi-symbol prediction

**Recommendation**: HIGH PRIORITY - Research implementation

---

### 2. Advanced Feature Engineering

#### Order Flow Imbalance (OFI) ⭐⭐⭐⭐⭐
**Source**: arXiv 2408.03594v1, August 2024

**Formula**:
```python
OFI = (bid_volume_change - ask_volume_change) / total_volume

# Advanced: Weighted by price levels
OFI_weighted = sum(
    (bid_volume[i] * (1/i)) - (ask_volume[i] * (1/i))
    for i in range(1, 11)  # Top 10 levels
)
```

**Research Findings**:
- Linear relationship with price changes
- Impact inversely proportional to market depth
- Predictive of short-term price movements

**Implementation**:
```python
def calculate_ofi(df):
    """Calculate Order Flow Imbalance"""
    # Requires order book data (Level 2)
    df['bid_volume_change'] = df['bid_volume'].diff()
    df['ask_volume_change'] = df['ask_volume'].diff()

    df['ofi'] = (df['bid_volume_change'] - df['ask_volume_change']) / \
                (df['bid_volume_change'].abs() + df['ask_volume_change'].abs() + 1e-10)

    # Smoothed OFI
    df['ofi_ema'] = df['ofi'].ewm(span=10).mean()

    return df
```

**Data Requirements**: Level 2 order book data
**Recommendation**: HIGH PRIORITY if order book data available

---

#### Volume Profile Features ⭐⭐⭐⭐
**Source**: Bookmap.com, 2024; Limit Order Book research

**Key Metrics**:
1. **Point of Control (POC)**: Price level with highest volume
2. **Value Area**: Price range containing 70% of volume
3. **Volume Delta**: Buy volume - Sell volume

```python
def calculate_volume_profile(df, bins=20):
    """Calculate volume profile metrics"""
    # Create price bins
    price_range = df['high'].max() - df['low'].min()
    bin_size = price_range / bins

    df['price_bin'] = ((df['close'] - df['low'].min()) / bin_size).astype(int)

    # Volume by price level
    volume_profile = df.groupby('price_bin')['volume'].sum()

    # Point of Control
    poc_bin = volume_profile.idxmax()
    poc_price = df['low'].min() + (poc_bin * bin_size)

    # Value Area (70% of volume)
    total_volume = volume_profile.sum()
    sorted_profile = volume_profile.sort_values(ascending=False)
    cumsum = sorted_profile.cumsum()
    value_area_bins = sorted_profile[cumsum <= total_volume * 0.7].index

    df['distance_from_poc'] = (df['close'] - poc_price) / poc_price
    df['in_value_area'] = df['price_bin'].isin(value_area_bins).astype(int)

    return df, poc_price
```

**Recommendation**: MEDIUM PRIORITY - Useful for support/resistance

---

#### Limit Order Book (LOB) Features ⭐⭐⭐⭐⭐
**Source**: GitHub nicolezattarin/LOB-feature-analysis, 2024

**Top 10 Features**:
1. Bid-Ask Spread
2. Mid-price momentum
3. Weighted average bid/ask prices (top 5 levels)
4. Order book depth imbalance
5. Queue position changes
6. Cumulative depth asymmetry
7. Price level crosses
8. Volume-weighted imbalance
9. Microstructure noise
10. Autocorrelation of order arrivals

```python
def extract_lob_features(df):
    """Extract Limit Order Book features"""
    # Requires Level 2 data

    # 1. Bid-Ask Spread
    df['spread'] = df['ask_price_1'] - df['bid_price_1']
    df['relative_spread'] = df['spread'] / df['mid_price']

    # 2. Depth imbalance
    total_bid_volume = df[[f'bid_volume_{i}' for i in range(1, 6)]].sum(axis=1)
    total_ask_volume = df[[f'ask_volume_{i}' for i in range(1, 6)]].sum(axis=1)
    df['depth_imbalance'] = (total_bid_volume - total_ask_volume) / \
                            (total_bid_volume + total_ask_volume + 1e-10)

    # 3. Weighted mid-price
    df['weighted_mid'] = (
        (df['bid_price_1'] * df['ask_volume_1'] +
         df['ask_price_1'] * df['bid_volume_1']) /
        (df['bid_volume_1'] + df['ask_volume_1'] + 1e-10)
    )

    # 4. Price level crosses
    df['bid_crosses'] = (df['bid_price_1'] > df['ask_price_1'].shift(1)).astype(int)
    df['ask_crosses'] = (df['ask_price_1'] < df['bid_price_1'].shift(1)).astype(int)

    return df
```

**Data Requirements**: Level 2 order book data from exchange
**Recommendation**: HIGH PRIORITY for crypto (available from Binance/Bybit APIs)

---

#### Hawkes Process Features ⭐⭐⭐⭐
**Source**: Hawkes Processes for Order Flow Forecasting, 2024

**Concept**: Model self-excitation in order arrivals
- Past events trigger future events
- Captures clustering in trades
- Predicts near-term order flow

```python
from tick.hawkes import HawkesExpKern

def fit_hawkes_process(trade_times):
    """Fit Hawkes process to trade arrival times"""
    model = HawkesExpKern(
        decays=1.0,  # Exponential decay rate
        gofit='least-squares'
    )

    model.fit([trade_times])

    # Extract features
    intensity = model.intensity(trade_times)
    baseline = model.baseline[0]
    adjacency = model.adjacency[0][0]

    return {
        'hawkes_intensity': intensity,
        'hawkes_baseline': baseline,
        'hawkes_excitation': adjacency
    }
```

**Recommendation**: ADVANCED - Requires high-frequency data

---

### 3. Advanced Architectures

#### Bayesian Optimization for Hyperparameters ⭐⭐⭐⭐⭐
**Source**: Multiple papers, 2024

**Benefits**:
- Finds optimal hyperparameters faster than grid search
- Probabilistic model of objective function
- Explores promising regions intelligently

```python
from skopt import BayesSearchCV
from skopt.space import Real, Integer

search_spaces = {
    'n_estimators': Integer(50, 300),
    'max_depth': Integer(3, 10),
    'learning_rate': Real(0.001, 0.3, prior='log-uniform'),
    'subsample': Real(0.5, 1.0),
    'min_samples_split': Integer(10, 50),
    'min_samples_leaf': Integer(5, 30)
}

bayes_search = BayesSearchCV(
    GradientBoostingClassifier(),
    search_spaces,
    n_iter=50,  # Number of parameter settings sampled
    cv=TimeSeriesSplit(n_splits=5),
    scoring='f1',
    n_jobs=-1,
    random_state=42
)

bayes_search.fit(X_train, y_train)
best_model = bayes_search.best_estimator_
```

**Recommendation**: HIGH PRIORITY - Easy win

---

#### CatBoost for Categorical Features ⭐⭐⭐⭐
**Source**: Yandex Research, widely used in Kaggle

**Advantages**:
- Native categorical feature support
- Reduced overfitting with ordered boosting
- GPU acceleration
- Built-in cross-validation

```python
from catboost import CatBoostClassifier

model = CatBoostClassifier(
    iterations=500,
    depth=6,
    learning_rate=0.05,
    l2_leaf_reg=3,  # Regularization
    border_count=128,
    feature_border_type='GreedyLogSum',
    loss_function='Logloss',
    eval_metric='F1',
    use_best_model=True,
    random_seed=42,
    verbose=False
)

# Specify categorical features
cat_features = ['hour', 'day_of_week', 'symbol']

model.fit(
    X_train, y_train,
    cat_features=cat_features,
    eval_set=(X_val, y_val),
    early_stopping_rounds=50
)
```

**Recommendation**: HIGH PRIORITY - Add to ensemble

---

#### LightGBM for Speed ⭐⭐⭐⭐
**Source**: Microsoft Research

**Advantages**:
- 10-20x faster than XGBoost
- Lower memory usage
- Handles large datasets (millions of rows)

```python
from lightgbm import LGBMClassifier

model = LGBMClassifier(
    n_estimators=300,
    max_depth=6,
    learning_rate=0.05,
    num_leaves=31,
    subsample=0.8,
    colsample_bytree=0.8,
    reg_alpha=0.1,  # L1 regularization
    reg_lambda=0.1,  # L2 regularization
    min_child_samples=20,
    class_weight='balanced',
    random_state=42
)

model.fit(
    X_train, y_train,
    eval_set=[(X_val, y_val)],
    eval_metric='auc',
    callbacks=[lgb.early_stopping(50)]
)
```

**Recommendation**: HIGH PRIORITY - Add to ensemble

---

### 4. Online Learning / Continual Learning ⭐⭐⭐⭐⭐

**Source**: Online Learning of Order Flow, Taylor & Francis 2024

**Concept**: Update model continuously with new data, not just daily
- Adapt to market regime changes in real-time
- Bayesian change-point detection
- Incremental learning

```python
from river import ensemble, tree, preprocessing

# Online learning pipeline
model = (
    preprocessing.StandardScaler() |
    ensemble.AdaptiveRandomForestClassifier(
        n_models=10,
        max_depth=6,
        lambda_value=6,  # For ADWIN drift detection
        drift_detector=ADWIN(delta=0.001)
    )
)

# Update model with each new data point
for x, y in stream:
    # Predict
    y_pred = model.predict_one(x)

    # Learn from new example
    model.learn_one(x, y)

    # Detect concept drift
    if model.drift_detected:
        print("⚠️ Concept drift detected - model adapting")
```

**Benefits**:
- No need for batch retraining
- Faster adaptation to market changes
- Lower latency

**Recommendation**: VERY HIGH PRIORITY - Game changer for live trading

---

### 5. Multimodal Learning ⭐⭐⭐⭐

**Source**: FinMultiTime Dataset, 2024

**Approach**: Combine multiple data types
1. **Price Time Series**: OHLCV candles
2. **News Sentiment**: NLP on financial news
3. **Technical Charts**: CNN on candlestick images
4. **Order Book**: Depth and flow data

```python
import torch
import torch.nn as nn

class MultimodalTradingModel(nn.Module):
    def __init__(self):
        super().__init__()

        # Time series branch (LSTM)
        self.lstm = nn.LSTM(
            input_size=50,  # Features
            hidden_size=128,
            num_layers=2,
            dropout=0.2,
            batch_first=True
        )

        # Sentiment branch (Transformer)
        self.sentiment_encoder = nn.TransformerEncoder(
            nn.TransformerEncoderLayer(d_model=768, nhead=8),
            num_layers=3
        )

        # Chart image branch (CNN)
        self.cnn = nn.Sequential(
            nn.Conv2d(3, 32, kernel_size=3),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Conv2d(32, 64, kernel_size=3),
            nn.ReLU(),
            nn.MaxPool2d(2),
            nn.Flatten()
        )

        # Fusion layer
        self.fusion = nn.Linear(128 + 768 + 1024, 256)
        self.classifier = nn.Linear(256, 2)

    def forward(self, price_data, sentiment, chart_image):
        # Process each modality
        lstm_out, _ = self.lstm(price_data)
        sentiment_out = self.sentiment_encoder(sentiment)
        cnn_out = self.cnn(chart_image)

        # Concatenate
        combined = torch.cat([
            lstm_out[:, -1, :],  # Last LSTM output
            sentiment_out.mean(dim=1),  # Average pooling
            cnn_out
        ], dim=1)

        # Final prediction
        fused = self.fusion(combined)
        return self.classifier(fused)
```

**Recommendation**: MEDIUM PRIORITY - Requires multi-source data integration

---

## Implementation Roadmap

### Phase 1: Quick Wins (1-2 weeks)

**Priority 1**: Add LightGBM and CatBoost to ensemble
```bash
pip install lightgbm catboost
# Modify retrain_model_v2.py to include these models
```

**Priority 2**: Implement Bayesian hyperparameter optimization
```bash
pip install scikit-optimize
# Replace GridSearchCV with BayesSearchCV
```

**Priority 3**: Add volume profile features
```python
# Add to engineer_features() in retrain_model_v2.py
df = calculate_volume_profile(df)
```

**Expected Impact**: +5-10% F1-Score improvement

---

### Phase 2: Advanced Features (2-4 weeks)

**Priority 1**: Integrate Level 2 order book data
```python
# Connect to Binance/Bybit WebSocket for order book
import websocket
# Implement order book listener
# Extract LOB features
```

**Priority 2**: Calculate Order Flow Imbalance
```python
# Add OFI calculation with Level 2 data
df = calculate_ofi(df)
```

**Priority 3**: Add market microstructure features
- Bid-ask spread dynamics
- Depth imbalance
- Price impact estimates

**Expected Impact**: +10-15% F1-Score improvement

---

### Phase 3: Deep Learning Models (1-2 months)

**Priority 1**: Implement LSTM model
```python
import torch
import torch.nn as nn

class LSTMReversalModel(nn.Module):
    def __init__(self, input_size, hidden_size=128):
        super().__init__()
        self.lstm = nn.LSTM(input_size, hidden_size, num_layers=2,
                           batch_first=True, dropout=0.2)
        self.fc = nn.Linear(hidden_size, 2)

    def forward(self, x):
        lstm_out, _ = self.lstm(x)
        return self.fc(lstm_out[:, -1, :])
```

**Priority 2**: Add Transformer layer
```python
class TransformerReversalModel(nn.Module):
    def __init__(self, d_model=64, nhead=4):
        super().__init__()
        self.transformer = nn.TransformerEncoder(
            nn.TransformerEncoderLayer(d_model, nhead),
            num_layers=3
        )
        self.fc = nn.Linear(d_model, 2)
```

**Priority 3**: Implement LSTM-Transformer hybrid
```python
class HybridModel(nn.Module):
    def __init__(self):
        super().__init__()
        self.lstm = LSTMReversalModel(input_size)
        self.transformer = TransformerReversalModel()
        self.fusion = nn.Linear(256, 2)
```

**Expected Impact**: +15-25% F1-Score improvement

---

### Phase 4: Online Learning (2-3 months)

**Priority 1**: Implement online learning with River
```bash
pip install river
```

**Priority 2**: Set up streaming data pipeline
```python
# Real-time data ingestion
# Incremental model updates
# Concept drift detection
```

**Priority 3**: A/B test online vs batch learning

**Expected Impact**: Faster adaptation, lower latency

---

## Code Snippets: Ready to Use

### 1. Add CatBoost to Ensemble

```python
# In retrain_model_v2.py, add to models_to_train dict:

from catboost import CatBoostClassifier

'catboost': CatBoostClassifier(
    iterations=300,
    depth=5,
    learning_rate=0.05,
    l2_leaf_reg=3,
    border_count=128,
    loss_function='Logloss',
    eval_metric='F1',
    use_best_model=True,
    random_seed=42,
    verbose=False
)
```

### 2. Add LightGBM to Ensemble

```python
from lightgbm import LGBMClassifier

'lightgbm': LGBMClassifier(
    n_estimators=200,
    max_depth=5,
    learning_rate=0.05,
    num_leaves=31,
    subsample=0.7,
    colsample_bytree=0.7,
    reg_alpha=0.1,
    reg_lambda=0.1,
    min_child_samples=20,
    class_weight='balanced',
    random_state=42,
    verbose=-1
)
```

### 3. Bayesian Optimization

```python
# Replace train_models() hyperparameters with:

from skopt import BayesSearchCV
from skopt.space import Real, Integer, Categorical

search_space = {
    'n_estimators': Integer(100, 500),
    'max_depth': Integer(3, 8),
    'learning_rate': Real(0.01, 0.3, prior='log-uniform'),
    'subsample': Real(0.5, 0.9)
}

bayes_opt = BayesSearchCV(
    GradientBoostingClassifier(),
    search_space,
    n_iter=30,
    cv=TimeSeriesSplit(n_splits=3),
    scoring='f1',
    n_jobs=-1
)

bayes_opt.fit(X_train, y_train, sample_weight=sample_weights_train)
best_gb = bayes_opt.best_estimator_
```

### 4. Volume Profile Features

```python
# Add to engineer_features():

def add_volume_profile_features(df, lookback=100):
    """Add volume profile features"""

    for i in range(lookback, len(df)):
        window = df.iloc[i-lookback:i]

        # Volume-weighted average price
        df.loc[df.index[i], 'vwap'] = (
            (window['volume'] * window['close']).sum() /
            window['volume'].sum()
        )

        # Distance from VWAP
        df.loc[df.index[i], 'dist_from_vwap'] = (
            (df.loc[df.index[i], 'close'] - df.loc[df.index[i], 'vwap']) /
            df.loc[df.index[i], 'vwap']
        )

    # Volume percentile
    df['volume_percentile'] = df['volume'].rolling(lookback).apply(
        lambda x: (x.iloc[-1] - x.min()) / (x.max() - x.min())
    )

    return df

df = add_volume_profile_features(df)
```

---

## Performance Expectations

### Current V2 Model
```
F1-Score: 60-72%
Precision: 55-70%
Recall: 60-75%
```

### With Quick Wins (Phase 1)
```
F1-Score: 65-77%  (+5-10%)
Precision: 60-75%
Recall: 65-78%
```

### With Advanced Features (Phase 2)
```
F1-Score: 70-82%  (+10-15%)
Precision: 65-80%
Recall: 70-82%
```

### With Deep Learning (Phase 3)
```
F1-Score: 75-87%  (+15-25%)
Precision: 70-85%
Recall: 75-85%
```

### With Online Learning (Phase 4)
```
Adaptation Speed: Real-time (vs 24h batch)
Concept Drift: Auto-detected
Latency: <100ms (vs minutes)
```

---

## References

1. **LSTM-Transformer Hybrid**: "LSTM–Transformer-Based Robust Hybrid Deep Learning Model for Financial Time Series Forecasting" (MDPI Computers, Jan 2025)

2. **XGBoost-LSTM**: "CRYPTO PRICE PREDICTION USING LSTM+XGBOOST" (arXiv 2506.22055v1, 2024)

3. **Order Flow Imbalance**: "Forecasting high frequency order flow imbalance using Hawkes processes" (arXiv 2408.03594v1, Aug 2024)

4. **LOB Features**: "Deep limit order book forecasting: a microstructural guide" (Quantitative Finance, 2025)

5. **Online Learning**: "Online learning of order flow and market impact with Bayesian change-point detection methods" (Taylor & Francis, 2024)

6. **Multimodal**: FinMultiTime Dataset, covering 5,105 stocks (2009-2025)

7. **Machine Learning for Trading**: Stefan Jansen's GitHub repository (github.com/stefan-jansen/machine-learning-for-trading)

8. **Production ML**: EthicalML awesome-production-machine-learning (github.com/EthicalML/awesome-production-machine-learning)

---

## Next Steps

1. ✅ **Immediate**: Add LightGBM + CatBoost to current ensemble
2. ✅ **This Week**: Implement Bayesian optimization
3. ⏳ **Next Week**: Add volume profile features
4. ⏳ **This Month**: Integrate Level 2 order book data
5. ⏳ **Q1 2026**: Implement LSTM-Transformer hybrid
6. ⏳ **Q2 2026**: Deploy online learning system

---

**Maintained by**: AlgoTrendy Research Team
**Last Updated**: October 20, 2025
**Status**: ✅ Ready for Implementation
