#!/usr/bin/env python3
"""
AlgoTrendy Backtesting.py Microservice
Flask-based API for backtesting trading strategies using Backtesting.py library
Provides professional-grade backtesting as the 4th engine in AlgoTrendy's QUAD-ENGINE system
"""

from flask import Flask, request, jsonify
from backtesting import Backtest, Strategy
from backtesting.lib import crossover
from backtesting.test import SMA
import pandas as pd
import numpy as np
import logging
from datetime import datetime, timedelta
import sys
from typing import Dict, List, Any
import json
import os
import psycopg2
from psycopg2.extras import RealDictCursor

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] %(levelname)s - %(name)s: %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler('backtesting-py-service.log')
    ]
)

logger = logging.getLogger(__name__)

app = Flask(__name__)

# In-memory storage for backtest results (replace with database in production)
backtest_results = {}

# QuestDB configuration from environment variables
QUESTDB_HOST = os.getenv('QUESTDB_HOST', 'localhost')
QUESTDB_PORT = int(os.getenv('QUESTDB_PORT', '8812'))
QUESTDB_USER = os.getenv('QUESTDB_USER', 'admin')
QUESTDB_PASSWORD = os.getenv('QUESTDB_PASSWORD', 'quest')
QUESTDB_DATABASE = os.getenv('QUESTDB_DATABASE', 'qdb')


class SMAStrategy(Strategy):
    """
    Simple Moving Average Crossover Strategy
    Long when fast SMA crosses above slow SMA
    Exit when fast SMA crosses below slow SMA
    """
    # Define strategy parameters
    fast_period = 20
    slow_period = 50

    def init(self):
        # Precompute SMAs
        close = self.data.Close
        self.sma_fast = self.I(SMA, close, self.fast_period)
        self.sma_slow = self.I(SMA, close, self.slow_period)

    def next(self):
        # If fast SMA crosses above slow SMA, go long
        if crossover(self.sma_fast, self.sma_slow):
            self.buy()
        # If fast SMA crosses below slow SMA, close position
        elif crossover(self.sma_slow, self.sma_fast):
            self.position.close()


class RSIStrategy(Strategy):
    """
    RSI Mean Reversion Strategy
    Long when RSI crosses below oversold threshold
    Exit when RSI crosses above overbought threshold
    """
    rsi_period = 14
    oversold = 30
    overbought = 70

    def init(self):
        from backtesting.test import RSI
        close = self.data.Close
        self.rsi = self.I(RSI, close, self.rsi_period)

    def next(self):
        if self.rsi[-1] < self.oversold and not self.position:
            self.buy()
        elif self.rsi[-1] > self.overbought and self.position:
            self.position.close()


def get_db_connection():
    """Get connection to QuestDB via PostgreSQL protocol"""
    try:
        conn = psycopg2.connect(
            host=QUESTDB_HOST,
            port=QUESTDB_PORT,
            user=QUESTDB_USER,
            password=QUESTDB_PASSWORD,
            database=QUESTDB_DATABASE
        )
        return conn
    except Exception as e:
        logger.error(f"Database connection failed: {e}")
        raise


