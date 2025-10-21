#!/usr/bin/env python3
"""
🔍 COMPREHENSIVE PATTERN ANALYSIS & EXPLOITABLE OPPORTUNITY DETECTION
Uses MEM ML models + real-time data to find trading opportunities
"""

import warnings
warnings.filterwarnings('ignore')

import os
import sys
import json
import joblib
import pandas as pd
import numpy as np
from datetime import datetime, timedelta
from pathlib import Path

# Try importing ML/data libraries
try:
    from sklearn.ensemble import GradientBoostingClassifier
    from sklearn.preprocessing import StandardScaler
    import yfinance as yf
    print("✅ All required libraries loaded")
except ImportError as e:
    print(f"❌ Missing library: {e}")
    print("Installing dependencies...")
    os.system("pip3 install -q scikit-learn yfinance joblib pandas numpy")
    from sklearn.ensemble import GradientBoostingClassifier
    from sklearn.preprocessing import StandardScaler
    import yfinance as yf


class PatternAnalyzer:
    """Comprehensive pattern detection and opportunity finder"""

    def __init__(self):
        self.model_dir = Path("/root/AlgoTrendy_v2.6/ml_models/trend_reversals/20251016_234123")
        self.model = None
        self.scaler = None
        self.symbols = ["BTC-USD", "ETH-USD", "BNB-USD", "XRP-USD", "SOL-USD", "ADA-USD"]
        self.crypto_names = {
            "BTC-USD": "BTCUSDT",
            "ETH-USD": "ETHUSDT",
            "BNB-USD": "BNBUSDT",
            "XRP-USD": "XRPUSDT",
            "SOL-USD": "SOLUSDT",
            "ADA-USD": "ADAUSDT"
        }

    def load_model(self):
        """Load trained ML model"""
        print("\n" + "="*70)
        print("🤖 LOADING ML MODEL")
        print("="*70)

        try:
            model_path = self.model_dir / "reversal_model.joblib"
            scaler_path = self.model_dir / "reversal_scaler.joblib"

            if model_path.exists():
                self.model = joblib.load(model_path)
                print(f"✅ Model loaded from {model_path}")
            else:
                print(f"⚠️  Model not found at {model_path}")
                return False

            if scaler_path.exists():
                self.scaler = joblib.load(scaler_path)
                print(f"✅ Scaler loaded from {scaler_path}")
            else:
                print(f"⚠️  Scaler not found, will create new one")
                self.scaler = StandardScaler()

            # Load config
            config_path = self.model_dir / "config.json"
            if config_path.exists():
                with open(config_path) as f:
                    config = json.load(f)
                print(f"✅ Model config loaded - Features: {len(config['features'])}")
                print(f"   Accuracy: {config['metrics']['accuracy']:.1%}")

            return True
        except Exception as e:
            print(f"❌ Error loading model: {e}")
            return False

    def fetch_market_data(self, symbol, period="30d", interval="5m"):
        """Fetch real-time market data from yfinance"""
        try:
            ticker = yf.Ticker(symbol)
            df = ticker.history(period=period, interval=interval)

            if df.empty:
                return None

            # Rename columns to lowercase
            df.columns = [c.lower() for c in df.columns]
            df = df.reset_index()

            return df
        except Exception as e:
            print(f"⚠️  Error fetching {symbol}: {e}")
            return None

    def calculate_features(self, df):
        """Calculate all technical indicators"""
        df = df.copy()

        # Ensure numeric types
        for col in ['open', 'high', 'low', 'close', 'volume']:
            df[col] = pd.to_numeric(df[col], errors='coerce')

        # Price features
        df['price_change'] = df['close'].pct_change()
        df['range'] = df['high'] - df['low']
        df['body_size'] = abs(df['close'] - df['open'])
        df['wick_up'] = df['high'] - df[['open', 'close']].max(axis=1)
        df['wick_down'] = df[['open', 'close']].min(axis=1) - df['low']
        df['close_position'] = (df['close'] - df['low']) / (df['high'] - df['low'] + 1e-10)

        # Moving averages
        df['sma_5'] = df['close'].rolling(5).mean()
        df['sma_20'] = df['close'].rolling(20).mean()
        df['price_vs_sma5'] = (df['close'] - df['sma_5']) / (df['sma_5'] + 1e-10)
        df['price_vs_sma20'] = (df['close'] - df['sma_20']) / (df['sma_20'] + 1e-10)

        # Momentum
        df['momentum_3'] = df['close'].pct_change(3)
        df['momentum_5'] = df['close'].pct_change(5)
        df['roc_5'] = df['close'].pct_change(5) * 100

        # Volatility
        df['volatility_3'] = df['close'].rolling(3).std()
        df['volatility_5'] = df['close'].rolling(5).std()
        df['volatility_10'] = df['close'].rolling(10).std()

        # RSI
        delta = df['close'].diff()
        gain = (delta.where(delta > 0, 0)).rolling(14).mean()
        loss = (-delta.where(delta < 0, 0)).rolling(14).mean()
        rs = gain / (loss + 1e-10)
        df['rsi'] = 100 - (100 / (1 + rs))

        # Volume
        df['volume_ratio'] = df['volume'] / (df['volume'].rolling(20).mean() + 1e-10)

        # Consecutive candles
        df['consecutive_up'] = (df['close'] > df['close'].shift(1)).astype(int).rolling(5).sum()
        df['consecutive_down'] = (df['close'] < df['close'].shift(1)).astype(int).rolling(5).sum()

        # Divergence detection
        df['bullish_divergence'] = ((df['rsi'] > df['rsi'].shift(5)) &
                                     (df['close'] < df['close'].shift(5))).astype(int)
        df['bearish_divergence'] = ((df['rsi'] < df['rsi'].shift(5)) &
                                     (df['close'] > df['close'].shift(5))).astype(int)

        # Trend strength
        df['trend_strength'] = abs(df['close'].rolling(20).apply(lambda x: np.polyfit(range(len(x)), x, 1)[0]))

        return df

    def predict_reversals(self, df):
        """Use ML model to predict trend reversals"""
        # Use original 12 features from retrain_model.py
        feature_cols = [
            'sma_5', 'sma_20', 'rsi', 'volume_ratio',
            'close_position', 'price_change', 'momentum_3', 'momentum_5',
            'volatility_3', 'volatility_5', 'range', 'body_size'
        ]

        # Prepare features
        df_clean = df[feature_cols].copy()
        df_clean = df_clean.replace([np.inf, -np.inf], 0)
        df_clean = df_clean.fillna(0)

        # Scale and predict
        X = df_clean.values

        if self.model:
            # Use fitted scaler if available
            try:
                X_scaled = self.scaler.transform(X)
            except:
                # Fit new scaler if transform fails
                X_scaled = self.scaler.fit_transform(X)

            predictions = self.model.predict(X_scaled)
            probabilities = self.model.predict_proba(X_scaled)

            df['reversal_prediction'] = predictions
            df['reversal_confidence'] = probabilities[:, 1] if probabilities.shape[1] > 1 else probabilities[:, 0]
        else:
            df['reversal_prediction'] = 0
            df['reversal_confidence'] = 0.0

        return df

    def detect_patterns(self, df):
        """Detect common trading patterns"""
        patterns = []

        # Get last 50 candles for analysis
        recent = df.tail(50)
        latest = df.iloc[-1]

        # PATTERN 1: High-confidence reversal
        if latest['reversal_confidence'] > 0.7:
            patterns.append({
                'type': 'ML_REVERSAL',
                'confidence': latest['reversal_confidence'],
                'signal': 'BUY' if latest['rsi'] < 40 else 'SELL',
                'reason': f"ML model predicts reversal with {latest['reversal_confidence']:.1%} confidence"
            })

        # PATTERN 2: RSI Divergence
        if latest['bullish_divergence'] > 0 and latest['rsi'] < 35:
            patterns.append({
                'type': 'BULLISH_DIVERGENCE',
                'confidence': 0.75,
                'signal': 'BUY',
                'reason': 'RSI bullish divergence detected - price down but RSI up'
            })

        if latest['bearish_divergence'] > 0 and latest['rsi'] > 65:
            patterns.append({
                'type': 'BEARISH_DIVERGENCE',
                'confidence': 0.75,
                'signal': 'SELL',
                'reason': 'RSI bearish divergence detected - price up but RSI down'
            })

        # PATTERN 3: Volume spike + momentum
        if latest['volume_ratio'] > 2.0 and abs(latest['momentum_5']) > 0.03:
            patterns.append({
                'type': 'VOLUME_BREAKOUT',
                'confidence': 0.70,
                'signal': 'BUY' if latest['momentum_5'] > 0 else 'SELL',
                'reason': f"Volume spike {latest['volume_ratio']:.1f}x with strong momentum"
            })

        # PATTERN 4: Oversold/Overbought with volatility
        if latest['rsi'] < 25 and latest['volatility_5'] > df['volatility_5'].quantile(0.75):
            patterns.append({
                'type': 'OVERSOLD_REVERSAL',
                'confidence': 0.68,
                'signal': 'BUY',
                'reason': f"Heavily oversold (RSI {latest['rsi']:.0f}) with high volatility"
            })

        if latest['rsi'] > 75 and latest['volatility_5'] > df['volatility_5'].quantile(0.75):
            patterns.append({
                'type': 'OVERBOUGHT_REVERSAL',
                'confidence': 0.68,
                'signal': 'SELL',
                'reason': f"Heavily overbought (RSI {latest['rsi']:.0f}) with high volatility"
            })

        # PATTERN 5: Momentum + consecutive candles
        if latest['consecutive_up'] >= 4 and latest['momentum_3'] > 0.05:
            patterns.append({
                'type': 'STRONG_UPTREND',
                'confidence': 0.72,
                'signal': 'BUY',
                'reason': f"{int(latest['consecutive_up'])} consecutive up candles with momentum"
            })

        if latest['consecutive_down'] >= 4 and latest['momentum_3'] < -0.05:
            patterns.append({
                'type': 'STRONG_DOWNTREND',
                'confidence': 0.72,
                'signal': 'SELL',
                'reason': f"{int(latest['consecutive_down'])} consecutive down candles with momentum"
            })

        return patterns

    def analyze_symbol(self, symbol):
        """Complete analysis for one symbol"""
        print(f"\n{'─'*70}")
        print(f"📊 Analyzing {symbol} ({self.crypto_names.get(symbol, symbol)})")
        print(f"{'─'*70}")

        # Fetch data
        df = self.fetch_market_data(symbol)
        if df is None or len(df) < 100:
            print(f"⚠️  Insufficient data for {symbol}")
            return None

        print(f"✅ Fetched {len(df)} candles (5m interval)")

        # Calculate features
        df = self.calculate_features(df)
        df = df.dropna()

        if len(df) < 50:
            print(f"⚠️  Not enough data after feature calculation")
            return None

        # Predict reversals
        df = self.predict_reversals(df)

        # Detect patterns
        patterns = self.detect_patterns(df)

        # Get latest stats
        latest = df.iloc[-1]

        result = {
            'symbol': self.crypto_names.get(symbol, symbol),
            'price': float(latest['close']),
            'rsi': float(latest['rsi']),
            'volume_ratio': float(latest['volume_ratio']),
            'momentum_5': float(latest['momentum_5']),
            'volatility_5': float(latest['volatility_5']),
            'reversal_confidence': float(latest['reversal_confidence']),
            'patterns': patterns
        }

        return result

    def generate_report(self, results):
        """Generate comprehensive trading report"""
        print("\n" + "█"*70)
        print("█ 🎯 EXPLOITABLE TRADING OPPORTUNITIES - PATTERN ANALYSIS REPORT")
        print("█"*70)
        print(f"█ Timestamp: {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}")
        print(f"█ Symbols Analyzed: {len([r for r in results if r])}")
        print("█"*70)

        # Sort by opportunity score
        opportunities = []
        for result in results:
            if not result or not result['patterns']:
                continue

            # Calculate opportunity score
            max_confidence = max([p['confidence'] for p in result['patterns']], default=0)
            num_patterns = len(result['patterns'])
            score = max_confidence * (1 + 0.1 * num_patterns)

            opportunities.append({
                **result,
                'opportunity_score': score,
                'num_patterns': num_patterns,
                'max_confidence': max_confidence
            })

        opportunities.sort(key=lambda x: x['opportunity_score'], reverse=True)

        if not opportunities:
            print("\n⚠️  No high-confidence patterns detected at this time")
            print("   Market may be in consolidation or trending without clear signals")
            return

        # TOP OPPORTUNITIES
        print("\n" + "="*70)
        print("🚀 TOP TRADING OPPORTUNITIES (Ranked by Confidence)")
        print("="*70)

        for i, opp in enumerate(opportunities[:5], 1):
            print(f"\n#{i} {opp['symbol']} - Score: {opp['opportunity_score']:.2f}")
            print(f"   Price: ${opp['price']:,.2f}")
            print(f"   RSI: {opp['rsi']:.1f} | Volume: {opp['volume_ratio']:.1f}x | Momentum: {opp['momentum_5']:+.2%}")
            print(f"   ML Reversal Confidence: {opp['reversal_confidence']:.1%}")
            print(f"   Detected Patterns: {opp['num_patterns']}")

            for j, pattern in enumerate(opp['patterns'], 1):
                emoji = "🟢" if pattern['signal'] == 'BUY' else "🔴"
                print(f"      {emoji} {pattern['type']} - {pattern['signal']}")
                print(f"         Confidence: {pattern['confidence']:.1%}")
                print(f"         Reason: {pattern['reason']}")

        # MARKET OVERVIEW
        print("\n" + "="*70)
        print("📈 MARKET OVERVIEW - ALL SYMBOLS")
        print("="*70)

        for result in results:
            if not result:
                continue

            status = "🟢 OPPORTUNITY" if result['patterns'] else "⚪ NEUTRAL"
            print(f"\n{status} {result['symbol']}")
            print(f"   Price: ${result['price']:,.2f} | RSI: {result['rsi']:.1f} | "
                  f"Vol: {result['volume_ratio']:.1f}x | Mom: {result['momentum_5']:+.2%}")

            if result['patterns']:
                signals = [p['signal'] for p in result['patterns']]
                buy_count = signals.count('BUY')
                sell_count = signals.count('SELL')
                print(f"   Signals: {buy_count} BUY, {sell_count} SELL")

        # PATTERN STATISTICS
        print("\n" + "="*70)
        print("📊 PATTERN STATISTICS")
        print("="*70)

        all_patterns = {}
        for opp in opportunities:
            for pattern in opp['patterns']:
                ptype = pattern['type']
                if ptype not in all_patterns:
                    all_patterns[ptype] = {'count': 0, 'avg_conf': [], 'signals': []}
                all_patterns[ptype]['count'] += 1
                all_patterns[ptype]['avg_conf'].append(pattern['confidence'])
                all_patterns[ptype]['signals'].append(pattern['signal'])

        for ptype, stats in sorted(all_patterns.items(), key=lambda x: x[1]['count'], reverse=True):
            avg_conf = np.mean(stats['avg_conf'])
            buy_signals = stats['signals'].count('BUY')
            sell_signals = stats['signals'].count('SELL')
            print(f"\n   {ptype}:")
            print(f"      Occurrences: {stats['count']}")
            print(f"      Avg Confidence: {avg_conf:.1%}")
            print(f"      Signals: {buy_signals} BUY, {sell_signals} SELL")

        # RECOMMENDATIONS
        print("\n" + "="*70)
        print("💡 TRADING RECOMMENDATIONS")
        print("="*70)

        if opportunities:
            top = opportunities[0]
            print(f"\n🎯 HIGHEST CONFIDENCE TRADE:")
            print(f"   Symbol: {top['symbol']}")
            print(f"   Entry: ${top['price']:,.2f}")

            buy_patterns = [p for p in top['patterns'] if p['signal'] == 'BUY']
            sell_patterns = [p for p in top['patterns'] if p['signal'] == 'SELL']

            if buy_patterns:
                avg_conf = np.mean([p['confidence'] for p in buy_patterns])
                print(f"   Direction: LONG (BUY)")
                print(f"   Confidence: {avg_conf:.1%}")
                print(f"   Stop Loss: ${top['price'] * 0.98:,.2f} (-2%)")
                print(f"   Take Profit: ${top['price'] * 1.03:,.2f} (+3%)")
            elif sell_patterns:
                avg_conf = np.mean([p['confidence'] for p in sell_patterns])
                print(f"   Direction: SHORT (SELL)")
                print(f"   Confidence: {avg_conf:.1%}")
                print(f"   Stop Loss: ${top['price'] * 1.02:,.2f} (+2%)")
                print(f"   Take Profit: ${top['price'] * 0.97:,.2f} (-3%)")

        print("\n" + "="*70)
        print("⚠️  RISK DISCLAIMER")
        print("="*70)
        print("This analysis is for educational purposes only.")
        print("Always do your own research and manage risk appropriately.")
        print("Past performance does not guarantee future results.")
        print("="*70)

        # Save report
        report_path = Path("/root/AlgoTrendy_v2.6/pattern_analysis_report.json")
        with open(report_path, 'w') as f:
            json.dump({
                'timestamp': datetime.now().isoformat(),
                'opportunities': opportunities,
                'all_results': results
            }, f, indent=2, default=str)

        print(f"\n💾 Report saved to: {report_path}")

    def run(self):
        """Execute complete analysis pipeline"""
        print("\n" + "█"*70)
        print("█ 🔍 MEM PATTERN ANALYSIS ENGINE")
        print("█ Powered by Machine Learning + Real-Time Market Data")
        print("█"*70)

        # Load ML model
        if not self.load_model():
            print("⚠️  Continuing without ML model - pattern-based analysis only")

        # Analyze all symbols
        print("\n" + "="*70)
        print("📡 FETCHING LIVE MARKET DATA")
        print("="*70)

        results = []
        for symbol in self.symbols:
            result = self.analyze_symbol(symbol)
            if result:
                results.append(result)

        # Generate report
        self.generate_report(results)

        print("\n✅ Analysis complete!")


if __name__ == "__main__":
    analyzer = PatternAnalyzer()
    analyzer.run()
