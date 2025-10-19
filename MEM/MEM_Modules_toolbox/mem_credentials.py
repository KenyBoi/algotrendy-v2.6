#!/usr/bin/env python3
"""
🔐 MEM CREDENTIALS - Secure credential management
Handles API keys, tokens, and connection parameters
Supports environment variables, .env files, and encrypted storage
"""

import os
import json
from typing import Optional, Dict, Any
from pathlib import Path
import logging

logger = logging.getLogger(__name__)


class MemCredentials:
    """Secure credential manager for MEM API"""
    
    REQUIRED_FIELDS = {
        "mem_ws_url": "WebSocket URL",
        "mem_rest_url": "REST API URL",
    }
    
    OPTIONAL_FIELDS = {
        "mem_api_key": "API Key",
        "mem_api_secret": "API Secret",
        "mem_timeout": "Connection timeout (seconds)",
        "mem_max_reconnect": "Max reconnect attempts",
    }
    
    def __init__(self, env_file: Optional[str] = None):
        """
        Initialize credentials manager
        
        Args:
            env_file: Path to .env file (default: .env in current directory)
        """
        self.env_file = env_file or ".env"
        self.credentials = {}
        self._load_credentials()
    
    def _load_credentials(self):
        """Load credentials from environment or .env file"""
        logger.info("🔐 Loading credentials...")
        
        # First, try to load from .env file
        if os.path.exists(self.env_file):
            self._load_env_file()
        
        # Override with environment variables
        self._load_env_vars()
        
        # Validate required fields
        self._validate()
    
    def _load_env_file(self):
        """Load credentials from .env file"""
        try:
            with open(self.env_file, 'r') as f:
                for line in f:
                    line = line.strip()
                    if line and not line.startswith('#') and '=' in line:
                        key, value = line.split('=', 1)
                        key = key.strip()
                        value = value.strip().strip('"\'')
                        
                        if key.startswith('MEM_'):
                            self.credentials[key.lower()] = value
            
            logger.info(f"✅ Loaded .env file: {self.env_file}")
        except Exception as e:
            logger.warning(f"⚠️  Error reading .env file: {e}")
    
    def _load_env_vars(self):
        """Load credentials from environment variables"""
        for key in os.environ:
            if key.startswith('MEM_'):
                self.credentials[key.lower()] = os.environ[key]
    
    def _validate(self):
        """Validate required fields are present"""
        missing = []
        for field, description in self.REQUIRED_FIELDS.items():
            if field not in self.credentials:
                missing.append(f"{field} ({description})")
        
        if missing:
            logger.warning(f"⚠️  Missing credentials: {', '.join(missing)}")
            logger.info("ℹ️  Using defaults: ws://127.0.0.1:8765 and http://127.0.0.1:5000")
            
            # Set defaults
            if "mem_ws_url" not in self.credentials:
                self.credentials["mem_ws_url"] = "ws://127.0.0.1:8765"
            if "mem_rest_url" not in self.credentials:
                self.credentials["mem_rest_url"] = "http://127.0.0.1:5000"
    
    def get(self, key: str, default: Any = None) -> Any:
        """Get credential value"""
        return self.credentials.get(key, default)
    
    def get_all(self) -> Dict[str, Any]:
        """Get all credentials (masked for security)"""
        result = {}
        for key, value in self.credentials.items():
            if "key" in key or "secret" in key or "password" in key:
                result[key] = "***" + str(value)[-4:] if value else None
            else:
                result[key] = value
        return result
    
    def to_connector_config(self) -> Dict[str, Any]:
        """Convert to MemConnector config dict"""
        return {
            "ws_url": self.get("mem_ws_url", "ws://127.0.0.1:8765"),
            "rest_url": self.get("mem_rest_url", "http://127.0.0.1:5000"),
            "api_key": self.get("mem_api_key"),
            "api_secret": self.get("mem_api_secret"),
            "max_reconnect_attempts": int(self.get("mem_max_reconnect", 10)),
            "reconnect_interval": int(self.get("mem_reconnect_interval", 5)),
        }
    
    def validate_connection_params(self) -> bool:
        """Validate connection parameters"""
        ws_url = self.get("mem_ws_url")
        rest_url = self.get("mem_rest_url")
        
        if not ws_url or not rest_url:
            logger.error("❌ WebSocket and REST URLs are required")
            return False
        
        if not ws_url.startswith("ws://") and not ws_url.startswith("wss://"):
            logger.error(f"❌ Invalid WebSocket URL: {ws_url}")
            return False
        
        if not rest_url.startswith("http://") and not rest_url.startswith("https://"):
            logger.error(f"❌ Invalid REST URL: {rest_url}")
            return False
        
        logger.info("✅ Connection parameters valid")
        return True
    
    def create_env_file(self, output_path: str = ".env.example"):
        """Create an example .env file"""
        example = """# MEM Connector Configuration

# WebSocket connection (required)
MEM_WS_URL=ws://127.0.0.1:8765

# REST API connection (required)
MEM_REST_URL=http://127.0.0.1:5000

# API Authentication (optional)
# MEM_API_KEY=your_api_key_here
# MEM_API_SECRET=your_api_secret_here

# Connection settings (optional)
MEM_TIMEOUT=30
MEM_MAX_RECONNECT=10
MEM_RECONNECT_INTERVAL=5
"""
        
        try:
            with open(output_path, 'w') as f:
                f.write(example)
            logger.info(f"✅ Created example .env file: {output_path}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to create .env file: {e}")
            return False


# Global credentials instance
_credentials = None


def get_credentials() -> MemCredentials:
    """Get global credentials instance (lazy initialization)"""
    global _credentials
    if _credentials is None:
        _credentials = MemCredentials()
    return _credentials


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    
    # Initialize
    creds = MemCredentials()
    
    # Show credentials (masked)
    print("\n📋 Loaded Credentials:")
    print(json.dumps(creds.get_all(), indent=2))
    
    # Validate
    print(f"\n✅ Validation: {creds.validate_connection_params()}")
    
    # Show connector config
    print("\n🔗 Connector Config:")
    print(json.dumps(creds.to_connector_config(), indent=2))
    
    # Create example file
    creds.create_env_file()
