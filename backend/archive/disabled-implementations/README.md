# Disabled Implementations Archive

This directory contains alternative/earlier implementations that were disabled during development.

## Purpose

These files are preserved for reference and historical context but are not actively used in the project. They represent different design approaches or work-in-progress refactoring attempts.

## Contents

### Backtesting Models (6 files)

Alternative implementation of backtesting models with different design philosophy:

- **BacktestConfig.cs.disabled**
  - Uses `string` dates instead of `DateTime`
  - DataAnnotations validation (`[Required]`, `IValidatableObject`)
  - Simple `Dictionary<string, bool>` for indicators
  - Separate `IndicatorParams` dictionary

- **BacktestMetrics.cs.disabled**
  - Simpler property structure without aliases
  - Uses `double` for `AvgTradeDuration`
  - Missing some properties like `FinalValue`, `TotalPnL`

- **BacktestResults.cs.disabled**
  - Earlier version of results model

- **Enums.cs.disabled**
  - Has `TradeSide` enum (vs `TradeDirection` in active)
  - `BacktesterEngine` has `BacktesterCom` (vs `Backtester`)
  - Missing `IndicatorType` enum

- **EquityPoint.cs.disabled**
  - Includes `Drawdown` property (not in active version)
  - Non-required properties

- **TradeResult.cs.disabled**
  - Uses `TradeSide` enum instead of `TradeDirection`
  - Non-required properties

**Active versions:** `AlgoTrendy.Backtesting/Models/BacktestModels.cs` and `BacktestingEnums.cs`

### API Controllers (1 file)

- **BacktestingController.cs.disabled** (7.8KB)
  - REST API controller for Phase 7B backtesting
  - Disabled because `IBacktestService` not fully implemented yet
  - **Status:** WIP, will be re-enabled when Phase 7B completes

### Infrastructure (1 file)

- **OrderRepositoryV2.cs.disabled**
  - Refactoring attempt for order repository
  - **Status:** Abandoned/incomplete, active version works

### TradingEngine (1 file)

- **BinanceBrokerV2.cs.disabled**
  - Refactoring attempt for Binance broker
  - **Status:** Abandoned/incomplete, active version works

## Key Differences from Active Implementation

### Design Philosophy

**Disabled (DataAnnotations approach):**
- ASP.NET Core DataAnnotations for validation
- String-based dates for easier API serialization
- Simpler data structures
- More traditional C# approach

**Active (Modern C# approach):**
- `required` keyword (C# 11)
- Strong typing with `DateTime`
- Complex nested configurations
- Custom validation methods
- Property aliases for backward compatibility

## When to Use

Reference these files when:
- Understanding historical design decisions
- Evaluating alternative validation approaches
- Considering migration strategies
- Researching why certain patterns were chosen/rejected

## Archived Date

2025-10-19

## Notes

These implementations are NOT interchangeable with active versions due to:
- Different validation mechanisms
- Different data types (string vs DateTime)
- Different enum names
- Different property requirements
