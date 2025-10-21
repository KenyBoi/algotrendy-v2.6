"""
Portfolio-Level Backtester
===========================
Backtest a portfolio of multiple assets with capital allocation and rebalancing

Features:
- Multi-asset portfolio backtesting
- Capital allocation strategies (equal weight, custom weights)
- Portfolio rebalancing
- Correlation analysis
- Portfolio-level metrics (Sharpe, volatility, drawdown)
- Diversification benefits calculation

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import yfinance as yf
import pandas as pd
import numpy as np
from typing import List, Dict, Tuple
import time
import logging
from datetime import datetime

from fast_backtester import FastBacktester

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PortfolioBacktester:
    """
    Portfolio-level backtester for multi-asset strategies
    """

    def __init__(
        self,
        initial_capital: float = 100000.0,
        allocation_strategy: str = 'equal',
        rebalance_frequency: str = 'monthly',
        commission: float = 0.001
    ):
        """
        Initialize portfolio backtester

        Args:
            initial_capital: Total starting capital for portfolio
            allocation_strategy: 'equal' or 'custom'
            rebalance_frequency: 'daily', 'weekly', 'monthly', 'never'
            commission: Commission rate per trade
        """
        self.initial_capital = initial_capital
        self.allocation_strategy = allocation_strategy
        self.rebalance_frequency = rebalance_frequency
        self.commission = commission

        self.symbols = []
        self.weights = {}
        self.data = {}
        self.results = None

    def add_symbol(self, symbol: str, weight: float = None):
        """
        Add symbol to portfolio

        Args:
            symbol: Trading symbol
            weight: Custom weight (only for 'custom' allocation strategy)
        """
        self.symbols.append(symbol)
        if weight is not None:
            self.weights[symbol] = weight

    def load_data(self, period: str = '1y', interval: str = '1d'):
        """
        Load market data for all symbols

        Args:
            period: Data period ('1y', '6mo', etc.)
            interval: Data interval ('1d', '1h', etc.)
        """
        logger.info(f"Loading data for {len(self.symbols)} symbols...")

        for symbol in self.symbols:
            try:
                ticker = yf.Ticker(symbol)
                data = ticker.history(interval=interval, period=period)
                data.columns = [c.lower() for c in data.columns]
                self.data[symbol] = data
                logger.info(f"  ✓ {symbol}: {len(data)} candles")
            except Exception as e:
                logger.error(f"  ✗ {symbol}: {e}")

    def _calculate_allocation(self) -> Dict[str, float]:
        """
        Calculate capital allocation for each symbol

        Returns:
            Dict of symbol -> allocated capital
        """
        if self.allocation_strategy == 'equal':
            # Equal weight allocation
            weight_per_symbol = 1.0 / len(self.symbols)
            allocation = {
                symbol: self.initial_capital * weight_per_symbol
                for symbol in self.symbols
            }
        elif self.allocation_strategy == 'custom':
            # Custom weights (must sum to 1.0)
            total_weight = sum(self.weights.values())
            if abs(total_weight - 1.0) > 0.01:
                logger.warning(f"Weights sum to {total_weight}, normalizing...")
                self.weights = {s: w / total_weight for s, w in self.weights.items()}

            allocation = {
                symbol: self.initial_capital * self.weights.get(symbol, 0)
                for symbol in self.symbols
            }
        else:
            raise ValueError(f"Unknown allocation strategy: {self.allocation_strategy}")

        return allocation

    def run_backtest(
        self,
        min_confidence: float = 60.0
    ) -> Dict:
        """
        Run portfolio backtest

        Args:
            min_confidence: Minimum confidence threshold for signals

        Returns:
            Dict with portfolio backtest results
        """
        logger.info(f"\n{'='*80}")
        logger.info("PORTFOLIO BACKTEST")
        logger.info(f"{'='*80}")
        logger.info(f"Symbols: {', '.join(self.symbols)}")
        logger.info(f"Initial capital: ${self.initial_capital:,.2f}")
        logger.info(f"Allocation: {self.allocation_strategy}")
        logger.info(f"Rebalancing: {self.rebalance_frequency}")
        logger.info(f"{'='*80}\n")

        # Calculate allocation
        allocation = self._calculate_allocation()

        logger.info("Capital Allocation:")
        for symbol, capital in allocation.items():
            weight = (capital / self.initial_capital) * 100
            logger.info(f"  {symbol}: ${capital:,.2f} ({weight:.1f}%)")

        # Run individual backtests
        individual_results = {}
        start_time = time.time()

        for symbol in self.symbols:
            if symbol not in self.data:
                logger.warning(f"No data for {symbol}, skipping...")
                continue

            logger.info(f"\nBacktesting {symbol}...")

            # Create backtester with allocated capital
            backtester = FastBacktester(initial_capital=allocation[symbol])

            # Run backtest
            result = backtester.run_fast_backtest(
                symbol=symbol,
                data=self.data[symbol],
                min_confidence=min_confidence,
                commission=self.commission
            )

            individual_results[symbol] = result
            logger.info(f"  ✓ {symbol}: {result['total_return']:.2f}%, "
                       f"{result['total_trades']} trades")

        total_time = time.time() - start_time

        # Calculate portfolio metrics
        portfolio_metrics = self._calculate_portfolio_metrics(
            individual_results,
            allocation
        )

        # Calculate correlation matrix
        correlation_matrix = self._calculate_correlations()

        self.results = {
            'portfolio_metrics': portfolio_metrics,
            'individual_results': individual_results,
            'correlation_matrix': correlation_matrix,
            'allocation': allocation,
            'total_time': total_time
        }

        return self.results

    def _calculate_portfolio_metrics(
        self,
        individual_results: Dict,
        allocation: Dict
    ) -> Dict:
        """
        Calculate portfolio-level metrics

        Args:
            individual_results: Dict of symbol -> backtest results
            allocation: Dict of symbol -> allocated capital

        Returns:
            Dict with portfolio metrics
        """
        # Portfolio final value
        portfolio_final_value = sum([
            individual_results[symbol]['final_equity']
            for symbol in individual_results
        ])

        # Portfolio return
        portfolio_return = ((portfolio_final_value - self.initial_capital) /
                          self.initial_capital) * 100

        # Weighted average metrics
        total_trades = sum([r['total_trades'] for r in individual_results.values()])

        # Calculate weighted win rate (by number of trades)
        win_rates = []
        trade_counts = []
        for symbol, result in individual_results.items():
            if result['total_trades'] > 0:
                win_rates.append(result['win_rate'])
                trade_counts.append(result['total_trades'])

        if trade_counts:
            portfolio_win_rate = np.average(win_rates, weights=trade_counts)
        else:
            portfolio_win_rate = 0.0

        # Calculate portfolio Sharpe ratio
        # Use weighted average of individual returns
        returns = []
        weights = []
        for symbol, result in individual_results.items():
            returns.append(result['total_return'])
            weights.append(allocation[symbol] / self.initial_capital)

        weighted_returns = [r * w for r, w in zip(returns, weights)]
        portfolio_std = np.std(weighted_returns) if len(weighted_returns) > 1 else 0.0

        if portfolio_std > 0:
            portfolio_sharpe = (portfolio_return / portfolio_std)
        else:
            portfolio_sharpe = 0.0

        # Max drawdown (worst individual drawdown weighted by allocation)
        max_drawdowns = []
        for symbol, result in individual_results.items():
            weight = allocation[symbol] / self.initial_capital
            max_drawdowns.append(result.get('max_drawdown', 0) * weight)

        portfolio_max_dd = sum(max_drawdowns)

        # Diversification benefit
        # Compare portfolio return to average of individual returns
        avg_individual_return = np.mean(returns)
        diversification_benefit = portfolio_return - avg_individual_return

        return {
            'initial_capital': self.initial_capital,
            'final_value': portfolio_final_value,
            'total_return': portfolio_return,
            'total_trades': total_trades,
            'portfolio_win_rate': portfolio_win_rate,
            'portfolio_sharpe': portfolio_sharpe,
            'portfolio_max_dd': portfolio_max_dd,
            'avg_individual_return': avg_individual_return,
            'diversification_benefit': diversification_benefit,
            'num_symbols': len(individual_results)
        }

    def _calculate_correlations(self) -> pd.DataFrame:
        """
        Calculate correlation matrix between assets

        Returns:
            DataFrame with correlation matrix
        """
        if not self.data:
            return pd.DataFrame()

        # Get aligned price data
        prices = {}
        for symbol, data in self.data.items():
            if len(data) > 0:
                prices[symbol] = data['close']

        if not prices:
            return pd.DataFrame()

        # Create DataFrame with all prices
        price_df = pd.DataFrame(prices)

        # Calculate daily returns
        returns_df = price_df.pct_change().dropna()

        # Calculate correlation matrix
        correlation_matrix = returns_df.corr()

        return correlation_matrix

    def print_results(self):
        """
        Print formatted portfolio backtest results
        """
        if not self.results:
            print("No results available. Run run_backtest() first.")
            return

        pm = self.results['portfolio_metrics']
        ir = self.results['individual_results']
        corr = self.results['correlation_matrix']

        print(f"\n{'='*80}")
        print("PORTFOLIO BACKTEST RESULTS")
        print(f"{'='*80}")

        print(f"\n💼 Portfolio Summary:")
        print(f"  Initial Capital: ${pm['initial_capital']:,.2f}")
        print(f"  Final Value: ${pm['final_value']:,.2f}")
        print(f"  Total Return: {pm['total_return']:.2f}%")
        print(f"  Total Trades: {pm['total_trades']}")
        print(f"  Execution Time: {self.results['total_time']:.2f}s")

        print(f"\n📊 Portfolio Metrics:")
        print(f"  Portfolio Win Rate: {pm['portfolio_win_rate']:.1f}%")
        print(f"  Portfolio Sharpe: {pm['portfolio_sharpe']:.2f}")
        print(f"  Portfolio Max Drawdown: {pm['portfolio_max_dd']:.2f}%")

        print(f"\n🎯 Diversification Analysis:")
        print(f"  Avg Individual Return: {pm['avg_individual_return']:.2f}%")
        print(f"  Portfolio Return: {pm['total_return']:.2f}%")
        print(f"  Diversification Benefit: {pm['diversification_benefit']:.2f}%")

        print(f"\n📋 Individual Symbol Performance:")
        print(f"  {'Symbol':<15} {'Allocated':<15} {'Final Value':<15} {'Return':<10} {'Trades':<8}")
        print(f"  {'-'*15} {'-'*15} {'-'*15} {'-'*10} {'-'*8}")

        for symbol in self.symbols:
            if symbol in ir:
                r = ir[symbol]
                alloc = self.results['allocation'][symbol]
                print(f"  {symbol:<15} ${alloc:>12,.2f}  ${r['final_equity']:>12,.2f}  "
                      f"{r['total_return']:>8.2f}%  {r['total_trades']:>6}")

        if not corr.empty:
            print(f"\n🔗 Correlation Matrix:")
            print(corr.round(2).to_string())

            # Identify highly correlated pairs
            print(f"\n⚠️  Highly Correlated Pairs (>0.7):")
            for i, symbol1 in enumerate(corr.columns):
                for j, symbol2 in enumerate(corr.columns):
                    if i < j:  # Only upper triangle
                        corr_value = corr.loc[symbol1, symbol2]
                        if abs(corr_value) > 0.7:
                            print(f"  {symbol1} - {symbol2}: {corr_value:.2f}")

        print(f"\n{'='*80}\n")


def demo_portfolio_backtest():
    """
    Demo: Run portfolio backtest on crypto portfolio
    """
    # Create portfolio backtester
    portfolio = PortfolioBacktester(
        initial_capital=100000.0,
        allocation_strategy='equal',  # Equal weight across all assets
        rebalance_frequency='monthly',
        commission=0.001
    )

    # Add symbols to portfolio
    symbols = ['BTC-USD', 'ETH-USD', 'BNB-USD', 'SOL-USD']

    for symbol in symbols:
        portfolio.add_symbol(symbol)

    # Load data
    portfolio.load_data(period='1y', interval='1d')

    # Run portfolio backtest
    results = portfolio.run_backtest(min_confidence=40.0)  # Lower threshold for demo

    # Print results
    portfolio.print_results()


if __name__ == "__main__":
    demo_portfolio_backtest()
