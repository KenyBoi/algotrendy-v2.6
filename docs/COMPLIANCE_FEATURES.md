# AlgoTrendy Compliance & Regulatory Features

## Overview

AlgoTrendy now includes comprehensive compliance and regulatory features to meet SEC, FINRA, and AML/OFAC requirements for financial trading platforms.

## Table of Contents

1. [Features Implemented](#features-implemented)
2. [Database Schema](#database-schema)
3. [Services](#services)
4. [Configuration](#configuration)
5. [API Integration](#api-integration)
6. [Regulatory Requirements](#regulatory-requirements)
7. [Getting Started](#getting-started)

---

## Features Implemented

### ✅ 1. SEC/FINRA Regulatory Reporting

**What it does:**
- Generates **Form PF** (Private Fund reporting) for hedge funds with $150M+ AUM
- Generates **Form 13F** (Institutional Investment Manager holdings) for managers with $100M+ assets
- Generates **FINRA CAT** (Consolidated Audit Trail) for order/trade reporting
- Supports XML, JSON, CSV, and XBRL export formats

**Compliance Standard:**
- SEC Rule 17a-3: Recordkeeping requirements
- SEC Rule 17a-4: Records preservation requirements
- FINRA Rule 4530: Reporting requirements

**File:** `AlgoTrendy.Infrastructure/Services/RegulatoryReportingService.cs`

---

### ✅ 2. AML/OFAC Sanctions Screening

**What it does:**
- Screens users against OFAC SDN (Specially Designated Nationals) list
- Automatically refreshes sanctions list from Treasury.gov
- Fuzzy name matching with configurable threshold (default: 85% match)
- Blocks trades from sanctioned individuals
- Date of birth and country verification

**Compliance Standard:**
- Bank Secrecy Act (BSA)
- USA PATRIOT Act
- OFAC Sanctions Programs

**File:** `AlgoTrendy.Infrastructure/Services/OFACScreeningService.cs`

**Key Methods:**
```csharp
// Screen a user
await ofacService.ScreenUserAsync(user);

// Screen a trade
bool approved = await ofacService.ScreenTradeAsync(userId, symbol, amount);

// Refresh OFAC list
await ofacService.RefreshOFACListIfNeededAsync();
```

---

### ✅ 3. AML Transaction Monitoring

**What it does:**
- Detects high-value transactions (default: $10,000+ threshold)
- Monitors daily volume limits (default: $50,000/day)
- Identifies rapid transaction patterns (10+ trades in 5 minutes)
- Detects structuring (multiple trades just below threshold)
- Automatic account blocking for high-risk users
- Risk scoring (0-100)

**Compliance Standard:**
- FinCEN CTR (Currency Transaction Report) - $10,000 threshold
- Structuring detection
- SAR (Suspicious Activity Report) triggers

**File:** `AlgoTrendy.Infrastructure/Services/AMLMonitoringService.cs`

**Key Methods:**
```csharp
// Check trade before execution
var result = await amlService.CheckTradeAsync(userId, symbol, quantity, price);

if (!result.Approved) {
    // Block trade
}

// Periodic user review
var review = await amlService.ReviewUserAccountAsync(userId);
```

---

### ✅ 4. Trade Surveillance for Market Manipulation

**What it does:**
- **Pump & Dump Detection**: Monitors rapid price increases with volume spikes followed by large sells
- **Spoofing/Layering Detection**: Identifies orders placed without intent to execute
- **Wash Trading Detection**: Detects buying/selling same security to create artificial volume
- **Front Running Detection**: Identifies trading ahead of large orders

**Compliance Standard:**
- SEC Rule 10b-5: Market manipulation
- Dodd-Frank Act Market Abuse provisions
- FINRA Rule 5210: Publication of transactions and quotations

**File:** `AlgoTrendy.Infrastructure/Services/TradeSurveillanceService.cs`

**Detection Algorithms:**

| Pattern | Detection Logic | Severity |
|---------|----------------|----------|
| Pump & Dump | Price change > 5% + Volume spike > 3x + Large sell near peak | High |
| Spoofing | 5+ orders cancelled within 1 minute | High |
| Wash Trading | Equal buy/sell volumes at similar prices (within 1%) | Critical |
| Front Running | Large opposite trade within 5 seconds | High |

**Key Methods:**
```csharp
// Real-time surveillance
await surveillanceService.MonitorTradeAsync(trade, userId);

// Get active alerts
var alerts = await surveillanceService.GetActiveAlertsAsync();
```

---

### ✅ 5. 7-Year Data Retention Policy

**What it does:**
- Implements SEC Rule 17a-3/17a-4 retention requirements
- Archives old data to compressed JSON files
- Automatic purging after retention period
- SHA-256 hash verification for archived files
- Configurable retention periods per data type

**Retention Periods:**
- Orders/Trades: **7 years** (SEC requirement)
- Audit Logs: **6 years**
- Market Data: **1 year**
- User KYC Data: **7 years**
- Compliance Events: **7 years**

**Compliance Standard:**
- SEC Rule 17a-3: 3-6 year retention
- SEC Rule 17a-4: 6 year retention for broker-dealers
- Best practice: 7 years for legal protection

**File:** `AlgoTrendy.Infrastructure/Services/DataRetentionService.cs`

**Key Methods:**
```csharp
// Execute retention policy
await retentionService.ExecuteRetentionPolicyAsync();

// Get statistics
var stats = await retentionService.GetRetentionStatisticsAsync();
```

---

## Database Schema

### New Tables Created

#### 1. **users** - User KYC/AML Information
```sql
CREATE TABLE users (
    user_id UUID PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    date_of_birth DATE,
    address VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(50),
    postal_code VARCHAR(20),
    country VARCHAR(2),
    phone_number VARCHAR(20),
    tax_id VARCHAR(50),  -- Encrypted
    kyc_status VARCHAR(20) DEFAULT 'Pending',
    kyc_completed_at TIMESTAMP WITH TIME ZONE,
    aml_status VARCHAR(20) DEFAULT 'Clean',
    last_aml_check TIMESTAMP WITH TIME ZONE,
    sanctions_screened BOOLEAN DEFAULT false,
    last_sanctions_check TIMESTAMP WITH TIME ZONE,
    is_sanctioned BOOLEAN DEFAULT false,
    risk_level VARCHAR(20) DEFAULT 'Low',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    metadata JSONB
);
```

#### 2. **compliance_events** - Audit Trail
```sql
CREATE TABLE compliance_events (
    event_id UUID PRIMARY KEY,
    event_type VARCHAR(50) NOT NULL,
    severity VARCHAR(20) DEFAULT 'Info',
    user_id UUID,
    order_id UUID,
    trade_id UUID,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(2000),
    event_data JSONB,
    source VARCHAR(100),
    reviewed_by UUID,
    review_status VARCHAR(20) DEFAULT 'Pending',
    review_notes VARCHAR(1000),
    reviewed_at TIMESTAMP WITH TIME ZONE,
    requires_action BOOLEAN DEFAULT false,
    action_taken VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    correlation_id VARCHAR(100)
);
```

#### 3. **regulatory_reports** - SEC/FINRA Reports
```sql
CREATE TABLE regulatory_reports (
    report_id UUID PRIMARY KEY,
    report_type VARCHAR(50) NOT NULL,  -- FormPF, Form13F, CAT
    period_start TIMESTAMP WITH TIME ZONE NOT NULL,
    period_end TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(20) DEFAULT 'Draft',
    file_path VARCHAR(500),
    format VARCHAR(20) DEFAULT 'XML',
    content TEXT,
    file_hash VARCHAR(64),
    generated_by UUID,
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    approved_by UUID,
    approved_at TIMESTAMP WITH TIME ZONE,
    submitted_at TIMESTAMP WITH TIME ZONE,
    confirmation_number VARCHAR(100),
    submission_response TEXT,
    error_message TEXT,
    metadata JSONB
);
```

#### 4. **ofac_sanctions_list** - OFAC SDN Cache
```sql
CREATE TABLE ofac_sanctions_list (
    entry_id BIGSERIAL PRIMARY KEY,
    entity_number VARCHAR(50),
    sdn_type VARCHAR(20),
    program VARCHAR(100),
    full_name VARCHAR(350) NOT NULL,
    aliases TEXT,
    address TEXT,
    citizenship VARCHAR(100),
    nationality VARCHAR(100),
    date_of_birth DATE,
    place_of_birth VARCHAR(200),
    id_number VARCHAR(100),
    remarks TEXT,
    list_source VARCHAR(50),
    last_updated TIMESTAMP WITH TIME ZONE DEFAULT now(),
    metadata JSONB
);
```

#### 5. **surveillance_alerts** - Trade Surveillance
```sql
CREATE TABLE surveillance_alerts (
    alert_id UUID PRIMARY KEY,
    alert_type VARCHAR(50) NOT NULL,  -- PumpAndDump, Spoofing, WashTrading, FrontRunning
    severity VARCHAR(20) DEFAULT 'Medium',
    symbol VARCHAR(20),
    exchange VARCHAR(50),
    user_id UUID,
    order_ids JSONB,
    trade_ids JSONB,
    detection_time TIMESTAMP WITH TIME ZONE DEFAULT now(),
    pattern_data JSONB,
    description VARCHAR(1000),
    confidence_score NUMERIC(5,2),
    status VARCHAR(20) DEFAULT 'Active',
    investigated_by UUID,
    investigation_notes TEXT,
    resolved_at TIMESTAMP WITH TIME ZONE,
    action_taken VARCHAR(500)
);
```

#### 6. **data_retention_log** - Retention Audit Trail
```sql
CREATE TABLE data_retention_log (
    log_id UUID PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(20) NOT NULL,  -- Archive, Delete, Archive+Delete
    records_affected INTEGER,
    retention_period_years INTEGER,
    archive_path VARCHAR(500),
    file_hash VARCHAR(64),
    performed_by UUID,
    performed_at TIMESTAMP WITH TIME ZONE DEFAULT now(),
    status VARCHAR(20) NOT NULL,
    error_message TEXT,
    metadata JSONB
);
```

### Migration File
**Location:** `/database/migrations/compliance-tables.xml`

**Apply Migration:**
```bash
cd /root/AlgoTrendy_v2.6/database
liquibase update
```

---

## Services

### 1. OFACScreeningService

**Namespace:** `AlgoTrendy.Infrastructure.Services`

**Dependencies:**
- PostgreSQL (user and OFAC cache storage)
- HttpClient (OFAC SDN list download)
- ComplianceSettings

**Usage:**
```csharp
// Inject in controller/service
private readonly OFACScreeningService _ofacService;

// Screen user
var result = await _ofacService.ScreenUserAsync(user);
if (result.IsMatch) {
    // Handle sanctions match
    logger.LogWarning("OFAC match: {FullName} - Score: {Score}",
        user.FullName, result.MatchScore);
}

// Screen trade
bool allowed = await _ofacService.ScreenTradeAsync(userId, symbol, amount);
```

---

### 2. AMLMonitoringService

**Namespace:** `AlgoTrendy.Infrastructure.Services`

**Dependencies:**
- PostgreSQL
- ComplianceSettings

**Usage:**
```csharp
// Pre-trade check
var amlCheck = await _amlService.CheckTradeAsync(userId, symbol, quantity, price);

if (!amlCheck.Approved) {
    return BadRequest(new {
        message = "Trade blocked by AML checks",
        riskScore = amlCheck.RiskScore,
        flags = amlCheck.Flags
    });
}

// Periodic review (run as background job)
var review = await _amlService.ReviewUserAccountAsync(userId);
```

---

### 3. TradeSurveillanceService

**Namespace:** `AlgoTrendy.Infrastructure.Services`

**Dependencies:**
- PostgreSQL
- ComplianceSettings

**Usage:**
```csharp
// After trade execution
await _surveillanceService.MonitorTradeAsync(trade, userId);

// Get active alerts for compliance review
var alerts = await _surveillanceService.GetActiveAlertsAsync();
foreach (var alert in alerts) {
    logger.LogWarning("Surveillance alert: {Type} - {Description}",
        alert.AlertType, alert.Description);
}
```

---

### 4. DataRetentionService

**Namespace:** `AlgoTrendy.Infrastructure.Services`

**Dependencies:**
- PostgreSQL
- File System (archive storage)
- ComplianceSettings

**Usage:**
```csharp
// Run as scheduled job (e.g., weekly cron)
await _retentionService.ExecuteRetentionPolicyAsync();

// Get statistics
var stats = await _retentionService.GetRetentionStatisticsAsync();
logger.LogInformation("Total records eligible for archive: {Count}",
    stats.TotalRecordsEligible);
```

---

### 5. RegulatoryReportingService

**Namespace:** `AlgoTrendy.Infrastructure.Services`

**Dependencies:**
- PostgreSQL
- File System (report export)
- ComplianceSettings

**Usage:**
```csharp
// Generate Form PF (quarterly)
var periodStart = new DateTime(2025, 1, 1);
var periodEnd = new DateTime(2025, 3, 31);
var formPF = await _reportingService.GenerateFormPFAsync(periodStart, periodEnd);

// Generate Form 13F (quarterly)
var asOfDate = new DateTime(2025, 3, 31);
var form13F = await _reportingService.GenerateForm13FAsync(periodStart, asOfDate);

// Generate CAT report (daily)
var today = DateTime.UtcNow.Date;
var catReport = await _reportingService.GenerateCATReportAsync(today, today.AddDays(1));
```

---

## Configuration

### appsettings.Compliance.json

**Location:** `/backend/AlgoTrendy.API/appsettings.Compliance.json`

**Example Configuration:**
```json
{
  "Compliance": {
    "EnableComplianceFeatures": true,

    "AML": {
      "Enabled": true,
      "HighValueThreshold": 10000,
      "DailyVolumeThreshold": 50000,
      "RapidTransactionThreshold": 10,
      "RapidTransactionWindowMinutes": 5,
      "AutoBlockSuspiciousAccounts": false,
      "RequireManualReview": true
    },

    "OFAC": {
      "Enabled": true,
      "SDNListUrl": "https://www.treasury.gov/ofac/downloads/sdn.csv",
      "RefreshIntervalHours": 24,
      "MinimumMatchScore": 85,
      "BlockSanctionedOrders": true,
      "ScreenAllTrades": true
    },

    "TradeSurveillance": {
      "Enabled": true,
      "DetectPumpAndDump": true,
      "DetectSpoofing": true,
      "DetectWashTrading": true,
      "DetectFrontRunning": true,
      "PriceDeviationThreshold": 5.0,
      "VolumeSpikeMultiplier": 3.0
    },

    "RegulatoryReporting": {
      "Enabled": true,
      "GenerateFormPF": true,
      "GenerateForm13F": true,
      "GenerateCATReports": true,
      "ReportExportDirectory": "/var/compliance/reports",
      "ReportFormat": "XML"
    },

    "DataRetention": {
      "Enabled": true,
      "OrderTradeRetentionYears": 7,
      "AuditLogRetentionYears": 6,
      "EnableAutoArchive": true,
      "ArchiveStoragePath": "/var/compliance/archive"
    }
  }
}
```

### Environment Variables

**Required for Production:**
```bash
# PostgreSQL connection
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=algotrendy;Username=postgres;Password=***"

# OFAC SDN List (optional override)
export Compliance__OFAC__SDNListUrl="https://www.treasury.gov/ofac/downloads/sdn.csv"

# SEC/FINRA credentials
export Compliance__RegulatoryReporting__EDGARFilerCIK="0001234567"
export Compliance__RegulatoryReporting__FINRAFirmID="12345"
```

---

## API Integration

### Example: Integrate AML/OFAC in TradingController

**File:** `/backend/AlgoTrendy.API/Controllers/TradingController.cs`

```csharp
[HttpPost("orders")]
public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
{
    try
    {
        // 1. OFAC Screening
        var ofacApproved = await _ofacService.ScreenTradeAsync(
            request.UserId, request.Symbol, request.Quantity * request.Price);

        if (!ofacApproved)
        {
            return Forbidden(new { message = "User is on sanctions list" });
        }

        // 2. AML Check
        var amlCheck = await _amlService.CheckTradeAsync(
            request.UserId, request.Symbol, request.Quantity, request.Price);

        if (!amlCheck.Approved)
        {
            return BadRequest(new
            {
                message = "Trade blocked by AML checks",
                riskScore = amlCheck.RiskScore,
                flags = amlCheck.Flags,
                requiresManualReview = amlCheck.RequiresManualReview
            });
        }

        // 3. Place order
        var order = await _tradingEngine.PlaceOrderAsync(request);

        // 4. Trade surveillance (after execution)
        if (order.Status == OrderStatus.Filled)
        {
            var trades = await _tradeRepository.GetByOrderIdAsync(order.OrderId);
            foreach (var trade in trades)
            {
                await _surveillanceService.MonitorTradeAsync(trade, request.UserId);
            }
        }

        return Ok(order);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error placing order");
        return StatusCode(500, new { message = "Internal server error" });
    }
}
```

---

## Regulatory Requirements

### SEC Requirements Met

| Requirement | Rule | Implementation |
|-------------|------|----------------|
| Order recordkeeping | 17a-3 | `orders` table with full lifecycle tracking |
| Trade recordkeeping | 17a-3 | `trades` table with execution details |
| 6-year retention | 17a-4 | Data retention service (7 years) |
| Form PF reporting | Form PF | Regulatory reporting service |
| Form 13F reporting | Form 13F | Regulatory reporting service |
| Market manipulation detection | Rule 10b-5 | Trade surveillance service |

### FINRA Requirements Met

| Requirement | Rule | Implementation |
|-------------|------|----------------|
| CAT reporting | CAT NMS Plan | Regulatory reporting service |
| Order audit trail | OATS | `compliance_events` table |
| Trade surveillance | 3310 | Trade surveillance service |
| AML program | 3310 | AML monitoring service |

### AML/BSA Requirements Met

| Requirement | Standard | Implementation |
|-------------|---------|----------------|
| Customer identification | CIP | `users` table with KYC fields |
| OFAC screening | OFAC regulations | OFAC screening service |
| SAR triggers | FinCEN | AML monitoring (high-value, rapid, structuring) |
| Transaction monitoring | BSA | AML monitoring service |

---

## Getting Started

### 1. Apply Database Migrations

```bash
cd /root/AlgoTrendy_v2.6/database
liquibase update --changelog-file=migrations/compliance-tables.xml
```

### 2. Configure Settings

Edit `appsettings.json`:
```json
{
  "Compliance": {
    "EnableComplianceFeatures": true,
    // ... (see Configuration section)
  }
}
```

### 3. Register Services in DI Container

**File:** `/backend/AlgoTrendy.API/Program.cs`

```csharp
// Add compliance settings
builder.Services.Configure<ComplianceSettings>(
    builder.Configuration.GetSection("Compliance"));

// Register services
builder.Services.AddScoped<OFACScreeningService>();
builder.Services.AddScoped<AMLMonitoringService>();
builder.Services.AddScoped<TradeSurveillanceService>();
builder.Services.AddScoped<DataRetentionService>();
builder.Services.AddScoped<RegulatoryReportingService>();
```

### 4. Create Scheduled Jobs

**Recommended Schedule:**
- **OFAC Refresh**: Daily at 3 AM
- **AML Review**: Daily at 4 AM
- **Data Retention**: Weekly on Sunday at 2 AM
- **Form PF/13F**: Quarterly (manual trigger)
- **CAT Reports**: Daily at 11 PM

**Example using Hangfire:**
```csharp
RecurringJob.AddOrUpdate<OFACScreeningService>(
    "ofac-refresh",
    service => service.RefreshOFACListIfNeededAsync(CancellationToken.None),
    "0 3 * * *"); // Daily at 3 AM

RecurringJob.AddOrUpdate<DataRetentionService>(
    "data-retention",
    service => service.ExecuteRetentionPolicyAsync(CancellationToken.None),
    "0 2 * * 0"); // Sunday at 2 AM
```

---

## Testing

### Unit Tests

```bash
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.Tests
dotnet test
```

### Integration Tests

```csharp
[Fact]
public async Task OFACScreening_ShouldBlockSanctionedUser()
{
    // Arrange
    var user = new User { FullName = "PUTIN, Vladimir Vladimirovich" };

    // Act
    var result = await _ofacService.ScreenUserAsync(user);

    // Assert
    Assert.True(result.IsMatch);
    Assert.True(result.MatchScore >= 85);
}

[Fact]
public async Task AML_ShouldFlagHighValueTransaction()
{
    // Arrange
    var userId = Guid.NewGuid();
    var amount = 50000m; // Above $10k threshold

    // Act
    var result = await _amlService.CheckTradeAsync(userId, "BTCUSDT", 1, amount);

    // Assert
    Assert.Contains("High-value transaction", result.Flags);
    Assert.True(result.RiskScore >= 30);
}
```

---

## Monitoring & Alerts

### Key Metrics to Monitor

1. **AML Alerts**: Track `compliance_events` where `event_type` LIKE 'AML%'
2. **OFAC Matches**: Track `compliance_events` where `event_type` = 'OFACSanctionsMatch'
3. **Surveillance Alerts**: Track `surveillance_alerts` where `status` = 'Active'
4. **Report Generation**: Track `regulatory_reports` where `status` != 'Submitted'

### Grafana Dashboard Query Examples

```sql
-- AML High-Risk Events (last 24 hours)
SELECT COUNT(*)
FROM compliance_events
WHERE event_type LIKE 'AML%'
  AND severity IN ('High', 'Critical')
  AND created_at >= NOW() - INTERVAL '24 hours';

-- Active Surveillance Alerts
SELECT alert_type, COUNT(*), AVG(confidence_score)
FROM surveillance_alerts
WHERE status = 'Active'
GROUP BY alert_type;
```

---

## Security Considerations

### Data Encryption

**Sensitive Fields:**
- `users.tax_id`: Should be encrypted at rest
- `users.phone_number`: Consider encryption
- `ofac_sanctions_list.id_number`: Encrypted in OFAC cache

**Recommendation:** Use PostgreSQL `pgcrypto` or application-level encryption

```sql
-- Example: Encrypt tax_id
UPDATE users SET tax_id = pgp_sym_encrypt(tax_id, 'encryption-key');
```

### Access Control

**RBAC Recommendations:**
- **Compliance Officer**: Read access to all compliance tables
- **Auditor**: Read-only access to `compliance_events`, `regulatory_reports`
- **Admin**: Full access
- **Regular Users**: No access to compliance tables

---

## Troubleshooting

### Common Issues

**1. OFAC List Download Fails**
```
Error: Failed to refresh OFAC SDN list
```
**Solution:** Check internet connectivity, verify URL in config

**2. Data Retention Cleanup Fails**
```
Error: Permission denied writing to /var/compliance/archive
```
**Solution:** Create directory and set permissions
```bash
sudo mkdir -p /var/compliance/archive
sudo chown algotrendy:algotrendy /var/compliance/archive
sudo chmod 755 /var/compliance/archive
```

**3. Regulatory Report Generation Fails**
```
Error: Database connection string not configured
```
**Solution:** Add PostgreSQL connection string to `appsettings.json`

---

## Compliance Checklist

Before going to production:

- [ ] Database migrations applied
- [ ] Compliance settings configured
- [ ] OFAC SDN list downloaded and cached
- [ ] Scheduled jobs configured (OFAC refresh, AML review, data retention)
- [ ] Report export directory created with proper permissions
- [ ] Archive storage directory created
- [ ] SEC EDGAR CIK configured (if filing electronically)
- [ ] FINRA Firm ID configured
- [ ] Encryption enabled for sensitive fields
- [ ] Monitoring and alerts configured
- [ ] Compliance officer access granted
- [ ] Tested with sample data
- [ ] Legal review completed

---

## Support

For questions or issues:
- **Documentation**: `/docs/COMPLIANCE_FEATURES.md`
- **Source Code**: `/backend/AlgoTrendy.Infrastructure/Services/`
- **Configuration**: `/backend/AlgoTrendy.API/appsettings.Compliance.json`

---

**Disclaimer:** This implementation provides technical infrastructure for compliance. Consult with legal and compliance professionals to ensure full regulatory compliance for your specific jurisdiction and use case.
