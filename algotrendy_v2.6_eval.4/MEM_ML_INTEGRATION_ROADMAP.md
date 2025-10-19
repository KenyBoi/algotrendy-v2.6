# AlgoTrendy v2.6 - MEM/ML Integration Roadmap

**Status:** Integration Phase Started
**Date:** October 19, 2025
**Goal:** Integrate MemGPT Agent & ML Models into v2.6 C# architecture

---

## 🎯 Integration Strategy

### Why Integrate MEM/ML?
- **MemGPT Agent:** Persistent memory learning system that improves trading over time
- **ML Models:** Trend reversal detection using Gradient Boosting (78% accuracy)
- **Combined:** Smart trading with memory-based adaptability

### Architecture Pattern
```
v2.6 Trading Engine
    ↓
Existing Strategy Resolver (Momentum, RSI)
    ↓
MemGPT Connector (NEW)
    ↓ (enhanced signals with confidence scores)
ML Predictor (NEW)
    ↓ (trend reversal detection)
Final Decision
    ↓
Broker Integration
```

---

## 📋 Integration Phases

### Phase 1: ML Model Integration (4-6 hours)
**Objective:** Load and use ML models for predictions

#### 1.1 Create ML Model Loader Service
**File:** `backend/AlgoTrendy.TradingEngine/Services/MLModelService.cs`

```csharp
public class MLModelService
{
    private GradientBoostingModel _reversalModel;
    private readonly ILogger<MLModelService> _logger;

    public async Task InitializeAsync()
    {
        // Load joblib model files
        // Option A: Use Python.NET bridge
        // Option B: Re-implement with ML.NET
        // For now: Use Python subprocess or REST bridge
    }

    public async Task<TrendReversalPrediction> PredictReversalAsync(
        MarketData[] candles,
        string symbol)
    {
        // Prepare features (technical indicators)
        var features = CalculateFeatures(candles);

        // Call ML model
        var prediction = _reversalModel.Predict(features);

        return new TrendReversalPrediction
        {
            IsReversal = prediction.Label == 1,
            Confidence = prediction.Probability,
            Timestamp = DateTime.UtcNow
        };
    }

    private double[] CalculateFeatures(MarketData[] candles)
    {
        // Must match training features:
        // sma_5, sma_20, sma_50, rsi, macd, macd_signal, macd_hist,
        // bb_position, volume_ratio, hl_range, close_position, oc_range

        return new double[12];
    }
}
```

#### 1.2 Create ML Prediction Models
**File:** `backend/AlgoTrendy.Core/Models/MLModels.cs`

```csharp
public class TrendReversalPrediction
{
    public bool IsReversal { get; set; }
    public double Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MLPredictionResult
{
    public string Symbol { get; set; }
    public TrendReversalPrediction Reversal { get; set; }
    public Dictionary<string, double> AllFeatures { get; set; }
    public string ModelVersion { get; set; }
}
```

#### 1.3 Register ML Service in DI
**File:** `backend/AlgoTrendy.API/Program.cs`

```csharp
services.AddSingleton<MLModelService>();
services.AddSingleton<MLPredictionService>();
```

#### 1.4 ML Model Bridge Options

**Option A: Python.NET (Recommended for now)**
```
+ Uses existing joblib models directly
+ No re-training needed
- Requires Python runtime
- Performance overhead (~50ms per prediction)
```

**Option B: ML.NET (Long-term)**
```
+ Pure .NET, best performance
+ No Python dependency
- Requires model conversion
- ~40-60 hours conversion work
```

**Option C: REST Bridge**
```
+ Decoupled architecture
+ Easy to update models
- Extra network latency
- Additional service to maintain
```

**Recommended:** Option A (Python.NET) for Phase 1, migrate to Option B in Phase 2

#### 1.5 Test ML Integration
```bash
# Unit test trend reversal predictions
dotnet test --filter "Category=MLPrediction"

# Integration test with real data
dotnet test --filter "Category=MLIntegration"
```

