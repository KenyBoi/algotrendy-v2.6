# AlgoTrendy v2.6 Scripts

**Last Updated:** 2025-10-21

This directory contains all utility scripts organized by purpose following software engineering best practices.

## Scripts Structure

### `/setup/`
Setup and installation scripts:
- `quick_setup_credentials.sh` - Quick credential setup utility
- `check-dns.sh` - DNS configuration checker
- `use_local_lean.sh` - Local LEAN engine setup

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
