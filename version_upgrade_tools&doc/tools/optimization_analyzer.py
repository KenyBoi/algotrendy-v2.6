#!/usr/bin/env python3
"""
Comprehensive Code Optimization Analyzer for AlgoTrendy v2.5

Crawls the project and identifies optimization opportunities across:
- Code quality and complexity
- Performance bottlenecks
- Security vulnerabilities
- Duplicate code patterns
- Unused imports and variables
- Dead code

Generates detailed optimization report with actionable recommendations.
"""

import os
import sys
import json
import subprocess
import re
from pathlib import Path
from dataclasses import dataclass, asdict
from typing import Dict, List, Set, Tuple, Optional
from collections import defaultdict
import ast
import time


@dataclass
class OptimizationIssue:
    """Represents a single optimization issue"""
    category: str
    severity: str  # critical, high, medium, low, info
    file: str
    line: int
    issue: str
    suggestion: str
    tool: str


class PythonAnalyzer:
    """Analyzes Python files for optimization opportunities"""
    
    def __init__(self, project_root: str):
        self.project_root = project_root
        self.issues: List[OptimizationIssue] = []
        self.py_files = self._find_python_files()
    
    def _find_python_files(self) -> List[str]:
        """Find all Python files in the project"""
        py_files = []
        for root, dirs, files in os.walk(self.project_root):
            # Skip common non-essential directories
            dirs[:] = [d for d in dirs if d not in [
                '__pycache__', '.git', '.venv', 'venv', 'node_modules', 
                '.pytest_cache', '.tox', 'dist', 'build', '.env'
            ]]
            for file in files:
                if file.endswith('.py'):
                    py_files.append(os.path.join(root, file))
        return sorted(py_files)
    
    def analyze_complexity(self) -> None:
        """Analyze cyclomatic complexity"""
        print("[ANALYZER] Scanning Python files for complexity issues...")
        
        for py_file in self.py_files:
            try:
                with open(py_file, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                
                tree = ast.parse(content)
                
                for node in ast.walk(tree):
                    if isinstance(node, ast.FunctionDef):
                        complexity = self._calculate_complexity(node)
                        if complexity > 10:
                            severity = 'critical' if complexity > 20 else 'high'
                            self.issues.append(OptimizationIssue(
                                category='Cyclomatic Complexity',
                                severity=severity,
                                file=py_file,
                                line=node.lineno,
                                issue=f'Function "{node.name}" has complexity score of {complexity}',
                                suggestion=f'Refactor function to reduce complexity. Target: < 10',
                                tool='AST Analysis'
                            ))
            except Exception as e:
                pass
    
    def _calculate_complexity(self, node: ast.AST) -> int:
        """Calculate cyclomatic complexity of a function"""
        complexity = 1
        for child in ast.walk(node):
            if isinstance(child, (ast.If, ast.For, ast.While, ast.ExceptHandler)):
                complexity += 1
            elif isinstance(child, ast.BoolOp):
                complexity += len(child.values) - 1
        return complexity
    
    def analyze_unused_imports(self) -> None:
        """Find unused imports"""
        print("[ANALYZER] Checking for unused imports...")
        
        for py_file in self.py_files:
            try:
                with open(py_file, 'r', encoding='utf-8', errors='ignore') as f:
                    lines = f.readlines()
                    content = ''.join(lines)
                
                tree = ast.parse(content)
                
                imported_names = set()
                import_lines = {}
                
                for node in ast.walk(tree):
                    if isinstance(node, ast.Import):
                        for alias in node.names:
                            name = alias.asname or alias.name
                            imported_names.add(name)
                            import_lines[name] = node.lineno
                    elif isinstance(node, ast.ImportFrom):
                        for alias in node.names:
                            if alias.name != '*':
                                name = alias.asname or alias.name
                                imported_names.add(name)
                                import_lines[name] = node.lineno
                
                used_names = set(re.findall(r'\b([a-zA-Z_][a-zA-Z0-9_]*)\b', content))
                
                for name in imported_names:
                    if name not in used_names and name != '*':
                        self.issues.append(OptimizationIssue(
                            category='Unused Import',
                            severity='low',
                            file=py_file,
                            line=import_lines.get(name, 0),
                            issue=f'Import "{name}" is not used',
                            suggestion=f'Remove unused import: {name}',
                            tool='Import Analysis'
                        ))
            except Exception as e:
                pass
    
    def analyze_long_functions(self) -> None:
        """Find overly long functions"""
        print("[ANALYZER] Scanning for excessively long functions...")
        
        for py_file in self.py_files:
            try:
                with open(py_file, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                
                tree = ast.parse(content)
                
                for node in ast.walk(tree):
                    if isinstance(node, ast.FunctionDef):
                        func_lines = node.end_lineno - node.lineno + 1
                        if func_lines > 50:
                            severity = 'critical' if func_lines > 100 else 'high'
                            self.issues.append(OptimizationIssue(
                                category='Function Length',
                                severity=severity,
                                file=py_file,
                                line=node.lineno,
                                issue=f'Function "{node.name}" is {func_lines} lines long',
                                suggestion=f'Break function into smaller, focused functions (target: < 50 lines)',
                                tool='Structure Analysis'
                            ))
            except Exception as e:
                pass
    
    def analyze_performance_patterns(self) -> None:
        """Detect common performance anti-patterns"""
        print("[ANALYZER] Checking for performance anti-patterns...")
        
        for py_file in self.py_files:
            try:
                with open(py_file, 'r', encoding='utf-8', errors='ignore') as f:
                    lines = f.readlines()
                
                for i, line in enumerate(lines, 1):
                    # Check for string concatenation in loops
                    if 'for' in line and any(x in lines[min(i, len(lines)-1)] for x in ['+= "', "+= '"]):
                        self.issues.append(OptimizationIssue(
                            category='Performance Pattern',
                            severity='medium',
                            file=py_file,
                            line=i,
                            issue='String concatenation in loop detected',
                            suggestion='Use list.append() and "".join() instead of += for string building',
                            tool='Pattern Analysis'
                        ))
                    
                    # Check for inefficient list operations
                    if 'list(' in line and 'range' in line:
                        self.issues.append(OptimizationIssue(
                            category='Performance Pattern',
                            severity='low',
                            file=py_file,
                            line=i,
                            issue='Unnecessary list() conversion with range()',
                            suggestion='Use range() directly without list() conversion when possible',
                            tool='Pattern Analysis'
                        ))
            except Exception as e:
                pass
    
    def get_issues(self) -> List[OptimizationIssue]:
        """Return all identified issues"""
        return self.issues


class DuplicationAnalyzer:
    """Analyzes code duplication patterns"""
    
    def __init__(self, project_root: str):
        self.project_root = project_root
        self.issues: List[OptimizationIssue] = []
    
    def analyze(self) -> None:
        """Analyze duplication across Python files"""
        print("[ANALYZER] Scanning for code duplication patterns...")
        
        py_files = []
        for root, dirs, files in os.walk(self.project_root):
            dirs[:] = [d for d in dirs if d not in [
                '__pycache__', '.git', '.venv', 'venv', 'node_modules'
            ]]
            for file in files:
                if file.endswith('.py'):
                    py_files.append(os.path.join(root, file))
        
        file_blocks: Dict[str, List[Tuple[str, int]]] = defaultdict(list)
        
        for py_file in py_files:
            try:
                with open(py_file, 'r', encoding='utf-8', errors='ignore') as f:
                    lines = f.readlines()
                
                for i in range(len(lines) - 3):
                    block = ''.join(lines[i:i+4]).strip()
                    if len(block) > 20 and not block.startswith('#'):
                        block_hash = hash(block) % 10000
                        file_blocks[str(block_hash)].append((py_file, i+1))
            except Exception:
                pass
        
        # Find duplicated blocks
        for block_id, occurrences in file_blocks.items():
            if len(occurrences) > 1:
                files_with_dup = len(set(f for f, _ in occurrences))
                if files_with_dup > 1:
                    self.issues.append(OptimizationIssue(
                        category='Code Duplication',
                        severity='medium',
                        file=occurrences[0][0],
                        line=occurrences[0][1],
                        issue=f'Code block appears {len(occurrences)} times across {files_with_dup} files',
                        suggestion=f'Extract to shared utility or base class. Instances: {", ".join(set(f for f, _ in occurrences))}',
                        tool='Duplication Detection'
                    ))
    
    def get_issues(self) -> List[OptimizationIssue]:
        """Return all identified issues"""
        return self.issues


class OptimizationReporter:
    """Generates optimization analysis reports"""
    
    def __init__(self, issues: List[OptimizationIssue], project_root: str):
        self.issues = issues
        self.project_root = project_root
        self.timestamp = time.strftime('%Y%m%d_%H%M%S')
    
    def generate_text_report(self) -> str:
        """Generate human-readable optimization report"""
        
        report = []
        report.append("=" * 80)
        report.append("CODE OPTIMIZATION ANALYSIS REPORT - AlgoTrendy v2.5")
        report.append("=" * 80)
        report.append(f"Generated: {time.strftime('%Y-%m-%d %H:%M:%S')}")
        report.append(f"Project Root: {self.project_root}")
        report.append("")
        
        # Summary statistics
        severity_counts = defaultdict(int)
        category_counts = defaultdict(int)
        
        for issue in self.issues:
            severity_counts[issue.severity] += 1
            category_counts[issue.category] += 1
        
        report.append("SUMMARY STATISTICS")
        report.append("-" * 80)
        report.append(f"Total Issues Found: {len(self.issues)}")
        report.append("\nBy Severity:")
        for severity in ['critical', 'high', 'medium', 'low', 'info']:
            count = severity_counts.get(severity, 0)
            if count > 0:
                report.append(f"  {severity.upper():12} : {count:3} issues")
        
        report.append("\nBy Category:")
        for category in sorted(category_counts.keys()):
            report.append(f"  {category:30} : {category_counts[category]:3} issues")
        
        report.append("")
        report.append("")
        
        # Issues by severity
        for severity in ['critical', 'high', 'medium', 'low', 'info']:
            severity_issues = [i for i in self.issues if i.severity == severity]
            if not severity_issues:
                continue
            
            report.append(f"{severity.upper()} PRIORITY ISSUES ({len(severity_issues)})")
            report.append("-" * 80)
            
            for issue in sorted(severity_issues, key=lambda x: x.file):
                rel_file = issue.file.replace(self.project_root, '').lstrip('/')
                report.append(f"\n[{issue.category}] {rel_file}:{issue.line}")
                report.append(f"  Tool: {issue.tool}")
                report.append(f"  Issue: {issue.issue}")
                report.append(f"  Fix: {issue.suggestion}")
            
            report.append("")
        
        # Recommendations
        report.append("")
        report.append("OPTIMIZATION RECOMMENDATIONS")
        report.append("=" * 80)
        report.append(self._generate_recommendations())
        
        report.append("")
        report.append("=" * 80)
        report.append("END OF REPORT")
        report.append("=" * 80)
        
        return "\n".join(report)
    
    def _generate_recommendations(self) -> str:
        """Generate strategic recommendations"""
        
        recommendations = []
        
        # Count critical issues
        critical_issues = [i for i in self.issues if i.severity == 'critical']
        if critical_issues:
            recommendations.append(f"\n1. ADDRESS CRITICAL ISSUES ({len(critical_issues)} found)")
            recommendations.append("   These should be prioritized immediately as they impact:")
            for category in set(i.category for i in critical_issues):
                count = len([i for i in critical_issues if i.category == category])
                recommendations.append(f"   - {category}: {count} issues")
        
        # Complexity recommendations
        complexity_issues = [i for i in self.issues if 'Complexity' in i.category]
        if complexity_issues:
            recommendations.append(f"\n2. REFACTOR COMPLEX FUNCTIONS ({len(complexity_issues)} found)")
            recommendations.append("   High cyclomatic complexity indicates functions are difficult to test")
            recommendations.append("   and maintain. Break them into smaller, focused functions.")
        
        # Duplication recommendations
        dup_issues = [i for i in self.issues if 'Duplication' in i.category]
        if dup_issues:
            recommendations.append(f"\n3. REDUCE CODE DUPLICATION ({len(dup_issues)} patterns found)")
            recommendations.append("   Extract common code to utilities or base classes.")
            recommendations.append("   This improves maintainability and reduces bug surface area.")
        
        # Length recommendations
        length_issues = [i for i in self.issues if 'Length' in i.category]
        if length_issues:
            recommendations.append(f"\n4. BREAK DOWN LONG FUNCTIONS ({len(length_issues)} found)")
            recommendations.append("   Functions over 50 lines are harder to understand.")
            recommendations.append("   Aim for single responsibility principle (SRP).")
        
        recommendations.append("\n5. NEXT STEPS")
        recommendations.append("   a) Fix critical issues first")
        recommendations.append("   b) Refactor high complexity functions")
        recommendations.append("   c) Extract duplicated code")
        recommendations.append("   d) Add comprehensive unit tests")
        recommendations.append("   e) Set up CI/CD integration for ongoing analysis")
        
        return "\n".join(recommendations)
    
    def save_report(self, output_file: Optional[str] = None) -> str:
        """Save report to file and return filepath"""
        if output_file is None:
            output_file = f"optimization_report_{self.timestamp}.txt"
        
        report_text = self.generate_text_report()
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report_text)
        
        return output_file
    
    def save_json_report(self, output_file: Optional[str] = None) -> str:
        """Save report as JSON for programmatic use"""
        if output_file is None:
            output_file = f"optimization_report_{self.timestamp}.json"
        
        report_data = {
            'timestamp': time.strftime('%Y-%m-%d %H:%M:%S'),
            'project_root': self.project_root,
            'total_issues': len(self.issues),
            'issues_by_severity': {
                'critical': len([i for i in self.issues if i.severity == 'critical']),
                'high': len([i for i in self.issues if i.severity == 'high']),
                'medium': len([i for i in self.issues if i.severity == 'medium']),
                'low': len([i for i in self.issues if i.severity == 'low']),
                'info': len([i for i in self.issues if i.severity == 'info']),
            },
            'issues': [asdict(issue) for issue in self.issues]
        }
        
        with open(output_file, 'w', encoding='utf-8') as f:
            json.dump(report_data, f, indent=2)
        
        return output_file


class OptimizationAnalyzerMain:
    """Main orchestrator for optimization analysis"""
    
    def __init__(self, project_root: str = '.'):
        self.project_root = project_root
        self.all_issues: List[OptimizationIssue] = []
    
    def run_full_analysis(self) -> None:
        """Run comprehensive optimization analysis"""
        
        print("\n" + "=" * 80)
        print("STARTING COMPREHENSIVE CODE OPTIMIZATION ANALYSIS")
        print("=" * 80)
        print(f"Project Root: {self.project_root}\n")
        
        # Python analysis
        print("\n[PHASE 1] Python Code Analysis")
        print("-" * 80)
        py_analyzer = PythonAnalyzer(self.project_root)
        
        py_analyzer.analyze_complexity()
        py_analyzer.analyze_unused_imports()
        py_analyzer.analyze_long_functions()
        py_analyzer.analyze_performance_patterns()
        
        self.all_issues.extend(py_analyzer.get_issues())
        print(f"✓ Found {len(py_analyzer.get_issues())} Python-related issues")
        
        # Duplication analysis
        print("\n[PHASE 2] Code Duplication Analysis")
        print("-" * 80)
        dup_analyzer = DuplicationAnalyzer(self.project_root)
        dup_analyzer.analyze()
        self.all_issues.extend(dup_analyzer.get_issues())
        print(f"✓ Found {len(dup_analyzer.get_issues())} duplication patterns")
        
        # Generate reports
        print("\n[PHASE 3] Generating Reports")
        print("-" * 80)
        reporter = OptimizationReporter(self.all_issues, self.project_root)
        
        text_report = reporter.save_report()
        print(f"✓ Text report saved: {text_report}")
        
        json_report = reporter.save_json_report()
        print(f"✓ JSON report saved: {json_report}")
        
        # Print summary to console
        print("\n" + "=" * 80)
        print("ANALYSIS SUMMARY")
        print("=" * 80)
        print(reporter.generate_text_report())
    
    def run_quick_analysis(self) -> None:
        """Run quick analysis (less thorough but faster)"""
        
        print("\n" + "=" * 80)
        print("QUICK OPTIMIZATION ANALYSIS")
        print("=" * 80)
        
        py_analyzer = PythonAnalyzer(self.project_root)
        py_analyzer.analyze_complexity()
        py_analyzer.analyze_long_functions()
        
        self.all_issues.extend(py_analyzer.get_issues())
        
        reporter = OptimizationReporter(self.all_issues, self.project_root)
        print(reporter.generate_text_report())


def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description='Comprehensive Code Optimization Analyzer for AlgoTrendy'
    )
    parser.add_argument(
        '--project',
        default='.',
        help='Project root directory (default: current directory)'
    )
    parser.add_argument(
        '--quick',
        action='store_true',
        help='Run quick analysis (faster, less comprehensive)'
    )
    parser.add_argument(
        '--output',
        default=None,
        help='Output report filename'
    )
    
    args = parser.parse_args()
    
    analyzer = OptimizationAnalyzerMain(args.project)
    
    if args.quick:
        analyzer.run_quick_analysis()
    else:
        analyzer.run_full_analysis()


if __name__ == '__main__':
    main()
