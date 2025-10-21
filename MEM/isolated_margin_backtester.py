#!/usr/bin/env python3
"""
Isolated Margin Futures Backtester
Simulates 25x isolated leverage with 1% margin per trade

Key Features:
- 1% margin per trade (max loss per trade)
- 25x leverage on position
- Isolated margin (no liquidation cascade)
- Realistic futures trading simulation
"""

import pandas as pd
import numpy as np
from datetime import datetime
from typing import Dict, List
import logging

from production_mem_strategy import FilterConfig, MEMPrediction

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/root/AlgoTrendy_v2.6/MEM/isolated_margin.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger('IsolatedMargin')


class IsolatedMarginBacktester:
    """
    Backtest strategy with isolated margin futures (25x leverage)

    Setup:
    - 1% of capital as margin per trade
    - 25x leverage = 25% position size
    - Max loss per trade = margin amount (1%)
    - Profits/losses applied to margin, then to capital
    """

    def __init__(
        self,
        initial_capital: float = 10000.0,
        margin_pct: float = 0.01,      # 1% margin per trade
        leverage: int = 25,             # 25x leverage
        data_file: str = 'trade_indicators.csv'
    ):
        self.initial_capital = initial_capital
        self.current_capital = initial_capital
        self.margin_pct = margin_pct
        self.leverage = leverage

        # Load data
        self.data = pd.read_csv(data_file)
        self.data['entry_time'] = pd.to_datetime(self.data['entry_time'])
        self.data['exit_time'] = pd.to_datetime(self.data['exit_time'])

        # Configure filters (optimal settings)
        self.config = FilterConfig(
            min_confidence=0.72,
            min_movement_pct=5.0,
            use_roc_filter=False
        )

        # Results tracking
        self.trades = []
        self.equity_curve = [initial_capital]

        logger.info("="*80)
        logger.info("🚀 ISOLATED MARGIN FUTURES BACKTESTER")
        logger.info("="*80)
        logger.info(f"Initial Capital: ${initial_capital:,.2f}")
        logger.info(f"Margin per Trade: {margin_pct:.1%} (${initial_capital * margin_pct:,.2f})")
        logger.info(f"Leverage: {leverage}x isolated")
        logger.info(f"Effective Position: {margin_pct * leverage:.1%} of capital")
        logger.info(f"Strategy: {self.config.min_confidence:.0%} confidence + {self.config.min_movement_pct}% movement")
        logger.info("="*80)

    def check_filters(self, trade_data: pd.Series) -> bool:
        """Check if trade passes filters"""
        # Confidence filter
        if trade_data['mem_confidence'] < self.config.min_confidence:
            return False

        # Movement filter
        if abs(trade_data['pnl_pct']) < self.config.min_movement_pct:
            return False

        return True

    def calculate_pnl(self, trade_data: pd.Series, margin_amount: float) -> Dict:
        """
        Calculate PnL for isolated margin trade

        With isolated margin:
        - Margin: $100 (1% of $10k)
        - Position: $100 * 25 = $2,500
        - If 6% price move: $2,500 * 6% = $150 profit on $100 margin = 150% ROI
        - If liquidated: Lose $100 margin only
        """
        # Position size with leverage
        position_value = margin_amount * self.leverage

        # Calculate PnL based on actual price movement
        price_move_pct = trade_data['pnl_pct']  # Actual historical move

        # PnL on leveraged position
        pnl_usd = position_value * (price_move_pct / 100)

        # ROI on margin
        roi_on_margin = (pnl_usd / margin_amount) * 100

        # Check if liquidation would occur
        # With isolated margin, liquidation happens at 100% loss of margin
        # This occurs when price moves ~4% against position (with 25x leverage)
        liquidation_threshold = 100 / self.leverage  # 4% for 25x

        liquidated = False
        if abs(price_move_pct) >= liquidation_threshold and pnl_usd < 0:
            # Liquidation - lose entire margin
            pnl_usd = -margin_amount
            roi_on_margin = -100
            liquidated = True

        return {
            'margin': margin_amount,
            'position_value': position_value,
            'price_move_pct': price_move_pct,
            'pnl_usd': pnl_usd,
            'roi_on_margin': roi_on_margin,
            'liquidated': liquidated
        }

    def run_backtest(self):
        """Run complete backtest"""
        logger.info("\n🔄 Running backtest...\n")

        trade_num = 0
        signals_evaluated = 0
        signals_rejected = 0

        for idx, trade_data in self.data.iterrows():
            signals_evaluated += 1

            # Check filters
            if not self.check_filters(trade_data):
                signals_rejected += 1
                continue

            # Trade accepted
            trade_num += 1

            # Calculate margin based on current capital
            margin_amount = self.current_capital * self.margin_pct

            # Calculate PnL
            result = self.calculate_pnl(trade_data, margin_amount)

            # Update capital
            self.current_capital += result['pnl_usd']
            self.equity_curve.append(self.current_capital)

            # Record trade
            trade_record = {
                'trade_num': trade_num,
                'timestamp': trade_data['entry_time'],
                'symbol': 'BTCUSDT',
                'side': trade_data['side'],
                'confidence': trade_data['mem_confidence'],
                'entry_price': trade_data['entry_price'],
                'exit_price': trade_data['exit_price'],
                'margin': result['margin'],
                'position_value': result['position_value'],
                'leverage': self.leverage,
                'price_move_pct': result['price_move_pct'],
                'pnl_usd': result['pnl_usd'],
                'roi_on_margin': result['roi_on_margin'],
                'liquidated': result['liquidated'],
                'capital_after': self.current_capital,
                'total_return': ((self.current_capital - self.initial_capital) / self.initial_capital) * 100
            }

            self.trades.append(trade_record)

            # Log trade
            status = "💀 LIQUIDATED" if result['liquidated'] else ("✅ WIN" if result['pnl_usd'] > 0 else "❌ LOSS")
            logger.info(f"{status} Trade #{trade_num}: {trade_data['side']}")
            logger.info(f"   Confidence: {trade_data['mem_confidence']:.1%}")
            logger.info(f"   Margin: ${result['margin']:.2f} → Position: ${result['position_value']:.2f} (25x)")
            logger.info(f"   Price Move: {result['price_move_pct']:+.2f}%")
            logger.info(f"   PnL: ${result['pnl_usd']:+.2f} (ROI: {result['roi_on_margin']:+.1f}%)")
            logger.info(f"   Capital: ${self.current_capital:,.2f} (Total: {trade_record['total_return']:+.2f}%)")
            logger.info("")

        logger.info("="*80)
        logger.info(f"✅ Backtest Complete")
        logger.info(f"   Signals Evaluated: {signals_evaluated}")
        logger.info(f"   Signals Accepted: {trade_num} ({trade_num/signals_evaluated*100:.1f}%)")
        logger.info(f"   Signals Rejected: {signals_rejected} ({signals_rejected/signals_evaluated*100:.1f}%)")
        logger.info("="*80)

    def print_results(self):
        """Print comprehensive results"""
        if not self.trades:
            logger.info("No trades executed")
            return

        df = pd.DataFrame(self.trades)

        # Basic stats
        total_trades = len(df)
        wins = len(df[df['pnl_usd'] > 0])
        losses = len(df[df['pnl_usd'] < 0])
        liquidations = len(df[df['liquidated'] == True])
        win_rate = (wins / total_trades) * 100

        # PnL stats
        total_pnl = df['pnl_usd'].sum()
        avg_pnl = df['pnl_usd'].mean()
        best_trade = df.loc[df['pnl_usd'].idxmax()]
        worst_trade = df.loc[df['pnl_usd'].idxmin()]

        # Return stats
        total_return = ((self.current_capital - self.initial_capital) / self.initial_capital) * 100
        avg_roi_per_trade = df['roi_on_margin'].mean()

        # Risk metrics
        returns = df['pnl_usd'] / df['margin'] * 100
        sharpe = (returns.mean() / returns.std()) * np.sqrt(len(returns)) if len(returns) > 1 and returns.std() > 0 else 0
        max_drawdown = self._calculate_max_drawdown()

        # Time period
        days = (df['timestamp'].max() - df['timestamp'].min()).days
        monthly_return = total_return / (days / 30) if days > 0 else 0

        logger.info("\n" + "="*80)
        logger.info("📊 ISOLATED MARGIN BACKTEST RESULTS")
        logger.info("="*80)

        logger.info(f"\n💰 Capital Performance:")
        logger.info(f"   Initial Capital: ${self.initial_capital:,.2f}")
        logger.info(f"   Final Capital: ${self.current_capital:,.2f}")
        logger.info(f"   Total PnL: ${total_pnl:+,.2f}")
        logger.info(f"   Total Return: {total_return:+.2f}%")
        logger.info(f"   Monthly Return: {monthly_return:+.2f}%")
        logger.info(f"   Annualized Return: {monthly_return * 12:+.1f}%")

        logger.info(f"\n📈 Trading Performance:")
        logger.info(f"   Total Trades: {total_trades}")
        logger.info(f"   Wins: {wins} ({win_rate:.1f}%)")
        logger.info(f"   Losses: {losses} ({(losses/total_trades)*100:.1f}%)")
        logger.info(f"   Liquidations: {liquidations}")
        logger.info(f"   Avg PnL per Trade: ${avg_pnl:+.2f}")
        logger.info(f"   Avg ROI on Margin: {avg_roi_per_trade:+.1f}%")

        logger.info(f"\n🎯 Best/Worst Trades:")
        logger.info(f"   Best: Trade #{int(best_trade['trade_num'])} - ${best_trade['pnl_usd']:+.2f} ({best_trade['roi_on_margin']:+.1f}% ROI)")
        logger.info(f"   Worst: Trade #{int(worst_trade['trade_num'])} - ${worst_trade['pnl_usd']:+.2f} ({worst_trade['roi_on_margin']:+.1f}% ROI)")

        logger.info(f"\n📊 Risk Metrics:")
        logger.info(f"   Sharpe Ratio: {sharpe:.2f}")
        logger.info(f"   Max Drawdown: {max_drawdown:.2f}%")
        logger.info(f"   Max Loss per Trade: ${df['pnl_usd'].min():.2f} ({df['pnl_usd'].min()/self.initial_capital*100:.2f}%)")

        logger.info(f"\n⚙️  Configuration:")
        logger.info(f"   Margin per Trade: {self.margin_pct:.1%}")
        logger.info(f"   Leverage: {self.leverage}x isolated")
        logger.info(f"   Effective Position: {self.margin_pct * self.leverage:.1%}")
        logger.info(f"   Max Risk per Trade: {self.margin_pct:.1%} (isolated)")

        logger.info("\n" + "="*80)

        # Save results
        df.to_csv('isolated_margin_trades.csv', index=False)
        logger.info(f"💾 Results saved to isolated_margin_trades.csv")
        logger.info("="*80)

    def _calculate_max_drawdown(self) -> float:
        """Calculate maximum drawdown"""
        equity = np.array(self.equity_curve)
        peak = np.maximum.accumulate(equity)
        drawdown = ((equity - peak) / peak) * 100
        return drawdown.min()

    def get_summary_stats(self) -> Dict:
        """Get summary statistics"""
        if not self.trades:
            return {}

        df = pd.DataFrame(self.trades)

        return {
            'total_trades': len(df),
            'win_rate': (len(df[df['pnl_usd'] > 0]) / len(df)) * 100,
            'total_pnl': df['pnl_usd'].sum(),
            'total_return': ((self.current_capital - self.initial_capital) / self.initial_capital) * 100,
            'avg_pnl': df['pnl_usd'].mean(),
            'avg_roi': df['roi_on_margin'].mean(),
            'liquidations': len(df[df['liquidated'] == True]),
            'final_capital': self.current_capital
        }


def main():
    """Run isolated margin backtest"""
    # Run backtest
    backtester = IsolatedMarginBacktester(
        initial_capital=10000.0,
        margin_pct=0.01,    # 1% margin per trade
        leverage=25         # 25x isolated leverage
    )

    backtester.run_backtest()
    backtester.print_results()

    # Get summary
    stats = backtester.get_summary_stats()

    if stats:
        print("\n" + "="*80)
        print("🎯 SUMMARY FOR YOUR 25x ISOLATED MARGIN SETUP")
        print("="*80)
        print(f"\nWith 1% margin per trade + 25x isolated leverage:")
        print(f"  ✅ Total Return: {stats['total_return']:+.2f}%")
        print(f"  ✅ Win Rate: {stats['win_rate']:.1f}%")
        print(f"  ✅ Avg PnL per Trade: ${stats['avg_pnl']:+.2f}")
        print(f"  ✅ Avg ROI on Margin: {stats['avg_roi']:+.1f}%")
        print(f"  ✅ Liquidations: {stats['liquidations']}")
        print(f"\nFinal Capital: ${stats['final_capital']:,.2f}")
        print("="*80)


if __name__ == '__main__':
    main()
