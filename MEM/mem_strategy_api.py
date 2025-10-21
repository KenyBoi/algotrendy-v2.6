"""
MEM Strategy REST API
====================
Flask API to expose MEM Advanced Trading Strategy to .NET backend

Endpoints:
- POST /api/strategy/analyze - Analyze market data and generate signals
- POST /api/strategy/backtest - Run quick backtest
- GET /api/strategy/health - Health check
- GET /api/strategy/indicators - List available indicators

Usage:
    python3 mem_strategy_api.py

Author: MEM AI System
Date: 2025-10-21
Version: 1.0
"""

from flask import Flask, request, jsonify
import pandas as pd
import numpy as np
import logging
from datetime import datetime
import traceback
import hashlib

from advanced_trading_strategy import AdvancedTradingStrategy
from advanced_indicators import get_indicators, list_all_indicators
from mem_indicator_integration import (
    analyze_market,
    get_trading_signals,
    get_risk_metrics,
    get_support_resistance
)

# Try to import flask-caching (optional)
try:
    from flask_caching import Cache
    CACHING_AVAILABLE = True
except ImportError:
    CACHING_AVAILABLE = False
    print("Warning: flask-caching not installed. API will run without caching.")

# Configure Flask app
app = Flask(__name__)
app.config['JSON_SORT_KEYS'] = False

# Configure caching (if available)
if CACHING_AVAILABLE:
    # Try Redis first, fall back to simple cache
    try:
        app.config['CACHE_TYPE'] = 'redis'
        app.config['CACHE_REDIS_URL'] = 'redis://localhost:6379/0'
        app.config['CACHE_DEFAULT_TIMEOUT'] = 300  # 5 minutes
        cache = Cache(app)
        print("✓ Redis caching enabled")
    except:
        app.config['CACHE_TYPE'] = 'simple'  # In-memory fallback
        cache = Cache(app)
        print("✓ Simple caching enabled (Redis not available)")
else:
    cache = None

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Initialize strategy (reusable instance)
default_strategy = AdvancedTradingStrategy(
    min_confidence=70.0,
    max_risk_per_trade=0.02,
    use_multi_timeframe=True
)


def parse_market_data(data_json) -> pd.DataFrame:
    """
    Parse market data from JSON to DataFrame

    Expected format:
    {
        "data": [
            {"timestamp": "2025-01-01T00:00:00", "open": 100, "high": 101, "low": 99, "close": 100.5, "volume": 1000},
            ...
        ]
    }

    Returns:
        DataFrame with datetime index and OHLCV columns
    """
    try:
        df = pd.DataFrame(data_json['data'])

        # Parse timestamp
        df['timestamp'] = pd.to_datetime(df['timestamp'])
        df.set_index('timestamp', inplace=True)

        # Ensure required columns
        required_cols = ['open', 'high', 'low', 'close', 'volume']
        for col in required_cols:
            if col not in df.columns:
                raise ValueError(f"Missing required column: {col}")

        # Convert to numeric
        for col in required_cols:
            df[col] = pd.to_numeric(df[col])

        logger.info(f"Parsed {len(df)} candles from {df.index[0]} to {df.index[-1]}")
        return df

    except Exception as e:
        logger.error(f"Error parsing market data: {e}")
        raise ValueError(f"Invalid market data format: {e}")


@app.route('/api/strategy/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'service': 'MEM Advanced Strategy API',
        'version': '1.0',
        'timestamp': datetime.now().isoformat()
    })


@app.route('/api/strategy/indicators', methods=['GET'])
def list_indicators():
    """List all available indicators"""
    try:
        indicators = list_all_indicators()

        total_count = sum(len(inds) for inds in indicators.values())

        return jsonify({
            'success': True,
            'total_indicators': total_count,
            'categories': indicators
        })

    except Exception as e:
        logger.error(f"Error listing indicators: {e}")
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500


