# Developer Onboarding Checklist

Welcome to AlgoTrendy! This checklist will help you get set up and productive quickly.

## ✅ Day 1: Environment Setup

### 1. Clone Repository & Install Prerequisites

- [ ] Clone repository: `git clone https://github.com/KenyBoi/algotrendy-v2.6.git`
- [ ] Install .NET 8.0 SDK: https://dotnet.microsoft.com/download
- [ ] Install Docker & Docker Compose: https://docs.docker.com/get-docker/
- [ ] Install Git: https://git-scm.com/downloads
- [ ] (Optional) Install VS Code: https://code.visualstudio.com/

### 2. Security Tools Setup (REQUIRED) 🔒

**This is mandatory for all developers:**

```bash
cd algotrendy-v2.6/file_mgmt_code
./setup-security-tools.sh
./setup-precommit-hooks.sh
```

- [ ] Security tools installed (Gitleaks + Semgrep)
- [ ] Pre-commit hooks configured
- [ ] Run first security scan: `./scan-security.sh`
- [ ] Review [SECURITY.md](SECURITY.md) policy

### 3. Environment Configuration

```bash
cd algotrendy-v2.6
cp .env.example .env
```

- [ ] Copy `.env.example` to `.env`
- [ ] **Never commit `.env`** to git (verify it's in `.gitignore`)
- [ ] Configure local database credentials
- [ ] (Optional) Add broker API keys for testing

### 4. Run Development Setup Script

```bash
./scripts/dev-setup.sh
```

This automated script will:
- [ ] Check all prerequisites
- [ ] Restore NuGet packages
- [ ] Build all projects
- [ ] Set up databases
- [ ] Run migrations
- [ ] Configure user secrets

---

## ✅ Day 1-2: Understand the Codebase

### 5. Read Core Documentation

- [ ] [README.md](README.md) - Project overview
- [ ] [ARCHITECTURE.md](docs/ARCHITECTURE.md) - System architecture
- [ ] [CONTRIBUTING.md](CONTRIBUTING.md) - Development workflow
- [ ] [SECURITY.md](SECURITY.md) - Security policy
- [ ] [DOCKER_SETUP.md](DOCKER_SETUP.md) - Docker deployment

### 6. Explore the Code Structure

```
backend/
├── AlgoTrendy.API          # REST API & Controllers
├── AlgoTrendy.TradingEngine # Broker integrations & trading
├── AlgoTrendy.Backtesting   # QuantConnect + Custom engines
├── AlgoTrendy.DataChannels  # Market data providers
├── AlgoTrendy.Core          # Domain models & interfaces
└── AlgoTrendy.Infrastructure # Database, caching, etc.
```

- [ ] Review `AlgoTrendy.Core` - Domain models
- [ ] Explore `AlgoTrendy.API` - REST endpoints
- [ ] Understand `AlgoTrendy.TradingEngine` - Broker integrations
- [ ] Check `AlgoTrendy.Tests` - Testing structure

### 7. Run the Application

**Option A: Docker (Recommended)**
```bash
docker-compose up -d
```

**Option B: Local Development**
```bash
cd backend/AlgoTrendy.API
dotnet run
```

- [ ] Application runs successfully
- [ ] Access API at http://localhost:5002
- [ ] View Swagger docs at http://localhost:5002/swagger
- [ ] QuestDB console at http://localhost:9000

### 8. Run Tests

```bash
cd backend
dotnet test
```

- [ ] All tests pass (306/407 should pass)
- [ ] Understand test structure (Unit, Integration)
- [ ] Review test coverage: 75%

---

## ✅ Week 1: Make Your First Contribution

### 9. Development Workflow

- [ ] Create a feature branch: `git checkout -b feature/my-first-feature`
- [ ] Make a small change (fix typo, improve comments, etc.)
- [ ] Run security scan: `cd file_mgmt_code && ./scan-security.sh`
- [ ] Commit changes: `git commit -m "docs: Fix typo in README"`
- [ ] **Pre-commit hooks will run automatically** - verify they pass
- [ ] Push to your fork: `git push origin feature/my-first-feature`
- [ ] Open a Pull Request

### 10. Coding Standards

- [ ] Follow .NET coding conventions
- [ ] Use meaningful variable names
- [ ] Write XML documentation for public APIs
- [ ] Write unit tests for new features
- [ ] Use conventional commits: `feat:`, `fix:`, `docs:`, `refactor:`, etc.
- [ ] **Never commit secrets** - Pre-commit hooks will prevent this

### 11. Security Best Practices

- [ ] Always use environment variables for secrets
- [ ] Never hardcode API keys or passwords
- [ ] Use ASP.NET Core User Secrets for development:
  ```bash
  cd backend/AlgoTrendy.API
  dotnet user-secrets set "Broker:ApiKey" "your-key-here"
  ```
- [ ] Review security scan results before committing
- [ ] Report security issues via [SECURITY.md](SECURITY.md) guidelines

---

## ✅ Week 2: Deep Dive into Specific Areas

### 12. Choose Your Focus Area

**Trading Engine:**
- [ ] Review broker integrations (`AlgoTrendy.TradingEngine/Brokers/`)
- [ ] Understand order execution flow
- [ ] Study rate limiting implementation
- [ ] Read broker documentation (Binance, Bybit, etc.)

**Backtesting:**
- [ ] Explore QuantConnect integration
- [ ] Review custom backtesting engine
- [ ] Understand MEM AI integration
- [ ] Study performance metrics calculation

**API Development:**
- [ ] Review REST API endpoints
- [ ] Understand authentication/authorization
- [ ] Study rate limiting middleware
- [ ] Explore SignalR hubs for real-time data

**Data Infrastructure:**
- [ ] Study data provider integrations
- [ ] Understand QuestDB time-series storage
- [ ] Review caching strategy (Redis)
- [ ] Explore data pipeline architecture

### 13. Join Team Communications

- [ ] Join team Slack/Discord (if applicable)
- [ ] Introduce yourself to the team
- [ ] Ask questions in appropriate channels
- [ ] Review recent pull requests
- [ ] Participate in code reviews

---

## ✅ Ongoing: Best Practices

### Security Checklist (Every Commit)

- [ ] Pre-commit hooks passed
- [ ] No hardcoded credentials
- [ ] Security scan clean
- [ ] `.env` not committed

### Code Quality Checklist (Every PR)

- [ ] Code builds successfully
- [ ] All tests pass
- [ ] New features have tests
- [ ] Documentation updated
- [ ] Conventional commit messages
- [ ] Code reviewed by peers

### Weekly Tasks

- [ ] Run security scan: `cd file_mgmt_code && ./scan-security.sh`
- [ ] Update dependencies: `dotnet list package --outdated`
- [ ] Review project roadmap
- [ ] Sync with main branch

---

## 📚 Resources

### Documentation
- **Main README**: [README.md](README.md)
- **API Docs**: [docs/api/](docs/api/)
- **Integration Guides**: [docs/integration/](docs/integration/)
- **Architecture**: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

### Security
- **Security Policy**: [SECURITY.md](SECURITY.md)
- **Scan Report**: [file_mgmt_code/SECURITY_SCAN_REPORT.md](file_mgmt_code/SECURITY_SCAN_REPORT.md)
- **Quick Reference**: [file_mgmt_code/QUICK_REFERENCE.md](file_mgmt_code/QUICK_REFERENCE.md)

### Development Tools
- **Setup Script**: [scripts/dev-setup.sh](scripts/dev-setup.sh)
- **Security Tools**: [file_mgmt_code/](file_mgmt_code/)
- **Docker Guide**: [DOCKER_SETUP.md](DOCKER_SETUP.md)

### External Resources
- .NET 8.0 Docs: https://learn.microsoft.com/en-us/dotnet/
- QuantConnect API: https://www.quantconnect.com/docs/
- Docker Docs: https://docs.docker.com/
- Git Best Practices: https://www.conventionalcommits.org/

---

## 🆘 Getting Help

### Common Issues

**Pre-commit hooks failing?**
```bash
# Run security scan manually
cd file_mgmt_code
./scan-security.sh

# Review findings and fix issues
```

**Tests not passing?**
```bash
# Ensure databases are running
docker-compose up -d questdb redis

# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean && dotnet build
```

**Can't connect to database?**
```bash
# Check .env configuration
cat .env | grep QUESTDB

# Verify Docker containers
docker ps

# Check logs
docker logs algotrendy-questdb
```

### Support Channels

- **Documentation**: Check docs/ directory first
- **Security Issues**: Follow [SECURITY.md](SECURITY.md) guidelines
- **General Questions**: Open a GitHub issue
- **Pull Requests**: Get code review from maintainers

---

## ✨ You're Ready!

Once you've completed this checklist, you're ready to contribute to AlgoTrendy!

**Next Steps:**
1. Pick an issue from GitHub Issues
2. Ask for assignment
3. Create a branch and start coding
4. Run security scan before committing
5. Open a Pull Request
6. Celebrate your contribution! 🎉

---

**Last Updated**: 2025-10-21
**Version**: 1.0
