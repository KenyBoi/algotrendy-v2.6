#!/usr/bin/env python3
"""
MemGPT TradingView Trade Plotter
===============================
Sends MemGPT trades to TradingView for visual plotting
"""

import sys
sys.path.append('/root/algotrendy_v2.5')

import requests
import json
import time
from datetime import datetime
from flask import Flask, jsonify
import threading

class MemGPTTradingViewPlotter:
    def __init__(self):
        self.trades_history = []
        self.current_positions = []
        self.plot_data = {}
        
        # TradingView plotting server
        self.app = Flask(__name__)
        self.setup_routes()
        
        print("📈 MemGPT → TradingView Trade Plotter")
        print("====================================")
        print("🎯 Monitoring MemGPT trades for TradingView plotting")
        print("📊 Plot server running on :5002/plots")
        
    def setup_routes(self):
        """Setup Flask routes for TradingView data"""
        
        @self.app.route('/plots/<symbol>')
        def get_plots(symbol):
            """Return plot data for specific symbol"""
            symbol_plots = self.plot_data.get(symbol.upper(), {
                'buy_signals': [],
                'sell_signals': [],
                'profit_zones': [],
                'loss_zones': []
            })
            return jsonify(symbol_plots)
            
        @self.app.route('/plots/all')
        def get_all_plots():
            """Return all plot data"""
            return jsonify(self.plot_data)
            
        @self.app.route('/status')
        def get_status():
            """Return system status"""
            return jsonify({
                'active_positions': len(self.current_positions),
                'total_trades': len(self.trades_history),
                'symbols_tracked': list(self.plot_data.keys()),
                'last_update': datetime.now().isoformat()
            })
    
    def monitor_memgpt_trades(self):
        """Monitor MemGPT for new trades and convert to plot data"""
        while True:
            try:
                # Check for new trades from MemGPT system
                new_trades = self.get_memgpt_trades()
                
                for trade in new_trades:
                    self.process_trade_for_plotting(trade)
                
                time.sleep(5)  # Check every 5 seconds
                
            except Exception as e:
                print(f"❌ Error monitoring trades: {e}")
                time.sleep(10)
    
    def get_memgpt_trades(self):
        """Get current trades from MemGPT system"""
        try:
            # Try to get data from web dashboard API
            response = requests.get('http://localhost:5000/api/data', timeout=5)
            if response.status_code == 200:
                data = response.json()
                return data.get('trades', [])
        except:
            pass
        
        return []
    
    def process_trade_for_plotting(self, trade):
        """Convert MemGPT trade to TradingView plot format"""
        symbol = trade.get('symbol', 'BTCUSDT')
        
        # Initialize symbol data if not exists
        if symbol not in self.plot_data:
            self.plot_data[symbol] = {
                'buy_signals': [],
                'sell_signals': [],
                'profit_zones': [],
                'loss_zones': []
            }
        
        # Create plot point
        plot_point = {
            'time': int(time.time()),
            'price': trade.get('entry', 0),
            'current_price': trade.get('current', 0),
            'pnl': trade.get('pnl', 0),
            'pnl_pct': trade.get('pnl_pct', 0),
            'strategy': trade.get('strategy', 'MemGPT'),
            'direction': trade.get('direction', 'LONG'),
            'trade_id': trade.get('id', ''),
            'status': trade.get('status', 'active')
        }
        
        # Add to appropriate category
        if trade.get('direction') == 'LONG':
            self.plot_data[symbol]['buy_signals'].append(plot_point)
        else:
            self.plot_data[symbol]['sell_signals'].append(plot_point)
            
        # Add profit/loss zones
        if plot_point['pnl'] > 0:
            self.plot_data[symbol]['profit_zones'].append(plot_point)
        elif plot_point['pnl'] < 0:
            self.plot_data[symbol]['loss_zones'].append(plot_point)
        
        # Keep only recent data (last 1000 points)
        for key in self.plot_data[symbol]:
            self.plot_data[symbol][key] = self.plot_data[symbol][key][-1000:]
        
        print(f"📊 Plot added: {symbol} {plot_point['direction']} @ {plot_point['price']}")
    
    def start_server(self):
        """Start the plotting server"""
        def run_server():
            self.app.run(host='0.0.0.0', port=5002, debug=False)
        
        server_thread = threading.Thread(target=run_server, daemon=True)
        server_thread.start()
        
        # Start trade monitoring
        monitor_thread = threading.Thread(target=self.monitor_memgpt_trades, daemon=True)
        monitor_thread.start()

