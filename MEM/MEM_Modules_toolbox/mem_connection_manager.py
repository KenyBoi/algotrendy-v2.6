#!/usr/bin/env python3
"""
🌉 MEM CONNECTION MANAGER - Lifecycle management
Handles initialization, monitoring, and graceful shutdown
Provides singleton pattern for application-wide connectivity
"""

import asyncio
import logging
from typing import Optional, Callable, Dict, Any
import sys
from pathlib import Path

# Add current directory to path
sys.path.insert(0, str(Path(__file__).parent))

from singleton_decorator import singleton
from mem_connector import MemConnector, ConnectionMode, ConnectionMetrics
from mem_credentials import get_credentials

logger = logging.getLogger(__name__)


@singleton
class MemConnectionManager:
    """
    Singleton connection manager for MEM
    Manages lifecycle of MemConnector instances
    """
    
    def __init__(self):
        self.connector: Optional[MemConnector] = None
        self.is_initialized = False
        self.event_loop: Optional[asyncio.AbstractEventLoop] = None
        self.monitoring_task = None
        self.health_check_interval = 30  # seconds
        logger.info("🌉 MemConnectionManager initialized")
    
    async def initialize(self) -> bool:
        """
        Initialize connection using credentials
        Must be called before using the manager
        """
        logger.info("🚀 Initializing MemConnectionManager...")
        
        try:
            # Load credentials
            credentials = get_credentials()
            if not credentials.validate_connection_params():
                logger.error("❌ Invalid credentials")
                return False
            
            # Create connector
            config = credentials.to_connector_config()
            self.connector = MemConnector(**config)
            
            # Connect
            if await self.connector.connect():
                self.is_initialized = True
                logger.info("✅ MemConnectionManager initialized successfully")
                
                # Start monitoring
                await self.start_monitoring()
                
                return True
            else:
                logger.error("❌ Failed to connect")
                return False
                
        except Exception as e:
            logger.error(f"❌ Initialization error: {e}")
            return False
    
    async def start_monitoring(self):
        """Start background monitoring task"""
        if self.monitoring_task:
            return  # Already monitoring
        
        logger.info("📊 Starting connection monitoring...")
        self.monitoring_task = asyncio.create_task(self._monitor_health())
    
    async def _monitor_health(self):
        """Monitor connection health periodically"""
        while self.is_initialized:
            try:
                health = await self.connector.health_check()
                if health["status"] == "unhealthy":
                    logger.warning("⚠️  Connection unhealthy, attempting reconnection...")
                    await self.connector.connect()
                
            except Exception as e:
                logger.error(f"❌ Health check error: {e}")
            
            await asyncio.sleep(self.health_check_interval)
    
    def is_connected(self) -> bool:
        """Check if currently connected"""
        return (
            self.is_initialized and
            self.connector and
            self.connector.mode != ConnectionMode.DISCONNECTED
        )
    
    async def send_command(self, data: Dict[str, Any]) -> bool:
        """Send command to MEM"""
        if not self.is_connected():
            logger.error("❌ Not connected")
            return False
        
        return await self.connector.send_message(data)
    
    async def get_status(self) -> Dict[str, Any]:
        """Get current connection status"""
        if not self.connector:
            return {"status": "not_initialized"}
        
        return await self.connector.health_check()
    
    async def get_metrics(self) -> Dict[str, Any]:
        """Get connection metrics"""
        if not self.connector:
            return {}
        
        return await self.connector.get_metrics()
    
    def register_callback(self, event: str, callback: Callable):
        """Register event callback"""
        if self.connector:
            self.connector.register_callback(event, callback)
    
    async def shutdown(self):
        """Graceful shutdown"""
        logger.info("🛑 Shutting down MemConnectionManager...")
        
        # Cancel monitoring
        if self.monitoring_task:
            self.monitoring_task.cancel()
            try:
                await self.monitoring_task
            except asyncio.CancelledError:
                pass
        
        # Disconnect
        if self.connector:
            await self.connector.disconnect()
        
        self.is_initialized = False
        logger.info("✅ Shutdown complete")


def get_connection_manager() -> MemConnectionManager:
    """Get singleton connection manager instance"""
    return MemConnectionManager()


async def demo():
    """Demonstration of connection manager usage"""
    logging.basicConfig(level=logging.INFO)
    
    manager = get_connection_manager()
    
    # Initialize
    if not await manager.initialize():
        logger.error("❌ Failed to initialize")
        return
    
    # Register callbacks
    async def on_message(data):
        logger.info(f"📨 {data}")
    
    manager.register_callback("on_message", on_message)
    
    # Get status
    status = await manager.get_status()
    logger.info(f"📊 Status: {status['status']}")
    
    # Send test command
    await manager.send_command({"type": "ping"})
    
    # Wait
    await asyncio.sleep(5)
    
    # Get metrics
    metrics = await manager.get_metrics()
    logger.info(f"📊 Metrics: {metrics}")
    
    # Shutdown
    await manager.shutdown()


if __name__ == "__main__":
    asyncio.run(demo())
