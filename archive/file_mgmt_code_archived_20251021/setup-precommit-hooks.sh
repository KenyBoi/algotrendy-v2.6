#!/bin/bash
# Setup pre-commit hooks for AlgoTrendy
# This prevents committing secrets and security issues

set -e

echo "=================================="
echo "Pre-commit Hooks Setup"
echo "=================================="
echo ""

# Check if we're in a git repository
if [ ! -d .git ]; then
    echo "Error: Not in a git repository root"
    echo "Please run this script from the repository root directory"
    exit 1
fi

# Install pre-commit if not already installed
if ! command -v pre-commit &> /dev/null; then
    echo "Installing pre-commit..."
    pip3 install --break-system-packages pre-commit
fi

# Create .pre-commit-config.yaml
echo "Creating .pre-commit-config.yaml..."

cat > .pre-commit-config.yaml << 'EOF'
# AlgoTrendy Pre-commit Hooks Configuration
# See https://pre-commit.com for more information

repos:
  # Gitleaks - Secret Detection
  - repo: https://github.com/gitleaks/gitleaks
    rev: v8.18.2
    hooks:
      - id: gitleaks
        name: Detect hardcoded secrets
        description: Scan for secrets and credentials
        entry: gitleaks protect --verbose --redact --staged
        language: system

  # Semgrep - Security Analysis
  - repo: https://github.com/returntocorp/semgrep
    rev: v1.140.0
    hooks:
      - id: semgrep
        name: Semgrep security check
        args: ['--config', 'auto', '--error']
        language: system

  # Additional useful hooks
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.5.0
    hooks:
      - id: trailing-whitespace
        name: Trim trailing whitespace
      - id: end-of-file-fixer
        name: Fix end of files
      - id: check-yaml
        name: Check YAML syntax
        exclude: 'templates/.*\.yaml$'
      - id: check-json
        name: Check JSON syntax
      - id: check-added-large-files
        name: Check for large files
        args: ['--maxkb=5000']
      - id: detect-private-key
        name: Detect private keys
      - id: check-merge-conflict
        name: Check for merge conflicts
      - id: check-case-conflict
        name: Check for case conflicts

  # .NET specific checks
  - repo: local
    hooks:
      - id: dotnet-format
        name: dotnet format
        entry: dotnet format --verify-no-changes
        language: system
        files: '\.(cs|csproj|sln)$'
        pass_filenames: false

  # Python specific checks
  - repo: https://github.com/psf/black
    rev: 24.1.1
    hooks:
      - id: black
        name: Black Python formatter
        language_version: python3
        files: '\.py$'
EOF

echo "✓ .pre-commit-config.yaml created"
echo ""

# Install the hooks
echo "Installing pre-commit hooks..."
pre-commit install

# Also install for commit-msg
pre-commit install --hook-type commit-msg

echo ""
echo "✓ Pre-commit hooks installed successfully!"
echo ""
echo "The following checks will run before each commit:"
echo "  ✓ Gitleaks - Secret detection"
echo "  ✓ Semgrep - Security analysis"
echo "  ✓ YAML/JSON syntax checks"
echo "  ✓ Large file detection"
echo "  ✓ Private key detection"
echo "  ✓ .NET code formatting"
echo "  ✓ Python code formatting"
echo ""
echo "To skip hooks temporarily (not recommended):"
echo "  git commit --no-verify"
echo ""
echo "To run hooks manually on all files:"
echo "  pre-commit run --all-files"
echo ""