def _generate_cache_key():
    """Generate cache key from request data"""
    try:
        data = request.get_json()
        # Create hash from request data for cache key
        data_str = str(data)
        cache_key = f"analyze:{hashlib.md5(data_str.encode()).hexdigest()}"
        return cache_key
    except:
        return None


@app.route('/api/strategy/analyze', methods=['POST'])
def analyze():
    """
    Analyze market data and generate trading signal

    Request body:
    {
        "symbol": "BTCUSDT",
        "data_1h": { "data": [...] },
        "data_4h": { "data": [...] },  // Optional
        "data_1d": { "data": [...] },  // Optional
        "account_balance": 10000.0,    // Optional
        "config": {                     // Optional
            "min_confidence": 70.0,
            "max_risk_per_trade": 0.02
        }
    }

    Returns:
    {
        "success": true,
        "symbol": "BTCUSDT",
        "signal": {...}
    }
    """
    try:
        # Check cache first (if available)
        if cache is not None:
            cache_key = _generate_cache_key()
            if cache_key:
                cached_response = cache.get(cache_key)
                if cached_response:
                    logger.info(f"Cache HIT for analyze request (key: {cache_key[:16]}...)")
                    cached_response['cached'] = True
                    return jsonify(cached_response)

        data = request.get_json()

        if not data:
            return jsonify({'success': False, 'error': 'No data provided'}), 400

        symbol = data.get('symbol', 'UNKNOWN')
        account_balance = data.get('account_balance', 10000.0)

        # Parse market data
        data_1h = parse_market_data(data['data_1h'])

        data_4h = None
        if 'data_4h' in data:
            data_4h = parse_market_data(data['data_4h'])

        data_1d = None
        if 'data_1d' in data:
            data_1d = parse_market_data(data['data_1d'])

        # Get strategy config or use defaults
        config = data.get('config', {})
        min_conf = config.get('min_confidence', 70.0)
        max_risk = config.get('max_risk_per_trade', 0.02)
        use_mtf = config.get('use_multi_timeframe', data_4h is not None and data_1d is not None)

        # Initialize strategy with custom config
        strategy = AdvancedTradingStrategy(
            min_confidence=min_conf,
            max_risk_per_trade=max_risk,
            use_multi_timeframe=use_mtf
        )

        # Generate signal
        signal = strategy.generate_trade_signal(
            data_1h=data_1h,
            data_4h=data_4h,
            data_1d=data_1d,
            account_balance=account_balance
        )

        # Convert numpy types to native Python types for JSON serialization
        def convert_types(obj):
            if isinstance(obj, np.integer):
                return int(obj)
            elif isinstance(obj, np.floating):
                return float(obj)
            elif isinstance(obj, np.ndarray):
                return obj.tolist()
            elif isinstance(obj, dict):
                return {k: convert_types(v) for k, v in obj.items()}
            elif isinstance(obj, list):
                return [convert_types(i) for i in obj]
            else:
                return obj

        signal = convert_types(signal)

        logger.info(f"Generated {signal['action']} signal for {symbol} "
                   f"(confidence: {signal.get('confidence', 0):.1f}%)")

        response = {
            'success': True,
            'symbol': symbol,
            'signal': signal,
            'timestamp': datetime.now().isoformat(),
            'cached': False
        }

        # Cache the response (if available)
        if cache is not None and cache_key:
            cache.set(cache_key, response, timeout=60)  # Cache for 1 minute
            logger.info(f"Cached response for {symbol}")

        return jsonify(response)

    except ValueError as e:
        logger.warning(f"Validation error: {e}")
        return jsonify({'success': False, 'error': str(e)}), 400

    except Exception as e:
        logger.error(f"Error in analyze endpoint: {e}")
        logger.error(traceback.format_exc())
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/strategy/indicators/calculate', methods=['POST'])
def calculate_indicators():
    """
    Calculate specific indicators

    Request body:
    {
        "data": { "data": [...] },
        "indicators": ["rsi", "macd", "bollinger_bands"]
    }

    Returns:
    {
        "success": true,
        "results": {
            "rsi": [30.5, 32.1, ...],
            "macd": {...},
            ...
        }
    }
    """
    try:
        data = request.get_json()

        if not data or 'data' not in data:
            return jsonify({'success': False, 'error': 'No data provided'}), 400

        # Parse market data
        df = parse_market_data(data['data'])

        requested_indicators = data.get('indicators', [])
        if not requested_indicators:
            return jsonify({'success': False, 'error': 'No indicators specified'}), 400

        # Get indicators instance
        indicators = get_indicators()

        results = {}

        for ind_name in requested_indicators:
            try:
                # Map indicator names to methods
                if ind_name == 'rsi':
                    result = indicators.rsi(df['close'])
                    results['rsi'] = result.tail(10).tolist()

                elif ind_name == 'macd':
                    macd, signal, hist = indicators.macd(df['close'])
                    results['macd'] = {
                        'macd': macd.tail(10).tolist(),
                        'signal': signal.tail(10).tolist(),
                        'histogram': hist.tail(10).tolist()
                    }

                elif ind_name == 'bollinger_bands':
                    upper, mid, lower = indicators.bollinger_bands(df['close'])
                    results['bollinger_bands'] = {
                        'upper': upper.tail(10).tolist(),
                        'middle': mid.tail(10).tolist(),
                        'lower': lower.tail(10).tolist()
                    }

                elif ind_name == 'atr':
                    result = indicators.atr(df['high'], df['low'], df['close'])
                    results['atr'] = result.tail(10).tolist()

                elif ind_name == 'adx':
                    adx, plus_di, minus_di = indicators.adx(df['high'], df['low'], df['close'])
                    results['adx'] = {
                        'adx': adx.tail(10).tolist(),
                        'plus_di': plus_di.tail(10).tolist(),
                        'minus_di': minus_di.tail(10).tolist()
                    }

                else:
                    results[ind_name] = f"Indicator '{ind_name}' not implemented in API"

            except Exception as e:
                results[ind_name] = f"Error: {str(e)}"

        return jsonify({
            'success': True,
            'results': results
        })

    except ValueError as e:
        return jsonify({'success': False, 'error': str(e)}), 400

    except Exception as e:
        logger.error(f"Error calculating indicators: {e}")
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/strategy/market-analysis', methods=['POST'])
def market_analysis():
    """
    Get comprehensive market analysis

    Request body:
    {
        "data": { "data": [...] }
    }

    Returns full market analysis with signals, trends, etc.
    """
    try:
        data = request.get_json()

        if not data or 'data' not in data:
            return jsonify({'success': False, 'error': 'No data provided'}), 400

        df = parse_market_data(data['data'])

        # Get comprehensive analysis
        analysis = analyze_market(df)

        # Convert numpy types
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

        analysis = convert_types(analysis)

        return jsonify({
            'success': True,
            'analysis': analysis,
            'timestamp': datetime.now().isoformat()
        })

    except ValueError as e:
        return jsonify({'success': False, 'error': str(e)}), 400

    except Exception as e:
        logger.error(f"Error in market analysis: {e}")
        return jsonify({'success': False, 'error': str(e)}), 500


if __name__ == '__main__':
    print("\n" + "="*80)
    print("MEM ADVANCED STRATEGY API")
    print("="*80)
    print("\nEndpoints:")
    print("  GET  /api/strategy/health")
    print("  GET  /api/strategy/indicators")
    print("  POST /api/strategy/analyze")
    print("  POST /api/strategy/indicators/calculate")
    print("  POST /api/strategy/market-analysis")
    print("\n" + "="*80)
    print("Starting server on http://0.0.0.0:5004")
    print("="*80 + "\n")

    app.run(host='0.0.0.0', port=5004, debug=False)
