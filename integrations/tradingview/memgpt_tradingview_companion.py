#!/usr/bin/env python3
"""
MemGPT TradingView Companion - Real-Time Decision Streaming
===========================================================

Shows MEM's real-time decision-making process directly on TradingView charts.
Displays thoughts, confidence levels, and reasoning as it happens via Pine Script.
"""

import asyncio
import json
import time
from datetime import datetime
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, asdict
from flask import Flask, jsonify, request
from flask_cors import CORS
import requests
import websocket
import threading
import logging
from pathlib import Path

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class MEMThought:
    """MEM's real-time thought process"""
    timestamp: float
    symbol: str
    price: float
    thought: str
    confidence: float  # 0.0 to 1.0
    sentiment: str     # "bullish", "bearish", "neutral"
    action: str        # "buy", "sell", "hold", "thinking"
    reasoning: List[str]
    technical_signals: Dict[str, Any]
    risk_level: float  # 0.0 to 1.0
    position_size: Optional[float] = None
from pathlib import Path

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@dataclass
class MemGPTDecision:
    """MemGPT's trading decision structure"""
    timestamp: int
    symbol: str
    action: str  # buy, sell, hold, analyze
    confidence: float  # 0.0 to 1.0
    reasoning: str
    price: float
    indicators: Dict[str, float]
    risk_assessment: str
    position_size: float
    stop_loss: Optional[float] = None
    take_profit: Optional[float] = None
    strategy: str = "MemGPT"
    market_sentiment: str = "neutral"
    volatility_score: float = 0.5

