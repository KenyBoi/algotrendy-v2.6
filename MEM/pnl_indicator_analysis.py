"""
PnL Indicator Analysis
Applies technical indicators to equity curve and trade PnL for insights
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from datetime import datetime, timedelta
from typing import Dict, List, Tuple
import sys

# Import our components
from test_mem_integration import MEMIntegrationTester

class PnLIndicatorAnalysis:
    """Analyzes PnL using technical indicators"""

    def __init__(self, trades: List[Dict], equity_curve: List[Dict]):
        """
        Args:
            trades: List of trade dictionaries
            equity_curve: List of equity point dictionaries
        """
        self.trades_df = pd.DataFrame(trades)
        self.equity_df = pd.DataFrame(equity_curve)
        self.equity_df.set_index('timestamp', inplace=True)

    def calculate_equity_indicators(self) -> pd.DataFrame:
        """Apply technical indicators to equity curve"""
        df = self.equity_df.copy()

        # Treat equity like a price chart
        equity = df['equity']

        # 1. Moving Averages on Equity
        df['sma_10'] = equity.rolling(10).mean()
        df['sma_20'] = equity.rolling(20).mean()
        df['sma_50'] = equity.rolling(50).mean()
        df['ema_10'] = equity.ewm(span=10, adjust=False).mean()
        df['ema_20'] = equity.ewm(span=20, adjust=False).mean()

        # 2. Equity Momentum
        df['momentum_10'] = equity - equity.shift(10)
        df['momentum_20'] = equity - equity.shift(20)
        df['roc_10'] = ((equity / equity.shift(10)) - 1) * 100  # Rate of change

        # 3. Equity Volatility
        returns = equity.pct_change()
        df['volatility_10'] = returns.rolling(10).std() * np.sqrt(24)  # Hourly to daily
        df['volatility_20'] = returns.rolling(20).std() * np.sqrt(24)

        # 4. Equity RSI
        df['rsi_14'] = self._calculate_rsi(equity, 14)

        # 5. Equity MACD
        ema_12 = equity.ewm(span=12, adjust=False).mean()
        ema_26 = equity.ewm(span=26, adjust=False).mean()
        df['macd'] = ema_12 - ema_26
        df['macd_signal'] = df['macd'].ewm(span=9, adjust=False).mean()
        df['macd_histogram'] = df['macd'] - df['macd_signal']

        # 6. Equity Bollinger Bands
        sma_20 = equity.rolling(20).mean()
        std_20 = equity.rolling(20).std()
        df['bb_upper'] = sma_20 + (2 * std_20)
        df['bb_middle'] = sma_20
        df['bb_lower'] = sma_20 - (2 * std_20)
        df['bb_width'] = df['bb_upper'] - df['bb_lower']

        # 7. Drawdown Indicators
        peak = equity.expanding().max()
        df['peak_equity'] = peak
        df['drawdown_pct'] = ((equity - peak) / peak) * 100
        df['underwater_time'] = (equity < peak).astype(int).groupby((equity >= peak).cumsum()).cumsum()

        # 8. Profit/Loss Streaks
        df['is_profitable'] = (equity > equity.shift(1)).astype(int)
        df['profit_streak'] = df['is_profitable'].groupby((df['is_profitable'] != df['is_profitable'].shift()).cumsum()).cumsum()

        return df

    def calculate_trade_indicators(self) -> pd.DataFrame:
        """Apply indicators to trade PnL series"""
        if len(self.trades_df) == 0:
            return pd.DataFrame()

        df = self.trades_df.copy()
        df.set_index('exit_time', inplace=True)
        df = df.sort_index()

        # 1. Cumulative PnL
        df['cumulative_pnl'] = df['pnl'].cumsum()
        df['cumulative_pnl_pct'] = ((df['pnl'].cumsum() / 10000) * 100)  # Assuming $10k initial

        # 2. Win/Loss Streaks
        df['is_win'] = (df['pnl'] > 0).astype(int)
        df['is_loss'] = (df['pnl'] <= 0).astype(int)

        # Win streak
        df['win_streak'] = 0
        df['loss_streak'] = 0

        win_count = 0
        loss_count = 0
        win_streaks = []
        loss_streaks = []

        for idx, row in df.iterrows():
            if row['is_win']:
                win_count += 1
                loss_count = 0
            else:
                loss_count += 1
                win_count = 0

            win_streaks.append(win_count)
            loss_streaks.append(loss_count)

        df['win_streak'] = win_streaks
        df['loss_streak'] = loss_streaks

        # 3. Moving Averages of PnL
        df['pnl_sma_5'] = df['pnl'].rolling(5).mean()
        df['pnl_sma_10'] = df['pnl'].rolling(10).mean()
        df['pnl_ema_5'] = df['pnl'].ewm(span=5, adjust=False).mean()

        # 4. PnL Volatility
        df['pnl_volatility_5'] = df['pnl'].rolling(5).std()
        df['pnl_volatility_10'] = df['pnl'].rolling(10).std()

        # 5. Rolling Win Rate
        df['rolling_win_rate_5'] = df['is_win'].rolling(5).mean() * 100
        df['rolling_win_rate_10'] = df['is_win'].rolling(10).mean() * 100

        # 6. Rolling Profit Factor
        def calc_profit_factor(wins, losses):
            total_wins = wins[wins > 0].sum()
            total_losses = abs(losses[losses <= 0].sum())
            return total_wins / total_losses if total_losses > 0 else 0

        df['rolling_pf_10'] = df['pnl'].rolling(10).apply(
            lambda x: calc_profit_factor(x, x)
        )

        # 7. Expectancy
        df['expectancy_10'] = df['pnl'].rolling(10).mean()

        # 8. Max Favorable/Adverse Excursion (simplified)
        df['mae'] = df['pnl_pct'].apply(lambda x: min(0, x))  # Max Adverse
        df['mfe'] = df['pnl_pct'].apply(lambda x: max(0, x))  # Max Favorable

        return df

    def _calculate_rsi(self, series: pd.Series, period: int = 14) -> pd.Series:
        """Calculate RSI"""
        delta = series.diff()
        gain = (delta.where(delta > 0, 0)).rolling(window=period).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(window=period).mean()

        rs = gain / loss
        rsi = 100 - (100 / (1 + rs))
        return rsi

    def detect_equity_signals(self, equity_indicators: pd.DataFrame) -> Dict:
        """Detect trading signals on equity curve"""
        latest = equity_indicators.iloc[-1]

        signals = {
            'timestamp': latest.name,
            'equity': latest['equity'],
            'signals': []
        }

        # 1. Trend Signals (MA crossovers)
        if not pd.isna(latest['sma_10']) and not pd.isna(latest['sma_20']):
            if latest['sma_10'] > latest['sma_20']:
                signals['signals'].append({
                    'type': 'BULLISH_TREND',
                    'indicator': 'SMA Crossover',
                    'description': 'Equity SMA(10) > SMA(20) - Account growing'
                })
            else:
                signals['signals'].append({
                    'type': 'BEARISH_TREND',
                    'indicator': 'SMA Crossover',
                    'description': 'Equity SMA(10) < SMA(20) - Account declining'
                })

        # 2. RSI Signals
        if not pd.isna(latest['rsi_14']):
            if latest['rsi_14'] > 70:
                signals['signals'].append({
                    'type': 'OVERBOUGHT',
                    'indicator': f'RSI({latest["rsi_14"]:.1f})',
                    'description': 'Equity RSI > 70 - May be due for pullback'
                })
            elif latest['rsi_14'] < 30:
                signals['signals'].append({
                    'type': 'OVERSOLD',
                    'indicator': f'RSI({latest["rsi_14"]:.1f})',
                    'description': 'Equity RSI < 30 - Account may recover'
                })

        # 3. MACD Signals
        if not pd.isna(latest['macd']) and not pd.isna(latest['macd_signal']):
            if latest['macd'] > latest['macd_signal'] and latest['macd_histogram'] > 0:
                signals['signals'].append({
                    'type': 'MACD_BULLISH',
                    'indicator': 'MACD',
                    'description': 'MACD above signal - Positive momentum'
                })
            elif latest['macd'] < latest['macd_signal'] and latest['macd_histogram'] < 0:
                signals['signals'].append({
                    'type': 'MACD_BEARISH',
                    'indicator': 'MACD',
                    'description': 'MACD below signal - Negative momentum'
                })

        # 4. Bollinger Band Signals
        if not pd.isna(latest['bb_upper']) and not pd.isna(latest['bb_lower']):
            if latest['equity'] > latest['bb_upper']:
                signals['signals'].append({
                    'type': 'BB_OVERBOUGHT',
                    'indicator': 'Bollinger Bands',
                    'description': 'Equity above upper BB - Exceptional performance'
                })
            elif latest['equity'] < latest['bb_lower']:
                signals['signals'].append({
                    'type': 'BB_OVERSOLD',
                    'indicator': 'Bollinger Bands',
                    'description': 'Equity below lower BB - Underperforming'
                })

        # 5. Drawdown Alerts
        if not pd.isna(latest['drawdown_pct']):
            if latest['drawdown_pct'] < -10:
                signals['signals'].append({
                    'type': 'HIGH_DRAWDOWN',
                    'indicator': f'Drawdown({latest["drawdown_pct"]:.1f}%)',
                    'description': 'Drawdown > 10% - Risk management alert!'
                })
            elif latest['drawdown_pct'] < -5:
                signals['signals'].append({
                    'type': 'MODERATE_DRAWDOWN',
                    'indicator': f'Drawdown({latest["drawdown_pct"]:.1f}%)',
                    'description': 'Drawdown > 5% - Monitor closely'
                })

        # 6. Volatility Alerts
        if not pd.isna(latest['volatility_20']):
            avg_vol = equity_indicators['volatility_20'].mean()
            if latest['volatility_20'] > avg_vol * 1.5:
                signals['signals'].append({
                    'type': 'HIGH_VOLATILITY',
                    'indicator': f'Volatility({latest["volatility_20"]:.2%})',
                    'description': 'Equity volatility 50% above average - Unstable performance'
                })

        return signals

    def analyze_trade_patterns(self, trade_indicators: pd.DataFrame) -> Dict:
        """Analyze patterns in trade results"""
        if len(trade_indicators) == 0:
            return {}

        latest = trade_indicators.iloc[-1]

        patterns = {
            'streaks': {},
            'performance': {},
            'warnings': []
        }

        # 1. Current Streaks
        patterns['streaks'] = {
            'current_win_streak': int(latest['win_streak']),
            'current_loss_streak': int(latest['loss_streak']),
            'max_win_streak': int(trade_indicators['win_streak'].max()),
            'max_loss_streak': int(trade_indicators['loss_streak'].max())
        }

        # 2. Recent Performance
        last_10 = trade_indicators.tail(10)
        patterns['performance'] = {
            'win_rate_last_10': last_10['is_win'].mean() * 100,
            'avg_pnl_last_10': last_10['pnl'].mean(),
            'profit_factor_last_10': float(latest['rolling_pf_10']) if not pd.isna(latest['rolling_pf_10']) else 0,
            'expectancy_last_10': float(latest['expectancy_10']) if not pd.isna(latest['expectancy_10']) else 0
        }

        # 3. Warnings
        if patterns['streaks']['current_loss_streak'] >= 3:
            patterns['warnings'].append({
                'type': 'LOSS_STREAK',
                'severity': 'HIGH' if patterns['streaks']['current_loss_streak'] >= 5 else 'MEDIUM',
                'message': f"Current loss streak: {patterns['streaks']['current_loss_streak']} trades"
            })

        if patterns['performance']['win_rate_last_10'] < 30:
            patterns['warnings'].append({
                'type': 'LOW_WIN_RATE',
                'severity': 'HIGH',
                'message': f"Win rate last 10 trades: {patterns['performance']['win_rate_last_10']:.1f}%"
            })

        if patterns['performance']['expectancy_last_10'] < 0:
            patterns['warnings'].append({
                'type': 'NEGATIVE_EXPECTANCY',
                'severity': 'CRITICAL',
                'message': f"Negative expectancy last 10 trades: ${patterns['performance']['expectancy_last_10']:.2f}/trade"
            })

        # 4. Trend Analysis
        if len(trade_indicators) >= 10:
            recent_pnl_trend = np.polyfit(range(10), last_10['cumulative_pnl'].values, 1)[0]
            patterns['pnl_trend'] = {
                'direction': 'IMPROVING' if recent_pnl_trend > 0 else 'DETERIORATING',
                'slope': recent_pnl_trend
            }

        return patterns

    def generate_report(self) -> str:
        """Generate comprehensive PnL analysis report"""
        equity_ind = self.calculate_equity_indicators()
        trade_ind = self.calculate_trade_indicators()

        signals = self.detect_equity_signals(equity_ind)
        patterns = self.analyze_trade_patterns(trade_ind)

        # Build report
        report = []
        report.append("="*70)
        report.append("📊 PnL INDICATOR ANALYSIS REPORT")
        report.append("="*70)
        report.append("")

        # 1. Equity Curve Analysis
        report.append("📈 EQUITY CURVE INDICATORS")
        report.append("-"*70)

        latest_equity = equity_ind.iloc[-1]
        report.append(f"Current Equity:    ${latest_equity['equity']:,.2f}")
        report.append(f"Peak Equity:       ${latest_equity['peak_equity']:,.2f}")
        report.append(f"Drawdown:          {latest_equity['drawdown_pct']:.2f}%")
        report.append("")

        if not pd.isna(latest_equity['sma_10']):
            report.append(f"SMA(10):           ${latest_equity['sma_10']:,.2f}")
            report.append(f"SMA(20):           ${latest_equity['sma_20']:,.2f}")
            report.append(f"SMA(50):           ${latest_equity['sma_50']:,.2f}")
        report.append("")

        if not pd.isna(latest_equity['rsi_14']):
            report.append(f"RSI(14):           {latest_equity['rsi_14']:.1f}")

        if not pd.isna(latest_equity['volatility_20']):
            report.append(f"Volatility(20):    {latest_equity['volatility_20']:.2%}")

        if not pd.isna(latest_equity['macd']):
            report.append(f"MACD:              {latest_equity['macd']:.2f}")
            report.append(f"MACD Signal:       {latest_equity['macd_signal']:.2f}")
            report.append(f"MACD Histogram:    {latest_equity['macd_histogram']:.2f}")

        report.append("")

        # 2. Equity Signals
        report.append("🚦 EQUITY SIGNALS")
        report.append("-"*70)

        if signals['signals']:
            for signal in signals['signals']:
                icon = {
                    'BULLISH_TREND': '🟢',
                    'BEARISH_TREND': '🔴',
                    'OVERBOUGHT': '⚠️',
                    'OVERSOLD': '⚠️',
                    'MACD_BULLISH': '🟢',
                    'MACD_BEARISH': '🔴',
                    'BB_OVERBOUGHT': '⚡',
                    'BB_OVERSOLD': '⚡',
                    'HIGH_DRAWDOWN': '🚨',
                    'MODERATE_DRAWDOWN': '⚠️',
                    'HIGH_VOLATILITY': '📊'
                }.get(signal['type'], '•')

                report.append(f"{icon} {signal['indicator']}: {signal['description']}")
        else:
            report.append("No significant signals detected")

        report.append("")

        # 3. Trade Pattern Analysis
        if patterns:
            report.append("🎯 TRADE PATTERN ANALYSIS")
            report.append("-"*70)

            if 'streaks' in patterns:
                streaks = patterns['streaks']
                report.append(f"Current Win Streak:  {streaks['current_win_streak']}")
                report.append(f"Current Loss Streak: {streaks['current_loss_streak']}")
                report.append(f"Max Win Streak:      {streaks['max_win_streak']}")
                report.append(f"Max Loss Streak:     {streaks['max_loss_streak']}")
                report.append("")

            if 'performance' in patterns:
                perf = patterns['performance']
                report.append(f"Win Rate (last 10):     {perf['win_rate_last_10']:.1f}%")
                report.append(f"Avg PnL (last 10):      ${perf['avg_pnl_last_10']:.2f}")
                report.append(f"Profit Factor (last 10):{perf['profit_factor_last_10']:.2f}")
                report.append(f"Expectancy (last 10):   ${perf['expectancy_last_10']:.2f}/trade")
                report.append("")

            if 'pnl_trend' in patterns:
                trend_icon = '📈' if patterns['pnl_trend']['direction'] == 'IMPROVING' else '📉'
                report.append(f"{trend_icon} PnL Trend: {patterns['pnl_trend']['direction']}")
                report.append(f"   Slope: ${patterns['pnl_trend']['slope']:.2f}/trade")
                report.append("")

            # 4. Warnings
            if patterns.get('warnings'):
                report.append("⚠️  WARNINGS")
                report.append("-"*70)
                for warning in patterns['warnings']:
                    severity_icon = {
                        'CRITICAL': '🚨',
                        'HIGH': '🔴',
                        'MEDIUM': '🟡',
                        'LOW': '🟢'
                    }.get(warning['severity'], '⚠️')

                    report.append(f"{severity_icon} {warning['type']}: {warning['message']}")
                report.append("")

        # 5. Trade Statistics with Indicators
        if len(trade_ind) >= 5:
            report.append("📊 TRADE INDICATOR SUMMARY")
            report.append("-"*70)

            latest_trade = trade_ind.iloc[-1]

            if not pd.isna(latest_trade['pnl_sma_5']):
                report.append(f"PnL SMA(5):         ${latest_trade['pnl_sma_5']:.2f}/trade")

            if not pd.isna(latest_trade['pnl_volatility_5']):
                report.append(f"PnL Volatility(5):  ${latest_trade['pnl_volatility_5']:.2f}")

            if not pd.isna(latest_trade['rolling_win_rate_10']):
                report.append(f"Rolling Win Rate:   {latest_trade['rolling_win_rate_10']:.1f}%")

            report.append("")

        # 6. Recommendations
        report.append("💡 RECOMMENDATIONS")
        report.append("-"*70)

        recommendations = []

        # Based on equity trend
        if len(signals['signals']) > 0:
            bearish_signals = sum(1 for s in signals['signals'] if 'BEARISH' in s['type'] or 'DRAWDOWN' in s['type'])
            if bearish_signals >= 2:
                recommendations.append("🔴 Multiple bearish signals detected - Consider reducing position sizes")

        # Based on streaks
        if patterns.get('streaks', {}).get('current_loss_streak', 0) >= 3:
            recommendations.append("🔴 Loss streak detected - Review strategy, reduce risk, or pause trading")

        # Based on performance
        if patterns.get('performance', {}).get('win_rate_last_10', 100) < 40:
            recommendations.append("🟡 Win rate declining - Analyze recent trades for pattern changes")

        if patterns.get('performance', {}).get('expectancy_last_10', 0) < 0:
            recommendations.append("🚨 CRITICAL: Negative expectancy - STOP TRADING and review strategy")

        # Based on volatility
        if not pd.isna(latest_equity.get('volatility_20', 0)):
            avg_vol = equity_ind['volatility_20'].mean()
            if latest_equity['volatility_20'] > avg_vol * 1.5:
                recommendations.append("🟡 High equity volatility - Consider tightening stops or reducing size")

        # Positive signals
        bullish_signals = sum(1 for s in signals['signals'] if 'BULLISH' in s['type'])
        if bullish_signals >= 2 and patterns.get('streaks', {}).get('current_win_streak', 0) >= 2:
            recommendations.append("🟢 Multiple bullish signals + win streak - Strategy performing well")

        if recommendations:
            for rec in recommendations:
                report.append(rec)
        else:
            report.append("✅ No critical issues detected - Continue monitoring")

        report.append("")
        report.append("="*70)

        return "\n".join(report)


def main():
    """Run PnL indicator analysis on integration test results"""
    print("🚀 Running MEM Integration Test with PnL Analysis...\n")

    # Run integration test
    tester = MEMIntegrationTester(capital=10000)
    market_data = tester.generate_market_data(days=30)

    print("📊 Running backtest...")
    results = tester.run_backtest(market_data)
    print("\n✅ Backtest complete\n")

    # Analyze with indicators
    print("📈 Applying indicators to PnL...\n")
    analyzer = PnLIndicatorAnalysis(tester.trades, tester.equity_curve)

    # Generate report
    report = analyzer.generate_report()
    print(report)

    # Calculate and display equity indicators
    equity_ind = analyzer.calculate_equity_indicators()
    trade_ind = analyzer.calculate_trade_indicators()

    # Save detailed results
    print("\n💾 Saving detailed indicator data...")
    equity_ind.to_csv('/root/AlgoTrendy_v2.6/MEM/equity_indicators.csv')
    if len(trade_ind) > 0:
        trade_ind.to_csv('/root/AlgoTrendy_v2.6/MEM/trade_indicators.csv')

    print("✅ Saved to:")
    print("   - equity_indicators.csv")
    print("   - trade_indicators.csv")

    # Show sample of equity indicators
    print("\n📊 Sample Equity Indicators (Last 5 bars):")
    print(equity_ind[['equity', 'sma_10', 'sma_20', 'rsi_14', 'drawdown_pct']].tail())

    if len(trade_ind) > 0:
        print("\n📊 Sample Trade Indicators (Last 5 trades):")
        print(trade_ind[['pnl', 'cumulative_pnl', 'win_streak', 'loss_streak', 'rolling_win_rate_10']].tail())


if __name__ == "__main__":
    main()
