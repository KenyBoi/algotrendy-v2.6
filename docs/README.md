# AlgoTrendy v2.6 Documentation

**Last Updated:** 2025-10-21

This directory contains all project documentation organized by category following software engineering best practices.

## Documentation Structure

### `/user/`
User-facing documentation, guides, and tutorials for end users of the AlgoTrendy platform.

### `/developer/`
Developer documentation including:
- Development workflows
- Code contribution guidelines
- Technical references
- [TODO Tree](developer/todo-tree.md) - Development task tracking

### `/integration/`
Integration guides and summaries organized by subsystem:

#### `/integration/data-providers/`
- [Summary](integration/data-providers/summary.md) - Data provider integration overview
- [Tiingo](integration/data-providers/tiingo.md) - Tiingo API integration guide
- Additional data provider integrations

#### `/integration/mem/`
Memory Management (MEM) system integration:
- [Frontend Integration](integration/mem/frontend-integration.md) - MEM frontend integration
- [QuantConnect Integration](integration/mem/quantconnect-integration.md) - MEM with QuantConnect

#### `/integration/ml/`
Machine Learning system integration:
- [Data Connection Points](integration/ml/data-connection-points.md) - ML data pipeline connections
- [Model Retraining Guide](integration/ml/model-retraining-guide.md) - ML model retraining procedures
- [Research Enhancements](integration/ml/research-enhancements.md) - ML research improvements
- [Training Web Page](integration/ml/training-web-page.md) - ML training web interface
- [Pattern Analysis Insights](integration/ml/pattern-analysis-insights-20251020.md) - Pattern analysis results
- [Pattern Analysis Summary](integration/ml/pattern-analysis-summary.md) - Pattern analysis overview

#### `/integration/backtesting/`
Backtesting system integration documentation

### `/deployment/`
Deployment, operations, and infrastructure documentation:
- [Credentials Setup Guide](deployment/credentials-setup-guide.md) - Setting up API credentials
- [Security Updates](deployment/security-updates.md) - Security configuration and updates

### `/security/` ⭐ NEW
Security documentation, scanning tools, and policies:
- **[SECURITY.md](../SECURITY.md)** - Complete security policy
- **[Security Scan Report](../file_mgmt_code/SECURITY_SCAN_REPORT.md)** - Latest scan findings
- **[Fixes Applied](../file_mgmt_code/FIXES_APPLIED.md)** - Security improvements
- **[Quick Reference](../file_mgmt_code/QUICK_REFERENCE.md)** - Security command reference
- Security tools in `../file_mgmt_code/`

### `/architecture/`
System architecture, design decisions, and technical specifications.

### `/historical/`
Historical documentation, summaries, and reports from previous development phases:
- [Cleanup Summary](historical/cleanup-summary.md)
- [Documentation Update Summary](historical/documentation-update-summary.md)
- [Phase 7 Enablement](historical/phase-7-enablement-summary.md)
- [Refactoring Complete](historical/refactoring-complete-summary.md)
- [PR Description](historical/pr-description.md)
- [Custom Engine Disabled](historical/custom-engine-disabled.md)

## Root Documentation Files

Essential documentation files remain at project root:
- `../README.md` - Main project README
- `../CONTRIBUTING.md` - Contribution guidelines
- `../DOCKER_SETUP.md` - **NEW!** Docker deployment guide (one-command setup)
- `../REORGANIZATION_PLAN.md` - Project reorganization plan (this cleanup effort)

## Quick Start Guides

### For New Users
1. **[Docker Setup Guide](../DOCKER_SETUP.md)** - 🐳 Get started in one command
2. **[API Usage Examples](API_USAGE_EXAMPLES.md)** - 💻 Integration examples in 4 languages
3. **[Development Setup Script](../scripts/dev-setup.sh)** - 🛠️ Automated development environment

### For Contributors
1. **[Contributing Guide](../CONTRIBUTING.md)** - Development workflow and standards
2. **[Developer TODO Tree](developer/todo-tree.md)** - Project roadmap and tasks
3. **[Architecture Overview](ARCHITECTURE.md)** - System design
4. **[Documentation Automation](DOCUMENTATION_AUTOMATION.md)** - 🤖 Automated quality checks (NEW!)

## Finding Documentation

### By Topic
- **Getting Started**: `../README.md` and `../DOCKER_SETUP.md`
- **Security** ⭐: `../SECURITY.md` and `../file_mgmt_code/`
- **API Integration**: `API_USAGE_EXAMPLES.md` and `/integration/data-providers/`
- **Docker/Deployment**: `../DOCKER_SETUP.md` and `/deployment/`
- **ML/AI Features**: `/integration/ml/` and `../MEM/`
- **Development**: `../CONTRIBUTING.md`, `../scripts/dev-setup.sh`, and `/developer/`
- **Documentation Automation** 🤖: `DOCUMENTATION_AUTOMATION.md` - Automated quality checks
- **Architecture**: `ARCHITECTURE.md`

### By Date
Historical documentation in `/historical/` includes dates or phase numbers for chronological reference.

## Documentation Standards

All documentation should follow these standards:
1. **Markdown format** (.md files)
2. **Descriptive filenames** using kebab-case
3. **Date stamps** for time-sensitive documents (YYYYMMDD format)
4. **Clear headers** with table of contents for long documents
5. **Links** using relative paths within docs/

## Contributing to Documentation

See `../CONTRIBUTING.md` for guidelines on updating documentation.

## Archive

Deprecated and historical files are stored in `../archive/` with timestamped subdirectories.
