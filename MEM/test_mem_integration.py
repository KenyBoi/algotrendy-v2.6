"""
MEM Integration Test
Tests complete MEM trading system with all components
"""

import sys
import pandas as pd
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List

# Import MEM components
from regime_detector import RegimeDetector, MarketRegime
from mem_position_sizer import MEMPositionSizer
from spread_model import SpreadModel

class MockMEMModel:
    """Mock MEM model for testing"""

    def __init__(self):
        self.accuracy_history = []

    def predict(self, features: pd.DataFrame) -> Dict:
        """
        Mock MEM prediction

        Args:
            features: Market features

        Returns:
            {
                'action': 'BUY' | 'SELL' | 'HOLD',
                'confidence': 0.0-1.0,
                'predicted_price': float,
                'prediction_horizon': int
            }
        """
        # Simulate realistic confidence based on trend
        current_price = features['close'].iloc[-1]
        sma_20 = features['close'].rolling(20).mean().iloc[-1]
        sma_50 = features['close'].rolling(50).mean().iloc[-1]

        # Determine action based on trend
        if sma_20 > sma_50 * 1.01:
            action = 'BUY'
            base_confidence = 0.7
        elif sma_20 < sma_50 * 0.99:
            action = 'SELL'
            base_confidence = 0.7
        else:
            action = 'HOLD'
            base_confidence = 0.4

        # Add some randomness to confidence
        confidence = base_confidence + np.random.uniform(-0.1, 0.1)
        confidence = np.clip(confidence, 0.0, 1.0)

        # Predict price (simple trend projection)
        recent_return = features['close'].pct_change().iloc[-1]
        predicted_price = current_price * (1 + recent_return * 2)

        return {
            'action': action,
            'confidence': confidence,
            'predicted_price': predicted_price,
            'prediction_horizon': 60  # 1 hour
        }


