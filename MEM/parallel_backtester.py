"""
Parallel Multi-Symbol Backtester
================================
Backtest strategy across multiple symbols simultaneously using multiprocessing

Features:
- Parallel execution using all available CPU cores
- Test strategy across multiple symbols/markets
- Aggregate portfolio-level metrics
- Individual symbol performance breakdown
- Optimal symbol selection based on performance

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import yfinance as yf
import pandas as pd
import numpy as np
from datetime import datetime
import time
from multiprocessing import Pool, cpu_count
from typing import List, Dict, Tuple
import logging

from fast_backtester import FastBacktester

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def backtest_single_symbol(args: Tuple[str, dict]) -> Dict:
    """
    Backtest a single symbol (used in parallel processing)

    Args:
        args: Tuple of (symbol, config)
            symbol: Symbol to backtest (e.g., 'BTC-USD')
            config: Dict with configuration parameters

    Returns:
        Dict with backtest results for this symbol
    """
    symbol, config = args

    try:
        logger.info(f"Starting backtest for {symbol}...")

        # Fetch data
        ticker = yf.Ticker(symbol)
        data = ticker.history(
            interval=config.get('interval', '1d'),
            period=config.get('period', '1y')
        )

        if len(data) == 0:
            logger.warning(f"No data available for {symbol}")
            return {
                'symbol': symbol,
                'success': False,
                'error': 'No data available'
            }

        # Normalize column names
        data.columns = [c.lower() for c in data.columns]

        # Create backtester
        backtester = FastBacktester(
            initial_capital=config.get('initial_capital', 10000.0)
        )

        # Run backtest
        results = backtester.run_fast_backtest(
            symbol=symbol,
            data=data,
            min_confidence=config.get('min_confidence', 60.0),
            commission=config.get('commission', 0.001)
        )

        results['success'] = True
        logger.info(f"Completed {symbol}: {results['total_return']:.2f}% return, {results['total_trades']} trades")

        return results

    except Exception as e:
        logger.error(f"Error backtesting {symbol}: {e}")
        return {
            'symbol': symbol,
            'success': False,
            'error': str(e)
        }


class ParallelBacktester:
    """
    Parallel backtester for testing strategies across multiple symbols
    """

    def __init__(self, initial_capital_per_symbol: float = 10000.0, max_workers: int = None):
        """
        Initialize parallel backtester

        Args:
            initial_capital_per_symbol: Starting capital for each symbol
            max_workers: Max parallel workers (default: CPU count)
        """
        self.initial_capital_per_symbol = initial_capital_per_symbol
        self.max_workers = max_workers or cpu_count()

    def run_parallel_backtest(
        self,
        symbols: List[str],
        interval: str = '1d',
        period: str = '1y',
        min_confidence: float = 60.0,
        commission: float = 0.001
    ) -> Dict:
        """
        Run backtests for multiple symbols in parallel

        Args:
            symbols: List of symbols to backtest (e.g., ['BTC-USD', 'ETH-USD'])
            interval: Data interval ('1d', '1h', etc.)
            period: Data period ('1y', '6mo', etc.)
            min_confidence: Minimum confidence threshold for signals
            commission: Commission rate (0.001 = 0.1%)

        Returns:
            Dict with aggregated results across all symbols
        """
        logger.info(f"\n{'='*80}")
        logger.info(f"PARALLEL MULTI-SYMBOL BACKTEST")
        logger.info(f"{'='*80}")
        logger.info(f"Symbols: {len(symbols)}")
        logger.info(f"Period: {period} @ {interval}")
        logger.info(f"Workers: {self.max_workers}")
        logger.info(f"Initial capital per symbol: ${self.initial_capital_per_symbol:,.2f}")
        logger.info(f"{'='*80}\n")

        # Prepare arguments for each symbol
        config = {
            'interval': interval,
            'period': period,
            'min_confidence': min_confidence,
            'commission': commission,
            'initial_capital': self.initial_capital_per_symbol
        }

        args_list = [(symbol, config) for symbol in symbols]

        # Run backtests in parallel
        start_time = time.time()

        with Pool(processes=self.max_workers) as pool:
            results = pool.map(backtest_single_symbol, args_list)

        total_time = time.time() - start_time

        # Aggregate results
        aggregated = self._aggregate_results(results, total_time)

        return aggregated

    def _aggregate_results(self, results: List[Dict], total_time: float) -> Dict:
        """
        Aggregate results from multiple symbol backtests

        Args:
            results: List of individual backtest results
            total_time: Total execution time

        Returns:
            Aggregated portfolio-level results
        """
        successful = [r for r in results if r.get('success', False)]
        failed = [r for r in results if not r.get('success', False)]

        if not successful:
            return {
                'success': False,
                'total_symbols': len(results),
                'failed_symbols': len(failed),
                'error': 'All symbols failed to backtest'
            }

        # Portfolio-level metrics
        total_return = np.mean([r['total_return'] for r in successful])
        total_trades = sum([r['total_trades'] for r in successful])

        # Win rate (weighted by number of trades)
        win_rates = []
        trade_counts = []
        for r in successful:
            if r['total_trades'] > 0:
                win_rates.append(r['win_rate'])
                trade_counts.append(r['total_trades'])

        if trade_counts:
            weighted_win_rate = np.average(win_rates, weights=trade_counts)
        else:
            weighted_win_rate = 0.0

        # Calculate portfolio equity curve (equal weight)
        portfolio_return = np.mean([r['total_return'] for r in successful])

        # Find best and worst performers
        successful_sorted = sorted(successful, key=lambda x: x['total_return'], reverse=True)
        best_symbol = successful_sorted[0] if successful_sorted else None
        worst_symbol = successful_sorted[-1] if successful_sorted else None

        # Calculate Sharpe ratio (portfolio-level)
        returns = [r['total_return'] for r in successful]
        sharpe_ratio = (np.mean(returns) / np.std(returns)) if len(returns) > 1 and np.std(returns) > 0 else 0.0

        # Aggregate profit factor
        profit_factors = [r['profit_factor'] for r in successful if r['total_trades'] > 0 and r['profit_factor'] > 0]
        avg_profit_factor = np.mean(profit_factors) if profit_factors else 0.0

        # Max drawdown
        max_drawdowns = [r['max_drawdown'] for r in successful]
        avg_max_drawdown = np.mean(max_drawdowns) if max_drawdowns else 0.0

        aggregated = {
            'success': True,
            'execution_time': total_time,
            'total_symbols': len(results),
            'successful_symbols': len(successful),
            'failed_symbols': len(failed),

            # Portfolio metrics
            'portfolio_return': portfolio_return,
            'portfolio_sharpe': sharpe_ratio,
            'total_trades': total_trades,
            'weighted_win_rate': weighted_win_rate,
            'avg_profit_factor': avg_profit_factor,
            'avg_max_drawdown': avg_max_drawdown,

            # Best/worst
            'best_symbol': best_symbol['symbol'] if best_symbol else None,
            'best_return': best_symbol['total_return'] if best_symbol else 0.0,
            'worst_symbol': worst_symbol['symbol'] if worst_symbol else None,
            'worst_return': worst_symbol['total_return'] if worst_symbol else 0.0,

            # Individual results
            'individual_results': successful,
            'failed_symbols_details': failed
        }

        return aggregated

    def print_results(self, results: Dict):
        """
        Print formatted results to console

        Args:
            results: Aggregated results dict
        """
        if not results.get('success', False):
            print(f"\n❌ Backtest failed: {results.get('error', 'Unknown error')}")
            return

        print(f"\n{'='*80}")
        print("PARALLEL BACKTEST RESULTS")
        print(f"{'='*80}")

        print(f"\n📊 Portfolio Summary:")
        print(f"  Total Symbols: {results['total_symbols']}")
        print(f"  Successful: {results['successful_symbols']}")
        print(f"  Failed: {results['failed_symbols']}")
        print(f"  Execution Time: {results['execution_time']:.2f}s")

        print(f"\n💰 Portfolio Performance:")
        print(f"  Average Return: {results['portfolio_return']:.2f}%")
        print(f"  Sharpe Ratio: {results['portfolio_sharpe']:.2f}")
        print(f"  Total Trades: {results['total_trades']}")
        print(f"  Weighted Win Rate: {results['weighted_win_rate']:.1f}%")
        print(f"  Avg Profit Factor: {results['avg_profit_factor']:.2f}")
        print(f"  Avg Max Drawdown: {results['avg_max_drawdown']:.2f}%")

        print(f"\n🏆 Best Performer:")
        print(f"  Symbol: {results['best_symbol']}")
        print(f"  Return: {results['best_return']:.2f}%")

        print(f"\n📉 Worst Performer:")
        print(f"  Symbol: {results['worst_symbol']}")
        print(f"  Return: {results['worst_return']:.2f}%")

        print(f"\n📋 Individual Symbol Results:")
        print(f"  {'Symbol':<15} {'Return':<12} {'Trades':<8} {'Win Rate':<10} {'Sharpe':<8}")
        print(f"  {'-'*15} {'-'*12} {'-'*8} {'-'*10} {'-'*8}")

        for r in sorted(results['individual_results'], key=lambda x: x['total_return'], reverse=True):
            print(f"  {r['symbol']:<15} {r['total_return']:>10.2f}%  {r['total_trades']:>6}   "
                  f"{r['win_rate']:>8.1f}%  {r.get('sharpe_ratio', 0):>6.2f}")

        if results['failed_symbols'] > 0:
            print(f"\n❌ Failed Symbols:")
            for f in results['failed_symbols_details']:
                print(f"  - {f['symbol']}: {f.get('error', 'Unknown error')}")

        print(f"\n{'='*80}\n")


def demo_parallel_backtest():
    """
    Demo: Run parallel backtest on multiple crypto symbols
    """
    # Popular crypto symbols
    symbols = [
        'BTC-USD',
        'ETH-USD',
        'BNB-USD',
        'SOL-USD',
        'ADA-USD',
        'XRP-USD',
        'DOGE-USD',
        'MATIC-USD'
    ]

    # Create parallel backtester
    backtester = ParallelBacktester(
        initial_capital_per_symbol=10000.0,
        max_workers=None  # Use all available cores
    )

    # Run parallel backtest
    results = backtester.run_parallel_backtest(
        symbols=symbols,
        period='1y',
        interval='1d',
        min_confidence=60.0,
        commission=0.001
    )

    # Print results
    backtester.print_results(results)

    # Calculate speedup
    num_symbols = len(symbols)
    time_per_symbol = 0.55  # From fast_backtester.py benchmarks
    sequential_time = num_symbols * time_per_symbol
    speedup = sequential_time / results['execution_time']

    print(f"⚡ Performance Analysis:")
    print(f"  Sequential time (estimated): {sequential_time:.2f}s ({num_symbols} × {time_per_symbol:.2f}s)")
    print(f"  Parallel time (actual): {results['execution_time']:.2f}s")
    print(f"  Speedup: {speedup:.1f}x")
    print(f"  Workers used: {cpu_count()}")
    print(f"  Efficiency: {(speedup / cpu_count() * 100):.1f}%")


if __name__ == "__main__":
    demo_parallel_backtest()
