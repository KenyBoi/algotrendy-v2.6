# ML Model Training Report

**Version**: 20251020_215713
**Date**: 2025-10-20 21:58:33
**Best Model**: adaboost

## Model Comparison

| Model | Val Accuracy | Val Precision | Val Recall | Val F1 | Overfit Score |
|-------|--------------|---------------|------------|--------|---------------|
| gradient_boosting | 99.9% | 99.5% | 99.0% | 99.2% | +0.1% |
| random_forest | 97.7% | 99.6% | 69.1% | 81.6% | +2.3% |
| adaboost | 100.0% | 99.7% | 100.0% | 99.9% | +0.0% |
| logistic_regression | 45.3% | 8.2% | 63.1% | 14.6% | +12.8% |

## Best Model Details

- **Accuracy**: 100.0%
- **Precision**: 99.7%
- **Recall**: 100.0%
- **F1-Score**: 99.9%

### Confusion Matrix

```
              Predicted
              0      1
Actual  0    4789      1
        1       0    382
```
