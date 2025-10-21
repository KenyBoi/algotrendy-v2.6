"""
Configuration management for AlgoTrendy v2.5
Loads settings from environment variables and .env file
"""

from pydantic_settings import BaseSettings
from typing import Optional
import os


class Settings(BaseSettings):
    """Application settings"""

    # Polygon.io Configuration
    polygon_api_key: Optional[str] = None
    polygon_cluster: str = "crypto"  # stocks, crypto, forex
    polygon_enabled: bool = False

    # API Server Configuration
    api_host: str = "0.0.0.0"
    api_port: int = 8000
    api_workers: int = 4
    uvloop_enabled: bool = True

    # Database Configuration
    database_url: Optional[str] = None
    db_host: str = "localhost"
    db_port: int = 5432
    db_name: str = "algotrendy_v25"
    db_user: str = "algotrendy"
    db_password: str = "algotrendy"

    # Redis Configuration
    redis_url: str = "redis://localhost:6379/0"
    redis_max_connections: int = 50
    cache_default_ttl: int = 300

    # Security
    secret_key: str = "your-secret-key-here-change-in-production"
    jwt_algorithm: str = "HS256"
    jwt_expiration_minutes: int = 60

    # Logging & Monitoring
    log_level: str = "INFO"
    enable_prometheus: bool = True
    enable_debug: bool = False

    # CORS Configuration
    cors_origins: str = "http://localhost:3000,http://localhost:8000"
    cors_allow_credentials: bool = True

    # Performance Settings
    connection_pool_size: int = 20
    connection_max_overflow: int = 10
    query_timeout: int = 60
    request_timeout: int = 30

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"
        case_sensitive = False
        extra = "ignore"  # Ignore extra fields from .env


# Global settings instance
settings = Settings()


def get_settings() -> Settings:
    """Get application settings"""
    return settings


def is_polygon_enabled() -> bool:
    """Check if Polygon.io integration is enabled"""
    return settings.polygon_enabled and settings.polygon_api_key is not None


def get_polygon_api_key() -> Optional[str]:
    """Get Polygon.io API key"""
    return settings.polygon_api_key
