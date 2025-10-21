"""
Live Trading Example - MEM Advanced Strategy
============================================
Demonstrates how to use the Advanced Trading Strategy with real market data.

This example shows:
1. Fetching real-time market data from yfinance
2. Running the advanced strategy
3. Generating trade signals
4. Monitoring multiple symbols
5. Risk management in action

Usage:
    python3 live_trading_example.py [SYMBOL]

Example:
    python3 live_trading_example.py BTCUSDT
    python3 live_trading_example.py AAPL
"""

import pandas as pd
import yfinance as yf
from datetime import datetime, timedelta
import sys
import logging
from advanced_trading_strategy import AdvancedTradingStrategy

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


def fetch_market_data(symbol: str, interval: str = '1h', period: str = '7d') -> pd.DataFrame:
    """
    Fetch market data from Yahoo Finance

    Args:
        symbol: Trading symbol (e.g., 'BTC-USD', 'AAPL')
        interval: Data interval ('1m', '5m', '15m', '1h', '4h', '1d')
        period: Data period ('1d', '5d', '1mo', '3mo', '1y')

    Returns:
        DataFrame with OHLCV data
    """
    try:
        logger.info(f"Fetching {interval} data for {symbol} (period: {period})")

        ticker = yf.Ticker(symbol)
        data = ticker.history(interval=interval, period=period)

        if data.empty:
            raise ValueError(f"No data received for {symbol}")

        # Rename columns to lowercase for consistency
        data.columns = [col.lower() for col in data.columns]

        # Ensure we have required columns
        required_cols = ['open', 'high', 'low', 'close', 'volume']
        if not all(col in data.columns for col in required_cols):
            raise ValueError(f"Missing required columns. Got: {data.columns.tolist()}")

        logger.info(f"✓ Fetched {len(data)} data points for {symbol}")
        return data

    except Exception as e:
        logger.error(f"Error fetching data for {symbol}: {e}")
        raise


def analyze_symbol(symbol: str, account_balance: float = 10000.0):
    """
    Analyze a single symbol and generate trade signals

    Args:
        symbol: Trading symbol
        account_balance: Account balance for position sizing
    """
    print("\n" + "=" * 80)
    print(f"ANALYZING: {symbol}")
    print("=" * 80)

    try:
        # Fetch data for multiple timeframes
        logger.info("Fetching multi-timeframe data...")

        data_1h = fetch_market_data(symbol, interval='1h', period='7d')
        data_4h = fetch_market_data(symbol, interval='4h', period='1mo')
        data_1d = fetch_market_data(symbol, interval='1d', period='3mo')

        # Initialize strategy
        strategy = AdvancedTradingStrategy(
            min_confidence=70.0,  # 70% minimum confidence
            max_risk_per_trade=0.02,  # 2% max risk per trade
            use_multi_timeframe=True
        )

        # Generate trade signal
        signal = strategy.generate_trade_signal(
            data_1h=data_1h,
            data_4h=data_4h,
            data_1d=data_1d,
            account_balance=account_balance
        )

        # Display results
        print("\n" + "=" * 80)
        print("TRADE RECOMMENDATION")
        print("=" * 80)
        print(f"Symbol: {symbol}")
        print(f"Timestamp: {signal['timestamp']}")
        print(f"Action: {signal['action']}")

        if signal['action'] != 'HOLD':
            print(f"\nEntry Details:")
            print(f"  Entry Price: ${signal['entry_price']:.2f}")
            print(f"  Stop Loss: ${signal['stop_loss']:.2f} "
                  f"({((signal['stop_loss']-signal['entry_price'])/signal['entry_price']*100):.2f}%)")
            print(f"  Take Profit: ${signal['take_profit']:.2f} "
                  f"({((signal['take_profit']-signal['entry_price'])/signal['entry_price']*100):.2f}%)")

            print(f"\nPosition Sizing:")
            print(f"  Position Size: {signal['position_size']:.4f} units")
            print(f"  Risk Amount: ${signal['risk_amount']:.2f}")
            print(f"  Risk Percent: {signal['risk_percent']:.2f}%")
            print(f"  Risk/Reward: 1:{signal['risk_reward_ratio']:.1f}")

            print(f"\nMarket Analysis:")
            print(f"  Signal: {signal['signal']}")
            print(f"  Confidence: {signal['confidence']:.1f}%")
            print(f"  Trend: {signal['trend']}")
            print(f"  Volatility: {signal['volatility']}")
            print(f"  Risk Level: {signal['risk_level']}")
            print(f"  Timeframes Aligned: {signal['timeframes_aligned']}")
            print(f"  Sharpe Ratio: {signal['sharpe_ratio']:.2f}")
            print(f"  ATR: {signal['atr']:.4f}")

            print(f"\nReasoning:")
            for i, reason in enumerate(signal['reasoning'], 1):
                print(f"  {i}. {reason}")

        else:
            print(f"Reason: {signal.get('reason', 'Unknown')}")
            print(f"Confidence: {signal.get('confidence', 0):.1f}%")
            print(f"Signal: {signal.get('signal', 'N/A')}")

        print("=" * 80)

        return signal

    except Exception as e:
        logger.error(f"Error analyzing {symbol}: {e}")
        import traceback
        traceback.print_exc()
        return None


