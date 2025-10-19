"""
Kraken market data channel
Fetches OHLCV data from Kraken REST API
"""

from typing import Dict, Any, List
from datetime import datetime, timedelta
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, ConnectionError


class KrakenChannel(DataChannel):
    """Kraken exchange data channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Kraken",
            data_type=DataType.MARKET_DATA,
            config=config or {}
        )
        self.base_url = "https://api.kraken.com"
        # Kraken uses unique pair names
        self.default_symbols = [
            "XXBTZUSD", "XETHZUSD", "SOLUSD", "ADAUSD", "XXRPZUSD",
            "DOGEUSD", "DOTUSD", "MATICUSD", "AVAXUSD", "LINKUSD"
        ]
        # Symbol mapping for standardization
        self.symbol_map = {
            "XXBTZUSD": "BTCUSD",
            "XETHZUSD": "ETHUSD",
            "XXRPZUSD": "XRPUSD",
            "SOLUSD": "SOLUSD",
            "ADAUSD": "ADAUSD",
            "DOGEUSD": "DOGEUSD",
            "DOTUSD": "DOTUSD",
            "MATICUSD": "MATICUSD",
            "AVAXUSD": "AVAXUSD",
            "LINKUSD": "LINKUSD"
        }

    async def _connect(self) -> bool:
        """Test connection to Kraken API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/0/public/Time",
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    if response.status == 200:
                        data = await response.json()
                        return len(data.get("error", [])) == 0
                    return False
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, symbols: List[str] = None, interval: int = 1, since: int = None) -> List[Dict[str, Any]]:
        """
        Fetch OHLC data from Kraken.

        Args:
            symbols: List of trading pairs (Kraken format: XXBTZUSD)
            interval: Time frame in minutes (1, 5, 15, 30, 60, 240, 1440, 10080, 21600)
            since: Return committed OHLC data since given timestamp
        """
        symbols = symbols or self.default_symbols
        all_data = []

        # Kraken interval mapping (in minutes)
        valid_intervals = [1, 5, 15, 30, 60, 240, 1440, 10080, 21600]
        if interval not in valid_intervals:
            interval = min(valid_intervals, key=lambda x: abs(x - interval))

        async with aiohttp.ClientSession() as session:
            for symbol in symbols:
                try:
                    params = {
                        "pair": symbol,
                        "interval": interval
                    }
                    if since:
                        params["since"] = since

                    async with session.get(
                        f"{self.base_url}/0/public/OHLC",
                        params=params,
                        timeout=aiohttp.ClientTimeout(total=10)
                    ) as response:
                        if response.status == 429:
                            raise RateLimitError("Kraken rate limit exceeded", retry_after=60)

                        if response.status != 200:
                            self.logger.error(f"HTTP {response.status} for {symbol}")
                            continue

                        result = await response.json()

                        if result.get("error") and len(result["error"]) > 0:
                            self.logger.error(f"Kraken API error for {symbol}: {result['error']}")
                            continue

                        # Kraken returns data under the pair name
                        pair_data = result.get("result", {})

                        # Find the actual pair key (Kraken sometimes changes it)
                        ohlc_data = None
                        for key in pair_data:
                            if key != "last" and isinstance(pair_data[key], list):
                                ohlc_data = pair_data[key]
                                break

                        if not ohlc_data:
                            self.logger.warning(f"No OHLC data for {symbol}")
                            continue

                        # Transform OHLC data
                        # Kraken format: [time, open, high, low, close, vwap, volume, count]
                        standard_symbol = self.symbol_map.get(symbol, symbol)

                        for ohlc in ohlc_data:
                            all_data.append({
                                "symbol": standard_symbol,
                                "timestamp": datetime.fromtimestamp(int(ohlc[0])),
                                "open": float(ohlc[1]),
                                "high": float(ohlc[2]),
                                "low": float(ohlc[3]),
                                "close": float(ohlc[4]),
                                "vwap": float(ohlc[5]),
                                "volume": float(ohlc[6]),
                                "trades_count": int(ohlc[7]),
                            })

                except aiohttp.ClientError as e:
                    self.logger.error(f"Network error fetching {symbol}: {e}")
                    continue
                except Exception as e:
                    self.logger.error(f"Error fetching {symbol}: {e}")
                    continue

        self.logger.info(f"Fetched {len(all_data)} OHLC records from {len(symbols)} symbols")
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
            "trades_count": raw_data.get("trades_count"),
            "vwap": raw_data.get("vwap"),
            "metadata_json": {
                "exchange": "kraken",
            }
        }

    async def save_to_db(self, db: Session, data: List[Dict[str, Any]]) -> int:
        """Save OHLC data to database."""
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
