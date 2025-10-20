#!/usr/bin/env python3
"""
TradingView → MemGPT Paper Trading Dashboard
============================================
Real-time monitoring of paper trades from TradingView alerts
"""

from flask import Flask, render_template_string, jsonify
import requests
import json
from datetime import datetime

app = Flask(__name__)

WEBHOOK_SERVER = "http://localhost:5004"

HTML_TEMPLATE = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>📈 TradingView Paper Trading Monitor</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <style>
        .trade-card { transition: all 0.3s ease; }
        .trade-card:hover { transform: translateY(-2px); box-shadow: 0 4px 20px rgba(0,255,0,0.3); }
        .pulse { animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite; }
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: .5; }
        }
        .status-active { color: #10b981; }
        .status-pending { color: #fbbf24; }
        .status-failed { color: #ef4444; }
    </style>
</head>
<body class="bg-gray-900 text-white">
    
    <!-- Header -->
    <div class="bg-gradient-to-r from-blue-900 to-purple-900 p-6 shadow-lg">
        <div class="container mx-auto">
            <h1 class="text-4xl font-bold mb-2">📈 TradingView → MemGPT Paper Trading</h1>
            <p class="text-gray-300">Real-time monitoring of AI-powered trading signals</p>
        </div>
    </div>

    <!-- Status Bar -->
    <div class="bg-gray-800 border-b border-gray-700 p-4">
        <div class="container mx-auto flex justify-between items-center">
            <div class="flex space-x-6">
                <div class="flex items-center">
                    <span class="pulse inline-block w-3 h-3 bg-green-500 rounded-full mr-2"></span>
                    <span id="webhookStatus" class="text-green-400">Webhook Active</span>
                </div>
                <div class="flex items-center">
                    <span class="text-gray-400">Total Trades:</span>
                    <span id="totalTrades" class="ml-2 text-xl font-bold text-blue-400">0</span>
                </div>
                <div class="flex items-center">
                    <span class="text-gray-400">Last Update:</span>
                    <span id="lastUpdate" class="ml-2 text-sm text-gray-500">Never</span>
                </div>
            </div>
            <button onclick="refreshData()" class="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg transition">
                🔄 Refresh
            </button>
        </div>
    </div>

    <!-- Main Content -->
    <div class="container mx-auto p-6">
        
        <!-- Quick Stats -->
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
            <div class="bg-gray-800 rounded-lg p-6 border border-gray-700">
                <div class="text-gray-400 text-sm mb-1">Active Signals</div>
                <div id="activeSignals" class="text-3xl font-bold text-green-400">0</div>
            </div>
            <div class="bg-gray-800 rounded-lg p-6 border border-gray-700">
                <div class="text-gray-400 text-sm mb-1">BUY Signals</div>
                <div id="buySignals" class="text-3xl font-bold text-lime-400">0</div>
            </div>
            <div class="bg-gray-800 rounded-lg p-6 border border-gray-700">
                <div class="text-gray-400 text-sm mb-1">SELL Signals</div>
                <div id="sellSignals" class="text-3xl font-bold text-red-400">0</div>
            </div>
            <div class="bg-gray-800 rounded-lg p-6 border border-gray-700">
                <div class="text-gray-400 text-sm mb-1">Avg Confidence</div>
                <div id="avgConfidence" class="text-3xl font-bold text-blue-400">0%</div>
            </div>
        </div>

        <!-- Recent Trades -->
        <div class="bg-gray-800 rounded-lg border border-gray-700 mb-6">
            <div class="p-6 border-b border-gray-700">
                <h2 class="text-2xl font-bold">📊 Recent Paper Trades</h2>
            </div>
            <div id="tradesContainer" class="p-6">
                <div class="text-center text-gray-500 py-8">
                    <div class="text-6xl mb-4">📭</div>
                    <p>No trades yet. Waiting for TradingView alerts...</p>
                    <p class="text-sm mt-2">Make sure your TradingView alert is configured to send webhooks to:</p>
                    <code class="bg-gray-900 px-3 py-1 rounded mt-2 inline-block">http://your-server:5004/webhook</code>
                </div>
            </div>
        </div>

        <!-- System Info -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="bg-gray-800 rounded-lg border border-gray-700 p-6">
                <h3 class="text-xl font-bold mb-4">🔗 Connection Status</h3>
                <div class="space-y-3">
                    <div class="flex justify-between items-center">
                        <span class="text-gray-400">Webhook Server</span>
                        <span id="webhookServerStatus" class="text-green-400">✅ Active</span>
                    </div>
                    <div class="flex justify-between items-center">
                        <span class="text-gray-400">MemGPT Analysis</span>
                        <span id="memgptStatus" class="text-green-400">✅ Connected</span>
                    </div>
                    <div class="flex justify-between items-center">
                        <span class="text-gray-400">TradeStation Paper</span>
                        <span id="tradestationStatus" class="text-green-400">✅ Ready</span>
                    </div>
                </div>
            </div>

            <div class="bg-gray-800 rounded-lg border border-gray-700 p-6">
                <h3 class="text-xl font-bold mb-4">⚙️ Configuration</h3>
                <div class="space-y-2 text-sm">
                    <div class="flex justify-between">
                        <span class="text-gray-400">Webhook Port:</span>
                        <span class="text-blue-400">5004</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-gray-400">MemGPT Server:</span>
                        <span class="text-blue-400">216.238.90.131:5003</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-gray-400">Paper Trading:</span>
                        <span class="text-green-400">Enabled</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-gray-400">Auto-refresh:</span>
                        <span class="text-green-400">5 seconds</span>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let lastTradeCount = 0;

        async function refreshData() {
            try {
                // Get status
                const statusRes = await fetch('/api/status');
                const status = await statusRes.json();
                
                document.getElementById('totalTrades').textContent = status.total_trades || 0;
                document.getElementById('lastUpdate').textContent = new Date().toLocaleTimeString();
                
                // Get trades
                const tradesRes = await fetch('/api/trades');
                const tradesData = await tradesRes.json();
                
                updateStats(tradesData.trades);
                displayTrades(tradesData.trades);
                
                // Check for new trades
                if (status.total_trades > lastTradeCount) {
                    showNotification('New trade received!');
                    lastTradeCount = status.total_trades;
                }
                
            } catch (error) {
                console.error('Error fetching data:', error);
                document.getElementById('webhookStatus').textContent = 'Connection Error';
                document.getElementById('webhookStatus').className = 'text-red-400';
            }
        }

        function updateStats(trades) {
            const buyCount = trades.filter(t => t.action === 'BUY').length;
            const sellCount = trades.filter(t => t.action === 'SELL').length;
            const avgConf = trades.length > 0 
                ? Math.round(trades.reduce((sum, t) => sum + (t.confidence || 0), 0) / trades.length * 100)
                : 0;
            
            document.getElementById('activeSignals').textContent = trades.length;
            document.getElementById('buySignals').textContent = buyCount;
            document.getElementById('sellSignals').textContent = sellCount;
            document.getElementById('avgConfidence').textContent = avgConf + '%';
        }

        function displayTrades(trades) {
            const container = document.getElementById('tradesContainer');
            
            if (trades.length === 0) {
                container.innerHTML = `
                    <div class="text-center text-gray-500 py-8">
                        <div class="text-6xl mb-4">📭</div>
                        <p>No trades yet. Waiting for TradingView alerts...</p>
                    </div>
                `;
                return;
            }
            
            const tradesHtml = trades.slice().reverse().map(trade => `
                <div class="trade-card bg-gray-900 rounded-lg p-4 mb-3 border border-gray-700">
                    <div class="flex justify-between items-start">
                        <div class="flex-1">
                            <div class="flex items-center space-x-3 mb-2">
                                <span class="text-2xl">${trade.action === 'BUY' ? '🚀' : '📉'}</span>
                                <span class="text-xl font-bold ${trade.action === 'BUY' ? 'text-lime-400' : 'text-red-400'}">
                                    ${trade.action}
                                </span>
                                <span class="text-lg text-blue-400">${trade.symbol}</span>
                            </div>
                            <div class="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                                <div>
                                    <div class="text-gray-400">Price</div>
                                    <div class="text-white font-semibold">$${trade.price || 'N/A'}</div>
                                </div>
                                <div>
                                    <div class="text-gray-400">Confidence</div>
                                    <div class="text-white font-semibold">${Math.round((trade.confidence || 0) * 100)}%</div>
                                </div>
                                <div>
                                    <div class="text-gray-400">MemGPT</div>
                                    <div class="text-white font-semibold">${Math.round((trade.memgpt_confidence || 0) * 100)}%</div>
                                </div>
                                <div>
                                    <div class="text-gray-400">Status</div>
                                    <div class="status-${trade.success ? 'active' : 'pending'}">${trade.success ? '✅ Executed' : '⏳ Pending'}</div>
                                </div>
                            </div>
                        </div>
                        <div class="text-xs text-gray-500 ml-4">
                            ${new Date(trade.timestamp).toLocaleString()}
                        </div>
                    </div>
                </div>
            `).join('');
            
            container.innerHTML = tradesHtml;
        }

        function showNotification(message) {
            // Simple notification
            const notification = document.createElement('div');
            notification.className = 'fixed top-4 right-4 bg-green-600 text-white px-6 py-3 rounded-lg shadow-lg z-50';
            notification.textContent = '🔔 ' + message;
            document.body.appendChild(notification);
            
            setTimeout(() => {
                notification.remove();
            }, 3000);
        }

        // Auto-refresh every 5 seconds
        setInterval(refreshData, 5000);
        
        // Initial load
        refreshData();
    </script>
</body>
</html>
"""

@app.route('/')
def index():
    return render_template_string(HTML_TEMPLATE)

@app.route('/api/status')
def api_status():
    try:
        response = requests.get(f"{WEBHOOK_SERVER}/status", timeout=5)
        return jsonify(response.json())
    except:
        return jsonify({"error": "Cannot connect to webhook server"}), 503

@app.route('/api/trades')
def api_trades():
    try:
        response = requests.get(f"{WEBHOOK_SERVER}/trades", timeout=5)
        return jsonify(response.json())
    except:
        return jsonify({"trades": [], "error": "Cannot connect to webhook server"}), 503

if __name__ == "__main__":
    print("🚀 TradingView Paper Trading Dashboard")
    print("======================================")
    print("📊 Dashboard: http://localhost:5005")
    print("📡 Webhook Server: http://localhost:5004")
    print("")
    app.run(host='0.0.0.0', port=5005, debug=False)
