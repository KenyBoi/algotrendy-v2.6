"""
Backtesting Engine Wrappers

Provides abstraction layer for different backtesting engines:
- QuantConnect
- Backtester.com
- Custom Engine (built-in)
"""

from abc import ABC, abstractmethod
from typing import Dict, Any, List
import uuid
from datetime import datetime, timedelta
import pandas as pd
import numpy as np
import logging

from .models import (
    BacktestConfig,
    BacktestResults,
    BacktestStatus,
    BacktestMetrics,
    TradeResult,
    EquityPoint,
)
from .indicators import calculate_indicators

logger = logging.getLogger(__name__)


class BacktestEngine(ABC):
    """Abstract base class for backtesting engines"""

    def __init__(self, config: BacktestConfig):
        self.config = config
        self.backtest_id = str(uuid.uuid4())

    @abstractmethod
    async def run(self) -> BacktestResults:
        """Execute the backtest"""
        pass

    @abstractmethod
    def validate_config(self) -> bool:
        """Validate configuration for this engine"""
        pass


class CustomEngine(BacktestEngine):
    """
    Custom built-in backtesting engine

    Uses simple moving average crossover strategy for demonstration.
    Can be enhanced with more sophisticated strategies.
    """

    def validate_config(self) -> bool:
        """Validate configuration"""
        # Basic validation
        if not self.config.symbol:
            return False
        if self.config.initial_capital <= 0:
            return False
        return True

    async def run(self) -> BacktestResults:
        """Run backtest using custom engine"""
        logger.info(f"Starting custom backtest for {self.config.symbol}")

        started_at = datetime.utcnow()

        try:
            # 1. Fetch historical data
            df = await self._fetch_historical_data()

            # 2. Calculate indicators
            df = calculate_indicators(df, self.config.indicators, self.config.indicator_params)

            # 3. Run strategy
            trades, equity_curve = await self._run_strategy(df)

            # 4. Calculate metrics
            metrics = self._calculate_metrics(trades, equity_curve, self.config.initial_capital)

            # 5. Build results
            completed_at = datetime.utcnow()
            execution_time = (completed_at - started_at).total_seconds()

            indicators_used = [name for name, enabled in self.config.indicators.items() if enabled]

            results = BacktestResults(
                backtest_id=self.backtest_id,
                status=BacktestStatus.COMPLETED,
                config=self.config,
                started_at=started_at,
                completed_at=completed_at,
                execution_time_seconds=execution_time,
                metrics=metrics,
                equity_curve=equity_curve,
                trades=trades,
                indicators_used=indicators_used,
                metadata={
                    "engine": "custom",
                    "data_points": len(df),
                    "strategy": "SMA Crossover"
                }
            )

            logger.info(f"Backtest completed: {metrics.total_trades} trades, {metrics.total_return:.2f}% return")
            return results

        except Exception as e:
            logger.error(f"Backtest failed: {e}", exc_info=True)
            return BacktestResults(
                backtest_id=self.backtest_id,
                status=BacktestStatus.FAILED,
                config=self.config,
                started_at=started_at,
                completed_at=datetime.utcnow(),
                error_message=str(e),
                error_details={"error_type": type(e).__name__}
            )

    async def _fetch_historical_data(self) -> pd.DataFrame:
        """Fetch historical price data"""
        # For now, generate mock data
        # TODO: Replace with actual data fetching from database or API
        start_date = datetime.strptime(self.config.start_date, '%Y-%m-%d')
        end_date = datetime.strptime(self.config.end_date, '%Y-%m-%d')

        # Generate date range
        days = (end_date - start_date).days
        dates = pd.date_range(start=start_date, end=end_date, periods=days)

        # Generate mock OHLCV data
        np.random.seed(42)  # For reproducibility

        # Starting price based on asset
        if self.config.symbol.startswith('BTC'):
            base_price = 40000
        elif self.config.symbol.startswith('ETH'):
            base_price = 2500
        else:
            base_price = 100

        # Generate price walk
        returns = np.random.normal(0.0005, 0.02, len(dates))
        price = base_price * (1 + returns).cumprod()

        # Create OHLCV
        df = pd.DataFrame({
            'timestamp': dates,
            'open': price * (1 + np.random.uniform(-0.01, 0.01, len(dates))),
            'high': price * (1 + np.random.uniform(0, 0.02, len(dates))),
            'low': price * (1 + np.random.uniform(-0.02, 0, len(dates))),
            'close': price,
            'volume': np.random.uniform(1000, 10000, len(dates))
        })

        logger.info(f"Generated {len(df)} data points from {start_date} to {end_date}")
        return df

    async def _run_strategy(self, df: pd.DataFrame) -> tuple[List[TradeResult], List[EquityPoint]]:
        """
        Run trading strategy on data

        Uses SMA crossover as example strategy:
        - Long when fast SMA crosses above slow SMA
        - Exit when fast SMA crosses below slow SMA
        """
        trades = []
        equity_curve = []

        cash = self.config.initial_capital
        position = 0
        position_price = 0
        position_entry_time = None

        # Calculate SMAs for strategy (if not already done)
        if 'sma_20' not in df.columns:
            df['sma_20'] = df['close'].rolling(window=20).mean()
        if 'sma_50' not in df.columns:
            df['sma_50'] = df['close'].rolling(window=50).mean()

        peak_equity = self.config.initial_capital

        for idx, row in df.iterrows():
            timestamp = row['timestamp']
            close_price = row['close']
            sma_fast = row['sma_20']
            sma_slow = row['sma_50']

            # Skip if indicators not ready
            if pd.isna(sma_fast) or pd.isna(sma_slow):
                equity_curve.append(EquityPoint(
                    timestamp=timestamp,
                    equity=cash,
                    cash=cash,
                    positions_value=0,
                    drawdown=0
                ))
                continue

            # Calculate current equity
            positions_value = position * close_price if position > 0 else 0
            current_equity = cash + positions_value

            # Update peak for drawdown calculation
            if current_equity > peak_equity:
                peak_equity = current_equity

            drawdown = ((current_equity - peak_equity) / peak_equity) * 100 if peak_equity > 0 else 0

            # Entry signal: SMA crossover (fast > slow) and no position
            if sma_fast > sma_slow and position == 0:
                # Calculate position size (use 95% of cash to leave room for fees)
                position_size = (cash * 0.95) / close_price
                cost = position_size * close_price
                commission = cost * self.config.commission

                if cash >= (cost + commission):
                    position = position_size
                    position_price = close_price
                    position_entry_time = timestamp
                    cash -= (cost + commission)

                    logger.debug(f"LONG entry at {close_price:.2f}, size: {position:.4f}")

            # Exit signal: SMA crossover (fast < slow) and have position
            elif sma_fast < sma_slow and position > 0:
                # Close position
                proceeds = position * close_price
                commission = proceeds * self.config.commission
                cash += (proceeds - commission)

                # Calculate PnL
                pnl = proceeds - (position * position_price)
                pnl_percent = (pnl / (position * position_price)) * 100

                # Record trade
                duration = (timestamp - position_entry_time).total_seconds() / 60  # minutes
                trade = TradeResult(
                    entry_time=position_entry_time,
                    exit_time=timestamp,
                    entry_price=position_price,
                    exit_price=close_price,
                    quantity=position,
                    side="long",
                    pnl=pnl - (2 * commission),  # Subtract entry and exit commissions
                    pnl_percent=pnl_percent,
                    duration_minutes=int(duration),
                    exit_reason="sma_crossover"
                )
                trades.append(trade)

                logger.debug(f"LONG exit at {close_price:.2f}, PnL: ${pnl:.2f} ({pnl_percent:.2f}%)")

                # Reset position
                position = 0
                position_price = 0
                position_entry_time = None

            # Record equity point
            positions_value = position * close_price if position > 0 else 0
            current_equity = cash + positions_value

            equity_curve.append(EquityPoint(
                timestamp=timestamp,
                equity=current_equity,
                cash=cash,
                positions_value=positions_value,
                drawdown=drawdown
            ))

        # Close any open positions at end
        if position > 0:
            final_row = df.iloc[-1]
            close_price = final_row['close']
            timestamp = final_row['timestamp']

            proceeds = position * close_price
            commission = proceeds * self.config.commission
            cash += (proceeds - commission)

            pnl = proceeds - (position * position_price)
            pnl_percent = (pnl / (position * position_price)) * 100
            duration = (timestamp - position_entry_time).total_seconds() / 60

            trade = TradeResult(
                entry_time=position_entry_time,
                exit_time=timestamp,
                entry_price=position_price,
                exit_price=close_price,
                quantity=position,
                side="long",
                pnl=pnl - (2 * commission),
                pnl_percent=pnl_percent,
                duration_minutes=int(duration),
                exit_reason="end_of_backtest"
            )
            trades.append(trade)

            # Update final equity
            equity_curve[-1].cash = cash
            equity_curve[-1].positions_value = 0
            equity_curve[-1].equity = cash

        return trades, equity_curve

    def _calculate_metrics(self, trades: List[TradeResult], equity_curve: List[EquityPoint], initial_capital: float) -> BacktestMetrics:
        """Calculate performance metrics"""

        if not trades:
            return BacktestMetrics(
                total_return=0,
                annual_return=0,
                sharpe_ratio=0,
                sortino_ratio=0,
                max_drawdown=0,
                win_rate=0,
                profit_factor=0,
                total_trades=0,
                winning_trades=0,
                losing_trades=0,
                avg_win=0,
                avg_loss=0,
                largest_win=0,
                largest_loss=0,
                avg_trade_duration=0
            )

        # Basic trade statistics
        winning_trades = [t for t in trades if t.pnl and t.pnl > 0]
        losing_trades = [t for t in trades if t.pnl and t.pnl <= 0]

        total_trades = len(trades)
        num_winning = len(winning_trades)
        num_losing = len(losing_trades)

        win_rate = (num_winning / total_trades * 100) if total_trades > 0 else 0

        # PnL statistics
        total_pnl = sum(t.pnl for t in trades if t.pnl)
        total_return = (total_pnl / initial_capital) * 100

        wins = [t.pnl for t in winning_trades if t.pnl]
        losses = [abs(t.pnl) for t in losing_trades if t.pnl]

        avg_win = sum(wins) / len(wins) if wins else 0
        avg_loss = sum(losses) / len(losses) if losses else 0
        largest_win = max(wins) if wins else 0
        largest_loss = max(losses) if losses else 0

        profit_factor = sum(wins) / sum(losses) if losses and sum(losses) > 0 else 0

        # Time-based metrics
        if equity_curve:
            days = (equity_curve[-1].timestamp - equity_curve[0].timestamp).days
            annual_return = (total_return / days * 365) if days > 0 else 0
        else:
            annual_return = 0

        # Sharpe ratio (simplified)
        if equity_curve and len(equity_curve) > 1:
            returns = []
            for i in range(1, len(equity_curve)):
                ret = (equity_curve[i].equity / equity_curve[i-1].equity) - 1
                returns.append(ret)

            returns_array = np.array(returns)
            if len(returns_array) > 0 and returns_array.std() > 0:
                sharpe_ratio = (returns_array.mean() / returns_array.std()) * np.sqrt(252)  # Annualized
            else:
                sharpe_ratio = 0
        else:
            sharpe_ratio = 0

        # Sortino ratio (simplified - using downside deviation)
        if equity_curve and len(equity_curve) > 1:
            negative_returns = [r for r in returns if r < 0]
            if negative_returns:
                downside_std = np.std(negative_returns)
                sortino_ratio = (returns_array.mean() / downside_std) * np.sqrt(252) if downside_std > 0 else 0
            else:
                sortino_ratio = sharpe_ratio
        else:
            sortino_ratio = 0

        # Max drawdown
        max_drawdown = min((point.drawdown for point in equity_curve), default=0)

        # Average trade duration
        durations = [t.duration_minutes for t in trades if t.duration_minutes]
        avg_duration_hours = (sum(durations) / len(durations) / 60) if durations else 0

        return BacktestMetrics(
            total_return=round(total_return, 2),
            annual_return=round(annual_return, 2),
            sharpe_ratio=round(sharpe_ratio, 2),
            sortino_ratio=round(sortino_ratio, 2),
            max_drawdown=round(max_drawdown, 2),
            win_rate=round(win_rate, 2),
            profit_factor=round(profit_factor, 2),
            total_trades=total_trades,
            winning_trades=num_winning,
            losing_trades=num_losing,
            avg_win=round(avg_win, 2),
            avg_loss=round(avg_loss, 2),
            largest_win=round(largest_win, 2),
            largest_loss=round(largest_loss, 2),
            avg_trade_duration=round(avg_duration_hours, 2)
        )


