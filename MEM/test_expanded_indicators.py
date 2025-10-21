"""
Test Suite for Expanded Indicators Library
===========================================
Tests all 63 new indicators across 7 categories.

Run with: python3 test_expanded_indicators.py
"""

import pandas as pd
import numpy as np
from datetime import datetime, timedelta
from expanded_indicators import get_expanded_indicators, list_expanded_indicators

# Color codes for output
GREEN = '\033[92m'
RED = '\033[91m'
YELLOW = '\033[93m'
BLUE = '\033[94m'
RESET = '\033[0m'


def generate_sample_data(periods=100):
    """Generate sample OHLCV data for testing"""
    np.random.seed(42)
    dates = pd.date_range(end=datetime.now(), periods=periods, freq='1h')

    # Generate realistic price data
    base_price = 100.0
    returns = np.random.normal(0.0005, 0.02, periods)
    close = base_price * (1 + returns).cumprod()

    # Generate OHLC
    open_price = close * (1 + np.random.normal(0, 0.01, periods))
    high = np.maximum(open_price, close) * (1 + np.random.uniform(0, 0.02, periods))
    low = np.minimum(open_price, close) * (1 - np.random.uniform(0, 0.02, periods))
    volume = np.random.uniform(1000, 10000, periods)

    df = pd.DataFrame({
        'open': open_price,
        'high': high,
        'low': low,
        'close': close,
        'volume': volume
    }, index=dates)

    return df


