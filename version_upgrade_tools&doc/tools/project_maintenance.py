#!/usr/bin/env python3
"""
AlgoTrendy v2.5 - Project Analysis & Reporting Tool
Safe read-only analysis of code duplication and file organization.

Usage:
    python3 project_maintenance.py                      # Full analysis report
    python3 project_maintenance.py --check-duplicates   # Find code duplication only
    python3 project_maintenance.py --analyze-files      # Analyze file organization only
    python3 project_maintenance.py . -t 0.80            # Legacy duplicate checker mode

Date: October 16, 2025
Purpose: Safe project analysis and quality reporting (NO FILE REMOVAL)
"""

import os
import sys
import argparse
import difflib
import shutil
from pathlib import Path
from datetime import datetime
from itertools import combinations

# Maximum file size (bytes) to compare; skip larger files
MAX_FILE_SIZE = 5 * 1024 * 1024  # 5MB

class ProjectAnalyzer:
    def __init__(self, base_path="."):
        self.base_path = Path(base_path).resolve()
        self.duplicates_found = []
        self.dev_files_found = []
        self.temp_files_found = []
        self.cache_dirs_found = []
        
    def log_action(self, action, item, reason=""):
        timestamp = datetime.now().strftime("%H:%M:%S")
        print(f"[{timestamp}] {action}: {item} {reason}")

    # ==================== DUPLICATE DETECTION ====================
    
    def find_py_files(self, root_dir):
        """Recursively collect all .py files under root_dir, excluding this script itself."""
        py_files = []
        this_file = os.path.abspath(__file__)
        for dirpath, _, filenames in os.walk(root_dir):
            # Skip virtual environments and node_modules
            if any(skip in dirpath for skip in ['.venv', 'node_modules', '__pycache__', '.git']):
                continue
                
            for fname in filenames:
                if not fname.endswith('.py'):
                    continue
                full_path = os.path.join(dirpath, fname)
                if os.path.abspath(full_path) == this_file:
                    continue
                try:
                    size = os.path.getsize(full_path)
                    if size == 0 or size > MAX_FILE_SIZE:
                        continue
                    py_files.append(full_path)
                except OSError:
                    continue
        return py_files

    def compute_similarity(self, file1, file2):
        """Compute a similarity ratio between two files using difflib."""
        try:
            with open(file1, 'r', encoding='utf-8', errors='ignore') as f1:
                text1 = f1.read()
            with open(file2, 'r', encoding='utf-8', errors='ignore') as f2:
                text2 = f2.read()
        except OSError:
            return 0.0
        seq = difflib.SequenceMatcher(None, text1, text2)
        return seq.ratio()

    def check_duplicates(self, threshold=0.75):
        """Check for code duplication above threshold"""
        print("🔍 Checking for code duplication...")
        print("=" * 50)
        
        files = self.find_py_files(self.base_path)
        if len(files) < 2:
            print('Not enough Python files to compare.')
            return
            
        self.log_action("🔍 SCANNING", f"{len(files)} Python files")
        
        duplicates_found = False
        for f1, f2 in combinations(files, 2):
            score = self.compute_similarity(f1, f2)
            if score >= threshold:
                duplicates_found = True
                rel_f1 = os.path.relpath(f1, self.base_path)
                rel_f2 = os.path.relpath(f2, self.base_path)
                self.duplicates_found.append((rel_f1, rel_f2, score))
                print(f"🔄 {rel_f1} <--> {rel_f2}: similarity {score:.3f}")
                
        if not duplicates_found:
            print('✅ No significant code duplication found.')
        else:
            print(f"\n⚠️  Found {len(self.duplicates_found)} potential duplications")

    # ==================== FILE ANALYSIS (READ-ONLY) ====================
    
    def analyze_development_files(self):
        """Analyze and report on development files that could be cleaned (READ-ONLY)"""
        print("📁 Analyzing file organization...")
        print("=" * 50)
        
        # Patterns to identify development/temporary files
        dev_patterns = [
            ("Development Scripts", ["*test*.py", "*debug*.py", "*temp*.py"]),
            ("Temporary Files", ["*.tmp", "*.bak", "*.old", "*.log~"]),
            ("Cache Directories", ["__pycache__"]),
            ("Log Files", ["*.log", "nohup.out"])
        ]
        
        total_found = 0
        
        for category, patterns in dev_patterns:
            category_files = []
            
            for pattern in patterns:
                if pattern == "__pycache__":
                    # Special handling for cache directories
                    for cache_dir in self.base_path.rglob("__pycache__"):
                        if ".venv" not in str(cache_dir):
                            rel_path = os.path.relpath(cache_dir, self.base_path)
                            category_files.append(rel_path)
                            self.cache_dirs_found.append(rel_path)
                else:
                    # Handle file patterns
                    for file_path in self.base_path.rglob(pattern):
                        if (file_path.is_file() and 
                            ".venv" not in str(file_path) and 
                            ".git" not in str(file_path)):
                            rel_path = os.path.relpath(file_path, self.base_path)
                            category_files.append(rel_path)
                            
                            if category == "Development Scripts":
                                self.dev_files_found.append(rel_path)
                            elif category == "Temporary Files":
                                self.temp_files_found.append(rel_path)
            
            if category_files:
                print(f"\n📂 {category} ({len(category_files)} found):")
                for file in sorted(category_files)[:10]:  # Show first 10
                    self.log_action("📄 FOUND", file, f"({category.lower()})")
                if len(category_files) > 10:
                    print(f"     ... and {len(category_files) - 10} more")
                total_found += len(category_files)
        
        if total_found == 0:
            print("✅ No development files found - project is well organized!")
        else:
            print(f"\n📊 Total files that could be cleaned: {total_found}")
            print("💡 Note: This is a read-only analysis. No files were modified.")

    def analyze_project_structure(self):
        """Analyze overall project structure and organization"""
        print("\n🏗️  Project Structure Analysis...")
        print("=" * 50)
        
        # Count files by type
        file_stats = {}
        dir_count = 0
        
        for item in self.base_path.rglob("*"):
            if ".venv" in str(item) or ".git" in str(item):
                continue
                
            if item.is_dir():
                dir_count += 1
            elif item.is_file():
                ext = item.suffix.lower()
                if not ext:
                    ext = "no extension"
                file_stats[ext] = file_stats.get(ext, 0) + 1
        
        # Display statistics
        print(f"📁 Directories: {dir_count}")
        print(f"📄 Files by type:")
        
        # Sort by count (most common first)
        sorted_stats = sorted(file_stats.items(), key=lambda x: x[1], reverse=True)
        for ext, count in sorted_stats[:15]:  # Show top 15 file types
            print(f"   {ext:15} : {count:4d} files")
        
        # Check for important directories
        important_dirs = [
            "algotrendy", "algotrendy-api", "algotrendy-web",
            "data", "docs", "scripts", "integrations"
        ]
        
        print(f"\n🎯 Core Directories Status:")
        for dir_name in important_dirs:
            dir_path = self.base_path / dir_name
            if dir_path.exists():
                file_count = len(list(dir_path.rglob("*"))) - len(list(dir_path.rglob("__pycache__")))
                print(f"   ✅ {dir_name:20} : {file_count:4d} items")
            else:
                print(f"   ❌ {dir_name:20} : Missing")

    def analyze_project_health(self):
        """Analyze overall project health indicators"""
        print(f"\n💚 Project Health Check...")
        print("=" * 50)
        
        health_score = 100
        issues = []
        
        # Check for excessive duplication
        if len(self.duplicates_found) > 5:
            health_score -= 20
            issues.append(f"High code duplication ({len(self.duplicates_found)} pairs)")
        
        # Check for too many temp files
        total_temp = len(self.dev_files_found) + len(self.temp_files_found) + len(self.cache_dirs_found)
        if total_temp > 20:
            health_score -= 15
            issues.append(f"Many temporary files ({total_temp})")
        elif total_temp > 10:
            health_score -= 5
            issues.append(f"Some temporary files ({total_temp})")
        
        # Check for missing core directories
        core_dirs = ["algotrendy", "algotrendy-api", "algotrendy-web"]
        missing_core = [d for d in core_dirs if not (self.base_path / d).exists()]
        if missing_core:
            health_score -= 30
            issues.append(f"Missing core directories: {', '.join(missing_core)}")
        
        # Display health status
        if health_score >= 90:
            status = "🟢 Excellent"
        elif health_score >= 75:
            status = "🟡 Good"
        elif health_score >= 60:
            status = "🟠 Fair"
        else:
            status = "🔴 Needs Attention"
        
        print(f"Overall Health Score: {health_score}/100 {status}")
        
        if issues:
            print(f"\n⚠️  Issues Found:")
            for issue in issues:
                print(f"   • {issue}")
        else:
            print(f"\n✅ No significant issues found!")
        
        return health_score

    # ==================== REPORTING ====================
    
    def generate_report(self):
        """Generate comprehensive analysis report"""
        print("\n" + "="*60)
        print("📊 PROJECT ANALYSIS REPORT")
        print("="*60)
        
        timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')
        
        print(f"\n🕐 **Timestamp**: {timestamp}")
        print(f"📁 **Project Path**: {self.base_path}")
        
        # Duplicate detection results
        if self.duplicates_found:
            print(f"\n🔄 **Code Duplication Analysis**:")
            print(f"   • Potential Duplicates: {len(self.duplicates_found)}")
            for f1, f2, score in self.duplicates_found[:5]:  # Show top 5
                print(f"   • {f1} ↔ {f2} ({score:.3f} similarity)")
            if len(self.duplicates_found) > 5:
                print(f"   • ... and {len(self.duplicates_found) - 5} more")
        else:
            print(f"\n🔄 **Code Duplication Analysis**: ✅ No significant duplication found")
        
        # File organization results
        total_cleanup_candidates = (len(self.dev_files_found) + 
                                  len(self.temp_files_found) + 
                                  len(self.cache_dirs_found))
        
        if total_cleanup_candidates > 0:
            print(f"\n📁 **File Organization Analysis**:")
            print(f"   • Development files: {len(self.dev_files_found)}")
            print(f"   • Temporary files: {len(self.temp_files_found)}")
            print(f"   • Cache directories: {len(self.cache_dirs_found)}")
            print(f"   • Total cleanup candidates: {total_cleanup_candidates}")
            print(f"   • 💡 Consider cleaning these files manually if not needed")
        else:
            print(f"\n📁 **File Organization Analysis**: ✅ Well organized, no cleanup needed")
        
        # Project health
        health_score = self.analyze_project_health()
        
        # Save detailed report
        report_path = self.base_path / f"PROJECT_ANALYSIS_{datetime.now().strftime('%Y-%m-%d_%H%M')}.md"
        with open(report_path, 'w') as f:
            f.write(f"# AlgoTrendy v2.5 - Project Analysis Report\n\n")
            f.write(f"**Date**: {timestamp}\n")
            f.write(f"**Project**: {self.base_path}\n")
            f.write(f"**Health Score**: {health_score}/100\n\n")
            
            # Duplication section
            f.write(f"## Code Duplication Analysis\n")
            if self.duplicates_found:
                f.write(f"Found {len(self.duplicates_found)} potential duplications:\n\n")
                for f1, f2, score in self.duplicates_found:
                    f.write(f"- `{f1}` ↔ `{f2}` (similarity: {score:.3f})\n")
            else:
                f.write(f"✅ No significant code duplication detected.\n")
            f.write(f"\n")
            
            # File organization section
            f.write(f"## File Organization Analysis\n")
            if total_cleanup_candidates > 0:
                f.write(f"Files that could be cleaned up:\n\n")
                
                if self.dev_files_found:
                    f.write(f"### Development Files ({len(self.dev_files_found)})\n")
                    for file in self.dev_files_found:
                        f.write(f"- `{file}`\n")
                    f.write(f"\n")
                
                if self.temp_files_found:
                    f.write(f"### Temporary Files ({len(self.temp_files_found)})\n")
                    for file in self.temp_files_found:
                        f.write(f"- `{file}`\n")
                    f.write(f"\n")
                
                if self.cache_dirs_found:
                    f.write(f"### Cache Directories ({len(self.cache_dirs_found)})\n")
                    for dir in self.cache_dirs_found:
                        f.write(f"- `{dir}`\n")
                    f.write(f"\n")
            else:
                f.write(f"✅ Project is well organized, no cleanup needed.\n\n")
                
            f.write(f"## Recommendations\n")
            if total_cleanup_candidates > 0:
                f.write(f"- Consider manually reviewing and removing unnecessary files\n")
            if self.duplicates_found:
                f.write(f"- Review duplicate code for refactoring opportunities\n")
            f.write(f"- Regular analysis helps maintain code quality\n\n")
            f.write(f"## Status\n✅ Analysis completed successfully (read-only)\n")
            
        print(f"\n📄 Detailed report saved: {report_path.name}")
        print(f"🔒 **SAFE MODE**: No files were modified during this analysis")