---

### Phase 2: Decision Logging & Memory (3-4 hours)
**Objective:** Track trading decisions with outcomes

#### 2.1 Create Decision Logger Service
**File:** `backend/AlgoTrendy.TradingEngine/Services/DecisionLoggerService.cs`

```csharp
public class DecisionLoggerService
{
    private readonly string _memoryPath = "data/mem_knowledge/";

    public async Task LogDecisionAsync(TradingDecision decision, OrderResult result)
    {
        // Format: [timestamp] Strategy: X - Decision: Y - Confidence: Z
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] " +
            $"Strategy: {decision.StrategyName} - " +
            $"Decision: {decision.Action} - " +
            $"Confidence: {decision.Confidence}";

        await File.AppendAllTextAsync(
            Path.Combine(_memoryPath, "core_memory_updates.txt"),
            logEntry + Environment.NewLine
        );

        // Also log outcome
        if (result != null)
        {
            var outcomeEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] " +
                $"P&L: {result.PnL} - Status: {result.Status}";

            await File.AppendAllTextAsync(
                Path.Combine(_memoryPath, "core_memory_updates.txt"),
                outcomeEntry + Environment.NewLine
            );
        }
    }
}
```

#### 2.2 Create Parameter Tracker
**File:** `backend/AlgoTrendy.Core/Models/ParameterUpdate.cs`

```csharp
public class ParameterUpdate
{
    public DateTime Date { get; set; }
    public string Parameter { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public string Reason { get; set; }

    public static void SaveToJson(List<ParameterUpdate> updates)
    {
        var json = JsonConvert.SerializeObject(updates, Formatting.Indented);
        File.WriteAllText("data/mem_knowledge/parameter_updates.json", json);
    }
}
```

#### 2.3 Integrate Logging into Trading Engine
**File:** `backend/AlgoTrendy.TradingEngine/TradingEngine.cs`

```csharp
public async Task<Order> PlaceOrderAsync(OrderRequest request)
{
    // ... existing order logic ...

    // NEW: Log decision
    await _decisionLogger.LogDecisionAsync(decision, null);

    // Place order
    var result = await _broker.PlaceOrderAsync(request);

    // NEW: Log outcome
    await _decisionLogger.LogDecisionAsync(decision, result);

    return result;
}
```

---

### Phase 3: MemGPT Connector Integration (6-8 hours)
**Objective:** Integrate MemGPT agent for enhanced signal generation

#### 3.1 Create C# MemGPT Wrapper
**File:** `backend/AlgoTrendy.TradingEngine/Services/MemGPTConnectorService.cs`

```csharp
public class MemGPTConnectorService
{
    private readonly string _agentId = "trader_001";
    private readonly string _memoryPath = "data/mem_knowledge/";
    private readonly ILogger<MemGPTConnectorService> _logger;

    public async Task<EnhancedSignal> AnalyzeSignalAsync(
        StrategySignal baseSignal,
        MarketContext context)
    {
        // Load learned patterns from memory
        var learnedStrategies = LoadLearnedStrategies();

        // Enhance signal with confidence scoring
        var enhancement = CalculateEnhancement(baseSignal, learnedStrategies);

        return new EnhancedSignal
        {
            BaseSignal = baseSignal,
            MemGPTConfidence = enhancement.Confidence,
            MemGPTReason = enhancement.Reason,
            LearnedPattern = enhancement.PatternName,
            FinalConfidence = (baseSignal.Confidence + enhancement.Confidence) / 2
        };
    }

    private LearnedStrategy[] LoadLearnedStrategies()
    {
        // Read from data/mem_knowledge/strategy_modules.py
        // Parse and load learned patterns
        // For now: Return hardcoded patterns
        return new LearnedStrategy[0];
    }

    public async Task LogDecisionOutcomeAsync(
        EnhancedSignal signal,
        OrderResult result)
    {
        // Update memory with this decision's outcome
        var outcome = new DecisionOutcome
        {
            Signal = signal,
            Result = result,
            Timestamp = DateTime.UtcNow
        };

        // Append to memory
        var json = JsonConvert.SerializeObject(outcome);
        await File.AppendAllTextAsync(
            Path.Combine(_memoryPath, "core_memory_updates.txt"),
            json + Environment.NewLine
        );
    }
}
```

