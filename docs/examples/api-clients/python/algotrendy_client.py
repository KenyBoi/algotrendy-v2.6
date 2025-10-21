"""
AlgoTrendy Python API Client
Usage example for interacting with AlgoTrendy v2.6 API
"""

import requests
import json
from typing import Dict, List, Optional
from datetime import datetime, timedelta


class AlgoTrendyClient:
    """
    Python client for AlgoTrendy API

    Example:
        client = AlgoTrendyClient("http://localhost:5002")
        balance = client.get_balance("bybit")
        print(f"Balance: {balance['balance']} USDT")
    """

    def __init__(self, base_url: str, api_key: Optional[str] = None):
        """
        Initialize AlgoTrendy client

        Args:
            base_url: API base URL (e.g., "http://localhost:5002")
            api_key: Optional API key for authentication
        """
        self.base_url = base_url.rstrip('/')
        self.api_key = api_key
        self.session = requests.Session()

        if api_key:
            self.session.headers.update({'X-API-Key': api_key})

    def _get(self, endpoint: str, params: Optional[Dict] = None) -> Dict:
        """Make GET request"""
        url = f"{self.base_url}{endpoint}"
        response = self.session.get(url, params=params)
        response.raise_for_status()
        return response.json()

    def _post(self, endpoint: str, data: Dict) -> Dict:
        """Make POST request"""
        url = f"{self.base_url}{endpoint}"
        response = self.session.post(url, json=data)
        response.raise_for_status()
        return response.json()

    # Health & Status

    def get_health(self) -> Dict:
        """Get API health status"""
        return self._get("/health")

    def get_detailed_health(self) -> Dict:
        """Get detailed health status with all checks"""
        return self._get("/api/health/detailed")

    # Trading Operations

    def get_balance(self, exchange: str, currency: str = "USDT") -> Dict:
        """
        Get account balance

        Args:
            exchange: Exchange name (bybit, binance, etc.)
            currency: Currency symbol (default: USDT)

        Returns:
            Dict with balance information
        """
        return self._get(f"/api/trading/balance", {
            'exchange': exchange,
            'currency': currency
        })

    def get_positions(self, exchange: str) -> List[Dict]:
        """
        Get all open positions

        Args:
            exchange: Exchange name

        Returns:
            List of position dictionaries
        """
        return self._get(f"/api/trading/positions", {'exchange': exchange})

    def place_order(self,
                   exchange: str,
                   symbol: str,
                   side: str,
                   order_type: str,
                   quantity: float,
                   price: Optional[float] = None,
                   stop_price: Optional[float] = None,
                   client_order_id: Optional[str] = None) -> Dict:
        """
        Place a trading order

        Args:
            exchange: Exchange name (bybit, binance, etc.)
            symbol: Trading pair (BTCUSDT, ETHUSDT, etc.)
            side: Buy or Sell
            order_type: Market, Limit, StopLoss, etc.
            quantity: Order quantity
            price: Limit price (for Limit orders)
            stop_price: Stop price (for Stop orders)
            client_order_id: Custom order ID for idempotency

        Returns:
            Order result dictionary

        Example:
            # Market buy
            order = client.place_order(
                exchange="bybit",
                symbol="BTCUSDT",
                side="Buy",
                order_type="Market",
                quantity=0.001
            )

            # Limit sell
            order = client.place_order(
                exchange="bybit",
                symbol="ETHUSDT",
                side="Sell",
                order_type="Limit",
                quantity=0.1,
                price=3000.00
            )
        """
        order_data = {
            "exchange": exchange,
            "symbol": symbol,
            "side": side,
            "type": order_type,
            "quantity": quantity
        }

        if price:
            order_data["price"] = price
        if stop_price:
            order_data["stopPrice"] = stop_price
        if client_order_id:
            order_data["clientOrderId"] = client_order_id

        return self._post("/api/trading/order", order_data)

    def cancel_order(self, exchange: str, order_id: str) -> Dict:
        """Cancel an order"""
        return self._post("/api/trading/order/cancel", {
            "exchange": exchange,
            "orderId": order_id
        })

    # Market Data

    def get_market_data(self,
                       symbol: str,
                       exchange: str,
                       interval: str = "1h",
                       limit: int = 100) -> List[Dict]:
        """
        Get historical market data

        Args:
            symbol: Trading symbol
            exchange: Exchange name
            interval: Time interval (1m, 5m, 1h, 1d)
            limit: Number of candles to retrieve

        Returns:
            List of OHLCV candles
        """
        return self._get("/api/marketdata", {
            'symbol': symbol,
            'exchange': exchange,
            'interval': interval,
            'limit': limit
        })

    # Backtesting

    def run_backtest(self,
                    strategy: str,
                    symbols: List[str],
                    start_date: str,
                    end_date: str,
                    initial_capital: float = 10000,
                    engine: str = "auto") -> Dict:
        """
        Run a backtest

        Args:
            strategy: Strategy name
            symbols: List of symbols to trade
            start_date: Start date (YYYY-MM-DD)
            end_date: End date (YYYY-MM-DD)
            initial_capital: Starting capital
            engine: Backtest engine (auto, quantconnect, local, custom)

        Returns:
            Backtest results

        Example:
            results = client.run_backtest(
                strategy="EMA Cross",
                symbols=["BTCUSDT"],
                start_date="2024-01-01",
                end_date="2024-12-31",
                initial_capital=10000
            )
        """
        return self._post("/api/backtest/run", {
            "strategy": strategy,
            "symbols": symbols,
            "startDate": start_date,
            "endDate": end_date,
            "initialCapital": initial_capital,
            "engine": engine
        })

    def get_backtest_results(self, backtest_id: str) -> Dict:
        """Get backtest results by ID"""
        return self._get(f"/api/backtest/results/{backtest_id}")

    # Metrics & Monitoring

    def get_metrics(self) -> Dict:
        """Get application metrics"""
        return self._get("/api/metrics")

    def get_metrics_summary(self) -> Dict:
        """Get metrics summary"""
        return self._get("/api/metrics/summary")


