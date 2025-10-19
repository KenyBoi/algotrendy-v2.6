"""
Yahoo Finance news channel
Fetches general financial and crypto news via RSS
"""

from typing import Dict, Any, List
from datetime import datetime
import aiohttp
import xml.etree.ElementTree as ET
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..base import DataChannel, DataType
from ..exceptions import RateLimitError


class YahooFinanceChannel(DataChannel):
    """Yahoo Finance news channel (RSS)."""

    def __init__(self, source_id: int, config: Dict[str, Any] = None):
        super().__init__(
            source_id=source_id,
            source_name="Yahoo Finance",
            data_type=DataType.NEWS,
            config=config or {}
        )
        self.rss_urls = {
            "crypto": "https://finance.yahoo.com/rss/topic/crypto",
            "markets": "https://finance.yahoo.com/rss/topstories",
        }

    async def _connect(self) -> bool:
        """Test connection to Yahoo Finance RSS."""
        try:
            async with aiohttp.ClientSession() as session:
                async with session.get(
                    self.rss_urls["crypto"],
                    timeout=aiohttp.ClientTimeout(total=5)
                ) as response:
                    return response.status == 200
        except Exception as e:
            self.logger.error(f"Connection test failed: {e}")
            return False

    async def fetch_data(self, categories: List[str] = None) -> List[Dict[str, Any]]:
        """
        Fetch news from Yahoo Finance RSS feeds.

        Args:
            categories: List of categories to fetch (default: crypto, markets)
        """
        if not categories:
            categories = ["crypto", "markets"]

        all_articles = []

        async with aiohttp.ClientSession() as session:
            for category in categories:
                if category not in self.rss_urls:
                    continue

                try:
                    async with session.get(
                        self.rss_urls[category],
                        timeout=aiohttp.ClientTimeout(total=10)
                    ) as response:
                        if response.status == 429:
                            raise RateLimitError("Yahoo Finance rate limit exceeded", retry_after=60)

                        if response.status != 200:
                            self.logger.error(f"HTTP {response.status} for {category}")
                            continue

                        xml_content = await response.text()

                        # Parse RSS XML
                        root = ET.fromstring(xml_content)

                        for item in root.findall(".//item"):
                            title = item.find("title")
                            link = item.find("link")
                            pub_date = item.find("pubDate")
                            description = item.find("description")

                            if title is not None and link is not None:
                                all_articles.append({
                                    "url": link.text,
                                    "title": title.text,
                                    "description": description.text if description is not None else None,
                                    "published_date": pub_date.text if pub_date is not None else None,
                                    "category": category,
                                })

                except RateLimitError:
                    raise
                except Exception as e:
                    self.logger.error(f"Error fetching {category}: {e}")
                    continue

        self.logger.info(f"Fetched {len(all_articles)} articles from Yahoo Finance")
        return all_articles

    async def validate_data(self, data: Dict[str, Any]) -> bool:
        """Validate news article data."""
        required_fields = ["title", "url"]
        return all(field in data and data[field] for field in required_fields)

    async def transform_data(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """Transform to standardized format."""
        # Parse published date (RFC 2822 format)
        timestamp = datetime.utcnow()
        if raw_data.get("published_date"):
            try:
                from email.utils import parsedate_to_datetime
                timestamp = parsedate_to_datetime(raw_data["published_date"])
            except:
                pass

        return {
            "timestamp": timestamp,
            "source_id": self.source_id,
            "article_url": raw_data.get("url"),
            "title": raw_data.get("title"),
            "summary": raw_data.get("description", "")[:500] if raw_data.get("description") else None,
            "content": raw_data.get("description"),
            "author": "Yahoo Finance",
            "symbols": None,
            "categories": ["finance", raw_data.get("category", "general")],
            "sentiment_score": None,
            "sentiment_label": None,
            "image_url": None,
            "metadata_json": {
                "source": "yahoo_finance",
                "category": raw_data.get("category"),
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