#### 3.2 Create Enhanced Signal Model
**File:** `backend/AlgoTrendy.Core/Models/EnhancedSignal.cs`

```csharp
public class EnhancedSignal
{
    public StrategySignal BaseSignal { get; set; }
    public double MemGPTConfidence { get; set; }
    public string MemGPTReason { get; set; }
    public string LearnedPattern { get; set; }
    public double FinalConfidence { get; set; }
}
```

#### 3.3 Integrate into Strategy Resolver
**File:** `backend/AlgoTrendy.TradingEngine/Services/StrategyResolver.cs`

```csharp
public async Task<EnhancedSignal> ResolveStrategyAsync(string symbol, int lookbackPeriods = 20)
{
    // ... existing strategy logic ...

    var baseSignal = _strategyFactory.ResolveStrategy(symbol, indicators);

    // NEW: Enhance with MemGPT
    var context = new MarketContext
    {
        Symbol = symbol,
        MarketData = recentCandles
    };
    var enhancedSignal = await _memgptConnector.AnalyzeSignalAsync(baseSignal, context);

    return enhancedSignal;
}
```

---

### Phase 4: Live Dashboard Enhancement (4-5 hours)
**Objective:** Display MemGPT & ML insights real-time

#### 4.1 Create Dashboard Service
**File:** `backend/AlgoTrendy.API/Services/DashboardService.cs`

```csharp
public class DashboardService
{
    public async Task<MemGPTDashboardData> GetDashboardDataAsync()
    {
        return new MemGPTDashboardData
        {
            RecentDecisions = await LoadRecentDecisions(limit: 10),
            MemoryStats = GetMemoryStatistics(),
            LearnedPatterns = GetLearnedPatterns(),
            MLModelMetrics = GetMLModelMetrics()
        };
    }
}
```

#### 4.2 Add SignalR Hub for Real-Time Updates
**File:** `backend/AlgoTrendy.API/Hubs/MemGPTHub.cs`

```csharp
public class MemGPTHub : Hub
{
    [HubMethodName("RequestMemoryUpdate")]
    public async Task RequestMemoryUpdate()
    {
        var data = await _dashboardService.GetDashboardDataAsync();
        await Clients.All.SendAsync("MemoryUpdated", data);
    }
}
```

#### 4.3 Extend Existing Swagger Endpoints
```
GET  /api/memgpt/dashboard       - Dashboard data
GET  /api/memgpt/decisions       - Decision history
GET  /api/memgpt/memory          - Memory state
POST /api/memgpt/learn           - Teach MemGPT pattern
```

---

### Phase 5: Model Retraining Setup (3-4 hours)
**Objective:** Automated model updates

#### 5.1 Create Retraining Service
**File:** `backend/AlgoTrendy.Core/Services/ModelRetrainingService.cs`

```csharp
public class ModelRetrainingService
{
    public async Task RetainModelAsync(string symbol)
    {
        // 1. Fetch recent market data from QuestDB
        var trainingData = await _marketDataRepository.GetHistoricalDataAsync(
            symbol,
            days: 30
        );

        // 2. Call retrain_model.py via Python.NET or process
        var result = await CallRetrainPythonScript(trainingData);

        // 3. Load new model
        if (result.Success)
        {
            await _mlModelService.LoadModelAsync(result.ModelPath);

            // 4. Update model version
            await _versionService.UpdateModelVersionAsync(symbol, result.Version);
        }
    }

    private async Task<RetrainingResult> CallRetrainPythonScript(
        List<MarketData> data)
    {
        // Execute /root/AlgoTrendy_v2.6/retrain_model.py
        // via subprocess or Python.NET

        return new RetrainingResult { Success = true };
    }
}
```

