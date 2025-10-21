# Code Analysis and Quality Tools

This document describes the code analysis and quality tools configured for AlgoTrendy v2.6.

## Overview

AlgoTrendy uses multiple code analysis tools to maintain high code quality:

1. **.NET Code Analysis** - Built-in analyzers and code formatting
2. **SonarCloud** - Comprehensive code quality and security analysis
3. **Security Scanning** - Vulnerability detection in dependencies
4. **Code Metrics** - Coverage and quality metrics
5. **ESLint** - Frontend code quality

## Automated Analysis

Code analysis runs automatically via GitHub Actions:

- **On every push** to main, development, or modular branches
- **On every pull request** to these branches
- **Weekly schedule** - Monday at 9:00 AM UTC

View results at: `https://github.com/KenyBoi/algotrendy-v2.6/actions`

## Running Analysis Locally

### .NET Code Analysis

```bash
# Format code according to .editorconfig
cd backend
dotnet format AlgoTrendy.sln

# Verify formatting (no changes)
dotnet format AlgoTrendy.sln --verify-no-changes

# Build with analyzers enabled
dotnet build AlgoTrendy.sln /p:RunAnalyzers=true
```

### Security Scanning

```bash
# Check for vulnerable packages
cd backend
dotnet list package --vulnerable --include-transitive

# Check for outdated packages
dotnet tool install --global dotnet-outdated-tool
dotnet outdated AlgoTrendy.sln
```

### Code Coverage

```bash
# Run tests with coverage
cd backend
dotnet test AlgoTrendy.sln \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Generate HTML report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:./coverage/**/coverage.cobertura.xml \
  -targetdir:./coverage-report \
  -reporttypes:Html

# Open report
open ./coverage-report/index.html
```

### Frontend Analysis

```bash
# Run ESLint
cd frontend
npm run lint

# Fix auto-fixable issues
npm run lint -- --fix
```

## SonarCloud Integration

### Setup

1. Sign up at https://sonarcloud.io
2. Create a new project: `KenyBoi/algotrendy-v2.6`
3. Generate a token
4. Add to GitHub Secrets: `SONAR_TOKEN`

### Running Locally

```bash
# Install scanner
dotnet tool install --global dotnet-sonarscanner

# Begin analysis
cd backend
dotnet sonarscanner begin \
  /k:"KenyBoi_algotrendy-v2.6" \
  /o:"kenyboi" \
  /d:sonar.token="YOUR_TOKEN" \
  /d:sonar.host.url="https://sonarcloud.io"

# Build
dotnet build AlgoTrendy.sln --configuration Release

# End analysis (upload results)
dotnet sonarscanner end /d:sonar.token="YOUR_TOKEN"
```

## Code Quality Standards

### .NET

- **Naming Conventions**: Follow Microsoft .NET naming guidelines
- **Code Style**: Enforced via `.editorconfig`
- **Warnings**: Treat as errors in Release builds
- **Coverage Target**: 85%+

### JavaScript/TypeScript

- **ESLint Rules**: Airbnb style guide
- **Formatting**: Prettier integration
- **Type Safety**: Strict TypeScript mode

## Quality Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Code Coverage | 85%+ | 75% |
| Technical Debt | < 5% | ~3% |
| Bugs | 0 | 0 |
| Security Hotspots | 0 | 0 |
| Code Smells | < 100 | ~50 |
| Duplication | < 3% | ~2% |

## Code Analysis Configuration Files

### .NET
- `.editorconfig` - Code style rules
- `Directory.Build.props` - MSBuild analyzers
- `stylecop.json` - StyleCop rules (if configured)

### Frontend
- `.eslintrc.json` - ESLint configuration
- `.prettierrc` - Prettier configuration
- `tsconfig.json` - TypeScript compiler options

## Fixing Common Issues

### Async/Await Warnings

```csharp
// Bad - missing await
public async Task<string> GetDataAsync()
{
    return database.GetData(); // Warning: async method lacks await
}

// Good - proper async/await
public async Task<string> GetDataAsync()
{
    return await database.GetDataAsync();
}
```

### Null Reference Warnings

```csharp
// Bad - possible null reference
public void Process(string? data)
{
    var length = data.Length; // Warning: possible null reference
}

// Good - null check
public void Process(string? data)
{
    if (data != null)
    {
        var length = data.Length;
    }
}
```

### Unused Variables

```csharp
// Bad - unused variable
public void Calculate()
{
    var result = 42; // Warning: unused variable
}

// Good - use or remove
public int Calculate()
{
    var result = 42;
    return result;
}
```

## Pre-commit Hooks

### Install Git Hooks

```bash
# Install husky (if using frontend)
cd frontend
npm install --save-dev husky
npx husky install

# Add pre-commit hook
npx husky add .husky/pre-commit "npm run lint"
```

### Manual Pre-commit Check

```bash
# Format and analyze before committing
dotnet format backend/AlgoTrendy.sln
dotnet build backend/AlgoTrendy.sln /p:RunAnalyzers=true

# If all passes, commit
git add .
git commit -m "Your commit message"
```

## Continuous Improvement

### Weekly Tasks
- Review SonarCloud dashboard
- Address new code smells
- Update dependencies
- Review security alerts

### Monthly Tasks
- Review code coverage trends
- Update analysis tools
- Refactor high-complexity code
- Document architectural decisions

## Resources

- [.NET Code Analysis](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [SonarCloud Documentation](https://docs.sonarcloud.io/)
- [ESLint Rules](https://eslint.org/docs/rules/)
- [.editorconfig Specification](https://editorconfig.org/)

## Troubleshooting

### SonarCloud Analysis Fails

```bash
# Clear cache
rm -rf ~/.sonar/cache

# Update scanner
dotnet tool update --global dotnet-sonarscanner

# Try again
dotnet sonarscanner begin ...
```

### Coverage Report Empty

```bash
# Ensure test project has coverage package
cd backend/AlgoTrendy.Tests
dotnet add package coverlet.collector

# Run tests again
dotnet test --collect:"XPlat Code Coverage"
```

## Contact

For questions about code analysis:
- GitHub Issues: https://github.com/KenyBoi/algotrendy-v2.6/issues
- Documentation: `/docs/developer/`
