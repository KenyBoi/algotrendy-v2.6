"""
MEM Phase 2 Advanced API Extensions
====================================
REST API endpoints for Phase 2 features:
- Parallel backtesting
- Genetic optimization
- Strategy comparison
- Portfolio backtesting

Author: MEM AI System
Date: 2025-10-21
Version: 2.0
"""

from flask import Flask, request, jsonify
import pandas as pd
import numpy as np
import logging
from datetime import datetime
import traceback
import yfinance as yf

from parallel_backtester import ParallelBacktester
from genetic_optimizer import GeneticOptimizer
from strategy_comparison import StrategyComparison, StrategyConfig
from portfolio_backtester import PortfolioBacktester

# Configure Flask app
app = Flask(__name__)
app.config['JSON_SORT_KEYS'] = False

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def parse_market_data(data_json) -> pd.DataFrame:
    """Parse market data from JSON to DataFrame"""
    try:
        df = pd.DataFrame(data_json['data'])
        df['timestamp'] = pd.to_datetime(df['timestamp'])
        df.set_index('timestamp', inplace=True)

        required_cols = ['open', 'high', 'low', 'close', 'volume']
        for col in required_cols:
            if col not in df.columns:
                raise ValueError(f"Missing required column: {col}")
            df[col] = pd.to_numeric(df[col])

        return df
    except Exception as e:
        raise ValueError(f"Invalid market data format: {e}")


def fetch_symbol_data(symbol: str, period: str, interval: str) -> pd.DataFrame:
    """Fetch data from yfinance"""
    try:
        ticker = yf.Ticker(symbol)
        data = ticker.history(interval=interval, period=period)
        data.columns = [c.lower() for c in data.columns]
        return data
    except Exception as e:
        raise ValueError(f"Error fetching data for {symbol}: {e}")


@app.route('/api/phase2/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'service': 'MEM Phase 2 Advanced API',
        'version': '2.0',
        'features': [
            'parallel_backtest',
            'genetic_optimization',
            'strategy_comparison',
            'portfolio_backtest'
        ],
        'timestamp': datetime.now().isoformat()
    })


