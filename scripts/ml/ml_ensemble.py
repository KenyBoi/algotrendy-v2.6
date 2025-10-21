#!/usr/bin/env python3
"""
ML Ensemble Prediction System
Combines predictions from multiple models (XGBoost, LSTM, traditional ML) for robust predictions
"""

import warnings
warnings.filterwarnings('ignore')

import numpy as np
import joblib
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from datetime import datetime

# ML models
try:
    import xgboost as xgb
    XGBOOST_AVAILABLE = True
except ImportError:
    XGBOOST_AVAILABLE = False

try:
    from tensorflow import keras
    TENSORFLOW_AVAILABLE = True
except ImportError:
    TENSORFLOW_AVAILABLE = False


class EnsemblePredictor:
    """
    Ensemble prediction system that combines multiple models

    Strategies:
    - Voting: Simple majority vote
    - Weighted: Weighted average based on validation performance
    - Stacking: Meta-model trained on base model predictions
    - Confidence-based: Weight by prediction confidence
    """

    def __init__(self, model_dir: Path):
        self.model_dir = Path(model_dir)
        self.models = {}
        self.weights = {}
        self.scalers = {}
        self.config = {}

    def load_models(self, model_version: str = "latest"):
        """Load all available models from a version directory"""
        print("\n" + "="*70)
        print("📦 LOADING ENSEMBLE MODELS")
        print("="*70)

        version_dir = self.model_dir / model_version
        if not version_dir.exists():
            raise FileNotFoundError(f"Model version {model_version} not found")

        # Load config
        config_path = version_dir / "config.json"
        if config_path.exists():
            import json
            with open(config_path) as f:
                self.config = json.load(f)

        # Load scalers
        scaler_path = version_dir / "scalers.joblib"
        if scaler_path.exists():
            self.scalers = joblib.load(scaler_path)
            print(f"  ✅ Loaded scalers: {list(self.scalers.keys())}")

        # Load XGBoost
        xgb_path = version_dir / "xgboost_model.joblib"
        if xgb_path.exists() and XGBOOST_AVAILABLE:
            self.models['xgboost'] = joblib.load(xgb_path)
            print(f"  ✅ Loaded XGBoost model")

        # Load LSTM
        lstm_path = version_dir / "lstm_model.h5"
        if lstm_path.exists() and TENSORFLOW_AVAILABLE:
            self.models['lstm'] = keras.models.load_model(lstm_path)
            print(f"  ✅ Loaded LSTM model")

        # Load traditional models (AdaBoost, GradientBoosting, etc.)
        for model_name in ['adaboost', 'gradient_boosting', 'random_forest']:
            model_path = version_dir / f"{model_name}_model.joblib"
            if model_path.exists():
                self.models[model_name] = joblib.load(model_path)
                print(f"  ✅ Loaded {model_name} model")

        # Load metrics for weighting
        metrics_path = version_dir / "metrics.json"
        if metrics_path.exists():
            import json
            with open(metrics_path) as f:
                metrics = json.load(f)

            # Calculate weights based on validation accuracy
            total_acc = 0
            for model_name in self.models.keys():
                if model_name in metrics:
                    acc = metrics[model_name].get('val_accuracy', 0)
                    self.weights[model_name] = acc
                    total_acc += acc

            # Normalize weights
            if total_acc > 0:
                for model_name in self.weights:
                    self.weights[model_name] /= total_acc

                print(f"\n  📊 Model Weights (based on validation accuracy):")
                for model_name, weight in self.weights.items():
                    print(f"     {model_name}: {weight:.3f}")

        if not self.models:
            raise ValueError("No models loaded!")

        print(f"\n  ✅ Total models loaded: {len(self.models)}")
        return self

    def predict_voting(self, X: np.ndarray) -> Tuple[int, float]:
        """
        Simple majority voting

        Args:
            X: Feature array (scaled)

        Returns:
            (prediction, confidence)
        """
        predictions = []

        for model_name, model in self.models.items():
            if model_name == 'lstm':
                # LSTM requires sequence input, skip for now
                # In production, you'd maintain a sequence buffer
                continue

            pred = model.predict(X)[0]
            predictions.append(pred)

        if not predictions:
            raise ValueError("No predictions generated")

        # Majority vote
        vote_counts = np.bincount(predictions)
        prediction = np.argmax(vote_counts)
        confidence = vote_counts[prediction] / len(predictions)

        return int(prediction), float(confidence)

    def predict_weighted(self, X: np.ndarray) -> Tuple[int, float]:
        """
        Weighted voting based on model validation performance

        Args:
            X: Feature array (scaled)

        Returns:
            (prediction, confidence)
        """
        weighted_proba = np.zeros(2)  # Binary classification
        total_weight = 0

        for model_name, model in self.models.items():
            if model_name == 'lstm':
                # LSTM requires sequence input, skip for now
                continue

            if model_name not in self.weights:
                continue

            weight = self.weights[model_name]

            # Get probability predictions
            if hasattr(model, 'predict_proba'):
                proba = model.predict_proba(X)[0]
            else:
                # For models without predict_proba, use hard predictions
                pred = model.predict(X)[0]
                proba = np.array([1 - pred, pred])

            weighted_proba += weight * proba
            total_weight += weight

        if total_weight == 0:
            raise ValueError("No weighted predictions generated")

        # Normalize
        weighted_proba /= total_weight

        prediction = np.argmax(weighted_proba)
        confidence = weighted_proba[prediction]

        return int(prediction), float(confidence)

    def predict_confidence_based(self, X: np.ndarray, min_confidence: float = 0.7) -> Dict:
        """
        Make predictions using only high-confidence models

        Args:
            X: Feature array (scaled)
            min_confidence: Minimum confidence threshold

        Returns:
            Dict with prediction and detailed confidence breakdown
        """
        model_predictions = {}

        for model_name, model in self.models.items():
            if model_name == 'lstm':
                continue

            # Get probability prediction
            if hasattr(model, 'predict_proba'):
                proba = model.predict_proba(X)[0]
                pred = np.argmax(proba)
                confidence = proba[pred]
            else:
                pred = model.predict(X)[0]
                confidence = 0.5  # Unknown confidence

            model_predictions[model_name] = {
                'prediction': int(pred),
                'confidence': float(confidence),
                'weight': self.weights.get(model_name, 0),
                'used': confidence >= min_confidence
            }

        # Weighted vote using only high-confidence predictions
        weighted_sum = 0
        total_weight = 0

        for model_name, pred_info in model_predictions.items():
            if pred_info['used']:
                weighted_sum += pred_info['prediction'] * pred_info['weight'] * pred_info['confidence']
                total_weight += pred_info['weight'] * pred_info['confidence']

        if total_weight == 0:
            # No high-confidence predictions, fall back to weighted average
            return self._fallback_prediction(X, model_predictions)

        final_prediction = int(round(weighted_sum / total_weight))
        final_confidence = total_weight / sum(
            p['weight'] * p['confidence'] for p in model_predictions.values()
        )

        return {
            'prediction': final_prediction,
            'confidence': float(final_confidence),
            'model_breakdown': model_predictions,
            'high_confidence_models': sum(1 for p in model_predictions.values() if p['used']),
            'total_models': len(model_predictions)
        }

    def _fallback_prediction(self, X: np.ndarray, model_predictions: Dict) -> Dict:
        """Fallback when no high-confidence predictions"""
        # Use weighted average of all predictions
        weighted_sum = sum(
            p['prediction'] * p['weight']
            for p in model_predictions.values()
        )
        total_weight = sum(p['weight'] for p in model_predictions.values())

        final_prediction = int(round(weighted_sum / total_weight)) if total_weight > 0 else 0

        return {
            'prediction': final_prediction,
            'confidence': 0.5,  # Low confidence
            'model_breakdown': model_predictions,
            'high_confidence_models': 0,
            'total_models': len(model_predictions),
            'fallback': True
        }

    def predict_ensemble(self, X: np.ndarray, strategy: str = 'weighted') -> Dict:
        """
        Main prediction method with multiple strategies

        Args:
            X: Raw feature array (will be scaled internally)
            strategy: 'voting', 'weighted', or 'confidence'

        Returns:
            Dict with prediction and metadata
        """
        # Scale features
        scaler = self.scalers.get('standard')
        if scaler is None:
            raise ValueError("Scaler not loaded")

        X_scaled = scaler.transform(X.reshape(1, -1) if X.ndim == 1 else X)

        # Make prediction based on strategy
        if strategy == 'voting':
            prediction, confidence = self.predict_voting(X_scaled)
            return {
                'prediction': prediction,
                'confidence': confidence,
                'strategy': 'voting',
                'timestamp': datetime.now().isoformat()
            }

        elif strategy == 'weighted':
            prediction, confidence = self.predict_weighted(X_scaled)
            return {
                'prediction': prediction,
                'confidence': confidence,
                'strategy': 'weighted',
                'timestamp': datetime.now().isoformat()
            }

        elif strategy == 'confidence':
            result = self.predict_confidence_based(X_scaled)
            result['strategy'] = 'confidence'
            result['timestamp'] = datetime.now().isoformat()
            return result

        else:
            raise ValueError(f"Unknown strategy: {strategy}")

    def batch_predict(self, X_batch: np.ndarray, strategy: str = 'weighted') -> List[Dict]:
        """Make predictions for a batch of samples"""
        predictions = []
        for X in X_batch:
            pred = self.predict_ensemble(X, strategy=strategy)
            predictions.append(pred)
        return predictions

    def evaluate_ensemble(self, X_test: np.ndarray, y_test: np.ndarray,
                         strategy: str = 'weighted') -> Dict:
        """
        Evaluate ensemble performance on test data

        Args:
            X_test: Test features
            y_test: Test labels
            strategy: Prediction strategy to use

        Returns:
            Dict with evaluation metrics
        """
        predictions = self.batch_predict(X_test, strategy=strategy)
        y_pred = np.array([p['prediction'] for p in predictions])
        confidences = np.array([p['confidence'] for p in predictions])

        # Calculate metrics
        from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score

        accuracy = accuracy_score(y_test, y_pred)
        precision = precision_score(y_test, y_pred, zero_division=0)
        recall = recall_score(y_test, y_pred, zero_division=0)
        f1 = f1_score(y_test, y_pred, zero_division=0)

        # Calculate confidence statistics
        avg_confidence = np.mean(confidences)
        correct_high_conf = np.mean(confidences[y_pred == y_test])
        incorrect_high_conf = np.mean(confidences[y_pred != y_test]) if (y_pred != y_test).any() else 0

        return {
            'accuracy': float(accuracy),
            'precision': float(precision),
            'recall': float(recall),
            'f1_score': float(f1),
            'avg_confidence': float(avg_confidence),
            'correct_confidence': float(correct_high_conf),
            'incorrect_confidence': float(incorrect_high_conf),
            'strategy': strategy,
            'test_samples': len(y_test)
        }


if __name__ == "__main__":
    print("\n" + "="*70)
    print("🎯 ENSEMBLE PREDICTION SYSTEM")
    print("="*70)

    print("\nFeatures:")
    print("- Voting: Simple majority vote")
    print("- Weighted: Performance-based weighting")
    print("- Confidence: Use only high-confidence predictions")
    print("\nSupported Models:")
    print("- XGBoost")
    print("- LSTM")
    print("- AdaBoost")
    print("- GradientBoosting")
    print("- RandomForest")

    print("\nUsage Example:")
    print("```python")
    print("ensemble = EnsemblePredictor('/path/to/models')")
    print("ensemble.load_models('latest')")
    print("prediction = ensemble.predict_ensemble(X, strategy='weighted')")
    print("```")
    print("="*70 + "\n")
