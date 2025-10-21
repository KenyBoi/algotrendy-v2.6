"""
MACD and RSI Testing on Equity Curve
Tests if MACD/RSI on equity can predict when to pause/resume trading
"""

import pandas as pd
import numpy as np
from datetime import datetime
from test_mem_integration import MEMIntegrationTester

class MACDRSIPnLTester:
    """Tests MACD and RSI signals on equity curve"""

    def __init__(self, equity_df: pd.DataFrame):
        self.equity_df = equity_df.copy()
        self.equity_df.set_index('timestamp', inplace=True)

    def calculate_macd(self, prices: pd.Series, fast=12, slow=26, signal=9) -> pd.DataFrame:
        """Calculate MACD indicator"""
        ema_fast = prices.ewm(span=fast, adjust=False).mean()
        ema_slow = prices.ewm(span=slow, adjust=False).mean()
        macd_line = ema_fast - ema_slow
        signal_line = macd_line.ewm(span=signal, adjust=False).mean()
        histogram = macd_line - signal_line

        return pd.DataFrame({
            'macd': macd_line,
            'signal': signal_line,
            'histogram': histogram
        })

    def calculate_rsi(self, prices: pd.Series, period=14) -> pd.Series:
        """Calculate RSI indicator"""
        delta = prices.diff()
        gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()

        rs = gain / loss
        rsi = 100 - (100 / (1 + rs))
        return rsi

    def detect_macd_signals(self, macd_df: pd.DataFrame) -> pd.DataFrame:
        """Detect MACD crossover signals"""
        signals = pd.DataFrame(index=macd_df.index)

        # Crossovers
        signals['macd_bullish_cross'] = (
            (macd_df['macd'] > macd_df['signal']) &
            (macd_df['macd'].shift(1) <= macd_df['signal'].shift(1))
        )

        signals['macd_bearish_cross'] = (
            (macd_df['macd'] < macd_df['signal']) &
            (macd_df['macd'].shift(1) >= macd_df['signal'].shift(1))
        )

        # Divergence detection (simplified)
        signals['histogram_increasing'] = macd_df['histogram'] > macd_df['histogram'].shift(1)
        signals['histogram_decreasing'] = macd_df['histogram'] < macd_df['histogram'].shift(1)

        # Trend strength
        signals['macd_above_zero'] = macd_df['macd'] > 0
        signals['histogram_above_zero'] = macd_df['histogram'] > 0

        return signals

    def detect_rsi_signals(self, rsi: pd.Series) -> pd.DataFrame:
        """Detect RSI signals"""
        signals = pd.DataFrame(index=rsi.index)

        # Overbought/Oversold
        signals['rsi_overbought'] = rsi > 70
        signals['rsi_oversold'] = rsi < 30
        signals['rsi_neutral'] = (rsi >= 40) & (rsi <= 60)

        # Extreme levels
        signals['rsi_extreme_overbought'] = rsi > 80
        signals['rsi_extreme_oversold'] = rsi < 20

        # Crossovers
        signals['rsi_crosses_above_50'] = (rsi > 50) & (rsi.shift(1) <= 50)
        signals['rsi_crosses_below_50'] = (rsi < 50) & (rsi.shift(1) >= 50)

        # Divergence zones
        signals['rsi_bullish_zone'] = (rsi >= 50) & (rsi < 70)
        signals['rsi_bearish_zone'] = (rsi > 30) & (rsi <= 50)

        return signals

    def generate_trading_decisions(self, macd_signals: pd.DataFrame, rsi_signals: pd.DataFrame) -> pd.DataFrame:
        """Generate trading decisions based on MACD + RSI"""
        decisions = pd.DataFrame(index=macd_signals.index)

        # Strong TRADE signals (both agree)
        decisions['strong_go'] = (
            (macd_signals['macd_bullish_cross'] | macd_signals['histogram_above_zero']) &
            (rsi_signals['rsi_bullish_zone'] | rsi_signals['rsi_crosses_above_50'])
        )

        decisions['strong_stop'] = (
            (macd_signals['macd_bearish_cross'] | ~macd_signals['histogram_above_zero']) &
            (rsi_signals['rsi_bearish_zone'] | rsi_signals['rsi_crosses_below_50'])
        )

        # Moderate signals
        decisions['moderate_go'] = (
            macd_signals['histogram_increasing'] &
            ~rsi_signals['rsi_overbought']
        )

        decisions['moderate_stop'] = (
            macd_signals['histogram_decreasing'] &
            ~rsi_signals['rsi_oversold']
        )

        # Extreme caution signals
        decisions['extreme_caution'] = (
            rsi_signals['rsi_extreme_overbought'] |
            rsi_signals['rsi_extreme_oversold']
        )

        # Overall recommendation
        decisions['action'] = 'NEUTRAL'
        decisions.loc[decisions['strong_go'], 'action'] = 'TRADE_AGGRESSIVELY'
        decisions.loc[decisions['moderate_go'], 'action'] = 'TRADE_NORMALLY'
        decisions.loc[decisions['moderate_stop'], 'action'] = 'REDUCE_RISK'
        decisions.loc[decisions['strong_stop'], 'action'] = 'PAUSE_TRADING'
        decisions.loc[decisions['extreme_caution'], 'action'] = 'EXTREME_CAUTION'

        return decisions

    def backtest_with_signals(self, decisions: pd.DataFrame, original_trades: list) -> dict:
        """Simulate what would happen if we followed MACD/RSI signals"""

        # Create a map of timestamps to actions
        action_map = decisions['action'].to_dict()

        # Categorize original trades by what action was recommended
        trades_by_action = {
            'TRADE_AGGRESSIVELY': [],
            'TRADE_NORMALLY': [],
            'REDUCE_RISK': [],
            'PAUSE_TRADING': [],
            'EXTREME_CAUTION': [],
            'NEUTRAL': []
        }

        for trade in original_trades:
            # Find closest action recommendation before trade exit
            exit_time = trade['exit_time']

            # Get action at that time (or closest before)
            closest_action = 'NEUTRAL'
            for ts, action in action_map.items():
                if ts <= exit_time:
                    closest_action = action

            trades_by_action[closest_action].append(trade)

        # Calculate performance by action
        results = {}
        for action, trades in trades_by_action.items():
            if not trades:
                results[action] = {
                    'count': 0,
                    'total_pnl': 0,
                    'avg_pnl': 0,
                    'win_rate': 0
                }
                continue

            pnls = [t['pnl'] for t in trades]
            wins = [p for p in pnls if p > 0]

            results[action] = {
                'count': len(trades),
                'total_pnl': sum(pnls),
                'avg_pnl': np.mean(pnls),
                'win_rate': (len(wins) / len(trades) * 100) if trades else 0,
                'best_trade': max(pnls),
                'worst_trade': min(pnls)
            }

        return results

    def analyze(self, original_trades: list) -> str:
        """Complete MACD/RSI analysis"""
        equity = self.equity_df['equity']

        # Calculate indicators
        macd_df = self.calculate_macd(equity)
        rsi = self.calculate_rsi(equity)

        # Detect signals
        macd_signals = self.detect_macd_signals(macd_df)
        rsi_signals = self.detect_rsi_signals(rsi)

        # Generate decisions
        decisions = self.generate_trading_decisions(macd_signals, rsi_signals)

        # Backtest
        backtest_results = self.backtest_with_signals(decisions, original_trades)

        # Build report
        report = []
        report.append("="*70)
        report.append("📊 MACD + RSI EQUITY ANALYSIS")
        report.append("="*70)
        report.append("")

        # Current state
        latest = self.equity_df.iloc[-1]
        latest_macd = macd_df.iloc[-1]
        latest_rsi = rsi.iloc[-1]
        latest_decision = decisions.iloc[-1]

        report.append("📈 CURRENT EQUITY STATE")
        report.append("-"*70)
        report.append(f"Equity:            ${latest['equity']:,.2f}")
        report.append(f"Peak:              ${self.equity_df['equity'].max():,.2f}")
        report.append(f"Drawdown:          {((latest['equity'] - self.equity_df['equity'].max()) / self.equity_df['equity'].max() * 100):.2f}%")
        report.append("")

        report.append("📊 MACD INDICATORS")
        report.append("-"*70)
        report.append(f"MACD Line:         {latest_macd['macd']:.2f}")
        report.append(f"Signal Line:       {latest_macd['signal']:.2f}")
        report.append(f"Histogram:         {latest_macd['histogram']:.2f}")

        if latest_macd['macd'] > latest_macd['signal']:
            report.append(f"Status:            🟢 BULLISH (MACD > Signal)")
        else:
            report.append(f"Status:            🔴 BEARISH (MACD < Signal)")

        if abs(latest_macd['histogram']) > 5:
            report.append(f"Strength:          💪 STRONG momentum")
        elif abs(latest_macd['histogram']) > 2:
            report.append(f"Strength:          ⚡ MODERATE momentum")
        else:
            report.append(f"Strength:          😐 WEAK momentum")

        report.append("")

        report.append("📊 RSI INDICATORS")
        report.append("-"*70)
        report.append(f"RSI(14):           {latest_rsi:.1f}")

        if latest_rsi > 70:
            report.append(f"Status:            ⚠️  OVERBOUGHT - Account may pullback")
        elif latest_rsi < 30:
            report.append(f"Status:            ⚠️  OVERSOLD - Account may recover")
        elif latest_rsi > 50:
            report.append(f"Status:            🟢 BULLISH zone (50-70)")
        else:
            report.append(f"Status:            🔴 BEARISH zone (30-50)")

        # Interpretation
        if latest_rsi > 60:
            report.append(f"Interpretation:    Account performing ABOVE average")
        elif latest_rsi < 40:
            report.append(f"Interpretation:    Account performing BELOW average")
        else:
            report.append(f"Interpretation:    Account performing at AVERAGE levels")

        report.append("")

        report.append("🎯 TRADING RECOMMENDATION")
        report.append("-"*70)

        action = latest_decision['action']
        action_icon = {
            'TRADE_AGGRESSIVELY': '🚀',
            'TRADE_NORMALLY': '✅',
            'REDUCE_RISK': '⚠️',
            'PAUSE_TRADING': '🛑',
            'EXTREME_CAUTION': '🚨',
            'NEUTRAL': '😐'
        }.get(action, '•')

        report.append(f"{action_icon} Action: {action}")
        report.append("")

        # Explanation
        if action == 'TRADE_AGGRESSIVELY':
            report.append("📝 Why: Both MACD and RSI showing bullish signals")
            report.append("   → Consider INCREASING position sizes")
            report.append("   → Take more trades")
            report.append("   → Strategy performing well")
        elif action == 'TRADE_NORMALLY':
            report.append("📝 Why: Moderate positive signals")
            report.append("   → Continue current position sizing")
            report.append("   → No special adjustments needed")
        elif action == 'REDUCE_RISK':
            report.append("📝 Why: Negative momentum detected")
            report.append("   → REDUCE position sizes by 50%")
            report.append("   → Increase confidence threshold")
            report.append("   → Take fewer trades")
        elif action == 'PAUSE_TRADING':
            report.append("📝 Why: Strong bearish signals on equity")
            report.append("   → ⛔ STOP TAKING NEW TRADES")
            report.append("   → Review strategy performance")
            report.append("   → Wait for momentum to improve")
        elif action == 'EXTREME_CAUTION':
            report.append("📝 Why: Extreme RSI levels detected")
            report.append("   → 🚨 HIGH RISK CONDITIONS")
            report.append("   → Pause or drastically reduce trading")
            report.append("   → Account may be overextended")

        report.append("")

        # Historical performance by action
        report.append("📊 HISTORICAL PERFORMANCE BY SIGNAL")
        report.append("-"*70)
        report.append(f"{'Action':<25} {'Trades':<8} {'Avg PnL':<12} {'Win Rate':<10} {'Total PnL':<12}")
        report.append("-"*70)

        for action in ['TRADE_AGGRESSIVELY', 'TRADE_NORMALLY', 'NEUTRAL', 'REDUCE_RISK', 'PAUSE_TRADING', 'EXTREME_CAUTION']:
            res = backtest_results[action]
            if res['count'] > 0:
                action_short = action.replace('_', ' ')[:23]
                report.append(
                    f"{action_short:<25} "
                    f"{res['count']:<8} "
                    f"${res['avg_pnl']:<11.2f} "
                    f"{res['win_rate']:<9.1f}% "
                    f"${res['total_pnl']:<11.2f}"
                )

        report.append("")

        # Key insights
        report.append("💡 KEY INSIGHTS")
        report.append("-"*70)

        # Compare aggressive vs pause performance
        aggressive = backtest_results['TRADE_AGGRESSIVELY']
        pause = backtest_results['PAUSE_TRADING']

        if aggressive['count'] > 0 and pause['count'] > 0:
            if aggressive['avg_pnl'] > pause['avg_pnl']:
                diff = aggressive['avg_pnl'] - pause['avg_pnl']
                report.append(f"✅ Trades during AGGRESSIVE signals performed ${diff:.2f} better on average")
            else:
                diff = pause['avg_pnl'] - aggressive['avg_pnl']
                report.append(f"⚠️  Trades during PAUSE signals actually performed ${diff:.2f} better!")
                report.append(f"   → Indicator may not be predictive for this strategy")

        # Signal effectiveness
        if aggressive['count'] > 0:
            if aggressive['win_rate'] > 50:
                report.append(f"✅ AGGRESSIVE signals had {aggressive['win_rate']:.1f}% win rate - FOLLOW THEM!")
            else:
                report.append(f"⚠️  AGGRESSIVE signals only had {aggressive['win_rate']:.1f}% win rate - BE CAUTIOUS")

        if pause['count'] > 0:
            if pause['avg_pnl'] < 0:
                report.append(f"✅ PAUSE signals correctly identified losing periods (${pause['avg_pnl']:.2f}/trade)")
                report.append(f"   → Pausing during these signals would IMPROVE performance")
            else:
                report.append(f"⚠️  PAUSE signals had positive PnL (${pause['avg_pnl']:.2f}/trade)")
                report.append(f"   → Pausing might miss profitable trades")

        report.append("")

        # Final recommendation
        report.append("🎯 STRATEGY ADJUSTMENT")
        report.append("-"*70)

        # Calculate if following signals would have improved results
        total_original_pnl = sum(t['pnl'] for t in original_trades)

        # What if we only traded during AGGRESSIVE and NORMAL signals?
        selective_pnl = (
            backtest_results['TRADE_AGGRESSIVELY']['total_pnl'] +
            backtest_results['TRADE_NORMALLY']['total_pnl']
        )
        selective_count = (
            backtest_results['TRADE_AGGRESSIVELY']['count'] +
            backtest_results['TRADE_NORMALLY']['count']
        )

        if selective_count > 0:
            report.append(f"Original Strategy:")
            report.append(f"  Trades: {len(original_trades)}")
            report.append(f"  Total PnL: ${total_original_pnl:.2f}")
            report.append(f"  Avg PnL: ${total_original_pnl/len(original_trades):.2f}/trade")
            report.append("")
            report.append(f"If Only Traded During AGGRESSIVE/NORMAL Signals:")
            report.append(f"  Trades: {selective_count}")
            report.append(f"  Total PnL: ${selective_pnl:.2f}")
            report.append(f"  Avg PnL: ${selective_pnl/selective_count:.2f}/trade")
            report.append("")

            if selective_pnl > total_original_pnl:
                improvement = selective_pnl - total_original_pnl
                report.append(f"✅ RESULT: Following MACD/RSI would have IMPROVED PnL by ${improvement:.2f}")
                report.append(f"   → RECOMMENDATION: USE these signals to filter trades")
            else:
                loss = total_original_pnl - selective_pnl
                report.append(f"❌ RESULT: Following MACD/RSI would have REDUCED PnL by ${loss:.2f}")
                report.append(f"   → RECOMMENDATION: DO NOT use these signals for filtering")

        report.append("")
        report.append("="*70)

        return "\n".join(report)


def main():
    """Run MACD/RSI test on equity curve"""
    print("🚀 Running MACD/RSI Equity Analysis...\n")

    # Run integration test
    tester = MEMIntegrationTester(capital=10000)
    market_data = tester.generate_market_data(days=30)

    print("📊 Running backtest...")
    results = tester.run_backtest(market_data)
    print("\n✅ Backtest complete\n")

    # Analyze with MACD/RSI
    print("📈 Applying MACD and RSI to equity curve...\n")
    analyzer = MACDRSIPnLTester(pd.DataFrame(tester.equity_curve))

    report = analyzer.analyze(tester.trades)
    print(report)


if __name__ == "__main__":
    main()
