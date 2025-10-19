#!/usr/bin/env python3
"""
MemGPT TradingView → TradeStation Webhook Bridge
===============================================

Receives TradingView alerts and routes them to TradeStation paper trading.
Integrates with MemGPT companion server for enhanced decision making.
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import json
import asyncio
import threading
import time
from datetime import datetime
import logging
from typing import Dict, Any
import requests

# Import our TradeStation integration
from memgpt_tradestation_integration import TradeStationPaperTrader, MemGPTTradeSignal

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Flask app
app = Flask(__name__)
CORS(app)

# Global TradeStation trader instance
tradestation_trader = None

class TradingViewWebhookServer:
    """Webhook server for TradingView → MemGPT → TradeStation integration"""
    
    def __init__(self, port=5004):
        self.port = port
        self.memgpt_server = "http://216.238.90.131:5003"
        self.trade_log = []
        
        # Initialize TradeStation integration
        global tradestation_trader
        tradestation_trader = TradeStationPaperTrader()
        
        logger.info("🌉 TradingView → TradeStation Bridge initialized")
        logger.info(f"🔗 Webhook server: http://localhost:{self.port}")
        logger.info(f"🧠 MemGPT server: {self.memgpt_server}")
    
    async def setup_tradestation(self):
        """Initialize TradeStation connection"""
        try:
            await tradestation_trader.authenticate()
            account_info = await tradestation_trader.get_account_info()
            logger.info("✅ TradeStation paper trading ready")
            return True
        except Exception as e:
            logger.error(f"❌ TradeStation setup failed: {e}")
            return False

# Initialize webhook server
webhook_server = TradingViewWebhookServer()

@app.route('/', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        "status": "active",
        "service": "MemGPT TradingView → TradeStation Bridge",
        "timestamp": datetime.now().isoformat(),
        "endpoints": {
            "webhook": "/webhook",
            "memgpt": "/memgpt/<symbol>",
            "status": "/status",
            "trades": "/trades"
        }
    })

@app.route('/webhook', methods=['POST'])
def tradingview_webhook():
    """Main webhook endpoint for TradingView alerts"""
    try:
        # Get TradingView alert data
        data = request.get_json()
        
        if not data:
            return jsonify({"error": "No data received"}), 400
        
        logger.info(f"📡 TradingView alert received: {data}")
        
        # Extract alert information
        symbol = data.get('symbol', 'BTCUSDT')
        action = data.get('action', 'HOLD')
        price = data.get('price', 0.0)
        message = data.get('message', 'TradingView alert')
        
        # Get MemGPT analysis for this symbol
        memgpt_analysis = get_memgpt_analysis(symbol)
        
        # Combine TradingView alert with MemGPT intelligence
        enhanced_signal = process_combined_signal(data, memgpt_analysis)
        
        # Execute trade if conditions are met
        if enhanced_signal and enhanced_signal.get('should_trade'):
            trade_result = execute_tradestation_order(enhanced_signal)
            
            # Log the trade
            trade_record = {
                "timestamp": datetime.now().isoformat(),
                "symbol": symbol,
                "action": action,
                "price": price,
                "tradingview_signal": data,
                "memgpt_analysis": memgpt_analysis,
                "trade_result": trade_result,
                "success": trade_result.get('success', False)
            }
            
            webhook_server.trade_log.append(trade_record)
            
            return jsonify({
                "status": "success",
                "message": "Alert processed and trade executed",
                "trade_result": trade_result,
                "memgpt_confidence": memgpt_analysis.get('confidence', 0.0)
            })
        else:
            return jsonify({
                "status": "ignored",
                "message": "Signal not strong enough for execution",
                "memgpt_analysis": memgpt_analysis
            })
            
    except Exception as e:
        logger.error(f"❌ Webhook error: {e}")
        return jsonify({"error": str(e)}), 500

@app.route('/memgpt/<symbol>', methods=['GET'])
def get_memgpt_signal(symbol):
    """Get MemGPT analysis for a symbol"""
    try:
        analysis = get_memgpt_analysis(symbol)
        return jsonify(analysis)
    except Exception as e:
        logger.error(f"❌ MemGPT signal error: {e}")
        return jsonify({"error": str(e)}), 500

@app.route('/status', methods=['GET'])
def get_status():
    """Get system status"""
    return jsonify({
        "webhook_server": "active",
        "tradestation_connected": tradestation_trader is not None,
        "memgpt_server": webhook_server.memgpt_server,
        "total_trades": len(webhook_server.trade_log),
        "last_trade": webhook_server.trade_log[-1] if webhook_server.trade_log else None
    })

@app.route('/trades', methods=['GET'])
def get_trades():
    """Get trade history"""
    return jsonify({
        "total_trades": len(webhook_server.trade_log),
        "trades": webhook_server.trade_log[-10:]  # Last 10 trades
    })

def get_memgpt_analysis(symbol: str) -> Dict[str, Any]:
    """Get MemGPT analysis from companion server"""
    try:
        response = requests.get(f"{webhook_server.memgpt_server}/memgpt/live/{symbol}", timeout=5)
        
        if response.status_code == 200:
            data = response.json()
            logger.info(f"🧠 MemGPT analysis for {symbol}: {data.get('action')} ({data.get('confidence', 0):.2%})")
            return data
        else:
            logger.warning(f"⚠️ MemGPT server unavailable for {symbol}")
            return {
                "symbol": symbol,
                "action": "HOLD",
                "confidence": 0.5,
                "reasoning": "MemGPT server unavailable",
                "price": 0.0
            }
            
    except Exception as e:
        logger.error(f"❌ MemGPT analysis error: {e}")
        return {
            "symbol": symbol,
            "action": "HOLD",
            "confidence": 0.0,
            "reasoning": f"Error: {e}",
            "price": 0.0
        }

def process_combined_signal(tradingview_data: Dict, memgpt_data: Dict) -> Dict[str, Any]:
    """Combine TradingView alert with MemGPT analysis"""
    try:
        tv_action = tradingview_data.get('action', 'HOLD')
        memgpt_action = memgpt_data.get('action', 'HOLD')
        memgpt_confidence = memgpt_data.get('confidence', 0.0)
        
        # Signal agreement logic
        signals_agree = tv_action == memgpt_action
        high_confidence = memgpt_confidence > 0.75
        
        should_trade = signals_agree and high_confidence and tv_action in ['BUY', 'SELL']
        
        logger.info(f"📊 Signal Analysis:")
        logger.info(f"   TradingView: {tv_action}")
        logger.info(f"   MemGPT: {memgpt_action} ({memgpt_confidence:.2%})")
        logger.info(f"   Agreement: {signals_agree}")
        logger.info(f"   Should Trade: {should_trade}")
        
        return {
            "should_trade": should_trade,
            "symbol": tradingview_data.get('symbol', 'BTCUSDT'),
            "action": memgpt_action if signals_agree else 'HOLD',
            "confidence": memgpt_confidence,
            "reasoning": f"TradingView: {tv_action}, MemGPT: {memgpt_action} ({memgpt_confidence:.2%})",
            "quantity": 100,  # Default quantity
            "price": tradingview_data.get('price', memgpt_data.get('price', 0.0)),
            "tradingview_signal": tradingview_data,
            "memgpt_analysis": memgpt_data
        }
        
    except Exception as e:
        logger.error(f"❌ Signal processing error: {e}")
        return {"should_trade": False, "error": str(e)}

def execute_tradestation_order(signal: Dict[str, Any]) -> Dict[str, Any]:
    """Execute order on TradeStation paper trading"""
    try:
        if not tradestation_trader:
            return {"success": False, "error": "TradeStation not initialized"}
        
        # Create MemGPT signal object
        memgpt_signal = MemGPTTradeSignal(
            symbol=signal['symbol'],
            action=signal['action'],
            confidence=signal['confidence'],
            reasoning=signal['reasoning'],
            quantity=signal['quantity'],
            price=signal['price'],
            timestamp=int(time.time())
        )
        
        # Execute the trade asynchronously
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        result = loop.run_until_complete(tradestation_trader.process_memgpt_signal(memgpt_signal))
        loop.close()
        
        return result
        
    except Exception as e:
        logger.error(f"❌ TradeStation execution error: {e}")
        return {"success": False, "error": str(e)}

def run_webhook_server():
    """Run the webhook server"""
    logger.info("🚀 Starting TradingView → TradeStation webhook bridge")
    
    # Setup TradeStation in background
    def setup_ts():
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        loop.run_until_complete(webhook_server.setup_tradestation())
        loop.close()
    
    threading.Thread(target=setup_ts, daemon=True).start()
    
    # Start Flask server
    app.run(
        host='0.0.0.0',
        port=webhook_server.port,
        debug=False,
        threaded=True
    )

if __name__ == '__main__':
    run_webhook_server()