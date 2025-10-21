# Security Policy

## Overview

AlgoTrendy takes security seriously. This document outlines our security practices, how to report vulnerabilities, and guidelines for secure development.

## Reporting a Vulnerability

If you discover a security vulnerability, please report it by:

1. **DO NOT** open a public GitHub issue
2. Email the security team at: [Add your security contact email]
3. Include:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

We will respond within 48 hours and provide regular updates on the progress.

## Security Measures

### 1. Secrets Management

**NEVER commit secrets to version control.** This includes:

- API keys and secrets
- Passwords
- Private keys (SSL/TLS certificates)
- Database credentials
- JWT secret keys
- Encryption keys

#### For Development

Use **ASP.NET Core User Secrets** for .NET projects:

```bash
cd backend/AlgoTrendy.API
dotnet user-secrets init
dotnet user-secrets set "Binance:ApiKey" "your-key-here"
```

Use **.env files** (git-ignored) for local development:

```bash
cp .env.example .env
# Edit .env with your local credentials
# NEVER commit .env to git
```

#### For Production

Use a secure secret management solution:

- **Azure Key Vault** (if using Azure)
- **AWS Secrets Manager** (if using AWS)
- **HashiCorp Vault** (self-hosted)
- **Environment variables** (via secure deployment pipelines)

### 2. Docker Security

All Docker containers run as non-root users for security:

```dockerfile
# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser
```

Docker Compose services specify user:

```yaml
services:
  api:
    user: "1000:1000"  # Run as non-root
```

### 3. WebSocket Security

Always use secure WebSocket (wss://) in production:

- **Development (localhost):** `ws://localhost:5000` (acceptable)
- **Production:** `wss://yourdomain.com` (required)

The frontend automatically upgrades to wss:// for non-localhost connections.

### 4. API Key Encryption

API keys stored in the database are encrypted using AES-256:

- Set `ENCRYPTION_KEY` in environment variables
- Keys are encrypted before storage
- Decrypted only when needed for API calls

### 5. Authentication & Authorization

- **JWT tokens** with secure secret keys
- **Token expiration**: 24 hours (configurable)
- **Refresh tokens**: 7 days (configurable)
- **HTTPS only** in production
- **CORS** properly configured

## Pre-commit Security Checks

We use automated security scanning to prevent secrets from being committed:

### Setup Pre-commit Hooks

```bash
cd /root/AlgoTrendy_v2.6/file_mgmt_code
./setup-precommit-hooks.sh
```

This installs:

- **Gitleaks** - Detects secrets and credentials
- **Semgrep** - Static application security testing (SAST)
- Additional code quality checks

### Manual Security Scan

Run security scans manually:

```bash
cd /root/AlgoTrendy_v2.6/file_mgmt_code
./scan-security.sh
```

## Security Checklist for Developers

Before committing code, ensure:

- [ ] No hardcoded credentials or API keys
- [ ] No `.env` files committed (check `.gitignore`)
- [ ] API keys use environment variables
- [ ] WebSocket connections use wss:// (not ws://)
- [ ] Docker containers run as non-root
- [ ] Pre-commit hooks are installed and passing
- [ ] Security scan passes (no critical findings)

## Dependencies

### Regular Updates

Keep dependencies up to date to patch security vulnerabilities:

```bash
# .NET dependencies
dotnet list package --outdated
dotnet add package [PackageName]

# Python dependencies
pip list --outdated
pip install --upgrade [package-name]

# Node.js dependencies
npm audit
npm update
```

### Dependency Scanning

We use:

- **Dependabot** (GitHub) - Automated dependency updates
- **dotnet list package --vulnerable** - .NET vulnerability scanning
- **npm audit** - Node.js vulnerability scanning
- **pip-audit** - Python vulnerability scanning

## File Permissions

### Sensitive Files

Ensure proper file permissions:

```bash
chmod 600 .env           # Only owner can read/write
chmod 600 *.pem          # SSL certificates
chmod 600 *.key          # Private keys
```

### Git-Ignored Patterns

The following are automatically ignored by git:

- `.env` and `.env.*` files
- `*.pem`, `*.key`, `*.p12`, `*.pfx` (certificates)
- `secrets/` directory
- `certbot/conf/` directory
- `credentials.json`

## Network Security

### Firewall Rules

Only expose necessary ports:

- **443 (HTTPS)** - Public access
- **5000-5004** - Internal services (not public)
- **9000, 8812** - Database (localhost only)

### SSL/TLS

Use Let's Encrypt for SSL certificates:

```bash
# Generate certificates
certbot certonly --standalone -d yourdomain.com

# Auto-renewal
certbot renew --dry-run
```

**IMPORTANT:** SSL private keys should NEVER be committed to git.

## Database Security

### QuestDB

- Change default password from `quest`
- Use strong passwords (min 16 characters)
- Bind to localhost in production
- Use SSL for PostgreSQL wire protocol

### Redis

- Require password authentication
- Disable dangerous commands in production
- Use SSL/TLS for connections

## Monitoring & Logging

### Security Events to Monitor

- Failed authentication attempts
- API key usage anomalies
- Unusual trading patterns
- Database connection failures
- SSL certificate expiration

### Log Sanitization

Logs are automatically sanitized to remove:

- API keys (masked as `***1234`)
- Passwords (never logged)
- JWT tokens (never logged)
- Sensitive request data

## Incident Response

In case of a security incident:

1. **Immediately**: Revoke compromised credentials
2. **Within 1 hour**: Notify the team
3. **Within 24 hours**: Root cause analysis
4. **Within 48 hours**: Implement fixes
5. **Within 1 week**: Post-mortem document

## Security Training

All developers should:

- Review OWASP Top 10: https://owasp.org/www-project-top-ten/
- Complete secure coding training annually
- Stay updated on security best practices

## Compliance

AlgoTrendy follows:

- **OWASP Top 10** security guidelines
- **CWE Top 25** vulnerability mitigation
- **NIST Cybersecurity Framework** (where applicable)
- **SOC 2 Type II** (planned)

## Security Tools

We use the following security tools:

| Tool | Purpose | Frequency |
|------|---------|-----------|
| Gitleaks | Secret detection | Pre-commit + Weekly |
| Semgrep | SAST | Pre-commit + Weekly |
| Dependabot | Dependency updates | Daily |
| npm audit | Node.js vulnerabilities | Weekly |
| dotnet list package --vulnerable | .NET vulnerabilities | Weekly |

## Contact

For security questions or concerns:

- **Security Email**: [Add your security contact]
- **General Contact**: [Add your general contact]

## Acknowledgments

We thank the security research community for responsible disclosure of vulnerabilities.

---

**Last Updated**: 2025-10-21
**Version**: 1.0
**Next Review**: 2026-01-21
