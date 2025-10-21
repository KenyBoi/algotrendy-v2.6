#!/usr/bin/env python3
"""
Enhanced MEM Trading Strategy
Combines multiple quality filters to improve trade selection:
1. 80% MEM confidence threshold
2. 2% minimum predicted price movement
3. ROC(20) > 0 equity momentum filter (best performing indicator)
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Optional
from dataclasses import dataclass
from datetime import datetime


@dataclass
class MEMPrediction:
    """MEM model prediction"""
    action: str  # 'BUY', 'SELL', or 'HOLD'
    confidence: float  # 0.0 to 1.0
    predicted_price_change_pct: float  # Expected % price movement
    current_price: float
    predicted_price: float
    timestamp: datetime


@dataclass
class TradeSignal:
    """Final trade signal after all filters"""
    should_trade: bool
    action: str
    confidence: float
    predicted_movement: float
    filter_results: Dict[str, bool]
    rejection_reasons: List[str]


class EnhancedMEMStrategy:
    """
    Enhanced MEM strategy with strict quality filters

    Only takes trades when:
    1. MEM confidence >= 80%
    2. Predicted price movement >= 2%
    3. Equity ROC(20) > 0 (momentum filter)
    """

    def __init__(
        self,
        min_confidence: float = 0.80,
        min_price_movement_pct: float = 2.0,
        use_roc_filter: bool = True,
        roc_period: int = 20
    ):
        self.min_confidence = min_confidence
        self.min_price_movement_pct = min_price_movement_pct
        self.use_roc_filter = use_roc_filter
        self.roc_period = roc_period

        # Track equity for ROC calculation
        self.equity_history = []
        self.initial_equity = 10000

        print(f"🚀 Enhanced MEM Strategy Initialized")
        print(f"   Min Confidence: {self.min_confidence:.0%}")
        print(f"   Min Movement: {self.min_price_movement_pct}%")
        print(f"   ROC Filter: {'Enabled' if self.use_roc_filter else 'Disabled'} (period={self.roc_period})")

    def calculate_roc(self, equity_series: List[float], period: int) -> float:
        """Calculate Rate of Change on equity"""
        if len(equity_series) < period + 1:
            return 0.0

        current = equity_series[-1]
        previous = equity_series[-(period + 1)]

        if previous == 0:
            return 0.0

        roc = ((current - previous) / previous) * 100
        return roc

    def check_confidence_filter(self, mem_prediction: MEMPrediction) -> bool:
        """Filter 1: Check if confidence meets threshold"""
        return mem_prediction.confidence >= self.min_confidence

    def check_movement_filter(self, mem_prediction: MEMPrediction) -> bool:
        """Filter 2: Check if predicted movement is significant enough"""
        abs_movement = abs(mem_prediction.predicted_price_change_pct)
        return abs_movement >= self.min_price_movement_pct

    def check_roc_filter(self) -> bool:
        """Filter 3: Check if equity momentum is positive"""
        if not self.use_roc_filter:
            return True

        if len(self.equity_history) < self.roc_period + 1:
            # Not enough data yet, allow trading
            return True

        roc = self.calculate_roc(self.equity_history, self.roc_period)
        return roc > 0

    def evaluate_trade(self, mem_prediction: MEMPrediction) -> TradeSignal:
        """
        Evaluate whether to take a trade based on all filters

        Returns TradeSignal with detailed filter results
        """
        rejection_reasons = []
        filter_results = {}

        # Filter 1: Confidence
        confidence_pass = self.check_confidence_filter(mem_prediction)
        filter_results['confidence'] = confidence_pass
        if not confidence_pass:
            rejection_reasons.append(
                f"Low confidence: {mem_prediction.confidence:.1%} < {self.min_confidence:.0%}"
            )

        # Filter 2: Movement
        movement_pass = self.check_movement_filter(mem_prediction)
        filter_results['movement'] = movement_pass
        if not movement_pass:
            rejection_reasons.append(
                f"Small movement: {abs(mem_prediction.predicted_price_change_pct):.2f}% < {self.min_price_movement_pct}%"
            )

        # Filter 3: ROC
        roc_pass = self.check_roc_filter()
        filter_results['roc'] = roc_pass
        if not roc_pass and self.use_roc_filter:
            roc_value = self.calculate_roc(self.equity_history, self.roc_period)
            rejection_reasons.append(
                f"Negative equity momentum: ROC({self.roc_period}) = {roc_value:.2f}%"
            )

        # All filters must pass
        should_trade = confidence_pass and movement_pass and roc_pass

        return TradeSignal(
            should_trade=should_trade,
            action=mem_prediction.action if should_trade else 'HOLD',
            confidence=mem_prediction.confidence,
            predicted_movement=mem_prediction.predicted_price_change_pct,
            filter_results=filter_results,
            rejection_reasons=rejection_reasons
        )

    def update_equity(self, current_equity: float):
        """Update equity history for ROC calculation"""
        self.equity_history.append(current_equity)

    def get_current_roc(self) -> Optional[float]:
        """Get current ROC value"""
        if len(self.equity_history) < self.roc_period + 1:
            return None
        return self.calculate_roc(self.equity_history, self.roc_period)


def simulate_mem_predictions(df: pd.DataFrame) -> List[MEMPrediction]:
    """
    Simulate MEM predictions based on actual trade data
    In production, this would come from the real MEM model
    """
    predictions = []

    for idx, trade in df.iterrows():
        # Use actual trade data to simulate what MEM would have predicted
        entry_price = trade['entry_price']
        exit_price = trade['exit_price']
        actual_pct_change = ((exit_price - entry_price) / entry_price) * 100

        # Direction
        action = trade['side']  # Already BUY or SELL

        # Use mem_confidence from actual trades
        confidence = trade['mem_confidence']

        # Predicted price change (in production, this comes from MEM)
        # For simulation, use actual with some noise
        predicted_pct = actual_pct_change

        predicted_price = entry_price * (1 + predicted_pct / 100)

        prediction = MEMPrediction(
            action=action,
            confidence=confidence,
            predicted_price_change_pct=abs(predicted_pct),  # MEM predicts magnitude
            current_price=entry_price,
            predicted_price=predicted_price,
            timestamp=pd.to_datetime(trade['entry_time'])
        )

        predictions.append(prediction)

    return predictions


def test_enhanced_strategy():
    """Test the enhanced MEM strategy with strict filters"""
    print("="*120)
    print("🧪 TESTING ENHANCED MEM STRATEGY")
    print("="*120)
    print()

    # Load trade data
    trades_df = pd.read_csv('trade_indicators.csv')
    trades_df['entry_time'] = pd.to_datetime(trades_df['entry_time'])
    trades_df['exit_time'] = pd.to_datetime(trades_df['exit_time'])

    print(f"📊 Loaded {len(trades_df)} historical trades")
    print(f"   Baseline PnL: ${trades_df['pnl'].sum():.2f}")
    print(f"   Baseline Win Rate: {(trades_df['is_win'] == 1).mean():.1%}")
    print()

    # Get MEM predictions
    mem_predictions = simulate_mem_predictions(trades_df)

    # Test different filter configurations
    configs = [
        {'name': 'No Filters (Baseline)', 'min_conf': 0.0, 'min_move': 0.0, 'use_roc': False},
        {'name': '80% Confidence Only', 'min_conf': 0.80, 'min_move': 0.0, 'use_roc': False},
        {'name': '2% Movement Only', 'min_conf': 0.0, 'min_move': 2.0, 'use_roc': False},
        {'name': '80% Conf + 2% Move', 'min_conf': 0.80, 'min_move': 2.0, 'use_roc': False},
        {'name': 'ROC(20) Only', 'min_conf': 0.0, 'min_move': 0.0, 'use_roc': True},
        {'name': '80% + 2% + ROC(20)', 'min_conf': 0.80, 'min_move': 2.0, 'use_roc': True},
    ]

    results = []

    for config in configs:
        print(f"\n{'='*120}")
        print(f"📋 Testing: {config['name']}")
        print(f"{'='*120}")

        strategy = EnhancedMEMStrategy(
            min_confidence=config['min_conf'],
            min_price_movement_pct=config['min_move'],
            use_roc_filter=config['use_roc']
        )

        trades_taken = []
        trades_rejected = []
        current_equity = 10000

        for i, (trade, prediction) in enumerate(zip(trades_df.itertuples(), mem_predictions)):
            # Update equity
            strategy.update_equity(current_equity)

            # Evaluate trade
            signal = strategy.evaluate_trade(prediction)

            if signal.should_trade:
                trades_taken.append(trade)
                current_equity += trade.pnl
            else:
                trades_rejected.append({
                    'trade': trade,
                    'reasons': signal.rejection_reasons
                })

        # Calculate metrics
        if len(trades_taken) > 0:
            taken_df = pd.DataFrame([t._asdict() for t in trades_taken])
            total_pnl = taken_df['pnl'].sum()
            avg_pnl = taken_df['pnl'].mean()
            win_rate = (taken_df['is_win'] == 1).mean()
            final_equity = 10000 + total_pnl
            total_return = (final_equity - 10000) / 10000 * 100
        else:
            total_pnl = 0
            avg_pnl = 0
            win_rate = 0
            final_equity = 10000
            total_return = 0

        baseline_pnl = trades_df['pnl'].sum()
        improvement = total_pnl - baseline_pnl

        print(f"\n📊 Results:")
        print(f"   Trades Taken: {len(trades_taken)}/{len(trades_df)} ({len(trades_taken)/len(trades_df)*100:.1f}%)")
        print(f"   Trades Rejected: {len(trades_rejected)}")
        print(f"   Total PnL: ${total_pnl:.2f}")
        print(f"   Avg PnL per Trade: ${avg_pnl:.2f}")
        print(f"   Win Rate: {win_rate:.1%}")
        print(f"   Final Equity: ${final_equity:.2f}")
        print(f"   Total Return: {total_return:+.2f}%")
        print(f"   Improvement vs Baseline: ${improvement:.2f} ({improvement/abs(baseline_pnl)*100:+.1f}%)")

        # Show rejection reasons
        if len(trades_rejected) > 0:
            rejection_counts = {}
            for rej in trades_rejected:
                for reason in rej['reasons']:
                    # Simplify reason
                    if 'confidence' in reason.lower():
                        key = 'Low Confidence'
                    elif 'movement' in reason.lower():
                        key = 'Small Movement'
                    elif 'momentum' in reason.lower():
                        key = 'Negative ROC'
                    else:
                        key = reason[:30]

                    rejection_counts[key] = rejection_counts.get(key, 0) + 1

            print(f"\n   Rejection Reasons:")
            for reason, count in sorted(rejection_counts.items(), key=lambda x: x[1], reverse=True):
                print(f"     • {reason}: {count} trades")

        results.append({
            'config': config['name'],
            'trades_taken': len(trades_taken),
            'trades_rejected': len(trades_rejected),
            'total_pnl': total_pnl,
            'avg_pnl': avg_pnl,
            'win_rate': win_rate,
            'final_equity': final_equity,
            'total_return': total_return,
            'improvement': improvement
        })

    # Summary comparison
    print(f"\n\n{'='*120}")
    print(f"📈 CONFIGURATION COMPARISON")
    print(f"{'='*120}")
    print(f"{'Configuration':<25} {'Trades':<12} {'Total PnL':<15} {'Avg PnL':<12} {'Win Rate':<12} {'Return':<12} {'Improvement':<15}")
    print(f"{'-'*120}")

    for r in results:
        improvement_symbol = "📈" if r['improvement'] > 0 else "📉"
        print(f"{r['config']:<25} {r['trades_taken']:<12} "
              f"${r['total_pnl']:>12.2f}  ${r['avg_pnl']:>10.2f}  {r['win_rate']:>10.1%}  "
              f"{r['total_return']:>10.2f}%  {improvement_symbol}${r['improvement']:>12.2f}")

    # Find best configuration
    best_config = max(results, key=lambda x: x['total_pnl'])
    print(f"\n🏆 BEST CONFIGURATION: {best_config['config']}")
    print(f"   Total PnL: ${best_config['total_pnl']:.2f}")
    print(f"   Win Rate: {best_config['win_rate']:.1%}")
    print(f"   Return: {best_config['total_return']:+.2f}%")
    print(f"   Improvement: ${best_config['improvement']:.2f}\n")

    # Save results
    results_df = pd.DataFrame(results)
    results_df.to_csv('enhanced_strategy_comparison.csv', index=False)
    print(f"💾 Results saved to enhanced_strategy_comparison.csv\n")


def main():
    test_enhanced_strategy()


if __name__ == '__main__':
    main()
