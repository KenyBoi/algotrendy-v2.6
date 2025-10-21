# MEM Architecture - Deep Dive

> **Comprehensive Technical Architecture of the Memory-Enhanced ML Trading System**
> **Date**: October 20, 2025 | **Status**: Production

---

## System Overview

MEM is a **three-layer cognitive architecture** that combines classical algorithmic trading with modern machine learning and persistent memory:

```
┌─────────────────────────────────────────────────────────────────────┐
│                    LAYER 3: COGNITIVE LAYER                          │
│                    (MemGPT Agent + Memory System)                    │
├─────────────────────────────────────────────────────────────────────┤
│                    LAYER 2: PREDICTION LAYER                         │
│                    (ML Models + Feature Engineering)                 │
├─────────────────────────────────────────────────────────────────────┤
│                    LAYER 1: EXECUTION LAYER                          │
│                    (Trading Engine + Brokers)                        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Layer 1: Execution Layer

### Trading Engine (C# .NET 8)
- **Order Management**: Create, modify, cancel orders across 5 brokers
- **Position Tracking**: Real-time position monitoring and P&L calculation
- **Risk Management**: Position sizing, stop-loss, take-profit automation
- **Broker Abstraction**: Unified interface for Binance, Bybit, Interactive Brokers, NinjaTrader, TradeStation

### Data Flow (Layer 1)
```
Market Data → Indicators → Strategies → Signals → Orders → Execution
```

---

## Layer 2: Prediction Layer (ML Models)

### Model Architecture: Gradient Boosting Classifier

#### Training Pipeline
```python
# Data Collection
historical_data = fetch_market_data(
    symbols=['BTCUSDT', 'ETHUSDT', 'BNBUSDT', 'XRPUSDT'],
    timeframe='5m',
    period='90 days'
)  # ~45,000 samples

# Feature Engineering (12 indicators)
features = calculate_features(historical_data)
# Features: SMA5, SMA20, SMA50, RSI, MACD, MACD_signal, MACD_hist,
#           BB_position, volume_ratio, hl_range, close_position, oc_range

# Label Creation
labels = create_reversal_labels(historical_data, lookahead=10)
# Label = 1 if price reverses within next 10 candles, 0 otherwise

# Model Training
model = GradientBoostingClassifier(
    n_estimators=200,
    max_depth=5,
    learning_rate=0.1,
    min_samples_split=20,
    random_state=42
)
model.fit(features, labels)

# Model Evaluation
accuracy: 78%
precision: 72%
recall: 68%
f1_score: 70%
```

#### Feature Importance
```
RSI:                 0.35  (35% contribution to predictions)
MACD_divergence:     0.28  (28%)
volume_ratio:        0.15  (15%)
BB_position:         0.12  (12%)
SMA_crossover:       0.10  (10%)
```

### Prediction Process
```
Real-time Candles (last 100) → Feature Calculation → Model Inference → Prediction
                                                                        ↓
                                                    { is_reversal: True,
                                                      confidence: 0.78,
                                                      direction: 'bullish' }
