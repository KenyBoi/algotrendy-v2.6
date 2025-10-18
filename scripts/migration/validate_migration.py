#!/usr/bin/env python3
"""
AlgoTrendy v2.6 - Migration Validation Script
Performs security and syntax checks on migrated files
"""

import os
import re
import json
import sys
from pathlib import Path
from typing import List, Dict, Tuple

# ANSI color codes
RED = '\033[0;31m'
GREEN = '\033[0;32m'
YELLOW = '\033[1;33m'
BLUE = '\033[0;34m'
NC = '\033[0m'  # No Color

V26_DIR = Path("/root/AlgoTrendy_v2.6")

# Security patterns to detect
SECURITY_PATTERNS = {
    'hardcoded_secrets': [
        r'password\s*=\s*["\'][^"\']+["\']',
        r'api_key\s*=\s*["\'][^"\']+["\']',
        r'api_secret\s*=\s*["\'][^"\']+["\']',
        r'token\s*=\s*["\'][^"\']+["\']',
        r'private_key\s*=\s*["\'][^"\']+["\']',
    ],
    'sql_injection': [
        r'f["\'].*SELECT.*\{.*\}.*["\']',
        r'f["\'].*INSERT.*\{.*\}.*["\']',
        r'f["\'].*UPDATE.*\{.*\}.*["\']',
        r'f["\'].*DELETE.*\{.*\}.*["\']',
        r'\.format\(.*\).*SELECT',
        r'%.*%.*SELECT',
    ],
    'eval_usage': [
        r'\beval\s*\(',
        r'\bexec\s*\(',
    ],
    'insecure_random': [
        r'random\.random\(',
        r'random\.randint\(',
    ],
}

# Allowed exceptions (e.g., in .env.example)
ALLOWED_PATTERNS = [
    r'CHANGE_ME',
    r'your_.*_here',
    r'example',
    r'\.env\.example',
    r'# Template',
]


class ValidationResult:
    def __init__(self):
        self.errors: List[Dict] = []
        self.warnings: List[Dict] = []
        self.info: List[Dict] = []

    def add_error(self, file: str, line: int, category: str, message: str):
        self.errors.append({
            'file': file,
            'line': line,
            'category': category,
            'message': message,
            'severity': 'ERROR'
        })

    def add_warning(self, file: str, line: int, category: str, message: str):
        self.warnings.append({
            'file': file,
            'line': line,
            'category': category,
            'message': message,
            'severity': 'WARNING'
        })

    def add_info(self, message: str):
        self.info.append({'message': message})

    def has_errors(self) -> bool:
        return len(self.errors) > 0

    def print_summary(self):
        print(f"\n{BLUE}{'='*60}{NC}")
        print(f"{BLUE}Validation Summary{NC}")
        print(f"{BLUE}{'='*60}{NC}\n")

        if self.errors:
            print(f"{RED}✗ ERRORS: {len(self.errors)}{NC}")
            for error in self.errors:
                print(f"  {RED}[{error['category']}]{NC} {error['file']}:{error['line']}")
                print(f"    {error['message']}\n")

        if self.warnings:
            print(f"{YELLOW}⚠ WARNINGS: {len(self.warnings)}{NC}")
            for warning in self.warnings:
                print(f"  {YELLOW}[{warning['category']}]{NC} {warning['file']}:{warning['line']}")
                print(f"    {warning['message']}\n")

        if not self.errors and not self.warnings:
            print(f"{GREEN}✓ All checks passed!{NC}\n")

        print(f"{BLUE}{'='*60}{NC}\n")


def is_allowed_exception(line: str, file_path: str) -> bool:
    """Check if a line matches allowed exception patterns"""
    for pattern in ALLOWED_PATTERNS:
        if re.search(pattern, line, re.IGNORECASE) or re.search(pattern, str(file_path), re.IGNORECASE):
            return True
    return False


