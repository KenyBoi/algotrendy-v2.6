"""
Authentication module for AlgoTrendy API
Handles user authentication, password hashing, and token management

COMPLETED: Bcrypt password hashing implementation
COMPLETED: JWT token generation and validation
COMPLETED: Secure password verification
SECURITY: Move SECRET_KEY to environment variable (CRITICAL)
TODO: Implement refresh token mechanism
TODO: Add password reset functionality
TODO: Implement rate limiting for login attempts
TODO: Add OAuth2 support for external providers
TODO: Move user storage to database
"""

from datetime import datetime, timedelta
from typing import Optional
from passlib.context import CryptContext
from jose import JWTError, jwt
from pydantic import BaseModel
import logging

logger = logging.getLogger(__name__)

# Password hashing context
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

# JWT Configuration
SECRET_KEY = "your-secret-key-change-in-production"  # TODO: Move to environment variable
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 30


class User(BaseModel):
    """User model"""
    id: str
    email: str
    name: str
    role: str
    avatar: Optional[str] = None


class TokenData(BaseModel):
    """Token data model"""
    email: Optional[str] = None


class AuthService:
    """Authentication service for user management"""

    def __init__(self):
        # In-memory user store (replace with database in production)
        # Passwords are now hashed
        self.users = {
            "admin@algotrendy.com": {
                "password_hash": pwd_context.hash("admin123"),
                "user": User(
                    id="admin_001",
                    email="admin@algotrendy.com",
                    name="Admin User",
                    role="admin",
                    avatar=None
                )
            },
            "demo@algotrendy.com": {
                "password_hash": pwd_context.hash("demo123"),
                "user": User(
                    id="demo_001",
                    email="demo@algotrendy.com",
                    name="Demo User",
                    role="trader",
                    avatar=None
                )
            },
            "trader@algotrendy.com": {
                "password_hash": pwd_context.hash("trader123"),
                "user": User(
                    id="trader_001",
                    email="trader@algotrendy.com",
                    name="Pro Trader",
                    role="trader",
                    avatar=None
                )
            },
            "test@algotrendy.com": {
                "password_hash": pwd_context.hash("test123"),
                "user": User(
                    id="test_001",
                    email="test@algotrendy.com",
                    name="Test User",
                    role="user",
                    avatar=None
                )
            }
        }

    def verify_password(self, plain_password: str, hashed_password: str) -> bool:
        """Verify password against hash"""
        return pwd_context.verify(plain_password, hashed_password)

    def get_password_hash(self, password: str) -> str:
        """Hash a password"""
        return pwd_context.hash(password)

    def authenticate_user(self, email: str, password: str) -> Optional[User]:
        """
        Authenticate a user by email and password

        Args:
            email: User email
            password: Plain text password

        Returns:
            User object if authentication successful, None otherwise
        """
        email = email.lower().strip()
        password = password.strip()

        if email not in self.users:
            logger.warning(f"Login attempt for non-existent user: {email}")
            return None

        user_data = self.users[email]
        if not self.verify_password(password, user_data["password_hash"]):
            logger.warning(f"Invalid password for user: {email}")
            return None

        logger.info(f"Successful login: {email}")
        return user_data["user"]

    def create_access_token(self, data: dict, expires_delta: Optional[timedelta] = None) -> str:
        """
        Create JWT access token

        Args:
            data: Data to encode in token
            expires_delta: Token expiration time

        Returns:
            Encoded JWT token
        """
        to_encode = data.copy()

        if expires_delta:
            expire = datetime.utcnow() + expires_delta
        else:
            expire = datetime.utcnow() + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)

        to_encode.update({"exp": expire})
        encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)

        return encoded_jwt

    def verify_token(self, token: str) -> Optional[str]:
        """
        Verify JWT token and extract email

        Args:
            token: JWT token

        Returns:
            User email if token is valid, None otherwise
        """
        try:
            payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
            email: str = payload.get("sub")
            if email is None:
                return None
            return email
        except JWTError as e:
            logger.error(f"Token verification failed: {e}")
            return None

    def get_user_by_email(self, email: str) -> Optional[User]:
        """
        Get user by email

        Args:
            email: User email

        Returns:
            User object if found, None otherwise
        """
        email = email.lower().strip()
        if email in self.users:
            return self.users[email]["user"]
        return None


# Global auth service instance
auth_service = AuthService()
