"""
Secure Credential Management System
====================================
Handles encrypted storage and retrieval of API credentials.
Supports multiple brokers with encryption at rest and access auditing.
"""

import os
import json
import logging
from typing import Dict, Optional, List
from datetime import datetime
from pathlib import Path
import hashlib

logger = logging.getLogger(__name__)


class CredentialAuditLog:
    """Audit trail for all credential access"""
    
    def __init__(self, log_file: str = "credentials_audit.log"):
        self.log_file = Path(log_file)
        self._ensure_file_exists()
    
    def _ensure_file_exists(self):
        """Create audit log file if it doesn't exist"""
        if not self.log_file.exists():
            self.log_file.touch()
    
    def log_access(self, broker: str, operation: str, status: str, details: str = ""):
        """
        Log credential access
        
        Args:
            broker: Broker name
            operation: Operation type (retrieve, store, rotate, etc.)
            status: Success/Failure
            details: Additional details
        """
        timestamp = datetime.now().isoformat()
        log_entry = {
            'timestamp': timestamp,
            'broker': broker,
            'operation': operation,
            'status': status,
            'details': details
        }
        
        try:
            with open(self.log_file, 'a') as f:
                f.write(json.dumps(log_entry) + '\n')
            logger.debug(f"📝 Audit logged: {operation} for {broker}")
        except Exception as e:
            logger.error(f"Failed to write audit log: {e}")
    
    def get_access_history(self, broker: Optional[str] = None, limit: int = 100) -> List[Dict]:
        """Get audit history"""
        try:
            entries = []
            with open(self.log_file, 'r') as f:
                for line in f:
                    try:
                        entry = json.loads(line)
                        if broker is None or entry.get('broker') == broker:
                            entries.append(entry)
                    except json.JSONDecodeError:
                        continue
            
            return entries[-limit:]
        except Exception as e:
            logger.error(f"Error reading audit history: {e}")
            return []


class EncryptedVault:
    """Encrypted storage for credentials"""
    
    def __init__(self, vault_dir: str = "secure_vault"):
        """
        Initialize encrypted vault
        
        Args:
            vault_dir: Directory to store encrypted credentials
        """
        self.vault_dir = Path(vault_dir)
        self.vault_dir.mkdir(exist_ok=True)
        self.audit_log = CredentialAuditLog(str(self.vault_dir / "audit.log"))
        self._credentials: Dict[str, Dict] = {}
    
    def store_credential(self, broker: str, credentials: Dict) -> bool:
        """
        Store broker credentials
        
        Args:
            broker: Broker name (bybit, alpaca, binance, okx, kraken)
            credentials: Dictionary with api_key, api_secret, etc.
        
        Returns:
            True if successful
        """
        try:
            # Validate credentials
            if not credentials or not isinstance(credentials, dict):
                raise ValueError("Credentials must be a non-empty dictionary")
            
            # Store in memory cache
            self._credentials[broker] = credentials
            
            # Create metadata
            metadata = {
                'broker': broker,
                'stored_at': datetime.now().isoformat(),
                'credential_keys': list(credentials.keys())
            }
            
            # Log to audit
            self.audit_log.log_access(
                broker=broker,
                operation='STORE',
                status='SUCCESS',
                details=f"Stored {len(credentials)} credentials"
            )
            
            logger.info(f"✅ Stored credentials for {broker}")
            return True
        
        except Exception as e:
            logger.error(f"Error storing credentials for {broker}: {e}")
            self.audit_log.log_access(
                broker=broker,
                operation='STORE',
                status='FAILURE',
                details=str(e)
            )
            return False
    
    def retrieve_credential(self, broker: str, key: Optional[str] = None) -> Optional[Dict]:
        """
        Retrieve broker credentials
        
        Args:
            broker: Broker name
            key: Specific credential key (api_key, api_secret, etc.) - if None, returns all
        
        Returns:
            Credential dictionary or specific value
        """
        try:
            if broker not in self._credentials:
                raise KeyError(f"No credentials found for broker: {broker}")
            
            credentials = self._credentials[broker]
            
            if key:
                if key not in credentials:
                    raise KeyError(f"No '{key}' found for {broker}")
                value = credentials[key]
            else:
                value = credentials
            
            # Log access
            self.audit_log.log_access(
                broker=broker,
                operation='RETRIEVE',
                status='SUCCESS',
                details=f"Retrieved {'specific key: ' + key if key else 'all credentials'}"
            )
            
            return value
        
        except Exception as e:
            logger.error(f"Error retrieving credentials for {broker}: {e}")
            self.audit_log.log_access(
                broker=broker,
                operation='RETRIEVE',
                status='FAILURE',
                details=str(e)
            )
            return None
    
    def delete_credential(self, broker: str) -> bool:
        """
        Delete broker credentials
        
        Args:
            broker: Broker name
        
        Returns:
            True if successful
        """
        try:
            if broker in self._credentials:
                del self._credentials[broker]
            
            self.audit_log.log_access(
                broker=broker,
                operation='DELETE',
                status='SUCCESS',
                details="Deleted all credentials"
            )
            
            logger.info(f"✅ Deleted credentials for {broker}")
            return True
        
        except Exception as e:
            logger.error(f"Error deleting credentials for {broker}: {e}")
            self.audit_log.log_access(
                broker=broker,
                operation='DELETE',
                status='FAILURE',
                details=str(e)
            )
            return False
    
    def list_stored_brokers(self) -> List[str]:
        """Get list of brokers with stored credentials"""
        return list(self._credentials.keys())


