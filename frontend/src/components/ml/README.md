# ML Monitoring Dashboard Components

## Overview

Interactive React components for visualizing ML model performance, overfitting detection, and feature analysis using Plotly.js.

## Components

### MLMonitoringDashboard
Main dashboard component with tabs for different visualizations.

**Features:**
- Model performance overview
- Learning curves (overfitting detection)
- Feature importance analysis
- Model comparison
- Comprehensive overfitting dashboard

**Usage:**
```tsx
import MLMonitoringDashboard from './components/ml/MLMonitoringDashboard';

<MLMonitoringDashboard />
```

### PlotlyChart
Generic Plotly chart renderer that accepts figure JSON from the ML API.

**Props:**
- `data`: Plotly figure JSON (from API)
- `title?`: Optional chart title
- `height?`: Chart height in pixels (default: 500)

**Usage:**
```tsx
import PlotlyChart from './components/ml/PlotlyChart';

const vizData = await mlApi.getLearningCurves('latest');

<PlotlyChart data={vizData.visualization} title="Learning Curves" height={600} />
```

## Setup

### 1. Install Dependencies

```bash
cd frontend
npm install plotly.js-dist-min@^2.27.1
```

### 2. Update API Base URL

The ML API endpoints are configured in `src/lib/api.ts`:

```typescript
const API_BASE_URL = 'http://localhost:5050'; // ML API Server
```

Make sure the ML API server is running:

```bash
cd /root/AlgoTrendy_v2.6/scripts/ml
python3 ml_api_server.py
```

### 3. Start Frontend

```bash
npm run dev
```

Navigate to `/ml` to view the dashboard.

## API Integration

The dashboard uses these ML API endpoints:

- `GET /visualizations/learning-curves/{model_id}` - Overfitting detection
- `GET /visualizations/feature-importance/{model_id}` - Feature analysis
- `GET /visualizations/model-comparison` - Compare all models
- `GET /visualizations/training-history/{model_id}` - LSTM training history
- `GET /visualizations/overfitting-dashboard/{model_id}` - Full dashboard

See `src/lib/api.ts` for complete API client implementation.

## Features

### Overview Tab
- Model performance cards with accuracy, precision, recall, F1
- Overfitting status indicators (Excellent/Good/Poor)
- Training date and row count
- Overfitting detection guide

### Learning Curves Tab
- Interactive Plotly chart showing train vs. validation accuracy
- Automatic overfitting gap detection
- Hover tooltips with detailed metrics

### Feature Importance Tab
- Top 20 most important features
- Color-coded importance scores
- Sortable and interactive

### Model Comparison Tab
- Side-by-side comparison of all models
- Grouped bar charts for accuracy, precision, recall, F1
- Easy identification of best-performing model

### Overfitting Dashboard Tab
- Comprehensive 4-panel dashboard
- Learning curves, ROC curve, confusion matrix, overfitting gauge
- All visualizations in one view

## Troubleshooting

### Charts Not Rendering

1. **Check ML API Server**
   ```bash
   curl http://localhost:5050/health
   ```

2. **Check Browser Console**
   - Look for CORS errors
   - Verify API responses

3. **Verify Plotly Import**
   ```bash
   npm list plotly.js-dist-min
   ```

### API Connection Errors

1. **Update API URL in .env**
   ```env
   VITE_API_URL=http://localhost:5050
   ```

2. **Check CORS Settings**
   ML API server has CORS enabled by default

3. **Verify Model Exists**
   ```bash
   ls /root/AlgoTrendy_v2.6/ml_models/trend_reversals/
   ```

## Performance Tips

- Plotly is dynamically imported to reduce initial bundle size
- Charts are cached until model changes
- Auto-refresh disabled by default (enable in MLTrainingDashboard.tsx)

## Customization

### Change Chart Colors

Edit the Plotly figure in `ml_visualizations.py`:

```python
fig.update_layout(
    template='plotly_white',  # Try: plotly_dark, seaborn, ggplot2
    colorway=['#0066CC', '#FF6600', ...]
)
```

### Add New Visualization

1. Create new endpoint in `ml_api_server.py`
2. Add API method to `src/lib/api.ts`
3. Create new tab in `MLMonitoringDashboard.tsx`
4. Use `PlotlyChart` component to render

## Example: Custom Visualization

```tsx
// 1. Add API method
async getCustomViz(modelId: string) {
  const response = await axios.get(`${API_BASE_URL}/visualizations/custom/${modelId}`);
  return response.data;
}

// 2. Add to MLMonitoringDashboard
const [customViz, setCustomViz] = useState<any>(null);

useEffect(() => {
  if (activeTab === 'custom') {
    mlApi.getCustomViz(selectedModel).then(data => setCustomViz(data.visualization));
  }
}, [activeTab, selectedModel]);

// 3. Render in tab
{activeTab === 'custom' && (
  <PlotlyChart data={customViz} title="Custom Analysis" height={600} />
)}
```

## Support

For issues or questions:
- Check ML API documentation: `/docs/ml/ML_SYSTEM_DOCUMENTATION.md`
- Review API endpoints: `http://localhost:5050/docs`
- Check frontend logs: Browser console

---

**Version:** 2.6.0
**Last Updated:** October 21, 2025