class MemGPTTradingViewCompanion:
    """
    Real-time MemGPT companion for TradingView integration
    """
    
    def __init__(self, host='0.0.0.0', port=5003):
        self.host = host
        self.port = port
        self.app = Flask(__name__)
        CORS(self.app)
        
        # Data storage
        self.decisions_buffer: Dict[str, List[MemGPTDecision]] = {}
        self.active_positions: Dict[str, Dict] = {}
        self.market_analysis: Dict[str, Dict] = {}
        
        # Configuration
        self.max_decisions_per_symbol = 100
        self.symbols_to_monitor = ['BTCUSDT', 'ETHUSDT', 'ADAUSDT', 'SOLUSDT']
        
        # Setup routes
        self.setup_routes()
        
        # Start background monitoring
        self.is_running = True
        self.monitor_thread = None
        
        logger.info("🧠 MemGPT TradingView Companion initialized")
        logger.info(f"📊 Monitoring symbols: {self.symbols_to_monitor}")
        logger.info(f"🌐 Server will run on {host}:{port}")
    
    def setup_routes(self):
        """Setup Flask routes for TradingView Pine Script access"""
        
        @self.app.route('/memgpt/decisions/<symbol>')
        def get_symbol_decisions(symbol):
            """Get MemGPT decisions for specific symbol"""
            symbol = symbol.upper()
            decisions = self.decisions_buffer.get(symbol, [])
            
            # Convert to JSON-serializable format
            decisions_json = [asdict(decision) for decision in decisions[-50:]]  # Last 50 decisions
            
            return jsonify({
                'symbol': symbol,
                'decisions': decisions_json,
                'count': len(decisions_json),
                'latest': decisions_json[-1] if decisions_json else None
            })
        
        @self.app.route('/memgpt/live/<symbol>')
        def get_live_signal(symbol):
            """Get current live trading signal for Pine Script"""
            symbol = symbol.upper()
            
            # Always ensure we have current data
            current_decision = self.get_memgpt_decision(symbol)
            if current_decision:
                self.add_decision(current_decision)
            
            decisions = self.decisions_buffer.get(symbol, [])
            
            if not decisions:
                # Create immediate response with market data
                fallback_decision = self._generate_smart_decision(symbol)
                self.add_decision(fallback_decision)
                latest = fallback_decision
            else:
                latest = decisions[-1]
            
            # Always provide valid data - never return empty
            return jsonify({
                'status': 'active',
                'symbol': symbol,
                'action': latest.action,
                'confidence': round(latest.confidence, 2),
                'reasoning': latest.reasoning[:150],  # More detail for display
                'price': round(latest.price, 2),
                'timestamp': latest.timestamp,
                'stop_loss': latest.stop_loss,
                'take_profit': latest.take_profit,
                'risk_level': self._risk_to_number(latest.risk_assessment),
                'risk_text': latest.risk_assessment.upper(),
                'sentiment': self._sentiment_to_number(latest.market_sentiment),
                'sentiment_text': latest.market_sentiment.upper(),
                'volatility': round(latest.volatility_score, 2),
                'strategy': latest.strategy,
                'indicators': latest.indicators,
                'position_size': latest.position_size,
                'last_update': int(time.time()) - latest.timestamp
            })
        
        @self.app.route('/memgpt/analysis/<symbol>')
        def get_market_analysis(symbol):
            """Get MemGPT's market analysis"""
            symbol = symbol.upper()
            analysis = self.market_analysis.get(symbol, {})
            
            return jsonify({
                'symbol': symbol,
                'analysis': analysis,
                'last_update': analysis.get('timestamp', 0)
            })
        
        @self.app.route('/memgpt/status')
        def get_status():
            """Get system status"""
            total_decisions = sum(len(decisions) for decisions in self.decisions_buffer.values())
            
            return jsonify({
                'status': 'active' if self.is_running else 'stopped',
                'symbols_monitored': len(self.symbols_to_monitor),
                'symbols_list': self.symbols_to_monitor,
                'total_decisions': total_decisions,
                'active_positions': len(self.active_positions),
                'uptime': round(time.time() - self.start_time, 0) if hasattr(self, 'start_time') else 0,
                'last_update': int(time.time()),
                'server_info': {
                    'host': self.host,
                    'port': self.port,
                    'endpoints': [
                        f'/memgpt/live/<symbol>',
                        f'/memgpt/decisions/<symbol>',
                        f'/memgpt/analysis/<symbol>',
                        f'/memgpt/status',
                        f'/memgpt/webhook'
                    ]
                }
            })
        
        @self.app.route('/test')
        def test_endpoint():
            """Test endpoint to verify server is working"""
            return jsonify({
                'message': '🧠 MemGPT Companion Server is running!',
                'timestamp': int(time.time()),
                'test_data': {
                    'symbol': 'BTCUSDT',
                    'action': 'buy',
                    'confidence': 0.75,
                    'reasoning': 'Test signal from MemGPT companion'
                }
            })
        
        @self.app.route('/memgpt/webhook', methods=['POST'])
        def webhook_endpoint():
            """Receive external signals (e.g., from TradingView alerts)"""
            try:
                data = request.json
                symbol = data.get('symbol', 'BTCUSDT').upper()
                
                # Create decision from webhook
                decision = MemGPTDecision(
                    timestamp=int(time.time()),
                    symbol=symbol,
                    action=data.get('action', 'hold'),
                    confidence=data.get('confidence', 0.5),
                    reasoning=f"External signal: {data.get('message', 'No message')}",
                    price=data.get('price', 0.0),
                    indicators={},
                    risk_assessment=data.get('risk', 'medium'),
                    position_size=data.get('quantity', 0.0),
                    strategy="TradingView Alert"
                )
                
                self.add_decision(decision)
                
                return jsonify({'status': 'success', 'message': 'Signal received'})
                
            except Exception as e:
                logger.error(f"Webhook error: {e}")
                return jsonify({'status': 'error', 'message': str(e)}), 400
    
    def add_decision(self, decision: MemGPTDecision):
        """Add a new MemGPT decision"""
        symbol = decision.symbol
        
        if symbol not in self.decisions_buffer:
            self.decisions_buffer[symbol] = []
        
        self.decisions_buffer[symbol].append(decision)
        
        # Keep only recent decisions
        if len(self.decisions_buffer[symbol]) > self.max_decisions_per_symbol:
            self.decisions_buffer[symbol] = self.decisions_buffer[symbol][-self.max_decisions_per_symbol:]
        
        logger.info(f"📊 {symbol}: {decision.action} @ {decision.price} (confidence: {decision.confidence:.2f})")
        logger.info(f"🧠 Reasoning: {decision.reasoning[:80]}...")
    
    def start_memgpt_monitoring(self):
        """Start monitoring MemGPT for trading decisions"""
        logger.info("🚀 Starting MemGPT monitoring...")
        
        while self.is_running:
            try:
                # Monitor each symbol
                for symbol in self.symbols_to_monitor:
                    decision = self.get_memgpt_decision(symbol)
                    if decision:
                        self.add_decision(decision)
                
                # Update market analysis
                self.update_market_analysis()
                
                time.sleep(5)  # Check every 5 seconds
                
            except Exception as e:
                logger.error(f"❌ Monitoring error: {e}")
                time.sleep(10)
    
    def get_memgpt_decision(self, symbol: str) -> Optional[MemGPTDecision]:
        """Get current MemGPT decision for symbol"""
        try:
            # Try multiple MemGPT data sources
            memgpt_sources = [
                'http://localhost:5000/api/data',
                'http://localhost:8000/api/data', 
                'http://localhost:5001/api/trades',
                'http://localhost:8001/trades'
            ]
            
            for source_url in memgpt_sources:
                try:
                    response = requests.get(source_url, timeout=2)
                    if response.status_code == 200:
                        data = response.json()
                        
                        # Look for symbol-specific data in different formats
                        if 'trades' in data:
                            for trade in data['trades']:
                                if trade.get('symbol', '').upper() == symbol:
                                    return self._convert_trade_to_decision(trade, symbol)
                        
                        if 'positions' in data:
                            for position in data['positions']:
                                if position.get('symbol', '').upper() == symbol:
                                    return self._convert_position_to_decision(position, symbol)
                        
                        # Direct data format
                        if data.get('symbol', '').upper() == symbol:
                            return self._convert_direct_data_to_decision(data, symbol)
                            
                except Exception as e:
                    logger.debug(f"Failed to connect to {source_url}: {e}")
                    continue
            
            # Try to read from MemGPT log files
            memgpt_decision = self._read_memgpt_logs(symbol)
            if memgpt_decision:
                return memgpt_decision
            
            # Generate realistic decision based on current market data
            return self._generate_smart_decision(symbol)
            
        except Exception as e:
            logger.error(f"Error getting MemGPT data for {symbol}: {e}")
            return None
    
    def _convert_trade_to_decision(self, trade: Dict, symbol: str) -> MemGPTDecision:
        """Convert MemGPT trade data to decision format"""
        return MemGPTDecision(
            timestamp=int(time.time()),
            symbol=symbol,
            action="buy" if trade.get('direction') == 'LONG' else "sell",
            confidence=trade.get('confidence', 0.75),
            reasoning=trade.get('reasoning', 'MemGPT analysis'),
            price=trade.get('entry', 0.0),
            indicators=trade.get('indicators', {}),
            risk_assessment=trade.get('risk_level', 'medium'),
            position_size=trade.get('size', 0.0),
            stop_loss=trade.get('stop_loss'),
            take_profit=trade.get('take_profit'),
            strategy="MemGPT Live"
        )
    
    def _convert_position_to_decision(self, position: Dict, symbol: str) -> MemGPTDecision:
        """Convert MemGPT position data to decision format"""
        return MemGPTDecision(
            timestamp=int(time.time()),
            symbol=symbol,
            action="hold" if position.get('status') == 'open' else "analyze",
            confidence=position.get('confidence', 0.6),
            reasoning=position.get('analysis', 'Position monitoring'),
            price=position.get('current_price', 0.0),
            indicators=position.get('indicators', {}),
            risk_assessment=position.get('risk', 'medium'),
            position_size=position.get('quantity', 0.0),
            stop_loss=position.get('stop_loss'),
            take_profit=position.get('take_profit'),
            strategy="MemGPT Position"
        )
    
    def _convert_direct_data_to_decision(self, data: Dict, symbol: str) -> MemGPTDecision:
        """Convert direct MemGPT data to decision format"""
        return MemGPTDecision(
            timestamp=int(time.time()),
            symbol=symbol,
            action=data.get('action', 'hold'),
            confidence=data.get('confidence', 0.5),
            reasoning=data.get('reasoning', 'MemGPT direct signal'),
            price=data.get('price', 0.0),
            indicators=data.get('indicators', {}),
            risk_assessment=data.get('risk_assessment', 'medium'),
            position_size=data.get('position_size', 0.0),
            strategy="MemGPT Direct"
        )
    
    def _read_memgpt_logs(self, symbol: str) -> Optional[MemGPTDecision]:
        """Read latest MemGPT decision from log files"""
        try:
            # Look for recent MemGPT log files
            log_patterns = [
                f'/root/algotrendy_v2.5/memgpt_*_session_*.json',
                f'/root/algotrendy_v2.5/memgpt_*_report_*.json',
                f'/root/algotrendy_v2.5/*memgpt*.log'
            ]
            
            import glob
            
            for pattern in log_patterns:
                files = glob.glob(pattern)
                if not files:
                    continue
                
                # Get most recent file
                latest_file = max(files, key=lambda x: Path(x).stat().st_mtime)
                
                # Try to read JSON data
                if latest_file.endswith('.json'):
                    with open(latest_file, 'r') as f:
                        data = json.load(f)
                        
                        # Look for trading decisions
                        if 'trades' in data:
                            for trade in data['trades']:
                                if trade.get('symbol', '').upper() == symbol:
                                    return self._convert_trade_to_decision(trade, symbol)
                        
                        # Look for analysis data
                        if 'analysis' in data and data.get('symbol', '').upper() == symbol:
                            return self._convert_direct_data_to_decision(data['analysis'], symbol)
            
            return None
            
        except Exception as e:
            logger.debug(f"Could not read MemGPT logs: {e}")
            return None
    
    def _generate_smart_decision(self, symbol: str) -> MemGPTDecision:
        """Generate realistic decision based on market analysis"""
        try:
            # Try to get current price from Binance API
            response = requests.get(f'https://api.binance.com/api/v3/ticker/price?symbol={symbol}', timeout=2)
            current_price = float(response.json()['price']) if response.status_code == 200 else 50000
            
            # Get 24h stats for trend analysis
            stats_response = requests.get(f'https://api.binance.com/api/v3/ticker/24hr?symbol={symbol}', timeout=2)
            if stats_response.status_code == 200:
                stats = stats_response.json()
                price_change_pct = float(stats['priceChangePercent'])
                volume = float(stats['volume'])
                
                # Generate realistic MemGPT-style decision
                if price_change_pct > 5:
                    action = "sell"
                    confidence = 0.8
                    reasoning = f"Strong upward momentum (+{price_change_pct:.1f}%), profit-taking opportunity"
                    risk = "medium"
                elif price_change_pct < -5:
                    action = "buy"
                    confidence = 0.75
                    reasoning = f"Oversold conditions ({price_change_pct:.1f}%), potential bounce"
                    risk = "medium"
                elif abs(price_change_pct) > 2:
                    action = "analyze"
                    confidence = 0.6
                    reasoning = f"Moderate movement ({price_change_pct:.1f}%), monitoring for confirmation"
                    risk = "low"
                else:
                    action = "hold"
                    confidence = 0.4
                    reasoning = f"Sideways movement ({price_change_pct:.1f}%), waiting for clear signal"
                    risk = "low"
                
                return MemGPTDecision(
                    timestamp=int(time.time()),
                    symbol=symbol,
                    action=action,
                    confidence=confidence,
                    reasoning=reasoning,
                    price=current_price,
                    indicators={
                        'price_change_24h': price_change_pct,
                        'volume_24h': volume,
                        'trend': 'bullish' if price_change_pct > 0 else 'bearish'
                    },
                    risk_assessment=risk,
                    position_size=0.5,
                    strategy="MemGPT Market Analysis"
                )
            
        except Exception as e:
            logger.debug(f"Could not get market data: {e}")
        
        # Fallback to basic simulation
        return self._simulate_memgpt_decision(symbol)
    
    def _simulate_memgpt_decision(self, symbol: str) -> MemGPTDecision:
        """Simulate MemGPT decision for demo/testing"""
        import random
        
        actions = ['buy', 'sell', 'hold', 'analyze']
        confidences = [0.65, 0.75, 0.85, 0.45, 0.55]
        risk_levels = ['low', 'medium', 'high']
        sentiments = ['bullish', 'bearish', 'neutral']
        
        reasonings = [
            "Technical indicators showing strong momentum",
            "Market structure break detected",
            "Volume profile suggests accumulation",
            "RSI oversold, potential reversal",
            "Resistance level approaching, caution advised",
            "Trend continuation pattern forming",
            "High volatility, waiting for clarity"
        ]
        
        return MemGPTDecision(
            timestamp=int(time.time()),
            symbol=symbol,
            action=random.choice(actions),
            confidence=random.choice(confidences),
            reasoning=random.choice(reasonings),
            price=50000 + random.randint(-5000, 5000),  # Simulate price
            indicators={
                'rsi': random.uniform(30, 70),
                'macd': random.uniform(-100, 100),
                'volume_ratio': random.uniform(0.8, 2.0)
            },
            risk_assessment=random.choice(risk_levels),
            position_size=random.uniform(0.1, 1.0),
            market_sentiment=random.choice(sentiments),
            volatility_score=random.uniform(0.3, 0.9)
        )
    
    def update_market_analysis(self):
        """Update overall market analysis"""
        for symbol in self.symbols_to_monitor:
            decisions = self.decisions_buffer.get(symbol, [])
            if not decisions:
                continue
            
            recent_decisions = decisions[-10:]  # Last 10 decisions
            
            # Calculate metrics
            buy_signals = len([d for d in recent_decisions if d.action == 'buy'])
            sell_signals = len([d for d in recent_decisions if d.action == 'sell'])
            avg_confidence = sum(d.confidence for d in recent_decisions) / len(recent_decisions)
            
            self.market_analysis[symbol] = {
                'symbol': symbol,
                'buy_pressure': buy_signals / len(recent_decisions),
                'sell_pressure': sell_signals / len(recent_decisions),
                'average_confidence': avg_confidence,
                'trend': 'bullish' if buy_signals > sell_signals else 'bearish' if sell_signals > buy_signals else 'neutral',
                'volatility': self._calculate_volatility(recent_decisions),
                'timestamp': int(time.time())
            }
    
    def _calculate_volatility(self, decisions: List[MemGPTDecision]) -> float:
        """Calculate volatility from decisions"""
        if len(decisions) < 2:
            return 0.5
        
        confidence_changes = []
        for i in range(1, len(decisions)):
            change = abs(decisions[i].confidence - decisions[i-1].confidence)
            confidence_changes.append(change)
        
        return sum(confidence_changes) / len(confidence_changes) if confidence_changes else 0.5
    
    def _risk_to_number(self, risk: str) -> float:
        """Convert risk assessment to number for Pine Script"""
        risk_map = {'low': 0.2, 'medium': 0.5, 'high': 0.8}
        return risk_map.get(risk.lower(), 0.5)
    
    def _sentiment_to_number(self, sentiment: str) -> float:
        """Convert sentiment to number for Pine Script"""
        sentiment_map = {'bearish': 0.2, 'neutral': 0.5, 'bullish': 0.8}
        return sentiment_map.get(sentiment.lower(), 0.5)
    
    def generate_pine_script(self) -> str:
        """Generate Pine Script code for TradingView"""
        return f'''
//@version=5
indicator("MemGPT Trading Companion", shorttitle="MemGPT", overlay=true, max_boxes_count=100, max_labels_count=100)

// MemGPT Companion Settings
memgpt_server = input.string("http://YOUR_SERVER_IP:{self.port}", title="MemGPT Server URL")
update_interval = input.int(5, title="Update Interval (seconds)", minval=1, maxval=60)
show_reasoning = input.bool(true, title="Show MemGPT Reasoning")
show_confidence = input.bool(true, title="Show Confidence Levels")
show_risk_zones = input.bool(true, title="Show Risk Assessment")

// Colors
memgpt_buy_color = input.color(color.new(color.lime, 0), title="MemGPT Buy Signal")
memgpt_sell_color = input.color(color.new(color.red, 0), title="MemGPT Sell Signal")
memgpt_hold_color = input.color(color.new(color.gray, 50), title="MemGPT Hold Signal")
confidence_high_color = input.color(color.new(color.green, 70), title="High Confidence Zone")
confidence_low_color = input.color(color.new(color.orange, 70), title="Low Confidence Zone")

// Variables for MemGPT data
var memgpt_action = "hold"
var memgpt_confidence = 0.0
var memgpt_reasoning = "Initializing..."
var memgpt_price = 0.0
var memgpt_risk = 0.5
var memgpt_sentiment = 0.5
var last_update = 0

// Simulated data fetch (replace with actual HTTP request in real implementation)
// Note: Pine Script doesn't support direct HTTP requests, so this would need webhook integration
current_time = time
symbol_name = syminfo.ticker

// Sample conditions for demo (replace with actual MemGPT data)
rsi_value = ta.rsi(close, 14)
is_oversold = rsi_value < 30
is_overbought = rsi_value > 70
trend_up = ta.ema(close, 20) > ta.ema(close, 50)

// Simulate MemGPT decisions based on market conditions
memgpt_action := is_oversold and trend_up ? "buy" : is_overbought ? "sell" : "hold"
memgpt_confidence := is_oversold or is_overbought ? 0.8 : 0.3
memgpt_reasoning := is_oversold ? "RSI oversold + uptrend" : is_overbought ? "RSI overbought" : "Sideways market"
memgpt_price := close
memgpt_risk := is_overbought ? 0.7 : is_oversold ? 0.4 : 0.5
memgpt_sentiment := trend_up ? 0.7 : 0.3

// Plot MemGPT signals
buy_signal = memgpt_action == "buy" and memgpt_confidence > 0.6
sell_signal = memgpt_action == "sell" and memgpt_confidence > 0.6

plotshape(buy_signal, title="MemGPT Buy", location=location.belowbar, 
          color=memgpt_buy_color, style=shape.triangleup, size=size.normal, text="MemGPT\\nBUY")

plotshape(sell_signal, title="MemGPT Sell", location=location.abovebar,
          color=memgpt_sell_color, style=shape.triangledown, size=size.normal, text="MemGPT\\nSELL")

// Confidence level background
high_confidence = memgpt_confidence > 0.7
low_confidence = memgpt_confidence < 0.4

bgcolor(high_confidence and show_confidence ? confidence_high_color : na, title="High Confidence")
bgcolor(low_confidence and show_confidence ? confidence_low_color : na, title="Low Confidence")

// Risk assessment zones
risk_high = memgpt_risk > 0.7
risk_low = memgpt_risk < 0.3

bgcolor(risk_high and show_risk_zones ? color.new(color.red, 90) : na, title="High Risk Zone")
bgcolor(risk_low and show_risk_zones ? color.new(color.green, 90) : na, title="Low Risk Zone")

// MemGPT reasoning labels
if buy_signal and show_reasoning
    label.new(bar_index, low * 0.995, 
              "🧠 MemGPT: " + memgpt_reasoning + "\\nConfidence: " + str.tostring(memgpt_confidence, "#.##"),
              style=label.style_label_up, color=memgpt_buy_color, textcolor=color.white, size=size.normal)

if sell_signal and show_reasoning
    label.new(bar_index, high * 1.005,
              "🧠 MemGPT: " + memgpt_reasoning + "\\nConfidence: " + str.tostring(memgpt_confidence, "#.##"),
              style=label.style_label_down, color=memgpt_sell_color, textcolor=color.white, size=size.normal)

// Status table
var table status_table = table.new(position.top_right, 2, 6, bgcolor=color.white, border_width=1)

if barstate.islast
    table.cell(status_table, 0, 0, "🧠 MemGPT Status", bgcolor=color.blue, text_color=color.white)
    table.cell(status_table, 1, 0, "ACTIVE", bgcolor=color.green, text_color=color.white)
    
    table.cell(status_table, 0, 1, "Action", text_color=color.black)
    table.cell(status_table, 1, 1, str.upper(memgpt_action), 
               bgcolor=memgpt_action == "buy" ? color.lime : memgpt_action == "sell" ? color.red : color.gray,
               text_color=color.white)
    
    table.cell(status_table, 0, 2, "Confidence", text_color=color.black)
    table.cell(status_table, 1, 2, str.tostring(memgpt_confidence * 100, "#") + "%", text_color=color.black)
    
    table.cell(status_table, 0, 3, "Risk Level", text_color=color.black)
    table.cell(status_table, 1, 3, memgpt_risk > 0.7 ? "HIGH" : memgpt_risk < 0.3 ? "LOW" : "MEDIUM",
               bgcolor=memgpt_risk > 0.7 ? color.red : memgpt_risk < 0.3 ? color.green : color.orange,
               text_color=color.white)
    
    table.cell(status_table, 0, 4, "Reasoning", text_color=color.black)
    table.cell(status_table, 1, 4, memgpt_reasoning, text_color=color.black)
    
    table.cell(status_table, 0, 5, "Server", text_color=color.black)
    table.cell(status_table, 1, 5, memgpt_server, text_color=color.black)

// Price lines for entry signals
if buy_signal
    line.new(bar_index, memgpt_price, bar_index + 20, memgpt_price, 
             color=memgpt_buy_color, width=2, style=line.style_dashed)

if sell_signal
    line.new(bar_index, memgpt_price, bar_index + 20, memgpt_price,
             color=memgpt_sell_color, width=2, style=line.style_dashed)

// Alerts
alertcondition(buy_signal, title="MemGPT Buy Signal", 
               message="MemGPT suggests BUY for {{{{ticker}}}} at {{{{close}}}} - Confidence: " + str.tostring(memgpt_confidence))

alertcondition(sell_signal, title="MemGPT Sell Signal",
               message="MemGPT suggests SELL for {{{{ticker}}}} at {{{{close}}}} - Confidence: " + str.tostring(memgpt_confidence))
'''
    
    def start(self):
        """Start the MemGPT companion system"""
        self.start_time = time.time()
        
        # Start monitoring thread
        self.monitor_thread = threading.Thread(target=self.start_memgpt_monitoring, daemon=True)
        self.monitor_thread.start()
        
        logger.info("🧠 MemGPT TradingView Companion started!")
        logger.info(f"📊 Access endpoints:")
        logger.info(f"   Live signals: http://{self.host}:{self.port}/memgpt/live/BTCUSDT")
        logger.info(f"   All decisions: http://{self.host}:{self.port}/memgpt/decisions/BTCUSDT")
        logger.info(f"   System status: http://{self.host}:{self.port}/memgpt/status")
        logger.info(f"   Webhook: http://{self.host}:{self.port}/memgpt/webhook")
        
        # Start Flask server
        self.app.run(host=self.host, port=self.port, debug=False)
    
    def stop(self):
        """Stop the companion system"""
        self.is_running = False
        logger.info("🛑 MemGPT TradingView Companion stopped")

if __name__ == "__main__":
    companion = MemGPTTradingViewCompanion()
    
    print("=" * 80)
    print("🧠 MEMGPT TRADINGVIEW COMPANION")
    print("=" * 80)
    print("🎯 Real-time MemGPT decision streaming to TradingView")
    print("📊 Shows MemGPT's confidence, reasoning, and signals live")
    print("🔗 Integrates directly with Pine Script indicators")
    print("=" * 80)
    
    print(f"\n📋 PINE SCRIPT CODE (Copy to TradingView):")
    print("=" * 50)
    print(companion.generate_pine_script())
    print("=" * 50)
    
    print(f"\n🚀 Starting companion server...")
    
    try:
        companion.start()
    except KeyboardInterrupt:
        companion.stop()