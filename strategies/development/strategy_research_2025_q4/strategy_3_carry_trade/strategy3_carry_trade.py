"""
Strategy #3: Currency Carry Trade (Baseline)
Based on: "The Carry Trade: Risks and Drawdowns" by Daniel, Hodrick & Lu (2017)

This is the BASELINE implementation without MEM enhancements.
Uses traditional interest rate differential ranking with equal weighting.
"""

import numpy as np
import pandas as pd
from typing import Dict, List
from datetime import datetime

class CurrencyCarryTrade:
    """
    Baseline currency carry trade strategy

    Parameters:
    - Rebalancing: Monthly
    - Position sizing: Equal-weighted
    - Long top 3 currencies, Short bottom 3 currencies
    """

    def __init__(
        self,
        num_long: int = 3,
        num_short: int = 3,
        base_capital: float = 100000.0
    ):
        self.num_long = num_long
        self.num_short = num_short
        self.base_capital = base_capital

        # State tracking
        self.current_positions = {}
        self.trades = []
        self.equity_curve = []

    def calculate_interest_differential(
        self,
        currency: str,
        interest_rates: Dict[str, float],
        base_currency: str = 'USD'
    ) -> float:
        """
        Calculate interest rate differential vs. base currency

        Args:
            currency: Currency code (e.g., 'EUR', 'JPY')
            interest_rates: Dictionary of {currency: interest_rate}
            base_currency: Base currency (default 'USD')

        Returns:
            Interest rate differential
        """
        if currency not in interest_rates or base_currency not in interest_rates:
            return 0.0

        differential = interest_rates[currency] - interest_rates[base_currency]
        return differential

    def rank_currencies(
        self,
        interest_rates: Dict[str, float],
        base_currency: str = 'USD'
    ) -> List[tuple]:
        """
        Rank currencies by interest rate differential

        Args:
            interest_rates: Dictionary of {currency: interest_rate}
            base_currency: Base currency

        Returns:
            List of (currency, differential) tuples, sorted high to low
        """
        differentials = {}

        for currency in interest_rates.keys():
            if currency != base_currency:
                diff = self.calculate_interest_differential(
                    currency,
                    interest_rates,
                    base_currency
                )
                differentials[currency] = diff

        # Sort by differential (high to low)
        ranked = sorted(differentials.items(), key=lambda x: x[1], reverse=True)

        return ranked

    def generate_signals(
        self,
        interest_rates: Dict[str, float],
        date: datetime,
        base_currency: str = 'USD'
    ) -> Dict:
        """
        Generate trading signals based on interest rate differentials

        Args:
            interest_rates: Dictionary of {currency: interest_rate}
            date: Current date
            base_currency: Base currency

        Returns:
            Signal dictionary with long/short positions
        """
        # Rank currencies
        ranked = self.rank_currencies(interest_rates, base_currency)

        if len(ranked) < (self.num_long + self.num_short):
            return None

        # Select top and bottom currencies
        long_currencies = ranked[:self.num_long]
        short_currencies = ranked[-self.num_short:]

        # Calculate equal weights
        weight_per_long = 1.0 / self.num_long
        weight_per_short = -1.0 / self.num_short

        # Create signals
        signals = {
            'date': date,
            'long_positions': {curr: weight_per_long for curr, diff in long_currencies},
            'short_positions': {curr: weight_per_short for curr, diff in short_currencies},
            'long_differentials': {curr: diff for curr, diff in long_currencies},
            'short_differentials': {curr: diff for curr, diff in short_currencies}
        }

        return signals

    def should_rebalance(self, date: datetime, last_rebalance: datetime = None) -> bool:
        """
        Check if we should rebalance (end of month)

        Args:
            date: Current date
            last_rebalance: Last rebalance date

        Returns:
            True if end of month
        """
        if last_rebalance is None:
            return True

        # Check if we've moved to a new month
        return date.month != last_rebalance.month or date.year != last_rebalance.year

    def execute_trades(
        self,
        signals: Dict,
        fx_rates: Dict[str, float],
        transaction_cost: float = 0.001
    ) -> List[Dict]:
        """
        Execute trades based on signals

        Args:
            signals: Signal dictionary from generate_signals()
            fx_rates: Current FX rates vs. base currency
            transaction_cost: Transaction cost (0.1% default)

        Returns:
            List of trade executions
        """
        if signals is None:
            return []

        trades = []
        new_positions = {}

        # Combine long and short positions
        all_positions = {**signals['long_positions'], **signals['short_positions']}

        for currency, weight in all_positions.items():
            if currency not in fx_rates:
                continue

            # Calculate position size in base currency
            position_size = weight * self.base_capital

            # Convert to currency units
            fx_rate = fx_rates[currency]
            currency_units = position_size / fx_rate

            # Check if we need to change position
            current_weight = self.current_positions.get(currency, 0.0)

            if abs(weight - current_weight) > 0.01:  # Meaningful change
                trade = {
                    'date': signals['date'],
                    'currency': currency,
                    'action': 'BUY' if weight > 0 else 'SELL',
                    'old_weight': current_weight,
                    'new_weight': weight,
                    'position_change': weight - current_weight,
                    'fx_rate': fx_rate,
                    'position_size': abs(position_size),
                    'transaction_cost': abs(position_size) * transaction_cost
                }
                trades.append(trade)

            new_positions[currency] = weight

        # Close positions no longer in portfolio
        for currency in list(self.current_positions.keys()):
            if currency not in new_positions:
                old_weight = self.current_positions[currency]
                position_size = abs(old_weight * self.base_capital)

                trade = {
                    'date': signals['date'],
                    'currency': currency,
                    'action': 'CLOSE',
                    'old_weight': old_weight,
                    'new_weight': 0.0,
                    'position_change': -old_weight,
                    'fx_rate': fx_rates.get(currency, 1.0),
                    'position_size': position_size,
                    'transaction_cost': position_size * transaction_cost
                }
                trades.append(trade)

        # Update positions
        self.current_positions = new_positions

        # Track trades
        self.trades.extend(trades)

        return trades

    def calculate_daily_pnl(
        self,
        fx_returns: Dict[str, float],
        interest_rates: Dict[str, float],
        days_held: float = 1.0
    ) -> float:
        """
        Calculate daily P&L from positions

        Args:
            fx_returns: Dictionary of {currency: daily_return}
            interest_rates: Dictionary of {currency: annual_interest_rate}
            days_held: Number of days held (default 1)

        Returns:
            Daily P&L
        """
        total_pnl = 0.0

        for currency, weight in self.current_positions.items():
            # Position value
            position_value = weight * self.base_capital

            # FX return
            fx_return = fx_returns.get(currency, 0.0)
            fx_pnl = position_value * fx_return

            # Interest accrual (pro-rated)
            annual_rate = interest_rates.get(currency, 0.0)
            daily_rate = annual_rate / 365
            interest_pnl = position_value * daily_rate * days_held

            # Total P&L
            total_pnl += fx_pnl + interest_pnl

        return total_pnl

    def backtest(
        self,
        fx_prices: Dict[str, pd.DataFrame],
        interest_rates: Dict[str, pd.DataFrame],
        base_currency: str = 'USD',
        transaction_cost: float = 0.001
    ) -> Dict:
        """
        Run backtest on historical FX and interest rate data

        Args:
            fx_prices: Dictionary of {currency: DataFrame with 'CLOSE' column}
            interest_rates: Dictionary of {currency: DataFrame with 'RATE' column}
            base_currency: Base currency (default 'USD')
            transaction_cost: Transaction cost

        Returns:
            Backtest results dictionary
        """
        # Reset state
        self.trades = []
        self.equity_curve = []
        self.current_positions = {}

        # Get common date range
        all_dates = None
        for symbol, df in fx_prices.items():
            if all_dates is None:
                all_dates = df.index
            else:
                all_dates = all_dates.intersection(df.index)

        if len(all_dates) == 0:
            return {'error': 'No common dates'}

        # Initialize
        equity = self.base_capital
        last_rebalance = None

        # Run backtest day by day
        for date_idx, date in enumerate(all_dates):
            # Get current interest rates
            current_rates = {}
            for currency, df in interest_rates.items():
                if date in df.index:
                    current_rates[currency] = df.loc[date, 'RATE']

            # Check if we should rebalance
            if self.should_rebalance(date, last_rebalance):
                # Generate signals
                signals = self.generate_signals(current_rates, date, base_currency)

                # Get current FX rates
                current_fx = {}
                for currency, df in fx_prices.items():
                    if date in df.index:
                        current_fx[currency] = df.loc[date, 'CLOSE']

                # Execute trades
                trades = self.execute_trades(signals, current_fx, transaction_cost)

                # Deduct transaction costs from equity
                for trade in trades:
                    equity -= trade['transaction_cost']

                last_rebalance = date

            # Calculate daily P&L from positions
            if date_idx > 0:
                # Calculate FX returns
                fx_returns = {}
                for currency, df in fx_prices.items():
                    if date in df.index and date_idx > 0:
                        prev_date = all_dates[date_idx - 1]
                        if prev_date in df.index:
                            fx_returns[currency] = df.loc[date, 'CLOSE'] / df.loc[prev_date, 'CLOSE'] - 1

                # Calculate P&L
                daily_pnl = self.calculate_daily_pnl(fx_returns, current_rates)
                equity += daily_pnl

            # Track equity curve
            self.equity_curve.append({
                'date': date,
                'equity': equity,
                'num_positions': len(self.current_positions)
            })

        # Calculate performance metrics
        results = self.calculate_performance_metrics()

        return results

    def calculate_performance_metrics(self) -> Dict:
        """Calculate performance metrics from backtest"""

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

        # Sharpe ratio
        sharpe = (equity_returns.mean() * 252) / annual_vol if annual_vol > 0 else 0

        # Maximum drawdown
        cumulative = (1 + equity_returns).cumprod()
        running_max = cumulative.expanding().max()
        drawdown = (cumulative - running_max) / running_max
        max_drawdown = drawdown.min()

        # Sortino ratio
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
            'num_trades': len(self.trades),
            'final_equity': equity_df['equity'].iloc[-1],
            'total_periods': len(equity_df)
        }

        return metrics