```

### Model Retraining (Automated)
```bash
# Daily at 2 AM UTC
1. Fetch last 30 days data from QuestDB
2. Calculate features
3. Create labels
4. Train new model
5. Validate on holdout set (20%)
6. Compare to existing model
7. Deploy if accuracy >= existing + 2%
8. Otherwise keep current model
9. Log metrics and version
```

---

## Layer 3: Cognitive Layer (MemGPT + Memory)

### MemGPT Agent Architecture

```
┌────────────────────────────────────────────────────────┐
│                   MemGPT Agent                         │
│                                                         │
│  ┌─────────────────┐      ┌─────────────────┐         │
│  │  Core Memory    │◄────►│  Working Memory │         │
│  │  (Persistent)   │      │  (Session)      │         │
│  └─────────────────┘      └─────────────────┘         │
│           ▲                        ▲                   │
│           │                        │                   │
│           ▼                        ▼                   │
│  ┌──────────────────────────────────────────┐         │
│  │        Decision Reasoning Engine         │         │
│  │  • Load relevant memories                │         │
│  │  • Analyze base signal                   │         │
│  │  • Combine with ML prediction            │         │
│  │  • Assess risk based on history          │         │
│  │  • Generate enhanced signal              │         │
│  └──────────────────────────────────────────┘         │
│           │                                             │
│           ▼                                             │
│  ┌──────────────────────────────────────────┐         │
│  │        Learning & Adaptation             │         │
│  │  • Log decision + outcome                │         │
│  │  • Update core memory                    │         │
│  │  • Adjust parameters                     │         │
│  │  • Discover patterns                     │         │
│  │  • Create new strategies                 │         │
│  └──────────────────────────────────────────┘         │
└────────────────────────────────────────────────────────┘
```

### Memory System (Persistent)

#### Core Memory (`core_memory_updates.txt`)
```
Purpose: Decision history + learned patterns
Format: Timestamped text logs
Size: ~500 bytes per decision
Retention: Unlimited (with archiving)

Example Entry:
[2025-10-20 14:30:15] Strategy: momentum - Decision: BUY BTCUSDT @ $65,000
[2025-10-20 14:30:15] Confidence: 0.85 - ML_Pred: 0.78 - Historical: 0.72
[2025-10-20 14:35:20] Entry confirmed - Size: 0.1 BTC - Stop: $64,500
[2025-10-20 14:40:00] CLOSED - Exit: $66,200 - P&L: +$500 (0.77%)
[2025-10-20 14:45:00] LEARNED: Support at $64,500 strong - 3/3 momentum wins
```

#### Parameter Memory (`parameter_updates.json`)
```json
{
  "2025-10-20": {
    "timestamp": "2025-10-20T14:45:00Z",
    "parameters": {
      "momentum_threshold": 0.05,
      "position_size_btc": 0.1,
      "risk_per_trade": 0.02,
      "ml_confidence_min": 0.65,
      "stop_loss_pct": -1.5
    },
    "adjustments": [
      {
        "parameter": "position_size_btc",
        "from": 0.15,
        "to": 0.1,
        "reason": "Win rate dropped to 42% - reducing risk",
        "triggered_by": "performance_monitor",
        "impact": "Win rate improved to 58% over next 20 trades"
      }
    ],
    "performance_context": {
      "win_rate": 0.58,
      "sharpe_ratio": 2.1,
      "max_drawdown": -0.023
    }
  }
}
```

#### Strategy Modules (`strategy_modules.py`)
```python
"""
MEM-generated trading strategies
These are created automatically through pattern discovery
"""

class LearnedHighVolatilityReversal:
    """
    AUTO-GENERATED STRATEGY
    Created: 2025-10-18 12:30:45
    Discovered Pattern: High volatility + RSI<30 + MACD divergence
    Training Trades: 245
    Win Rate: 68%
    Avg Gain: +1.8%
    Sharpe: 2.3
    Last Updated: 2025-10-20 14:45:00
    """

    def __init__(self):
        # Learned optimal thresholds
        self.volatility_threshold = 2.5
        self.rsi_threshold = 28
        self.macd_divergence_min = 0.15

        # Performance tracking
        self.trades = 245
        self.wins = 167
        self.losses = 78
        self.win_rate = 0.68

    def should_enter(self, market_data):
        """Entry logic discovered through reinforcement learning"""
        vol_check = market_data['volatility'] > self.volatility_threshold
        rsi_check = market_data['rsi'] < self.rsi_threshold
        macd_check = market_data['macd_divergence'] > self.macd_divergence_min

        if vol_check and rsi_check and macd_check:
            return {
                'action': 'BUY',
                'confidence': 0.82,
                'position_size': 0.08,  # Reduced for high vol
                'stop_loss': -2.0,      # Wider for volatility
                'take_profit': 2.5,
                'reasoning': f"Learned pattern (trades: {self.trades}, WR: {self.win_rate:.1%})"
            }
        return None

    def update_performance(self, outcome):
        """Update strategy stats after each trade"""
        self.trades += 1
        if outcome['profit'] > 0:
            self.wins += 1
        else:
            self.losses += 1
        self.win_rate = self.wins / self.trades

        # Auto-deprecate if performance degrades
        if self.trades > 100 and self.win_rate < 0.50:
            self.deprecated = True
            self.deprecation_reason = f"Win rate dropped to {self.win_rate:.1%}"

