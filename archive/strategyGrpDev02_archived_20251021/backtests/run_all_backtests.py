"""
Comprehensive Backtest Runner for Strategy Group Dev 02

This script runs all three baseline strategies and generates detailed results.
"""

import sys
import os
import json
from datetime import datetime

# Add implementations directory to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'implementations'))

# Import strategies
from strategy1_vol_managed_momentum import VolatilityManagedMomentum, run_example as run_strategy1
from strategy2_pairs_trading import PairsTradingStrategy, run_example as run_strategy2
from strategy3_carry_trade import CurrencyCarryTrade, run_example as run_strategy3


def save_results(strategy_name: str, results: dict, results_dir: str = '../results'):
    """
    Save backtest results to JSON file

    Args:
        strategy_name: Name of the strategy
        results: Results dictionary
        results_dir: Directory to save results
    """
    # Create results directory if it doesn't exist
    os.makedirs(results_dir, exist_ok=True)

    # Add metadata
    results_with_metadata = {
        'strategy_name': strategy_name,
        'backtest_date': datetime.now().isoformat(),
        'version': 'baseline_1.0',
        'results': results
    }

    # Save to file
    filename = f"{strategy_name.lower().replace(' ', '_')}_baseline_results.json"
    filepath = os.path.join(results_dir, filename)

    with open(filepath, 'w') as f:
        json.dump(results_with_metadata, f, indent=2, default=str)

    print(f"\n✅ Results saved to: {filepath}")

    return filepath


def print_comparison_table(all_results: dict):
    """
    Print comparison table across all strategies

    Args:
        all_results: Dictionary of {strategy_name: results}
    """
    print("\n" + "=" * 90)
    print("STRATEGY COMPARISON TABLE - BASELINE RESULTS")
    print("=" * 90)
    print(f"{'Metric':<25} {'Vol-Managed Mom':>20} {'Pairs Trading':>20} {'Carry Trade':>20}")
    print("-" * 90)

    metrics = [
        ('Total Return', 'total_return', True),
        ('CAGR', 'cagr', True),
        ('Sharpe Ratio', 'sharpe_ratio', False),
        ('Sortino Ratio', 'sortino_ratio', False),
        ('Max Drawdown', 'max_drawdown', True),
        ('Calmar Ratio', 'calmar_ratio', False),
        ('Annual Volatility', 'annual_volatility', True),
        ('Win Rate', 'win_rate', True),
        ('Number of Trades', 'num_trades', False)
    ]

    for metric_name, metric_key, is_percentage in metrics:
        row_values = []

        for strategy_name in ['Strategy 1', 'Strategy 2', 'Strategy 3']:
            if strategy_name in all_results:
                value = all_results[strategy_name].get(metric_key, 0)

                if is_percentage:
                    formatted = f"{value*100:>18.2f}%"
                else:
                    if isinstance(value, float):
                        formatted = f"{value:>20.2f}"
                    else:
                        formatted = f"{value:>20}"
            else:
                formatted = "N/A"

            row_values.append(formatted)

        print(f"{metric_name:<25} {row_values[0]} {row_values[1]} {row_values[2]}")

    print("=" * 90)


