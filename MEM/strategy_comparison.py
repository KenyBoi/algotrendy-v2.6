"""
Strategy Comparison Framework
==============================
Compare multiple strategy configurations side-by-side

Features:
- Compare different parameter configurations
- Side-by-side performance metrics
- Rank strategies by multiple criteria
- Export results to CSV/JSON
- Visual comparison tables
- Statistical analysis

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import yfinance as yf
import pandas as pd
import numpy as np
from typing import List, Dict
import time
import json
import logging
from datetime import datetime

from fast_backtester import FastBacktester

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class StrategyConfig:
    """
    Represents a strategy configuration for comparison
    """

    def __init__(self, name: str, min_confidence: float, **kwargs):
        """
        Args:
            name: Strategy name/description
            min_confidence: Minimum confidence threshold
            **kwargs: Additional strategy parameters
        """
        self.name = name
        self.min_confidence = min_confidence
        self.params = kwargs
        self.results = None

    def __repr__(self):
        return f"StrategyConfig(name='{self.name}', confidence={self.min_confidence})"


class StrategyComparison:
    """
    Framework for comparing multiple strategy configurations
    """

    def __init__(self, initial_capital: float = 10000.0, commission: float = 0.001):
        """
        Initialize strategy comparison framework

        Args:
            initial_capital: Starting capital for each strategy
            commission: Commission rate (0.001 = 0.1%)
        """
        self.initial_capital = initial_capital
        self.commission = commission
        self.strategies = []
        self.comparison_results = None

    def add_strategy(self, strategy: StrategyConfig):
        """
        Add strategy configuration to comparison

        Args:
            strategy: StrategyConfig instance
        """
        self.strategies.append(strategy)

    def add_strategies(self, strategies: List[StrategyConfig]):
        """
        Add multiple strategy configurations

        Args:
            strategies: List of StrategyConfig instances
        """
        self.strategies.extend(strategies)

    def run_comparison(self, symbol: str, data: pd.DataFrame) -> Dict:
        """
        Run backtest for all strategies and compare results

        Args:
            symbol: Trading symbol
            data: Market data for backtesting

        Returns:
            Dict with comparison results
        """
        logger.info(f"\n{'='*80}")
        logger.info("STRATEGY COMPARISON")
        logger.info(f"{'='*80}")
        logger.info(f"Symbol: {symbol}")
        logger.info(f"Data: {len(data)} candles")
        logger.info(f"Strategies to compare: {len(self.strategies)}")
        logger.info(f"Initial capital: ${self.initial_capital:,.2f}")
        logger.info(f"{'='*80}\n")

        results = []
        start_time = time.time()

        for i, strategy in enumerate(self.strategies, 1):
            logger.info(f"Testing strategy {i}/{len(self.strategies)}: {strategy.name}...")

            # Create backtester
            backtester = FastBacktester(initial_capital=self.initial_capital)

            # Run backtest
            backtest_result = backtester.run_fast_backtest(
                symbol=symbol,
                data=data,
                min_confidence=strategy.min_confidence,
                commission=self.commission
            )

            # Store results
            strategy.results = backtest_result
            results.append({
                'strategy_name': strategy.name,
                'config': strategy,
                'results': backtest_result
            })

            logger.info(f"  ✓ Return: {backtest_result['total_return']:.2f}%, "
                       f"Trades: {backtest_result['total_trades']}")

        total_time = time.time() - start_time

        # Analyze results
        self.comparison_results = self._analyze_results(results, total_time)

        return self.comparison_results

    def _analyze_results(self, results: List[Dict], total_time: float) -> Dict:
        """
        Analyze and rank strategy results

        Args:
            results: List of strategy backtest results
            total_time: Total execution time

        Returns:
            Dict with analyzed comparison results
        """
        # Calculate rankings for different metrics
        rankings = {}

        # Rank by total return
        sorted_by_return = sorted(
            results,
            key=lambda x: x['results']['total_return'],
            reverse=True
        )
        rankings['by_return'] = [r['strategy_name'] for r in sorted_by_return]

        # Rank by Sharpe ratio
        sorted_by_sharpe = sorted(
            results,
            key=lambda x: x['results'].get('sharpe_ratio', 0),
            reverse=True
        )
        rankings['by_sharpe'] = [r['strategy_name'] for r in sorted_by_sharpe]

        # Rank by win rate (only strategies with trades)
        with_trades = [r for r in results if r['results']['total_trades'] > 0]
        if with_trades:
            sorted_by_winrate = sorted(
                with_trades,
                key=lambda x: x['results']['win_rate'],
                reverse=True
            )
            rankings['by_win_rate'] = [r['strategy_name'] for r in sorted_by_winrate]
        else:
            rankings['by_win_rate'] = []

        # Rank by profit factor
        if with_trades:
            sorted_by_pf = sorted(
                with_trades,
                key=lambda x: x['results'].get('profit_factor', 0),
                reverse=True
            )
            rankings['by_profit_factor'] = [r['strategy_name'] for r in sorted_by_pf]
        else:
            rankings['by_profit_factor'] = []

        # Calculate composite score
        for r in results:
            ret = r['results']['total_return'] / 100.0
            wr = r['results'].get('win_rate', 0.0) / 100.0
            sharpe = r['results'].get('sharpe_ratio', 0.0)
            dd = abs(r['results'].get('max_drawdown', 100.0)) / 100.0

            # Composite: 40% return, 20% win rate, 20% sharpe, 20% drawdown
            composite = (ret * 0.4 + wr * 0.2 + sharpe * 0.2 + (1 - dd) * 0.2)
            r['composite_score'] = composite

        # Rank by composite score
        sorted_by_composite = sorted(
            results,
            key=lambda x: x['composite_score'],
            reverse=True
        )
        rankings['by_composite'] = [r['strategy_name'] for r in sorted_by_composite]

        # Find best overall
        best_strategy = sorted_by_composite[0] if sorted_by_composite else None

        # Calculate statistics
        returns = [r['results']['total_return'] for r in results]
        stats = {
            'mean_return': np.mean(returns),
            'median_return': np.median(returns),
            'std_return': np.std(returns),
            'min_return': np.min(returns),
            'max_return': np.max(returns)
        }

        return {
            'total_strategies': len(results),
            'execution_time': total_time,
            'results': results,
            'rankings': rankings,
            'best_strategy': best_strategy,
            'statistics': stats
        }

    def print_comparison(self):
        """
        Print formatted comparison results
        """
        if not self.comparison_results:
            print("No comparison results available. Run run_comparison() first.")
            return

        results = self.comparison_results['results']
        rankings = self.comparison_results['rankings']
        stats = self.comparison_results['statistics']

        print(f"\n{'='*100}")
        print("STRATEGY COMPARISON RESULTS")
        print(f"{'='*100}")

        print(f"\n📊 Summary:")
        print(f"  Total Strategies: {self.comparison_results['total_strategies']}")
        print(f"  Execution Time: {self.comparison_results['execution_time']:.2f}s")

        print(f"\n📈 Performance Statistics:")
        print(f"  Mean Return: {stats['mean_return']:.2f}%")
        print(f"  Median Return: {stats['median_return']:.2f}%")
        print(f"  Std Dev: {stats['std_return']:.2f}%")
        print(f"  Min Return: {stats['min_return']:.2f}%")
        print(f"  Max Return: {stats['max_return']:.2f}%")

        print(f"\n{'='*100}")
        print("DETAILED COMPARISON TABLE")
        print(f"{'='*100}")

        # Table header
        print(f"{'Strategy':<30} {'Return':<10} {'Trades':<8} {'Win Rate':<10} "
              f"{'Sharpe':<8} {'PF':<8} {'MaxDD':<8} {'Score':<8}")
        print(f"{'-'*30} {'-'*10} {'-'*8} {'-'*10} {'-'*8} {'-'*8} {'-'*8} {'-'*8}")

        # Sort by composite score for display
        sorted_results = sorted(results, key=lambda x: x['composite_score'], reverse=True)

        for r in sorted_results:
            res = r['results']
            name = r['strategy_name'][:29]  # Truncate if too long
            ret = res['total_return']
            trades = res['total_trades']
            wr = res.get('win_rate', 0.0)
            sharpe = res.get('sharpe_ratio', 0.0)
            pf = res.get('profit_factor', 0.0)
            dd = res.get('max_drawdown', 0.0)
            score = r['composite_score']

            print(f"{name:<30} {ret:>8.2f}%  {trades:>6}   {wr:>8.1f}%  "
                  f"{sharpe:>6.2f}  {pf:>6.2f}  {dd:>6.2f}%  {score:>6.3f}")

        print(f"{'='*100}")

        # Rankings
        print(f"\n🏆 RANKINGS")
        print(f"{'='*100}")

        print(f"\nTop 3 by Return:")
        for i, name in enumerate(rankings['by_return'][:3], 1):
            result = next(r for r in results if r['strategy_name'] == name)
            print(f"  {i}. {name}: {result['results']['total_return']:.2f}%")

        if rankings['by_win_rate']:
            print(f"\nTop 3 by Win Rate:")
            for i, name in enumerate(rankings['by_win_rate'][:3], 1):
                result = next(r for r in results if r['strategy_name'] == name)
                print(f"  {i}. {name}: {result['results']['win_rate']:.1f}%")

        print(f"\nTop 3 by Sharpe Ratio:")
        for i, name in enumerate(rankings['by_sharpe'][:3], 1):
            result = next(r for r in results if r['strategy_name'] == name)
            print(f"  {i}. {name}: {result['results'].get('sharpe_ratio', 0):.2f}")

        print(f"\nTop 3 by Composite Score:")
        for i, name in enumerate(rankings['by_composite'][:3], 1):
            result = next(r for r in results if r['strategy_name'] == name)
            print(f"  {i}. {name}: {result['composite_score']:.3f}")

        # Best overall
        if self.comparison_results['best_strategy']:
            best = self.comparison_results['best_strategy']
            print(f"\n🥇 OVERALL WINNER: {best['strategy_name']}")
            print(f"   Composite Score: {best['composite_score']:.3f}")
            print(f"   Return: {best['results']['total_return']:.2f}%")
            print(f"   Win Rate: {best['results'].get('win_rate', 0):.1f}%")
            print(f"   Sharpe: {best['results'].get('sharpe_ratio', 0):.2f}")

        print(f"\n{'='*100}\n")

    def export_to_csv(self, filename: str):
        """
        Export comparison results to CSV

        Args:
            filename: Output CSV filename
        """
        if not self.comparison_results:
            logger.error("No results to export")
            return

        rows = []
        for r in self.comparison_results['results']:
            res = r['results']
            row = {
                'strategy_name': r['strategy_name'],
                'total_return': res['total_return'],
                'total_trades': res['total_trades'],
                'win_rate': res.get('win_rate', 0),
                'profit_factor': res.get('profit_factor', 0),
                'sharpe_ratio': res.get('sharpe_ratio', 0),
                'max_drawdown': res.get('max_drawdown', 0),
                'final_equity': res['final_equity'],
                'composite_score': r['composite_score']
            }
            rows.append(row)

        df = pd.DataFrame(rows)
        df.to_csv(filename, index=False)
        logger.info(f"✓ Exported results to {filename}")

    def export_to_json(self, filename: str):
        """
        Export comparison results to JSON

        Args:
            filename: Output JSON filename
        """
        if not self.comparison_results:
            logger.error("No results to export")
            return

        # Prepare simplified results for JSON
        export_data = {
            'timestamp': datetime.now().isoformat(),
            'total_strategies': self.comparison_results['total_strategies'],
            'execution_time': self.comparison_results['execution_time'],
            'statistics': self.comparison_results['statistics'],
            'rankings': self.comparison_results['rankings'],
            'strategies': []
        }

        for r in self.comparison_results['results']:
            export_data['strategies'].append({
                'name': r['strategy_name'],
                'composite_score': r['composite_score'],
                'results': r['results']
            })

        with open(filename, 'w') as f:
            json.dump(export_data, f, indent=2, default=str)

        logger.info(f"✓ Exported results to {filename}")


def demo_strategy_comparison():
    """
    Demo: Compare different strategy configurations
    """
    # Fetch data
    symbol = 'BTC-USD'
    print(f"\nFetching {symbol} data...")

    ticker = yf.Ticker(symbol)
    data = ticker.history(interval='1d', period='1y')
    data.columns = [c.lower() for c in data.columns]

    print(f"Loaded {len(data)} candles")

    # Create comparison framework
    comparison = StrategyComparison(initial_capital=10000.0, commission=0.001)

    # Add different strategy configurations to compare
    strategies = [
        StrategyConfig("Conservative (80% confidence)", min_confidence=80.0),
        StrategyConfig("Moderate (70% confidence)", min_confidence=70.0),
        StrategyConfig("Balanced (60% confidence)", min_confidence=60.0),
        StrategyConfig("Aggressive (50% confidence)", min_confidence=50.0),
        StrategyConfig("Very Aggressive (40% confidence)", min_confidence=40.0),
    ]

    comparison.add_strategies(strategies)

    # Run comparison
    results = comparison.run_comparison(symbol, data)

    # Print results
    comparison.print_comparison()

    # Export results
    comparison.export_to_csv('strategy_comparison_results.csv')
    comparison.export_to_json('strategy_comparison_results.json')


if __name__ == "__main__":
    demo_strategy_comparison()
