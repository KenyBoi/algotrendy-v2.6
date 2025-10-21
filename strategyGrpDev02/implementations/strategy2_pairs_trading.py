"""
Strategy #2: Statistical Arbitrage Pairs Trading (Baseline)
Based on: "Statistical Arbitrage in the US Equities Market" and O-U process papers

This is the BASELINE implementation without MEM enhancements.
Uses traditional cointegration testing and fixed z-score thresholds.
"""

import numpy as np
import pandas as pd
from typing import Dict, List, Tuple, Optional
from datetime import datetime
from scipy import stats
from statsmodels.tsa.stattools import coint, adfuller

class PairsTradingStrategy:
    """
    Baseline statistical arbitrage pairs trading strategy

    Parameters:
    - Lookback for spread statistics: 60 days
    - Entry z-score threshold: 2.0
    - Exit z-score threshold: 0.5
    - Stop loss z-score: 4.0
    - Rebalancing: Daily
    """

    def __init__(
        self,
        lookback_period: int = 60,
        entry_threshold: float = 2.0,
        exit_threshold: float = 0.5,
        stop_loss_threshold: float = 4.0,
        cointegration_pvalue: float = 0.05,
        base_capital: float = 100000.0
    ):
        self.lookback_period = lookback_period
        self.entry_threshold = entry_threshold
        self.exit_threshold = exit_threshold
        self.stop_loss_threshold = stop_loss_threshold
        self.cointegration_pvalue = cointegration_pvalue
        self.base_capital = base_capital

        # State tracking
        self.current_positions = {}  # {pair_name: {'asset_a': qty, 'asset_b': qty, 'direction': 1/-1}}
        self.trades = []
        self.equity_curve = []

    def test_cointegration(
        self,
        series_a: pd.Series,
        series_b: pd.Series
    ) -> Tuple[bool, float, float]:
        """
        Test if two series are cointegrated using Engle-Granger test

        Args:
            series_a: First price series
            series_b: Second price series

        Returns:
            (is_cointegrated, p_value, hedge_ratio)
        """
        # Align series
        df = pd.DataFrame({'a': series_a, 'b': series_b}).dropna()

        if len(df) < 30:  # Need minimum data
            return False, 1.0, 0.0

        # Engle-Granger cointegration test
        score, p_value, _ = coint(df['a'], df['b'])

        # Calculate hedge ratio (beta from regression)
        from scipy.stats import linregress
        slope, intercept, r_value, p_val, std_err = linregress(df['b'], df['a'])

        is_cointegrated = p_value < self.cointegration_pvalue
        hedge_ratio = slope

        return is_cointegrated, p_value, hedge_ratio

    def calculate_spread(
        self,
        price_a: float,
        price_b: float,
        hedge_ratio: float
    ) -> float:
        """
        Calculate spread between two assets

        Args:
            price_a: Price of asset A
            price_b: Price of asset B
            hedge_ratio: Hedge ratio (beta)

        Returns:
            Spread value
        """
        # Simple spread: A - beta * B
        spread = price_a - hedge_ratio * price_b
        return spread

    def calculate_z_score(
        self,
        spread: pd.Series,
        current_spread: float
    ) -> float:
        """
        Calculate z-score of current spread

        Args:
            spread: Historical spread series
            current_spread: Current spread value

        Returns:
            Z-score
        """
        if len(spread) < 2:
            return 0.0

        mean_spread = spread.mean()
        std_spread = spread.std()

        if std_spread == 0:
            return 0.0

        z_score = (current_spread - mean_spread) / std_spread
        return z_score

    def generate_signal(
        self,
        pair_name: str,
        price_a: float,
        price_b: float,
        historical_a: pd.Series,
        historical_b: pd.Series,
        hedge_ratio: float,
        date: datetime
    ) -> Optional[Dict]:
        """
        Generate trading signal for a pair

        Args:
            pair_name: Name of the pair (e.g., "BTC_ETH")
            price_a: Current price of asset A
            price_b: Current price of asset B
            historical_a: Historical prices of asset A
            historical_b: Historical prices of asset B
            hedge_ratio: Hedge ratio between assets
            date: Current date

        Returns:
            Signal dictionary or None
        """
        # Calculate historical spreads
        spread_history = historical_a - hedge_ratio * historical_b

        # Get lookback window
        if len(spread_history) > self.lookback_period:
            spread_window = spread_history.iloc[-self.lookback_period:]
        else:
            spread_window = spread_history

        # Calculate current spread
        current_spread = self.calculate_spread(price_a, price_b, hedge_ratio)

        # Calculate z-score
        z_score = self.calculate_z_score(spread_window, current_spread)

        # Check current position
        current_position = self.current_positions.get(pair_name, None)

        # Generate signal based on z-score
        signal = None

        if current_position is None:
            # No position - check for entry
            if z_score > self.entry_threshold:
                # Spread is too high - SHORT spread (short A, long B)
                signal = {
                    'date': date,
                    'pair': pair_name,
                    'action': 'ENTER_SHORT',
                    'z_score': z_score,
                    'spread': current_spread,
                    'price_a': price_a,
                    'price_b': price_b,
                    'hedge_ratio': hedge_ratio
                }
            elif z_score < -self.entry_threshold:
                # Spread is too low - LONG spread (long A, short B)
                signal = {
                    'date': date,
                    'pair': pair_name,
                    'action': 'ENTER_LONG',
                    'z_score': z_score,
                    'spread': current_spread,
                    'price_a': price_a,
                    'price_b': price_b,
                    'hedge_ratio': hedge_ratio
                }
        else:
            # Have position - check for exit
            position_direction = current_position['direction']

            # Check stop loss
            if abs(z_score) > self.stop_loss_threshold:
                signal = {
                    'date': date,
                    'pair': pair_name,
                    'action': 'CLOSE_STOP_LOSS',
                    'z_score': z_score,
                    'spread': current_spread,
                    'price_a': price_a,
                    'price_b': price_b,
                    'reason': 'Stop loss triggered'
                }
            # Check mean reversion
            elif abs(z_score) < self.exit_threshold:
                signal = {
                    'date': date,
                    'pair': pair_name,
                    'action': 'CLOSE_PROFIT',
                    'z_score': z_score,
                    'spread': current_spread,
                    'price_a': price_a,
                    'price_b': price_b,
                    'reason': 'Mean reversion complete'
                }

        return signal

    def execute_trade(
        self,
        signal: Dict,
        transaction_cost: float = 0.001
    ) -> Optional[Dict]:
        """
        Execute trade based on signal

        Args:
            signal: Signal dictionary
            transaction_cost: Transaction cost per leg (0.1% default)

        Returns:
            Trade execution details
        """
        if signal is None:
            return None

        pair_name = signal['pair']
        action = signal['action']

        # Calculate position sizes
        # Equal dollar allocation to each leg
        capital_per_leg = self.base_capital * 0.5

        qty_a = capital_per_leg / signal['price_a']
        qty_b = capital_per_leg / signal['price_b'] * signal['hedge_ratio']

        trade = {
            'date': signal['date'],
            'pair': pair_name,
            'action': action,
            'z_score': signal['z_score'],
            'price_a': signal['price_a'],
            'price_b': signal['price_b']
        }

        if action in ['ENTER_LONG', 'ENTER_SHORT']:
            # Open new position
            if action == 'ENTER_LONG':
                # Long A, Short B
                direction = 1
            else:
                # Short A, Long B
                direction = -1

            self.current_positions[pair_name] = {
                'direction': direction,
                'qty_a': qty_a * direction,
                'qty_b': qty_b * -direction,  # Opposite direction
                'entry_price_a': signal['price_a'],
                'entry_price_b': signal['price_b'],
                'entry_z': signal['z_score'],
                'entry_date': signal['date']
            }

            # Calculate transaction costs (both legs)
            cost = (capital_per_leg + capital_per_leg) * transaction_cost
            trade['transaction_cost'] = cost

        elif action in ['CLOSE_PROFIT', 'CLOSE_STOP_LOSS']:
            # Close existing position
            if pair_name in self.current_positions:
                position = self.current_positions[pair_name]

                # Calculate P&L
                pnl_a = position['qty_a'] * (signal['price_a'] - position['entry_price_a'])
                pnl_b = position['qty_b'] * (signal['price_b'] - position['entry_price_b'])
                total_pnl = pnl_a + pnl_b

                # Calculate transaction costs
                cost = (abs(position['qty_a']) * signal['price_a'] +
                       abs(position['qty_b']) * signal['price_b']) * transaction_cost

                trade['pnl'] = total_pnl - cost
                trade['pnl_pct'] = total_pnl / self.base_capital
                trade['transaction_cost'] = cost
                trade['entry_z'] = position['entry_z']
                trade['exit_z'] = signal['z_score']
                trade['hold_days'] = (signal['date'] - position['entry_date']).days

                # Remove position
                del self.current_positions[pair_name]

        self.trades.append(trade)
        return trade

    def find_cointegrated_pairs(
        self,
        prices: Dict[str, pd.Series],
        min_data_points: int = 252
    ) -> List[Dict]:
        """
        Find all cointegrated pairs in the universe

        Args:
            prices: Dictionary of {symbol: price_series}
            min_data_points: Minimum data points required

        Returns:
            List of cointegrated pairs with metadata
        """
        symbols = list(prices.keys())
        cointegrated_pairs = []

        for i in range(len(symbols)):
            for j in range(i + 1, len(symbols)):
                symbol_a = symbols[i]
                symbol_b = symbols[j]

                # Align series
                df = pd.DataFrame({
                    symbol_a: prices[symbol_a],
                    symbol_b: prices[symbol_b]
                }).dropna()

                if len(df) < min_data_points:
                    continue

                # Test cointegration
                is_coint, p_value, hedge_ratio = self.test_cointegration(
                    df[symbol_a],
                    df[symbol_b]
                )

                if is_coint:
                    pair_name = f"{symbol_a}_{symbol_b}"
                    cointegrated_pairs.append({
                        'pair_name': pair_name,
                        'asset_a': symbol_a,
                        'asset_b': symbol_b,
                        'p_value': p_value,
                        'hedge_ratio': hedge_ratio,
                        'correlation': df[symbol_a].corr(df[symbol_b])
                    })

        return cointegrated_pairs

    def backtest(
        self,
        prices: Dict[str, pd.DataFrame],
        pairs: List[Dict],
        transaction_cost: float = 0.001
    ) -> Dict:
        """
        Run backtest on historical price data for multiple pairs

        Args:
            prices: Dictionary of {symbol: DataFrame with 'CLOSE' column}
            pairs: List of pairs to trade (from find_cointegrated_pairs)
            transaction_cost: Transaction cost per leg

        Returns:
            Backtest results dictionary
        """
        # Reset state
        self.trades = []
        self.equity_curve = []
        self.current_positions = {}

        # Get common date range
        all_dates = None
        for symbol, df in prices.items():
            if all_dates is None:
                all_dates = df.index
            else:
                all_dates = all_dates.intersection(df.index)

        if len(all_dates) == 0:
            return {'error': 'No common dates across all symbols'}

        # Initialize equity
        equity = self.base_capital

        # Run backtest day by day
        for date_idx, date in enumerate(all_dates):
            daily_pnl = 0.0

            # Process each pair
            for pair in pairs:
                asset_a = pair['asset_a']
                asset_b = pair['asset_b']
                pair_name = pair['pair_name']
                hedge_ratio = pair['hedge_ratio']

                # Get current prices
                price_a = prices[asset_a].loc[date, 'CLOSE']
                price_b = prices[asset_b].loc[date, 'CLOSE']

                # Get historical data up to current date
                historical_a = prices[asset_a].loc[:date, 'CLOSE']
                historical_b = prices[asset_b].loc[:date, 'CLOSE']

                # Generate signal
                signal = self.generate_signal(
                    pair_name,
                    price_a,
                    price_b,
                    historical_a,
                    historical_b,
                    hedge_ratio,
                    date
                )

                # Execute trade
                trade = self.execute_trade(signal, transaction_cost)

                # Update equity if trade closed
                if trade and 'pnl' in trade:
                    daily_pnl += trade['pnl']

            # Update equity
            equity += daily_pnl

            # Track equity curve
            self.equity_curve.append({
                'date': date,
                'equity': equity,
                'daily_pnl': daily_pnl,
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

        # Win rate (from closed trades)
        closed_trades = [t for t in self.trades if 'pnl' in t]
        if len(closed_trades) > 0:
            winning_trades = sum(1 for t in closed_trades if t['pnl'] > 0)
            win_rate = winning_trades / len(closed_trades)
            avg_win = np.mean([t['pnl'] for t in closed_trades if t['pnl'] > 0]) if winning_trades > 0 else 0
            avg_loss = np.mean([t['pnl'] for t in closed_trades if t['pnl'] < 0]) if (len(closed_trades) - winning_trades) > 0 else 0
            profit_factor = abs(avg_win / avg_loss) if avg_loss != 0 else 0
        else:
            win_rate = 0.0
            avg_win = 0.0
            avg_loss = 0.0
            profit_factor = 0.0

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
            'win_rate': win_rate,
            'profit_factor': profit_factor,
            'avg_win': avg_win,
            'avg_loss': avg_loss,
            'num_trades': len(closed_trades),
            'final_equity': equity_df['equity'].iloc[-1],
            'total_periods': len(equity_df)
        }

        return metrics


def run_example():
    """Example usage of PairsTradingStrategy"""

    # Create sample price data for 3 assets
    dates = pd.date_range('2020-01-01', '2024-10-21', freq='D')
    np.random.seed(42)

    # Asset A and B will be cointegrated
    common_trend = np.cumsum(np.random.normal(0, 0.01, len(dates)))
    noise_a = np.cumsum(np.random.normal(0, 0.005, len(dates)))
    noise_b = np.cumsum(np.random.normal(0, 0.005, len(dates)))

    price_a = 100 * np.exp(common_trend + noise_a)
    price_b = 80 * np.exp(common_trend + noise_b)
    price_c = 100 * np.exp(np.cumsum(np.random.normal(0.0005, 0.02, len(dates))))  # Independent

    prices = {
        'ASSET_A': pd.DataFrame({'CLOSE': price_a}, index=dates),
        'ASSET_B': pd.DataFrame({'CLOSE': price_b}, index=dates),
        'ASSET_C': pd.DataFrame({'CLOSE': price_c}, index=dates)
    }

    # Run strategy
    strategy = PairsTradingStrategy()

    # Find cointegrated pairs
    print("Finding cointegrated pairs...")
    price_series = {symbol: df['CLOSE'] for symbol, df in prices.items()}
    pairs = strategy.find_cointegrated_pairs(price_series)

    print(f"Found {len(pairs)} cointegrated pairs:")
    for pair in pairs:
        print(f"  {pair['pair_name']}: p-value={pair['p_value']:.4f}, correlation={pair['correlation']:.4f}")

    if len(pairs) == 0:
        print("No cointegrated pairs found. Exiting.")
        return None, None

    # Run backtest
    print("\nRunning backtest...")
    results = strategy.backtest(prices, pairs)

    # Print results
    print("\n" + "=" * 60)
    print("PAIRS TRADING - BASELINE BACKTEST RESULTS")
    print("=" * 60)
    print(f"Total Return:      {results['total_return']*100:>8.2f}%")
    print(f"CAGR:              {results['cagr']*100:>8.2f}%")
    print(f"Sharpe Ratio:      {results['sharpe_ratio']:>8.2f}")
    print(f"Sortino Ratio:     {results['sortino_ratio']:>8.2f}")
    print(f"Max Drawdown:      {results['max_drawdown']*100:>8.2f}%")
    print(f"Calmar Ratio:      {results['calmar_ratio']:>8.2f}")
    print(f"Win Rate:          {results['win_rate']*100:>8.2f}%")
    print(f"Profit Factor:     {results['profit_factor']:>8.2f}")
    print(f"Number of Trades:  {results['num_trades']:>8}")
    print(f"Final Equity:      ${results['final_equity']:>8,.2f}")
    print("=" * 60)

    return strategy, results


if __name__ == '__main__':
    strategy, results = run_example()