def main():
    """Main execution with argument parsing"""
    parser = argparse.ArgumentParser(
        description='AlgoTrendy v2.5 Project Maintenance Tool',
        epilog='Examples:\n  %(prog)s --check-duplicates\n  %(prog)s --cleanup\n  %(prog)s --full\n  %(prog)s . -t 0.80',
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    parser.add_argument('path', nargs='?', default='.', 
                       help='Directory path to analyze (default: current)')
    parser.add_argument('-t', '--threshold', type=float, default=0.75,
                       help='Similarity threshold for duplicate detection (0-1)')
    parser.add_argument('--check-duplicates', action='store_true',
                       help='Check for code duplication only')
    parser.add_argument('--analyze-files', action='store_true', 
                       help='Analyze file organization only')
    parser.add_argument('--full', action='store_true',
                       help='Run complete analysis (duplicates + files + health)')
    
    args = parser.parse_args()
    
    # Validate inputs
    if args.threshold < 0 or args.threshold > 1:
        print('Threshold must be between 0 and 1.')
        sys.exit(1)
        
    if not os.path.isdir(args.path):
        print(f"Path {args.path} is not a valid directory.")
        sys.exit(1)
    
    # Initialize analyzer tool
    analyzer = ProjectAnalyzer(args.path)
    
    print("� AlgoTrendy v2.5 - Project Analysis Tool (Safe Read-Only Mode)")
    print("=" * 65)
    
    try:
        # Determine operation mode
        if args.full:
            analyzer.check_duplicates(args.threshold)
            print()
            analyzer.analyze_development_files()
            analyzer.analyze_project_structure()
        elif args.check_duplicates or (not args.analyze_files and not args.full):
            # Default to duplicate checking if no specific operation specified
            analyzer.check_duplicates(args.threshold)
        elif args.analyze_files:
            analyzer.analyze_development_files()
            analyzer.analyze_project_structure()
        
        # Generate report
        analyzer.generate_report()
        
        print(f"\n🎉 **Analysis Complete!**")
        
    except Exception as e:
        print(f"\n❌ **ERROR**: Analysis failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()