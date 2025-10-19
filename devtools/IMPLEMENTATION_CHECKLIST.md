# Implementation Checklist
## AlgoTrendy v2.6 - Cleanup & Remediation

**Date Started**: _____________
**Completed By**: _____________

---

## Pre-Implementation

### Setup & Backup
- [ ] **Create full backup**
  ```bash
  tar -czf ~/algotrendy_v2.6_backup_$(date +%Y%m%d_%H%M%S).tar.gz /root/AlgoTrendy_v2.6/
  ```
  Backup location: _____________

- [ ] **Verify backup created successfully**
  ```bash
  tar -tzf ~/algotrendy_v2.6_backup_*.tar.gz | head -20
  ```

- [ ] **Create feature branch**
  ```bash
  cd /root/AlgoTrendy_v2.6
  git checkout -b fix/cleanup-orphaned-files
  git push -u origin fix/cleanup-orphaned-files
  ```

- [ ] **Review comprehensive plan**
  - Read: `devtools/COMPREHENSIVE_CLEANUP_PLAN.md`
  - Read: `devtools/RISK_ASSESSMENT.md`
  - Read: `devtools/analysis/README.md`

- [ ] **Install pre-commit hook** (optional but recommended)
  ```bash
  cp devtools/scripts/pre-commit-hook-template.sh .git/hooks/pre-commit
  chmod +x .git/hooks/pre-commit
  ```

---

## Phase 1: Critical Build Fixes

**Priority**: 🔴 **CRITICAL** - Must complete first
**Estimated Time**: 4-6 hours

### 1.1 Add Missing NuGet Packages

- [ ] **Navigate to backend directory**
  ```bash
  cd /root/AlgoTrendy_v2.6/backend
  ```

- [ ] **Add Serilog packages**
  ```bash
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Serilog
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Serilog.Sinks.File
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Serilog.Sinks.Console
  ```

- [ ] **Add Azure SDK packages**
  ```bash
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Azure.Identity
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Azure.Security.KeyVault.Secrets
  dotnet add AlgoTrendy.API/AlgoTrendy.API.csproj package Azure.Extensions.AspNetCore.Configuration.Secrets
  ```

- [ ] **Add Microsoft.Extensions.Options**
  ```bash
  dotnet add AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package Microsoft.Extensions.Options
  ```

- [ ] **Restore packages**
  ```bash
  dotnet restore
  ```

- [ ] **Verify packages added**
  ```bash
  dotnet list AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj package
  ```

- [ ] **Test build**
  ```bash
  dotnet build --no-restore
  ```
  Errors before: 78, Errors after: _____ (should be ~23)

- [ ] **Commit package additions**
  ```bash
  git add backend/AlgoTrendy.Infrastructure/AlgoTrendy.Infrastructure.csproj
  git add backend/AlgoTrendy.API/AlgoTrendy.API.csproj
  git commit -m "feat: Add required NuGet packages for logging and Azure Key Vault"
  ```

### 1.2 Fix Missing ClientOrderId

- [ ] **Review OrderFactory usage**
  - Check: `backend/AlgoTrendy.Core/Models/OrderFactory.cs`
  - Understand: `GenerateUniqueOrderId()` method

- [ ] **Fix BinanceBroker.cs**
  - File: `backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs`
  - [ ] Line 191: Add ClientOrderId to market order
  - [ ] Line 251: Add ClientOrderId to limit order
  - [ ] Line 303: Add ClientOrderId to stop order

  Use OrderFactory or:
  ```csharp
  ClientOrderId = OrderFactory.GenerateUniqueOrderId(),
  ```

- [ ] **Fix OrderBuilder.cs**
  - File: `backend/AlgoTrendy.Tests/TestHelpers/Builders/OrderBuilder.cs`
  - [ ] Line 140: Add ClientOrderId

- [ ] **Fix MockBrokerFixture.cs**
  - File: `backend/AlgoTrendy.Tests/TestHelpers/Fixtures/MockBrokerFixture.cs`
  - [ ] Line 49: Add ClientOrderId

- [ ] **Fix test files** (13 locations)
  - [ ] `TradingEngineSimpleTests.cs` - Lines 56, 79
  - [ ] `TradingEngineTests.cs` - Lines 56, 70, 114, 152, 167, 221, 257, 307, 321, 342, 390

