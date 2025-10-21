#!/usr/bin/env python3
"""
Multi-Symbol Isolated Margin Backtester
Tests MEM strategy across ALL major crypto tokens

Simulates trading on:
- 20+ major crypto futures pairs
- Same strategy settings for all
- Gathers performance data per symbol
- Identifies best performing tokens

Purpose: Determine which cryptos work best with MEM strategy
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List
import logging

from production_mem_strategy import FilterConfig

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('/root/AlgoTrendy_v2.6/MEM/multi_symbol.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger('MultiSymbol')


class MultiSymbolBacktester:
    """
    Backtest MEM strategy across multiple crypto symbols

    Uses BTC historical data as template and simulates similar
    patterns for other cryptos with realistic variations
    """

    # Major crypto futures pairs to test (35 tokens)
    SYMBOLS = [
        # Top 10 by Market Cap
        'BTCUSDT',   # Bitcoin
        'ETHUSDT',   # Ethereum
        'BNBUSDT',   # Binance Coin
        'SOLUSDT',   # Solana
        'XRPUSDT',   # Ripple
        'ADAUSDT',   # Cardano
        'DOGEUSDT',  # Dogecoin ← Included
        'MATICUSDT', # Polygon
        'DOTUSDT',   # Polkadot
        'AVAXUSDT',  # Avalanche

        # DeFi Tokens
        'LINKUSDT',  # Chainlink
        'UNIUSDT',   # Uniswap
        'ATOMUSDT',  # Cosmos
        'LTCUSDT',   # Litecoin
        'ETCUSDT',   # Ethereum Classic

        # Layer 1/2 Blockchains
        'NEARUSDT',  # Near Protocol
        'APTUSDT',   # Aptos
        'ARBUSDT',   # Arbitrum
        'OPUSDT',    # Optimism
        'SUIUSDT',   # Sui
        'SEIUSDT',   # Sei
        'TIAUSDT',   # Celestia

        # AI/ML Tokens ← For COAI interest
        'FETUSDT',   # Fetch.ai (AI)
        'RENDERUSDT',# Render (AI/GPU)
        'AGIXUSDT',  # SingularityNET (AI)
        'WLDUSDT',   # Worldcoin (AI)
        'GRTUSDT',   # The Graph

        # Meme/Community
        'SHIBUSDT',  # Shiba Inu
        'PEPEUSDT',  # Pepe

        # Other Major
        'INJUSDT',   # Injective
        'FILUSDT',   # Filecoin
        'THETAUSDT', # Theta
        'SANDUSDT',  # Sandbox
        'MANAUSDT',  # Decentraland
    ]

    # Symbol characteristics (relative to BTC)
    # volatility: how much more volatile than BTC (1.0 = same, 2.0 = 2x)
    # liquidity: relative trading volume (1.0 = BTC level, 0.5 = half)
    # correlation: how closely follows BTC (1.0 = perfect, 0.5 = independent)
    SYMBOL_PROFILES = {
        # Top tier (highest liquidity)
        'BTCUSDT': {'volatility': 1.0, 'liquidity': 1.0, 'correlation': 1.0},
        'ETHUSDT': {'volatility': 1.2, 'liquidity': 0.9, 'correlation': 0.95},
        'BNBUSDT': {'volatility': 1.3, 'liquidity': 0.7, 'correlation': 0.85},
        'SOLUSDT': {'volatility': 1.8, 'liquidity': 0.6, 'correlation': 0.80},
        'XRPUSDT': {'volatility': 1.5, 'liquidity': 0.8, 'correlation': 0.75},

        # Mid tier
        'ADAUSDT': {'volatility': 1.4, 'liquidity': 0.7, 'correlation': 0.80},
        'DOGEUSDT': {'volatility': 2.0, 'liquidity': 0.6, 'correlation': 0.70},
        'MATICUSDT': {'volatility': 1.6, 'liquidity': 0.6, 'correlation': 0.75},
        'DOTUSDT': {'volatility': 1.5, 'liquidity': 0.6, 'correlation': 0.78},
        'AVAXUSDT': {'volatility': 1.7, 'liquidity': 0.6, 'correlation': 0.80},
        'LINKUSDT': {'volatility': 1.6, 'liquidity': 0.5, 'correlation': 0.75},
        'UNIUSDT': {'volatility': 1.7, 'liquidity': 0.5, 'correlation': 0.72},
        'ATOMUSDT': {'volatility': 1.5, 'liquidity': 0.5, 'correlation': 0.75},
        'LTCUSDT': {'volatility': 1.3, 'liquidity': 0.7, 'correlation': 0.85},
        'ETCUSDT': {'volatility': 1.6, 'liquidity': 0.4, 'correlation': 0.70},

        # Layer 1/2 (newer, higher volatility)
        'NEARUSDT': {'volatility': 1.8, 'liquidity': 0.5, 'correlation': 0.72},
        'APTUSDT': {'volatility': 2.0, 'liquidity': 0.5, 'correlation': 0.68},
        'ARBUSDT': {'volatility': 1.9, 'liquidity': 0.5, 'correlation': 0.70},
        'OPUSDT': {'volatility': 1.9, 'liquidity': 0.5, 'correlation': 0.70},
        'SUIUSDT': {'volatility': 2.2, 'liquidity': 0.5, 'correlation': 0.68},
        'SEIUSDT': {'volatility': 2.1, 'liquidity': 0.4, 'correlation': 0.65},
        'TIAUSDT': {'volatility': 2.3, 'liquidity': 0.4, 'correlation': 0.65},

        # AI/ML tokens (higher volatility, lower correlation)
        'FETUSDT': {'volatility': 2.0, 'liquidity': 0.4, 'correlation': 0.65},
        'RENDERUSDT': {'volatility': 2.4, 'liquidity': 0.4, 'correlation': 0.60},
        'AGIXUSDT': {'volatility': 2.2, 'liquidity': 0.3, 'correlation': 0.60},
        'WLDUSDT': {'volatility': 2.5, 'liquidity': 0.4, 'correlation': 0.58},
        'GRTUSDT': {'volatility': 1.9, 'liquidity': 0.4, 'correlation': 0.68},

        # Meme coins (very high volatility, low correlation)
        'SHIBUSDT': {'volatility': 2.5, 'liquidity': 0.5, 'correlation': 0.60},
        'PEPEUSDT': {'volatility': 3.0, 'liquidity': 0.4, 'correlation': 0.55},

        # Other
        'INJUSDT': {'volatility': 2.2, 'liquidity': 0.4, 'correlation': 0.65},
        'FILUSDT': {'volatility': 1.8, 'liquidity': 0.4, 'correlation': 0.70},
        'THETAUSDT': {'volatility': 1.9, 'liquidity': 0.3, 'correlation': 0.65},
        'SANDUSDT': {'volatility': 2.1, 'liquidity': 0.4, 'correlation': 0.63},
        'MANAUSDT': {'volatility': 2.0, 'liquidity': 0.3, 'correlation': 0.63},
    }

    def __init__(
        self,
        initial_capital: float = 10000.0,
        margin_pct: float = 0.01,
        leverage: int = 25,
        data_file: str = 'trade_indicators.csv'
    ):
        self.initial_capital = initial_capital
        self.current_capital = initial_capital
        self.margin_pct = margin_pct
        self.leverage = leverage

        # Load BTC data as template
        self.btc_data = pd.read_csv(data_file)
        self.btc_data['entry_time'] = pd.to_datetime(self.btc_data['entry_time'])
        self.btc_data['exit_time'] = pd.to_datetime(self.btc_data['exit_time'])

        # Strategy config
        self.config = FilterConfig(
            min_confidence=0.72,
            min_movement_pct=5.0,
            use_roc_filter=False
        )

        # Results per symbol
        self.symbol_results = {symbol: [] for symbol in self.SYMBOLS}
        self.symbol_stats = {}

        logger.info("="*100)
        logger.info("🌐 MULTI-SYMBOL ISOLATED MARGIN BACKTESTER")
        logger.info("="*100)
        logger.info(f"Initial Capital: ${initial_capital:,.2f}")
        logger.info(f"Margin per Trade: {margin_pct:.1%}")
        logger.info(f"Leverage: {leverage}x isolated")
        logger.info(f"Symbols to Test: {len(self.SYMBOLS)}")
        logger.info(f"Strategy: {self.config.min_confidence:.0%} confidence + {self.config.min_movement_pct}% movement")
        logger.info("="*100)

    def simulate_symbol_data(self, symbol: str) -> pd.DataFrame:
        """
        Simulate historical data for a symbol based on BTC template

        Applies symbol-specific characteristics:
        - Volatility multiplier (affects movement size)
        - Correlation (how closely it follows BTC)
        - Opportunity frequency (more/less trades than BTC)
        """
        profile = self.SYMBOL_PROFILES.get(symbol, {'volatility': 1.0, 'liquidity': 1.0, 'correlation': 1.0})

        df = self.btc_data.copy()

        # Adjust based on symbol characteristics
        np.random.seed(hash(symbol) % 2**32)  # Deterministic per symbol

        for idx in range(len(df)):
            # Correlation: how much this crypto follows BTC
            correlation = profile['correlation']
            independent_noise = np.random.normal(0, 0.3) * (1 - correlation)

            # Volatility: adjust movement size
            volatility = profile['volatility']
            df.loc[df.index[idx], 'pnl_pct'] = df.iloc[idx]['pnl_pct'] * volatility + independent_noise

            # Confidence: slightly adjust based on correlation
            # Higher correlation = MEM might be more confident
            conf_adjustment = (correlation - 0.75) * 0.1  # -0.05 to +0.05
            new_conf = df.iloc[idx]['mem_confidence'] + conf_adjustment
            df.loc[df.index[idx], 'mem_confidence'] = np.clip(new_conf, 0.6, 0.85)

            # Liquidity affects opportunity - lower liquidity = fewer trades pass filters
            if profile['liquidity'] < 0.7:
                if np.random.random() > profile['liquidity']:
                    # Skip this opportunity
                    df.loc[df.index[idx], 'mem_confidence'] = 0.50  # Will fail filter

        return df

    def check_filters(self, trade_data: pd.Series) -> bool:
        """Check if trade passes strategy filters"""
        if trade_data['mem_confidence'] < self.config.min_confidence:
            return False
        if abs(trade_data['pnl_pct']) < self.config.min_movement_pct:
            return False
        return True

    def calculate_pnl(self, trade_data: pd.Series, margin_amount: float) -> Dict:
        """Calculate PnL for isolated margin trade"""
        position_value = margin_amount * self.leverage
        price_move_pct = trade_data['pnl_pct']
        pnl_usd = position_value * (price_move_pct / 100)
        roi_on_margin = (pnl_usd / margin_amount) * 100

        # Liquidation check
        liquidation_threshold = 100 / self.leverage
        liquidated = False
        if abs(price_move_pct) >= liquidation_threshold and pnl_usd < 0:
            pnl_usd = -margin_amount
            roi_on_margin = -100
            liquidated = True

        return {
            'margin': margin_amount,
            'position_value': position_value,
            'price_move_pct': price_move_pct,
            'pnl_usd': pnl_usd,
            'roi_on_margin': roi_on_margin,
            'liquidated': liquidated
        }

    def run_backtest_for_symbol(self, symbol: str):
        """Run backtest for a single symbol"""
        logger.info(f"\n{'='*100}")
        logger.info(f"📊 Testing {symbol}")
        logger.info(f"{'='*100}")

        # Simulate data for this symbol
        symbol_data = self.simulate_symbol_data(symbol)

        trades = []
        capital = self.initial_capital

        for idx, trade_data in symbol_data.iterrows():
            if not self.check_filters(trade_data):
                continue

            margin_amount = capital * self.margin_pct
            result = self.calculate_pnl(trade_data, margin_amount)

            capital += result['pnl_usd']

            trade_record = {
                'symbol': symbol,
                'timestamp': trade_data['entry_time'],
                'confidence': trade_data['mem_confidence'],
                'price_move_pct': result['price_move_pct'],
                'margin': result['margin'],
                'pnl_usd': result['pnl_usd'],
                'roi_on_margin': result['roi_on_margin'],
                'liquidated': result['liquidated'],
                'capital_after': capital
            }

            trades.append(trade_record)

        self.symbol_results[symbol] = trades

        # Calculate stats
        if trades:
            df = pd.DataFrame(trades)
            wins = len(df[df['pnl_usd'] > 0])
            losses = len(df[df['pnl_usd'] <= 0])
            total_pnl = df['pnl_usd'].sum()
            win_rate = (wins / len(df)) * 100 if len(df) > 0 else 0
            avg_pnl = df['pnl_usd'].mean()
            total_return = ((capital - self.initial_capital) / self.initial_capital) * 100
            liquidations = len(df[df['liquidated'] == True])

            self.symbol_stats[symbol] = {
                'trades': len(df),
                'wins': wins,
                'losses': losses,
                'win_rate': win_rate,
                'total_pnl': total_pnl,
                'avg_pnl': avg_pnl,
                'total_return': total_return,
                'liquidations': liquidations,
                'final_capital': capital
            }

            logger.info(f"✅ {symbol} Results:")
            logger.info(f"   Trades: {len(df)}")
            logger.info(f"   Win Rate: {win_rate:.1f}%")
            logger.info(f"   Total PnL: ${total_pnl:+,.2f}")
            logger.info(f"   Total Return: {total_return:+.2f}%")
            logger.info(f"   Liquidations: {liquidations}")
        else:
            logger.info(f"⚠️  {symbol}: No trades passed filters")
            self.symbol_stats[symbol] = {
                'trades': 0,
                'wins': 0,
                'losses': 0,
                'win_rate': 0,
                'total_pnl': 0,
                'avg_pnl': 0,
                'total_return': 0,
                'liquidations': 0,
                'final_capital': self.initial_capital
            }

    def run_all_symbols(self):
        """Run backtest for all symbols"""
        logger.info(f"\n🚀 Running backtest across {len(self.SYMBOLS)} symbols...\n")

        for symbol in self.SYMBOLS:
            self.run_backtest_for_symbol(symbol)

    def print_summary(self):
        """Print comprehensive summary"""
        logger.info("\n" + "="*100)
        logger.info("📊 MULTI-SYMBOL BACKTEST SUMMARY")
        logger.info("="*100)

        # Sort by total return
        sorted_symbols = sorted(
            self.symbol_stats.items(),
            key=lambda x: x[1]['total_return'],
            reverse=True
        )

        logger.info(f"\n{'Symbol':<12} {'Trades':<8} {'Win Rate':<10} {'Total PnL':<15} {'Return':<12} {'Liq':<5}")
        logger.info("-"*100)

        total_trades = 0
        total_wins = 0
        total_losses = 0

        for symbol, stats in sorted_symbols:
            if stats['trades'] > 0:
                status = "✅" if stats['total_return'] > 0 else "❌"
                logger.info(
                    f"{status} {symbol:<10} {stats['trades']:<8} {stats['win_rate']:<9.1f}% "
                    f"${stats['total_pnl']:>12,.2f}  {stats['total_return']:>10.2f}%  {stats['liquidations']:<5}"
                )
                total_trades += stats['trades']
                total_wins += stats['wins']
                total_losses += stats['losses']

        # Overall stats
        logger.info("\n" + "="*100)
        logger.info("📈 OVERALL PERFORMANCE")
        logger.info("="*100)

        total_pnl_all = sum(s['total_pnl'] for s in self.symbol_stats.values())
        avg_return = np.mean([s['total_return'] for s in self.symbol_stats.values() if s['trades'] > 0])
        profitable_symbols = len([s for s in self.symbol_stats.values() if s['total_return'] > 0])

        logger.info(f"\nTotal Trades Across All Symbols: {total_trades}")
        logger.info(f"Total Wins: {total_wins} ({total_wins/total_trades*100:.1f}%)" if total_trades > 0 else "Total Wins: 0")
        logger.info(f"Total Losses: {total_losses}")
        logger.info(f"Combined PnL (if traded all): ${total_pnl_all:+,.2f}")
        logger.info(f"Average Return per Symbol: {avg_return:+.2f}%")
        logger.info(f"Profitable Symbols: {profitable_symbols}/{len(self.SYMBOLS)}")

        # Top performers
        logger.info("\n" + "="*100)
        logger.info("🏆 TOP 5 PERFORMING SYMBOLS")
        logger.info("="*100)

        top_5 = sorted_symbols[:5]
        for i, (symbol, stats) in enumerate(top_5, 1):
            if stats['trades'] > 0:
                logger.info(f"\n{i}. {symbol}")
                logger.info(f"   Trades: {stats['trades']}")
                logger.info(f"   Win Rate: {stats['win_rate']:.1f}%")
                logger.info(f"   Total Return: {stats['total_return']:+.2f}%")
                logger.info(f"   Avg PnL per Trade: ${stats['avg_pnl']:+.2f}")

        # Bottom performers
        logger.info("\n" + "="*100)
        logger.info("⚠️  BOTTOM 5 PERFORMING SYMBOLS")
        logger.info("="*100)

        bottom_5 = sorted_symbols[-5:][::-1]
        for i, (symbol, stats) in enumerate(bottom_5, 1):
            if stats['trades'] > 0:
                logger.info(f"\n{i}. {symbol}")
                logger.info(f"   Trades: {stats['trades']}")
                logger.info(f"   Win Rate: {stats['win_rate']:.1f}%")
                logger.info(f"   Total Return: {stats['total_return']:+.2f}%")
                logger.info(f"   Avg PnL per Trade: ${stats['avg_pnl']:+.2f}")

        # Save results
        self._save_results()

        logger.info("\n" + "="*100)

    def _save_results(self):
        """Save results to CSV"""
        # Summary stats
        summary_df = pd.DataFrame.from_dict(self.symbol_stats, orient='index')
        summary_df.index.name = 'symbol'
        summary_df = summary_df.sort_values('total_return', ascending=False)
        summary_df.to_csv('multi_symbol_summary.csv')

        # All trades
        all_trades = []
        for symbol, trades in self.symbol_results.items():
            all_trades.extend(trades)

        if all_trades:
            trades_df = pd.DataFrame(all_trades)
            trades_df.to_csv('multi_symbol_trades.csv', index=False)

        logger.info(f"\n💾 Results saved:")
        logger.info(f"   multi_symbol_summary.csv - Performance by symbol")
        logger.info(f"   multi_symbol_trades.csv - All trades")

    def get_recommended_symbols(self, min_trades: int = 5, min_win_rate: float = 70.0) -> List[str]:
        """Get recommended symbols for live trading"""
        recommended = []

        for symbol, stats in self.symbol_stats.items():
            if stats['trades'] >= min_trades and stats['win_rate'] >= min_win_rate:
                recommended.append({
                    'symbol': symbol,
                    'trades': stats['trades'],
                    'win_rate': stats['win_rate'],
                    'return': stats['total_return']
                })

        # Sort by return
        recommended = sorted(recommended, key=lambda x: x['return'], reverse=True)

        return recommended


def main():
    """Run multi-symbol backtest"""
    backtester = MultiSymbolBacktester(
        initial_capital=10000.0,
        margin_pct=0.01,
        leverage=25
    )

    # Run backtest
    backtester.run_all_symbols()

    # Print summary
    backtester.print_summary()

    # Get recommendations
    recommended = backtester.get_recommended_symbols(min_trades=5, min_win_rate=70.0)

    logger.info("\n" + "="*100)
    logger.info("💡 RECOMMENDED SYMBOLS FOR LIVE TRADING")
    logger.info("="*100)
    logger.info(f"\nSymbols with >= 5 trades and >= 70% win rate:\n")

    if recommended:
        for i, rec in enumerate(recommended, 1):
            logger.info(f"{i}. {rec['symbol']}: {rec['trades']} trades, {rec['win_rate']:.1f}% WR, {rec['return']:+.2f}% return")
    else:
        logger.info("None found with current criteria")

    logger.info("\n" + "="*100)


if __name__ == '__main__':
    main()
