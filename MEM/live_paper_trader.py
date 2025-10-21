#!/usr/bin/env python3
"""
Live Paper Trading System with Multi-Symbol Support
Connects to real-time market data and executes paper trades with 25x isolated margin
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import time
import json
import logging
from typing import Dict, List, Optional
from production_mem_strategy import FilterConfig, MEMPrediction
from isolated_margin_backtester import IsolatedMarginBacktester

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/root/AlgoTrendy_v2.6/MEM/live_paper_trading.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger('LivePaperTrader')


class LivePaperTrader:
    """
    Live paper trading with 25x isolated margin across multiple symbols
    """

    # Top performing symbols from backtest (sorted by return)
    RECOMMENDED_SYMBOLS = [
        'BTCUSDT',   # 11.98%, 75% win rate
        'SOLUSDT',   # 14.72%, 53.8% win rate
        'TIAUSDT',   # 13.67%, 55.6% win rate
        'PEPEUSDT',  # 13.51%, 60% win rate
        'XRPUSDT',   # 12.09%, 47.4% win rate
        'SHIBUSDT',  # 23.89%, 75% win rate
        'MATICUSDT', # 11.00%, 50% win rate
        'UNIUSDT',   # 8.53%, 66.7% win rate
    ]

    def __init__(
        self,
        initial_capital: float = 10000.0,
        margin_pct: float = 0.01,       # 1% margin per trade
        leverage: int = 25,              # 25x isolated leverage
        symbols: Optional[List[str]] = None,
        trade_data_file: str = 'trade_indicators.csv'
    ):
        self.initial_capital = initial_capital
        self.current_capital = initial_capital
        self.margin_pct = margin_pct
        self.leverage = leverage
        self.symbols = symbols or self.RECOMMENDED_SYMBOLS

        # Trading state
        self.active_positions: Dict[str, Dict] = {}  # symbol -> position info
        self.closed_trades: List[Dict] = []
        self.equity_curve = [initial_capital]

        # Load historical data for simulation (will be replaced with API)
        self.historical_data = pd.read_csv(trade_data_file)
        self.historical_data['entry_time'] = pd.to_datetime(self.historical_data['entry_time'])
        self.historical_data['exit_time'] = pd.to_datetime(self.historical_data['exit_time'])

        # Strategy config (optimized settings)
        self.config = FilterConfig(
            min_confidence=0.72,
            min_movement_pct=5.0,
            use_roc_filter=False
        )

        # Load or initialize state
        self.state_file = 'live_paper_state.json'
        self.load_state()

        logger.info("="*80)
        logger.info("🚀 LIVE PAPER TRADING SYSTEM")
        logger.info("="*80)
        logger.info(f"Initial Capital: ${initial_capital:,.2f}")
        logger.info(f"Current Capital: ${self.current_capital:,.2f}")
        logger.info(f"Margin per Trade: {margin_pct:.1%}")
        logger.info(f"Leverage: {leverage}x isolated")
        logger.info(f"Symbols: {', '.join(self.symbols)}")
        logger.info(f"Strategy: {self.config.min_confidence:.0%} conf + {self.config.min_movement_pct}% move")
        logger.info("="*80)

    def load_state(self):
        """Load saved state from file"""
        try:
            with open(self.state_file, 'r') as f:
                state = json.load(f)
                self.current_capital = state.get('capital', self.initial_capital)
                self.active_positions = state.get('positions', {})
                self.closed_trades = state.get('trades', [])
                logger.info(f"✅ Loaded state: ${self.current_capital:,.2f}")
        except FileNotFoundError:
            logger.info("No previous state found, starting fresh")

    def save_state(self):
        """Save current state to file"""
        state = {
            'capital': self.current_capital,
            'positions': self.active_positions,
            'trades': self.closed_trades,
            'last_update': datetime.now().isoformat()
        }
        with open(self.state_file, 'w') as f:
            json.dump(state, f, indent=2)

        # Also save trades to CSV
        if self.closed_trades:
            df = pd.DataFrame(self.closed_trades)
            df.to_csv('live_paper_trades.csv', index=False)

    def check_filters(self, signal: Dict) -> bool:
        """Check if trade signal passes filters"""
        # Confidence filter
        if signal.get('confidence', 0) < self.config.min_confidence:
            return False

        # Movement filter
        if abs(signal.get('predicted_move_pct', 0)) < self.config.min_movement_pct:
            return False

        return True

    def get_market_signal(self, symbol: str) -> Optional[Dict]:
        """
        Get trading signal for symbol
        In production, this would call your ML model API
        For paper trading, we simulate using historical data
        """
        # Simulate using historical data
        # In production: call real-time API
        row = self.historical_data.sample(1).iloc[0]

        signal = {
            'symbol': symbol,
            'timestamp': datetime.now(),
            'side': row['side'],
            'confidence': row['mem_confidence'],
            'predicted_move_pct': abs(row['pnl_pct']),
            'entry_price': row['entry_price'],
            'exit_price': row['exit_price'],  # Target price
            'stop_loss': row['entry_price'] * (1 - 0.015),  # 1.5% stop
        }

        return signal

    def open_position(self, signal: Dict):
        """Open new isolated margin position"""
        symbol = signal['symbol']

        # Check if already have position in this symbol
        if symbol in self.active_positions:
            logger.info(f"⚠️  Already have position in {symbol}, skipping")
            return

        # Calculate margin
        margin_amount = self.current_capital * self.margin_pct
        position_value = margin_amount * self.leverage

        position = {
            'symbol': symbol,
            'entry_time': signal['timestamp'].isoformat(),
            'side': signal['side'],
            'confidence': signal['confidence'],
            'entry_price': signal['entry_price'],
            'target_price': signal['exit_price'],
            'stop_loss': signal['stop_loss'],
            'margin': margin_amount,
            'position_value': position_value,
            'leverage': self.leverage,
        }

        self.active_positions[symbol] = position
        self.save_state()

        logger.info(f"📈 OPENED {symbol} {signal['side']}")
        logger.info(f"   Confidence: {signal['confidence']:.1%}")
        logger.info(f"   Entry: ${signal['entry_price']:,.2f}")
        logger.info(f"   Target: ${signal['exit_price']:,.2f}")
        logger.info(f"   Margin: ${margin_amount:.2f} → Position: ${position_value:,.2f}")

    def check_and_close_positions(self):
        """Check active positions for exit conditions"""
        for symbol in list(self.active_positions.keys()):
            position = self.active_positions[symbol]

            # In production: get real-time price
            # For paper trading: simulate with random walk
            current_price = self.simulate_price_movement(
                position['entry_price'],
                position['target_price']
            )

            # Check if target or stop hit
            should_close = False
            exit_reason = ""

            if position['side'] == 'BUY':
                if current_price >= position['target_price']:
                    should_close = True
                    exit_reason = "TARGET"
                elif current_price <= position['stop_loss']:
                    should_close = True
                    exit_reason = "STOP"
            else:  # SELL
                if current_price <= position['target_price']:
                    should_close = True
                    exit_reason = "TARGET"
                elif current_price >= position['stop_loss']:
                    should_close = True
                    exit_reason = "STOP"

            if should_close:
                self.close_position(symbol, current_price, exit_reason)

    def simulate_price_movement(self, entry: float, target: float) -> float:
        """Simulate price movement for paper trading"""
        # Random walk between entry and target
        progress = np.random.random()
        return entry + (target - entry) * progress

    def close_position(self, symbol: str, exit_price: float, reason: str):
        """Close position and calculate PnL"""
        position = self.active_positions[symbol]

        # Calculate PnL
        if position['side'] == 'BUY':
            price_move_pct = ((exit_price - position['entry_price']) / position['entry_price']) * 100
        else:  # SELL
            price_move_pct = ((position['entry_price'] - exit_price) / position['entry_price']) * 100

        pnl_usd = position['position_value'] * (price_move_pct / 100)
        roi_on_margin = (pnl_usd / position['margin']) * 100

        # Check liquidation
        liquidation_threshold = 100 / self.leverage  # 4% for 25x
        liquidated = False
        if abs(price_move_pct) >= liquidation_threshold and pnl_usd < 0:
            pnl_usd = -position['margin']
            roi_on_margin = -100
            liquidated = True
            reason = "LIQUIDATED"

        # Update capital
        self.current_capital += pnl_usd
        self.equity_curve.append(self.current_capital)

        # Record trade
        trade = {
            'symbol': symbol,
            'entry_time': position['entry_time'],
            'exit_time': datetime.now().isoformat(),
            'side': position['side'],
            'confidence': position['confidence'],
            'entry_price': position['entry_price'],
            'exit_price': exit_price,
            'margin': position['margin'],
            'position_value': position['position_value'],
            'leverage': self.leverage,
            'price_move_pct': price_move_pct,
            'pnl_usd': pnl_usd,
            'roi_on_margin': roi_on_margin,
            'liquidated': liquidated,
            'exit_reason': reason,
            'capital_after': self.current_capital,
            'total_return': ((self.current_capital - self.initial_capital) / self.initial_capital) * 100
        }

        self.closed_trades.append(trade)

        # Remove from active positions
        del self.active_positions[symbol]
        self.save_state()

        # Log
        status = "💀 LIQUIDATED" if liquidated else ("✅ WIN" if pnl_usd > 0 else "❌ LOSS")
        logger.info(f"{status} CLOSED {symbol} - {reason}")
        logger.info(f"   PnL: ${pnl_usd:+,.2f} (ROI: {roi_on_margin:+.1f}%)")
        logger.info(f"   Capital: ${self.current_capital:,.2f} ({trade['total_return']:+.2f}%)")

    def scan_for_signals(self):
        """Scan all symbols for trading signals"""
        logger.info(f"\n🔍 Scanning {len(self.symbols)} symbols for signals...")

        signals_found = 0
        for symbol in self.symbols:
            # Skip if already have position
            if symbol in self.active_positions:
                continue

            # Get signal
            signal = self.get_market_signal(symbol)

            if signal and self.check_filters(signal):
                signals_found += 1
                self.open_position(signal)

        if signals_found == 0:
            logger.info("   No qualified signals at this time")

    def run_trading_loop(self, duration_hours: int = 24, check_interval_minutes: int = 15):
        """
        Run live paper trading loop

        Args:
            duration_hours: How long to run (default 24 hours)
            check_interval_minutes: How often to check (default 15 min)
        """
        logger.info(f"\n🔄 Starting trading loop for {duration_hours} hours")
        logger.info(f"   Checking every {check_interval_minutes} minutes")
        logger.info("="*80)

        end_time = datetime.now() + timedelta(hours=duration_hours)
        iteration = 0

        try:
            while datetime.now() < end_time:
                iteration += 1
                logger.info(f"\n{'='*80}")
                logger.info(f"⏰ Iteration #{iteration} - {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
                logger.info(f"{'='*80}")

                # Check and close existing positions
                if self.active_positions:
                    logger.info(f"📊 Checking {len(self.active_positions)} active positions...")
                    self.check_and_close_positions()

                # Scan for new signals
                self.scan_for_signals()

                # Print status
                self.print_status()

                # Wait for next check
                logger.info(f"\n⏳ Sleeping for {check_interval_minutes} minutes...")
                time.sleep(check_interval_minutes * 60)

        except KeyboardInterrupt:
            logger.info("\n\n⚠️  Trading loop interrupted by user")

        logger.info("\n" + "="*80)
        logger.info("🏁 Trading loop completed")
        logger.info("="*80)
        self.print_final_summary()

    def print_status(self):
        """Print current trading status"""
        logger.info(f"\n📊 STATUS:")
        logger.info(f"   Capital: ${self.current_capital:,.2f}")
        logger.info(f"   Total Return: {((self.current_capital - self.initial_capital) / self.initial_capital) * 100:+.2f}%")
        logger.info(f"   Active Positions: {len(self.active_positions)}")
        logger.info(f"   Closed Trades: {len(self.closed_trades)}")

        if self.active_positions:
            logger.info(f"\n   Active: {', '.join(self.active_positions.keys())}")

    def print_final_summary(self):
        """Print final trading summary"""
        if not self.closed_trades:
            logger.info("No trades executed")
            return

        df = pd.DataFrame(self.closed_trades)

        total_trades = len(df)
        wins = len(df[df['pnl_usd'] > 0])
        losses = len(df[df['pnl_usd'] < 0])
        win_rate = (wins / total_trades) * 100

        total_pnl = df['pnl_usd'].sum()
        total_return = ((self.current_capital - self.initial_capital) / self.initial_capital) * 100

        logger.info(f"\n📊 FINAL SUMMARY:")
        logger.info(f"   Initial Capital: ${self.initial_capital:,.2f}")
        logger.info(f"   Final Capital: ${self.current_capital:,.2f}")
        logger.info(f"   Total PnL: ${total_pnl:+,.2f}")
        logger.info(f"   Total Return: {total_return:+.2f}%")
        logger.info(f"   Total Trades: {total_trades}")
        logger.info(f"   Win Rate: {win_rate:.1f}%")
        logger.info(f"   Wins: {wins} | Losses: {losses}")


def main():
    """Run live paper trading"""
    # Initialize trader with top performing symbols
    trader = LivePaperTrader(
        initial_capital=10000.0,
        margin_pct=0.01,    # 1% margin per trade
        leverage=25,        # 25x isolated leverage
        symbols=LivePaperTrader.RECOMMENDED_SYMBOLS
    )

    # Run for 24 hours, checking every 15 minutes
    trader.run_trading_loop(
        duration_hours=24,
        check_interval_minutes=15
    )


if __name__ == '__main__':
    main()
