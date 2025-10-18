#!/usr/bin/env python3
"""
AlgoTrendy v2.6 - Secure Secret Generation Script
Generates cryptographically secure secrets for production use
"""

import secrets
import string
import base64
import hashlib
from datetime import datetime


def generate_jwt_secret(length: int = 64) -> str:
    """
    Generate a cryptographically secure JWT secret key.

    Args:
        length: Length of the secret (default 64 bytes = 512 bits)

    Returns:
        Base64-encoded secret key
    """
    random_bytes = secrets.token_bytes(length)
    return base64.urlsafe_b64encode(random_bytes).decode('utf-8').rstrip('=')


def generate_api_key(prefix: str = "atk", length: int = 32) -> str:
    """
    Generate a cryptographically secure API key with prefix.

    Args:
        prefix: Key prefix for identification (default "atk" = AlgoTrendy Key)
        length: Length of random portion (default 32 chars)

    Returns:
        API key in format: prefix_randomstring
    """
    alphabet = string.ascii_letters + string.digits
    random_part = ''.join(secrets.choice(alphabet) for _ in range(length))
    return f"{prefix}_{random_part}"


def generate_encryption_key(length: int = 32) -> str:
    """
    Generate a cryptographically secure encryption key for AES-256.

    Args:
        length: Key length in bytes (32 bytes = 256 bits for AES-256)

    Returns:
        Hex-encoded encryption key
    """
    return secrets.token_hex(length)


def generate_database_password(length: int = 32) -> str:
    """
    Generate a strong database password.

    Args:
        length: Password length (default 32 characters)

    Returns:
        Secure password with mixed character types
    """
    alphabet = string.ascii_letters + string.digits + "!@#$%^&*-_=+"
    password = ''.join(secrets.choice(alphabet) for _ in range(length))

    # Ensure at least one of each character type
    if not any(c.islower() for c in password):
        password = password[:-1] + secrets.choice(string.ascii_lowercase)
    if not any(c.isupper() for c in password):
        password = password[:-1] + secrets.choice(string.ascii_uppercase)
    if not any(c.isdigit() for c in password):
        password = password[:-1] + secrets.choice(string.digits)

    return password


def generate_session_token(length: int = 48) -> str:
    """
    Generate a secure session token.

    Args:
        length: Token length in bytes

    Returns:
        URL-safe base64-encoded token
    """
    return secrets.token_urlsafe(length)


def main():
    print("=" * 80)
    print("AlgoTrendy v2.6 - Secure Secret Generator")
    print(f"Generated: {datetime.utcnow().isoformat()}Z")
    print("=" * 80)
    print("\n⚠️  CRITICAL SECURITY WARNING:")
    print("   - Store these secrets securely (use Azure Key Vault or AWS Secrets Manager)")
    print("   - Never commit these secrets to version control")
    print("   - Rotate secrets regularly (every 90 days minimum)")
    print("   - Use different secrets for dev/staging/production")
    print("=" * 80)
    print()

    # JWT Secret
    print("# ============================================")
    print("# JWT Authentication")
    print("# ============================================")
    jwt_secret = generate_jwt_secret(64)
    print(f"JWT_SECRET_KEY={jwt_secret}")
    print(f"# Length: {len(jwt_secret)} characters")
    print(f"# Entropy: ~512 bits")
    print()

    # Encryption Key
    print("# ============================================")
    print("# Data Encryption (AES-256)")
    print("# ============================================")
    encryption_key = generate_encryption_key(32)
    print(f"ENCRYPTION_KEY={encryption_key}")
    print(f"# Length: {len(encryption_key)} characters (32 bytes hex)")
    print(f"# Entropy: 256 bits")
    print()

    # Database Password
    print("# ============================================")
    print("# Database Credentials")
    print("# ============================================")
    db_password = generate_database_password(32)
    print(f"DB_PASSWORD={db_password}")
    print(f"# Length: {len(db_password)} characters")
    print(f"# Complexity: Mixed alphanumeric + special chars")
    print()

    # Redis Password
    print("# ============================================")
    print("# Redis Authentication")
    print("# ============================================")
    redis_password = generate_database_password(32)
    print(f"REDIS_PASSWORD={redis_password}")
    print()

    # API Keys for different services
    print("# ============================================")
    print("# Internal API Keys")
    print("# ============================================")
    admin_api_key = generate_api_key("atk_admin", 48)
    print(f"ADMIN_API_KEY={admin_api_key}")

    service_api_key = generate_api_key("atk_service", 48)
    print(f"SERVICE_API_KEY={service_api_key}")

    webhook_secret = generate_api_key("atk_webhook", 48)
    print(f"WEBHOOK_SECRET={webhook_secret}")
    print()

    # Session Secrets
    print("# ============================================")
    print("# Session Management")
    print("# ============================================")
    session_secret = generate_session_token(48)
    print(f"SESSION_SECRET={session_secret}")
    print()

    # HMAC Signing Key
    print("# ============================================")
    print("# Request Signing (HMAC)")
    print("# ============================================")
    hmac_key = generate_encryption_key(32)
    print(f"HMAC_SIGNING_KEY={hmac_key}")
    print()

    print("=" * 80)
    print("✅ Secret generation complete!")
    print()
    print("NEXT STEPS:")
    print("1. Copy the secrets above to your .env file")
    print("2. Delete this output from your terminal history")
    print("3. Store secrets in Azure Key Vault or AWS Secrets Manager")
    print("4. Configure production servers to use secrets manager")
    print("5. Set calendar reminder to rotate secrets in 90 days")
    print("=" * 80)
    print()
    print("SECURITY CHECKLIST:")
    print("☐ Secrets copied to secure location")
    print("☐ .env file permissions set to 600 (owner read/write only)")
    print("☐ .env added to .gitignore")
    print("☐ Old secrets invalidated/rotated")
    print("☐ Production secrets different from development")
    print("☐ Secrets backed up securely (encrypted)")
    print("=" * 80)


if __name__ == "__main__":
    main()
