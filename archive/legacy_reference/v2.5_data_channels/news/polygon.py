"""
Polygon.io news channel
Financial news and market data
"""

from typing import Dict, Any, List
from datetime import datetime
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, AuthenticationError


class PolygonChannel(DataChannel):
    """Polygon.io news channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Polygon.io",
            data_type=DataType.NEWS,
            config=config or {}
        )
        self.api_key = config.get("api_key", "")  # Requires API key
        self.base_url = "https://api.polygon.io"

    async def _connect(self) -> bool:
        """Test connection to Polygon.io API."""
        if not self.api_key:
            self.logger.warning("No API key configured for Polygon.io")
            return False

        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/v2/reference/news",
                    params={"apiKey": self.api_key, "limit": 1},
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status == 200
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, ticker: str = "X:BTCUSD", limit: int = 50) -> List[Dict[str, Any]]:
        """
        Fetch news from Polygon.io.

        Args:
            ticker: Ticker symbol (X:BTCUSD for Bitcoin, X:ETHUSD for Ethereum)
            limit: Number of articles to fetch (max 1000)
        """
        if not self.api_key:
            self.logger.warning("No API key configured")
            return []

        all_articles = []

        async with aiohttp.ClientSession() as session:
            try:
                async with session.get(
                    f"{self.base_url}/v2/reference/news",
                    params={
                        "ticker": ticker,
                        "limit": limit,
                        "apiKey": self.api_key
                    },
                    timeout=aiohttp.ClientTimeout(total=10)
                ) as response:
                    if response.status == 401 or response.status == 403:
                        raise AuthenticationError("Invalid Polygon.io API key")

                    if response.status == 429:
                        raise RateLimitError("Polygon.io rate limit exceeded", retry_after=60)

                    if response.status != 200:
                        self.logger.error(f"HTTP {response.status}")
                        return []

                    data = await response.json()
                    results = data.get("results", [])

                    for article in results:
                        all_articles.append({
                            "url": article.get("article_url"),
                            "title": article.get("title"),
                            "author": article.get("author"),
                            "published_utc": article.get("published_utc"),
                            "description": article.get("description"),
                            "tickers": article.get("tickers", []),
                            "image_url": article.get("image_url"),
                            "publisher": article.get("publisher", {}).get("name"),
                        })

            except (AuthenticationError, RateLimitError):
                raise
            except Exception as e:
                self.logger.error(f"Error fetching news: {e}")

        self.logger.info(f"Fetched {len(all_articles)} articles from Polygon.io")
        return all_articles

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate news article data."""
        required_fields = ["title", "url"]
        return all(field in data and data[field] for field in required_fields)

    async def transform_data(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """Transform to standardized format."""
        # Parse published date
        try:
            if raw_data.get("published_utc"):
                # Polygon uses ISO format
                timestamp = datetime.fromisoformat(raw_data["published_utc"].replace("Z", "+00:00"))
            else:
                timestamp = datetime.utcnow()
        except:
            timestamp = datetime.utcnow()

        # Extract crypto symbols from tickers
        symbols = []
        for ticker in raw_data.get("tickers", []):
            if ticker.startswith("X:"):  # Crypto tickers
                symbol = ticker.replace("X:", "").replace("USD", "USDT")
                symbols.append(symbol)

        return {
            "timestamp": timestamp,
            "source_id": self.source_id,
            "article_url": raw_data.get("url"),
            "title": raw_data.get("title"),
            "summary": raw_data.get("description", "")[:500] if raw_data.get("description") else None,
            "content": raw_data.get("description"),
            "author": raw_data.get("author") or raw_data.get("publisher"),
            "symbols": symbols if symbols else None,
            "categories": ["finance", "crypto"],
            "sentiment_score": None,
            "sentiment_label": None,
            "image_url": raw_data.get("image_url"),
            "metadata_json": {
                "source": "polygon",
                "publisher": raw_data.get("publisher"),
                "tickers": raw_data.get("tickers", []),
            }
        }

    async def save_to_db(self, db: Session, data: List[Dict[str, Any]]) -> int:
        """Save articles to database."""
        import json
        count = 0

        for record in data:
            try:
                record_copy = record.copy()
                if 'metadata_json' in record_copy and record_copy['metadata_json'] is not None:
                    record_copy['metadata_json'] = json.dumps(record_copy['metadata_json'])
                else:
                    record_copy['metadata_json'] = None

                # Convert lists to PostgreSQL array format
                if record_copy.get('symbols'):
                    record_copy['symbols'] = '{' + ','.join(record_copy['symbols']) + '}'
                else:
                    record_copy['symbols'] = None

                if record_copy.get('categories'):
                    record_copy['categories'] = '{' + ','.join(record_copy['categories']) + '}'
                else:
                    record_copy['categories'] = None

                db.execute(text("""
                    INSERT INTO news_articles (
                        timestamp, source_id, article_url, title, summary, content,
                        author, symbols, categories, sentiment_score, sentiment_label,
                        image_url, metadata_json
                    ) VALUES (
                        :timestamp, :source_id, :article_url, :title, :summary, :content,
                        :author, :symbols::text[], :categories::text[], :sentiment_score, :sentiment_label,
                        :image_url, CAST(:metadata_json AS jsonb)
                    )
                    ON CONFLICT (timestamp, article_id) DO NOTHING
                """), record_copy)
                count += 1
            except Exception as e:
                self.logger.error(f"Error saving article: {e}")
                continue

        db.commit()
        return count