@app.route('/api/phase2/parallel-backtest', methods=['POST'])
def parallel_backtest():
    """
    Run parallel backtest across multiple symbols

    Request body:
    {
        "symbols": ["BTC-USD", "ETH-USD", "BNB-USD"],
        "period": "1y",
        "interval": "1d",
        "min_confidence": 60.0,
        "commission": 0.001,
        "initial_capital_per_symbol": 10000.0
    }
    """
    try:
        data = request.get_json()

        if not data or 'symbols' not in data:
            return jsonify({'success': False, 'error': 'No symbols provided'}), 400

        symbols = data['symbols']
        period = data.get('period', '1y')
        interval = data.get('interval', '1d')
        min_confidence = data.get('min_confidence', 60.0)
        commission = data.get('commission', 0.001)
        initial_capital = data.get('initial_capital_per_symbol', 10000.0)

        logger.info(f"Parallel backtest requested for {len(symbols)} symbols")

        # Create parallel backtester
        backtester = ParallelBacktester(
            initial_capital_per_symbol=initial_capital,
            max_workers=None
        )

        # Run parallel backtest
        results = backtester.run_parallel_backtest(
            symbols=symbols,
            interval=interval,
            period=period,
            min_confidence=min_confidence,
            commission=commission
        )

        # Convert numpy types for JSON serialization
        def convert_types(obj):
            if isinstance(obj, (np.integer, np.int64)):
                return int(obj)
            elif isinstance(obj, (np.floating, np.float64)):
                return float(obj)
            elif isinstance(obj, dict):
                return {k: convert_types(v) for k, v in obj.items()}
            elif isinstance(obj, list):
                return [convert_types(i) for i in obj]
            else:
                return obj

        results = convert_types(results)

        return jsonify({
            'success': True,
            'results': results,
            'timestamp': datetime.now().isoformat()
        })

    except Exception as e:
        logger.error(f"Error in parallel backtest: {e}")
        logger.error(traceback.format_exc())
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/phase2/genetic-optimize', methods=['POST'])
def genetic_optimize():
    """
    Run genetic algorithm parameter optimization

    Request body:
    {
        "symbol": "BTC-USD",
        "period": "1y",
        "interval": "1d",
        "parameter_ranges": {
            "min_confidence": [50.0, 90.0]
        },
        "population_size": 20,
        "generations": 10,
        "fitness_function": "composite",
        "commission": 0.001
    }
    """
    try:
        data = request.get_json()

        if not data or 'symbol' not in data:
            return jsonify({'success': False, 'error': 'No symbol provided'}), 400

        symbol = data['symbol']
        period = data.get('period', '1y')
        interval = data.get('interval', '1d')
        parameter_ranges = data.get('parameter_ranges', {'min_confidence': [50.0, 90.0]})
        population_size = data.get('population_size', 20)
        generations = data.get('generations', 10)
        fitness_function = data.get('fitness_function', 'composite')
        commission = data.get('commission', 0.001)

        logger.info(f"Genetic optimization requested for {symbol}")

        # Fetch data
        market_data = fetch_symbol_data(symbol, period, interval)

        # Create optimizer
        optimizer = GeneticOptimizer(
            population_size=population_size,
            generations=generations,
            mutation_rate=0.15,
            crossover_rate=0.7,
            elitism_size=max(3, population_size // 10)
        )

        # Convert parameter ranges to tuples
        param_ranges_tuples = {
            k: tuple(v) for k, v in parameter_ranges.items()
        }
        optimizer.set_parameter_ranges(param_ranges_tuples)

        # Run optimization
        results = optimizer.optimize(
            symbol=symbol,
            data=market_data,
            fitness_function=fitness_function,
            commission=commission
        )

        # Simplify results for JSON
        simplified_results = {
            'success': results['success'],
            'best_parameters': results['best_parameters'],
            'best_fitness': results['best_fitness'],
            'total_time': results['total_time'],
            'generations': results['generations'],
            'history': results['history']
        }

        return jsonify({
            'success': True,
            'results': simplified_results,
            'timestamp': datetime.now().isoformat()
        })

    except Exception as e:
        logger.error(f"Error in genetic optimization: {e}")
        logger.error(traceback.format_exc())
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/phase2/strategy-comparison', methods=['POST'])
def strategy_comparison():
    """
    Compare multiple strategy configurations

    Request body:
    {
        "symbol": "BTC-USD",
        "period": "1y",
        "interval": "1d",
        "strategies": [
            {"name": "Conservative", "min_confidence": 80.0},
            {"name": "Moderate", "min_confidence": 70.0},
            {"name": "Aggressive", "min_confidence": 50.0}
        ],
        "commission": 0.001,
        "initial_capital": 10000.0
    }
    """
    try:
        data = request.get_json()

        if not data or 'symbol' not in data or 'strategies' not in data:
            return jsonify({'success': False, 'error': 'Missing required fields'}), 400

        symbol = data['symbol']
        period = data.get('period', '1y')
        interval = data.get('interval', '1d')
        strategies_config = data['strategies']
        commission = data.get('commission', 0.001)
        initial_capital = data.get('initial_capital', 10000.0)

        logger.info(f"Strategy comparison requested for {symbol} with {len(strategies_config)} strategies")

        # Fetch data
        market_data = fetch_symbol_data(symbol, period, interval)

        # Create comparison framework
        comparison = StrategyComparison(
            initial_capital=initial_capital,
            commission=commission
        )

        # Add strategies
        for strat_config in strategies_config:
            strategy = StrategyConfig(
                name=strat_config['name'],
                min_confidence=strat_config['min_confidence']
            )
            comparison.add_strategy(strategy)

        # Run comparison
        results = comparison.run_comparison(symbol, market_data)

        # Convert for JSON
        def convert_types(obj):
            if isinstance(obj, (np.integer, np.int64)):
                return int(obj)
            elif isinstance(obj, (np.floating, np.float64)):
                return float(obj)
            elif isinstance(obj, dict):
                return {k: convert_types(v) for k, v in obj.items()}
            elif isinstance(obj, list):
                return [convert_types(i) for i in obj]
            else:
                return obj

        results = convert_types(results)

        return jsonify({
            'success': True,
            'results': results,
            'timestamp': datetime.now().isoformat()
        })

    except Exception as e:
        logger.error(f"Error in strategy comparison: {e}")
        logger.error(traceback.format_exc())
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/phase2/portfolio-backtest', methods=['POST'])
def portfolio_backtest():
    """
    Run portfolio-level backtest

    Request body:
    {
        "symbols": ["BTC-USD", "ETH-USD", "BNB-USD"],
        "weights": {"BTC-USD": 0.4, "ETH-USD": 0.3, "BNB-USD": 0.3},
        "period": "1y",
        "interval": "1d",
        "min_confidence": 60.0,
        "commission": 0.001,
        "initial_capital": 100000.0,
        "allocation_strategy": "custom"
    }
    """
    try:
        data = request.get_json()

        if not data or 'symbols' not in data:
            return jsonify({'success': False, 'error': 'No symbols provided'}), 400

        symbols = data['symbols']
        weights = data.get('weights', {})
        period = data.get('period', '1y')
        interval = data.get('interval', '1d')
        min_confidence = data.get('min_confidence', 60.0)
        commission = data.get('commission', 0.001)
        initial_capital = data.get('initial_capital', 100000.0)
        allocation_strategy = data.get('allocation_strategy', 'equal')

        logger.info(f"Portfolio backtest requested for {len(symbols)} symbols")

        # Create portfolio backtester
        portfolio = PortfolioBacktester(
            initial_capital=initial_capital,
            allocation_strategy=allocation_strategy,
            commission=commission
        )

        # Add symbols
        for symbol in symbols:
            weight = weights.get(symbol) if allocation_strategy == 'custom' else None
            portfolio.add_symbol(symbol, weight)

        # Load data
        portfolio.load_data(period=period, interval=interval)

        # Run backtest
        results = portfolio.run_backtest(min_confidence=min_confidence)

        # Convert for JSON
        def convert_types(obj):
            if isinstance(obj, (np.integer, np.int64)):
                return int(obj)
            elif isinstance(obj, (np.floating, np.float64)):
                return float(obj)
            elif isinstance(obj, pd.DataFrame):
                return obj.to_dict()
            elif isinstance(obj, dict):
                return {k: convert_types(v) for k, v in obj.items()}
            elif isinstance(obj, list):
                return [convert_types(i) for i in obj]
            else:
                return obj

        results = convert_types(results)

        return jsonify({
            'success': True,
            'results': results,
            'timestamp': datetime.now().isoformat()
        })

    except Exception as e:
        logger.error(f"Error in portfolio backtest: {e}")
        logger.error(traceback.format_exc())
        return jsonify({'success': False, 'error': str(e)}), 500


if __name__ == '__main__':
    print("\n" + "="*80)
    print("MEM PHASE 2 ADVANCED API")
    print("="*80)
    print("\nPhase 2 Endpoints:")
    print("  GET  /api/phase2/health")
    print("  POST /api/phase2/parallel-backtest")
    print("  POST /api/phase2/genetic-optimize")
    print("  POST /api/phase2/strategy-comparison")
    print("  POST /api/phase2/portfolio-backtest")
    print("\n" + "="*80)
    print("Starting server on http://0.0.0.0:5005")
    print("="*80 + "\n")

    app.run(host='0.0.0.0', port=5005, debug=False)
