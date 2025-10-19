"""
OKX market data channel
Fetches OHLCV data from OKX REST API
"""

from typing import Dict, Any, List
from datetime import datetime, timedelta
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, ConnectionError


class OKXChannel(DataChannel):
    """OKX exchange data channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="OKX",
            data_type=DataType.MARKET_DATA,
            config=config or {}
        )
        self.base_url = "https://www.okx.com"
        self.default_symbols = [
            "BTC-USDT", "ETH-USDT", "SOL-USDT", "ADA-USDT", "XRP-USDT",
            "DOGE-USDT", "DOT-USDT", "MATIC-USDT", "AVAX-USDT", "LINK-USDT"
        ]

    async def _connect(self) -> bool:
        """Test connection to OKX API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/api/v5/public/time",
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        return data.get("code") == "0"
                    return False
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, symbols: List[str] = None, interval: str = "1m", limit: int = 100) -> List[Dict[str, Any]]:
        """
        Fetch OHLCV candlestick data from OKX.

        Args:
            symbols: List of trading pairs (OKX format: BTC-USDT)
            interval: Bar interval (1m, 5m, 15m, 1H, 4H, 1D)
            limit: Number of bars (max 100)
        """
        symbols = symbols or self.default_symbols
        all_data = []

        # OKX interval mapping
        interval_map = {
            "1m": "1m",
            "5m": "5m",
            "15m": "15m",
            "1h": "1H",
            "4h": "4H",
            "1d": "1D"
        }
        bar_interval = interval_map.get(interval, "1m")

        async with aiohttp.ClientSession() as session:
            for symbol in symbols:
                try:
                    async with session.get(
                        f"{self.base_url}/api/v5/market/candles",
                        params={
                            "instId": symbol,
                            "bar": bar_interval,
                            "limit": str(min(limit, 100))
                        },
                        timeout=aiohttp.ClientTimeout(total=10)
                    ) as response:
                        if response.status == 429:
                            raise RateLimitError("OKX rate limit exceeded", retry_after=60)

                        if response.status != 200:
                            self.logger.error(f"HTTP {response.status} for {symbol}")
                            continue

                        result = await response.json()

                        if result.get("code") != "0":
                            self.logger.error(f"OKX API error for {symbol}: {result.get('msg')}")
                            continue

                        candles = result.get("data", [])

                        # Transform candle data
                        # OKX format: [timestamp, open, high, low, close, volume, volCcy, volCcyQuote, confirm]
                        for candle in candles:
                            all_data.append({
                                "symbol": symbol.replace("-", ""),  # Convert BTC-USDT to BTCUSDT
                                "timestamp": datetime.fromtimestamp(int(candle[0]) / 1000),
                                "open": float(candle[1]),
                                "high": float(candle[2]),
                                "low": float(candle[3]),
                                "close": float(candle[4]),
                                "volume": float(candle[5]),
                                "quote_volume": float(candle[6]) if len(candle) > 6 else None,
                                "confirmed": candle[8] == "1" if len(candle) > 8 else True,
                            })

                except aiohttp.ClientError as e:
                    self.logger.error(f"Network error fetching {symbol}: {e}")
                    continue
                except Exception as e:
                    self.logger.error(f"Error fetching {symbol}: {e}")
                    continue

        self.logger.info(f"Fetched {len(all_data)} candles from {len(symbols)} symbols")
        return all_data

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate OHLCV data."""
        required_fields = ["symbol", "timestamp", "open", "high", "low", "close", "volume"]

        if not all(field in data for field in required_fields):
            return False

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
            "trades_count": None,
            "vwap": (raw_data["high"] + raw_data["low"] + raw_data["close"]) / 3,
            "metadata_json": {
                "exchange": "okx",
                "confirmed": raw_data.get("confirmed", True),
            }
        }

    async def save_to_db(self, db: Session, data: List[Dict[str, Any]]) -> int:
        """Save candles to database."""
        import json
        count = 0

        for record in data:
            try:
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
                        vwap = EXCLUDED.vwap,
                        metadata_json = EXCLUDED.metadata_json
                """), record_copy)
                count += 1
            except Exception as e:
                self.logger.error(f"Error saving record: {e}")
                continue

        db.commit()
        return count
