# Trading Strategies - Quick Start Implementation Guide

**Date**: October 21, 2025
**For**: Development Team
**Timeline**: 9 weeks to production

---

## TL;DR

We're implementing **5 proven trading strategies** with MEM (Memory-Enhanced ML) to create a diversified, adaptive trading system:

1. **Dual Momentum** - Gary Antonacci's award-winning strategy (17% CAGR, Sharpe 1.02)
2. **Pairs Trading** - Statistical arbitrage (50% return, Sharpe 8.14 in best case)
3. **Combined Momentum + Mean Reversion** - Works in all market conditions (Sharpe 1.85)
4. **Multi-Timeframe Trend** - CTA-style approach (Sharpe 1.0-2.0)
5. **Volatility Breakout** - ML-enhanced breakout detection (Sharpe 1.2-1.8)

**Target**: Portfolio Sharpe > 2.0, Max DD < 20%, CAGR > 18%

---

## Week 1: Setup (Start Immediately)

### Day 1: Environment Setup

```bash
# 1. Create virtual environment for backtesting
cd /root/AlgoTrendy_v2.6
python3 -m venv venv-backtest
source venv-backtest/bin/activate

# 2. Install dependencies
pip install pandas numpy scipy scikit-learn yfinance backtrader

# 3. Test MEM integration
python MEM/test_integration.py
```

### Day 2-3: Data Pipeline

```python
# Create data fetcher
# File: backtesting/data_fetcher.py

import yfinance as yf
import pandas as pd

class DataFetcher:
    """Fetch historical data for backtesting"""

    def fetch_data(self, symbols, start_date, end_date):
        """
        Fetch OHLCV data for multiple symbols

        Args:
            symbols: List of tickers (e.g., ['SPY', 'VEU', 'BND'])
            start_date: '2014-01-01'
            end_date: '2024-10-21'

        Returns:
            dict of DataFrames {symbol: df}
        """
        data = {}
        for symbol in symbols:
            df = yf.download(symbol, start=start_date, end=end_date)
            data[symbol] = df
        return data

    def fetch_crypto(self, symbol, start_date, end_date):
        """Fetch crypto data from Binance/AlgoTrendy DB"""
        # TODO: Integrate with AlgoTrendy.DataChannels
        pass
```

### Day 4-5: Strategy Base Class

```python
# File: backtesting/strategy_base.py

from abc import ABC, abstractmethod
from typing import Dict, Optional
import pandas as pd

class Strategy(ABC):
    """Base class for all trading strategies"""

    def __init__(self, name: str):
        self.name = name
        self.positions = {}
        self.trades = []
        self.equity_curve = []

    @abstractmethod
    async def generate_signal(self, symbol: str, date: pd.Timestamp, market_data: Dict) -> Optional[Dict]:
        """
        Generate trading signal

        Returns:
            {
                'action': 'BUY' | 'SELL' | 'CLOSE',
                'symbol': str,
                'confidence': float (0-1),
                'allocation': float (0-1),
                'reasoning': str
            }
        """
        pass

    def calculate_metrics(self) -> Dict:
        """Calculate performance metrics"""
        returns = pd.Series([t['pnl'] for t in self.trades])

        return {
            'total_return': returns.sum(),
            'sharpe_ratio': returns.mean() / returns.std() * (252 ** 0.5),
            'max_drawdown': self.calculate_max_drawdown(),
            'win_rate': len([t for t in self.trades if t['pnl'] > 0]) / len(self.trades),
            'total_trades': len(self.trades)
        }
```

---

## Week 2: Implement Strategies 1-3

### Day 1-2: Dual Momentum

