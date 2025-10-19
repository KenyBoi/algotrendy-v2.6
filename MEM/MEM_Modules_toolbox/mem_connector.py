#!/usr/bin/env python3
"""
🔌 MEM CONNECTOR - Universal API Bridge
Unified WebSocket + REST abstraction for MemGPT connectivity
Handles failover, reconnection, metrics, and error handling
"""

import asyncio
import json
import time
from typing import Optional, Dict, Any, Callable, List
from dataclasses import dataclass, asdict
from enum import Enum
import logging
from datetime import datetime, timedelta
import websockets
import aiohttp
from urllib.parse import urljoin

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class ConnectionMode(Enum):
    """Connection mode enum"""
    WEBSOCKET = "websocket"
    REST = "rest"
    DISCONNECTED = "disconnected"


@dataclass
class ConnectionMetrics:
    """Metrics for connection health"""
    mode: ConnectionMode
    connected: bool
    last_message_ts: float
    total_messages: int
    total_errors: int
    latency_ms: float
    reconnect_attempts: int
    uptime_seconds: float
    
    def to_dict(self):
        return {**asdict(self), "mode": self.mode.value}


class CircuitBreaker:
    """Circuit breaker for rate limiting and failure detection"""
    
    def __init__(self, failure_threshold=5, reset_timeout=60):
        self.failure_threshold = failure_threshold
        self.reset_timeout = reset_timeout
        self.failures = 0
        self.last_failure_time = None
        self.state = "closed"  # closed, open, half-open
    
    def record_success(self):
        self.failures = 0
        self.state = "closed"
    
    def record_failure(self):
        self.failures += 1
        self.last_failure_time = time.time()
        if self.failures >= self.failure_threshold:
            self.state = "open"
    
    def allow_request(self) -> bool:
        if self.state == "closed":
            return True
        
        if self.state == "open":
            # Check if reset timeout has passed
            if time.time() - self.last_failure_time > self.reset_timeout:
                self.state = "half-open"
                return True
            return False
        
        # half-open state: allow single test request
        return True
    
    def get_state(self) -> str:
        return self.state


