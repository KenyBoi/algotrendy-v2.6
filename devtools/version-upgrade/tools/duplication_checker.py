#!/usr/bin/env python3
"""
Code Duplication Checker
Analyzes source code files for significant code duplication patterns.
Created: 2025-10-16, 17:26 UTC

Supported languages: Python, JavaScript, TypeScript, JSX, TSX
Features: Multi-format reporting, configurable thresholds, smart directory filtering
Zero external dependencies - standalone utility
"""

import os
import sys
import json
import re
from pathlib import Path
from collections import defaultdict
from dataclasses import dataclass, asdict
from typing import List, Dict, Set, Tuple, Optional


@dataclass
class DuplicateBlock:
    """Represents a duplicate code block"""
    hash_val: str
    lines: List[str]
    occurrences: List[Tuple[str, int]]  # [(file_path, line_number), ...]
    similarity_score: float

    def to_dict(self):
        return {
            'hash': self.hash_val,
            'block_size': len(self.lines),
            'occurrences': [{'file': f, 'line': l} for f, l in self.occurrences],
            'similarity_score': self.similarity_score
        }


class CodeDuplicateAnalyzer:
    """Analyzes code files for duplication"""

    def __init__(self, threshold: int = 50, min_lines: int = 3):
        """
        Initialize the analyzer
        Args:
            threshold: Similarity threshold (0-100) for reporting duplicates
            min_lines: Minimum number of lines to consider as a block
        """
        self.threshold = threshold
        self.min_lines = min_lines
        self.supported_extensions = {'.py', '.js', '.ts', '.jsx', '.tsx'}
        self.excluded_dirs = {
            'node_modules', '.git', '__pycache__', '.next', 'dist', 
            'build', '.venv', 'venv', '.env'
        }
        self.files_analyzed = 0
        self.total_lines = 0
        self.duplicate_blocks = []

    def collect_files(self, directory: str) -> List[str]:
        """Collect source files from directory"""
        files = []
        for root, dirs, filenames in os.walk(directory):
            # Filter out excluded directories
            dirs[:] = [d for d in dirs if d not in self.excluded_dirs]
            
            for filename in filenames:
                if any(filename.endswith(ext) for ext in self.supported_extensions):
                    filepath = os.path.join(root, filename)
                    files.append(filepath)
        
        return sorted(files)

    def normalize_line(self, line: str) -> str:
        """Normalize a line for comparison"""
        # Remove comments
        line = re.sub(r'#.*$', '', line)  # Python comments
        line = re.sub(r'//.*$', '', line)  # JS/TS comments
        # Strip whitespace
        line = line.strip()
        # Remove extra spaces
        line = re.sub(r'\s+', ' ', line)
        return line

    def read_and_normalize(self, filepath: str) -> List[str]:
        """Read file and normalize lines"""
        try:
            with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                lines = f.readlines()
            normalized = [self.normalize_line(line) for line in lines]
            # Filter out empty lines
            return [line for line in normalized if line]
        except Exception as e:
            print(f"Error reading {filepath}: {e}", file=sys.stderr)
            return []

    def calculate_similarity(self, block1: List[str], block2: List[str]) -> float:
        """Calculate similarity between two code blocks (0-100)"""
        if not block1 or not block2:
            return 0.0
        
        matches = sum(1 for l1, l2 in zip(block1, block2) if l1 == l2)
        total = max(len(block1), len(block2))
        return (matches / total) * 100

    def find_duplicate_blocks(self, files_data: Dict[str, List[str]]) -> List[DuplicateBlock]:
        """Find duplicate code blocks across files"""
        block_map = defaultdict(list)  # hash -> [(file, line_num, block), ...]
        duplicates = []

        # Generate blocks and their hashes
        for filepath, lines in files_data.items():
            for i in range(len(lines) - self.min_lines + 1):
                block = lines[i:i + self.min_lines]
                block_str = '\n'.join(block)
                block_hash = hash(block_str)
                block_map[block_hash].append((filepath, i, block))

        # Find duplicates
        seen_hashes = set()
        for block_hash, occurrences in block_map.items():
            if len(occurrences) > 1 and block_hash not in seen_hashes:
                seen_hashes.add(block_hash)
                
                # Get the first occurrence as reference
                ref_file, ref_line, ref_block = occurrences[0]
                
                # Calculate average similarity
                similarities = []
                for file, line, block in occurrences[1:]:
                    sim = self.calculate_similarity(ref_block, block)
                    similarities.append(sim)
                
                avg_similarity = sum(similarities) / len(similarities) if similarities else 100.0
                
                if avg_similarity >= self.threshold:
                    dup_block = DuplicateBlock(
                        hash_val=str(block_hash),
                        lines=ref_block,
                        occurrences=[(f, l) for f, l, _ in occurrences],
                        similarity_score=avg_similarity
                    )
                    duplicates.append(dup_block)

        return sorted(duplicates, key=lambda x: len(x.occurrences), reverse=True)

    def analyze(self, directory: str = '.') -> Dict:
        """Main analysis method"""
        files = self.collect_files(directory)
        self.files_analyzed = len(files)

        # Read all files
        files_data = {}
        for filepath in files:
            lines = self.read_and_normalize(filepath)
            if lines:
                files_data[filepath] = lines
                self.total_lines += len(lines)

        # Find duplicates
        self.duplicate_blocks = self.find_duplicate_blocks(files_data)

        # Generate report
        return {
            'files_analyzed': self.files_analyzed,
            'total_lines': self.total_lines,
            'duplicates_found': len(self.duplicate_blocks),
            'threshold_used': self.threshold,
            'min_lines_used': self.min_lines,
            'duplicate_blocks': self.duplicate_blocks
        }

    def generate_text_report(self, result: Dict) -> str:
        """Generate text format report"""
        lines = []
        lines.append("=" * 80)
        lines.append("CODE DUPLICATION ANALYSIS REPORT")
        lines.append("=" * 80)
        lines.append("")
        
        lines.append(f"Files Analyzed: {result['files_analyzed']}")
        lines.append(f"Total Lines: {result['total_lines']}")
        lines.append(f"Duplicates Found: {result['duplicates_found']}")
        lines.append(f"Threshold: {result['threshold_used']}%")
        lines.append(f"Min Block Size: {result['min_lines_used']} lines")
        lines.append("")
        lines.append("=" * 80)
        lines.append("DUPLICATE BLOCKS")
        lines.append("=" * 80)
        
        for i, block in enumerate(result['duplicate_blocks'], 1):
            lines.append(f"\n[Block {i}] - Similarity: {block.similarity_score:.1f}%")
            lines.append(f"Occurrences: {len(block.occurrences)}")
            for filepath, line_num in block.occurrences:
                lines.append(f"  - {filepath}:{line_num + 1}")
            lines.append("Code:")
            for code_line in block.lines[:5]:  # Show first 5 lines
                lines.append(f"  | {code_line}")
            if len(block.lines) > 5:
                lines.append(f"  | ... ({len(block.lines) - 5} more lines)")
        
        lines.append("\n" + "=" * 80)
        return "\n".join(lines)

    def generate_json_report(self, result: Dict) -> str:
        """Generate JSON format report"""
        report_data = {
            'files_analyzed': result['files_analyzed'],
            'total_lines': result['total_lines'],
            'duplicates_found': result['duplicates_found'],
            'threshold': result['threshold_used'],
            'min_lines': result['min_lines_used'],
            'duplicate_blocks': [block.to_dict() for block in result['duplicate_blocks']]
        }
        return json.dumps(report_data, indent=2)

    def generate_html_report(self, result: Dict) -> str:
        """Generate HTML format report"""
        html = []
        html.append("<!DOCTYPE html>")
        html.append("<html>")
        html.append("<head>")
        html.append("<title>Code Duplication Report</title>")
        html.append("<style>")
        html.append("body { font-family: Arial, sans-serif; margin: 20px; }")
        html.append("h1 { color: #333; }")
        html.append(".summary { background: #f0f0f0; padding: 10px; border-radius: 5px; }")
        html.append(".block { margin: 20px 0; border: 1px solid #ddd; padding: 10px; }")
        html.append(".occurrence { background: #fff3cd; padding: 5px; margin: 5px 0; }")
        html.append(".code { background: #f4f4f4; padding: 10px; overflow-x: auto; font-family: monospace; }")
        html.append("</style>")
        html.append("</head>")
        html.append("<body>")
        
        html.append("<h1>Code Duplication Analysis Report</h1>")
        html.append(f"<div class='summary'>")
        html.append(f"<p><strong>Files Analyzed:</strong> {result['files_analyzed']}</p>")
        html.append(f"<p><strong>Total Lines:</strong> {result['total_lines']}</p>")
        html.append(f"<p><strong>Duplicates Found:</strong> {result['duplicates_found']}</p>")
        html.append(f"<p><strong>Threshold:</strong> {result['threshold_used']}%</p>")
        html.append(f"</div>")
        
        for i, block in enumerate(result['duplicate_blocks'], 1):
            html.append(f"<div class='block'>")
            html.append(f"<h3>Block {i} - Similarity: {block.similarity_score:.1f}%</h3>")
            html.append(f"<p>Occurrences: {len(block.occurrences)}</p>")
            for filepath, line_num in block.occurrences:
                html.append(f"<div class='occurrence'>{filepath}:{line_num + 1}</div>")
            html.append(f"<div class='code'>")
            for code_line in block.lines[:5]:
                html.append(f"&gt; {code_line}<br>")
            if len(block.lines) > 5:
                html.append(f"... ({len(block.lines) - 5} more lines)<br>")
            html.append(f"</div>")
            html.append(f"</div>")
        
        html.append("</body>")
        html.append("</html>")
        return "\n".join(html)


