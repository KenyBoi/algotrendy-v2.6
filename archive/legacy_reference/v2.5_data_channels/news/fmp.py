"""
Financial Modeling Prep (FMP) news channel
Fetches crypto and general market news
"""

from typing import Dict, Any, List
from datetime import datetime
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, AuthenticationError


class FMPChannel(DataChannel):
    """Financial Modeling Prep news channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Financial Modeling Prep",
            data_type=DataType.NEWS,
            config=config or {}
        )
        self.api_key = config.get("api_key", "demo")  # Demo key for testing
        self.base_url = "https://financialmodelingprep.com/api/v3"

    async def _connect(self) -> bool:
        """Test connection to FMP API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/stock/list",
                    params={"apikey": self.api_key},
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status == 200
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, limit: int = 50) -> List[Dict[str, Any]]:
        """
        Fetch crypto news from FMP.

        Args:
            limit: Number of articles to fetch
        """
        all_articles = []

        async with aiohttp.ClientSession() as session:
            try:
                # Fetch crypto news
                async with session.get(
                    f"{self.base_url}/stock_news",
                    params={
                        "tickers": "BTCUSD,ETHUSD,SOLUSD",
                        "limit": limit,
                        "apikey": self.api_key
                    },
                    timeout=aiohttp.ClientTimeout(total=10)
                ) as response:
                    if response.status == 401:
                        raise AuthenticationError("Invalid FMP API key")

                    if response.status == 429:
                        raise RateLimitError("FMP rate limit exceeded", retry_after=60)

                    if response.status != 200:
                        self.logger.error(f"HTTP {response.status}")
                        return []

                    articles = await response.json()

                    for article in articles:
                        all_articles.append({
                            "url": article.get("url"),
                            "title": article.get("title"),
                            "text": article.get("text"),
                            "symbol": article.get("symbol"),
                            "published_date": article.get("publishedDate"),
                            "site": article.get("site"),
                            "image": article.get("image"),
                        })

            except (AuthenticationError, RateLimitError):
                raise
            except Exception as e:
                self.logger.error(f"Error fetching news: {e}")

        self.logger.info(f"Fetched {len(all_articles)} articles from FMP")
        return all_articles

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate news article data."""
        required_fields = ["title", "url"]
        return all(field in data and data[field] for field in required_fields)

    async def transform_data(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """Transform to standardized format."""
        # Parse published date
        try:
            if raw_data.get("published_date"):
                timestamp = datetime.fromisoformat(raw_data["published_date"].replace("Z", "+00:00"))
            else:
                timestamp = datetime.utcnow()
        except:
            timestamp = datetime.utcnow()

        # Extract symbols
        symbols = []
        if raw_data.get("symbol"):
            symbols.append(raw_data["symbol"])

        return {
            "timestamp": timestamp,
            "source_id": self.source_id,
            "article_url": raw_data.get("url"),
            "title": raw_data.get("title"),
            "summary": raw_data.get("text", "")[:500] if raw_data.get("text") else None,
            "content": raw_data.get("text"),
            "author": raw_data.get("site"),
            "symbols": symbols if symbols else None,
            "categories": ["crypto", "finance"],
            "sentiment_score": None,
            "sentiment_label": None,
            "image_url": raw_data.get("image"),
            "metadata_json": {
                "source": "fmp",
                "site": raw_data.get("site"),
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
                if record_copy.get('categories'):
                    record_copy['categories'] = '{' + ','.join(record_copy['categories']) + '}'

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