def run_example():
    """Example usage of CurrencyCarryTrade strategy"""

    # Create sample FX price data (vs USD)
    dates = pd.date_range('2020-01-01', '2024-10-21', freq='D')
    np.random.seed(42)

    # Simulate 6 currencies with different characteristics
    currencies = ['EUR', 'JPY', 'GBP', 'AUD', 'CAD', 'CHF']

    fx_prices = {}
    interest_rates = {}

    for i, currency in enumerate(currencies):
        # FX prices (random walk)
        returns = np.random.normal(0, 0.005, len(dates))
        fx_price = 1.0 * (1 + returns).cumprod()

        fx_prices[currency] = pd.DataFrame({'CLOSE': fx_price}, index=dates)

        # Interest rates (gradually changing)
        base_rate = 0.01 + i * 0.01  # 1% to 6%
        rate_changes = np.random.normal(0, 0.0005, len(dates))
        rates = base_rate + np.cumsum(rate_changes)
        rates = np.clip(rates, 0, 0.10)  # Clip to 0-10%

        interest_rates[currency] = pd.DataFrame({'RATE': rates}, index=dates)

    # Add USD (base currency)
    fx_prices['USD'] = pd.DataFrame({'CLOSE': np.ones(len(dates))}, index=dates)
    interest_rates['USD'] = pd.DataFrame({'RATE': np.full(len(dates), 0.02)}, index=dates)

    # Run strategy
    print("Running Currency Carry Trade backtest...")
    strategy = CurrencyCarryTrade()
    results = strategy.backtest(fx_prices, interest_rates)

    # Print results
    print("\n" + "=" * 60)
    print("CURRENCY CARRY TRADE - BASELINE BACKTEST RESULTS")
    print("=" * 60)
    print(f"Total Return:      {results['total_return']*100:>8.2f}%")
    print(f"CAGR:              {results['cagr']*100:>8.2f}%")
    print(f"Sharpe Ratio:      {results['sharpe_ratio']:>8.2f}")
    print(f"Sortino Ratio:     {results['sortino_ratio']:>8.2f}")
    print(f"Max Drawdown:      {results['max_drawdown']*100:>8.2f}%")
    print(f"Calmar Ratio:      {results['calmar_ratio']:>8.2f}")
    print(f"Annual Volatility: {results['annual_volatility']*100:>8.2f}%")
    print(f"Number of Trades:  {results['num_trades']:>8}")
    print(f"Final Equity:      ${results['final_equity']:>8,.2f}")
    print("=" * 60)

    return strategy, results


if __name__ == '__main__':
    strategy, results = run_example()