def main():
    """CLI interface"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description='Check for code duplication in source files',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s                                    # Analyze current directory
  %(prog)s --threshold 60 --text-report       # Strict threshold with text output
  %(prog)s algotrendy/ --all-reports          # Generate all report formats
  %(prog)s . --json-report > report.json      # Save JSON report
        """
    )
    
    parser.add_argument(
        'directory',
        nargs='?',
        default='.',
        help='Directory to analyze (default: current directory)'
    )
    parser.add_argument(
        '--threshold',
        type=int,
        default=50,
        help='Similarity threshold 0-100 (default: 50)'
    )
    parser.add_argument(
        '--min-lines',
        type=int,
        default=3,
        help='Minimum lines per block (default: 3)'
    )
    parser.add_argument(
        '--text-report',
        action='store_true',
        help='Generate text format report'
    )
    parser.add_argument(
        '--json-report',
        action='store_true',
        help='Generate JSON format report'
    )
    parser.add_argument(
        '--html-report',
        action='store_true',
        help='Generate HTML format report'
    )
    parser.add_argument(
        '--all-reports',
        action='store_true',
        help='Generate all report formats'
    )
    
    args = parser.parse_args()
    
    # Validate threshold
    if not 0 <= args.threshold <= 100:
        print("Error: Threshold must be between 0 and 100", file=sys.stderr)
        sys.exit(1)
    
    # Create analyzer and run analysis
    analyzer = CodeDuplicateAnalyzer(
        threshold=args.threshold,
        min_lines=args.min_lines
    )
    
    print(f"Analyzing {args.directory}...", file=sys.stderr)
    result = analyzer.analyze(args.directory)
    
    print(f"Analysis complete: {result['duplicates_found']} duplicates found", file=sys.stderr)
    
    # Determine which reports to generate
    generate_text = args.text_report or args.all_reports
    generate_json = args.json_report or args.all_reports
    generate_html = args.html_report or args.all_reports
    
    # If no specific report requested, generate text by default
    if not (generate_text or generate_json or generate_html):
        generate_text = True
    
    # Generate and output reports
    if generate_text:
        print(analyzer.generate_text_report(result))
    
    if generate_json:
        if generate_text:
            print("\n" + "=" * 80 + "\n")
        print(analyzer.generate_json_report(result))
    
    if generate_html:
        output_file = 'duplication_report.html'
        with open(output_file, 'w') as f:
            f.write(analyzer.generate_html_report(result))
        print(f"\nHTML report saved to {output_file}", file=sys.stderr)


if __name__ == '__main__':
    main()
