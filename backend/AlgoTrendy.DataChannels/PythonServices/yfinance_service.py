#!/usr/bin/env python3
"""
yfinance Data Service - FREE tier Python service for Yahoo Finance data
Provides historical data, options chains, and fundamentals

Usage:
  python yfinance_service.py

Endpoints:
  GET /health - Health check
  GET /historical?symbol=AAPL&start=2020-01-01&end=2024-01-01&interval=1d
  GET /latest?symbol=AAPL
  GET /options?symbol=AAPL&expiration=2025-12-19
  GET /options/expirations?symbol=AAPL
  GET /info?symbol=AAPL

Install: pip install yfinance flask
"""

import yfinance as yf
import json
from datetime import datetime
from typing import Dict, List, Optional
from flask import Flask, request, jsonify
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)


class YFinanceService:
    """Python service for yfinance data fetching"""

    def get_historical(
        self,
        symbol: str,
        start_date: str,
        end_date: str,
        interval: str = "1d"
    ) -> Dict:
        """
        Fetch historical data

        Args:
            symbol: Stock symbol (e.g., 'AAPL')
            start_date: Start date in YYYY-MM-DD format
            end_date: End date in YYYY-MM-DD format
            interval: Data interval (1m, 5m, 15m, 30m, 1h, 1d, 1wk, 1mo)

        Returns:
            Dict with 'data' list or 'error' message
        """
        try:
            logger.info(f"Fetching historical data for {symbol} from {start_date} to {end_date}")

            ticker = yf.Ticker(symbol)
            hist = ticker.history(start=start_date, end=end_date, interval=interval)

            if hist.empty:
                logger.warning(f"No data found for {symbol}")
                return {"data": [], "count": 0}

            # Convert to list of dicts
            data = []
            for index, row in hist.iterrows():
                data.append({
                    "symbol": symbol,
                    "timestamp": index.isoformat(),
                    "open": float(row['Open']),
                    "high": float(row['High']),
                    "low": float(row['Low']),
                    "close": float(row['Close']),
                    "volume": int(row['Volume']),
                    "source": "yfinance"
                })

            logger.info(f"Fetched {len(data)} bars for {symbol}")
            return {"data": data, "count": len(data)}

        except Exception as e:
            logger.error(f"Error fetching historical data for {symbol}: {str(e)}")
            return {"error": str(e)}

    def get_latest(self, symbol: str) -> Dict:
        """
        Get latest quote

        Args:
            symbol: Stock symbol

        Returns:
            Dict with latest market data or error
        """
        try:
            logger.info(f"Fetching latest quote for {symbol}")

            ticker = yf.Ticker(symbol)
            hist = ticker.history(period="1d")

            if hist.empty:
                return {"error": f"No data found for {symbol}"}

            latest = hist.iloc[-1]
            latest_date = hist.index[-1]

            return {
                "symbol": symbol,
                "timestamp": latest_date.isoformat(),
                "open": float(latest['Open']),
                "high": float(latest['High']),
                "low": float(latest['Low']),
                "close": float(latest['Close']),
                "volume": int(latest['Volume']),
                "source": "yfinance"
            }

        except Exception as e:
            logger.error(f"Error fetching latest quote for {symbol}: {str(e)}")
            return {"error": str(e)}

    def get_options_chain(self, symbol: str, expiration: str) -> Dict:
        """
        Fetch options chain for a given expiration

        Args:
            symbol: Stock symbol
            expiration: Expiration date in YYYY-MM-DD format

        Returns:
            Dict with calls and puts lists or error
        """
        try:
            logger.info(f"Fetching options chain for {symbol} expiring {expiration}")

            ticker = yf.Ticker(symbol)
            options = ticker.option_chain(expiration)

            # Convert DataFrames to dicts
            calls = options.calls.to_dict('records')
            puts = options.puts.to_dict('records')

            # Clean up NaN values
            calls = [{k: (None if str(v) == 'nan' else v) for k, v in call.items()} for call in calls]
            puts = [{k: (None if str(v) == 'nan' else v) for k, v in put.items()} for put in puts]

            logger.info(f"Fetched {len(calls)} calls and {len(puts)} puts for {symbol}")

            return {
                "symbol": symbol,
                "expiration": expiration,
                "calls": calls,
                "puts": puts,
                "calls_count": len(calls),
                "puts_count": len(puts)
            }

        except Exception as e:
            logger.error(f"Error fetching options chain for {symbol}: {str(e)}")
            return {"error": str(e)}

    def get_options_expirations(self, symbol: str) -> Dict:
        """
        Get all available option expiration dates

        Args:
            symbol: Stock symbol

        Returns:
            Dict with expirations list or error
        """
        try:
            logger.info(f"Fetching option expirations for {symbol}")

            ticker = yf.Ticker(symbol)
            expirations = list(ticker.options)

            logger.info(f"Found {len(expirations)} expiration dates for {symbol}")

            return {
                "symbol": symbol,
                "expirations": expirations,
                "count": len(expirations)
            }

        except Exception as e:
            logger.error(f"Error fetching expirations for {symbol}: {str(e)}")
            return {"error": str(e)}

    def get_info(self, symbol: str) -> Dict:
        """
        Get company fundamentals

        Args:
            symbol: Stock symbol

        Returns:
            Dict with company info or error
        """
        try:
            logger.info(f"Fetching company info for {symbol}")

            ticker = yf.Ticker(symbol)
            info = ticker.info

            # Extract key metrics (handle missing values)
            data = {
                "symbol": symbol,
                "company_name": info.get('longName'),
                "sector": info.get('sector'),
                "industry": info.get('industry'),
                "market_cap": info.get('marketCap'),
                "pe_ratio": info.get('trailingPE'),
                "forward_pe": info.get('forwardPE'),
                "peg_ratio": info.get('pegRatio'),
                "dividend_yield": info.get('dividendYield'),
                "52w_high": info.get('fiftyTwoWeekHigh'),
                "52w_low": info.get('fiftyTwoWeekLow'),
                "beta": info.get('beta'),
                "price": info.get('currentPrice'),
                "previous_close": info.get('previousClose'),
                "volume": info.get('volume'),
                "average_volume": info.get('averageVolume'),
                "employees": info.get('fullTimeEmployees'),
                "website": info.get('website'),
                "description": info.get('longBusinessSummary')
            }

            return data

        except Exception as e:
            logger.error(f"Error fetching info for {symbol}: {str(e)}")
            return {"error": str(e)}


