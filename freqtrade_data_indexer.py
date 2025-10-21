#!/usr/bin/env python3
"""
Freqtrade Data Indexer for Algolia Search
Automatically fetches and indexes trading data from Freqtrade bots
"""

import os
import sys
import json
import requests
import logging
from datetime import datetime, timedelta
from requests.auth import HTTPBasicAuth

# Add current directory to path for imports
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Configuration
ALGOLIA_APP_ID = os.getenv('ALGOLIA_APP_ID', 'YOUR_APP_ID')
ALGOLIA_ADMIN_KEY = os.getenv('ALGOLIA_ADMIN_KEY', 'YOUR_ADMIN_KEY')

FREQTRADE_BOTS = [
    {
        'name': 'Conservative RSI Bot',
        'port': 8082,
        'username': 'memgpt',
        'password': 'trading123',
        'strategy': 'RSI_Conservative',
        'description': 'Conservative RSI-based trading with tight risk management'
    },
    {
        'name': 'MACD Hunter Bot',
        'port': 8083,
        'username': 'memgpt',
        'password': 'trading123',
        'strategy': 'MACD_Aggressive',
        'description': 'Aggressive MACD crossover strategy for trending markets'
    },
    {
        'name': 'Aggressive RSI Bot',
        'port': 8084,
        'username': 'memgpt',
        'password': 'trading123',
        'strategy': 'RSI_Aggressive',
        'description': 'High-frequency RSI trading with dynamic position sizing'
    }
]

def check_algolia_dependencies():
    """Check if Algolia client is installed"""
    try:
        from algoliasearch.search_client import SearchClient
        return True
    except ImportError:
        logger.error("Algolia client not installed. Run: pip install algoliasearch")
        return False

def get_freqtrade_data(bot_config):
    """Fetch comprehensive data from a Freqtrade bot"""
    try:
        base_url = f"http://127.0.0.1:{bot_config['port']}/api/v1"
        auth = HTTPBasicAuth(bot_config['username'], bot_config['password'])
        
        # Test connection first
        response = requests.get(f"{base_url}/ping", auth=auth, timeout=5)
        if response.status_code != 200:
            logger.warning(f"Bot {bot_config['name']} ping failed (port {bot_config['port']})")
            return None
        
        # Fetch all available data
        endpoints = {
            'balance': '/balance',
            'profit': '/profit',
            'status': '/status',
            'performance': '/performance',
            'trades': '/trades',
            'daily': '/daily',
            'stats': '/stats',
            'whitelist': '/whitelist',
            'strategy': '/strategy'
        }
        
        bot_data = {
            'name': bot_config['name'],
            'strategy': bot_config['strategy'],
            'description': bot_config['description'],
            'port': bot_config['port'],
            'last_updated': datetime.now().isoformat()
        }
        
        for key, endpoint in endpoints.items():
            try:
                response = requests.get(f"{base_url}{endpoint}", auth=auth, timeout=10)
                if response.status_code == 200:
                    bot_data[key] = response.json()
                else:
                    logger.warning(f"Failed to fetch {key} from {bot_config['name']}: {response.status_code}")
                    bot_data[key] = {}
            except Exception as e:
                logger.warning(f"Error fetching {key} from {bot_config['name']}: {e}")
                bot_data[key] = {}
        
        return bot_data
        
    except Exception as e:
        logger.error(f"Error connecting to {bot_config['name']}: {e}")
        return None

