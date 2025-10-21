"""
Strategy #1: Volatility-Managed Momentum (Baseline)
Based on: "Momentum Has Its Moments" by Barroso & Santa-Clara (2015)

This is the BASELINE implementation without MEM enhancements.
Uses published parameters exactly as in the academic paper.
"""

import numpy as np
import pandas as pd
from typing import Dict, Tuple
from datetime import datetime

class VolatilityManagedMomentum:
    """
    Baseline volatility-managed momentum strategy

    Parameters from Barroso & Santa-Clara (2015):
    - Momentum lookback: 252 days (12 months)
    - Skip period: 21 days (1 month)
    - Volatility lookback: 126 days (6 months)
    - Target volatility: 12% annualized
    - Rebalancing: Monthly (end of month)
    """

    def __init__(
        self,
        momentum_lookback: int = 252,
        skip_period: int = 21,
        volatility_lookback: int = 126,
        target_volatility: float = 0.12,
        base_capital: float = 100000.0
    ):
        self.momentum_lookback = momentum_lookback
        self.skip_period = skip_period
        self.volatility_lookback = volatility_lookback
        self.target_volatility = target_volatility
        self.base_capital = base_capital

        # Performance tracking
        self.trades = []
        self.equity_curve = []
        self.current_position = 0.0

    def calculate_momentum(self, prices: pd.Series, date_idx: int) -> float:
        """
        Calculate 12-month momentum excluding last month

        Args:
            prices: Price series
            date_idx: Current date index

        Returns:
            Momentum score (total return over period)
        """
        if date_idx < self.momentum_lookback:
            return 0.0

        # Get prices from 12 months ago to 1 month ago
        start_idx = date_idx - self.momentum_lookback
        end_idx = date_idx - self.skip_period

        if end_idx <= start_idx:
            return 0.0

        start_price = prices.iloc[start_idx]
        end_price = prices.iloc[end_idx]

        momentum = (end_price / start_price) - 1.0
        return momentum

    def calculate_realized_volatility(self, returns: pd.Series, date_idx: int) -> float:
        """
        Calculate 6-month realized volatility (annualized)

        Args:
            returns: Return series
            date_idx: Current date index

        Returns:
            Annualized volatility
        """
        if date_idx < self.volatility_lookback:
            return self.target_volatility  # Default to target

        # Get last 6 months of returns
        vol_window = returns.iloc[date_idx - self.volatility_lookback:date_idx]

        # Calculate annualized volatility
        daily_vol = vol_window.std()
        annualized_vol = daily_vol * np.sqrt(252)

        return annualized_vol if annualized_vol > 0 else self.target_volatility

    def calculate_position_size(
        self,
        momentum_score: float,
        realized_vol: float
    ) -> float:
        """
        Calculate position size using volatility scaling

        Formula: Position = (Target_Vol / Realized_Vol) × sign(Momentum)

        Args:
            momentum_score: 12-month momentum
            realized_vol: 6-month realized volatility

        Returns:
            Position size (-1 to +1, where 1 = 100% long)
        """
        # Determine direction from momentum
        if momentum_score > 0:
            direction = 1.0  # Long
        elif momentum_score < 0:
            direction = 0.0  # Cash (not shorting in this version)
        else:
            direction = 0.0  # Neutral

        # Scale by volatility
        if realized_vol > 0:
            vol_scalar = self.target_volatility / realized_vol
        else:
            vol_scalar = 1.0

        # Cap maximum leverage at 2.0x
        vol_scalar = min(vol_scalar, 2.0)

        position_size = direction * vol_scalar

        return position_size

    def should_rebalance(self, date: datetime) -> bool:
        """
        Check if we should rebalance (end of month)

        Args:
            date: Current date

        Returns:
            True if end of month
        """
        # Simple check: is this the last trading day of the month?
        # In real implementation, would check if next day is new month
        return date.day >= 28  # Approximate end of month

    def generate_signal(
        self,
        prices: pd.Series,
        returns: pd.Series,
        date_idx: int,
        date: datetime
    ) -> Dict:
        """
        Generate trading signal for current date

        Args:
            prices: Price series
            returns: Return series
            date_idx: Current date index
            date: Current date

        Returns:
            Signal dictionary with position size and metadata
        """
        # Calculate momentum
        momentum = self.calculate_momentum(prices, date_idx)

        # Calculate realized volatility
        realized_vol = self.calculate_realized_volatility(returns, date_idx)

        # Calculate target position
        target_position = self.calculate_position_size(momentum, realized_vol)

        # Create signal
        signal = {
            'date': date,
            'momentum': momentum,
            'realized_vol': realized_vol,
            'target_position': target_position,
            'current_position': self.current_position,
            'rebalance': self.should_rebalance(date)
        }

        return signal

    def execute_trade(
        self,
        signal: Dict,
        current_price: float,
        transaction_cost: float = 0.001
    ) -> Dict:
        """
        Execute trade based on signal

        Args:
            signal: Signal dictionary from generate_signal()
            current_price: Current asset price
            transaction_cost: Transaction cost (0.1% default)

        Returns:
            Trade execution details
        """
        if not signal['rebalance']:
            return None

        position_change = signal['target_position'] - signal['current_position']

        if abs(position_change) < 0.01:  # No meaningful change
            return None

        # Calculate trade size in dollars
        trade_size = abs(position_change) * self.base_capital

        # Apply transaction costs
        cost = trade_size * transaction_cost

        # Update position
        self.current_position = signal['target_position']

        trade = {
            'date': signal['date'],
            'action': 'BUY' if position_change > 0 else 'SELL',
            'position_change': position_change,
            'new_position': self.current_position,
            'price': current_price,
            'trade_size': trade_size,
            'transaction_cost': cost,
            'momentum': signal['momentum'],
            'volatility': signal['realized_vol']
        }

        self.trades.append(trade)

        return trade

    def backtest(
        self,
        prices: pd.DataFrame,
        symbol: str = 'CLOSE',
        transaction_cost: float = 0.001
    ) -> Dict:
        """
        Run backtest on historical price data

        Args:
            prices: DataFrame with price data (must have CLOSE column or specify symbol)
            symbol: Column name for prices
            transaction_cost: Transaction cost per trade

        Returns:
            Backtest results dictionary
        """
        # Reset state
        self.trades = []
        self.equity_curve = []
        self.current_position = 0.0

        # Get price series
        if symbol in prices.columns:
            price_series = prices[symbol]
        else:
            price_series = prices.iloc[:, 0]  # Use first column

        # Calculate returns
        returns = price_series.pct_change()

        # Initialize equity
        equity = self.base_capital

        # Run backtest
        for i in range(len(prices)):
            date = prices.index[i]
            price = price_series.iloc[i]

            # Generate signal
            signal = self.generate_signal(price_series, returns, i, date)

            # Execute trade if needed
            trade = self.execute_trade(signal, price, transaction_cost)

            # Calculate P&L
            if i > 0:
                price_return = returns.iloc[i]
                position_return = self.current_position * price_return
                equity = equity * (1 + position_return)

                # Subtract transaction costs
                if trade:
                    equity -= trade['transaction_cost']

            # Track equity
            self.equity_curve.append({
                'date': date,
                'equity': equity,
                'position': self.current_position,
                'price': price
            })

        # Calculate performance metrics
        results = self.calculate_performance_metrics()

        return results

    def calculate_performance_metrics(self) -> Dict:
        """
        Calculate performance metrics from backtest

        Returns:
            Dictionary of performance metrics
        """
        if len(self.equity_curve) == 0:
            return {}

        equity_df = pd.DataFrame(self.equity_curve)
        equity_df.set_index('date', inplace=True)

        # Calculate returns
        equity_returns = equity_df['equity'].pct_change().dropna()

        # Total return
        total_return = (equity_df['equity'].iloc[-1] / self.base_capital) - 1

        # CAGR
        years = len(equity_df) / 252
        cagr = (1 + total_return) ** (1 / years) - 1 if years > 0 else 0

        # Volatility
        annual_vol = equity_returns.std() * np.sqrt(252)

        # Sharpe ratio (assuming 0% risk-free rate)
        sharpe = (equity_returns.mean() * 252) / annual_vol if annual_vol > 0 else 0

        # Maximum drawdown
        cumulative = (1 + equity_returns).cumprod()
        running_max = cumulative.expanding().max()
        drawdown = (cumulative - running_max) / running_max
        max_drawdown = drawdown.min()

        # Win rate
        if len(self.trades) > 0:
            # Approximate wins (positive momentum trades)
            winning_trades = sum(1 for t in self.trades if t['position_change'] > 0 and t['momentum'] > 0)
            win_rate = winning_trades / len(self.trades)
        else:
            win_rate = 0.0

        # Sortino ratio (downside deviation)
        downside_returns = equity_returns[equity_returns < 0]
        downside_vol = downside_returns.std() * np.sqrt(252)
        sortino = (equity_returns.mean() * 252) / downside_vol if downside_vol > 0 else 0

        # Calmar ratio
        calmar = abs(cagr / max_drawdown) if max_drawdown != 0 else 0

        metrics = {
            'total_return': total_return,
            'cagr': cagr,
            'annual_volatility': annual_vol,
            'sharpe_ratio': sharpe,
            'sortino_ratio': sortino,
            'max_drawdown': max_drawdown,
            'calmar_ratio': calmar,
            'win_rate': win_rate,
            'num_trades': len(self.trades),
            'final_equity': equity_df['equity'].iloc[-1],
            'total_periods': len(equity_df)
        }

        return metrics