def monitor_multiple_symbols(symbols: list, account_balance: float = 10000.0):
    """
    Monitor multiple symbols and generate signals for all

    Args:
        symbols: List of trading symbols
        account_balance: Account balance for position sizing
    """
    print("\n" + "=" * 80)
    print("MULTI-SYMBOL ANALYSIS")
    print("=" * 80)
    print(f"Analyzing {len(symbols)} symbols: {', '.join(symbols)}")
    print(f"Account Balance: ${account_balance:,.2f}")
    print("=" * 80)

    results = []
    buy_signals = []
    sell_signals = []

    for symbol in symbols:
        signal = analyze_symbol(symbol, account_balance)
        if signal:
            results.append({
                'symbol': symbol,
                'signal': signal
            })

            if signal['action'] == 'BUY':
                buy_signals.append(symbol)
            elif signal['action'] == 'SELL':
                sell_signals.append(symbol)

    # Summary
    print("\n" + "=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Total Symbols Analyzed: {len(results)}")
    print(f"BUY Signals: {len(buy_signals)} - {buy_signals if buy_signals else 'None'}")
    print(f"SELL Signals: {len(sell_signals)} - {sell_signals if sell_signals else 'None'}")
    print(f"HOLD Signals: {len(results) - len(buy_signals) - len(sell_signals)}")
    print("=" * 80)

    # Show top opportunities
    actionable = [r for r in results if r['signal']['action'] != 'HOLD']
    if actionable:
        print("\nTOP OPPORTUNITIES:")
        actionable.sort(key=lambda x: x['signal']['confidence'], reverse=True)

        for i, result in enumerate(actionable[:3], 1):
            s = result['signal']
            print(f"\n{i}. {result['symbol']}")
            print(f"   Action: {s['action']}")
            print(f"   Confidence: {s['confidence']:.1f}%")
            print(f"   Entry: ${s['entry_price']:.2f}")
            print(f"   R/R: 1:{s['risk_reward_ratio']:.1f}")
    else:
        print("\nNo actionable signals at this time.")

    print("\n" + "=" * 80)


def main():
    """Main entry point"""
    # Default symbols to monitor
    default_symbols = [
        'BTC-USD',   # Bitcoin
        'ETH-USD',   # Ethereum
        'AAPL',      # Apple
        'TSLA',      # Tesla
        'SPY',       # S&P 500 ETF
    ]

    # Get symbol from command line or use defaults
    if len(sys.argv) > 1:
        # Single symbol mode
        symbol = sys.argv[1]

        # Handle crypto symbols
        if symbol.endswith('USDT') or symbol.endswith('USD'):
            if '-' not in symbol:
                symbol = symbol[:-4] + '-' + symbol[-4:]  # BTC-USD format

        analyze_symbol(symbol)
    else:
        # Multi-symbol mode
        monitor_multiple_symbols(default_symbols)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\nAnalysis interrupted by user.")
        sys.exit(0)
    except Exception as e:
        logger.error(f"Fatal error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