class MEMIntegrationTester:
    """Tests full MEM integration"""

    def __init__(self, capital: float = 10000):
        self.capital = capital
        self.initial_capital = capital

        # Initialize components
        self.mem_model = MockMEMModel()
        self.regime_detector = RegimeDetector()
        self.position_sizer = MEMPositionSizer(base_capital=capital)
        self.spread_model = SpreadModel()

        # Track results
        self.trades = []
        self.positions = []
        self.equity_curve = []
        self.regime_history = []

    def generate_market_data(self, days: int = 30) -> pd.DataFrame:
        """Generate realistic market data"""
        np.random.seed(42)

        # Generate hourly data
        periods = days * 24
        dates = pd.date_range(end=datetime.now(), periods=periods, freq='1h')

        # Simulate price with trend and volatility
        base_price = 66000
        drift = 0.0001  # Slight upward drift
        volatility = 0.015  # 1.5% hourly volatility

        returns = np.random.normal(drift, volatility, periods)
        prices = base_price * (1 + returns).cumprod()

        # Create OHLCV data
        df = pd.DataFrame({
            'timestamp': dates,
            'open': prices * (1 + np.random.uniform(-0.005, 0.005, periods)),
            'high': prices * (1 + np.random.uniform(0, 0.01, periods)),
            'low': prices * (1 + np.random.uniform(-0.01, 0, periods)),
            'close': prices,
            'volume': np.random.uniform(500000, 2000000, periods)
        })

        df.set_index('timestamp', inplace=True)

        # Add technical indicators
        df['returns'] = df['close'].pct_change()
        df['atr'] = self._calculate_atr(df)
        df['volatility'] = df['returns'].rolling(20).std()

        return df

    def _calculate_atr(self, df: pd.DataFrame, period: int = 14) -> pd.Series:
        """Calculate Average True Range"""
        high_low = df['high'] - df['low']
        high_close = np.abs(df['high'] - df['close'].shift())
        low_close = np.abs(df['low'] - df['close'].shift())

        tr = pd.concat([high_low, high_close, low_close], axis=1).max(axis=1)
        atr = tr.rolling(period).mean()

        return atr

    def run_backtest(self, market_data: pd.DataFrame) -> Dict:
        """Run complete backtest with MEM integration"""
        print("="*70)
        print("🧪 MEM INTEGRATION TEST - BACKTEST")
        print("="*70)
        print(f"Initial Capital: ${self.initial_capital:,}")
        print(f"Data Period: {market_data.index[0]} to {market_data.index[-1]}")
        print(f"Total Bars: {len(market_data)}")
        print()

        # v2.5 risk settings (simulating BrokerManager)
        v2_5_risk_settings = {
            'max_position_per_symbol': 2500,
            'min_position_size': 100,
            'max_total_exposure': self.capital
        }

        # Iterate through data
        lookback = 100  # Bars needed for indicators
        for i in range(lookback, len(market_data)):
            current_data = market_data.iloc[:i+1]
            current_bar = current_data.iloc[-1]

            # Update regime
            regime = self.regime_detector.detect_regime(
                current_data['close'].iloc[-100:],
                current_data['returns'].iloc[-100:],
                current_data['volume'].iloc[-100:]
            )
            self.regime_history.append({
                'timestamp': current_bar.name,
                'regime': str(regime)
            })

            # Get MEM prediction
            mem_signal = self.mem_model.predict(current_data.iloc[-100:])

            # Skip if HOLD
            if mem_signal['action'] == 'HOLD':
                continue

            # Check if we have an open position
            if self.positions:
                # Manage existing position
                self._manage_position(current_bar, regime)
            else:
                # Try to enter new position
                self._enter_position(
                    current_bar,
                    mem_signal,
                    regime,
                    v2_5_risk_settings
                )

            # Update equity
            position_value = sum(p['quantity'] * current_bar['close'] for p in self.positions)
            total_equity = self.capital + position_value
            self.equity_curve.append({
                'timestamp': current_bar.name,
                'equity': total_equity,
                'cash': self.capital,
                'positions_value': position_value
            })

        # Calculate results
        return self._calculate_results()

    def _enter_position(
        self,
        bar: pd.Series,
        mem_signal: Dict,
        regime: MarketRegime,
        v2_5_risk_settings: Dict
    ):
        """Enter new position based on MEM signal"""
        current_price = bar['close']
        volatility = bar['volatility']
        atr = bar['atr']

        # Calculate position size
        position_calc = self.position_sizer.calculate_position_size(
            mem_confidence=mem_signal['confidence'],
            current_price=current_price,
            volatility=volatility,
            regime=regime,
            v2_5_risk_settings=v2_5_risk_settings
        )

        # Skip if position size is 0
        if position_calc['position_size_usd'] == 0:
            return

        # Check if we have enough capital
        if position_calc['position_size_usd'] > self.capital:
            return

        # Calculate spread
        spread = self.spread_model.calculate_spread(
            volatility=volatility,
            volume=bar['volume'],
            avg_volume=bar['volume']  # Simplified
        )

        # Get execution price with spread
        execution_price = self.spread_model.get_execution_price(
            market_price=current_price,
            side=mem_signal['action'],
            spread_pct=spread
        )

        # Apply slippage (0.05%)
        slippage = 0.0005
        if mem_signal['action'] == 'BUY':
            execution_price *= (1 + slippage)
        else:
            execution_price *= (1 - slippage)

        # Apply commission (0.1%)
        commission = 0.001
        total_cost = position_calc['position_size_usd'] * (1 + commission)

        # Final check
        if total_cost > self.capital:
            return

        # Calculate stop loss and take profit
        stop_loss = self.position_sizer.get_stop_loss_price(
            entry_price=execution_price,
            side=mem_signal['action'],
            volatility=volatility,
            regime=regime,
            atr=atr
        )

        take_profit = self.position_sizer.get_take_profit_price(
            entry_price=execution_price,
            side=mem_signal['action'],
            mem_confidence=mem_signal['confidence'],
            volatility=volatility,
            atr=atr
        )

        # Create position
        position = {
            'entry_time': bar.name,
            'symbol': 'BTCUSDT',
            'side': mem_signal['action'],
            'quantity': position_calc['quantity'],
            'entry_price': execution_price,
            'stop_loss': stop_loss,
            'take_profit': take_profit,
            'mem_confidence': mem_signal['confidence'],
            'regime': str(regime),
            'commission_paid': total_cost - position_calc['position_size_usd']
        }

        self.positions.append(position)
        self.capital -= total_cost

        print(f"📈 {mem_signal['action']} @ {bar.name}")
        print(f"   Price: ${execution_price:,.2f}")
        print(f"   Size: ${position_calc['position_size_usd']:,.2f}")
        print(f"   Confidence: {mem_signal['confidence']:.2f}")
        print(f"   Regime: {regime.trend.value}")

    def _manage_position(self, bar: pd.Series, regime: MarketRegime):
        """Manage existing position (check stops/targets)"""
        current_price = bar['close']

        positions_to_close = []

        for i, position in enumerate(self.positions):
            # Check stop loss
            if position['side'] == 'BUY' and current_price <= position['stop_loss']:
                positions_to_close.append((i, 'stop_loss'))
            elif position['side'] == 'SELL' and current_price >= position['stop_loss']:
                positions_to_close.append((i, 'stop_loss'))

            # Check take profit
            elif position['side'] == 'BUY' and current_price >= position['take_profit']:
                positions_to_close.append((i, 'take_profit'))
            elif position['side'] == 'SELL' and current_price <= position['take_profit']:
                positions_to_close.append((i, 'take_profit'))

        # Close positions
        for i, reason in reversed(positions_to_close):
            self._close_position(i, bar, reason)

    def _close_position(self, index: int, bar: pd.Series, reason: str):
        """Close position"""
        position = self.positions[index]
        current_price = bar['close']

        # Apply spread
        volatility = bar['volatility']
        spread = self.spread_model.calculate_spread(
            volatility=volatility,
            volume=bar['volume'],
            avg_volume=bar['volume']
        )

        exit_price = self.spread_model.get_execution_price(
            market_price=current_price,
            side='SELL' if position['side'] == 'BUY' else 'BUY',
            spread_pct=spread
        )

        # Apply slippage
        slippage = 0.0005
        if position['side'] == 'BUY':
            exit_price *= (1 - slippage)  # Selling
        else:
            exit_price *= (1 + slippage)  # Buying back

        # Calculate PnL
        if position['side'] == 'BUY':
            pnl = (exit_price - position['entry_price']) * position['quantity']
        else:
            pnl = (position['entry_price'] - exit_price) * position['quantity']

        # Apply commission
        commission = 0.001
        exit_commission = exit_price * position['quantity'] * commission
        net_pnl = pnl - exit_commission - position['commission_paid']

        pnl_pct = (net_pnl / (position['entry_price'] * position['quantity'])) * 100

        # Update capital
        proceeds = position['entry_price'] * position['quantity'] + net_pnl
        self.capital += proceeds

        # Record trade
        trade = {
            'entry_time': position['entry_time'],
            'exit_time': bar.name,
            'symbol': position['symbol'],
            'side': position['side'],
            'quantity': position['quantity'],
            'entry_price': position['entry_price'],
            'exit_price': exit_price,
            'pnl': net_pnl,
            'pnl_pct': pnl_pct,
            'exit_reason': reason,
            'mem_confidence': position['mem_confidence'],
            'regime': position['regime'],
            'duration_hours': (bar.name - position['entry_time']).total_seconds() / 3600
        }

        self.trades.append(trade)
        del self.positions[index]

        print(f"📉 CLOSE @ {bar.name} ({reason})")
        print(f"   PnL: ${net_pnl:,.2f} ({pnl_pct:+.2f}%)")

    def _calculate_results(self) -> Dict:
        """Calculate backtest results"""
        if not self.trades:
            return {
                'total_trades': 0,
                'win_rate': 0,
                'total_pnl': 0,
                'final_capital': self.capital
            }

        trades_df = pd.DataFrame(self.trades)

        # Basic stats
        total_trades = len(trades_df)
        winning_trades = len(trades_df[trades_df['pnl'] > 0])
        losing_trades = len(trades_df[trades_df['pnl'] <= 0])
        win_rate = (winning_trades / total_trades * 100) if total_trades > 0 else 0

        # PnL stats
        total_pnl = trades_df['pnl'].sum()
        avg_win = trades_df[trades_df['pnl'] > 0]['pnl'].mean() if winning_trades > 0 else 0
        avg_loss = trades_df[trades_df['pnl'] <= 0]['pnl'].mean() if losing_trades > 0 else 0

        # Calculate max drawdown
        equity_df = pd.DataFrame(self.equity_curve)
        peak = equity_df['equity'].expanding().max()
        drawdown = (equity_df['equity'] - peak) / peak * 100
        max_drawdown = drawdown.min()

        # Calculate Sharpe ratio
        returns = equity_df['equity'].pct_change().dropna()
        sharpe_ratio = (returns.mean() / returns.std()) * np.sqrt(24 * 365) if returns.std() > 0 else 0

        final_capital = self.capital + sum(p['quantity'] * trades_df.iloc[-1]['exit_price'] for p in self.positions)
        total_return = ((final_capital - self.initial_capital) / self.initial_capital) * 100

        results = {
            'initial_capital': self.initial_capital,
            'final_capital': final_capital,
            'total_return': total_return,
            'total_pnl': total_pnl,
            'total_trades': total_trades,
            'winning_trades': winning_trades,
            'losing_trades': losing_trades,
            'win_rate': win_rate,
            'avg_win': avg_win,
            'avg_loss': avg_loss,
            'profit_factor': abs(avg_win * winning_trades / (avg_loss * losing_trades)) if losing_trades > 0 and avg_loss != 0 else 0,
            'max_drawdown': max_drawdown,
            'sharpe_ratio': sharpe_ratio,
            'avg_duration_hours': trades_df['duration_hours'].mean()
        }

        return results

    def print_results(self, results: Dict):
        """Print backtest results"""
        print("\n" + "="*70)
        print("📊 BACKTEST RESULTS")
        print("="*70)
        print(f"\n💰 Capital")
        print(f"   Initial:      ${results['initial_capital']:,.2f}")
        print(f"   Final:        ${results['final_capital']:,.2f}")
        print(f"   Total PnL:    ${results['total_pnl']:,.2f}")
        print(f"   Return:       {results['total_return']:+.2f}%")

        print(f"\n📈 Performance")
        print(f"   Sharpe Ratio: {results['sharpe_ratio']:.2f}")
        print(f"   Max Drawdown: {results['max_drawdown']:.2f}%")

        print(f"\n🎯 Trades")
        print(f"   Total:        {results['total_trades']}")
        print(f"   Wins:         {results['winning_trades']}")
        print(f"   Losses:       {results['losing_trades']}")
        print(f"   Win Rate:     {results['win_rate']:.1f}%")
        print(f"   Profit Factor:{results['profit_factor']:.2f}")

        print(f"\n💵 Trade Stats")
        print(f"   Avg Win:      ${results['avg_win']:,.2f}")
        print(f"   Avg Loss:     ${results['avg_loss']:,.2f}")
        print(f"   Avg Duration: {results['avg_duration_hours']:.1f} hours")

        print("\n" + "="*70)


def main():
    """Run MEM integration test"""
    print("🚀 Starting MEM Integration Test...\n")

    # Create tester
    tester = MEMIntegrationTester(capital=10000)

    # Generate market data
    print("📊 Generating market data...")
    market_data = tester.generate_market_data(days=30)
    print(f"   Generated {len(market_data)} bars\n")

    # Run backtest
    results = tester.run_backtest(market_data)

    # Print results
    tester.print_results(results)

    # Regime analysis
    regime_df = pd.DataFrame(tester.regime_history)
    regime_counts = regime_df['regime'].value_counts()
    print("\n📊 Regime Distribution")
    print("="*70)
    for regime, count in regime_counts.items():
        print(f"   {regime}: {count} bars ({count/len(regime_df)*100:.1f}%)")


if __name__ == "__main__":
    main()