# Example Usage
if __name__ == "__main__":
    # Initialize client
    client = AlgoTrendyClient("http://localhost:5002")

    # Check health
    print("Checking API health...")
    health = client.get_health()
    print(f"✅ API Status: {health['status']}")

    # Get balance (requires credentials configured)
    try:
        print("\nGetting balance...")
        balance = client.get_balance("bybit")
        print(f"💰 Balance: {balance['balance']} {balance['currency']}")
    except requests.exceptions.HTTPError as e:
        print(f"⚠️ Balance check failed (credentials may not be configured): {e}")

    # Get market data
    print("\nGetting market data...")
    candles = client.get_market_data(
        symbol="BTCUSDT",
        exchange="binance",
        interval="1h",
        limit=10
    )
    print(f"📊 Retrieved {len(candles)} candles")
    if candles:
        latest = candles[-1]
        print(f"   Latest: Open={latest['open']}, Close={latest['close']}")

    # Place market order (TESTNET ONLY)
    # Uncomment to test (requires testnet credentials)
    """
    print("\nPlacing test order...")
    order = client.place_order(
        exchange="bybit",
        symbol="BTCUSDT",
        side="Buy",
        order_type="Market",
        quantity=0.001
    )
    print(f"✅ Order placed: {order['orderId']}")
    print(f"   Status: {order['status']}")
    """

    # Run backtest
    print("\nRunning backtest...")
    try:
        backtest = client.run_backtest(
            strategy="RSI",
            symbols=["BTCUSDT"],
            start_date=(datetime.now() - timedelta(days=365)).strftime("%Y-%m-%d"),
            end_date=datetime.now().strftime("%Y-%m-%d"),
            initial_capital=10000
        )
        print(f"📈 Backtest ID: {backtest['backtestId']}")
        print(f"   Status: {backtest['status']}")
    except Exception as e:
        print(f"⚠️ Backtest failed: {e}")

    # Get metrics
    print("\nGetting metrics...")
    try:
        metrics = client.get_metrics_summary()
        print(f"📊 Total Requests: {metrics['summary']['totalRequests']}")
        print(f"   Error Rate: {metrics['summary']['errorRate']}%")
        print(f"   Avg Duration: {metrics['summary']['averageDurationMs']}ms")
    except Exception as e:
        print(f"⚠️ Metrics not available: {e}")

    print("\n✅ Example completed!")