def fetch_market_data_from_questdb(symbol: str, start_date: str, end_date: str, timeframe: str = 'day') -> pd.DataFrame:
    """
    Fetch real OHLCV data from AlgoTrendy QuestDB database

    Args:
        symbol: Trading symbol (e.g., BTCUSDT, ETHUSDT)
        start_date: Start date in YYYY-MM-DD format
        end_date: End date in YYYY-MM-DD format
        timeframe: Timeframe (minute, hour, day, week, month)

    Returns:
        DataFrame with OHLCV data indexed by timestamp
    """
    try:
        logger.info(f"Fetching data from QuestDB for {symbol} from {start_date} to {end_date}")

        conn = get_db_connection()

        # Build query to fetch 1-minute OHLCV data from QuestDB
        # Note: market_data_1m contains 1-minute bars, we'll resample for other timeframes
        query = """
        SELECT
            timestamp,
            open,
            high,
            low,
            close,
            volume
        FROM market_data_1m
        WHERE symbol = %s
          AND timestamp >= %s::timestamp
          AND timestamp <= %s::timestamp
        ORDER BY timestamp ASC
        """

        # Execute query
        df = pd.read_sql_query(
            query,
            conn,
            params=(symbol, start_date, end_date),
            parse_dates=['timestamp'],
            index_col='timestamp'
        )

        conn.close()

        if len(df) == 0:
            logger.warning(f"No data found in QuestDB for {symbol} from {start_date} to {end_date}")
            raise ValueError(
                f"No market data found for {symbol} in the specified date range. "
                f"Please ensure market data exists in QuestDB for this symbol."
            )

        # Rename columns to match Backtesting.py expectations (capitalized)
        df.columns = ['Open', 'High', 'Low', 'Close', 'Volume']

        # Resample if needed based on timeframe
        if timeframe != 'day':
            freq_map = {
                'minute': '1T',
                'hour': '1H',
                'day': '1D',
                'week': '1W',
                'month': '1M'
            }
            freq = freq_map.get(timeframe, '1D')

            # Resample OHLCV data
            df = df.resample(freq).agg({
                'Open': 'first',
                'High': 'max',
                'Low': 'min',
                'Close': 'last',
                'Volume': 'sum'
            }).dropna()

        logger.info(f"Fetched {len(df)} bars from QuestDB for {symbol}")

        if len(df) < 100:
            logger.warning(f"Only {len(df)} bars available - backtesting may not be meaningful")

        return df

    except psycopg2.Error as e:
        logger.error(f"Database error while fetching data: {e}")
        raise ValueError(f"Database error: {str(e)}")
    except Exception as e:
        logger.error(f"Error fetching market data: {e}", exc_info=True)
        raise


def get_strategy_class(strategy_name: str):
    """Get strategy class by name"""
    strategies = {
        'sma': SMAStrategy,
        'rsi': RSIStrategy,
    }
    return strategies.get(strategy_name.lower(), SMAStrategy)


