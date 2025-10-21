# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 2.6.x   | :white_check_mark: |
| 2.5.x   | :x:                |
| < 2.5   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security vulnerability, please follow these steps:

### 1. Do Not Publish Publicly

Please do NOT create a public GitHub issue for security vulnerabilities.

### 2. Report Privately

Send an email to **security@algotrendy.com** (or create a private security advisory on GitHub) with:

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### 3. Response Timeline

- **Initial Response:** Within 48 hours
- **Status Update:** Within 7 days
- **Fix Timeline:** Depends on severity
  - Critical: Within 7 days
  - High: Within 14 days
  - Medium: Within 30 days
  - Low: Next release cycle

### 4. Disclosure Policy

- We will acknowledge your report within 48 hours
- We will provide regular updates on our progress
- We will credit you in the security advisory (unless you prefer to remain anonymous)
- We request that you do not publicly disclose the vulnerability until we have released a fix

## Security Best Practices

When using AlgoTrendy:

### API Keys & Secrets

- ✅ Store API keys in user secrets or environment variables
- ✅ Never commit credentials to version control
- ✅ Use separate keys for development and production
- ✅ Enable IP whitelisting on broker accounts
- ✅ Rotate API keys regularly

### Network Security

- ✅ Use HTTPS/TLS for all API communications
- ✅ Keep SSL certificates up to date
- ✅ Configure firewall rules properly
- ✅ Use VPN when accessing from public networks

### Authentication

- ✅ Use strong, unique passwords
- ✅ Enable 2FA on broker accounts
- ✅ Set short JWT token expiration times
- ✅ Implement rate limiting
- ✅ Log authentication attempts

### Database Security

- ✅ Use strong database passwords
- ✅ Restrict database access to localhost
- ✅ Regularly backup database
- ✅ Encrypt sensitive data at rest

### Dependency Management

- ✅ Keep dependencies up to date
- ✅ Review Dependabot alerts promptly
- ✅ Use lock files for reproducible builds
- ✅ Audit dependencies regularly

## Known Security Considerations

### Testnet vs Production

- Always test with testnet credentials first
- Never use real funds for testing
- Separate production and development environments

### Risk Management

- Set position size limits
- Implement stop-loss mechanisms
- Monitor for unusual activity
- Review trade logs regularly

## Security Updates

Security updates are announced via:

- GitHub Security Advisories
- Release notes
- Security mailing list (subscribe at security@algotrendy.com)

## Third-Party Dependencies

We regularly scan our dependencies for vulnerabilities using:

- Dependabot
- CodeQL
- Trivy
- GitHub Security Advisories

## Contact

- Security Issues: security@algotrendy.com
- General Support: support@algotrendy.com
- GitHub: https://github.com/KenyBoi/algotrendy-v2.6/security

---

*Last Updated: October 21, 2025*