- [ ] **Build and verify**
  ```bash
  dotnet build
  ```
  Errors remaining: _____ (should be ~5)

- [ ] **Run tests**
  ```bash
  dotnet test --filter "Category=Unit" --no-build
  ```
  Result: _____________

- [ ] **Commit ClientOrderId fixes**
  ```bash
  git add backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs
  git add backend/AlgoTrendy.Tests/
  git commit -m "fix: Add ClientOrderId to all Order instantiations for idempotency"
  ```

### 1.3 Implement Missing Interface Members

- [ ] **Add BinanceBroker methods** (stubs with safe defaults)
  - File: `backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs`
  - [ ] Add `SetLeverageAsync` (safe default: 1x leverage only)
  - [ ] Add `GetLeverageInfoAsync` (return 1x leverage)
  - [ ] Add `GetMarginHealthRatioAsync` (return conservative 0.5)

  See: `devtools/COMPREHENSIVE_CLEANUP_PLAN.md` Phase 1.3 for code snippets

- [ ] **Add LeverageRepository method**
  - File: `backend/AlgoTrendy.Infrastructure/Repositories/LeverageRepository.cs`
  - [ ] Implement `RecordLeverageChangeAsync`

- [ ] **Build and verify**
  ```bash
  dotnet build
  ```
  Errors remaining: _____ (should be 0)

- [ ] **Commit interface implementations**
  ```bash
  git add backend/AlgoTrendy.TradingEngine/Brokers/BinanceBroker.cs
  git add backend/AlgoTrendy.Infrastructure/Repositories/LeverageRepository.cs
  git commit -m "feat: Implement missing leverage/margin interface members with safe defaults"
  ```

### 1.4 Fix Async Methods Without Await

- [ ] **Update IndicatorService.cs**
  - File: `backend/AlgoTrendy.TradingEngine/Services/IndicatorService.cs`
  - [ ] Line 46: Fix async without await
  - [ ] Line 118: Fix async without await
  - [ ] Line 177: Fix async without await
  - [ ] Line 235: Fix async without await
  - [ ] Line 273: Fix async without await

  Either remove `async` or wrap in `Task.FromResult()`

- [ ] **Build and verify**
  ```bash
  dotnet build
  ```
  Result: ✅ 0 errors expected

- [ ] **Commit async fixes**
  ```bash
  git add backend/AlgoTrendy.TradingEngine/Services/IndicatorService.cs
  git commit -m "fix: Remove async keyword from methods without await"
  ```

### 1.5 Validate Phase 1 Complete

- [ ] **Run full build**
  ```bash
  cd /root/AlgoTrendy_v2.6/backend
  dotnet clean
  dotnet restore
  dotnet build
  ```
  ✅ Expected: 0 errors

- [ ] **Run unit tests**
  ```bash
  dotnet test --filter "Category=Unit"
  ```
  Result: _____________

- [ ] **Create checkpoint**
  ```bash
  git tag checkpoint-phase1
  git push origin checkpoint-phase1
  ```

---

## Phase 2: Version Control

**Priority**: 🔴 **CRITICAL**
**Estimated Time**: 2-3 hours

### 2.1 Commit Critical Backend Files

**Option A: Use automated script (recommended)**

- [ ] **Review what will be committed (dry run)**
  ```bash
  cd /root/AlgoTrendy_v2.6
  ./devtools/scripts/commit_essential_files.sh --dry-run
  ```

- [ ] **Run actual commits**
  ```bash
  ./devtools/scripts/commit_essential_files.sh
  ```

- [ ] **Review commits**
  ```bash
  git log --oneline -10
  ```

**Option B: Manual commits**

Follow steps in `devtools/COMPREHENSIVE_CLEANUP_PLAN.md` Phase 2.1-2.3

- [ ] **Commit backend code**
- [ ] **Commit tests**
- [ ] **Commit database migrations**
- [ ] **Commit configuration files**
- [ ] **Commit documentation**
- [ ] **Commit scripts and tools**
- [ ] **Commit legacy reference**

### 2.2 Review Remaining Untracked Files