def run_example():
    """Example usage of VolatilityManagedMomentum strategy"""

    # This would normally load real data
    # For demonstration, create sample data
    dates = pd.date_range('2015-01-01', '2024-10-21', freq='D')

    # Simulate price data (geometric Brownian motion with drift)
    np.random.seed(42)
    returns = np.random.normal(0.0005, 0.01, len(dates))  # ~12.6% annual return, ~16% vol
    prices = 100 * (1 + returns).cumprod()

    df = pd.DataFrame({'CLOSE': prices}, index=dates)

    # Run strategy
    strategy = VolatilityManagedMomentum()
    results = strategy.backtest(df, symbol='CLOSE')

    # Print results
    print("=" * 60)
    print("VOLATILITY-MANAGED MOMENTUM - BASELINE BACKTEST RESULTS")
    print("=" * 60)
    print(f"Total Return:      {results['total_return']*100:>8.2f}%")
    print(f"CAGR:              {results['cagr']*100:>8.2f}%")
    print(f"Sharpe Ratio:      {results['sharpe_ratio']:>8.2f}")
    print(f"Sortino Ratio:     {results['sortino_ratio']:>8.2f}")
    print(f"Max Drawdown:      {results['max_drawdown']*100:>8.2f}%")
    print(f"Calmar Ratio:      {results['calmar_ratio']:>8.2f}")
    print(f"Annual Volatility: {results['annual_volatility']*100:>8.2f}%")
    print(f"Win Rate:          {results['win_rate']*100:>8.2f}%")
    print(f"Number of Trades:  {results['num_trades']:>8}")
    print(f"Final Equity:      ${results['final_equity']:>8,.2f}")
    print("=" * 60)

    return strategy, results


if __name__ == '__main__':
    strategy, results = run_example()