```python
# File: backtesting/strategies/dual_momentum.py

from strategy_base import Strategy
import pandas as pd

class DualMomentumStrategy(Strategy):
    """
    Gary Antonacci's Global Equity Momentum (GEM)
    Monthly rebalancing between SPY, VEU, and BND
    """

    def __init__(self):
        super().__init__("dual_momentum")
        self.lookback_days = 252  # 12 months
        self.skip_days = 21       # Skip last month
        self.symbols = ['SPY', 'VEU', 'BND']

    async def generate_signal(self, symbol, date, market_data):
        """Generate monthly rebalancing signal"""

        # Only rebalance on last day of month
        if not self.is_month_end(date):
            return None

        # Calculate momentum for SPY and VEU
        spy_momentum = self.calculate_momentum('SPY', date, market_data)
        veu_momentum = self.calculate_momentum('VEU', date, market_data)

        # Decision logic
        if spy_momentum > 0 or veu_momentum > 0:
            # At least one has positive momentum
            winner = 'SPY' if spy_momentum > veu_momentum else 'VEU'

            return {
                'action': 'BUY',
                'symbol': winner,
                'confidence': 0.85,
                'allocation': 1.0,  # 100% allocation
                'reasoning': f'Dual Momentum: {winner} momentum = {max(spy_momentum, veu_momentum):.2%}'
            }
        else:
            # Both negative - go to bonds
            return {
                'action': 'BUY',
                'symbol': 'BND',
                'confidence': 0.90,
                'allocation': 1.0,
                'reasoning': 'Negative momentum - defensive bonds'
            }

    def calculate_momentum(self, symbol, date, market_data):
        """Calculate 12-month momentum (excluding last month)"""
        df = market_data[symbol]

        end_date = date - pd.Timedelta(days=self.skip_days)
        start_date = date - pd.Timedelta(days=self.lookback_days)

        end_price = df.loc[df.index <= end_date, 'Close'].iloc[-1]
        start_price = df.loc[df.index <= start_date, 'Close'].iloc[-1]

        return (end_price / start_price) - 1

    def is_month_end(self, date):
        """Check if date is last trading day of month"""
        next_day = date + pd.Timedelta(days=1)
        return date.month != next_day.month
```

**Test Command**:
```python
# File: backtesting/test_dual_momentum.py

from strategies.dual_momentum import DualMomentumStrategy
from data_fetcher import DataFetcher
from backtester import Backtester

# Fetch data
fetcher = DataFetcher()
data = fetcher.fetch_data(['SPY', 'VEU', 'BND'], '2014-01-01', '2024-10-21')

# Run backtest
strategy = DualMomentumStrategy()
backtester = Backtester(initial_capital=100000)
results = backtester.run(strategy, data)

print(f"CAGR: {results['cagr']:.2%}")
print(f"Sharpe: {results['sharpe']:.2f}")
print(f"Max DD: {results['max_dd']:.2%}")
print(f"Win Rate: {results['win_rate']:.2%}")

# Target: CAGR > 12%, Sharpe > 0.8, Max DD < 30%, Win Rate > 65%
```

### Day 3-4: Pairs Trading

```python
# File: backtesting/strategies/pairs_trading.py

from strategy_base import Strategy
import numpy as np

class PairsTradingStrategy(Strategy):
    """
    Statistical arbitrage between correlated pairs
    """

    def __init__(self, asset_a, asset_b):
        super().__init__(f"pairs_{asset_a}_{asset_b}")
        self.asset_a = asset_a
        self.asset_b = asset_b
        self.lookback = 60
        self.entry_z = 2.0
        self.exit_z = 0.5
        self.position = None

    async def generate_signal(self, symbol, date, market_data):
        """Generate pairs trading signal based on z-score"""

        # Get prices
        price_a = market_data[self.asset_a].loc[:date, 'Close']
        price_b = market_data[self.asset_b].loc[:date, 'Close']

        # Calculate spread
        spread = np.log(price_a / price_b)

        # Calculate z-score
        mean = spread.rolling(self.lookback).mean().iloc[-1]
        std = spread.rolling(self.lookback).std().iloc[-1]
        z_score = (spread.iloc[-1] - mean) / std

        # Entry signals
        if self.position is None:
            if z_score > self.entry_z:
                # Spread too high - short A, long B
                return {
                    'action': 'OPEN_PAIR',
                    'leg_1': {'symbol': self.asset_a, 'side': 'SHORT', 'size': 1.0},
                    'leg_2': {'symbol': self.asset_b, 'side': 'LONG', 'size': 1.0},
                    'confidence': min(0.95, z_score / 4.0),
                    'reasoning': f'Spread overextended: z-score = {z_score:.2f}'
                }
            elif z_score < -self.entry_z:
                # Spread too low - long A, short B
                return {
                    'action': 'OPEN_PAIR',
                    'leg_1': {'symbol': self.asset_a, 'side': 'LONG', 'size': 1.0},
                    'leg_2': {'symbol': self.asset_b, 'side': 'SHORT', 'size': 1.0},
                    'confidence': min(0.95, abs(z_score) / 4.0),
                    'reasoning': f'Spread underextended: z-score = {z_score:.2f}'
                }

        # Exit signals
        else:
            if abs(z_score) < self.exit_z:
                return {
                    'action': 'CLOSE_PAIR',
                    'reasoning': f'Mean reversion complete: z-score = {z_score:.2f}'
                }

        return None  # Hold
```

