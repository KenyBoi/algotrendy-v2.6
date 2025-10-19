"""
Coinbase market data channel
Fetches OHLCV data from Coinbase Advanced Trade API
"""

from typing import Dict, Any, List
from datetime import datetime, timedelta
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, ConnectionError


class CoinbaseChannel(DataChannel):
    """Coinbase exchange data channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Coinbase",
            data_type=DataType.MARKET_DATA,
            config=config or {}
        )
        self.base_url = "https://api.exchange.coinbase.com"
        self.default_symbols = [
            "BTC-USD", "ETH-USD", "SOL-USD", "ADA-USD", "XRP-USD",
            "DOGE-USD", "DOT-USD", "MATIC-USD", "AVAX-USD", "LINK-USD"
        ]

    async def _connect(self) -> bool:
        """Test connection to Coinbase API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/time",
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status == 200
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, symbols: List[str] = None, interval: int = 60, limit: int = 300) -> List[Dict[str, Any]]:
        """
        Fetch OHLCV candles from Coinbase.

        Args:
            symbols: List of trading pairs (Coinbase format: BTC-USD)
            interval: Granularity in seconds (60, 300, 900, 3600, 21600, 86400)
            limit: Number of candles (max 300)
        """
        symbols = symbols or self.default_symbols
        all_data = []

        # Valid granularities in seconds
        valid_granularities = [60, 300, 900, 3600, 21600, 86400]
        if interval not in valid_granularities:
            interval = min(valid_granularities, key=lambda x: abs(x - interval))

        async with aiohttp.ClientSession() as session:
            for symbol in symbols:
                try:
                    # Calculate time range
                    end_time = datetime.utcnow()
                    start_time = end_time - timedelta(seconds=interval * limit)

                    async with session.get(
                        f"{self.base_url}/products/{symbol}/candles",
                        params={
                            "granularity": interval,
                            "start": start_time.isoformat(),
                            "end": end_time.isoformat()
                        },
                        timeout=aiohttp.ClientTimeout(total=10)
                    ) as response:
                        if response.status == 429:
                            raise RateLimitError("Coinbase rate limit exceeded", retry_after=60)

                        if response.status != 200:
                            self.logger.error(f"HTTP {response.status} for {symbol}")
                            continue

                        candles = await response.json()

                        # Transform candle data
                        # Coinbase format: [timestamp, low, high, open, close, volume]
                        for candle in candles:
                            all_data.append({
                                "symbol": symbol.replace("-", ""),  # Convert BTC-USD to BTCUSD
                                "timestamp": datetime.fromtimestamp(candle[0]),
                                "open": float(candle[3]),
                                "high": float(candle[2]),
                                "low": float(candle[1]),
                                "close": float(candle[4]),
                                "volume": float(candle[5]),
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
            "quote_volume": None,
            "trades_count": None,
            "vwap": (raw_data["high"] + raw_data["low"] + raw_data["close"]) / 3,
            "metadata_json": {
                "exchange": "coinbase",
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
                        vwap = EXCLUDED.vwap,
                        metadata_json = EXCLUDED.metadata_json
                """), record_copy)
                count += 1
            except Exception as e:
                self.logger.error(f"Error saving record: {e}")
                continue

        db.commit()
        return count