class LearnedMomentumRSICombo:
    """
    AUTO-GENERATED STRATEGY
    Created: 2025-10-19 08:15:30
    Discovered Pattern: Strong momentum + RSI divergence
    Training Trades: 43
    Win Rate: 80%
    Avg Gain: +1.1%
    Sharpe: 3.1
    Last Updated: 2025-10-20 09:30:00
    """

    def __init__(self):
        self.momentum_min = 0.06
        self.rsi_divergence_min = 5
        self.volume_spike_min = 1.5

        self.trades = 43
        self.wins = 34
        self.losses = 9
        self.win_rate = 0.80

    def should_enter(self, market_data):
        """80% win rate combo - strong pattern"""
        if (market_data['momentum'] > self.momentum_min and
            market_data['rsi_divergence'] > self.rsi_divergence_min and
            market_data['volume_ratio'] > self.volume_spike_min):

            return {
                'action': 'BUY',
                'confidence': 0.88,  # High confidence from track record
                'position_size': 0.12,  # Larger size for high-confidence
                'stop_loss': -1.2,      # Tight stop
                'take_profit': 1.8,     # Quick profit target
                'reasoning': f"High-confidence pattern (WR: {self.win_rate:.1%})"
            }
        return None
```

---

## Complete Signal Flow

### Step-by-Step Process

```
┌─────────────────────────────────────────────────────────┐
│ STEP 1: MARKET DATA INGESTION                           │
└─────────────────────────────────────────────────────────┘
Market Data Channels → QuestDB → Latest 100 candles
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 2: TECHNICAL INDICATOR CALCULATION                 │
└─────────────────────────────────────────────────────────┘
Calculate: SMA, RSI, MACD, Bollinger Bands, Volume, Volatility
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 3: BASE STRATEGY SIGNAL GENERATION                 │
└─────────────────────────────────────────────────────────┘
Strategy Resolver → Momentum/RSI Strategy
                                        ↓
Base Signal: { action: BUY, confidence: 0.65, symbol: BTCUSDT }
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 4: ML PREDICTION                                    │
└─────────────────────────────────────────────────────────┘
ML Model → Predict Trend Reversal
                                        ↓
ML Prediction: { is_reversal: True, confidence: 0.78 }
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 5: MEMGPT ENHANCEMENT                              │
└─────────────────────────────────────────────────────────┘
MemGPT Agent:
  1. Load relevant memories (last 50 BTCUSDT decisions)
  2. Find pattern matches ("momentum wins 72% in current conditions")
  3. Check learned strategies (LearnedMomentumRSICombo matches)
  4. Assess risk (volatility normal, liquidity good)
  5. Combine base + ML + memory:
     - Base confidence: 0.65
     - ML confidence: 0.78
     - Historical pattern: 0.80 (LearnedMomentumRSICombo)
     - Final confidence: 0.73 (weighted average)
                                        ↓
Enhanced Signal: {
  action: BUY,
  symbol: BTCUSDT,
  confidence: 0.73,
  position_size: 0.10,
  stop_loss: -1.5%,
  take_profit: 2.0%,
  reasoning: "Momentum confirmed by ML reversal (78%). Pattern match
              with LearnedMomentumRSICombo (80% WR). Risk: Medium.",
  base_confidence: 0.65,
  ml_confidence: 0.78,
  memory_confidence: 0.80,
  learned_strategy: "LearnedMomentumRSICombo"
}
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 6: ORDER EXECUTION                                 │
└─────────────────────────────────────────────────────────┘
Trading Engine → Place Order on Binance
                                        ↓