class QuantConnectEngine(BacktestEngine):
    """QuantConnect backtesting engine wrapper"""

    def validate_config(self) -> bool:
        """Validate configuration for QuantConnect"""
        # TODO: Add QuantConnect-specific validation
        return True

    async def run(self) -> BacktestResults:
        """Run backtest on QuantConnect"""
        # TODO: Implement QuantConnect integration
        # For now, return a placeholder
        logger.warning("QuantConnect engine not yet implemented, using mock results")

        return BacktestResults(
            backtest_id=self.backtest_id,
            status=BacktestStatus.FAILED,
            config=self.config,
            error_message="QuantConnect integration coming soon",
            metadata={"engine": "quantconnect"}
        )


class BacktesterComEngine(BacktestEngine):
    """Backtester.com API wrapper"""

    def validate_config(self) -> bool:
        """Validate configuration for Backtester.com"""
        # TODO: Add Backtester.com-specific validation
        return True

    async def run(self) -> BacktestResults:
        """Run backtest on Backtester.com"""
        # TODO: Implement Backtester.com integration
        # For now, return a placeholder
        logger.warning("Backtester.com engine not yet implemented, using mock results")

        return BacktestResults(
            backtest_id=self.backtest_id,
            status=BacktestStatus.FAILED,
            config=self.config,
            error_message="Backtester.com integration coming soon",
            metadata={"engine": "backtester"}
        )


def get_engine(config: BacktestConfig) -> BacktestEngine:
    """Factory function to get the appropriate engine"""
    if config.backtester.value == "quantconnect":
        return QuantConnectEngine(config)
    elif config.backtester.value == "backtester":
        return BacktesterComEngine(config)
    else:  # custom
        return CustomEngine(config)
