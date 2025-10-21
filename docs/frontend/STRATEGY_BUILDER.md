# Strategy Builder - Frontend Documentation

**Version**: 1.0.0
**Last Updated**: October 21, 2025
**Route**: `/strategies`

---

## Overview

The Strategy Builder is a comprehensive visual interface for creating, configuring, and testing trading strategies without writing code. It provides an intuitive way to build strategies that integrate with the Strategy Registry backend.

---

## Features

### ✅ Implemented

1. **Strategy Metadata Configuration**
   - Name, display name, and description
   - Category selection (momentum, mean reversion, carry, etc.)
   - Tag management
   - Language selection (Python/C#)
   - Status management

2. **Parameter Management**
   - Add/remove custom parameters
   - Support for number, string, boolean, and select types
   - Min/max validation for numbers
   - Default values and descriptions

3. **Signal Builder** (Basic)
   - Entry signal configuration
   - Exit signal configuration
   - Signal management

4. **Risk Management**
   - Stop loss configuration (fixed, trailing, ATR)
   - Take profit configuration
   - Position sizing methods
   - Max drawdown and position limits

5. **Backtesting Integration**
   - Backtest configuration panel
   - Results visualization
   - Performance metrics display
   - Equity curve chart

6. **Code Generation**
   - Generate Python or C# code from strategy configuration
   - Code viewer with syntax highlighting
   - Copy and download functionality

7. **Validation**
   - Real-time strategy validation
   - Error and warning display
   - Save button only enabled when valid

---

## Architecture

### File Structure

```
frontend/src/
├── types/
│   └── strategy.ts                    # TypeScript type definitions
├── lib/
│   └── strategyApi.ts                 # API service for Strategy Registry
├── pages/
│   └── StrategyBuilderPage.tsx        # Main page component
└── components/strategy/
    ├── MetadataForm.tsx               # Strategy info configuration
    ├── ParametersForm.tsx             # Parameter management
    ├── SignalsBuilder.tsx             # Entry/exit signals
    ├── RiskManagementForm.tsx         # Risk management settings
    ├── BacktestPanel.tsx              # Backtest configuration & results
    └── StrategyCodeViewer.tsx         # Generated code display
```

---

## Usage Guide

### Accessing the Strategy Builder

Navigate to `/strategies` or click "Strategy Builder" in the main navigation.

### Creating a Strategy

#### Step 1: Strategy Information

1. Fill in basic information:
   - **Strategy Name**: Technical name (lowercase, underscores)
   - **Display Name**: Human-readable name
   - **Description**: Detailed description of the strategy
   - **Category**: Select from predefined categories
   - **Language**: Python or C#
   - **Status**: Experimental, Backtested, Active, or Deprecated

2. Add tags for easier discovery:
   - Type a tag and press Enter or click +
   - Remove tags by clicking X

#### Step 2: Parameters

1. Click "Add Parameter" to create a new parameter
2. Configure:
   - **Name**: Parameter identifier
   - **Type**: Number, String, Boolean, or Select
   - **Default Value**: Initial value
   - **Description**: Optional explanation
   - **Min/Max**: For number types (optional)

3. Parameters can be edited or removed later

#### Step 3: Entry/Exit Signals

1. Add entry signals for when to enter positions
2. Add exit signals for when to exit positions
3. Configure conditions for each signal

**Note**: Advanced condition builder coming soon. Currently shows placeholder UI.

#### Step 4: Risk Management

Configure risk controls:

- **Stop Loss**:
  - Toggle on/off
  - Type: Fixed %, Trailing %, or ATR Multiple
  - Value: Percentage or multiplier

- **Take Profit**:
  - Toggle on/off
  - Type: Fixed % or Trailing %
  - Value: Percentage

- **Position Sizing**:
  - Method: Fixed, Percentage, Kelly Criterion, or Risk-Based
  - Value: Amount or percentage
  - Max Drawdown: Optional portfolio limit
  - Max Positions: Optional concurrent position limit

#### Step 5: Backtesting

1. Configure backtest parameters:
   - Symbol (e.g., BTC-USD)
   - Timeframe (1m to 1d)
   - Date range
   - Initial capital
   - Commission and slippage

2. Click "Run Backtest"

3. View results:
   - Performance metrics
   - Equity curve
   - Trade statistics

#### Step 6: Code Generation

1. Click "Generate Code" in the header
2. View generated code in the Code tab
3. Copy to clipboard or download as file

#### Step 7: Save

1. Ensure validation passes (green checkmark)
2. Click "Save Strategy"
3. Strategy is saved to the backend registry

---

## API Integration

### Strategy Registry API

The Strategy Builder integrates with the backend Strategy Registry API:

```typescript
// Get all strategies
const strategies = await strategyApi.getAllStrategies();

// Create a new strategy
const result = await strategyApi.createStrategy({
  metadata: { ... },
  parameters: { ... },
});

// Run backtest
const backtest = await strategyApi.runBacktest(strategyId, config);

// Generate code
const code = await strategyApi.generateCode(strategy, 'python');
```

### Backend Endpoints Required

The following API endpoints must be implemented in the backend:

- `GET /api/strategies` - List all strategies
- `POST /api/strategies` - Create new strategy
- `PUT /api/strategies/:id` - Update strategy
- `DELETE /api/strategies/:id` - Delete strategy
- `POST /api/strategies/validate` - Validate strategy config
- `POST /api/strategies/generate-code` - Generate code
- `POST /api/strategies/:id/backtest` - Run backtest
- `GET /api/strategies/:id/backtest/:backtestId` - Get backtest results

---

## Type Definitions

### Key Types

```typescript
interface Strategy {
  metadata: StrategyMetadata;
  parameters: StrategyParameter[];
  signals: {
    entry: StrategySignal[];
    exit: StrategySignal[];
  };
  riskManagement: StrategyRiskManagement;
  code?: string;
  backtestResults?: BacktestResults;
}

interface StrategyMetadata {
  id?: string;
  name: string;
  displayName: string;
  description: string;
  category: StrategyCategory;
  tags: string[];
  language: 'python' | 'csharp';
  status: StrategyStatus;
}

interface StrategyParameter {
  name: string;
  type: 'number' | 'string' | 'boolean' | 'select';
  value: any;
  min?: number;
  max?: number;
  description?: string;
}
```

See `/frontend/src/types/strategy.ts` for complete type definitions.

---

## Styling

The Strategy Builder uses the existing AlgoTrendy design system:

- **Colors**: Uses CSS variables (--primary, --background, --card-bg, etc.)
- **Spacing**: Consistent padding and margins
- **Components**: Reusable form inputs, buttons, cards
- **Responsive**: Grid layouts adapt to screen size

### CSS Variables Used

```css
--primary          # Primary accent color
--background       # Main background
--card-bg          # Card backgrounds
--border           # Border color
--text             # Primary text
--text-secondary   # Secondary text
--success          # Success/positive
--warning          # Warning
--error            # Error/negative
```

---

## Future Enhancements

### Phase 2: Advanced Features

1. **Visual Condition Builder**
   - Drag-and-drop indicator selection
   - Visual operator selection
   - Condition preview

2. **Strategy Templates**
   - Pre-built strategy templates
   - Community templates
   - Template customization

3. **Real-time Preview**
   - Live strategy preview
   - Mock trade simulation
   - Visual signal indicators on charts

4. **Advanced Backtesting**
   - Walk-forward analysis
   - Monte Carlo simulation
   - Parameter optimization

5. **Strategy Management**
   - List view of all strategies
   - Duplicate/clone strategies
   - Version history
   - Strategy comparison

6. **Collaboration**
   - Share strategies
   - Export/import strategies
   - Strategy marketplace

---

## Testing

### Manual Testing Checklist

- [ ] Load Strategy Builder page
- [ ] Fill out metadata form
- [ ] Add/edit/remove parameters
- [ ] Add entry and exit signals
- [ ] Configure risk management
- [ ] Run validation
- [ ] Generate code
- [ ] Download code
- [ ] Save strategy
- [ ] Verify saved in backend

### Integration Testing

Test with backend API:

1. Start backend server
2. Configure `VITE_API_URL` in frontend
3. Create a test strategy
4. Verify it appears in Strategy Registry
5. Run backtest
6. Verify results display correctly

---

## Troubleshooting

### Common Issues

**Issue**: Strategy Builder page is blank
- **Solution**: Check browser console for errors. Ensure all components are imported correctly.

**Issue**: Save button disabled
- **Solution**: Check validation panel for errors. Fill required fields (name, displayName, description).

**Issue**: API calls fail
- **Solution**: Check `VITE_API_URL` environment variable. Ensure backend is running.

**Issue**: Generated code is empty
- **Solution**: Click "Generate Code" button first. Code generation requires complete strategy configuration.

**Issue**: Backtest fails
- **Solution**: Verify backend backtest endpoint is implemented. Check network tab for error details.

---

## Component Props Reference

### MetadataForm

```typescript
interface Props {
  metadata: StrategyMetadata;
  onChange: (metadata: StrategyMetadata) => void;
}
```

### ParametersForm

```typescript
interface Props {
  parameters: StrategyParameter[];
  onChange: (parameters: StrategyParameter[]) => void;
}
```

### SignalsBuilder

```typescript
interface Props {
  signals: { entry: StrategySignal[]; exit: StrategySignal[] };
  onChange: (signals: { entry: StrategySignal[]; exit: StrategySignal[] }) => void;
}
```

### RiskManagementForm

```typescript
interface Props {
  riskManagement: StrategyRiskManagement;
  onChange: (riskManagement: StrategyRiskManagement) => void;
}
```

### BacktestPanel

```typescript
interface Props {
  onRunBacktest: (config: BacktestConfig) => void;
  results: BacktestResults | null;
}
```

### StrategyCodeViewer

```typescript
interface Props {
  code: string;
  language: 'python' | 'csharp';
}
```

---

## Performance Considerations

1. **Validation**: Debounced to avoid excessive API calls
2. **State Management**: Local state for form fields, API calls only on save
3. **Code Generation**: On-demand, not automatic
4. **Backtesting**: Async operation with loading states

---

## Security Considerations

1. **Input Validation**: All inputs validated on frontend and backend
2. **Code Generation**: Backend generates code, frontend only displays
3. **API Authentication**: Strategy API calls should require authentication
4. **XSS Protection**: All user input sanitized before display

---

## Related Documentation

- [Strategy Registry Architecture](/strategies/development/strategy_research_2025_q4/reports/STRATEGY_REGISTRY_ARCHITECTURE.md)
- [Backend Strategy Registry](/backend/AlgoTrendy.Core/Services/StrategyRegistry/README.md)
- [API Documentation](/docs/api/API_USAGE_EXAMPLES.md)

---

**Maintained by**: Frontend Team
**Last Updated**: October 21, 2025
**Status**: ✅ Ready for Testing