def scan_file_for_security_issues(file_path: Path, result: ValidationResult):
    """Scan a single file for security issues"""
    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            lines = f.readlines()

        for line_num, line in enumerate(lines, start=1):
            # Skip allowed exceptions
            if is_allowed_exception(line, file_path):
                continue

            # Check for hardcoded secrets
            for pattern in SECURITY_PATTERNS['hardcoded_secrets']:
                if re.search(pattern, line, re.IGNORECASE):
                    result.add_error(
                        str(file_path.relative_to(V26_DIR)),
                        line_num,
                        'HARDCODED_SECRET',
                        f"Potential hardcoded secret detected: {line.strip()}"
                    )

            # Check for SQL injection vulnerabilities
            for pattern in SECURITY_PATTERNS['sql_injection']:
                if re.search(pattern, line, re.IGNORECASE):
                    result.add_error(
                        str(file_path.relative_to(V26_DIR)),
                        line_num,
                        'SQL_INJECTION',
                        f"Potential SQL injection vulnerability: {line.strip()}"
                    )

            # Check for dangerous eval/exec usage
            for pattern in SECURITY_PATTERNS['eval_usage']:
                if re.search(pattern, line):
                    result.add_warning(
                        str(file_path.relative_to(V26_DIR)),
                        line_num,
                        'DANGEROUS_FUNCTION',
                        f"Use of eval/exec detected: {line.strip()}"
                    )

            # Check for insecure random usage
            for pattern in SECURITY_PATTERNS['insecure_random']:
                if re.search(pattern, line) and ('secret' in line.lower() or 'token' in line.lower()):
                    result.add_warning(
                        str(file_path.relative_to(V26_DIR)),
                        line_num,
                        'INSECURE_RANDOM',
                        f"Insecure random for security-critical value: {line.strip()}"
                    )

    except Exception as e:
        result.add_warning(
            str(file_path.relative_to(V26_DIR)),
            0,
            'SCAN_ERROR',
            f"Error scanning file: {str(e)}"
        )


def validate_json_syntax(file_path: Path, result: ValidationResult):
    """Validate JSON file syntax"""
    try:
        with open(file_path, 'r') as f:
            json.load(f)
    except json.JSONDecodeError as e:
        result.add_error(
            str(file_path.relative_to(V26_DIR)),
            e.lineno,
            'JSON_SYNTAX',
            f"Invalid JSON: {str(e)}"
        )
    except Exception as e:
        result.add_warning(
            str(file_path.relative_to(V26_DIR)),
            0,
            'JSON_READ_ERROR',
            f"Error reading JSON file: {str(e)}"
        )


def validate_python_syntax(file_path: Path, result: ValidationResult):
    """Validate Python file syntax"""
    try:
        with open(file_path, 'r') as f:
            code = f.read()
        compile(code, str(file_path), 'exec')
    except SyntaxError as e:
        result.add_error(
            str(file_path.relative_to(V26_DIR)),
            e.lineno or 0,
            'PYTHON_SYNTAX',
            f"Invalid Python syntax: {str(e)}"
        )
    except Exception as e:
        result.add_warning(
            str(file_path.relative_to(V26_DIR)),
            0,
            'PYTHON_READ_ERROR',
            f"Error reading Python file: {str(e)}"
        )


def validate_required_files(result: ValidationResult):
    """Check that required files exist"""
    required_files = [
        '.env.example',
        '.gitignore',
        'README.md',
    ]

    for file_name in required_files:
        file_path = V26_DIR / file_name
        if not file_path.exists():
            result.add_error(
                file_name,
                0,
                'MISSING_FILE',
                f"Required file missing: {file_name}"
            )


def main():
    print(f"{BLUE}Starting AlgoTrendy v2.6 Migration Validation{NC}\n")

    result = ValidationResult()

    # Validate required files
    print(f"{BLUE}Checking required files...{NC}")
    validate_required_files(result)

    # Scan all Python files
    print(f"{BLUE}Scanning Python files for security issues...{NC}")
    for py_file in V26_DIR.rglob('*.py'):
        # Skip validation script itself, venv, node_modules, and .git
        if ('validate_migration.py' in str(py_file) or 'venv' in str(py_file) or
            'node_modules' in str(py_file) or '.git' in str(py_file)):
            continue
        scan_file_for_security_issues(py_file, result)
        validate_python_syntax(py_file, result)

    # Scan all JavaScript/TypeScript files
    print(f"{BLUE}Scanning JS/TS files for security issues...{NC}")
    for js_file in list(V26_DIR.rglob('*.js')) + list(V26_DIR.rglob('*.ts')) + list(V26_DIR.rglob('*.tsx')):
        if 'node_modules' in str(js_file) or '.next' in str(js_file) or '.git' in str(js_file):
            continue
        scan_file_for_security_issues(js_file, result)

    # Validate JSON files
    print(f"{BLUE}Validating JSON files...{NC}")
    for json_file in V26_DIR.rglob('*.json'):
        if 'node_modules' in str(json_file) or '.git' in str(json_file):
            continue
        validate_json_syntax(json_file, result)

    # Scan configuration files
    print(f"{BLUE}Scanning configuration files...{NC}")
    for config_file in list(V26_DIR.rglob('*.yml')) + list(V26_DIR.rglob('*.yaml')) + list(V26_DIR.rglob('*.env*')):
        if '.git' in str(config_file):
            continue
        scan_file_for_security_issues(config_file, result)

    # Print summary
    result.print_summary()

    # Exit with appropriate code
    if result.has_errors():
        print(f"{RED}Validation FAILED - Fix errors before proceeding{NC}")
        sys.exit(1)
    else:
        print(f"{GREEN}Validation PASSED - Ready to initialize Git repository{NC}")
        sys.exit(0)


if __name__ == '__main__':
    main()