### Day 5: Combined Momentum + Mean Reversion

```python
# File: backtesting/strategies/combined_mom_rev.py

from strategy_base import Strategy

class CombinedStrategy(Strategy):
    """
    Combines momentum and mean reversion based on volatility regime
    """

    def __init__(self):
        super().__init__("combined_mom_rev")
        self.high_vol_threshold = 0.025  # 2.5% daily vol

    async def generate_signal(self, symbol, date, market_data):
        # Calculate indicators
        momentum = self.calculate_momentum(symbol, date, market_data)
        rsi = self.calculate_rsi(symbol, date, market_data)
        volatility = self.calculate_volatility(symbol, date, market_data)

        # Regime detection
        if volatility > self.high_vol_threshold:
            # High volatility - use mean reversion
            if rsi < 30:
                return {
                    'action': 'BUY',
                    'symbol': symbol,
                    'confidence': 0.75,
                    'allocation': 0.5,
                    'reasoning': f'High vol mean reversion: RSI = {rsi:.0f}'
                }
            elif rsi > 70:
                return {
                    'action': 'SELL',
                    'symbol': symbol,
                    'confidence': 0.75,
                    'allocation': 0.5,
                    'reasoning': f'High vol mean reversion: RSI = {rsi:.0f}'
                }
        else:
            # Low volatility - use momentum
            if momentum > 0.10:
                return {
                    'action': 'BUY',
                    'symbol': symbol,
                    'confidence': 0.80,
                    'allocation': 0.75,
                    'reasoning': f'Low vol momentum: {momentum:.2%} gain'
                }
            elif momentum < -0.10:
                return {
                    'action': 'SELL',
                    'symbol': symbol,
                    'confidence': 0.80,
                    'allocation': 0.75,
                    'reasoning': f'Low vol momentum: {momentum:.2%} loss'
                }

        return None  # Hold
```

---

## Week 3: Implement Strategies 4-5 + MEM Integration

### Day 1-2: Multi-Timeframe + Volatility Breakout

(Implementation similar to above, see full document for details)

### Day 3-5: MEM Integration

```python
# File: backtesting/mem_integration.py

from MEM.mem_agent import MEMAgent

class MEMEnhancedStrategy(Strategy):
    """Base class for MEM-enhanced strategies"""

    def __init__(self, name, mem_agent):
        super().__init__(name)
        self.mem_agent = mem_agent

    async def generate_signal_with_mem(self, symbol, date, market_data):
        """Generate signal enhanced by MEM"""

        # Get base strategy signal
        base_signal = await self.generate_signal(symbol, date, market_data)

        if base_signal is None:
            return None

        # MEM enhancement
        enhanced = await self.mem_agent.enhance_signal(
            signal=base_signal,
            market_data=market_data,
            symbol=symbol,
            date=date
        )

        return enhanced
```

---

## Week 4: Backtesting

### Backtest Runner

```python
# File: backtesting/run_all_backtests.py

from strategies import *
from backtester import Backtester
from data_fetcher import DataFetcher
import pandas as pd

def main():
    # Fetch data
    fetcher = DataFetcher()

    # Test periods
    periods = [
        {'start': '2014-01-01', 'end': '2019-12-31', 'name': 'Pre-COVID'},
        {'start': '2020-01-01', 'end': '2024-10-21', 'name': 'COVID+Post'},
        {'start': '2014-01-01', 'end': '2024-10-21', 'name': 'Full Period'}
    ]

    # Strategies to test
    strategies = [
        DualMomentumStrategy(),
        PairsTradingStrategy('BTC', 'ETH'),
        CombinedStrategy(),
        # Add others...
    ]

    results = []

    for period in periods:
        print(f"\n=== Testing Period: {period['name']} ===")

        # Fetch data for period
        data = fetcher.fetch_data(['SPY', 'VEU', 'BND'], period['start'], period['end'])

        for strategy in strategies:
            print(f"\nBacktesting {strategy.name}...")

            backtester = Backtester(initial_capital=100000)
            result = backtester.run(strategy, data)

            results.append({
                'strategy': strategy.name,
                'period': period['name'],
                **result
            })

            print(f"  CAGR: {result['cagr']:.2%}")
            print(f"  Sharpe: {result['sharpe']:.2f}")
            print(f"  Max DD: {result['max_dd']:.2%}")
            print(f"  Win Rate: {result['win_rate']:.2%}")

    # Save results
    df = pd.DataFrame(results)
    df.to_csv('backtest_results.csv', index=False)
    print("\n✅ All backtests complete! Results saved to backtest_results.csv")

if __name__ == '__main__':
    main()
```

