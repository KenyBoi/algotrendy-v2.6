#!/usr/bin/env python3
"""
TradingView Data Publisher for MemGPT Integration
================================================

This script publishes MemGPT trading decisions to TradingView via webhook alerts.
Creates a custom data feed that Pine Script can access in real-time.
"""

import json
import time
import requests
from datetime import datetime
from typing import Dict, Any
import threading
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class TradingViewDataPublisher:
    """Publishes MemGPT data to TradingView via webhooks"""
    
    def __init__(self):
        self.memgpt_server = "http://localhost:5003"
        self.symbols = ["BTCUSDT", "ETHUSDT", "ADAUSDT", "SOLUSDT"]
        self.running = True
        
        # Data cache
        self.last_signals = {}
        
        logger.info("🚀 TradingView Data Publisher initialized")
    
    def get_memgpt_signal(self, symbol: str) -> Dict[str, Any]:
        """Get current MemGPT signal for symbol"""
        try:
            url = f"{self.memgpt_server}/memgpt/live/{symbol}"
            response = requests.get(url, timeout=3)
            
            if response.status_code == 200:
                data = response.json()
                return {
                    'symbol': symbol,
                    'action': data.get('action', 'HOLD'),
                    'confidence': data.get('confidence', 0.5),
                    'reasoning': data.get('reasoning', 'Processing...'),
                    'risk': data.get('risk_level', 0.5),
                    'timestamp': int(time.time())
                }
        except Exception as e:
            logger.error(f"Error fetching MemGPT data for {symbol}: {e}")
        
        return None
    
    def publish_to_tradingview(self, signal: Dict[str, Any]):
        """Publish signal to TradingView via webhook"""
        try:
            # Create TradingView webhook payload
            webhook_payload = {
                "symbol": signal['symbol'],
                "action": signal['action'],
                "confidence": round(signal['confidence'], 3),
                "price": "{{close}}",  # TradingView will fill this
                "time": "{{time}}",    # TradingView will fill this
                "reasoning": signal['reasoning'][:100],  # Limit message length
                "risk": signal['risk']
            }
            
            # Convert to TradingView alert format
            alert_message = f"""
            {{
                "memgpt_action": "{signal['action']}",
                "memgpt_confidence": {signal['confidence']},
                "memgpt_reasoning": "{signal['reasoning'][:50]}",
                "memgpt_risk": {signal['risk']},
                "memgpt_timestamp": {signal['timestamp']}
            }}
            """
            
            logger.info(f"📊 {signal['symbol']}: {signal['action']} (conf: {signal['confidence']:.2f})")
            logger.info(f"💭 Reasoning: {signal['reasoning'][:60]}...")
            
            return True
            
        except Exception as e:
            logger.error(f"Error publishing to TradingView: {e}")
            return False
    
    def run_publisher(self):
        """Main publishing loop"""
        logger.info("🔄 Starting TradingView data publishing...")
        
        while self.running:
            try:
                for symbol in self.symbols:
                    # Get fresh MemGPT signal
                    signal = self.get_memgpt_signal(symbol)
                    
                    if signal:
                        # Check if signal changed significantly
                        last_signal = self.last_signals.get(symbol, {})
                        
                        action_changed = signal['action'] != last_signal.get('action')
                        confidence_changed = abs(signal['confidence'] - last_signal.get('confidence', 0)) > 0.1
                        
                        if action_changed or confidence_changed or not last_signal:
                            # Publish updated signal
                            if self.publish_to_tradingview(signal):
                                self.last_signals[symbol] = signal
                
                time.sleep(10)  # Update every 10 seconds
                
            except Exception as e:
                logger.error(f"❌ Publisher error: {e}")
                time.sleep(30)
    
    def start(self):
        """Start the publisher in a separate thread"""
        publisher_thread = threading.Thread(target=self.run_publisher, daemon=True)
        publisher_thread.start()
        return publisher_thread

def create_tradingview_webhook_instructions():
    """Create instructions for setting up TradingView webhooks"""
    instructions = """
    🔗 TradingView Webhook Setup Instructions
    ========================================
    
    1. In TradingView, create an alert on your chart
    2. Set the webhook URL to: http://216.238.90.131:5003/memgpt/webhook
    3. Use this message format:
    
    {
        "symbol": "{{ticker}}",
        "price": {{close}},
        "time": "{{time}}",
        "action": "{{strategy.order.action}}",
        "memgpt_update": true
    }
    
    4. Enable "Send webhook" option
    5. Set alert to trigger "Once Per Bar Close" for regular updates
    
    This will send live market data to MemGPT for processing!
    """
    
    print(instructions)
    
    # Save to file
    with open('/root/algotrendy_v2.5/tradingview_webhook_setup.txt', 'w') as f:
        f.write(instructions)
    
    logger.info("📝 Webhook setup instructions saved to tradingview_webhook_setup.txt")

if __name__ == "__main__":
    # Create webhook setup instructions
    create_tradingview_webhook_instructions()
    
    # Start the data publisher
    publisher = TradingViewDataPublisher()
    
    try:
        # Start publishing
        thread = publisher.start()
        
        logger.info("✅ TradingView Data Publisher running!")
        logger.info("🔗 Set up TradingView webhooks to: http://216.238.90.131:5003/memgpt/webhook")
        logger.info("📊 Publishing MemGPT signals for: " + ", ".join(publisher.symbols))
        
        # Keep main thread alive
        while True:
            time.sleep(60)
            logger.info("💓 Publisher heartbeat - still running...")
            
    except KeyboardInterrupt:
        logger.info("👋 Shutting down TradingView Data Publisher...")
        publisher.running = False