def run_backtest(config: Dict[str, Any]) -> Dict[str, Any]:
    """
    Run a backtest with the given configuration

    Args:
        config: Backtest configuration containing:
            - symbol: Trading symbol
            - start_date: Start date (YYYY-MM-DD)
            - end_date: End date (YYYY-MM-DD)
            - timeframe: Timeframe (minute, hour, day, week, month)
            - strategy: Strategy name (sma, rsi)
            - initial_capital: Starting capital
            - commission: Commission rate (e.g., 0.001 for 0.1%)
            - strategy_params: Strategy parameters dict

    Returns:
        Dict containing backtest results and metrics
    """
    try:
        # Extract config
        symbol = config.get('symbol', 'BTCUSDT')
        start_date = config.get('start_date', '2024-01-01')
        end_date = config.get('end_date', '2024-10-01')
        timeframe = config.get('timeframe', 'day')
        strategy_name = config.get('strategy', 'sma')
        initial_capital = config.get('initial_capital', 10000)
        commission = config.get('commission', 0.001)
        strategy_params = config.get('strategy_params', {})

        logger.info(f"Running backtest: {symbol} | {start_date} to {end_date} | Strategy: {strategy_name}")

        # Fetch REAL data from QuestDB
        data = fetch_market_data_from_questdb(symbol, start_date, end_date, timeframe)

        if len(data) < 100:
            raise ValueError(f"Insufficient data: only {len(data)} bars available")

        # Get strategy class
        StrategyClass = get_strategy_class(strategy_name)

        # Create backtest
        bt = Backtest(
            data,
            StrategyClass,
            cash=initial_capital,
            commission=commission,
            exclusive_orders=True
        )

        # Run backtest with strategy parameters
        logger.info(f"Starting backtest with {len(data)} bars...")
        stats = bt.run(**strategy_params)

        # Extract metrics
        results = {
            'backtest_id': str(np.random.randint(100000, 999999)),
            'status': 'completed',
            'symbol': symbol,
            'strategy': strategy_name,
            'start_date': start_date,
            'end_date': end_date,
            'timeframe': timeframe,
            'initial_capital': initial_capital,
            'final_equity': float(stats['Equity Final [$]']),
            'metrics': {
                'total_return': float(stats['Return [%]']),
                'annual_return': float(stats['Return (Ann.) [%]']) if 'Return (Ann.) [%]' in stats else 0,
                'sharpe_ratio': float(stats['Sharpe Ratio']) if pd.notna(stats['Sharpe Ratio']) else 0,
                'sortino_ratio': float(stats['Sortino Ratio']) if pd.notna(stats['Sortino Ratio']) else 0,
                'max_drawdown': float(stats['Max. Drawdown [%]']),
                'win_rate': float(stats['Win Rate [%]']),
                'total_trades': int(stats['# Trades']),
                'avg_trade': float(stats['Avg. Trade [%]']) if 'Avg. Trade [%]' in stats else 0,
                'best_trade': float(stats['Best Trade [%]']) if 'Best Trade [%]' in stats else 0,
                'worst_trade': float(stats['Worst Trade [%]']) if 'Worst Trade [%]' in stats else 0,
                'avg_trade_duration': str(stats['Avg. Trade Duration']) if 'Avg. Trade Duration' in stats else '0',
                'profit_factor': float(stats['Profit Factor']) if pd.notna(stats['Profit Factor']) else 0,
                'expectancy': float(stats['Expectancy [%]']) if 'Expectancy [%]' in stats else 0,
                'sqn': float(stats['SQN']) if 'SQN' in stats and pd.notna(stats['SQN']) else 0,
            },
            'equity_curve': [],  # Simplified - full equity curve would be large
            'trades': [],  # Simplified - full trade list would be large
            'timestamp': datetime.utcnow().isoformat(),
            'data_points': len(data),
            'engine': 'Backtesting.py v0.3.3'
        }

        # Add summary of equity curve (first, middle, last 5 points)
        try:
            equity_curve = stats._equity_curve
            if equity_curve is not None and len(equity_curve) > 0:
                curve_data = equity_curve['Equity'].values
                results['equity_curve'] = {
                    'first': float(curve_data[0]),
                    'last': float(curve_data[-1]),
                    'max': float(curve_data.max()),
                    'min': float(curve_data.min()),
                    'points': len(curve_data)
                }
        except Exception as e:
            logger.warning(f"Could not extract equity curve: {e}")

        # Add summary of trades
        try:
            trades_df = stats._trades
            if trades_df is not None and len(trades_df) > 0:
                results['trade_summary'] = {
                    'total': len(trades_df),
                    'wins': len(trades_df[trades_df['PnL'] > 0]),
                    'losses': len(trades_df[trades_df['PnL'] < 0]),
                    'avg_pnl': float(trades_df['PnL'].mean()),
                    'total_pnl': float(trades_df['PnL'].sum())
                }
        except Exception as e:
            logger.warning(f"Could not extract trades: {e}")

        logger.info(f"Backtest completed: Return={results['metrics']['total_return']:.2f}%, "
                   f"Trades={results['metrics']['total_trades']}, "
                   f"Win Rate={results['metrics']['win_rate']:.2f}%")

        return results

    except Exception as e:
        logger.error(f"Backtest failed: {e}", exc_info=True)
        raise


@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'service': 'AlgoTrendy Backtesting.py Service',
        'version': '1.0.0',
        'engine': 'Backtesting.py v0.3.3',
        'timestamp': datetime.utcnow().isoformat()
    }), 200


