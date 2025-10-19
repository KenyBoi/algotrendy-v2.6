"""
CryptoPanic news channel
Crypto-specific news aggregator
"""

from typing import Dict, Any, List
from datetime import datetime
import aiohttp
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError, AuthenticationError


class CryptoPanicChannel(DataChannel):
    """CryptoPanic news aggregator channel."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="CryptoPanic",
            data_type=DataType.NEWS,
            config=config or {}
        )
        self.api_key = config.get("api_key", "")  # Free tier available
        self.base_url = "https://cryptopanic.com/api/v1"

    async def _connect(self) -> bool:
        """Test connection to CryptoPanic API."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    f"{self.base_url}/posts/",
                    params={"auth_token": self.api_key} if self.api_key else {},
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status in [200, 401]  # 401 means API is working but needs key
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, currencies: str = "BTC,ETH,SOL", filter_type: str = "rising") -> List[Dict[str, Any]]:
        """
        Fetch crypto news from CryptoPanic.

        Args:
            currencies: Comma-separated list of currencies
            filter_type: rising, hot, bullish, bearish, important, saved, lol
        """
        all_articles = []

        async with aiohttp.ClientSession() as session:
            try:
                params = {
                    "currencies": currencies,
                    "filter": filter_type,
                    "public": "true"
                }

                if self.api_key:
                    params["auth_token"] = self.api_key

                async with session.get(
                    f"{self.base_url}/posts/",
                    params=params,
                    timeout=aiohttp.ClientTimeout(total=10)
                ) as response:
                    if response.status == 429:
                        raise RateLimitError("CryptoPanic rate limit exceeded", retry_after=60)

                    if response.status != 200:
                        self.logger.error(f"HTTP {response.status}")
                        return []

                    data = await response.json()
                    results = data.get("results", [])

                    for post in results:
                        all_articles.append({
                            "url": post.get("url"),
                            "title": post.get("title"),
                            "published_at": post.get("published_at"),
                            "domain": post.get("domain"),
                            "votes": post.get("votes", {}),
                            "currencies": post.get("currencies", []),
                            "kind": post.get("kind"),  # news, media, blog
                            "source": post.get("source", {}),
                        })

            except RateLimitError:
                raise
            except Exception as e:
                self.logger.error(f"Error fetching news: {e}")

        self.logger.info(f"Fetched {len(all_articles)} articles from CryptoPanic")
        return all_articles

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate news article data."""
        required_fields = ["title", "url"]
        return all(field in data and data[field] for field in required_fields)

    async def transform_data(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """Transform to standardized format."""
        # Parse published date
        try:
            if raw_data.get("published_at"):
                timestamp = datetime.fromisoformat(raw_data["published_at"].replace("Z", "+00:00"))
            else:
                timestamp = datetime.utcnow()
        except:
            timestamp = datetime.utcnow()

        # Extract symbols from currencies
        symbols = []
        for currency in raw_data.get("currencies", []):
            if isinstance(currency, dict):
                symbols.append(currency.get("code", ""))
            elif isinstance(currency, str):
                symbols.append(currency)

        # Determine sentiment from votes
        sentiment_score = None
        sentiment_label = None
        votes = raw_data.get("votes", {})
        if votes:
            positive = votes.get("positive", 0)
            negative = votes.get("negative", 0)
            total = positive + negative
            if total > 0:
                sentiment_score = (positive - negative) / total
                if sentiment_score > 0.2:
                    sentiment_label = "positive"
                elif sentiment_score < -0.2:
                    sentiment_label = "negative"
                else:
                    sentiment_label = "neutral"

        return {
            "timestamp": timestamp,
            "source_id": self.source_id,
            "article_url": raw_data.get("url"),
            "title": raw_data.get("title"),
            "summary": None,
            "content": None,
            "author": raw_data.get("domain") or raw_data.get("source", {}).get("title"),
            "symbols": [s for s in symbols if s] if symbols else None,
            "categories": ["crypto", raw_data.get("kind", "news")],
            "sentiment_score": sentiment_score,
            "sentiment_label": sentiment_label,
            "image_url": None,
            "metadata_json": {
                "source": "cryptopanic",
                "domain": raw_data.get("domain"),
                "kind": raw_data.get("kind"),
                "votes": votes,
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
