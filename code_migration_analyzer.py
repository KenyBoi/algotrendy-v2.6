#!/usr/bin/env python3
"""
Code Migration Analyzer - v2.5 → v2.6 Comparison
Identifies valuable code from v2.5 that may be missing in v2.6.
Created: October 18, 2025

Scans for:
- Core trading logic and strategies
- Data channel implementations
- Broker integrations
- Database utilities
- API endpoints
- Backtesting logic
- Configuration patterns
"""

import os
import sys
import re
from pathlib import Path
from collections import defaultdict
from typing import Dict, Set, List, Tuple
import json


class CodeMigrationAnalyzer:
    """Analyzes what core code may be missing between v2.5 and v2.6"""

    # Key patterns to search for in v2.5 (valuable code indicators)
    SEARCH_PATTERNS = {
        'strategies': [
            r'class\s+\w*Strategy\w*\(.*\):',
            r'def\s+generate_signal',
            r'def\s+calculate_indicators',
            r'STRATEGY_CONFIG\s*=',
        ],
        'data_channels': [
            r'class\s+\w*Channel\w*',
            r'async def\s+fetch',
            r'def\s+fetch.*data',
            r'EXCHANGE_API',
            r'rate_limit',
        ],
        'brokers': [
            r'class\s+\w*Broker\w*',
            r'async def\s+place_order',
            r'async def\s+cancel_order',
            r'async def\s+get_balance',
            r'API_KEY|api_key',
        ],
        'backtesting': [
            r'class\s+Backtest',
            r'def\s+run_backtest',
            r'def\s+calculate_pnl',
            r'performance_report',
        ],
        'indicators': [
            r'def\s+calculate_rsi',
            r'def\s+calculate_macd',
            r'def\s+calculate_ema',
            r'def\s+calculate_sma',
            r'def\s+calculate_bollinger',
        ],
        'database': [
            r'class\s+.*Repository',
            r'async def\s+save',
            r'async def\s+query',
            r'CREATE TABLE',
            r'INSERT INTO',
        ],
        'risk_management': [
            r'class\s+RiskManager',
            r'def\s+validate_order',
            r'def\s+check_exposure',
            r'MAX_POSITION|max_position',
        ],
        'api': [
            r'@app\.route\|@app\.post\|@app\.get',
            r'def\s+\w*_endpoint',
            r'/api/.*order',
            r'/api/.*position',
        ],
    }

    # File patterns to exclude (noise/config/test files)
    EXCLUDE_PATTERNS = {
        '__pycache__', '.git', 'node_modules', '.pytest_cache',
        'dist', 'build', '.venv', 'venv', '__pycache__',
        '.next', 'configs/finagent', 'test_*.py', '*_test.py',
        '.env', '*.egg-info', '.idea', '.vscode',
    }

    # Key directories to analyze
    CORE_DIRS = {
        'v2.5': [
            'algotrendy/strategies',
            'algotrendy/data_channels',
            'algotrendy/broker_abstraction.py',
            'algotrendy/unified_trader.py',
            'algotrendy/indicators',
            'algotrendy/risk_management',
            'api',
            'database',
        ],
        'v2.6': [
            'backend/AlgoTrendy.Strategies',
            'backend/AlgoTrendy.DataChannels',
            'backend/AlgoTrendy.TradingEngine/Brokers',
            'backend/AlgoTrendy.TradingEngine/Indicators',
            'backend/AlgoTrendy.TradingEngine/RiskManagement',
            'backend/AlgoTrendy.API',
        ]
    }

    def __init__(self, v25_path: str, v26_path: str):
        """Initialize analyzer with paths to v2.5 and v2.6"""
        self.v25_path = Path(v25_path)
        self.v26_path = Path(v26_path)
        self.v25_files = {}
        self.v26_files = {}
        self.missing_code = defaultdict(list)
        self.comparison_results = {}

    def should_exclude_file(self, filepath: str) -> bool:
        """Check if file should be excluded"""
        for pattern in self.EXCLUDE_PATTERNS:
            if pattern in filepath:
                return True
        return False

    def collect_code_files(self, base_path: Path, version: str) -> Dict[str, str]:
        """Collect and index code files"""
        files = {}
        supported_extensions = {'.py', '.cs', '.js', '.ts', '.tsx', '.jsx'}

        for root, dirs, filenames in os.walk(base_path):
            dirs[:] = [d for d in dirs if not self.should_exclude_file(d)]

            for filename in filenames:
                if any(filename.endswith(ext) for ext in supported_extensions):
                    filepath = os.path.join(root, filename)

                    if self.should_exclude_file(filepath):
                        continue

                    try:
                        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                            content = f.read()
                        rel_path = os.path.relpath(filepath, base_path)
                        files[rel_path] = content
                    except Exception as e:
                        print(f"Error reading {filepath}: {e}", file=sys.stderr)

        return files

    def search_pattern_in_files(self, files: Dict[str, str],
                               pattern: str) -> List[Tuple[str, int, str]]:
        """Search for pattern in all files, return (file, line_num, content)"""
        results = []
        regex = re.compile(pattern, re.IGNORECASE | re.MULTILINE)

        for filepath, content in files.items():
            matches = regex.finditer(content)
            lines = content.split('\n')

            for match in matches:
                # Find line number
                line_num = content[:match.start()].count('\n') + 1
                if line_num <= len(lines):
                    results.append((filepath, line_num, lines[line_num - 1].strip()))

        return results

    def analyze_category(self, category: str) -> Dict:
        """Analyze a category of code"""
        v25_findings = []
        v26_findings = []

        patterns = self.SEARCH_PATTERNS.get(category, [])

        for pattern in patterns:
            v25_matches = self.search_pattern_in_files(self.v25_files, pattern)
            v26_matches = self.search_pattern_in_files(self.v26_files, pattern)

            v25_findings.extend(v25_matches)
            v26_findings.extend(v26_matches)

        # Count unique files and items
        v25_unique_files = set(f for f, _, _ in v25_findings)
        v26_unique_files = set(f for f, _, _ in v26_findings)

        return {
            'v25_count': len(v25_findings),
            'v26_count': len(v26_findings),
            'v25_files': len(v25_unique_files),
            'v26_files': len(v26_unique_files),
            'v25_samples': v25_findings[:3],  # First 3 matches
            'v26_samples': v26_findings[:3],
            'missing_files': list(v25_unique_files - v26_unique_files),
        }

    def generate_report(self) -> Dict:
        """Generate comprehensive migration report"""
        print("Loading v2.5 files...", file=sys.stderr)
        self.v25_files = self.collect_code_files(self.v25_path, 'v2.5')
        print(f"  Loaded {len(self.v25_files)} files from v2.5", file=sys.stderr)

        print("Loading v2.6 files...", file=sys.stderr)
        self.v26_files = self.collect_code_files(self.v26_path, 'v2.6')
        print(f"  Loaded {len(self.v26_files)} files from v2.6", file=sys.stderr)

        # Analyze each category
        report = {
            'timestamp': '2025-10-18',
            'v25_total_files': len(self.v25_files),
            'v26_total_files': len(self.v26_files),
            'categories': {}
        }

        for category in self.SEARCH_PATTERNS.keys():
            print(f"Analyzing {category}...", file=sys.stderr)
            report['categories'][category] = self.analyze_category(category)

        # Calculate summary
        total_v25_items = sum(r['v25_count'] for r in report['categories'].values())
        total_v26_items = sum(r['v26_count'] for r in report['categories'].values())
        total_missing_files = sum(len(r['missing_files']) for r in report['categories'].values())

        report['summary'] = {
            'total_v25_patterns': total_v25_items,
            'total_v26_patterns': total_v26_items,
            'migration_completeness': f"{(total_v26_items / total_v25_items * 100) if total_v25_items > 0 else 0:.1f}%",
            'potentially_missing_files': total_missing_files,
        }

        return report

    def print_text_report(self, report: Dict):
        """Print human-readable text report"""
        print("\n" + "=" * 80)
        print("CODE MIGRATION ANALYSIS REPORT - v2.5 → v2.6")
        print("=" * 80)
        print()

        summary = report['summary']
        print(f"Files analyzed:")
        print(f"  v2.5: {report['v25_total_files']} files")
        print(f"  v2.6: {report['v26_total_files']} files")
        print()

        print(f"Migration Completeness: {summary['migration_completeness']}")
        print(f"  v2.5 patterns found: {summary['total_v25_patterns']}")
        print(f"  v2.6 patterns found: {summary['total_v26_patterns']}")
        print(f"  Potentially missing files: {summary['potentially_missing_files']}")
        print()

        print("=" * 80)
        print("CATEGORY ANALYSIS")
        print("=" * 80)

        for category, data in report['categories'].items():
            print(f"\n📁 {category.upper()}")
            print(f"   v2.5: {data['v25_count']} patterns in {data['v25_files']} files")
            print(f"   v2.6: {data['v26_count']} patterns in {data['v26_files']} files")
            print(f"   Coverage: {(data['v26_count'] / data['v25_count'] * 100) if data['v25_count'] > 0 else 0:.1f}%")

            if data['missing_files']:
                print(f"   ⚠️  Missing files from v2.5:")
                for f in data['missing_files'][:5]:
                    print(f"      - {f}")
                if len(data['missing_files']) > 5:
                    print(f"      ... and {len(data['missing_files']) - 5} more")

            if data['v25_samples']:
                print(f"   v2.5 sample code:")
                for filepath, line_num, content in data['v25_samples']:
                    print(f"      {filepath}:{line_num}: {content[:60]}...")

        print("\n" + "=" * 80)
        print("RECOMMENDATIONS")
        print("=" * 80)
        print()

        incomplete = []
        for category, data in report['categories'].items():
            coverage = (data['v26_count'] / data['v25_count'] * 100) if data['v25_count'] > 0 else 100
            if coverage < 80:
                incomplete.append((category, coverage))

        if incomplete:
            print("Categories needing attention (< 80% coverage):")
            for category, coverage in sorted(incomplete, key=lambda x: x[1]):
                print(f"  - {category}: {coverage:.1f}% coverage")
        else:
            print("✅ All major categories have good coverage (≥ 80%)")

        print()

    def save_json_report(self, report: Dict, filepath: str):
        """Save report as JSON"""
        with open(filepath, 'w') as f:
            # Convert tuples to lists for JSON serialization
            for category in report['categories'].values():
                category['v25_samples'] = [(f, l, c) for f, l, c in category['v25_samples']]
                category['v26_samples'] = [(f, l, c) for f, l, c in category['v26_samples']]

            json.dump(report, f, indent=2)
        print(f"\nJSON report saved to {filepath}", file=sys.stderr)


def main():
    """Main entry point"""
    import argparse

    parser = argparse.ArgumentParser(
        description='Analyze code migration from v2.5 to v2.6',
        epilog='Example: python3 code_migration_analyzer.py /root/algotrendy_v2.5 /root/AlgoTrendy_v2.6'
    )

    parser.add_argument('v25_path', help='Path to v2.5 codebase')
    parser.add_argument('v26_path', help='Path to v2.6 codebase')
    parser.add_argument('--json', action='store_true', help='Also save JSON report')
    parser.add_argument('--output', default='migration_report.json', help='JSON output file')

    args = parser.parse_args()

    # Validate paths
    if not Path(args.v25_path).exists():
        print(f"Error: v2.5 path not found: {args.v25_path}", file=sys.stderr)
        sys.exit(1)

    if not Path(args.v26_path).exists():
        print(f"Error: v2.6 path not found: {args.v26_path}", file=sys.stderr)
        sys.exit(1)

    # Run analysis
    analyzer = CodeMigrationAnalyzer(args.v25_path, args.v26_path)
    report = analyzer.generate_report()

    # Output reports
    analyzer.print_text_report(report)

    if args.json:
        analyzer.save_json_report(report, args.output)


if __name__ == '__main__':
    main()