#### 5.2 Schedule Retraining Job
```csharp
// In Program.cs
services.AddHostedService<ModelRetrainingBackgroundService>();
```

#### 5.3 Create Retraining Background Service
**File:** `backend/AlgoTrendy.API/BackgroundServices/ModelRetrainingBackgroundService.cs`

```csharp
public class ModelRetrainingBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run daily at 2 AM UTC
        var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            if (now.Hour == 2)
            {
                await _modelRetrainingService.RetainModelAsync("BTCUSDT");
                await Task.Delay(TimeSpan.FromHours(1));
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
```

---

## 🔧 Implementation Checklist

### Phase 1: ML Integration
- [ ] Create `MLModelService.cs`
- [ ] Create `MLPredictionService.cs`
- [ ] Add ML models to project
- [ ] Create ML bridge (Python.NET or REST)
- [ ] Register in DI
- [ ] Write unit tests
- [ ] Integrate with StrategyResolver
- **Estimate:** 4-6 hours

### Phase 2: Decision Logging
- [ ] Create `DecisionLoggerService.cs`
- [ ] Create `ParameterUpdate.cs`
- [ ] Add logging to TradingEngine
- [ ] Write unit tests
- [ ] Verify log file creation
- **Estimate:** 3-4 hours

### Phase 3: MemGPT Connector
- [ ] Create `MemGPTConnectorService.cs`
- [ ] Create `EnhancedSignal.cs`
- [ ] Integrate with StrategyResolver
- [ ] Load learned strategies
- [ ] Write unit tests
- [ ] Integration tests
- **Estimate:** 6-8 hours

### Phase 4: Dashboard
- [ ] Create `DashboardService.cs`
- [ ] Create `MemGPTHub.cs`
- [ ] Add SignalR event handlers
- [ ] Create API endpoints
- [ ] Write unit tests
- **Estimate:** 4-5 hours

### Phase 5: Retraining
- [ ] Create `ModelRetrainingService.cs`
- [ ] Create background service
- [ ] Schedule daily runs
- [ ] Handle failures gracefully
- [ ] Write unit tests
- **Estimate:** 3-4 hours

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task MLModelService_LoadsModel_Successfully()
{
    // Arrange
    var service = new MLModelService();

    // Act
    await service.InitializeAsync();

    // Assert
    Assert.True(service.IsModelLoaded);
}

[Fact]
public async Task DecisionLogger_LogsDecision_ToFile()
{
    // Arrange
    var logger = new DecisionLoggerService();
    var decision = new TradingDecision { Action = "BUY" };

    // Act
    await logger.LogDecisionAsync(decision, null);

    // Assert
    var contents = File.ReadAllText("data/mem_knowledge/core_memory_updates.txt");
    Assert.Contains("BUY", contents);
}
```

### Integration Tests
```csharp
[Fact]
public async Task FullPipeline_MLSignalEnhancement_Works()
{
    // Setup: Full trading engine with ML
    var engine = new TradingEngineWithML();

    // Get market data
    var candles = await GetRecentCandles("BTCUSDT");

    // Generate base signal
    var baseSignal = await engine.GenerateSignalAsync(candles);

    // Enhance with ML
    var enhancedSignal = await engine.EnhanceSignalWithML(baseSignal);

    // Assert
    Assert.NotNull(enhancedSignal.MLPrediction);
    Assert.True(enhancedSignal.FinalConfidence > 0);
}
```

---

## 📊 Files & Dependencies

### New C# Files to Create
```
backend/
├── AlgoTrendy.TradingEngine/
│   └── Services/
│       ├── MLModelService.cs (NEW)
│       ├── MLPredictionService.cs (NEW)
│       ├── DecisionLoggerService.cs (NEW)
│       ├── MemGPTConnectorService.cs (NEW)
│       └── ModelRetrainingService.cs (NEW)
│
├── AlgoTrendy.Core/
│   └── Models/
│       ├── MLModels.cs (NEW)
│       ├── EnhancedSignal.cs (NEW)
│       └── ParameterUpdate.cs (NEW)
│
└── AlgoTrendy.API/
    ├── Services/
    │   └── DashboardService.cs (NEW)
    ├── Hubs/
    │   └── MemGPTHub.cs (NEW)
    └── BackgroundServices/
        └── ModelRetrainingBackgroundService.cs (NEW)