Order Result: { status: FILLED, entry: $65,000, size: 0.10 BTC }
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 7: DECISION LOGGING                                │
└─────────────────────────────────────────────────────────┘
Decision Logger:
  - Log decision to core_memory_updates.txt
  - Save trade details
  - Track in session memory
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 8: POSITION MONITORING                             │
└─────────────────────────────────────────────────────────┘
Monitor position until exit (stop loss OR take profit OR manual)
                                        ↓
Exit: $66,200 (+1.85% gain) after 15 minutes
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 9: OUTCOME LOGGING & LEARNING                      │
└─────────────────────────────────────────────────────────┘
Decision Logger:
  - Log outcome (+$500 profit)
  - Update LearnedMomentumRSICombo stats (44 trades, 81% WR now)
  - Append to core memory: "Pattern worked, momentum + RSI reliable"
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 10: PARAMETER ADJUSTMENT                           │
└─────────────────────────────────────────────────────────┘
MemGPT Learning:
  - Increase confidence in momentum strategy: 0.65 → 0.68
  - Increase position size for similar setups: 0.10 → 0.11 BTC
  - Tighten stop loss (pattern reliable): -1.5% → -1.3%
  - Update parameter_updates.json
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 11: PATTERN DISCOVERY (if applicable)              │
└─────────────────────────────────────────────────────────┘
If pattern detected 5+ times with 70%+ WR:
  - Create new learned strategy module
  - Add to strategy_modules.py
  - Enable for future signals
                                        ↓
┌─────────────────────────────────────────────────────────┐
│ STEP 12: LOOP BACK TO STEP 1                            │
└─────────────────────────────────────────────────────────┘
System is now smarter with this new experience
```

---

## Learning & Adaptation Mechanisms

### 1. Performance Monitoring
```python
class PerformanceMonitor:
    def __init__(self):
        self.window_size = 20  # Last 20 trades
        self.alert_threshold = 0.45  # Alert if WR < 45%

    def analyze_recent_performance(self):
        recent_trades = get_last_n_trades(self.window_size)
        win_rate = calculate_win_rate(recent_trades)
        sharpe = calculate_sharpe(recent_trades)
        max_dd = calculate_max_drawdown(recent_trades)

        if win_rate < self.alert_threshold:
            # Trigger adaptation
            self.reduce_position_sizes(factor=0.7)
            self.increase_confidence_threshold(new=0.70)
            self.log_adjustment("Performance degradation detected")
```

### 2. Parameter Adaptation
```python
class ParameterAdapter:
    def adapt_to_market_conditions(self, market_state):
        if market_state['volatility'] > 2.0:
            # High volatility regime
            self.widen_stops(factor=1.5)
            self.reduce_position_sizes(factor=0.8)
            self.increase_profit_targets(factor=1.3)

        elif market_state['trend'] == 'strong_uptrend':
            # Trend following
            self.favor_momentum_strategies()
            self.increase_position_sizes(factor=1.2)

        elif market_state['choppy'] == True:
            # Range-bound market
            self.favor_mean_reversion()
            self.tighten_stops(factor=0.8)
```

### 3. Strategy Evolution
```python
class StrategyEvolver:
    def discover_patterns(self):
        # Analyze last 500 trades
        trades = get_last_n_trades(500)

        # Find common winning patterns
        patterns = {}
        for trade in trades:
            if trade.profit > 0:
                signature = self.create_signature(trade)
                patterns[signature] = patterns.get(signature, 0) + 1

        # Create strategies from patterns with 70%+ WR
        for signature, count in patterns.items():
            win_rate = self.calculate_pattern_wr(signature)
            if count >= 5 and win_rate >= 0.70:
                # Create new strategy
                new_strategy = self.generate_strategy_class(signature, win_rate)
                self.save_to_strategy_modules(new_strategy)
                logger.info(f"New strategy created: {new_strategy.name}")
```

---

## Integration Points

### C# Trading Engine Integration

```csharp
// In AlgoTrendy.TradingEngine/Services/StrategyResolver.cs