def format_trades_for_algolia(bot_data):
    """Format trade data for Algolia indexing"""
    records = []
    bot_name = bot_data['name']
    strategy = bot_data['strategy']
    
    try:
        # Process closed trades
        trades_data = bot_data.get('trades', {})
        trades = trades_data.get('trades', []) if isinstance(trades_data, dict) else trades_data
        
        for trade in trades:
            record = {
                'objectID': f"freqtrade_{bot_name.lower().replace(' ', '_')}_{trade.get('trade_id', 'unknown')}",
                'source': 'freqtrade',
                'type': 'trade',
                'bot_name': bot_name,
                'strategy': strategy,
                'description': f"{strategy} trade on {trade.get('pair', 'UNKNOWN')}",
                'symbol': trade.get('pair', 'UNKNOWN'),
                'side': 'short' if trade.get('is_short', False) else 'long',
                'entry_price': float(trade.get('open_rate', 0)),
                'exit_price': float(trade.get('close_rate', 0)) if trade.get('close_rate') else None,
                'quantity': float(trade.get('amount', 0)),
                'pnl': float(trade.get('profit_abs', 0)),
                'pnl_percent': float(trade.get('profit_ratio', 0)) * 100,
                'entry_time': trade.get('open_date', ''),
                'exit_time': trade.get('close_date', ''),
                'duration_minutes': trade.get('trade_duration_s', 0) // 60 if trade.get('trade_duration_s') else 0,
                'status': 'closed' if not trade.get('is_open', True) else 'open',
                'confidence': 0.85,
                'tags': ['freqtrade', 'trade', bot_name.lower().replace(' ', '_'), strategy.lower()],
                'timestamp': datetime.now().isoformat(),
                'broker': 'freqtrade',
                'win': float(trade.get('profit_abs', 0)) > 0,
                'entry_reason': trade.get('enter_tag', ''),
                'exit_reason': trade.get('exit_reason', ''),
                'fee_open': float(trade.get('fee_open', 0)),
                'fee_close': float(trade.get('fee_close', 0))
            }
            records.append(record)
        
        # Process open positions
        status_data = bot_data.get('status', [])
        for position in status_data:
            record = {
                'objectID': f"freqtrade_open_{bot_name.lower().replace(' ', '_')}_{position.get('trade_id', 'unknown')}",
                'source': 'freqtrade',
                'type': 'open_position',
                'bot_name': bot_name,
                'strategy': strategy,
                'description': f"Open {strategy} position on {position.get('pair', 'UNKNOWN')}",
                'symbol': position.get('pair', 'UNKNOWN'),
                'side': 'short' if position.get('is_short', False) else 'long',
                'entry_price': float(position.get('open_rate', 0)),
                'current_price': float(position.get('current_rate', 0)),
                'quantity': float(position.get('amount', 0)),
                'pnl': float(position.get('profit_abs', 0)),
                'pnl_percent': float(position.get('profit_ratio', 0)) * 100,
                'entry_time': position.get('open_date', ''),
                'duration_minutes': position.get('trade_duration_s', 0) // 60 if position.get('trade_duration_s') else 0,
                'status': 'open',
                'confidence': 0.80,
                'tags': ['freqtrade', 'open_position', bot_name.lower().replace(' ', '_'), strategy.lower()],
                'timestamp': datetime.now().isoformat(),
                'broker': 'freqtrade',
                'entry_reason': position.get('enter_tag', ''),
                'stoploss': float(position.get('stop_loss', 0)) if position.get('stop_loss') else None
            }
            records.append(record)
            
    except Exception as e:
        logger.error(f"Error formatting trades for {bot_name}: {e}")
    
    return records

def format_performance_for_algolia(bot_data):
    """Format performance data for Algolia indexing"""
    records = []
    bot_name = bot_data['name']
    strategy = bot_data['strategy']
    
    try:
        performance = bot_data.get('performance', [])
        for perf in performance:
            record = {
                'objectID': f"freqtrade_perf_{bot_name.lower().replace(' ', '_')}_{perf.get('pair', 'unknown')}",
                'source': 'freqtrade',
                'type': 'performance',
                'bot_name': bot_name,
                'strategy': strategy,
                'description': f"{strategy} performance summary for {perf.get('pair', 'UNKNOWN')}",
                'symbol': perf.get('pair', 'UNKNOWN'),
                'profit_total': float(perf.get('profit', 0)),
                'profit_abs': float(perf.get('profit_abs', 0)),
                'trade_count': int(perf.get('count', 0)),
                'avg_profit_per_trade': float(perf.get('profit', 0)) / max(int(perf.get('count', 1)), 1),
                'tags': ['freqtrade', 'performance', bot_name.lower().replace(' ', '_'), strategy.lower()],
                'timestamp': datetime.now().isoformat(),
                'broker': 'freqtrade'
            }
            records.append(record)
            
        # Overall bot performance
        profit_data = bot_data.get('profit', {})
        if profit_data:
            record = {
                'objectID': f"freqtrade_summary_{bot_name.lower().replace(' ', '_')}",
                'source': 'freqtrade',
                'type': 'bot_summary',
                'bot_name': bot_name,
                'strategy': strategy,
                'description': f"{bot_name} overall performance summary",
                'total_profit': float(profit_data.get('profit_all_coin', 0)),
                'profit_percent': float(profit_data.get('profit_all_ratio', 0)) * 100,
                'trade_count': int(profit_data.get('closed_trade_count', 0)),
                'winning_trades': int(profit_data.get('winning_trades', 0)),
                'losing_trades': int(profit_data.get('losing_trades', 0)),
                'win_rate': float(profit_data.get('winning_trades', 0)) / max(int(profit_data.get('closed_trade_count', 1)), 1) * 100,
                'avg_duration': profit_data.get('avg_duration', ''),
                'best_pair': profit_data.get('best_pair', {}).get('key', '') if profit_data.get('best_pair') else '',
                'worst_pair': profit_data.get('worst_pair', {}).get('key', '') if profit_data.get('worst_pair') else '',
                'tags': ['freqtrade', 'summary', bot_name.lower().replace(' ', '_'), strategy.lower()],
                'timestamp': datetime.now().isoformat(),
                'broker': 'freqtrade'
            }
            records.append(record)
            
    except Exception as e:
        logger.error(f"Error formatting performance for {bot_name}: {e}")
    
    return records

