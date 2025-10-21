"""
Real-Time Strategy Monitoring System
=====================================
Monitor strategy performance in real-time with live updates and alerts

Features:
- Live performance tracking
- Real-time metrics dashboard
- Alert system for anomalies
- WebSocket streaming
- Performance degradation detection
- Auto-adaptation triggers

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Callable
import time
import json
import logging
from datetime import datetime, timedelta
from collections import deque
import threading

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PerformanceMetrics:
    """Track and calculate performance metrics"""

    def __init__(self, window_size: int = 100):
        """
        Args:
            window_size: Number of trades to keep in rolling window
        """
        self.window_size = window_size
        self.trades = deque(maxlen=window_size)
        self.equity_history = []
        self.start_time = datetime.now()
        self.initial_capital = None

    def add_trade(self, trade: Dict):
        """
        Add trade to metrics

        Args:
            trade: Dict with trade details (pnl, direction, timestamp, etc.)
        """
        self.trades.append(trade)

        if self.initial_capital is None and 'equity' in trade:
            self.initial_capital = trade['equity'] - trade.get('pnl', 0)

        if 'equity' in trade:
            self.equity_history.append({
                'timestamp': trade.get('timestamp', datetime.now()),
                'equity': trade['equity']
            })

    def get_current_metrics(self) -> Dict:
        """
        Calculate current performance metrics

        Returns:
            Dict with current metrics
        """
        if not self.trades:
            return {
                'total_trades': 0,
                'win_rate': 0.0,
                'profit_factor': 0.0,
                'sharpe_ratio': 0.0,
                'max_drawdown': 0.0,
                'current_equity': self.initial_capital or 0.0,
                'total_return': 0.0
            }

        # Basic metrics
        total_trades = len(self.trades)
        winning_trades = sum(1 for t in self.trades if t.get('pnl', 0) > 0)
        losing_trades = sum(1 for t in self.trades if t.get('pnl', 0) < 0)

        win_rate = (winning_trades / total_trades * 100) if total_trades > 0 else 0.0

        # Profit factor
        gross_profit = sum(t.get('pnl', 0) for t in self.trades if t.get('pnl', 0) > 0)
        gross_loss = abs(sum(t.get('pnl', 0) for t in self.trades if t.get('pnl', 0) < 0))
        profit_factor = (gross_profit / gross_loss) if gross_loss > 0 else 0.0

        # Current equity and return
        current_equity = self.equity_history[-1]['equity'] if self.equity_history else (self.initial_capital or 0.0)
        total_return = ((current_equity - self.initial_capital) / self.initial_capital * 100) if self.initial_capital else 0.0

        # Sharpe ratio (annualized)
        returns = [t.get('pnl', 0) / self.initial_capital for t in self.trades] if self.initial_capital else []
        if len(returns) > 1:
            avg_return = np.mean(returns)
            std_return = np.std(returns)
            sharpe_ratio = (avg_return / std_return * np.sqrt(252)) if std_return > 0 else 0.0
        else:
            sharpe_ratio = 0.0

        # Max drawdown
        max_drawdown = self._calculate_max_drawdown()

        return {
            'total_trades': total_trades,
            'winning_trades': winning_trades,
            'losing_trades': losing_trades,
            'win_rate': win_rate,
            'profit_factor': profit_factor,
            'sharpe_ratio': sharpe_ratio,
            'max_drawdown': max_drawdown,
            'current_equity': current_equity,
            'total_return': total_return,
            'gross_profit': gross_profit,
            'gross_loss': gross_loss
        }

    def _calculate_max_drawdown(self) -> float:
        """Calculate maximum drawdown from equity history"""
        if len(self.equity_history) < 2:
            return 0.0

        equity_values = [e['equity'] for e in self.equity_history]
        peak = equity_values[0]
        max_dd = 0.0

        for equity in equity_values:
            if equity > peak:
                peak = equity
            dd = (peak - equity) / peak * 100
            if dd > max_dd:
                max_dd = dd

        return max_dd


class AlertSystem:
    """Alert system for monitoring anomalies and performance issues"""

    def __init__(self):
        self.alerts = []
        self.alert_callbacks = []

    def add_alert_callback(self, callback: Callable):
        """
        Add callback function to be called when alert is triggered

        Args:
            callback: Function to call with alert details
        """
        self.alert_callbacks.append(callback)

    def check_performance_degradation(self, metrics: Dict) -> List[str]:
        """
        Check for performance degradation and create alerts

        Args:
            metrics: Current performance metrics

        Returns:
            List of alert messages
        """
        alerts = []

        # Check win rate
        if metrics['total_trades'] >= 10 and metrics['win_rate'] < 40.0:
            alert = f"⚠️ LOW WIN RATE: {metrics['win_rate']:.1f}% (expected >40%)"
            alerts.append(alert)

        # Check max drawdown
        if metrics['max_drawdown'] > 15.0:
            alert = f"⚠️ HIGH DRAWDOWN: {metrics['max_drawdown']:.1f}% (threshold: 15%)"
            alerts.append(alert)

        # Check profit factor
        if metrics['total_trades'] >= 10 and metrics['profit_factor'] < 1.0:
            alert = f"⚠️ LOW PROFIT FACTOR: {metrics['profit_factor']:.2f} (expected >1.0)"
            alerts.append(alert)

        # Check recent losing streak
        if len(alerts) > 0:
            for callback in self.alert_callbacks:
                for alert_msg in alerts:
                    callback({'message': alert_msg, 'timestamp': datetime.now(), 'metrics': metrics})

        return alerts


class RealtimeMonitor:
    """
    Real-time strategy monitoring system
    """

    def __init__(
        self,
        update_interval: float = 1.0,
        alert_on_degradation: bool = True
    ):
        """
        Initialize real-time monitor

        Args:
            update_interval: Update frequency in seconds
            alert_on_degradation: Enable alerts for performance issues
        """
        self.update_interval = update_interval
        self.alert_on_degradation = alert_on_degradation

        self.metrics = PerformanceMetrics()
        self.alert_system = AlertSystem()
        self.is_monitoring = False
        self.monitor_thread = None

        self.subscribers = []  # WebSocket subscribers

    def add_trade(self, trade: Dict):
        """
        Add trade to monitoring

        Args:
            trade: Trade details
        """
        self.metrics.add_trade(trade)

        # Check for alerts
        if self.alert_on_degradation:
            current_metrics = self.metrics.get_current_metrics()
            alerts = self.alert_system.check_performance_degradation(current_metrics)

            if alerts:
                logger.warning(f"Performance alerts triggered: {alerts}")

    def get_snapshot(self) -> Dict:
        """
        Get current monitoring snapshot

        Returns:
            Dict with current state
        """
        current_metrics = self.metrics.get_current_metrics()

        return {
            'timestamp': datetime.now().isoformat(),
            'metrics': current_metrics,
            'uptime_seconds': (datetime.now() - self.metrics.start_time).total_seconds(),
            'alerts': self.alert_system.alerts[-10:],  # Last 10 alerts
            'is_monitoring': self.is_monitoring
        }

    def start_monitoring(self):
        """Start real-time monitoring in background thread"""
        if self.is_monitoring:
            logger.warning("Monitoring already started")
            return

        self.is_monitoring = True
        self.monitor_thread = threading.Thread(target=self._monitor_loop, daemon=True)
        self.monitor_thread.start()
        logger.info("Real-time monitoring started")

    def stop_monitoring(self):
        """Stop real-time monitoring"""
        self.is_monitoring = False
        if self.monitor_thread:
            self.monitor_thread.join(timeout=5.0)
        logger.info("Real-time monitoring stopped")

    def _monitor_loop(self):
        """Background monitoring loop"""
        while self.is_monitoring:
            try:
                # Get current snapshot
                snapshot = self.get_snapshot()

                # Broadcast to subscribers
                self._broadcast_update(snapshot)

                # Sleep until next update
                time.sleep(self.update_interval)

            except Exception as e:
                logger.error(f"Error in monitor loop: {e}")

    def _broadcast_update(self, snapshot: Dict):
        """
        Broadcast update to all subscribers

        Args:
            snapshot: Current monitoring snapshot
        """
        # In a real implementation, this would send via WebSocket
        # For now, just log
        if len(self.subscribers) > 0:
            logger.debug(f"Broadcasting to {len(self.subscribers)} subscribers")

    def subscribe(self, subscriber_id: str, callback: Callable):
        """
        Subscribe to real-time updates

        Args:
            subscriber_id: Unique subscriber ID
            callback: Function to call with updates
        """
        self.subscribers.append({
            'id': subscriber_id,
            'callback': callback
        })

    def unsubscribe(self, subscriber_id: str):
        """
        Unsubscribe from updates

        Args:
            subscriber_id: Subscriber ID to remove
        """
        self.subscribers = [s for s in self.subscribers if s['id'] != subscriber_id]

    def generate_report(self) -> str:
        """
        Generate formatted monitoring report

        Returns:
            Formatted report string
        """
        snapshot = self.get_snapshot()
        metrics = snapshot['metrics']

        report = f"""
