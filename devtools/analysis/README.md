# AlgoTrendy v2.6 - Code Analysis Reports

Generated: 2025-10-19

## Overview

This directory contains automated analysis reports to identify disconnected, orphaned, and unused files/code in the AlgoTrendy v2.6 codebase.

## Analysis Tools Used

### 1. Git Untracked Files Analysis
**Tool**: `git ls-files --others --exclude-standard`
**Report**: `untracked-files.txt`
**Purpose**: Finds files in the repository that are not tracked by Git

**Results**: **194 untracked files** found

These files exist in your working directory but are not committed to version control. They may be:
- New files waiting to be committed
- Documentation/planning files not intended for version control
- Leftover files from migration or development
- Generated files that should be in .gitignore

### 2. JavaScript/TypeScript Frontend Analysis (knip)
**Tool**: `knip` - https://github.com/webpro/knip
**Report**: `knip-report.json`
**Location Analyzed**: `legacy_reference/v2.5_frontend/`
**Purpose**: Finds unused dependencies, files, exports, and types

**Results Summary**:
- **Unused Dependencies**: 7 packages in package.json
  - critters
  - instantsearch.js
  - next-auth
  - react-dom
  - react-hook-form
  - socket.io-client
  - zod

- **Unused Dev Dependencies**: 3 packages
  - class-variance-authority
  - clsx
  - tailwind-merge

- **Unused Exports**: Multiple exports found in:
  - `src/hooks/useFreqtrade.ts` - 5 exports
  - `src/hooks/useWebSocket.ts` - 4 exports
  - `src/components/ui/WebSocketStatus.tsx` - 1 export
  - `src/types/index.ts` - 4 type exports
  - `src/services/backtest.ts` - 5 type exports

**Note**: Current `/frontend/` directory does not have package.json set up yet (v2.6 is in development).

### 3. C# Backend Analysis (Roslynator)
**Tool**: `roslynator` - https://github.com/dotnet/roslynator
**Reports**: `roslynator-report.xml`, `roslynator-console.txt`
**Location Analyzed**: `backend/AlgoTrendy.sln`
**Purpose**: Finds code quality issues, unused code, and compilation errors

**Results Summary**: **153 diagnostics** found

#### Critical Issues (Build Errors)
- **CS9035**: 18 instances - Missing required `ClientOrderId` in Order objects
- **CS0535**: 4 instances - Missing interface implementations in `BinanceBroker` and `LeverageRepository`
- **CS0103**: 24 instances - Missing `Log` references in repositories
- **CS0246**: 30 instances - Missing namespace/type references (Azure SDK, Serilog, etc.)
- **CS0234**: 1 instance - Missing `Microsoft.Extensions.Options`

#### Code Quality Issues
- **CA1822**: 14 instances - Methods that can be marked as static
- **CA1860**: 13 instances - Use `.Count > 0` instead of `.Any()`
- **CA1861**: 8 instances - Avoid constant arrays as arguments
- **CA1859**: 3 instances - Use concrete types for better performance
- **CA1510**: 2 instances - Use `ArgumentNullException.ThrowIfNull` helper
- **CS1998**: 6 instances - Async methods without await
- **xUnit1004**: 27 instances - Skipped tests

## How to Use These Reports

### Safe Review Process

1. **Review the reports** - Don't delete anything immediately
2. **Categorize findings**:
   - False positives (code that IS used but tool can't detect)
   - True orphans (safe to remove)
   - Uncertain (needs investigation)
3. **Create a cleanup branch**: `git checkout -b cleanup/orphaned-files`
4. **Make incremental changes** - Small commits, test after each
5. **Run tests** - Ensure nothing breaks
6. **Get team review** - Before merging cleanup

### Common False Positives

Be careful with:
- **Reflection-loaded code** - C# types loaded via reflection
- **Dynamic imports** - JS files imported dynamically
- **Config files** - Referenced in Docker, CI/CD, deployment
- **Runtime dependencies** - Files loaded at runtime, not compile-time
- **Documentation** - README, planning docs may reference files
- **Build artifacts** - Generated during build process

### Next Steps

1. **Fix Critical Build Errors First** (C# backend):
   - Add missing `ClientOrderId` to Order objects
   - Implement missing interface members
   - Add missing NuGet packages (Azure SDK, Serilog)
   - Fix namespace references

2. **Review Untracked Files**:
   - Decide which should be committed
   - Add unwanted ones to .gitignore
   - Delete true orphans

3. **Clean Up Dependencies** (Frontend):
   - Verify unused packages are truly unused
   - Remove after testing
   - Update package.json

4. **Address Code Quality Issues**:
   - Mark appropriate methods as static
   - Enable skipped tests or document why they're skipped
   - Apply performance improvements (CA warnings)

## Re-running Analysis

```bash
# Update untracked files list
git ls-files --others --exclude-standard > devtools/analysis/untracked-files.txt

# Re-run knip on frontend (when package.json exists)
cd /root/AlgoTrendy_v2.6/frontend
npx knip --reporter json > /root/AlgoTrendy_v2.6/devtools/analysis/knip-report.json

# Re-run Roslynator on backend
cd /root/AlgoTrendy_v2.6/backend
roslynator analyze AlgoTrendy.sln --output /root/AlgoTrendy_v2.6/devtools/analysis/roslynator-report.xml --severity-level info
```

## Files in This Directory

- `README.md` - This file (overview and instructions)
- `untracked-files.txt` - List of 194 files not tracked by Git
- `knip-report.json` - JSON report of unused frontend code/dependencies
- `roslynator-report.xml` - XML report of C# code analysis
- `roslynator-console.txt` - Human-readable Roslynator output

## Important Notes

- **These tools are read-only** - They don't modify your code
- **Always review before deleting** - Automated tools can have false positives
- **Test thoroughly** - After any cleanup, run full test suite
- **Backup first** - Commit or backup before major cleanup operations

## References

- [knip Documentation](https://knip.dev/)
- [Roslynator Documentation](https://github.com/dotnet/roslynator)
- [Git Documentation](https://git-scm.com/docs/git-ls-files)
