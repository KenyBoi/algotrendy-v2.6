#!/usr/bin/env python3
"""
ML Prediction Service - Trend Reversal Prediction Service
Provides ML-based predictions for trend reversals using trained GradientBoosting model

Usage:
  python ml_prediction_service.py

Endpoints:
  GET /health - Health check with model info
  POST /predict/reversal - Make reversal predictions (accepts features array)
  GET /model/info - Get model configuration and metadata

Install: pip install flask joblib scikit-learn numpy
"""

import joblib
import json
import numpy as np
from pathlib import Path
from typing import Dict, List, Optional, Any
from flask import Flask, request, jsonify
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

app = Flask(__name__)

# Model configuration
MODEL_BASE_PATH = Path("/root/AlgoTrendy_v2.6/ml_models/trend_reversals/20251016_234123")
MODEL_FILE = "reversal_model.joblib"
SCALER_FILE = "reversal_scaler.joblib"
CONFIG_FILE = "config.json"


class MLPredictionService:
    """Service for ML-based trend reversal predictions"""

    def __init__(self, model_path: Path):
        """
        Initialize the ML prediction service

        Args:
            model_path: Path to the model directory containing model files
        """
        self.model_path = model_path
        self.model = None
        self.scaler = None
        self.config = None
        self.feature_names = []
        self._load_model()

    def _load_model(self) -> None:
        """
        Load the trained model, scaler, and configuration

        Raises:
            FileNotFoundError: If model files are not found
            Exception: If model loading fails
        """
        try:
            # Load model
            model_file = self.model_path / MODEL_FILE
            if not model_file.exists():
                raise FileNotFoundError(f"Model file not found: {model_file}")

            logger.info(f"Loading model from {model_file}")
            self.model = joblib.load(model_file)

            # Load scaler
            scaler_file = self.model_path / SCALER_FILE
            if not scaler_file.exists():
                raise FileNotFoundError(f"Scaler file not found: {scaler_file}")

            logger.info(f"Loading scaler from {scaler_file}")
            self.scaler = joblib.load(scaler_file)

            # Load config
            config_file = self.model_path / CONFIG_FILE
            if not config_file.exists():
                raise FileNotFoundError(f"Config file not found: {config_file}")

            logger.info(f"Loading config from {config_file}")
            with open(config_file, 'r') as f:
                self.config = json.load(f)

            self.feature_names = self.config.get('features', [])
            logger.info(f"Model loaded successfully. Features: {len(self.feature_names)}")

        except FileNotFoundError as e:
            logger.error(f"Model file not found: {str(e)}")
            raise
        except Exception as e:
            logger.error(f"Error loading model: {str(e)}")
            raise

    def predict_reversal(
        self,
        features: List[float],
        feature_dict: Optional[Dict[str, float]] = None
    ) -> Dict:
        """
        Predict trend reversal based on input features

        Args:
            features: List of feature values (must match config feature order)
            feature_dict: Optional dict mapping feature names to values

        Returns:
            Dict containing:
                - prediction: 0 (no reversal) or 1 (reversal)
                - confidence: float between 0 and 1
                - probabilities: dict with prob_no_reversal and prob_reversal
                - features_used: list of feature names

        Raises:
            ValueError: If feature count doesn't match expected count
        """
        try:
            # Use feature_dict if provided, otherwise use features list
            if feature_dict is not None:
                logger.info(f"Processing feature dictionary with {len(feature_dict)} features")
                # Convert dict to list in correct order
                features = [feature_dict.get(name, 0.0) for name in self.feature_names]

            # Validate feature count
            expected_features = len(self.feature_names)
            if len(features) != expected_features:
                raise ValueError(
                    f"Expected {expected_features} features, got {len(features)}. "
                    f"Required features: {self.feature_names}"
                )

            logger.info(f"Making prediction with {len(features)} features")

            # Convert to numpy array and reshape
            features_array = np.array(features).reshape(1, -1)

            # Scale features
            features_scaled = self.scaler.transform(features_array)

            # Make prediction
            prediction = int(self.model.predict(features_scaled)[0])

            # Get prediction probabilities
            probabilities = self.model.predict_proba(features_scaled)[0]

            # Calculate confidence (max probability)
            confidence = float(max(probabilities))

            result = {
                "prediction": prediction,
                "confidence": confidence,
                "probabilities": {
                    "prob_no_reversal": float(probabilities[0]),
                    "prob_reversal": float(probabilities[1])
                },
                "features_used": self.feature_names,
                "feature_count": len(features)
            }

            logger.info(
                f"Prediction: {prediction}, Confidence: {confidence:.4f}, "
                f"Prob[No Reversal]: {probabilities[0]:.4f}, Prob[Reversal]: {probabilities[1]:.4f}"
            )

            return result

        except ValueError as e:
            logger.error(f"Validation error: {str(e)}")
            return {"error": str(e)}
        except Exception as e:
            logger.error(f"Error making prediction: {str(e)}")
            return {"error": f"Prediction failed: {str(e)}"}

    def get_model_info(self) -> Dict:
        """
        Get model configuration and metadata

        Returns:
            Dict with model type, features, metrics, and configuration
        """
        try:
            if self.config is None:
                return {"error": "Model configuration not loaded"}

            info = {
                "model_type": self.config.get('model_type', 'Unknown'),
                "timestamp": self.config.get('timestamp', 'Unknown'),
                "features": self.feature_names,
                "feature_count": len(self.feature_names),
                "metrics": self.config.get('metrics', {}),
                "feature_importance": self.config.get('feature_importance', [])[:10],  # Top 10
                "model_path": str(self.model_path)
            }

            return info

        except Exception as e:
            logger.error(f"Error getting model info: {str(e)}")
            return {"error": str(e)}

    def get_health_status(self) -> Dict:
        """
        Get service health status

        Returns:
            Dict with service status and model info
        """
        try:
            status = {
                "status": "healthy",
                "service": "ml_prediction",
                "version": "1.0",
                "model_loaded": self.model is not None,
                "scaler_loaded": self.scaler is not None,
                "config_loaded": self.config is not None
            }

            if self.config:
                status["model_type"] = self.config.get('model_type', 'Unknown')
                status["feature_count"] = len(self.feature_names)
                status["model_timestamp"] = self.config.get('timestamp', 'Unknown')

            return status

        except Exception as e:
            logger.error(f"Error checking health status: {str(e)}")
            return {
                "status": "unhealthy",
                "error": str(e)
            }