╔══════════════════════════════════════════════════════════════════╗
║           REAL-TIME STRATEGY MONITORING REPORT                   ║
╚══════════════════════════════════════════════════════════════════╝

📅 Timestamp: {snapshot['timestamp']}
⏱️  Uptime: {snapshot['uptime_seconds']:.0f}s ({snapshot['uptime_seconds']/60:.1f} minutes)

📊 PERFORMANCE METRICS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Total Trades:      {metrics['total_trades']}
  Winning Trades:    {metrics['winning_trades']}
  Losing Trades:     {metrics['losing_trades']}

  Win Rate:          {metrics['win_rate']:.1f}%
  Profit Factor:     {metrics['profit_factor']:.2f}
  Sharpe Ratio:      {metrics['sharpe_ratio']:.2f}
  Max Drawdown:      {metrics['max_drawdown']:.2f}%

  Current Equity:    ${metrics['current_equity']:,.2f}
  Total Return:      {metrics['total_return']:.2f}%

  Gross Profit:      ${metrics['gross_profit']:,.2f}
  Gross Loss:        ${metrics['gross_loss']:,.2f}

⚠️  RECENT ALERTS ({len(snapshot['alerts'])})
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"""
        if snapshot['alerts']:
            for alert in snapshot['alerts'][-5:]:
                report += f"  • {alert}\n"
        else:
            report += "  No alerts\n"

        report += "\n" + "="*70 + "\n"

        return report


def demo_realtime_monitoring():
    """
    Demo: Simulate real-time monitoring with sample trades
    """
    print("\n" + "="*80)
    print("REAL-TIME STRATEGY MONITORING DEMO")
    print("="*80 + "\n")

    # Create monitor
    monitor = RealtimeMonitor(update_interval=2.0, alert_on_degradation=True)

    # Add alert callback
    def print_alert(alert):
        print(f"\n🚨 ALERT: {alert['message']}")

    monitor.alert_system.add_alert_callback(print_alert)

    # Start monitoring
    monitor.start_monitoring()

    # Simulate trades
    print("Simulating trades...\n")

    initial_equity = 10000.0
    current_equity = initial_equity

    # Simulate 20 trades (mix of wins and losses)
    for i in range(20):
        # Random trade outcome
        is_win = np.random.random() < 0.45  # 45% win rate
        pnl = np.random.uniform(50, 200) if is_win else -np.random.uniform(50, 150)

        current_equity += pnl

        trade = {
            'timestamp': datetime.now(),
            'direction': 'BUY' if i % 2 == 0 else 'SELL',
            'pnl': pnl,
            'equity': current_equity,
            'symbol': 'BTC-USD'
        }

        monitor.add_trade(trade)

        print(f"Trade {i+1}: {trade['direction']} PnL=${pnl:+.2f}, Equity=${current_equity:,.2f}")

        time.sleep(0.5)  # Simulate time between trades

    # Wait for final monitoring update
    time.sleep(3)

    # Print final report
    print(monitor.generate_report())

    # Stop monitoring
    monitor.stop_monitoring()


if __name__ == "__main__":
    demo_realtime_monitoring()