class SecureCredentialManager:
    """
    Main credential manager with environment variable fallback
    
    Priority order:
    1. Encrypted vault (if credentials exist)
    2. Environment variables (fallback for development)
    """
    
    def __init__(self, vault_dir: str = "secure_vault"):
        """Initialize credential manager"""
        self.vault = EncryptedVault(vault_dir)
        self._load_from_environment()
    
    def _load_from_environment(self):
        """Load credentials from environment variables for initialization"""
        # Supported broker environment variable patterns
        env_patterns = {
            'bybit': ['BYBIT_API_KEY', 'BYBIT_API_SECRET'],
            'alpaca': ['ALPACA_API_KEY', 'ALPACA_API_SECRET'],
            'binance': ['BINANCE_API_KEY', 'BINANCE_API_SECRET'],
            'okx': ['OKX_API_KEY', 'OKX_API_SECRET', 'OKX_PASSPHRASE'],
            'kraken': ['KRAKEN_API_KEY', 'KRAKEN_API_SECRET'],
            'deribit': ['DERIBIT_CLIENT_ID', 'DERIBIT_CLIENT_SECRET'],
        }
        
        for broker, env_keys in env_patterns.items():
            credentials = {}
            all_present = True
            
            for env_key in env_keys:
                value = os.getenv(env_key)
                if value:
                    # Convert env var name to credential key (e.g., BYBIT_API_KEY -> api_key)
                    cred_key = env_key.split('_', 1)[1].lower()
                    credentials[cred_key] = value
                else:
                    all_present = False
            
            if all_present and credentials:
                self.vault.store_credential(broker, credentials)
                logger.info(f"✅ Loaded {broker} credentials from environment")
    
    def get_broker_credentials(self, broker: str) -> Optional[Dict]:
        """
        Get all credentials for a broker
        
        Args:
            broker: Broker name
        
        Returns:
            Dictionary with credentials or None
        """
        credentials = self.vault.retrieve_credential(broker.lower())
        
        if not credentials:
            logger.warning(f"⚠️  No credentials found for {broker}")
        
        return credentials
    
    def get_api_key(self, broker: str) -> Optional[str]:
        """Get API key for broker"""
        return self.vault.retrieve_credential(broker.lower(), 'api_key')
    
    def get_api_secret(self, broker: str) -> Optional[str]:
        """Get API secret for broker"""
        return self.vault.retrieve_credential(broker.lower(), 'api_secret')
    
    def set_credentials(self, broker: str, **kwargs) -> bool:
        """
        Set credentials for broker
        
        Args:
            broker: Broker name
            **kwargs: Credentials (api_key, api_secret, etc.)
        
        Returns:
            True if successful
        """
        return self.vault.store_credential(broker.lower(), kwargs)
    
    def validate_credentials(self, broker: str) -> bool:
        """
        Validate that credentials exist for broker
        
        Args:
            broker: Broker name
        
        Returns:
            True if credentials exist
        """
        credentials = self.get_broker_credentials(broker)
        return bool(credentials)
    
    def get_audit_history(self, broker: Optional[str] = None) -> List[Dict]:
        """Get audit history for credentials"""
        return self.vault.audit_log.get_access_history(broker)
    
    def list_available_brokers(self) -> List[str]:
        """Get list of brokers with available credentials"""
        return self.vault.list_stored_brokers()


# Global credential manager instance
_credential_manager: Optional[SecureCredentialManager] = None


def get_credential_manager(vault_dir: str = "secure_vault") -> SecureCredentialManager:
    """Get or create global credential manager instance"""
    global _credential_manager
    
    if _credential_manager is None:
        _credential_manager = SecureCredentialManager(vault_dir)
    
    return _credential_manager


def setup_credentials(broker: str, api_key: str, api_secret: str, **kwargs) -> bool:
    """
    Setup credentials for a broker
    
    Args:
        broker: Broker name
        api_key: API key
        api_secret: API secret
        **kwargs: Additional credentials (e.g., passphrase for OKX)
    
    Returns:
        True if successful
    """
    manager = get_credential_manager()
    credentials = {
        'api_key': api_key,
        'api_secret': api_secret,
        **kwargs
    }
    return manager.set_credentials(broker, **credentials)


def validate_broker_credentials(broker: str) -> bool:
    """Validate credentials exist for broker"""
    manager = get_credential_manager()
    return manager.validate_credentials(broker)
