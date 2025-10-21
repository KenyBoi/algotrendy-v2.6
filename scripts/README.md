# AlgoTrendy v2.6 Scripts

**Last Updated:** 2025-10-21

This directory contains all utility scripts organized by purpose following software engineering best practices.

## Quick Start

### New to AlgoTrendy? Start here:

```bash
# Run automated development setup (RECOMMENDED)
./scripts/dev-setup.sh
```

This single script will:
- ✅ Check all prerequisites (.NET, Docker, Node.js, Python)
- ✅ Restore dependencies and build projects
- ✅ Setup databases (QuestDB, Seq)
- ✅ Configure environment files
- ✅ **Setup security tools (Gitleaks + Semgrep)** ⭐ NEW
- ✅ **Install pre-commit hooks** ⭐ NEW
- ✅ Run database migrations
- ✅ Setup user secrets
- ✅ Provide next steps

**Estimated time**: 5-10 minutes

## Scripts Structure

### Root Level (Quick Access)
- **`dev-setup.sh`** - 🛠️ **NEW!** Automated development environment setup (USE THIS!)
- `compare_models.py` - ML model comparison utility
- `schedule_model_retraining.sh` - Schedule ML model retraining

### `/setup/`
Setup and installation scripts:
- `quick_setup_credentials.sh` - Interactive credential setup utility
- `check-dns.sh` - DNS configuration checker
- `USE_LOCAL_LEAN.sh` - Local LEAN engine setup

### `/ml/`
Machine Learning scripts:
- `retrain_model.py` - Original model retraining script
- `retrain_model_v2.py` - Enhanced model retraining script (v2)
- `run_pattern_analysis.py` - Pattern analysis runner
- `ml_api_server.py` - ML API server
- `memgpt_metrics_dashboard.py` - MemGPT metrics dashboard
- `start_ml_system.sh` - Start ML system services
- `stop_ml_system.sh` - Stop ML system services

### `/testing/`
Testing and validation scripts:
- `test_providers.sh` - Data provider integration tests

### `/deployment/`
Deployment and operations scripts:
- (Future deployment scripts)

### `/database/`
Database management scripts:
- (Future database scripts)

### `/utilities/`
General utility scripts:
- `code_migration_analyzer.py` - Code migration analysis tool

### `../file_mgmt_code/` ⭐ NEW
Security tools and scanning scripts:
- **`setup-security-tools.sh`** - Install Gitleaks & Semgrep
- **`scan-security.sh`** - Run automated security scans
- **`setup-precommit-hooks.sh`** - Install pre-commit hooks
- `SECURITY_SCAN_REPORT.md` - Latest scan findings
- `FIXES_APPLIED.md` - Security improvements log
- `QUICK_REFERENCE.md` - Security command reference

## Usage

### Setup Scripts
```bash
# Quick credential setup
./scripts/setup/quick_setup_credentials.sh

# Check DNS configuration
./scripts/setup/check-dns.sh
```

### ML Scripts
```bash
# Start ML system
./scripts/ml/start_ml_system.sh

# Retrain model (v2)
python3 scripts/ml/retrain_model_v2.py

# Run pattern analysis
python3 scripts/ml/run_pattern_analysis.py

# Stop ML system
./scripts/ml/stop_ml_system.sh
```

### Testing Scripts
```bash
# Test data providers
./scripts/testing/test_providers.sh
```

### Security Scripts ⭐ NEW
```bash
# Set up security tools (one-time setup)
cd file_mgmt_code
./setup-security-tools.sh

# Install pre-commit hooks (required for all developers)
./setup-precommit-hooks.sh

# Run security scan manually
./scan-security.sh

# View latest security report
cat SECURITY_SCAN_REPORT.md
```

## Script Standards

All scripts should follow these standards:

### Shell Scripts (.sh)
1. Include shebang: `#!/bin/bash`
2. Set error handling: `set -e` (exit on error)
3. Add usage/help message
4. Make executable: `chmod +x script.sh`
5. Use descriptive variable names

### Python Scripts (.py)
1. Include shebang: `#!/usr/bin/env python3`
2. Use type hints
3. Add docstrings
4. Include argument parsing (argparse)
5. Follow PEP 8 style guide

## Adding New Scripts

When adding new scripts:
1. Place in appropriate subdirectory
2. Follow naming conventions (lowercase, underscores)
3. Add documentation header
4. Update this README
5. Set appropriate permissions
6. Test thoroughly before committing

## Dependencies

Scripts may require:
- Python 3.8+
- Bash 4.0+
- Various Python packages (see requirements.txt)
- Environment variables (see .env.example)

## Troubleshooting

Common issues:
- **Permission denied**: Run `chmod +x script.sh`
- **Module not found**: Install dependencies `pip install -r requirements.txt`
- **Environment variables**: Ensure .env file is properly configured

## See Also

- Root README for general project documentation
- `.env.example` for required environment variables
- `docs/deployment/` for deployment documentation
