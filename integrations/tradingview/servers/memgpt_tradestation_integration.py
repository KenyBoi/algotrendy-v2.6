#!/usr/bin/env python3
"""
MemGPT TradeStation Paper Trading Integration
============================================

Connects MemGPT to TradeStation's paper trading platform for automated trade execution.
Supports both TradeStation Desktop (EasyLanguage) and Web API integration.
"""

import asyncio
import json
import time
import requests
import websocket
import threading
from datetime import datetime
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, asdict
import logging
from pathlib import Path

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class TradeStationOrder:
    """TradeStation order structure"""
    symbol: str
    quantity: int
    side: str  # "Buy" or "Sell"
    order_type: str  # "Market" or "Limit"
    time_in_force: str  # "Day" or "GTC"
    price: Optional[float] = None
    stop_price: Optional[float] = None
    account_id: str = "SIM123456"  # Paper trading account

@dataclass
class MemGPTTradeSignal:
    """MemGPT trade signal structure"""
    symbol: str
    action: str  # "BUY", "SELL", "HOLD"
    confidence: float
    reasoning: str
    quantity: int
    price: float
    timestamp: int

class TradeStationPaperTrader:
    """TradeStation Paper Trading Integration for MemGPT"""
    
    def __init__(self, api_key: str = None, secret: str = None, paper_account: str = "SIM123456"):
        self.api_key = api_key or "YOUR_TRADESTATION_API_KEY"
        self.secret = secret or "YOUR_TRADESTATION_SECRET"
        self.paper_account = paper_account
        self.base_url = "https://api.tradestation.com/v3"
        self.sandbox_url = "https://sim-api.tradestation.com/v3"  # Paper trading endpoint
        
        # Use sandbox/paper trading URL
        self.api_url = self.sandbox_url
        
        # Authentication
        self.access_token = None
        self.refresh_token = None
        
        # Active positions and orders
        self.active_positions = {}
        self.active_orders = {}
        
        logger.info("🏦 TradeStation Paper Trader initialized")
        logger.info(f"📊 Paper Account: {self.paper_account}")
        logger.info(f"🔗 API Endpoint: {self.api_url}")
    
    async def authenticate(self):
        """Authenticate with TradeStation API"""
        try:
            auth_url = "https://signin.tradestation.com/oauth/token"
            
            # For paper trading, you can use demo credentials
            auth_data = {
                "grant_type": "client_credentials",
                "client_id": self.api_key,
                "client_secret": self.secret,
                "scope": "MarketData ReadAccount Trade Crypto"
            }
            
            response = requests.post(auth_url, data=auth_data)
            
            if response.status_code == 200:
                auth_result = response.json()
                self.access_token = auth_result.get("access_token")
                self.refresh_token = auth_result.get("refresh_token")
                
                logger.info("✅ TradeStation authentication successful")
                return True
            else:
                logger.error(f"❌ Authentication failed: {response.status_code}")
                logger.error(f"Response: {response.text}")
                
                # For demo purposes, use mock authentication
                self.access_token = "DEMO_ACCESS_TOKEN"
                logger.info("🔧 Using demo authentication for paper trading")
                return True
                
        except Exception as e:
            logger.error(f"❌ Authentication error: {e}")
            # Fallback to demo mode
            self.access_token = "DEMO_ACCESS_TOKEN"
            logger.info("🔧 Falling back to demo mode")
            return True
    
    def get_headers(self):
        """Get API headers with authentication"""
        return {
            "Authorization": f"Bearer {self.access_token}",
            "Content-Type": "application/json"
        }
    
    async def get_account_info(self):
        """Get paper trading account information"""
        try:
            url = f"{self.api_url}/brokerage/accounts"
            headers = self.get_headers()
            
            response = requests.get(url, headers=headers)
            
            if response.status_code == 200:
                accounts = response.json()
                logger.info("📊 Account info retrieved successfully")
                return accounts
            else:
                # Mock response for demo
                demo_account = {
                    "accounts": [{
                        "accountId": self.paper_account,
                        "name": "Paper Trading Account",
                        "equity": 100000.00,
                        "cashBalance": 100000.00,
                        "marketValue": 0.00,
                        "type": "Simulation"
                    }]
                }
                logger.info("📊 Using demo account info")
                return demo_account
                
        except Exception as e:
            logger.error(f"❌ Error getting account info: {e}")
            return None
    
    async def place_order(self, order: TradeStationOrder) -> Dict[str, Any]:
        """Place a paper trade order"""
        try:
            url = f"{self.api_url}/brokerage/accounts/{self.paper_account}/orders"
            headers = self.get_headers()
            
            # TradeStation order format
            order_data = {
                "AccountID": self.paper_account,
                "Symbol": order.symbol,
                "Quantity": str(order.quantity),
                "OrderType": order.order_type,
                "TradeAction": order.side,
                "TimeInForce": {
                    "Duration": order.time_in_force
                },
                "Route": "Intelligent"
            }
            
            # Add price for limit orders
            if order.order_type == "Limit" and order.price:
                order_data["LimitPrice"] = str(order.price)
            
            logger.info(f"📤 Placing {order.side} order for {order.quantity} {order.symbol}")
            
            response = requests.post(url, headers=headers, json=order_data)
            
            if response.status_code in [200, 201]:
                result = response.json()
                order_id = result.get("OrderID", f"DEMO_{int(time.time())}")
                
                # Store the order
                self.active_orders[order_id] = {
                    "order_id": order_id,
                    "symbol": order.symbol,
                    "quantity": order.quantity,
                    "side": order.side,
                    "status": "Filled",  # Paper trades fill immediately
                    "timestamp": datetime.now().isoformat()
                }
                
                logger.info(f"✅ Order placed successfully - ID: {order_id}")
                return {
                    "success": True,
                    "order_id": order_id,
                    "status": "Filled",
                    "message": f"{order.side} {order.quantity} {order.symbol} executed"
                }
            else:
                logger.error(f"❌ Order failed: {response.status_code}")
                return {
                    "success": False,
                    "error": response.text,
                    "message": "Order placement failed"
                }
                
        except Exception as e:
            logger.error(f"❌ Error placing order: {e}")
            
            # Demo order execution for testing
            demo_order_id = f"DEMO_{int(time.time())}"
            self.active_orders[demo_order_id] = {
                "order_id": demo_order_id,
                "symbol": order.symbol,
                "quantity": order.quantity,
                "side": order.side,
                "status": "Filled",
                "timestamp": datetime.now().isoformat()
            }
            
            logger.info(f"🔧 Demo order executed - ID: {demo_order_id}")
            return {
                "success": True,
                "order_id": demo_order_id,
                "status": "Filled (Demo)",
                "message": f"Demo {order.side} {order.quantity} {order.symbol} executed"
            }
    
    async def get_positions(self) -> Dict[str, Any]:
        """Get current positions"""
        try:
            url = f"{self.api_url}/brokerage/accounts/{self.paper_account}/positions"
            headers = self.get_headers()
            
            response = requests.get(url, headers=headers)
            
            if response.status_code == 200:
                positions = response.json()
                logger.info("📊 Positions retrieved successfully")
                return positions
            else:
                # Mock positions for demo
                demo_positions = {
                    "positions": []
                }
                return demo_positions
                
        except Exception as e:
            logger.error(f"❌ Error getting positions: {e}")
            return {"positions": []}
    
    async def process_memgpt_signal(self, signal: MemGPTTradeSignal) -> Dict[str, Any]:
        """Process MemGPT trading signal and place order"""
        try:
            logger.info(f"🧠 Processing MemGPT signal: {signal.action} {signal.symbol}")
            logger.info(f"💭 Reasoning: {signal.reasoning}")
            logger.info(f"📊 Confidence: {signal.confidence:.2%}")
            
            if signal.action in ["BUY", "SELL"] and signal.confidence > 0.6:
                # Create TradeStation order
                order = TradeStationOrder(
                    symbol=signal.symbol,
                    quantity=signal.quantity,
                    side="Buy" if signal.action == "BUY" else "Sell",
                    order_type="Market",  # Use market orders for immediate execution
                    time_in_force="Day",
                    account_id=self.paper_account
                )
                
                # Place the order
                result = await self.place_order(order)
                
                if result["success"]:
                    logger.info(f"✅ MemGPT signal executed successfully")
                    return {
                        "success": True,
                        "action": signal.action,
                        "symbol": signal.symbol,
                        "quantity": signal.quantity,
                        "order_id": result["order_id"],
                        "confidence": signal.confidence,
                        "reasoning": signal.reasoning
                    }
                else:
                    logger.error(f"❌ Failed to execute MemGPT signal")
                    return {
                        "success": False,
                        "error": result.get("error"),
                        "message": "Order execution failed"
                    }
                    
            else:
                logger.info(f"⏸️ MemGPT signal ignored - Action: {signal.action}, Confidence: {signal.confidence:.2%}")
                return {
                    "success": False,
                    "message": f"Signal ignored - {signal.action} with {signal.confidence:.2%} confidence",
                    "action": signal.action,
                    "confidence": signal.confidence
                }
                
        except Exception as e:
            logger.error(f"❌ Error processing MemGPT signal: {e}")
            return {
                "success": False,
                "error": str(e),
                "message": "Signal processing failed"
            }
    
    def start_memgpt_listener(self):
        """Start listening for MemGPT signals"""
        logger.info("👂 Starting MemGPT signal listener...")
        
        # This would connect to your MemGPT companion server
        memgpt_server = "http://216.238.90.131:5003"
        
        async def listen_for_signals():
            while True:
                try:
                    # Poll MemGPT server for signals
                    response = requests.get(f"{memgpt_server}/memgpt/live/BTCUSDT", timeout=5)
                    
                    if response.status_code == 200:
                        data = response.json()
                        
                        # Convert to MemGPT signal
                        signal = MemGPTTradeSignal(
                            symbol=data.get("symbol", "BTCUSDT"),
                            action=data.get("action", "HOLD"),
                            confidence=data.get("confidence", 0.5),
                            reasoning=data.get("reasoning", "No reasoning provided"),
                            quantity=100,  # Default quantity
                            price=data.get("price", 0.0),
                            timestamp=int(time.time())
                        )
                        
                        # Process high-confidence signals
                        if signal.confidence > 0.75 and signal.action in ["BUY", "SELL"]:
                            result = await self.process_memgpt_signal(signal)
                            logger.info(f"📊 Signal processed: {result}")
                    
                    await asyncio.sleep(10)  # Check every 10 seconds
                    
                except Exception as e:
                    logger.error(f"❌ Error in signal listener: {e}")
                    await asyncio.sleep(30)
        
        # Run listener in background
        asyncio.create_task(listen_for_signals())
    
    async def get_trade_history(self) -> List[Dict[str, Any]]:
        """Get trade execution history"""
        try:
            url = f"{self.api_url}/brokerage/accounts/{self.paper_account}/orders"
            headers = self.get_headers()
            
            response = requests.get(url, headers=headers)
            
            if response.status_code == 200:
                orders = response.json()
                return orders.get("Orders", [])
            else:
                # Return demo orders
                return list(self.active_orders.values())
                
        except Exception as e:
            logger.error(f"❌ Error getting trade history: {e}")
            return list(self.active_orders.values())

async def main():
    """Demo TradeStation integration"""
    logger.info("🚀 Starting TradeStation Paper Trading Demo")
    
    # Initialize trader
    trader = TradeStationPaperTrader(
        api_key="demo_key",
        secret="demo_secret",
        paper_account="SIM123456"
    )
    
    # Authenticate
    await trader.authenticate()
    
    # Get account info
    account_info = await trader.get_account_info()
    logger.info(f"💰 Account Info: {account_info}")
    
    # Demo MemGPT signal
    demo_signal = MemGPTTradeSignal(
        symbol="AAPL",
        action="BUY",
        confidence=0.85,
        reasoning="Strong bullish momentum with high volume confirmation",
        quantity=10,
        price=150.00,
        timestamp=int(time.time())
    )
    
    # Process the signal
    result = await trader.process_memgpt_signal(demo_signal)
    logger.info(f"📊 Demo signal result: {result}")
    
    # Get positions
    positions = await trader.get_positions()
    logger.info(f"📊 Current positions: {positions}")
    
    # Get trade history
    history = await trader.get_trade_history()
    logger.info(f"📈 Trade history: {history}")
    
    logger.info("✅ TradeStation Paper Trading Demo completed")

if __name__ == "__main__":
    asyncio.run(main())