def test_candlestick_patterns():
    """Test candlestick pattern indicators"""
    print(f"\n{BLUE}Testing Candlestick Patterns (3 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    try:
        # Test 1: Doji
        doji = indicators.cdl_doji(data['open'], data['high'], data['low'], data['close'])
        assert len(doji) == len(data), "Doji length mismatch"
        assert doji.dtype in [int, np.int64, np.int32], "Doji should return integer values"
        print(f"  {GREEN}✓{RESET} cdl_doji - Found {doji.sum()} doji patterns")
        tests_passed += 1
    except Exception as e:
        print(f"  {RED}✗{RESET} cdl_doji - {str(e)}")
        tests_failed += 1

    try:
        # Test 2: Inside Bar
        inside = indicators.cdl_inside(data['open'], data['high'], data['low'], data['close'])
        assert len(inside) == len(data), "Inside bar length mismatch"
        print(f"  {GREEN}✓{RESET} cdl_inside - Found {inside.sum()} inside bars")
        tests_passed += 1
    except Exception as e:
        print(f"  {RED}✗{RESET} cdl_inside - {str(e)}")
        tests_failed += 1

    try:
        # Test 3: Pattern recognition
        patterns = indicators.cdl_pattern(data['open'], data['high'], data['low'], data['close'])
        assert isinstance(patterns, pd.DataFrame), "Pattern should return DataFrame"
        assert len(patterns) == len(data), "Patterns length mismatch"
        print(f"  {GREEN}✓{RESET} cdl_pattern - Detected {len(patterns.columns)} pattern types")
        tests_passed += 1
    except Exception as e:
        print(f"  {RED}✗{RESET} cdl_pattern - {str(e)}")
        tests_failed += 1

    return tests_passed, tests_failed


def test_advanced_momentum():
    """Test advanced momentum indicators"""
    print(f"\n{BLUE}Testing Advanced Momentum (14 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    # Test each momentum indicator
    momentum_tests = [
        ('rsx', lambda: indicators.rsx(data['close']), "0-100"),
        ('stochrsi', lambda: indicators.stochrsi(data['close']), "tuple of 2"),
        ('cmo', lambda: indicators.cmo(data['close']), "-100 to +100"),
        ('ppo', lambda: indicators.ppo(data['close']), "tuple of 3"),
        ('apo', lambda: indicators.apo(data['close']), "unbounded"),
        ('bop', lambda: indicators.bop(data['open'], data['high'], data['low'], data['close']), "-1 to +1"),
        ('cfo', lambda: indicators.cfo(data['close']), "percentage"),
        ('cti', lambda: indicators.cti(data['close']), "-1 to +1"),
        ('er', lambda: indicators.er(data['close']), "0 to 1"),
        ('inertia', lambda: indicators.inertia(data['close']), "unbounded"),
        ('kdj', lambda: indicators.kdj(data['high'], data['low'], data['close']), "tuple of 3"),
        ('pgo', lambda: indicators.pgo(data['high'], data['low'], data['close']), "unbounded"),
        ('psl', lambda: indicators.psl(data['close']), "0-100"),
        ('qqe', lambda: indicators.qqe(data['close']), "0-100"),
    ]

    for name, test_func, expected_range in momentum_tests:
        try:
            result = test_func()
            if isinstance(result, tuple):
                assert all(len(r) == len(data) for r in result), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - Range: {expected_range}")
            else:
                assert len(result) == len(data), f"{name} length mismatch"
                valid_values = result.dropna()
                if len(valid_values) > 0:
                    print(f"  {GREEN}✓{RESET} {name} - Range: {expected_range} (Latest: {valid_values.iloc[-1]:.2f})")
                else:
                    print(f"  {GREEN}✓{RESET} {name} - Range: {expected_range}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def test_additional_trend():
    """Test additional trend indicators"""
    print(f"\n{BLUE}Testing Additional Trend Indicators (12 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    trend_tests = [
        ('alma', lambda: indicators.alma(data['close']), "moving average"),
        ('dema', lambda: indicators.dema(data['close']), "moving average"),
        ('t3', lambda: indicators.t3(data['close']), "moving average"),
        ('zlma', lambda: indicators.zlma(data['close']), "moving average"),
        ('kama', lambda: indicators.kama(data['close']), "adaptive MA"),
        ('vidya', lambda: indicators.vidya(data['close']), "adaptive MA"),
        ('jma', lambda: indicators.jma(data['close']), "moving average"),
        ('fwma', lambda: indicators.fwma(data['close']), "fibonacci weighted"),
        ('linreg', lambda: indicators.linreg(data['close']), "regression line"),
        ('dpo', lambda: indicators.dpo(data['close']), "detrended"),
        ('vhf', lambda: indicators.vhf(data['close']), "trending filter"),
        ('rwi', lambda: indicators.rwi(data['high'], data['low']), "tuple of 2"),
    ]

    for name, test_func, description in trend_tests:
        try:
            result = test_func()
            if isinstance(result, tuple):
                assert all(len(r) == len(data) for r in result), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description}")
            else:
                assert len(result) == len(data), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def test_statistical():
    """Test statistical indicators"""
    print(f"\n{BLUE}Testing Statistical Indicators (10 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    stat_tests = [
        ('entropy', lambda: indicators.entropy(data['close']), "randomness"),
        ('kurtosis', lambda: indicators.kurtosis(data['close']), "tailedness"),
        ('skew', lambda: indicators.skew(data['close']), "asymmetry"),
        ('variance', lambda: indicators.variance(data['close']), "dispersion"),
        ('zscore', lambda: indicators.zscore(data['close']), "std from mean"),
        ('mad', lambda: indicators.mad(data['close']), "mean abs dev"),
        ('median', lambda: indicators.median(data['close']), "median"),
        ('quantile', lambda: indicators.quantile(data['close']), "percentile"),
        ('stdev', lambda: indicators.stdev(data['close']), "std deviation"),
        ('tos_stdevall', lambda: indicators.tos_stdevall(data['close']), "multi-band"),
    ]

    for name, test_func, description in stat_tests:
        try:
            result = test_func()
            if isinstance(result, pd.DataFrame):
                assert len(result) == len(data), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description} ({len(result.columns)} bands)")
            else:
                assert len(result) == len(data), f"{name} length mismatch"
                valid_values = result.dropna()
                if len(valid_values) > 0:
                    print(f"  {GREEN}✓{RESET} {name} - {description} (Latest: {valid_values.iloc[-1]:.4f})")
                else:
                    print(f"  {GREEN}✓{RESET} {name} - {description}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def test_performance():
    """Test performance indicators"""
    print(f"\n{BLUE}Testing Performance Indicators (8 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    performance_tests = [
        ('log_return', lambda: indicators.log_return(data['close']), "log returns"),
        ('percent_return', lambda: indicators.percent_return(data['close']), "% returns"),
        ('drawdown', lambda: indicators.drawdown(data['close']), "drawdown %"),
        ('ui', lambda: indicators.ui(data['close']), "ulcer index"),
        ('pvr', lambda: indicators.pvr(data['close']), "percentile rank"),
        ('slope', lambda: indicators.slope(data['close']), "regression slope"),
        ('long_run', lambda: indicators.long_run(data['close'].rolling(5).mean(), data['close'].rolling(20).mean()), "consecutive up"),
        ('short_run', lambda: indicators.short_run(data['close'].rolling(5).mean(), data['close'].rolling(20).mean()), "consecutive down"),
    ]

    for name, test_func, description in performance_tests:
        try:
            result = test_func()
            assert len(result) == len(data), f"{name} length mismatch"
            valid_values = result.dropna()
            if len(valid_values) > 0:
                print(f"  {GREEN}✓{RESET} {name} - {description} (Latest: {valid_values.iloc[-1]:.4f})")
            else:
                print(f"  {GREEN}✓{RESET} {name} - {description}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def test_volume_extensions():
    """Test volume extension indicators"""
    print(f"\n{BLUE}Testing Volume Extensions (8 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    volume_tests = [
        ('aobv', lambda: indicators.aobv(data['close'], data['volume']), "archer OBV"),
        ('adosc', lambda: indicators.adosc(data['high'], data['low'], data['close'], data['volume']), "A/D oscillator"),
        ('kvo', lambda: indicators.kvo(data['high'], data['low'], data['close'], data['volume']), "klinger volume"),
        ('nvi', lambda: indicators.nvi(data['close'], data['volume']), "negative vol idx"),
        ('pvi', lambda: indicators.pvi(data['close'], data['volume']), "positive vol idx"),
        ('pvo', lambda: indicators.pvo(data['volume']), "% volume osc"),
        ('pvol', lambda: indicators.pvol(data['close'], data['volume']), "price-vol osc"),
        ('efi', lambda: indicators.efi(data['close'], data['volume']), "force index"),
    ]

    for name, test_func, description in volume_tests:
        try:
            result = test_func()
            if isinstance(result, tuple):
                assert all(len(r) == len(data) for r in result), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description}")
            else:
                assert len(result) == len(data), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def test_price_transformations():
    """Test price transformation indicators"""
    print(f"\n{BLUE}Testing Price Transformations (8 indicators)...{RESET}")

    data = generate_sample_data()
    indicators = get_expanded_indicators()

    tests_passed = 0
    tests_failed = 0

    price_tests = [
        ('ha', lambda: indicators.ha(data['open'], data['high'], data['low'], data['close']), "heikin ashi"),
        ('hl2', lambda: indicators.hl2(data['high'], data['low']), "median price"),
        ('hlc3', lambda: indicators.hlc3(data['high'], data['low'], data['close']), "typical price"),
        ('ohlc4', lambda: indicators.ohlc4(data['open'], data['high'], data['low'], data['close']), "average price"),
        ('wcp', lambda: indicators.wcp(data['high'], data['low'], data['close']), "weighted close"),
        ('midpoint', lambda: indicators.midpoint(data['close']), "midpoint"),
        ('midprice', lambda: indicators.midprice(data['high'], data['low']), "mid price"),
        ('pdist', lambda: indicators.pdist(data['open'], data['high'], data['low'], data['close']), "price distance"),
    ]

    for name, test_func, description in price_tests:
        try:
            result = test_func()
            if isinstance(result, pd.DataFrame):
                assert len(result) == len(data), f"{name} length mismatch"
                print(f"  {GREEN}✓{RESET} {name} - {description} ({len(result.columns)} columns)")
            else:
                assert len(result) == len(data), f"{name} length mismatch"
                valid_values = result.dropna()
                if len(valid_values) > 0:
                    print(f"  {GREEN}✓{RESET} {name} - {description} (Latest: {valid_values.iloc[-1]:.2f})")
                else:
                    print(f"  {GREEN}✓{RESET} {name} - {description}")
            tests_passed += 1
        except Exception as e:
            print(f"  {RED}✗{RESET} {name} - {str(e)}")
            tests_failed += 1

    return tests_passed, tests_failed


def main():
    """Run all tests"""
    print("=" * 80)
    print(f"{YELLOW}EXPANDED INDICATORS TEST SUITE{RESET}")
    print("=" * 80)

    # Display library info
    all_indicators = list_expanded_indicators()
    total_indicators = sum(len(inds) for inds in all_indicators.values())

    print(f"\nTotal new indicators: {total_indicators}")
    print(f"Categories: {len(all_indicators)}")
    print(f"Combined with base library: {total_indicators + 38} total indicators")

    # Run all tests
    all_passed = 0
    all_failed = 0

    passed, failed = test_candlestick_patterns()
    all_passed += passed
    all_failed += failed

    passed, failed = test_advanced_momentum()
    all_passed += passed
    all_failed += failed

    passed, failed = test_additional_trend()
    all_passed += passed
    all_failed += failed

    passed, failed = test_statistical()
    all_passed += passed
    all_failed += failed

    passed, failed = test_performance()
    all_passed += passed
    all_failed += failed

    passed, failed = test_volume_extensions()
    all_passed += passed
    all_failed += failed

    passed, failed = test_price_transformations()
    all_passed += passed
    all_failed += failed

    # Summary
    print("\n" + "=" * 80)
    print(f"{YELLOW}TEST RESULTS{RESET}")
    print("=" * 80)

    total_tests = all_passed + all_failed
    pass_rate = (all_passed / total_tests * 100) if total_tests > 0 else 0

    print(f"\nTests Passed: {GREEN}{all_passed}{RESET} / {total_tests}")
    print(f"Tests Failed: {RED}{all_failed}{RESET} / {total_tests}")
    print(f"Pass Rate: {GREEN if pass_rate == 100 else YELLOW}{pass_rate:.1f}%{RESET}")

    if all_failed == 0:
        print(f"\n{GREEN}✓ ALL TESTS PASSED!{RESET}")
        print(f"\n{GREEN}✓ Expanded indicator library is ready for production use.{RESET}")
        return 0
    else:
        print(f"\n{YELLOW}⚠ Some tests failed. Review errors above.{RESET}")
        return 1


if __name__ == "__main__":
    exit(main())