---

## Week 5: MEM Training & Optimization

```python
# File: backtesting/mem_training.py

from MEM.mem_agent import MEMAgent
from strategies import *

async def train_mem_on_strategies():
    """Train MEM on backtest results"""

    mem = MEMAgent()

    # Load backtest results
    results = pd.read_csv('backtest_results.csv')

    # Train MEM to:
    # 1. Optimize parameters
    # 2. Discover new patterns
    # 3. Learn regime detection
    # 4. Improve entry/exit timing

    for _, row in results.iterrows():
        await mem.learn_from_backtest(
            strategy=row['strategy'],
            performance=row
        )

    # Generate learned strategies
    learned_strategies = await mem.discover_strategies(
        min_win_rate=0.70,
        min_sharpe=1.5,
        min_trades=50
    )

    print(f"✅ MEM created {len(learned_strategies)} new strategies!")

    return learned_strategies
```

---

## Week 6-8: Paper Trading

```bash
# Deploy to paper trading environment

cd /root/AlgoTrendy_v2.6

# 1. Configure paper trading accounts
vim backend/AlgoTrendy.API/appsettings.PaperTrading.json

# 2. Deploy strategies
docker-compose -f docker-compose.paper.yml up -d

# 3. Monitor dashboard
open http://localhost:5001/dashboard
```

---

## Week 9: Production Deployment

```bash
# Final checklist before production:
# ✅ All backtests meet success criteria
# ✅ Paper trading showing positive results (2+ weeks)
# ✅ Risk management verified
# ✅ MEM learning successfully
# ✅ Monitoring dashboards functional
# ✅ Alerts configured

# Deploy to production with 10% of capital
docker-compose -f docker-compose.prod.yml up -d

# Monitor closely for first week
# Gradually increase allocation if performance meets targets
```

---

## Key Files to Create

```
AlgoTrendy_v2.6/
├── backtesting/
│   ├── __init__.py
│   ├── strategy_base.py          ← Base class for strategies
│   ├── backtester.py              ← Backtesting engine
│   ├── data_fetcher.py            ← Historical data fetcher
│   ├── mem_integration.py         ← MEM enhancement layer
│   ├── run_all_backtests.py       ← Batch backtest runner
│   ├── mem_training.py            ← Train MEM on results
│   │
│   └── strategies/
│       ├── __init__.py
│       ├── dual_momentum.py       ← Strategy #1
│       ├── pairs_trading.py       ← Strategy #2
│       ├── combined_mom_rev.py    ← Strategy #3
│       ├── multi_timeframe.py     ← Strategy #4
│       └── volatility_breakout.py ← Strategy #5
│
└── MEM/
    ├── mem_agent.py               ← Already exists
    ├── strategy_learning.py       ← New: Strategy discovery
    └── regime_detection.py        ← New: Market regime classifier
```

---

## Daily Standups

**Format**: 15-minute check-in each morning

**Questions**:
1. What did you complete yesterday?
2. What are you working on today?
3. Any blockers?
4. Are we on track for weekly goals?

---

## Success Criteria Checklist

### By End of Week 4 (Backtesting Complete)
- [ ] All 5 strategies implemented
- [ ] Backtests run successfully
- [ ] At least 3/5 strategies meet target metrics
- [ ] Results documented in `backtest_results.csv`

### By End of Week 5 (MEM Training)
- [ ] MEM trained on backtest data
- [ ] Parameters optimized
- [ ] At least 2 learned strategies created
- [ ] Performance improved > 10% vs baseline

### By End of Week 8 (Paper Trading)
- [ ] 2+ weeks of paper trading data
- [ ] Real-time performance matches backtest (within 10%)
- [ ] No major bugs or issues
- [ ] MEM learning actively improving results

### Week 9 (Production Ready)
- [ ] Risk management validated
- [ ] Compliance checks passed
- [ ] Monitoring dashboards operational
- [ ] Ready for 10% capital allocation

---

## Questions? Issues?

**Contact**: Development team lead
**Docs**: See `TOP_5_TRADING_STRATEGIES_FOR_MEM.md` for full details
**MEM Docs**: See `/root/AlgoTrendy_v2.6/MEM/README.md`

**Let's build something amazing!** 🚀