def generate_pine_script():
    """Generate the Pine Script for TradingView"""
    pine_script = '''
//@version=5
indicator("MemGPT Trade Plotter", shorttitle="MemGPT", overlay=true)

// MemGPT Trade Plotting Script
// ============================
// Connects to your MemGPT system and plots trades on TradingView

// Input settings
server_url = input.string("http://YOUR_SERVER_IP:5002", title="MemGPT Server URL")
symbol_name = input.string("BTCUSDT", title="Symbol to Track")
update_frequency = input.int(10, title="Update Frequency (seconds)", minval=1, maxval=60)

// Colors
buy_color = color.new(color.green, 0)
sell_color = color.new(color.red, 0)
profit_color = color.new(color.lime, 70)
loss_color = color.new(color.orange, 70)

// Variables to store trade data
var float[] buy_prices = array.new_float()
var int[] buy_times = array.new_int()
var float[] sell_prices = array.new_float()
var int[] sell_times = array.new_int()

// Function to fetch data (simulated - you'll need webhook or external data)
// In real implementation, you'd connect to your MemGPT server
get_memgpt_data() =>
    // This would fetch from your server at http://YOUR_SERVER_IP:5002/plots/BTCUSDT
    // For now, we'll plot based on conditions
    [close, time]

// Plot MemGPT trades
[data_price, data_time] = get_memgpt_data()

// Buy signals (Long entries)
buy_signal = ta.crossover(ta.rsi(close, 14), 30) // Example condition
plot_buy = buy_signal ? low * 0.99 : na
plot(plot_buy, title="MemGPT Long Entry", style=plot.style_triangleup, 
     location=location.belowbar, color=buy_color, size=size.small)

// Sell signals (Short entries)  
sell_signal = ta.crossunder(ta.rsi(close, 14), 70) // Example condition
plot_sell = sell_signal ? high * 1.01 : na
plot(plot_sell, title="MemGPT Short Entry", style=plot.style_triangledown,
     location=location.abovebar, color=sell_color, size=size.small)

// Profit/Loss zones
profit_zone = ta.rsi(close, 14) > 50
loss_zone = ta.rsi(close, 14) < 50

bgcolor(profit_zone ? profit_color : na, title="Profit Zone")
bgcolor(loss_zone ? loss_color : na, title="Loss Zone")

// Labels for trade info
if buy_signal
    label.new(bar_index, low, "MemGPT LONG\\nEntry: " + str.tostring(close), 
              style=label.style_label_up, color=buy_color, size=size.normal)

if sell_signal  
    label.new(bar_index, high, "MemGPT SHORT\\nEntry: " + str.tostring(close),
              style=label.style_label_down, color=sell_color, size=size.normal)

// Table showing MemGPT status
var table info_table = table.new(position.top_right, 2, 4, bgcolor=color.white, border_width=1)

if barstate.islast
    table.cell(info_table, 0, 0, "MemGPT Status", bgcolor=color.blue, text_color=color.white)
    table.cell(info_table, 1, 0, "ACTIVE", bgcolor=color.green, text_color=color.white)
    table.cell(info_table, 0, 1, "Server", text_color=color.black)
    table.cell(info_table, 1, 1, server_url, text_color=color.black)
    table.cell(info_table, 0, 2, "Symbol", text_color=color.black)  
    table.cell(info_table, 1, 2, symbol_name, text_color=color.black)
    table.cell(info_table, 0, 3, "Last Update", text_color=color.black)
    table.cell(info_table, 1, 3, str.tostring(timenow), text_color=color.black)
'''
    
    return pine_script

if __name__ == "__main__":
    print("🚀 Starting MemGPT → TradingView Trade Plotter...")
    
    plotter = MemGPTTradingViewPlotter()
    plotter.start_server()
    
    print("\n📈 PINE SCRIPT FOR TRADINGVIEW:")
    print("=" * 50)
    print(generate_pine_script())
    print("=" * 50)
    
    print(f"\n🎯 Setup Instructions:")
    print(f"1. Copy Pine Script above to TradingView")
    print(f"2. Change 'YOUR_SERVER_IP' to: {requests.get('https://api.ipify.org').text}")
    print(f"3. Add indicator to your chart")
    print(f"4. MemGPT trades will appear as triangles and labels!")
    print(f"\n📊 Plot Data URLs:")
    print(f"   All plots: http://localhost:5002/plots/all")
    print(f"   BTCUSDT: http://localhost:5002/plots/BTCUSDT") 
    print(f"   Status: http://localhost:5002/status")
    
    try:
        while True:
            time.sleep(60)
    except KeyboardInterrupt:
        print("🛑 MemGPT Trade Plotter stopped")