# Initialize service
try:
    service = MLPredictionService(MODEL_BASE_PATH)
    logger.info("ML Prediction Service initialized successfully")
except Exception as e:
    logger.error(f"Failed to initialize service: {str(e)}")
    service = None


# Flask routes
@app.route('/health', methods=['GET'])
def health():
    """
    Health check endpoint

    Returns:
        JSON response with service status and model info
    """
    if service is None:
        return jsonify({
            "status": "unhealthy",
            "error": "Service not initialized"
        }), 503

    result = service.get_health_status()
    status_code = 200 if result.get("status") == "healthy" else 503
    return jsonify(result), status_code


@app.route('/predict/reversal', methods=['POST'])
def predict_reversal():
    """
    Make trend reversal prediction

    Expected JSON body:
        {
            "features": [0.1, 0.2, ...],  // Array of 21 feature values
            OR
            "feature_dict": {              // Dict mapping feature names to values
                "volatility_3": 0.1,
                "volume_ratio": 1.5,
                ...
            }
        }

    Returns:
        JSON response with prediction, confidence, and probabilities
    """
    if service is None:
        return jsonify({
            "error": "Service not initialized"
        }), 503

    try:
        data = request.get_json()

        if not data:
            return jsonify({
                "error": "Request body is required (JSON with 'features' or 'feature_dict')"
            }), 400

        # Extract features from request
        features = data.get('features')
        feature_dict = data.get('feature_dict')

        if features is None and feature_dict is None:
            return jsonify({
                "error": "Either 'features' array or 'feature_dict' is required",
                "expected_features": service.feature_names,
                "expected_count": len(service.feature_names)
            }), 400

        # Make prediction
        result = service.predict_reversal(features, feature_dict)

        # Check if error occurred
        if "error" in result:
            return jsonify(result), 400

        return jsonify(result), 200

    except Exception as e:
        logger.error(f"Error in predict endpoint: {str(e)}")
        return jsonify({"error": str(e)}), 500


@app.route('/model/info', methods=['GET'])
def model_info():
    """
    Get model configuration and metadata

    Returns:
        JSON response with model type, features, metrics, and importance
    """
    if service is None:
        return jsonify({
            "error": "Service not initialized"
        }), 503

    result = service.get_model_info()

    if "error" in result:
        return jsonify(result), 500

    return jsonify(result), 200


if __name__ == "__main__":
    if service is None:
        logger.error("Cannot start server: Service initialization failed")
        exit(1)

    logger.info("Starting ML Prediction service on port 5003")
    app.run(host='0.0.0.0', port=5003, debug=False)