@app.route('/strategies', methods=['GET'])
def list_strategies():
    """List available strategies"""
    return jsonify({
        'strategies': [
            {
                'name': 'sma',
                'description': 'Simple Moving Average Crossover',
                'parameters': {
                    'fast_period': {'type': 'int', 'default': 20, 'range': [5, 50]},
                    'slow_period': {'type': 'int', 'default': 50, 'range': [20, 200]}
                }
            },
            {
                'name': 'rsi',
                'description': 'RSI Mean Reversion',
                'parameters': {
                    'rsi_period': {'type': 'int', 'default': 14, 'range': [5, 30]},
                    'oversold': {'type': 'int', 'default': 30, 'range': [10, 40]},
                    'overbought': {'type': 'int', 'default': 70, 'range': [60, 90]}
                }
            }
        ]
    }), 200


@app.route('/backtest/run', methods=['POST'])
def run_backtest_endpoint():
    """
    Run a backtest with the given configuration

    Request body:
    {
        "symbol": "BTCUSDT",
        "start_date": "2024-01-01",
        "end_date": "2024-10-01",
        "timeframe": "day",
        "strategy": "sma",
        "initial_capital": 10000,
        "commission": 0.001,
        "strategy_params": {"fast_period": 20, "slow_period": 50}
    }
    """
    try:
        config = request.get_json()

        if not config:
            return jsonify({
                'error': 'Missing request body',
                'timestamp': datetime.utcnow().isoformat()
            }), 400

        # Validate required fields
        required_fields = ['symbol', 'start_date', 'end_date']
        for field in required_fields:
            if field not in config:
                return jsonify({
                    'error': f'Missing required field: {field}',
                    'timestamp': datetime.utcnow().isoformat()
                }), 400

        # Run backtest
        results = run_backtest(config)

        # Store results
        backtest_id = results['backtest_id']
        backtest_results[backtest_id] = results

        return jsonify(results), 200

    except ValueError as e:
        logger.error(f"Validation error: {e}")
        return jsonify({
            'error': str(e),
            'timestamp': datetime.utcnow().isoformat()
        }), 400
    except Exception as e:
        logger.error(f"Backtest error: {e}", exc_info=True)
        return jsonify({
            'error': 'Internal server error',
            'details': str(e),
            'timestamp': datetime.utcnow().isoformat()
        }), 500


@app.route('/backtest/results/<backtest_id>', methods=['GET'])
def get_results(backtest_id):
    """Get backtest results by ID"""
    if backtest_id not in backtest_results:
        return jsonify({
            'error': f'Backtest {backtest_id} not found',
            'timestamp': datetime.utcnow().isoformat()
        }), 404

    return jsonify(backtest_results[backtest_id]), 200


@app.route('/backtest/history', methods=['GET'])
def get_history():
    """Get list of all backtest runs"""
    history = []
    for backtest_id, results in backtest_results.items():
        history.append({
            'backtest_id': backtest_id,
            'symbol': results.get('symbol'),
            'strategy': results.get('strategy'),
            'start_date': results.get('start_date'),
            'end_date': results.get('end_date'),
            'total_return': results.get('metrics', {}).get('total_return'),
            'timestamp': results.get('timestamp'),
            'status': results.get('status')
        })

    return jsonify({
        'count': len(history),
        'backtests': history
    }), 200


@app.route('/backtest/<backtest_id>', methods=['DELETE'])
def delete_backtest(backtest_id):
    """Delete a backtest by ID"""
    if backtest_id in backtest_results:
        del backtest_results[backtest_id]
        return jsonify({
            'message': f'Backtest {backtest_id} deleted',
            'timestamp': datetime.utcnow().isoformat()
        }), 200
    else:
        return jsonify({
            'error': f'Backtest {backtest_id} not found',
            'timestamp': datetime.utcnow().isoformat()
        }), 404


if __name__ == '__main__':
    logger.info("=" * 60)
    logger.info("Starting AlgoTrendy Backtesting.py Service")
    logger.info("=" * 60)
    logger.info("Engine: Backtesting.py v0.3.3")
    logger.info("Strategies: SMA Crossover, RSI Mean Reversion")
    logger.info("Listening on: http://0.0.0.0:5004")
    logger.info("=" * 60)

    app.run(
        host='0.0.0.0',
        port=5004,
        debug=False
    )
