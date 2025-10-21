# ML Training Web Page - Integration Guide

**Version**: 2.6
**Date**: October 20, 2025
**Purpose**: Complete integration guide for ML Training visualizer with drift detection and overfitting monitoring

---

## 📋 Table of Contents

1. [Data Connection Points](#data-connection-points)
2. [Backend API Endpoints](#backend-api-endpoints)
3. [Frontend Architecture](#frontend-architecture)
4. [ML Visualizer Components](#ml-visualizer-components)
5. [Drift Detection System](#drift-detection-system)
6. [Overfitting Detection](#overfitting-detection)
7. [Implementation Steps](#implementation-steps)

---

## 1. Data Connection Points

### 1.1 ML Model Files Location

```
/root/AlgoTrendy_v2.6/ml_models/
├── trend_reversals/
│   └── 20251016_234123/
│       ├── reversal_model.joblib         (111 KB) - Trained model
│       ├── reversal_scaler.joblib        (0.9 KB) - Feature scaler
│       ├── config.json                   (1.5 KB) - Model configuration
│       └── model_metrics.json            (0.2 KB) - Performance metrics
```

### 1.2 MEM Knowledge Files

```
/root/AlgoTrendy_v2.6/data/mem_knowledge/
├── core_memory_updates.txt               - Decision history & learned patterns
├── parameter_updates.json                - Parameter tuning log
└── strategy_modules.py                   - Auto-generated strategies
```

### 1.3 Market Data Sources

```
/root/AlgoTrendy_v2.6/data/
├── pattern_analysis_report.json          - Latest pattern analysis
├── market_data/                          - Historical OHLCV data
└── questdb/                              - Time-series database
```

### 1.4 Python ML Scripts

```
/root/AlgoTrendy_v2.6/
├── retrain_model.py                      - Model retraining pipeline
├── run_pattern_analysis.py               - Pattern discovery engine
└── MEM/                                  - MEM modules and connectors
```

---

## 2. Backend API Endpoints

### 2.1 Create New ML Controller

**File**: `backend/AlgoTrendy.API/Controllers/MLTrainingController.cs`

#### Endpoint Structure

```csharp
[ApiController]
[Route("api/[controller]")]
public class MLTrainingController : ControllerBase
{
    // GET /api/mltraining/models
    [HttpGet("models")]
    public async Task<ActionResult<List<MLModelInfo>>> GetAllModels()

    // GET /api/mltraining/models/{modelId}
    [HttpGet("models/{modelId}")]
    public async Task<ActionResult<MLModelDetails>> GetModelDetails(string modelId)

    // GET /api/mltraining/metrics/{modelId}
    [HttpGet("metrics/{modelId}")]
    public async Task<ActionResult<ModelMetrics>> GetModelMetrics(string modelId)

    // POST /api/mltraining/train
    [HttpPost("train")]
    public async Task<ActionResult<TrainingJobResult>> StartTraining(TrainingConfig config)

    // GET /api/mltraining/training/{jobId}
    [HttpGet("training/{jobId}")]
    public async Task<ActionResult<TrainingStatus>> GetTrainingStatus(string jobId)

    // GET /api/mltraining/drift/{modelId}
    [HttpGet("drift/{modelId}")]
    public async Task<ActionResult<DriftMetrics>> GetDriftMetrics(string modelId)

    // GET /api/mltraining/performance/{modelId}
    [HttpGet("performance/{modelId}")]
    public async Task<ActionResult<PerformanceHistory>> GetPerformanceHistory(string modelId)

    // GET /api/mltraining/patterns
    [HttpGet("patterns")]
    public async Task<ActionResult<PatternAnalysis>> GetLatestPatterns()

    // POST /api/mltraining/predict
    [HttpPost("predict")]
    public async Task<ActionResult<PredictionResult>> GetPrediction(PredictionRequest request)
}
```

### 2.2 Required DTOs

```csharp
// DTOs/MLTrainingDtos.cs

public class MLModelInfo
{
    public string ModelId { get; set; }
    public string ModelType { get; set; }
    public DateTime TrainedAt { get; set; }
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int TrainingRows { get; set; }
    public string Status { get; set; } // "active", "deprecated", "training"
}

public class MLModelDetails : MLModelInfo
{
    public List<string> Features { get; set; }
    public Dictionary<string, double> FeatureImportance { get; set; }
    public ConfusionMatrix ConfusionMatrix { get; set; }
    public List<ValidationMetric> ValidationHistory { get; set; }
}

public class ModelMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AUC { get; set; }
    public ConfusionMatrix ConfusionMatrix { get; set; }
    public List<MetricHistory> History { get; set; }
}

public class TrainingConfig
{
    public List<string> Symbols { get; set; }
    public int Days { get; set; }
    public string Interval { get; set; }
    public Dictionary<string, object> Hyperparameters { get; set; }
}

public class TrainingJobResult
{
    public string JobId { get; set; }
    public string Status { get; set; }
    public DateTime StartedAt { get; set; }
}

public class TrainingStatus
{
    public string JobId { get; set; }
    public string Status { get; set; } // "queued", "running", "completed", "failed"
    public double Progress { get; set; } // 0-100
    public string CurrentStep { get; set; }
    public List<string> Logs { get; set; }
    public ModelMetrics? Result { get; set; }
}

public class DriftMetrics
{
    public string ModelId { get; set; }
    public DateTime LastChecked { get; set; }
    public double DriftScore { get; set; } // 0-1 (0 = no drift, 1 = complete drift)
    public bool IsDrifting { get; set; }
    public Dictionary<string, double> FeatureDrift { get; set; }
    public List<DriftHistory> History { get; set; }
    public string RecommendedAction { get; set; }
}

public class PerformanceHistory
{
    public string ModelId { get; set; }
    public List<PerformancePoint> TrainingPerformance { get; set; }
    public List<PerformancePoint> ValidationPerformance { get; set; }
    public List<PerformancePoint> ProductionPerformance { get; set; }
}

public class PerformancePoint
{
    public DateTime Timestamp { get; set; }
    public double Accuracy { get; set; }
    public double Loss { get; set; }
    public int Epoch { get; set; }
}

public class PatternAnalysis
{
    public DateTime Timestamp { get; set; }
    public List<OpportunityInfo> Opportunities { get; set; }
    public Dictionary<string, int> PatternDistribution { get; set; }
    public int SymbolsAnalyzed { get; set; }
}

public class OpportunityInfo
{
    public string Symbol { get; set; }
    public double Price { get; set; }
    public double RSI { get; set; }
    public double VolumeRatio { get; set; }
    public double ReversalConfidence { get; set; }
    public List<PatternInfo> Patterns { get; set; }
    public double OpportunityScore { get; set; }
}

public class PredictionRequest
{
    public string Symbol { get; set; }
    public List<OHLCV> RecentCandles { get; set; }
}

public class PredictionResult
{
    public string Symbol { get; set; }
    public bool IsReversal { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> FeatureValues { get; set; }
    public string Reasoning { get; set; }
}
```

### 2.3 SignalR Hub for Real-Time Updates

```csharp
// Hubs/MLTrainingHub.cs

public class MLTrainingHub : Hub
{
    // Real-time training progress updates
    public async Task SubscribeToTrainingJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"training-{jobId}");
    }

    public async Task UnsubscribeFromTrainingJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"training-{jobId}");
    }

    // Server-side: Broadcast training progress
    // Called by TrainingService
    public async Task SendTrainingProgress(string jobId, TrainingProgress progress)
    {
        await Clients.Group($"training-{jobId}").SendAsync("TrainingProgress", progress);
    }

    // Real-time drift alerts
    public async Task SubscribeToDriftAlerts(string modelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"drift-{modelId}");
    }

    // Server-side: Broadcast drift alerts
    public async Task SendDriftAlert(string modelId, DriftAlert alert)
    {
        await Clients.Group($"drift-{modelId}").SendAsync("DriftAlert", alert);
    }
}
```

---

## 3. Frontend Architecture

### 3.1 Page Structure

```
frontend/src/pages/MLTraining/
├── MLTrainingDashboard.tsx          - Main page component
├── components/
│   ├── ModelList.tsx                - List of all ML models
│   ├── ModelDetails.tsx             - Detailed model view
│   ├── TrainingVisualizer.tsx       - Real-time training graphs
│   ├── DriftDetector.tsx            - Drift monitoring component
│   ├── OverfittingMonitor.tsx       - Overfitting detection
│   ├── FeatureImportance.tsx        - Feature importance charts
│   ├── ConfusionMatrix.tsx          - Confusion matrix visualizer
│   ├── PerformanceChart.tsx         - Performance over time
│   ├── PatternExplorer.tsx          - Pattern analysis viewer
│   └── TrainingControls.tsx         - Training configuration UI
├── hooks/
│   ├── useMLModels.ts               - Fetch ML models
│   ├── useTraining.ts               - Training job management
│   ├── useDriftDetection.ts         - Drift monitoring hook
│   └── useRealTimeMetrics.ts        - SignalR connection
├── services/
│   ├── mlApiService.ts              - API client
│   └── signalRService.ts            - SignalR client
├── types/
│   └── ml-types.ts                  - TypeScript interfaces
└── utils/
    ├── chartHelpers.ts              - Chart configuration
    └── metricCalculators.ts         - Metric calculations
```

### 3.2 Technology Stack

**Core**:
- React 18+ with TypeScript
- React Router for navigation
- SignalR for real-time updates
- Axios for HTTP requests

**UI Components**:
- shadcn/ui for base components
- Recharts for data visualization
- Tailwind CSS for styling
- Framer Motion for animations

**State Management**:
- React Query for server state
- Zustand for client state

---

## 4. ML Visualizer Components

### 4.1 Training Visualizer Component

**Purpose**: Real-time visualization of model training progress

**Features**:
- Live accuracy/loss curves
- Epoch progress bar
- Training vs validation metrics
- ETA and time elapsed
- GPU/CPU utilization (if available)

**Data Flow**:
```
Backend Training Job
    ↓ (SignalR)
TrainingVisualizer Component
    ↓ (updates every 100ms)
Recharts Line Chart
```

**Key Metrics Displayed**:
1. Training Accuracy (blue line)
2. Validation Accuracy (orange line)
3. Training Loss (green line)
4. Validation Loss (red line)
5. Learning Rate (yellow line)
6. Epoch counter
7. Samples processed
8. Time per epoch
9. ETA to completion

**Component Interface**:
```typescript
interface TrainingVisualizerProps {
  jobId: string;
  onComplete?: (metrics: ModelMetrics) => void;
  onError?: (error: Error) => void;
}
```

### 4.2 Drift Detection Component

**Purpose**: Monitor model performance degradation over time

**Features**:
- Drift score visualization (0-1 scale)
- Feature-level drift breakdown
- Historical drift trends
- Alert thresholds
- Automatic retraining triggers

**Drift Metrics**:
1. **Population Stability Index (PSI)**
   - Measures distribution shift in features
   - PSI < 0.1: No drift
   - PSI 0.1-0.25: Moderate drift
   - PSI > 0.25: Significant drift

2. **KS Statistic (Kolmogorov-Smirnov)**
   - Measures maximum difference between distributions
   - KS < 0.1: Minimal drift
   - KS 0.1-0.2: Moderate drift
   - KS > 0.2: High drift

3. **Accuracy Degradation**
   - Compare production accuracy to training accuracy
   - Alert if drops > 5%

**Visual Components**:
- Line chart: Drift score over time
- Bar chart: Feature-level PSI scores
- Heatmap: Feature correlations over time
- Alert banner: When drift threshold exceeded

**Component Interface**:
```typescript
interface DriftDetectorProps {
  modelId: string;
  thresholdPSI?: number; // default 0.25
  thresholdAccuracy?: number; // default 0.05 (5%)
  onDriftDetected?: (metrics: DriftMetrics) => void;
}
```

### 4.3 Overfitting Detection Component

**Purpose**: Identify and visualize overfitting during training

**Indicators**:
1. **Training vs Validation Gap**
   - If training accuracy >> validation accuracy
   - Gap > 10% = likely overfitting

2. **Validation Loss Increasing**
   - If validation loss increases while training loss decreases
   - Classic overfitting pattern

3. **High Variance**
   - Large fluctuations in validation metrics
   - Indicates model is too sensitive

4. **Early Stopping Trigger**
   - No improvement in validation for N epochs
   - Suggests overfitting started

**Visual Components**:
- Dual-axis chart: Training vs Validation
- Gap visualization (shaded area between lines)
- Variance bands (confidence intervals)
- Early stopping marker
- Overfitting score gauge (0-100)

**Overfitting Score Calculation**:
```python
gap_score = (train_accuracy - val_accuracy) * 100
loss_direction_score = 1 if val_loss_increasing else 0
variance_score = (val_accuracy_std / val_accuracy_mean) * 100
overfitting_score = (gap_score * 0.5) + (loss_direction_score * 0.3) + (variance_score * 0.2)
```

**Component Interface**:
```typescript
interface OverfittingMonitorProps {
  trainingHistory: PerformanceHistory;
  thresholdGap?: number; // default 0.10 (10%)
  onOverfittingDetected?: (score: number) => void;
}
```

---

## 5. Drift Detection System

### 5.1 Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Drift Detection Pipeline                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐     ┌──────────────┐                │
│  │   Training   │────>│  Reference   │                │
│  │     Data     │     │Distribution  │                │
│  └──────────────┘     └──────────────┘                │
│                              │                          │
│                              ▼                          │
│  ┌──────────────┐     ┌──────────────┐                │
│  │ Production   │────>│   Calculate  │                │
│  │     Data     │     │  Drift Score │                │
│  └──────────────┘     └──────┬───────┘                │
│                              │                          │
│                              ▼                          │
│                       ┌──────────────┐                 │
│                       │ Drift > 0.25?│                 │
│                       └──────┬───────┘                 │
│                              │                          │
│               YES            │            NO            │
│              ┌───────────────┴───────────────┐         │
│              ▼                               ▼         │
│       ┌──────────────┐             ┌──────────────┐   │
│       │Trigger Alert │             │  Continue    │   │
│       │& Retrain     │             │ Monitoring   │   │
│       └──────────────┘             └──────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 5.2 Implementation - Backend Service

```csharp
// Services/DriftDetectionService.cs

public class DriftDetectionService
{
    private readonly IMLModelService _mlService;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<DriftDetectionService> _logger;

    public async Task<DriftMetrics> CalculateDriftAsync(string modelId)
    {
        // 1. Load reference distribution (training data)
        var referenceData = await LoadReferenceDistribution(modelId);

        // 2. Get recent production data (last 7 days)
        var productionData = await GetRecentProductionData(modelId, days: 7);

        // 3. Calculate PSI for each feature
        var featureDrift = new Dictionary<string, double>();
        foreach (var feature in referenceData.Features)
        {
            var psi = CalculatePSI(
                referenceData.Distributions[feature],
                productionData.Distributions[feature]
            );
            featureDrift[feature] = psi;
        }

        // 4. Calculate overall drift score
        var driftScore = featureDrift.Values.Average();

        // 5. Check accuracy degradation
        var trainingAccuracy = referenceData.Accuracy;
        var productionAccuracy = await GetProductionAccuracy(modelId);
        var accuracyDrop = trainingAccuracy - productionAccuracy;

        // 6. Determine if retraining needed
        var isDrifting = driftScore > 0.25 || accuracyDrop > 0.05;

        return new DriftMetrics
        {
            ModelId = modelId,
            LastChecked = DateTime.UtcNow,
            DriftScore = driftScore,
            IsDrifting = isDrifting,
            FeatureDrift = featureDrift,
            AccuracyDrop = accuracyDrop,
            RecommendedAction = isDrifting
                ? "Retrain model immediately"
                : "No action needed"
        };
    }

    private double CalculatePSI(Distribution reference, Distribution production)
    {
        double psi = 0.0;
        var bins = reference.Bins;

        for (int i = 0; i < bins.Count; i++)
        {
            var refPct = reference.Frequencies[i] / reference.Total;
            var prodPct = production.Frequencies[i] / production.Total;

            // Avoid log(0)
            if (refPct < 0.0001) refPct = 0.0001;
            if (prodPct < 0.0001) prodPct = 0.0001;

            psi += (prodPct - refPct) * Math.Log(prodPct / refPct);
        }

        return psi;
    }
}
```

### 5.3 Implementation - Frontend Component

```typescript
// components/DriftDetector.tsx

import { useEffect, useState } from 'react';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
import { useDriftDetection } from '../hooks/useDriftDetection';

interface DriftDetectorProps {
  modelId: string;
  thresholdPSI?: number;
  onDriftDetected?: (metrics: DriftMetrics) => void;
}

export function DriftDetector({ modelId, thresholdPSI = 0.25, onDriftDetected }: DriftDetectorProps) {
  const { driftMetrics, history, loading, error } = useDriftDetection(modelId);

  useEffect(() => {
    if (driftMetrics?.isDrifting) {
      onDriftDetected?.(driftMetrics);
    }
  }, [driftMetrics]);

  if (loading) return <div>Loading drift metrics...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div className="drift-detector">
      {/* Alert Banner */}
      {driftMetrics?.isDrifting && (
        <div className="alert alert-warning">
          ⚠️ Model Drift Detected! Score: {driftMetrics.driftScore.toFixed(3)}
          <br />
          {driftMetrics.recommendedAction}
        </div>
      )}

      {/* Drift Score Gauge */}
      <div className="drift-gauge">
        <h3>Overall Drift Score</h3>
        <DriftGauge value={driftMetrics?.driftScore || 0} threshold={thresholdPSI} />
      </div>

      {/* Historical Drift Trend */}
      <div className="drift-history">
        <h3>Drift Over Time</h3>
        <LineChart width={800} height={300} data={history}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="timestamp" />
          <YAxis />
          <Tooltip />
          <Legend />
          <Line
            type="monotone"
            dataKey="driftScore"
            stroke="#8884d8"
            strokeWidth={2}
            name="Drift Score"
          />
          <Line
            type="monotone"
            dataKey="threshold"
            stroke="#ff0000"
            strokeDasharray="5 5"
            name="Alert Threshold"
          />
        </LineChart>
      </div>

      {/* Feature-Level Drift */}
      <div className="feature-drift">
        <h3>Feature Drift Breakdown</h3>
        <BarChart width={800} height={400} data={formatFeatureDrift(driftMetrics?.featureDrift)}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="feature" angle={-45} textAnchor="end" height={100} />
          <YAxis />
          <Tooltip />
          <Legend />
          <Bar
            dataKey="psi"
            fill="#82ca9d"
            name="PSI Score"
          />
        </BarChart>
      </div>
    </div>
  );
}

function DriftGauge({ value, threshold }: { value: number; threshold: number }) {
  const percentage = Math.min((value / (threshold * 2)) * 100, 100);
  const color = value < threshold * 0.5 ? '#4ade80' : value < threshold ? '#facc15' : '#ef4444';

  return (
    <div className="gauge">
      <svg width="200" height="200" viewBox="0 0 200 200">
        <circle cx="100" cy="100" r="80" fill="none" stroke="#e5e7eb" strokeWidth="20" />
        <circle
          cx="100"
          cy="100"
          r="80"
          fill="none"
          stroke={color}
          strokeWidth="20"
          strokeDasharray={`${percentage * 5.03} 503`}
          transform="rotate(-90 100 100)"
        />
        <text x="100" y="110" textAnchor="middle" fontSize="24" fontWeight="bold">
          {value.toFixed(3)}
        </text>
        <text x="100" y="130" textAnchor="middle" fontSize="12">
          {value < threshold ? 'Healthy' : 'Drifting'}
        </text>
      </svg>
    </div>
  );
}
```

---

## 6. Overfitting Detection

### 6.1 Detection Logic

```python
# Backend: Python service for overfitting detection

def detect_overfitting(training_history):
    """
    Detect overfitting from training history

    Returns:
        {
            'is_overfitting': bool,
            'overfitting_score': float (0-100),
            'indicators': {
                'gap': float,
                'val_loss_increasing': bool,
                'variance': float,
                'early_stop_triggered': bool
            },
            'recommendation': str
        }
    """
    train_acc = training_history['train_accuracy']
    val_acc = training_history['val_accuracy']
    train_loss = training_history['train_loss']
    val_loss = training_history['val_loss']

    # 1. Calculate accuracy gap
    final_gap = train_acc[-1] - val_acc[-1]

    # 2. Check if validation loss is increasing
    val_loss_increasing = val_loss[-1] > val_loss[-5] if len(val_loss) > 5 else False

    # 3. Calculate validation variance
    val_variance = np.std(val_acc[-10:]) / np.mean(val_acc[-10:]) if len(val_acc) >= 10 else 0

    # 4. Check early stopping
    best_val_acc = max(val_acc)
    epochs_since_improvement = len(val_acc) - val_acc.index(best_val_acc) - 1
    early_stop_triggered = epochs_since_improvement > 5

    # Calculate overfitting score (0-100)
    gap_score = min(final_gap * 100, 50)  # Max 50 points
    loss_score = 20 if val_loss_increasing else 0  # 20 points
    variance_score = min(val_variance * 100, 20)  # Max 20 points
    early_stop_score = 10 if early_stop_triggered else 0  # 10 points

    overfitting_score = gap_score + loss_score + variance_score + early_stop_score

    is_overfitting = overfitting_score > 30  # Threshold

    # Generate recommendation
    if overfitting_score < 20:
        recommendation = "Model is well-fit. Safe to deploy."
    elif overfitting_score < 40:
        recommendation = "Mild overfitting detected. Consider adding regularization (dropout, L2)."
    elif overfitting_score < 60:
        recommendation = "Moderate overfitting. Reduce model complexity or increase training data."
    else:
        recommendation = "Severe overfitting. Retrain with more data and stronger regularization."

    return {
        'is_overfitting': is_overfitting,
        'overfitting_score': overfitting_score,
        'indicators': {
            'gap': final_gap,
            'val_loss_increasing': val_loss_increasing,
            'variance': val_variance,
            'early_stop_triggered': early_stop_triggered
        },
        'recommendation': recommendation
    }
```

### 6.2 Frontend Visualization

```typescript
// components/OverfittingMonitor.tsx

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ReferenceLine } from 'recharts';

interface OverfittingMonitorProps {
  trainingHistory: PerformanceHistory;
  thresholdGap?: number;
  onOverfittingDetected?: (score: number) => void;
}

export function OverfittingMonitor({
  trainingHistory,
  thresholdGap = 0.10,
  onOverfittingDetected
}: OverfittingMonitorProps) {
  const overfittingAnalysis = analyzeOverfitting(trainingHistory, thresholdGap);

  useEffect(() => {
    if (overfittingAnalysis.isOverfitting) {
      onOverfittingDetected?.(overfittingAnalysis.score);
    }
  }, [overfittingAnalysis]);

  return (
    <div className="overfitting-monitor">
      {/* Alert */}
      {overfittingAnalysis.isOverfitting && (
        <div className="alert alert-danger">
          🚨 Overfitting Detected! Score: {overfittingAnalysis.score.toFixed(0)}/100
          <br />
          {overfittingAnalysis.recommendation}
        </div>
      )}

      {/* Overfitting Score Gauge */}
      <div className="score-display">
        <h3>Overfitting Score</h3>
        <OverfittingGauge score={overfittingAnalysis.score} />
      </div>

      {/* Training vs Validation Chart */}
      <div className="performance-chart">
        <h3>Training vs Validation Performance</h3>
        <LineChart width={800} height={400} data={formatChartData(trainingHistory)}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="epoch" label={{ value: 'Epoch', position: 'insideBottom', offset: -5 }} />
          <YAxis
            yAxisId="accuracy"
            label={{ value: 'Accuracy', angle: -90, position: 'insideLeft' }}
            domain={[0, 1]}
          />
          <YAxis
            yAxisId="loss"
            orientation="right"
            label={{ value: 'Loss', angle: 90, position: 'insideRight' }}
          />
          <Tooltip />
          <Legend />

          {/* Training Accuracy */}
          <Line
            yAxisId="accuracy"
            type="monotone"
            dataKey="trainAccuracy"
            stroke="#2563eb"
            strokeWidth={2}
            name="Training Accuracy"
          />

          {/* Validation Accuracy */}
          <Line
            yAxisId="accuracy"
            type="monotone"
            dataKey="valAccuracy"
            stroke="#f59e0b"
            strokeWidth={2}
            name="Validation Accuracy"
            strokeDasharray="5 5"
          />

          {/* Training Loss */}
          <Line
            yAxisId="loss"
            type="monotone"
            dataKey="trainLoss"
            stroke="#10b981"
            strokeWidth={1.5}
            name="Training Loss"
          />

          {/* Validation Loss */}
          <Line
            yAxisId="loss"
            type="monotone"
            dataKey="valLoss"
            stroke="#ef4444"
            strokeWidth={1.5}
            name="Validation Loss"
            strokeDasharray="5 5"
          />

          {/* Gap Area (Shaded) */}
          {overfittingAnalysis.indicators.gap > thresholdGap && (
            <ReferenceLine
              yAxisId="accuracy"
              y={overfittingAnalysis.indicators.gap}
              stroke="red"
              strokeDasharray="3 3"
              label="Overfitting Threshold"
            />
          )}
        </LineChart>
      </div>

      {/* Indicators Breakdown */}
      <div className="indicators-grid">
        <div className="indicator-card">
          <h4>Accuracy Gap</h4>
          <div className={`value ${overfittingAnalysis.indicators.gap > thresholdGap ? 'alert' : ''}`}>
            {(overfittingAnalysis.indicators.gap * 100).toFixed(1)}%
          </div>
          <div className="threshold">Threshold: {(thresholdGap * 100).toFixed(0)}%</div>
        </div>

        <div className="indicator-card">
          <h4>Validation Loss</h4>
          <div className={`value ${overfittingAnalysis.indicators.valLossIncreasing ? 'alert' : ''}`}>
            {overfittingAnalysis.indicators.valLossIncreasing ? '↗ Increasing' : '↘ Decreasing'}
          </div>
        </div>

        <div className="indicator-card">
          <h4>Variance</h4>
          <div className={`value ${overfittingAnalysis.indicators.variance > 0.1 ? 'alert' : ''}`}>
            {(overfittingAnalysis.indicators.variance * 100).toFixed(2)}%
          </div>
          <div className="threshold">Low variance is good</div>
        </div>

        <div className="indicator-card">
          <h4>Early Stopping</h4>
          <div className={`value ${overfittingAnalysis.indicators.earlyStopTriggered ? 'alert' : ''}`}>
            {overfittingAnalysis.indicators.earlyStopTriggered ? 'Triggered' : 'Not Triggered'}
          </div>
        </div>
      </div>
    </div>
  );
}

function OverfittingGauge({ score }: { score: number }) {
  const getColor = (s: number) => {
    if (s < 20) return '#4ade80'; // Green - Good
    if (s < 40) return '#facc15'; // Yellow - Mild
    if (s < 60) return '#f97316'; // Orange - Moderate
    return '#ef4444'; // Red - Severe
  };

  const getLabel = (s: number) => {
    if (s < 20) return 'Well-Fit';
    if (s < 40) return 'Mild';
    if (s < 60) return 'Moderate';
    return 'Severe';
  };

  return (
    <div className="gauge">
      <svg width="200" height="120" viewBox="0 0 200 120">
        {/* Background arc */}
        <path
          d="M20,100 A80,80 0 0,1 180,100"
          fill="none"
          stroke="#e5e7eb"
          strokeWidth="20"
        />

        {/* Score arc */}
        <path
          d={`M20,100 A80,80 0 0,1 ${20 + (score / 100) * 160},${100 - Math.sin((score / 100) * Math.PI) * 80}`}
          fill="none"
          stroke={getColor(score)}
          strokeWidth="20"
        />

        {/* Score text */}
        <text x="100" y="90" textAnchor="middle" fontSize="32" fontWeight="bold">
          {score.toFixed(0)}
        </text>
        <text x="100" y="110" textAnchor="middle" fontSize="14">
          {getLabel(score)}
        </text>
      </svg>
    </div>
  );
}
```

---

## 7. Implementation Steps

### Step 1: Backend Setup (Week 1)

1. **Create ML Controller**
   ```bash
   cd backend/AlgoTrendy.API/Controllers
   touch MLTrainingController.cs
   ```

2. **Create DTOs**
   ```bash
   cd backend/AlgoTrendy.API/DTOs
   touch MLTrainingDtos.cs
   ```

3. **Create Services**
   ```bash
   cd backend/AlgoTrendy.Infrastructure/Services
   touch MLTrainingService.cs
   touch DriftDetectionService.cs
   touch OverfittingDetectionService.cs
   ```

4. **Register Services in Program.cs**
   ```csharp
   builder.Services.AddScoped<IMLTrainingService, MLTrainingService>();
   builder.Services.AddScoped<IDriftDetectionService, DriftDetectionService>();
   builder.Services.AddHostedService<DriftMonitoringBackgroundService>();
   ```

5. **Create SignalR Hub**
   ```bash
   cd backend/AlgoTrendy.API/Hubs
   touch MLTrainingHub.cs
   ```

### Step 2: Python Bridge (Week 1)

1. **Create Python API wrapper**
   ```bash
   cd /root/AlgoTrendy_v2.6
   touch ml_api_server.py
   ```

2. **Implement endpoints**
   - `/train` - Start training job
   - `/predict` - Get predictions
   - `/drift` - Calculate drift metrics
   - `/overfitting` - Analyze overfitting

3. **Start Python API**
   ```bash
   python3 ml_api_server.py
   # Runs on http://localhost:5050
   ```

### Step 3: Frontend Components (Week 2)

1. **Create page structure**
   ```bash
   cd frontend/src/pages
   mkdir MLTraining
   cd MLTraining
   touch MLTrainingDashboard.tsx
   mkdir components hooks services types utils
   ```

2. **Implement core components**
   - ModelList.tsx
   - TrainingVisualizer.tsx
   - DriftDetector.tsx
   - OverfittingMonitor.tsx

3. **Set up SignalR connection**
   ```typescript
   // services/signalRService.ts
   import * as signalR from '@microsoft/signalr';

   const connection = new signalR.HubConnectionBuilder()
     .withUrl("http://localhost:5000/hubs/mltraining")
     .build();
   ```

### Step 4: Integration Testing (Week 3)

1. **Test training pipeline**
   - Start training job via UI
   - Verify real-time updates
   - Check completion callback

2. **Test drift detection**
   - Simulate production data
   - Verify drift alerts
   - Test retraining trigger

3. **Test overfitting detection**
   - Train deliberately overfitted model
   - Verify detection and alerts
   - Test recommendations

### Step 5: Deployment (Week 4)

1. **Configure environment**
   ```bash
   # appsettings.json
   {
     "MLTraining": {
       "PythonApiUrl": "http://localhost:5050",
       "DriftCheckInterval": "1h",
       "DriftThreshold": 0.25,
       "RetrainThreshold": 0.30
     }
   }
   ```

2. **Deploy to production**
   ```bash
   dotnet publish -c Release
   docker-compose up -d
   ```

3. **Monitor and iterate**

---

## 8. Quick Start Checklist

- [ ] Backend ML Controller created
- [ ] DTOs defined for all endpoints
- [ ] Python ML API server running
- [ ] SignalR hub configured
- [ ] Frontend page structure created
- [ ] TrainingVisualizer component implemented
- [ ] DriftDetector component implemented
- [ ] OverfittingMonitor component implemented
- [ ] Real-time updates working via SignalR
- [ ] Integration tests passing
- [ ] Documentation complete
- [ ] Deployed to staging environment

---

## 9. API Endpoint Summary

**Base URL**: `http://localhost:5000/api/mltraining`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/models` | List all ML models |
| GET | `/models/{id}` | Get model details |
| GET | `/metrics/{id}` | Get model metrics |
| POST | `/train` | Start training job |
| GET | `/training/{jobId}` | Get training status |
| GET | `/drift/{id}` | Get drift metrics |
| GET | `/performance/{id}` | Get performance history |
| GET | `/patterns` | Get latest patterns |
| POST | `/predict` | Get predictions |

**SignalR Hub**: `ws://localhost:5000/hubs/mltraining`

---

## 10. Monitoring & Alerts

### Drift Alerts
- Email notification when drift > 0.25
- Slack webhook integration
- Dashboard alert banner
- Automatic retraining trigger

### Performance Monitoring
- Track accuracy over time
- Alert on 5% degradation
- Weekly performance report
- Model comparison dashboard

### System Health
- Training job queue status
- GPU/CPU utilization
- API response times
- Error rates

---

**Last Updated**: October 20, 2025
**Author**: Claude Code - AlgoTrendy Development Team
**Status**: Ready for Implementation