# Initialize service
service = YFinanceService()


# Flask routes
@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({"status": "healthy", "service": "yfinance", "version": "1.0"}), 200


@app.route('/historical', methods=['GET'])
def historical():
    """Get historical data"""
    symbol = request.args.get('symbol')
    start = request.args.get('start')
    end = request.args.get('end')
    interval = request.args.get('interval', '1d')

    if not symbol:
        return jsonify({"error": "symbol parameter is required"}), 400
    if not start:
        return jsonify({"error": "start parameter is required"}), 400
    if not end:
        return jsonify({"error": "end parameter is required"}), 400

    result = service.get_historical(symbol, start, end, interval)
    return jsonify(result), 200


@app.route('/latest', methods=['GET'])
def latest():
    """Get latest quote"""
    symbol = request.args.get('symbol')

    if not symbol:
        return jsonify({"error": "symbol parameter is required"}), 400

    result = service.get_latest(symbol)
    return jsonify(result), 200


@app.route('/options', methods=['GET'])
def options():
    """Get options chain"""
    symbol = request.args.get('symbol')
    expiration = request.args.get('expiration')

    if not symbol:
        return jsonify({"error": "symbol parameter is required"}), 400
    if not expiration:
        return jsonify({"error": "expiration parameter is required"}), 400

    result = service.get_options_chain(symbol, expiration)
    return jsonify(result), 200


@app.route('/options/expirations', methods=['GET'])
def expirations():
    """Get available option expiration dates"""
    symbol = request.args.get('symbol')

    if not symbol:
        return jsonify({"error": "symbol parameter is required"}), 400

    result = service.get_options_expirations(symbol)
    return jsonify(result), 200


@app.route('/info', methods=['GET'])
def info():
    """Get company fundamentals"""
    symbol = request.args.get('symbol')

    if not symbol:
        return jsonify({"error": "symbol parameter is required"}), 400

    result = service.get_info(symbol)
    return jsonify(result), 200


if __name__ == "__main__":
    logger.info("Starting yfinance service on port 5001")
    app.run(host='0.0.0.0', port=5001, debug=False)