- [ ] **Check what's left**
  ```bash
  git status --short
  ```

- [ ] **Make decisions on remaining files**
  - [ ] `debt_mgmt_module/` - Decision: _____________
  - [ ] `file_mgmt_programs/` - Decision: _____________
  - [ ] `trees/` - Decision: _____________

- [ ] **Update .gitignore if needed**
  ```bash
  # Add patterns for files to ignore permanently
  echo "trees/" >> .gitignore
  git add .gitignore
  git commit -m "chore: Update .gitignore"
  ```

### 2.3 Validate Phase 2 Complete

- [ ] **Check git status**
  ```bash
  git status
  ```
  Only intentionally untracked files should remain

- [ ] **Push to remote**
  ```bash
  git push origin fix/cleanup-orphaned-files
  ```

- [ ] **Create checkpoint**
  ```bash
  git tag checkpoint-phase2
  git push origin checkpoint-phase2
  ```

---

## Phase 3: Frontend Cleanup

**Priority**: 🟡 **MEDIUM**
**Estimated Time**: 1-2 hours

### 3.1 Verify Unused Dependencies

- [ ] **Navigate to frontend**
  ```bash
  cd /root/AlgoTrendy_v2.6/legacy_reference/v2.5_frontend
  ```

- [ ] **Verify each package**
  For each package flagged by knip:

  - [ ] **critters**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "critters"
    ```
    Result: _____________

  - [ ] **instantsearch.js**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "instantsearch.js"
    ```
    Result: _____________

  - [ ] **next-auth**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "next-auth"
    ```
    Result: _____________

  - [ ] **react-hook-form**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "react-hook-form"
    ```
    Result: _____________

  - [ ] **socket.io-client**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "socket.io-client"
    ```
    Result: _____________

  - [ ] **zod**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "zod"
    ```
    Result: _____________

  - [ ] **class-variance-authority**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "class-variance-authority"
    ```
    Result: _____________

  - [ ] **clsx**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "clsx"
    ```
    Result: _____________

  - [ ] **tailwind-merge**
    ```bash
    /root/AlgoTrendy_v2.6/devtools/scripts/verify_dependency_usage.sh "tailwind-merge"
    ```
    Result: _____________

### 3.2 Remove Truly Unused Dependencies

**⚠️  DO NOT REMOVE react-dom (likely false positive)**

For each verified unused package:

- [ ] **Remove package**
  ```bash
  npm uninstall <package-name>
  ```

- [ ] **Test build**
  ```bash
  npm run build
  ```

- [ ] **Test dev server**
  ```bash
  npm run dev
  # Verify in browser
  ```

- [ ] **If successful, commit**
  ```bash
  git add package.json package-lock.json
  git commit -m "chore: Remove unused dependency <package-name>"
  ```

### 3.3 Validate Phase 3 Complete

- [ ] **Final build test**
  ```bash
  npm run build
  ```
  ✅ Expected: Build succeeds

- [ ] **Create checkpoint**
  ```bash
  git tag checkpoint-phase3
  git push origin checkpoint-phase3
  ```

---

## Phase 4: Code Quality (Optional)

**Priority**: 🟢 **LOW**
**Estimated Time**: 3-4 hours

### 4.1 Performance Optimizations

- [ ] **Replace .Any() with .Count > 0**
  - See: `devtools/COMPREHENSIVE_CLEANUP_PLAN.md` Phase 4.1.A
  - 13 instances to fix
  - Test after each file change

- [ ] **Mark methods as static**
  - See: `devtools/COMPREHENSIVE_CLEANUP_PLAN.md` Phase 4.1.B
  - 14 instances
  - Only if private and doesn't access `this`

- [ ] **Apply other optimizations**
  - Constant arrays (8 instances)
  - Concrete types (3 instances)
  - ArgumentNullException.ThrowIfNull (2 instances)

### 4.2 Test Categorization

- [ ] **Add test categories**
  ```csharp
  [Fact]
  [Trait("Category", "Unit")]
  // or
  [Trait("Category", "Integration")]
  // or
  [Trait("Category", "E2E")]
  ```

- [ ] **Document skipped tests**
  - 27 skipped tests to review
  - Either enable or document why skipped