```

### NuGet Dependencies Needed
```xml
<!-- Python.NET bridge (for loading joblib models) -->
<PackageReference Include="pythonnet" Version="3.20" />

<!-- Optional: ML.NET (for future conversion) -->
<PackageReference Include="Microsoft.ML" Version="2.0" />

<!-- JSON handling for memory -->
<PackageReference Include="Newtonsoft.Json" Version="13.0" />
```

---

## 🔄 Architecture Integration Points

### Current v2.6 Architecture
```
API Layer → Strategy Resolver → Trading Engine → Broker → Exchange
```

### New MEM/ML Architecture
```
API Layer
    ↓
Strategy Resolver
    ↓
ML Predictor (NEW) ← Loads joblib models
    ↓
MemGPT Connector (NEW) ← Loads learned strategies
    ↓
Enhanced Signal
    ↓
Trading Engine
    ↓
Decision Logger (NEW) ← Saves to memory
    ↓
Broker → Exchange
```

---

## ⚠️ Potential Challenges

### Challenge 1: Python.NET Bridge
**Issue:** Loading Python joblib models from C#
**Solution:**
- Option A: Use Python.NET (IronPython alternative)
- Option B: Create Python microservice
- Option C: Re-implement model in ML.NET (long-term)

### Challenge 2: Real-time Performance
**Issue:** ML predictions add latency (~50ms)
**Solution:**
- Cache predictions for 1-5 minutes
- Run ML predictions async
- Implement prediction batching

### Challenge 3: Model Version Management
**Issue:** Multiple model versions, rollback capability
**Solution:**
- Store models with timestamps
- Version endpoint responses
- Implement A/B testing framework

### Challenge 4: Memory Persistence
**Issue:** File-based memory not scalable
**Solution:**
- Migrate to database (future)
- Current: File-based is fine for Phase 1
- Consider: QuestDB table for decision history

---

## 📈 Success Metrics

### Phase 1 (ML Integration)
- [ ] Models load successfully
- [ ] Predictions <100ms latency
- [ ] Accuracy matches v2.5 (78%+)

### Phase 2 (Decision Logging)
- [ ] All decisions logged
- [ ] Log files created correctly
- [ ] Decision history accessible

### Phase 3 (MemGPT)
- [ ] Enhanced signals generated
- [ ] Confidence scores correct
- [ ] Pattern learning detected

### Phase 4 (Dashboard)
- [ ] Real-time updates work
- [ ] All metrics visible
- [ ] No performance degradation

### Phase 5 (Retraining)
- [ ] Models retrain daily
- [ ] New models load automatically
- [ ] Performance improves over time

---

## 📝 Next Steps

1. ✅ Copy MEM/ML files from v2.5
2. ⏳ Begin Phase 1: ML Integration (4-6 hours)
3. ⏳ Phase 2: Decision Logging (3-4 hours)
4. ⏳ Phase 3: MemGPT Connector (6-8 hours)
5. ⏳ Phase 4: Dashboard (4-5 hours)
6. ⏳ Phase 5: Retraining (3-4 hours)
7. ⏳ Testing & QA
8. ⏳ Documentation
9. ⏳ Deployment

**Total Estimate:** 30-40 hours (can be done in 1-2 weeks with focused effort)

---

**Status:** Roadmap Created
**Next:** Begin Phase 1 Implementation
**Target:** Complete all phases by end of October 2025

