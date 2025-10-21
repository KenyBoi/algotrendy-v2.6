#!/usr/bin/env python3
"""
Paper Trading Engine
Simulates live trading with optimized MEM strategy (72% confidence + 5% movement)

Runs autonomously and tracks all trades in real-time
"""

import pandas as pd
import numpy as np
import json
import time
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Optional
import logging

from production_mem_strategy import (
    ProductionMEMStrategy,
    FilterConfig,
    MEMPrediction,
    TradeSignal
)

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/root/AlgoTrendy_v2.6/MEM/paper_trading.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger('PaperTrading')


class PaperTradingEngine:
    """
    Paper trading engine that simulates live trading

    Features:
    - Real-time market data simulation
    - Optimized MEM strategy execution
    - Trade tracking and reporting
    - Performance monitoring
    - Auto-saves state
    """

    def __init__(
        self,
        initial_capital: float = 10000.0,
        data_file: str = 'trade_indicators.csv',
        state_file: str = 'paper_trading_state.json',
        trades_file: str = 'paper_trades.csv'
    ):
        self.initial_capital = initial_capital
        self.current_capital = initial_capital
        self.data_file = data_file
        self.state_file = state_file
        self.trades_file = trades_file

        # Initialize strategy with optimal config
        config = FilterConfig(
            min_confidence=0.72,
            min_movement_pct=5.0,
            use_roc_filter=False
        )

        self.strategy = ProductionMEMStrategy(
            config=config,
            initial_capital=initial_capital,
            position_size_pct=0.05
        )

        # Load historical data for simulation
        self.historical_data = self._load_historical_data()

        # Trading state
        self.current_position = None
        self.position_entry_time = None
        self.trades_executed = []
        self.current_index = 0
        self.start_time = datetime.now()

        # Load previous state if exists
        self._load_state()

        logger.info("="*80)
        logger.info("📄 PAPER TRADING ENGINE INITIALIZED")
        logger.info("="*80)
        logger.info(f"Initial Capital: ${self.initial_capital:,.2f}")
        logger.info(f"Current Capital: ${self.current_capital:,.2f}")
        logger.info(f"Strategy: 72% Confidence + 5% Movement")
        logger.info(f"Position Size: 5% of capital")
        logger.info(f"Data Points Available: {len(self.historical_data)}")
        logger.info("="*80)

    def _load_historical_data(self) -> pd.DataFrame:
        """Load historical trade data for simulation"""
        df = pd.read_csv(self.data_file)
        df['entry_time'] = pd.to_datetime(df['entry_time'])
        df['exit_time'] = pd.to_datetime(df['exit_time'])
        df = df.sort_values('entry_time')
        return df

    def _load_state(self):
        """Load previous state if exists"""
        if Path(self.state_file).exists():
            try:
                with open(self.state_file, 'r') as f:
                    state = json.load(f)
                    self.current_capital = state.get('current_capital', self.initial_capital)
                    self.current_index = state.get('current_index', 0)
                    self.trades_executed = state.get('trades_executed', [])
                    logger.info(f"✅ Loaded previous state: {len(self.trades_executed)} trades executed")
            except Exception as e:
                logger.warning(f"⚠️  Could not load state: {e}")

    def _save_state(self):
        """Save current state"""
        state = {
            'current_capital': self.current_capital,
            'current_index': self.current_index,
            'trades_executed': self.trades_executed,
            'last_updated': datetime.now().isoformat()
        }

        with open(self.state_file, 'w') as f:
            json.dump(state, f, indent=2)

    def _save_trades(self):
        """Save trades to CSV"""
        if self.trades_executed:
            df = pd.DataFrame(self.trades_executed)
            df.to_csv(self.trades_file, index=False)
            logger.info(f"💾 Saved {len(self.trades_executed)} trades to {self.trades_file}")

    def simulate_mem_prediction(self, trade_data: pd.Series) -> MEMPrediction:
        """
        Simulate MEM prediction from historical trade data
        In production, this would come from actual MEM model
        """
        return MEMPrediction(
            timestamp=trade_data['entry_time'],
            symbol='BTCUSDT',
            action=trade_data['side'],
            confidence=trade_data['mem_confidence'],
            current_price=trade_data['entry_price'],
            predicted_price=trade_data['exit_price'],
            predicted_movement_pct=abs(trade_data['pnl_pct'])
        )

    def execute_paper_trade(self, signal: TradeSignal, trade_data: pd.Series):
        """Execute a paper trade (simulated)"""

        # Record entry
        entry_time = trade_data['entry_time']
        exit_time = trade_data['exit_time']

        # Simulate trade execution
        entry_price = signal.entry_price
        quantity = signal.quantity

        # Use actual historical outcome for simulation
        exit_price = trade_data['exit_price']

        # Calculate PnL
        if signal.action == 'BUY':
            pnl = (exit_price - entry_price) * quantity
        else:  # SELL
            pnl = (entry_price - exit_price) * quantity

        # Update capital
        self.current_capital += pnl

        # Record trade
        trade_record = {
            'trade_number': len(self.trades_executed) + 1,
            'timestamp': datetime.now().isoformat(),
            'entry_time': entry_time.isoformat(),
            'exit_time': exit_time.isoformat(),
            'symbol': 'BTCUSDT',
            'action': signal.action,
            'confidence': signal.confidence,
            'predicted_movement': signal.predicted_movement,
            'entry_price': entry_price,
            'exit_price': exit_price,
            'quantity': quantity,
            'position_value': quantity * entry_price,
            'stop_loss': signal.stop_loss,
            'take_profit': signal.take_profit,
            'pnl': pnl,
            'pnl_pct': (pnl / (quantity * entry_price)) * 100,
            'capital_after': self.current_capital,
            'return_pct': ((self.current_capital - self.initial_capital) / self.initial_capital) * 100,
            'exit_reason': trade_data['exit_reason']
        }

        self.trades_executed.append(trade_record)

        # Update strategy
        self.strategy.record_trade_result(
            signal=signal,
            exit_price=exit_price,
            exit_reason=trade_data['exit_reason'],
            pnl=pnl
        )

        # Log trade
        logger.info("="*80)
        logger.info(f"📊 PAPER TRADE #{trade_record['trade_number']} EXECUTED")
        logger.info("="*80)
        logger.info(f"Action: {signal.action}")
        logger.info(f"Confidence: {signal.confidence:.1%}")
        logger.info(f"Entry: ${entry_price:,.2f}")
        logger.info(f"Exit: ${exit_price:,.2f}")
        logger.info(f"PnL: ${pnl:+.2f} ({trade_record['pnl_pct']:+.2f}%)")
        logger.info(f"Capital: ${self.current_capital:,.2f}")
        logger.info(f"Total Return: {trade_record['return_pct']:+.2f}%")
        logger.info("="*80)

        # Save state
        self._save_state()
        self._save_trades()

    def run_single_iteration(self) -> bool:
        """
        Run one iteration of the trading loop
        Returns True if more data available, False if done
        """
        if self.current_index >= len(self.historical_data):
            logger.info("✅ Reached end of historical data")
            return False

        # Get next trade opportunity
        trade_data = self.historical_data.iloc[self.current_index]

        # Simulate MEM prediction
        prediction = self.simulate_mem_prediction(trade_data)

        # Evaluate through strategy filters
        signal = self.strategy.evaluate_signal(prediction)

        if signal.should_trade:
            # Execute paper trade
            self.execute_paper_trade(signal, trade_data)
        else:
            logger.info(f"⏭️  Trade {self.current_index + 1} skipped: {signal.rejection_reason}")

        self.current_index += 1
        return True

    def run_continuous(self, delay_seconds: float = 1.0):
        """
        Run paper trading continuously

        Args:
            delay_seconds: Seconds to wait between iterations
        """
        logger.info("🚀 Starting continuous paper trading...")

        try:
            while True:
                has_more = self.run_single_iteration()

                if not has_more:
                    logger.info("✅ Paper trading simulation complete!")
                    self.print_final_report()
                    break

                # Wait before next iteration
                time.sleep(delay_seconds)

        except KeyboardInterrupt:
            logger.info("\n⏸️  Paper trading stopped by user")
            self.print_final_report()
        except Exception as e:
            logger.error(f"❌ Error in paper trading: {e}")
            raise

    def run_batch(self, num_trades: int = 10):
        """
        Run a batch of trades then stop

        Args:
            num_trades: Number of trades to process
        """
        logger.info(f"🚀 Running batch of {num_trades} trade opportunities...")

        trades_processed = 0

        while trades_processed < num_trades:
            has_more = self.run_single_iteration()

            if not has_more:
                logger.info("✅ Reached end of data")
                break

            trades_processed += 1

        self.print_current_stats()

    def print_current_stats(self):
        """Print current performance statistics"""
        stats = self.strategy.get_performance_stats()

        logger.info("\n" + "="*80)
        logger.info("📊 CURRENT PERFORMANCE")
        logger.info("="*80)
        logger.info(f"Opportunities Evaluated: {stats['total_signals']}")
        logger.info(f"Trades Executed: {stats['trades']}")
        logger.info(f"Acceptance Rate: {stats['acceptance_rate']:.1f}%")

        if stats['trades'] > 0:
            logger.info(f"\nTrading Performance:")
            logger.info(f"  Total PnL: ${stats['total_pnl']:,.2f}")
            logger.info(f"  Avg PnL/Trade: ${stats['avg_pnl']:,.2f}")
            logger.info(f"  Win Rate: {stats['win_rate']:.1f}% ({stats['wins']}W/{stats['losses']}L)")
            logger.info(f"\nCapital:")
            logger.info(f"  Initial: ${self.initial_capital:,.2f}")
            logger.info(f"  Current: ${self.current_capital:,.2f}")
            logger.info(f"  Return: {stats['total_return']:+.2f}%")

        logger.info("="*80 + "\n")

    def print_final_report(self):
        """Print comprehensive final report"""
        self.strategy.print_performance_report()

        if self.trades_executed:
            logger.info("\n" + "="*80)
            logger.info("📈 PAPER TRADING COMPLETE - FINAL RESULTS")
            logger.info("="*80)

            df = pd.DataFrame(self.trades_executed)

            logger.info(f"\nTrade Summary:")
            logger.info(f"  Total Opportunities: {len(self.historical_data)}")
            logger.info(f"  Trades Executed: {len(df)}")
            logger.info(f"  Trades Rejected: {len(self.historical_data) - len(df)}")
            logger.info(f"  Acceptance Rate: {len(df)/len(self.historical_data)*100:.1f}%")

            wins = len(df[df['pnl'] > 0])
            losses = len(df[df['pnl'] < 0])

            logger.info(f"\nPerformance:")
            logger.info(f"  Wins: {wins}")
            logger.info(f"  Losses: {losses}")
            logger.info(f"  Win Rate: {wins/(wins+losses)*100:.1f}%")
            logger.info(f"  Total PnL: ${df['pnl'].sum():,.2f}")
            logger.info(f"  Avg PnL: ${df['pnl'].mean():,.2f}")
            logger.info(f"  Best Trade: ${df['pnl'].max():,.2f}")
            logger.info(f"  Worst Trade: ${df['pnl'].min():,.2f}")

            logger.info(f"\nCapital:")
            logger.info(f"  Initial: ${self.initial_capital:,.2f}")
            logger.info(f"  Final: ${self.current_capital:,.2f}")
            logger.info(f"  Total Return: {((self.current_capital - self.initial_capital) / self.initial_capital) * 100:+.2f}%")

            logger.info(f"\nRisk Metrics:")
            returns = df['pnl'] / df['position_value'] * 100
            sharpe = (returns.mean() / returns.std()) * np.sqrt(len(returns)) if len(returns) > 1 else 0
            logger.info(f"  Sharpe Ratio: {sharpe:.2f}")
            logger.info(f"  Max Single Loss: ${df['pnl'].min():,.2f}")

            logger.info(f"\nFiles Saved:")
            logger.info(f"  Trades: {self.trades_file}")
            logger.info(f"  State: {self.state_file}")
            logger.info(f"  Logs: paper_trading.log")

            logger.info("="*80)


def main():
    """Main entry point for paper trading"""
    import sys

    engine = PaperTradingEngine(initial_capital=10000.0)

    if len(sys.argv) > 1:
        if sys.argv[1] == 'continuous':
            # Run continuously with delay
            delay = float(sys.argv[2]) if len(sys.argv) > 2 else 1.0
            engine.run_continuous(delay_seconds=delay)
        elif sys.argv[1] == 'batch':
            # Run batch of trades
            num = int(sys.argv[2]) if len(sys.argv) > 2 else 10
            engine.run_batch(num_trades=num)
        elif sys.argv[1] == 'status':
            # Just print current status
            engine.print_current_stats()
    else:
        # Default: run all trades quickly
        engine.run_continuous(delay_seconds=0.1)


if __name__ == '__main__':
    main()
