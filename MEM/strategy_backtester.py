"""
Strategy Backtesting Framework
==============================
Comprehensive backtesting system for the Advanced Trading Strategy.

Features:
- Historical data backtesting
- Multiple symbol support
- Performance metrics calculation
- Trade-by-trade analysis
- Strategy comparison
- Visual reports

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List, Tuple, Optional
import logging
import json
from collections import defaultdict

from advanced_trading_strategy import AdvancedTradingStrategy
import yfinance as yf

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class StrategyBacktester:
    """
    Comprehensive backtesting framework for trading strategies

    Runs historical simulations and calculates detailed performance metrics
    """

    def __init__(self, initial_capital: float = 10000.0):
        """
        Initialize backtester

        Args:
            initial_capital: Starting capital for backtests
        """
        self.initial_capital = initial_capital
        self.trades = []
        self.equity_curve = []
        self.logger = logging.getLogger(__name__)

    def fetch_historical_data(self, symbol: str,
                             interval: str = '1h',
                             start_date: str = None,
                             end_date: str = None) -> pd.DataFrame:
        """
        Fetch historical data for backtesting

        Args:
            symbol: Trading symbol
            interval: Data interval ('1m', '5m', '1h', '1d', etc.)
            start_date: Start date (YYYY-MM-DD) or None for auto
            end_date: End date (YYYY-MM-DD) or None for today

        Returns:
            DataFrame with OHLCV data
        """
        try:
            ticker = yf.Ticker(symbol)

            if start_date and end_date:
                data = ticker.history(interval=interval, start=start_date, end=end_date)
            else:
                # Default: get max available data
                data = ticker.history(interval=interval, period='max')

            if data.empty:
                raise ValueError(f"No data received for {symbol}")

            # Rename columns to lowercase
            data.columns = [col.lower() for col in data.columns]

            logger.info(f"Fetched {len(data)} {interval} candles for {symbol} "
                       f"({data.index[0]} to {data.index[-1]})")

            return data

        except Exception as e:
            logger.error(f"Error fetching data for {symbol}: {e}")
            raise

    def run_backtest(self,
                    symbol: str,
                    strategy: AdvancedTradingStrategy,
                    data: pd.DataFrame = None,
                    interval: str = '1h',
                    lookback_periods: int = 200,
                    commission: float = 0.001) -> Dict:
        """
        Run backtest on historical data

        Args:
            symbol: Trading symbol
            strategy: AdvancedTradingStrategy instance
            data: Pre-loaded data (optional, will fetch if None)
            interval: Data interval
            lookback_periods: Minimum periods needed for indicators
            commission: Trading commission (0.001 = 0.1%)

        Returns:
            Backtest results dictionary
        """
        logger.info(f"\n{'='*80}")
        logger.info(f"BACKTESTING: {symbol}")
        logger.info(f"{'='*80}")

        # Fetch data if not provided
        if data is None:
            data = self.fetch_historical_data(symbol, interval=interval)

        if len(data) < lookback_periods:
            raise ValueError(f"Insufficient data: {len(data)} < {lookback_periods}")

        # Initialize tracking
        trades = []
        equity = self.initial_capital
        equity_curve = [equity]
        position = None

        # Iterate through data
        for i in range(lookback_periods, len(data) - 1):
            current_time = data.index[i]
            current_price = data['close'].iloc[i]

            # Get data window for strategy
            data_window = data.iloc[:i+1]

            # Generate signal
            try:
                signal = strategy.generate_trade_signal(
                    data_1h=data_window,
                    account_balance=equity
                )
            except Exception as e:
                logger.warning(f"Signal generation error at {current_time}: {e}")
                equity_curve.append(equity)
                continue

            # Handle existing position
            if position is not None:
                # Check stop loss and take profit
                next_high = data['high'].iloc[i+1]
                next_low = data['low'].iloc[i+1]
                next_close = data['close'].iloc[i+1]
                exit_price = None
                exit_reason = None

                if position['side'] == 'BUY':
                    if next_low <= position['stop_loss']:
                        exit_price = position['stop_loss']
                        exit_reason = 'STOP_LOSS'
                    elif next_high >= position['take_profit']:
                        exit_price = position['take_profit']
                        exit_reason = 'TAKE_PROFIT'

                elif position['side'] == 'SELL':
                    if next_high >= position['stop_loss']:
                        exit_price = position['stop_loss']
                        exit_reason = 'STOP_LOSS'
                    elif next_low <= position['take_profit']:
                        exit_price = position['take_profit']
                        exit_reason = 'TAKE_PROFIT'

                # Close position if triggered
                if exit_price is not None:
                    # Calculate P&L
                    if position['side'] == 'BUY':
                        pnl = (exit_price - position['entry_price']) * position['size']
                    else:
                        pnl = (position['entry_price'] - exit_price) * position['size']

                    # Apply commission
                    trade_cost = (position['entry_price'] * position['size'] * commission +
                                 exit_price * position['size'] * commission)
                    pnl -= trade_cost

                    # Update equity
                    equity += pnl

                    # Record trade
                    trade = {
                        'symbol': symbol,
                        'entry_time': position['entry_time'],
                        'exit_time': data.index[i+1],
                        'side': position['side'],
                        'entry_price': position['entry_price'],
                        'exit_price': exit_price,
                        'stop_loss': position['stop_loss'],
                        'take_profit': position['take_profit'],
                        'size': position['size'],
                        'pnl': pnl,
                        'pnl_pct': (pnl / (position['entry_price'] * position['size'])) * 100,
                        'exit_reason': exit_reason,
                        'commission': trade_cost,
                        'equity_after': equity
                    }
                    trades.append(trade)

                    logger.info(f"{exit_reason}: {position['side']} @ ${exit_price:.2f}, "
                               f"P&L: ${pnl:.2f} ({trade['pnl_pct']:.2f}%), "
                               f"Equity: ${equity:.2f}")

                    position = None

            # Open new position if no current position
            if position is None and signal['action'] in ['BUY', 'SELL']:
                # Calculate position size based on current equity
                position_size = signal['position_size']
                position_value = current_price * position_size

                # Check if we have enough capital
                if position_value <= equity * 0.95:  # Use max 95% of equity
                    position = {
                        'entry_time': current_time,
                        'entry_price': current_price,
                        'side': signal['action'],
                        'stop_loss': signal['stop_loss'],
                        'take_profit': signal['take_profit'],
                        'size': position_size,
                        'confidence': signal['confidence']
                    }

                    logger.info(f"OPEN {signal['action']}: ${current_price:.2f}, "
                               f"Size: {position_size:.4f}, "
                               f"Confidence: {signal['confidence']:.1f}%")

            equity_curve.append(equity)

        # Close any remaining position at final price
        if position is not None:
            final_price = data['close'].iloc[-1]
            if position['side'] == 'BUY':
                pnl = (final_price - position['entry_price']) * position['size']
            else:
                pnl = (position['entry_price'] - final_price) * position['size']

            trade_cost = (position['entry_price'] * position['size'] * commission +
                         final_price * position['size'] * commission)
            pnl -= trade_cost
            equity += pnl

            trade = {
                'symbol': symbol,
                'entry_time': position['entry_time'],
                'exit_time': data.index[-1],
                'side': position['side'],
                'entry_price': position['entry_price'],
                'exit_price': final_price,
                'stop_loss': position['stop_loss'],
                'take_profit': position['take_profit'],
                'size': position['size'],
                'pnl': pnl,
                'pnl_pct': (pnl / (position['entry_price'] * position['size'])) * 100,
                'exit_reason': 'END_OF_DATA',
                'commission': trade_cost,
                'equity_after': equity
            }
            trades.append(trade)
            logger.info(f"CLOSE FINAL: {position['side']} @ ${final_price:.2f}, P&L: ${pnl:.2f}")

        # Calculate performance metrics
        results = self.calculate_metrics(trades, equity_curve, data)
        results['symbol'] = symbol
        results['interval'] = interval
        results['start_date'] = str(data.index[0])
        results['end_date'] = str(data.index[-1])
        results['initial_capital'] = self.initial_capital
        results['final_equity'] = equity
        results['trades'] = trades
        results['equity_curve'] = equity_curve

        return results

    def calculate_metrics(self, trades: List[Dict],
                         equity_curve: List[float],
                         data: pd.DataFrame) -> Dict:
        """
        Calculate comprehensive performance metrics

        Args:
            trades: List of completed trades
            equity_curve: Equity over time
            data: Market data

        Returns:
            Dictionary of metrics
        """
        if not trades:
            return {
                'total_trades': 0,
                'win_rate': 0,
                'total_return': 0,
                'sharpe_ratio': 0,
                'max_drawdown': 0
            }

        # Basic trade statistics
        total_trades = len(trades)
        winning_trades = [t for t in trades if t['pnl'] > 0]
        losing_trades = [t for t in trades if t['pnl'] <= 0]

        win_rate = (len(winning_trades) / total_trades * 100) if total_trades > 0 else 0

        # Returns
        total_pnl = sum(t['pnl'] for t in trades)
        total_return = ((equity_curve[-1] - self.initial_capital) / self.initial_capital) * 100

        # Average trade
        avg_win = np.mean([t['pnl'] for t in winning_trades]) if winning_trades else 0
        avg_loss = np.mean([t['pnl'] for t in losing_trades]) if losing_trades else 0
        avg_trade = total_pnl / total_trades if total_trades > 0 else 0

        # Profit factor
        gross_profit = sum(t['pnl'] for t in winning_trades)
        gross_loss = abs(sum(t['pnl'] for t in losing_trades))
        profit_factor = gross_profit / gross_loss if gross_loss > 0 else np.inf

        # Drawdown analysis
        equity_array = np.array(equity_curve)
        running_max = np.maximum.accumulate(equity_array)
        drawdown = (equity_array - running_max) / running_max * 100
        max_drawdown = abs(drawdown.min())

        # Sharpe ratio (simplified)
        returns = pd.Series(equity_curve).pct_change().dropna()
        if len(returns) > 0 and returns.std() > 0:
            sharpe_ratio = (returns.mean() / returns.std()) * np.sqrt(252)  # Annualized
        else:
            sharpe_ratio = 0

        # Trade duration
        trade_durations = []
        for t in trades:
            duration = (pd.Timestamp(t['exit_time']) - pd.Timestamp(t['entry_time'])).total_seconds() / 3600
            trade_durations.append(duration)
        avg_duration = np.mean(trade_durations) if trade_durations else 0

        # Exit reasons
        exit_reasons = defaultdict(int)
        for t in trades:
            exit_reasons[t['exit_reason']] += 1

        return {
            'total_trades': total_trades,
            'winning_trades': len(winning_trades),
            'losing_trades': len(losing_trades),
            'win_rate': win_rate,
            'total_pnl': total_pnl,
            'total_return': total_return,
            'avg_win': avg_win,
            'avg_loss': avg_loss,
            'avg_trade': avg_trade,
            'profit_factor': profit_factor,
            'max_drawdown': max_drawdown,
            'sharpe_ratio': sharpe_ratio,
            'avg_trade_duration_hours': avg_duration,
            'exit_reasons': dict(exit_reasons)
        }

    def backtest_multiple_symbols(self,
                                  symbols: List[str],
                                  strategy: AdvancedTradingStrategy,
                                  interval: str = '1h') -> Dict:
        """
        Run backtests on multiple symbols

        Args:
            symbols: List of trading symbols
            strategy: Strategy instance
            interval: Data interval

        Returns:
            Dictionary with results for each symbol
        """
        results = {}

        for symbol in symbols:
            try:
                logger.info(f"\nBacktesting {symbol}...")
                result = self.run_backtest(symbol, strategy, interval=interval)
                results[symbol] = result
            except Exception as e:
                logger.error(f"Error backtesting {symbol}: {e}")
                results[symbol] = {'error': str(e)}

        return results

    def generate_report(self, results: Dict) -> str:
        """
        Generate formatted backtest report

        Args:
            results: Backtest results dictionary

        Returns:
            Formatted report string
        """
        report = []
        report.append("\n" + "="*80)
        report.append("BACKTEST RESULTS")
        report.append("="*80)

        if 'symbol' in results:
            # Single symbol report
            r = results
            report.append(f"\nSymbol: {r['symbol']}")
            report.append(f"Period: {r['start_date']} to {r['end_date']}")
            report.append(f"Interval: {r['interval']}")
            report.append(f"\nCapital:")
            report.append(f"  Initial: ${r['initial_capital']:,.2f}")
            report.append(f"  Final: ${r['final_equity']:,.2f}")
            report.append(f"  Return: {r['total_return']:.2f}%")
            report.append(f"\nTrade Statistics:")
            report.append(f"  Total Trades: {r['total_trades']}")
            report.append(f"  Winning Trades: {r['winning_trades']}")
            report.append(f"  Losing Trades: {r['losing_trades']}")
            report.append(f"  Win Rate: {r['win_rate']:.2f}%")
            report.append(f"  Avg Win: ${r['avg_win']:.2f}")
            report.append(f"  Avg Loss: ${r['avg_loss']:.2f}")
            report.append(f"  Avg Trade: ${r['avg_trade']:.2f}")
            report.append(f"  Profit Factor: {r['profit_factor']:.2f}")
            report.append(f"\nRisk Metrics:")
            report.append(f"  Max Drawdown: {r['max_drawdown']:.2f}%")
            report.append(f"  Sharpe Ratio: {r['sharpe_ratio']:.2f}")
            report.append(f"  Avg Trade Duration: {r['avg_trade_duration_hours']:.1f} hours")
            report.append(f"\nExit Reasons:")
            for reason, count in r['exit_reasons'].items():
                pct = (count / r['total_trades']) * 100
                report.append(f"  {reason}: {count} ({pct:.1f}%)")
        else:
            # Multi-symbol report
            report.append(f"\nTested {len(results)} symbols")
            report.append("\nSummary:")
            for symbol, r in results.items():
                if 'error' in r:
                    report.append(f"  {symbol}: ERROR - {r['error']}")
                else:
                    report.append(f"  {symbol}: Return={r['total_return']:.2f}%, "
                                f"Trades={r['total_trades']}, WinRate={r['win_rate']:.1f}%")

        report.append("="*80)

        return "\n".join(report)


def demo_backtest():
    """Demo backtest with sample data"""
    print("\n" + "="*80)
    print("MEM STRATEGY BACKTESTER - DEMO")
    print("="*80)

    # Initialize
    backtester = StrategyBacktester(initial_capital=10000.0)
    strategy = AdvancedTradingStrategy(
        min_confidence=65.0,  # Lower for backtesting
        max_risk_per_trade=0.02,
        use_multi_timeframe=False  # Single timeframe for demo
    )

    # Run backtest on BTC
    print("\nBacktesting BTC-USD...")
    results = backtester.run_backtest(
        symbol='BTC-USD',
        strategy=strategy,
        interval='1d',  # Daily data
        lookback_periods=100
    )

    # Generate report
    report = backtester.generate_report(results)
    print(report)

    # Save results
    with open('/root/AlgoTrendy_v2.6/MEM/backtest_results.json', 'w') as f:
        # Remove non-serializable items
        save_results = {k: v for k, v in results.items()
                       if k not in ['trades', 'equity_curve']}
        json.dump(save_results, f, indent=2, default=str)

    print("\n✅ Backtest complete! Results saved to backtest_results.json")


if __name__ == "__main__":
    demo_backtest()
