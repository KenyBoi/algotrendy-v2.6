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
- `../REORGANIZATION_PLAN.md` - Project reorganization plan (this cleanup effort)

## Finding Documentation

### By Topic
- **Getting Started**: See root `../README.md`
- **API Integration**: Check `/integration/data-providers/`
- **ML/AI Features**: Check `/integration/ml/`
- **Deployment**: Check `/deployment/`
- **Development**: Check `/developer/`

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
