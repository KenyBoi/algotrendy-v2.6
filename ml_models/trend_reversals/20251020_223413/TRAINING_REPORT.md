# ML Model Training Report

**Version**: 20251020_223413
**Date**: 2025-10-20 22:35:34
**Best Model**: adaboost

## Model Comparison

| Model | Val Accuracy | Val Precision | Val Recall | Val F1 | Overfit Score |
|-------|--------------|---------------|------------|--------|---------------|
| gradient_boosting | 99.7% | 97.9% | 98.7% | 98.3% | +0.2% |
| random_forest | 97.6% | 99.6% | 68.2% | 81.0% | +2.3% |
| adaboost | 100.0% | 99.7% | 100.0% | 99.9% | +0.0% |
| logistic_regression | 52.3% | 8.2% | 54.1% | 14.3% | +20.0% |

## Best Model Details

- **Accuracy**: 100.0%
- **Precision**: 99.7%
- **Recall**: 100.0%
- **F1-Score**: 99.9%

### Confusion Matrix

```
              Predicted
              0      1
Actual  0    4798      1
        1       0    381
```