def index_to_algolia(records, index_name='algotrendy_trades'):
    """Index records to Algolia"""
    try:
        from algoliasearch.search_client import SearchClient
        
        # Initialize client
        client = SearchClient.create(ALGOLIA_APP_ID, ALGOLIA_ADMIN_KEY)
        index = client.init_index(index_name)
        
        if not records:
            logger.info(f"No records to index to {index_name}")
            return {'success': True, 'records_indexed': 0}
        
        # Save objects to Algolia
        result = index.save_objects(records)
        logger.info(f"Indexed {len(records)} records to {index_name}")
        
        return {
            'success': True, 
            'records_indexed': len(records),
            'task_id': result.get('taskID'),
            'object_ids': [r['objectID'] for r in records[:5]]  # First 5 for logging
        }
        
    except Exception as e:
        logger.error(f"Error indexing to Algolia: {e}")
        return {'success': False, 'error': str(e)}

def main():
    """Main indexing function"""
    logger.info("🚀 Starting Freqtrade data indexing to Algolia")
    
    # Check dependencies
    if not check_algolia_dependencies():
        sys.exit(1)
    
    # Check Algolia configuration
    if ALGOLIA_APP_ID == 'YOUR_APP_ID' or ALGOLIA_ADMIN_KEY == 'YOUR_ADMIN_KEY':
        logger.error("⚠️ Please set ALGOLIA_APP_ID and ALGOLIA_ADMIN_KEY environment variables")
        logger.info("Example:")
        logger.info("export ALGOLIA_APP_ID='your_app_id'")
        logger.info("export ALGOLIA_ADMIN_KEY='your_admin_key'")
        sys.exit(1)
    
    all_records = []
    bot_status = {}
    
    # Process each Freqtrade bot
    for bot_config in FREQTRADE_BOTS:
        logger.info(f"📊 Processing {bot_config['name']} (port {bot_config['port']})")
        
        # Fetch bot data
        bot_data = get_freqtrade_data(bot_config)
        
        if bot_data:
            # Format trades
            trade_records = format_trades_for_algolia(bot_data)
            
            # Format performance
            perf_records = format_performance_for_algolia(bot_data)
            
            bot_records = trade_records + perf_records
            all_records.extend(bot_records)
            
            bot_status[bot_config['name']] = {
                'status': 'success',
                'records': len(bot_records),
                'trades': len(trade_records),
                'performance': len(perf_records)
            }
            
            logger.info(f"✅ {bot_config['name']}: {len(bot_records)} records prepared")
        else:
            bot_status[bot_config['name']] = {
                'status': 'failed',
                'error': 'Could not connect to Freqtrade API'
            }
            logger.warning(f"❌ {bot_config['name']}: Connection failed")
    
    # Index all records to Algolia
    if all_records:
        logger.info(f"🔍 Indexing {len(all_records)} total records to Algolia...")
        result = index_to_algolia(all_records)
        
        if result['success']:
            logger.info(f"🎉 Successfully indexed {result['records_indexed']} records!")
            logger.info("📝 Summary:")
            for bot_name, status in bot_status.items():
                if status['status'] == 'success':
                    logger.info(f"  - {bot_name}: {status['records']} records")
                else:
                    logger.info(f"  - {bot_name}: {status['error']}")
        else:
            logger.error(f"❌ Indexing failed: {result.get('error')}")
            sys.exit(1)
    else:
        logger.warning("⚠️ No data retrieved from any Freqtrade bots")
        logger.info("💡 Make sure Freqtrade bots are running and API is enabled")
        sys.exit(1)

if __name__ == '__main__':
    main()