- [ ] **Update CI/CD** (if applicable)
  - Configure to run only appropriate test categories

---

## Phase 5: Final Validation

**Priority**: 🔴 **CRITICAL**
**Estimated Time**: 2-3 hours

### 5.1 Comprehensive Build & Test

- [ ] **Run validation suite**
  ```bash
  cd /root/AlgoTrendy_v2.6
  ./devtools/scripts/run_all_validations.sh
  ```
  Result: _____________

### 5.2 Manual Testing

- [ ] **Build backend**
  ```bash
  cd backend
  dotnet build
  ```
  ✅ Expected: 0 errors

- [ ] **Run all unit tests**
  ```bash
  dotnet test --filter "Category=Unit"
  ```
  Result: _____________

- [ ] **Start API** (if possible)
  ```bash
  cd AlgoTrendy.API
  dotnet run
  ```
  Starts successfully: [ ] Yes [ ] No

- [ ] **Test critical endpoints**
  ```bash
  curl http://localhost:5000/health
  ```
  Result: _____________

### 5.3 Code Review

- [ ] **Review all commits**
  ```bash
  git log --oneline origin/main..HEAD
  ```

- [ ] **Review diff**
  ```bash
  git diff origin/main..HEAD
  ```

- [ ] **No secrets committed**
  ```bash
  git diff origin/main..HEAD | grep -i "api_key\|secret\|password"
  ```
  ✅ Expected: No matches

### 5.4 Update Documentation

- [ ] **Update README.md**
  - Add build status
  - Update dependencies
  - Note any breaking changes

- [ ] **Create PR description**
  - Summary of changes
  - Testing performed
  - Known issues (if any)

---

## Post-Implementation

### Create Pull Request

- [ ] **Push final changes**
  ```bash
  git push origin fix/cleanup-orphaned-files
  ```

- [ ] **Create PR**
  - Title: "fix: Cleanup orphaned files and fix build errors"
  - Description: Summarize all changes
  - Link to analysis reports

- [ ] **Request review**
  - Assign reviewers
  - Add labels

### After PR Approval

- [ ] **Merge to main**
  ```bash
  git checkout main
  git merge fix/cleanup-orphaned-files
  git push origin main
  ```

- [ ] **Tag release** (if appropriate)
  ```bash
  git tag v2.6.0-alpha
  git push origin v2.6.0-alpha
  ```

- [ ] **Delete feature branch**
  ```bash
  git branch -d fix/cleanup-orphaned-files
  git push origin --delete fix/cleanup-orphaned-files
  ```

- [ ] **Archive analysis reports** (optional)
  ```bash
  tar -czf devtools/analysis_$(date +%Y%m%d).tar.gz devtools/analysis/
  ```

### Cleanup

- [ ] **Delete checkpoint tags** (if no longer needed)
  ```bash
  git tag -d checkpoint-phase1 checkpoint-phase2 checkpoint-phase3
  git push origin --delete checkpoint-phase1 checkpoint-phase2 checkpoint-phase3
  ```

- [ ] **Update project board/issues**
  - Close related issues
  - Update project status

---

## Rollback Procedures

If issues arise at any point:

### Rollback Last Commit
```bash
git revert HEAD
```

### Rollback to Checkpoint
```bash
git reset --hard checkpoint-phase1  # or phase2, phase3
```

### Rollback Entire Branch
```bash
git checkout main
git branch -D fix/cleanup-orphaned-files
```

### Full Project Restore
```bash
cd /root
tar -xzf algotrendy_v2.6_backup_*.tar.gz
```

---

## Notes & Issues Encountered

**Phase 1 Notes:**
_________________________________________________
_________________________________________________

**Phase 2 Notes:**
_________________________________________________
_________________________________________________

**Phase 3 Notes:**
_________________________________________________
_________________________________________________

**Phase 4 Notes:**
_________________________________________________
_________________________________________________

**Phase 5 Notes:**
_________________________________________________
_________________________________________________

**Overall Lessons Learned:**
_________________________________________________
_________________________________________________

---

**Implementation Started**: _____________
**Implementation Completed**: _____________
**Total Time**: _____________
**Completed By**: _____________

✅ **Checklist Complete**
