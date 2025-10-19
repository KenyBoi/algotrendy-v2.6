"""
Binance market data channel
Fetches OHLCV data from Binance REST API
"""

from typing import Dict, Any, List
from datetime import datetime, timedelta
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, ConnectionError, DataValidationError


class BinanceChannel(DataChannel):
    """Binance exchange data channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Binance",
            data_type=DataType.MARKET_DATA,
            config=config or {}
        )
        self.base_url = "https://api.binance.com"
        self.default_symbols = [
            "BTCUSDT", "ETHUSDT", "BNBUSDT", "SOLUSDT", "ADAUSDT",
            "XRPUSDT", "DOGEUSDT", "DOTUSDT", "MATICUSDT", "AVAXUSDT"
        ]

    async def _connect(self) -> bool:
        """Test connection to Binance API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/api/v3/ping",
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status == 200
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, symbols: List[str] = None, interval: str = "1m", limit: int = 100) -> List[Dict[str, Any]]:
        """
        Fetch OHLCV klines from Binance.

        Args:
            symbols: List of trading pairs (default: top 10 crypto)
            interval: Kline interval (1m, 5m, 15m, 1h, 4h, 1d)
            limit: Number of klines to fetch (max 1000)
        """
        symbols = symbols or self.default_symbols
        all_data = []

        async with aiohttp.ClientSession() as session:
            for symbol in symbols:
                try:
                    # Fetch klines
                    async with session.get(
                        f"{self.base_url}/api/v3/klines",
                        params={
                            "symbol": symbol,
                            "interval": interval,
                            "limit": limit
                        },
                        timeout=aiohttp.ClientTimeout(total=10)
                    ) as response:
                        # Check rate limits
                        if 'X-MBX-USED-WEIGHT-1M' in response.headers:
                            weight = int(response.headers['X-MBX-USED-WEIGHT-1M'])
                            if weight > 1000:  # Binance limit is 1200/min
                                self.logger.warning(f"High rate limit usage: {weight}/1200")

                        if response.status == 429:
                            retry_after = int(response.headers.get('Retry-After', 60))
                            raise RateLimitError(
                                f"Binance rate limit exceeded",
                                retry_after=retry_after
                            )

                        if response.status != 200:
                            self.logger.error(f"HTTP {response.status} for {symbol}")
                            continue

                        klines = await response.json()

                        # Transform kline data
                        for kline in klines:
                            all_data.append({
                                "symbol": symbol,
                                "timestamp": datetime.fromtimestamp(kline[0] / 1000),
                                "open": float(kline[1]),
                                "high": float(kline[2]),
                                "low": float(kline[3]),
                                "close": float(kline[4]),
                                "volume": float(kline[5]),
                                "close_time": datetime.fromtimestamp(kline[6] / 1000),
                                "quote_volume": float(kline[7]),
                                "trades_count": int(kline[8]),
                                "taker_buy_base_volume": float(kline[9]),
                                "taker_buy_quote_volume": float(kline[10]),
                            })

                except aiohttp.ClientError as e:
                    self.logger.error(f"Network error fetching {symbol}: {e}")
                    continue
                except Exception as e:
                    self.logger.error(f"Error fetching {symbol}: {e}")
                    continue

        self.logger.info(f"Fetched {len(all_data)} klines from {len(symbols)} symbols")
        return all_data

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate OHLCV data."""
        required_fields = ["symbol", "timestamp", "open", "high", "low", "close", "volume"]

        # Check required fields
        if not all(field in data for field in required_fields):
            return False

        # Validate OHLC relationship
        try:
            if not (data["low"] <= data["open"] <= data["high"]):
                return False
            if not (data["low"] <= data["close"] <= data["high"]):
                return False
            if data["high"] < data["low"]:
                return False
            if data["volume"] < 0:
                return False
        except (TypeError, KeyError):
            return False

        return True

    async def transform_data(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """Transform to standardized format."""
        return {
            "timestamp": raw_data["timestamp"],
            "symbol": raw_data["symbol"],
            "source_id": self.source_id,
            "open": raw_data["open"],
            "high": raw_data["high"],
            "low": raw_data["low"],
            "close": raw_data["close"],
            "volume": raw_data["volume"],
            "quote_volume": raw_data.get("quote_volume"),
            "trades_count": raw_data.get("trades_count"),
            "vwap": self._calculate_vwap(raw_data),
            "metadata_json": {
                "exchange": "binance",
                "close_time": raw_data.get("close_time").isoformat() if raw_data.get("close_time") else None,
                "taker_buy_base_volume": raw_data.get("taker_buy_base_volume"),
                "taker_buy_quote_volume": raw_data.get("taker_buy_quote_volume"),
            }
        }

    def _calculate_vwap(self, data: Dict[str, Any]) -> float:
        """Calculate Volume Weighted Average Price."""
        try:
            if data.get("quote_volume") and data.get("volume"):
                return data["quote_volume"] / data["volume"]
            else:
                return (data["high"] + data["low"] + data["close"]) / 3
        except (ZeroDivisionError, TypeError):
            return data.get("close", 0)

    async def save_to_db(self, db: Session, data: List[Dict[str, Any]]) -> int:
        """Save klines to database."""
        import json
        count = 0

        for record in data:
            try:
                # Convert metadata_json dict to JSON string
                record_copy = record.copy()
                if 'metadata_json' in record_copy and record_copy['metadata_json'] is not None:
                    record_copy['metadata_json'] = json.dumps(record_copy['metadata_json'])
                else:
                    record_copy['metadata_json'] = None

                db.execute(text("""
                    INSERT INTO market_data (
                        timestamp, symbol, source_id, open, high, low, close,
                        volume, quote_volume, trades_count, vwap, metadata_json
                    ) VALUES (
                        :timestamp, :symbol, :source_id, :open, :high, :low, :close,
                        :volume, :quote_volume, :trades_count, :vwap, CAST(:metadata_json AS jsonb)
                    )
                    ON CONFLICT (timestamp, symbol, source_id) DO UPDATE SET
                        open = EXCLUDED.open,
                        high = EXCLUDED.high,
                        low = EXCLUDED.low,
                        close = EXCLUDED.close,
                        volume = EXCLUDED.volume,
                        quote_volume = EXCLUDED.quote_volume,
                        trades_count = EXCLUDED.trades_count,
                        vwap = EXCLUDED.vwap,
                        metadata_json = EXCLUDED.metadata_json
                """), record_copy)
                count += 1
            except Exception as e:
                self.logger.error(f"Error saving record: {e}")
                continue

        db.commit()
        return count
