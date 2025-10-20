# Known Issues

## Integration Test Configuration (ApiEndpointsTests)

**Status:** Pre-existing issue, requires configuration update
**Impact:** 18 integration tests fail due to DI container configuration
**Affected:** `AlgoTrendy.Tests.Integration.ApiEndpointsTests`

### Issue Details

Integration tests that spin up the full WebApplicationFactory fail because some services are registered but their dependencies are not properly configured for the test environment.

**Missing Dependencies:**
1. `IMarketDataProvider` - Not registered in DI container
2. `AlphaVantageProvider` - Requires API key (string) in constructor

**Error:**
```
Unable to resolve service for type 'AlgoTrendy.Core.Interfaces.IMarketDataProvider'
while attempting to activate services
```

### Current Status

- ✅ Build: **SUCCESS** (0 errors, 40 warnings)
- ✅ Unit Tests: **401 passing** (up from 306)
- ⚠️ Integration Tests: **18 failing** (API endpoint tests)
- ⏭️ Skipped Tests: **62** (require credentials)

### Root Cause

The `Program.cs` registers services that depend on:
1. Configuration values (API keys, connection strings)
2. Runtime dependencies (IMarketDataProvider interface)

Tests create WebApplicationFactory without providing test-specific configurations.

### Recommended Fix

1. **Create Test-Specific DI Configuration:**
   ```csharp
   services.AddScoped<IMarketDataProvider, MockMarketDataProvider>();
   services.Configure<AlphaVantageSettings>(config =>
       config.ApiKey = "test-api-key"
   );
   ```

2. **Use WebApplicationFactory<TEntryPoint> with Custom Setup:**
   ```csharp
   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
       builder.ConfigureTestServices(services =>
       {
           // Remove production services
           services.RemoveAll<IMarketDataProvider>();
           // Add test doubles
           services.AddScoped<IMarketDataProvider, MockMarketDataProvider>();
       });
   }
   ```

3. **Or Mark as Skippable:**
   Convert integration tests to `[SkippableFact]` and skip when configuration missing.

### Workaround

Run only unit tests:
```bash
dotnet test --filter "FullyQualifiedName!~ApiEndpointsTests"
```

Or run with full configuration:
```bash
# Set environment variables
export ALPHA_VANTAGE_API_KEY="your-key"
export BINANCE_API_KEY="your-key"
# ... etc
dotnet test
```

### Priority

**P2 - Medium** - Does not block development or deployment. Integration tests are important but not critical for merge. Core functionality is verified by 401 passing unit tests.

---

**Last Updated:** October 20, 2025
**Tracked In:** This document (to be converted to GitHub issue)
