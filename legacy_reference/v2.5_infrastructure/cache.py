"""
Redis caching utility for AlgoTrendy API
Provides caching decorators and utilities for FastAPI endpoints

COMPLETED: Redis async connection manager
COMPLETED: Caching decorator with TTL support
COMPLETED: Pattern-based cache invalidation
COMPLETED: orjson for fast serialization
TODO: Add cache warming on startup
TODO: Implement cache key versioning
TODO: Add cache statistics endpoint
OPTIMIZE: Consider multi-layer caching (Redis + in-memory)
"""

import redis.asyncio as redis
from typing import Optional, Callable, Any
from functools import wraps
import orjson
import logging
from datetime import timedelta

logger = logging.getLogger(__name__)

class CacheManager:
    """Async Redis cache manager"""

    def __init__(self, redis_url: str = "redis://localhost:6379/0"):
        self.redis_url = redis_url
        self._client: Optional[redis.Redis] = None

    async def connect(self):
        """Connect to Redis"""
        try:
            self._client = await redis.from_url(
                self.redis_url,
                encoding="utf-8",
                decode_responses=False,
                max_connections=50
            )
            await self._client.ping()
            logger.info("✅ Redis connected successfully")
        except Exception as e:
            logger.warning(f"⚠️  Redis connection failed: {e}. Caching disabled.")
            self._client = None

    async def disconnect(self):
        """Disconnect from Redis"""
        if self._client:
            await self._client.close()
            logger.info("Redis disconnected")

    @property
    def is_connected(self) -> bool:
        """Check if Redis is connected"""
        return self._client is not None

    async def get(self, key: str) -> Optional[Any]:
        """Get value from cache"""
        if not self.is_connected:
            return None

        try:
            data = await self._client.get(key)
            if data:
                return orjson.loads(data)
            return None
        except Exception as e:
            logger.error(f"Cache get error: {e}")
            return None

    async def set(self, key: str, value: Any, ttl: int = 300):
        """Set value in cache with TTL (seconds)"""
        if not self.is_connected:
            return False

        try:
            data = orjson.dumps(value)
            await self._client.setex(key, ttl, data)
            return True
        except Exception as e:
            logger.error(f"Cache set error: {e}")
            return False

    async def delete(self, key: str):
        """Delete key from cache"""
        if not self.is_connected:
            return False

        try:
            await self._client.delete(key)
            return True
        except Exception as e:
            logger.error(f"Cache delete error: {e}")
            return False

    async def invalidate_pattern(self, pattern: str):
        """Invalidate all keys matching pattern"""
        if not self.is_connected:
            return 0

        try:
            keys = []
            async for key in self._client.scan_iter(match=pattern):
                keys.append(key)

            if keys:
                return await self._client.delete(*keys)
            return 0
        except Exception as e:
            logger.error(f"Cache invalidation error: {e}")
            return 0


# Global cache instance
cache_manager = CacheManager()


def cached(ttl: int = 300, key_prefix: str = ""):
    """
    Caching decorator for FastAPI endpoints

    Args:
        ttl: Time to live in seconds (default: 300)
        key_prefix: Prefix for cache key

    Example:
        @app.get("/api/portfolio")
        @cached(ttl=5, key_prefix="portfolio")
        async def get_portfolio():
            return {"data": "expensive query"}
    """
    def decorator(func: Callable) -> Callable:
        @wraps(func)
        async def wrapper(*args, **kwargs):
            # Generate cache key from function name and args
            cache_key = f"{key_prefix}:{func.__name__}"

            # Add kwargs to cache key if present
            if kwargs:
                sorted_kwargs = sorted(kwargs.items())
                key_suffix = ":".join(f"{k}={v}" for k, v in sorted_kwargs)
                cache_key = f"{cache_key}:{key_suffix}"

            # Try to get from cache
            cached_data = await cache_manager.get(cache_key)
            if cached_data is not None:
                logger.debug(f"Cache hit: {cache_key}")
                return cached_data

            # Cache miss - call function
            logger.debug(f"Cache miss: {cache_key}")
            result = await func(*args, **kwargs)

            # Store in cache
            await cache_manager.set(cache_key, result, ttl=ttl)

            return result

        return wrapper
    return decorator


async def invalidate_cache(patterns: list[str]):
    """
    Invalidate cache for multiple patterns

    Args:
        patterns: List of cache key patterns to invalidate

    Example:
        await invalidate_cache(["portfolio:*", "positions:*"])
    """
    total_deleted = 0
    for pattern in patterns:
        deleted = await cache_manager.invalidate_pattern(pattern)
        total_deleted += deleted
        logger.info(f"Invalidated {deleted} keys for pattern: {pattern}")

    return total_deleted