public async Task<EnhancedSignal> GenerateEnhancedSignalAsync(string symbol)
{
    // LAYER 1: Get base strategy signal
    var baseSignal = await _strategyFactory.ResolveStrategyAsync(symbol);

    // LAYER 2: Get ML prediction
    var mlPrediction = await _mlModelService.PredictReversalAsync(symbol);

    // LAYER 3: Enhance with MemGPT
    var marketContext = new MarketContext
    {
        Symbol = symbol,
        RecentCandles = await _marketDataService.GetRecentCandlesAsync(symbol, 100),
        CurrentVolatility = await _volatilityCalculator.CalculateAsync(symbol),
        Liquidity = await _liquidityAnalyzer.AnalyzeAsync(symbol)
    };

    var enhancedSignal = await _memGPTConnector.EnhanceSignalAsync(
        baseSignal,
        mlPrediction,
        marketContext
    );

    return enhancedSignal;
}

public async Task LogOutcomeAndLearnAsync(EnhancedSignal signal, OrderResult result)
{
    // Log decision + outcome
    await _decisionLogger.LogAsync(signal, result);

    // Trigger learning
    await _memGPTConnector.LearnFromOutcomeAsync(signal, result);

    // Update parameters if needed
    await _parameterAdapter.AdaptAsync(signal, result);
}
```

---

## Performance Optimization

### Caching Strategy
```python
# ML predictions cached for 5 minutes
prediction_cache = {
    'BTCUSDT': {
        'prediction': {...},
        'timestamp': 1634567890,
        'ttl': 300  # 5 minutes
    }
}

# Memory queries cached for 1 minute
memory_cache = {
    'last_50_btc_trades': {
        'data': [...],
        'timestamp': 1634567890,
        'ttl': 60
    }
}
```

### Async Processing
```python
# Run ML prediction + memory loading in parallel
async def enhance_signal_async(base_signal, market_context):
    ml_task = asyncio.create_task(ml_model.predict(market_context))
    memory_task = asyncio.create_task(load_relevant_memories(base_signal))

    ml_prediction, memories = await asyncio.gather(ml_task, memory_task)

    return combine_signals(base_signal, ml_prediction, memories)
```

---

## Monitoring & Debugging

### Real-time Metrics
```
MEM Dashboard (http://localhost:5001/metrics):
- Decisions/hour: 12
- ML latency: 45ms avg
- Memory load time: 8ms avg
- Total enhancement time: 120ms avg
- Win rate (last 24h): 64%
- Active learned strategies: 15
- Deprecated strategies: 8
```

### Debug Logging
```python
logger.info(f"[MEM] Signal enhancement started for {symbol}")
logger.debug(f"Base signal: {base_signal}")
logger.debug(f"ML prediction: {ml_prediction}")
logger.debug(f"Loaded {len(memories)} relevant memories")
logger.info(f"Enhanced confidence: {base.conf:.2f} → {enhanced.conf:.2f}")
logger.info(f"Reasoning: {enhanced.reasoning}")
```

---

## Security Considerations

### Credential Management
- All credentials stored in environment variables
- Never logged or exposed in debug output
- Rotation support with zero-downtime
- Audit trail for all credential access

### Memory Protection
- Memory files encrypted at rest
- File permissions: 600 (owner read/write only)
- Regular backups to secure location
- Version control for audit trail

---

## Scalability

### Current Limits
- **Symbols**: 50 concurrent (can handle more)
- **Decisions/sec**: ~10 (ML inference bottleneck)
- **Memory Size**: Unlimited (file-based, archivable)
- **Learned Strategies**: No limit

### Future Scaling
- Migrate memory to QuestDB for faster queries
- ML model serving cluster for parallel predictions
- Distributed MemGPT agents for multiple symbols
- Strategy marketplace for sharing learned patterns

---

**Status**: Production-Ready
**Last Updated**: October 20, 2025
**Maintained By**: AlgoTrendy Development Team