class MemConnector:
    """
    Universal MEM API connector with WebSocket + REST support
    Handles automatic failover, reconnection, and metrics
    """
    
    def __init__(
        self,
        ws_url: str = "ws://127.0.0.1:8765",
        rest_url: str = "http://127.0.0.1:5000",
        api_key: Optional[str] = None,
        api_secret: Optional[str] = None,
        max_reconnect_attempts: int = 10,
        reconnect_interval: int = 5,
    ):
        self.ws_url = ws_url
        self.rest_url = rest_url
        self.api_key = api_key
        self.api_secret = api_secret
        self.max_reconnect_attempts = max_reconnect_attempts
        self.reconnect_interval = reconnect_interval
        
        # Connection state
        self.mode = ConnectionMode.DISCONNECTED
        self.ws_connection = None
        self.session = None
        
        # Metrics
        self.metrics = ConnectionMetrics(
            mode=ConnectionMode.DISCONNECTED,
            connected=False,
            last_message_ts=time.time(),
            total_messages=0,
            total_errors=0,
            latency_ms=0.0,
            reconnect_attempts=0,
            uptime_seconds=0.0
        )
        self.start_time = time.time()
        
        # Circuit breaker
        self.circuit_breaker = CircuitBreaker()
        
        # Event callbacks
        self.callbacks: Dict[str, List[Callable]] = {
            "on_connect": [],
            "on_disconnect": [],
            "on_message": [],
            "on_error": [],
        }
        
        # Async tasks
        self.tasks = []
        
        logger.info(f"🔌 MemConnector initialized: {ws_url} | {rest_url}")
    
    def register_callback(self, event: str, callback: Callable):
        """Register callback for events"""
        if event in self.callbacks:
            self.callbacks[event].append(callback)
    
    async def _emit_event(self, event: str, *args, **kwargs):
        """Emit event to all registered callbacks"""
        for callback in self.callbacks.get(event, []):
            try:
                if asyncio.iscoroutinefunction(callback):
                    await callback(*args, **kwargs)
                else:
                    callback(*args, **kwargs)
            except Exception as e:
                logger.error(f"❌ Callback error for {event}: {e}")
    
    async def connect(self) -> bool:
        """Connect to MEM (WebSocket preferred, REST fallback)"""
        logger.info("🔗 Attempting to connect...")
        
        # Try WebSocket first
        if await self._connect_websocket():
            return True
        
        # Fallback to REST
        logger.warning("⚠️  WebSocket failed, falling back to REST")
        return await self._connect_rest()
    
    async def _connect_websocket(self) -> bool:
        """Connect via WebSocket"""
        try:
            if not self.circuit_breaker.allow_request():
                logger.warning("⚠️  Circuit breaker open, skipping WebSocket")
                return False
            
            logger.info(f"🔌 Connecting to WebSocket: {self.ws_url}")
            
            self.ws_connection = await asyncio.wait_for(
                websockets.connect(self.ws_url),
                timeout=10
            )
            
            self.mode = ConnectionMode.WEBSOCKET
            self.circuit_breaker.record_success()
            await self._emit_event("on_connect", ConnectionMode.WEBSOCKET)
            
            # Start message listener
            self.tasks.append(asyncio.create_task(self._ws_listener()))
            
            logger.info("✅ WebSocket connected")
            return True
            
        except Exception as e:
            logger.error(f"❌ WebSocket connection failed: {e}")
            self.circuit_breaker.record_failure()
            if self.ws_connection:
                await self.ws_connection.close()
            self.ws_connection = None
            return False
    
    async def _connect_rest(self) -> bool:
        """Connect via REST (polling)"""
        try:
            logger.info(f"🔌 Connecting to REST: {self.rest_url}")
            
            # Test REST connectivity
            async with aiohttp.ClientSession() as session:
                async with session.get(f"{self.rest_url}/health") as resp:
                    if resp.status == 200:
                        self.session = aiohttp.ClientSession()
                        self.mode = ConnectionMode.REST
                        await self._emit_event("on_connect", ConnectionMode.REST)
                        
                        # Start polling
                        self.tasks.append(asyncio.create_task(self._rest_poller()))
                        
                        logger.info("✅ REST connected")
                        return True
        except Exception as e:
            logger.error(f"❌ REST connection failed: {e}")
        
        return False
    
    async def _ws_listener(self):
        """Listen for WebSocket messages"""
        reconnect_count = 0
        
        while self.mode == ConnectionMode.WEBSOCKET and reconnect_count < self.max_reconnect_attempts:
            try:
                async for message in self.ws_connection:
                    try:
                        data = json.loads(message)
                        self.metrics.total_messages += 1
                        self.metrics.last_message_ts = time.time()
                        await self._emit_event("on_message", data)
                    except json.JSONDecodeError:
                        self.metrics.total_errors += 1
                        logger.warning(f"⚠️  Invalid JSON: {message}")
                        
            except websockets.exceptions.ConnectionClosed:
                logger.warning("⚠️  WebSocket disconnected")
                reconnect_count += 1
                self.metrics.reconnect_attempts += 1
                
                if reconnect_count < self.max_reconnect_attempts:
                    await asyncio.sleep(self.reconnect_interval)
                    if await self._connect_websocket():
                        reconnect_count = 0
            
            except Exception as e:
                self.metrics.total_errors += 1
                logger.error(f"❌ WebSocket listener error: {e}")
                await asyncio.sleep(self.reconnect_interval)
        
        await self._emit_event("on_disconnect")
        self.mode = ConnectionMode.DISCONNECTED
    
    async def _rest_poller(self, poll_interval: int = 1):
        """Poll REST endpoint for updates"""
        while self.mode == ConnectionMode.REST:
            try:
                async with self.session.get(f"{self.rest_url}/latest") as resp:
                    if resp.status == 200:
                        data = await resp.json()
                        self.metrics.total_messages += 1
                        self.metrics.last_message_ts = time.time()
                        await self._emit_event("on_message", data)
                    else:
                        self.metrics.total_errors += 1
                        logger.warning(f"⚠️  REST error: {resp.status}")
                        
            except Exception as e:
                self.metrics.total_errors += 1
                logger.error(f"❌ REST poller error: {e}")
            
            await asyncio.sleep(poll_interval)
    
    async def send_message(self, data: Dict[str, Any]) -> bool:
        """Send message to MEM"""
        if self.mode == ConnectionMode.WEBSOCKET:
            try:
                await self.ws_connection.send(json.dumps(data))
                return True
            except Exception as e:
                logger.error(f"❌ WebSocket send error: {e}")
                self.metrics.total_errors += 1
                return False
        
        elif self.mode == ConnectionMode.REST:
            try:
                async with self.session.post(
                    f"{self.rest_url}/command",
                    json=data,
                    headers=self._get_headers()
                ) as resp:
                    return resp.status == 200
            except Exception as e:
                logger.error(f"❌ REST send error: {e}")
                self.metrics.total_errors += 1
                return False
        
        return False
    
    def _get_headers(self) -> Dict[str, str]:
        """Get headers with auth"""
        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers["Authorization"] = f"Bearer {self.api_key}"
        return headers
    
    async def get_metrics(self) -> Dict[str, Any]:
        """Get current connection metrics"""
        self.metrics.uptime_seconds = time.time() - self.start_time
        self.metrics.mode = self.mode
        self.metrics.connected = self.mode != ConnectionMode.DISCONNECTED
        return self.metrics.to_dict()
    
    async def disconnect(self):
        """Disconnect and cleanup"""
        logger.info("🔌 Disconnecting...")
        
        self.mode = ConnectionMode.DISCONNECTED
        
        if self.ws_connection:
            await self.ws_connection.close()
        
        if self.session:
            await self.session.close()
        
        # Cancel all tasks
        for task in self.tasks:
            task.cancel()
        
        await self._emit_event("on_disconnect")
        logger.info("✅ Disconnected")
    
    async def health_check(self) -> Dict[str, Any]:
        """Perform health check"""
        return {
            "status": "healthy" if self.mode != ConnectionMode.DISCONNECTED else "unhealthy",
            "mode": self.mode.value,
            "circuit_breaker_state": self.circuit_breaker.get_state(),
            "metrics": await self.get_metrics(),
            "timestamp": datetime.now().isoformat()
        }


# Example usage
async def main():
    """Example usage of MemConnector"""
    
    connector = MemConnector(
        ws_url="ws://127.0.0.1:8765",
        rest_url="http://127.0.0.1:5000"
    )
    
    # Register callbacks
    async def on_message(data):
        logger.info(f"📨 Message received: {data}")
    
    def on_connect(mode):
        logger.info(f"✅ Connected via {mode.value}")
    
    connector.register_callback("on_message", on_message)
    connector.register_callback("on_connect", on_connect)
    
    # Connect
    if await connector.connect():
        logger.info("✅ Connected successfully")
        
        # Send test message
        await connector.send_message({"type": "ping"})
        
        # Wait for messages
        await asyncio.sleep(10)
        
        # Show metrics
        metrics = await connector.get_metrics()
        logger.info(f"📊 Metrics: {json.dumps(metrics, indent=2)}")
        
        # Disconnect
        await connector.disconnect()
    else:
        logger.error("❌ Failed to connect")


if __name__ == "__main__":
    asyncio.run(main())
