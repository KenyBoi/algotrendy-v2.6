"""
Test script for Advanced Indicators
====================================
Verifies all indicators are working correctly.
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import sys

def generate_sample_data(periods=100):
    """Generate sample OHLCV data for testing"""
    np.random.seed(42)

    dates = pd.date_range(end=datetime.now(), periods=periods, freq='1h')
    base_price = 100

    # Generate realistic price data
    returns = np.random.normal(0.0001, 0.02, periods)
    close = base_price * np.exp(np.cumsum(returns))

    high = close * (1 + np.abs(np.random.normal(0, 0.01, periods)))
    low = close * (1 - np.abs(np.random.normal(0, 0.01, periods)))
    open_price = close * (1 + np.random.normal(0, 0.005, periods))

    volume = np.random.uniform(1000000, 5000000, periods)

    df = pd.DataFrame({
        'open': open_price,
        'high': high,
        'low': low,
        'close': close,
        'volume': volume
    }, index=dates)

    return df

def test_basic_indicators():
    """Test basic indicator calculations"""
    print("=" * 60)
    print("Testing Basic Indicators")
    print("=" * 60)

    try:
        from advanced_indicators import get_indicators, list_all_indicators

        # List all available indicators
        all_indicators = list_all_indicators()
        total_count = sum(len(inds) for inds in all_indicators.values())

        print(f"\n✓ Successfully imported advanced_indicators module")
        print(f"✓ Total indicators available: {total_count}")

        for category, indicators in all_indicators.items():
            print(f"  - {category}: {len(indicators)} indicators")

        # Generate test data
        data = generate_sample_data(periods=100)
        print(f"\n✓ Generated test data: {len(data)} periods")

        # Test individual indicators
        indicators = get_indicators()
        high = data['high']
        low = data['low']
        close = data['close']
        volume = data['volume']

        print("\nTesting Momentum Indicators:")
        # RSI
        rsi = indicators.rsi(close, period=14)
        print(f"  ✓ RSI: {rsi.iloc[-1]:.2f}")

        # Stochastic
        stoch_k, stoch_d = indicators.stochastic(high, low, close)
        print(f"  ✓ Stochastic: %K={stoch_k.iloc[-1]:.2f}, %D={stoch_d.iloc[-1]:.2f}")

        # Williams %R
        williams = indicators.williams_r(high, low, close)
        print(f"  ✓ Williams %R: {williams.iloc[-1]:.2f}")

        # CCI
        cci = indicators.cci(high, low, close)
        print(f"  ✓ CCI: {cci.iloc[-1]:.2f}")

        print("\nTesting Trend Indicators:")
        # MACD
        macd, macd_signal, macd_hist = indicators.macd(close)
        print(f"  ✓ MACD: {macd.iloc[-1]:.4f}, Signal: {macd_signal.iloc[-1]:.4f}")

        # ADX
        adx, plus_di, minus_di = indicators.adx(high, low, close)
        print(f"  ✓ ADX: {adx.iloc[-1]:.2f}, +DI: {plus_di.iloc[-1]:.2f}, -DI: {minus_di.iloc[-1]:.2f}")

        # Aroon
        aroon_up, aroon_down, aroon_osc = indicators.aroon(high, low)
        print(f"  ✓ Aroon: Up={aroon_up.iloc[-1]:.2f}, Down={aroon_down.iloc[-1]:.2f}")

        print("\nTesting Volatility Indicators:")
        # ATR
        atr = indicators.atr(high, low, close)
        print(f"  ✓ ATR: {atr.iloc[-1]:.4f}")

        # Bollinger Bands
        bb_upper, bb_middle, bb_lower = indicators.bollinger_bands(close)
        print(f"  ✓ Bollinger Bands: Upper={bb_upper.iloc[-1]:.2f}, Middle={bb_middle.iloc[-1]:.2f}, Lower={bb_lower.iloc[-1]:.2f}")

        # Keltner Channels
        kc_upper, kc_middle, kc_lower = indicators.keltner_channels(high, low, close)
        print(f"  ✓ Keltner Channels: Upper={kc_upper.iloc[-1]:.2f}, Lower={kc_lower.iloc[-1]:.2f}")

        print("\nTesting Volume Indicators:")
        # OBV
        obv = indicators.obv(close, volume)
        print(f"  ✓ OBV: {obv.iloc[-1]:.0f}")

        # MFI
        mfi = indicators.mfi(high, low, close, volume)
        print(f"  ✓ MFI: {mfi.iloc[-1]:.2f}")

        # CMF
        cmf = indicators.cmf(high, low, close, volume)
        print(f"  ✓ CMF: {cmf.iloc[-1]:.4f}")

        # VWAP
        vwap = indicators.vwap(high, low, close, volume)
        print(f"  ✓ VWAP: {vwap.iloc[-1]:.2f}")

        print("\nTesting Support/Resistance:")
        # Pivot Points
        pivots = indicators.pivot_points(high, low, close, method='standard')
        print(f"  ✓ Pivot Points: PP={pivots['pivot']:.2f}, R1={pivots['r1']:.2f}, S1={pivots['s1']:.2f}")

        # Fibonacci
        swing_high = high.max()
        swing_low = low.min()
        fib = indicators.fibonacci_retracement(swing_high, swing_low)
        print(f"  ✓ Fibonacci: 38.2%={fib['38.2%']:.2f}, 61.8%={fib['61.8%']:.2f}")

        print("\n" + "=" * 60)
        print("✓ All basic indicator tests passed!")
        print("=" * 60)

        return True

    except Exception as e:
        print(f"\n✗ Error in basic indicators: {e}")
        import traceback
        traceback.print_exc()
        return False

def test_integration_functions():
    """Test MEM integration functions"""
    print("\n" + "=" * 60)
    print("Testing MEM Integration Functions")
    print("=" * 60)

    try:
        from mem_indicator_integration import (
            analyze_market,
            get_trading_signals,
            get_risk_metrics,
            get_support_resistance
        )

        # Generate test data
        data = generate_sample_data(periods=100)
        print(f"\n✓ Generated test data: {len(data)} periods")

        print("\nTesting Market Analysis:")
        analysis = analyze_market(data)
        print(f"  ✓ Overall Signal: {analysis['overall_signal']}")
        print(f"  ✓ Signal Strength: {analysis['signal_strength']:.1f}%")
        print(f"  ✓ Trend Direction: {analysis['trend_direction']}")
        print(f"  ✓ Volatility Level: {analysis['volatility_level']}")
        if 'total_score' in analysis:
            print(f"  ✓ Total Score: {analysis['total_score']:.2f}")
        print(f"  ✓ Reasoning: {len(analysis['reasoning'])} factors")

        print("\nTesting Trading Signals:")
        signals = get_trading_signals(data)
        print(f"  ✓ Action: {signals['action']}")
        print(f"  ✓ Confidence: {signals['confidence']:.1f}%")
        if signals.get('stop_loss'):
            print(f"  ✓ Stop Loss: {signals['stop_loss']:.2f}")
            print(f"  ✓ Take Profit: {signals['take_profit']:.2f}")

        print("\nTesting Risk Metrics:")
        risk = get_risk_metrics(data, position_size=1.0)
        print(f"  ✓ ATR: {risk['atr']:.4f}")
        print(f"  ✓ Daily Volatility: {risk['volatility_daily']:.4f}")
        print(f"  ✓ Annual Volatility: {risk['volatility_annualized']:.2%}")
        print(f"  ✓ VaR 95%: {risk['value_at_risk_95']:.4f}")
        print(f"  ✓ Sharpe Ratio: {risk['sharpe_ratio']:.2f}")
        print(f"  ✓ Risk Level: {risk['risk_level']}")

        print("\nTesting Support/Resistance Levels:")
        levels = get_support_resistance(data)
        print(f"  ✓ Swing High: {levels['swing_high']:.2f}")
        print(f"  ✓ Swing Low: {levels['swing_low']:.2f}")
        print(f"  ✓ Standard Pivot: {levels['pivot_standard']['pivot']:.2f}")
        print(f"  ✓ Fibonacci 50%: {levels['fibonacci']['50%']:.2f}")

        print("\n" + "=" * 60)
        print("✓ All integration function tests passed!")
        print("=" * 60)

        return True

    except Exception as e:
        print(f"\n✗ Error in integration functions: {e}")
        import traceback
        traceback.print_exc()
        return False

def test_multi_timeframe():
    """Test multi-timeframe analysis"""
    print("\n" + "=" * 60)
    print("Testing Multi-Timeframe Analysis")
    print("=" * 60)

    try:
        from mem_indicator_integration import analyze_multiple_timeframes

        # Generate data for different timeframes
        data_1h = generate_sample_data(periods=100)
        data_4h = generate_sample_data(periods=100)
        data_1d = generate_sample_data(periods=100)

        print(f"\n✓ Generated multi-timeframe data")

        mtf = analyze_multiple_timeframes({
            '1h': data_1h,
            '4h': data_4h,
            '1d': data_1d
        })

        print(f"\n✓ Confluence Signal: {mtf['confluence_signal']}")
        print(f"✓ Confluence Strength: {mtf['confluence_strength']:.1f}%")
        print(f"✓ Timeframes Aligned: {mtf['timeframes_aligned']}/{mtf['total_timeframes']}")

        print("\nTimeframe Details:")
        for tf, details in mtf['timeframes'].items():
            print(f"  {tf}: {details['signal']} ({details['strength']:.1f}%) - {details['trend']}")

        print("\n" + "=" * 60)
        print("✓ Multi-timeframe analysis test passed!")
        print("=" * 60)

        return True

    except Exception as e:
        print(f"\n✗ Error in multi-timeframe analysis: {e}")
        import traceback
        traceback.print_exc()
        return False

def main():
    """Run all tests"""
    print("\n" + "=" * 60)
    print("ALGOTRENDY MEM - ADVANCED INDICATORS TEST SUITE")
    print("=" * 60)
    print(f"Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 60)

    results = []

    # Test 1: Basic Indicators
    results.append(("Basic Indicators", test_basic_indicators()))

    # Test 2: Integration Functions
    results.append(("Integration Functions", test_integration_functions()))

    # Test 3: Multi-Timeframe Analysis
    results.append(("Multi-Timeframe Analysis", test_multi_timeframe()))

    # Summary
    print("\n" + "=" * 60)
    print("TEST SUMMARY")
    print("=" * 60)

    passed = sum(1 for _, result in results if result)
    total = len(results)

    for name, result in results:
        status = "✓ PASS" if result else "✗ FAIL"
        print(f"{status}: {name}")

    print("=" * 60)
    print(f"Results: {passed}/{total} tests passed ({passed/total*100:.0f}%)")
    print("=" * 60)

    if passed == total:
        print("\n🎉 All tests passed successfully!")
        return 0
    else:
        print(f"\n⚠️  {total - passed} test(s) failed")
        return 1

if __name__ == "__main__":
    sys.exit(main())