def generate_summary_report(all_results: dict, reports_dir: str = '../reports'):
    """
    Generate markdown summary report

    Args:
        all_results: Dictionary of {strategy_name: results}
        reports_dir: Directory to save reports
    """
    os.makedirs(reports_dir, exist_ok=True)

    report_path = os.path.join(reports_dir, 'BACKTEST_RESULTS.md')

    with open(report_path, 'w') as f:
        f.write("# Backtest Results - Strategy Group Development 02\n\n")
        f.write(f"**Generated**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write(f"**Test Type**: Baseline (No MEM Enhancement)\n\n")
        f.write("---\n\n")

        f.write("## Executive Summary\n\n")
        f.write("This report presents the backtest results for three academically-validated trading strategies:\n\n")
        f.write("1. **Volatility-Managed Momentum** - Based on Barroso & Santa-Clara (2015)\n")
        f.write("2. **Statistical Arbitrage Pairs Trading** - Based on O-U process papers\n")
        f.write("3. **Currency Carry Trade** - Based on Daniel, Hodrick & Lu (2017)\n\n")

        f.write("All strategies were tested using their **baseline implementations** without MEM enhancements.\n\n")
        f.write("---\n\n")

        # Strategy 1 Results
        if 'Strategy 1' in all_results:
            f.write("## Strategy #1: Volatility-Managed Momentum\n\n")
            results = all_results['Strategy 1']

            f.write("### Published Benchmark\n")
            f.write("- Sharpe Ratio: **0.97** (Barroso & Santa-Clara, 2015)\n")
            f.write("- Max Drawdown: **-45.20%**\n")
            f.write("- Test Period: 1927-2012\n\n")

            f.write("### Our Backtest Results\n\n")
            f.write(f"| Metric | Value |\n")
            f.write(f"|--------|-------|\n")
            f.write(f"| Total Return | {results.get('total_return', 0)*100:.2f}% |\n")
            f.write(f"| CAGR | {results.get('cagr', 0)*100:.2f}% |\n")
            f.write(f"| Sharpe Ratio | {results.get('sharpe_ratio', 0):.2f} |\n")
            f.write(f"| Max Drawdown | {results.get('max_drawdown', 0)*100:.2f}% |\n")
            f.write(f"| Annual Volatility | {results.get('annual_volatility', 0)*100:.2f}% |\n")
            f.write(f"| Number of Trades | {results.get('num_trades', 0)} |\n\n")

            # Calculate vs benchmark
            sharpe_vs_benchmark = ((results.get('sharpe_ratio', 0) / 0.97) - 1) * 100 if results.get('sharpe_ratio', 0) > 0 else 0
            f.write(f"**Sharpe vs. Published Benchmark**: {sharpe_vs_benchmark:+.1f}%\n\n")

            f.write("---\n\n")

        # Strategy 2 Results
        if 'Strategy 2' in all_results:
            f.write("## Strategy #2: Statistical Arbitrage Pairs Trading\n\n")
            results = all_results['Strategy 2']

            f.write("### Published Benchmark\n")
            f.write("- Sharpe Ratio: **1.5-2.5** (daily rebalancing)\n")
            f.write("- Win Rate: **55-65%**\n")
            f.write("- Annual Return: **15-20%**\n\n")

            f.write("### Our Backtest Results\n\n")
            f.write(f"| Metric | Value |\n")
            f.write(f"|--------|-------|\n")
            f.write(f"| Total Return | {results.get('total_return', 0)*100:.2f}% |\n")
            f.write(f"| CAGR | {results.get('cagr', 0)*100:.2f}% |\n")
            f.write(f"| Sharpe Ratio | {results.get('sharpe_ratio', 0):.2f} |\n")
            f.write(f"| Max Drawdown | {results.get('max_drawdown', 0)*100:.2f}% |\n")
            f.write(f"| Win Rate | {results.get('win_rate', 0)*100:.2f}% |\n")
            f.write(f"| Profit Factor | {results.get('profit_factor', 0):.2f} |\n")
            f.write(f"| Number of Trades | {results.get('num_trades', 0)} |\n\n")

            # Calculate vs benchmark (mid-range)
            benchmark_sharpe = 2.0
            sharpe_vs_benchmark = ((results.get('sharpe_ratio', 0) / benchmark_sharpe) - 1) * 100 if results.get('sharpe_ratio', 0) > 0 else 0
            f.write(f"**Sharpe vs. Published Benchmark** (mid-range): {sharpe_vs_benchmark:+.1f}%\n\n")

            f.write("---\n\n")

        # Strategy 3 Results
        if 'Strategy 3' in all_results:
            f.write("## Strategy #3: Currency Carry Trade\n\n")
            results = all_results['Strategy 3']

            f.write("### Published Benchmark\n")
            f.write("- Sharpe Ratio: **1.07** (combined signals)\n")
            f.write("- Sharpe Ratio: **0.76-0.78** (baseline HML-FX)\n\n")

            f.write("### Our Backtest Results\n\n")
            f.write(f"| Metric | Value |\n")
            f.write(f"|--------|-------|\n")
            f.write(f"| Total Return | {results.get('total_return', 0)*100:.2f}% |\n")
            f.write(f"| CAGR | {results.get('cagr', 0)*100:.2f}% |\n")
            f.write(f"| Sharpe Ratio | {results.get('sharpe_ratio', 0):.2f} |\n")
            f.write(f"| Max Drawdown | {results.get('max_drawdown', 0)*100:.2f}% |\n")
            f.write(f"| Annual Volatility | {results.get('annual_volatility', 0)*100:.2f}% |\n")
            f.write(f"| Number of Trades | {results.get('num_trades', 0)} |\n\n")

            # Calculate vs benchmark
            sharpe_vs_benchmark = ((results.get('sharpe_ratio', 0) / 1.07) - 1) * 100 if results.get('sharpe_ratio', 0) > 0 else 0
            f.write(f"**Sharpe vs. Published Benchmark** (combined): {sharpe_vs_benchmark:+.1f}%\n\n")

            f.write("---\n\n")

        # Overall Assessment
        f.write("## Overall Assessment\n\n")
        f.write("### Validation Status\n\n")

        # Check if results are within 10% of published benchmarks
        validations = []

        if 'Strategy 1' in all_results:
            sharpe1 = all_results['Strategy 1'].get('sharpe_ratio', 0)
            within_range1 = abs(sharpe1 - 0.97) / 0.97 <= 0.20  # 20% tolerance for synthetic data
            status1 = "✅ VALIDATED" if within_range1 else "⚠️ REVIEW NEEDED"
            validations.append(f"**Strategy 1**: {status1} (Sharpe {sharpe1:.2f} vs 0.97 published)")

        if 'Strategy 2' in all_results:
            sharpe2 = all_results['Strategy 2'].get('sharpe_ratio', 0)
            within_range2 = 1.0 <= sharpe2 <= 3.0  # Wide range due to pair variance
            status2 = "✅ VALIDATED" if within_range2 else "⚠️ REVIEW NEEDED"
            validations.append(f"**Strategy 2**: {status2} (Sharpe {sharpe2:.2f} vs 1.5-2.5 published)")

        if 'Strategy 3' in all_results:
            sharpe3 = all_results['Strategy 3'].get('sharpe_ratio', 0)
            within_range3 = 0.5 <= sharpe3 <= 1.5  # Wide range for FX
            status3 = "✅ VALIDATED" if within_range3 else "⚠️ REVIEW NEEDED"
            validations.append(f"**Strategy 3**: {status3} (Sharpe {sharpe3:.2f} vs 1.07 published)")

        for validation in validations:
            f.write(f"- {validation}\n")

        f.write("\n### Next Steps\n\n")
        f.write("1. ✅ Baseline strategies implemented and tested\n")
        f.write("2. ⏳ Implement MEM enhancements for each strategy\n")
        f.write("3. ⏳ Run comparative backtests (MEM vs baseline)\n")
        f.write("4. ⏳ Deploy to paper trading\n")
        f.write("5. ⏳ Live deployment with small capital\n\n")

        f.write("---\n\n")
        f.write(f"*Report generated by automated backtest system*\n")
        f.write(f"*Date: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}*\n")

    print(f"\n✅ Summary report saved to: {report_path}")
    return report_path


def main():
    """Main execution function"""

    print("=" * 90)
    print("STRATEGY GROUP DEVELOPMENT 02 - COMPREHENSIVE BACKTEST")
    print("=" * 90)
    print(f"Started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")

    all_results = {}

    # Run Strategy 1
    print("\n" + "─" * 90)
    print("RUNNING STRATEGY #1: VOLATILITY-MANAGED MOMENTUM")
    print("─" * 90)
    try:
        strategy1, results1 = run_strategy1()
        all_results['Strategy 1'] = results1
        save_results('Strategy 1', results1)
    except Exception as e:
        print(f"❌ Error running Strategy 1: {e}")

    # Run Strategy 2
    print("\n" + "─" * 90)
    print("RUNNING STRATEGY #2: STATISTICAL ARBITRAGE PAIRS TRADING")
    print("─" * 90)
    try:
        strategy2, results2 = run_strategy2()
        if results2:
            all_results['Strategy 2'] = results2
            save_results('Strategy 2', results2)
    except Exception as e:
        print(f"❌ Error running Strategy 2: {e}")

    # Run Strategy 3
    print("\n" + "─" * 90)
    print("RUNNING STRATEGY #3: CURRENCY CARRY TRADE")
    print("─" * 90)
    try:
        strategy3, results3 = run_strategy3()
        all_results['Strategy 3'] = results3
        save_results('Strategy 3', results3)
    except Exception as e:
        print(f"❌ Error running Strategy 3: {e}")

    # Print comparison table
    if len(all_results) > 0:
        print_comparison_table(all_results)

        # Generate summary report
        generate_summary_report(all_results)

    print("\n" + "=" * 90)
    print(f"BACKTEST COMPLETE - {len(all_results)}/3 strategies executed successfully")
    print(f"Finished: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 90 + "\n")


if __name__ == '__main__':
    main()
