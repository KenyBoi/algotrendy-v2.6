#!/usr/bin/env python3
"""
AlgoTrendy ML Prediction Microservice
Flask-based API for machine learning predictions
Provides reversal predictions for trading strategies
"""

from flask import Flask, request, jsonify
import numpy as np
import logging
from datetime import datetime
import sys

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] %(levelname)s - %(name)s: %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler('ml-service.log')
    ]
)

logger = logging.getLogger(__name__)

app = Flask(__name__)

# Mock ML model for now - will be replaced with actual trained model
class MockMLModel:
    """
    Mock ML model for testing integration
    Will be replaced with actual trained model from v2.5
    """
    def __init__(self):
        logger.info("Initializing Mock ML Model")
        self.feature_count = 21

    def predict(self, features):
        """
        Mock prediction based on feature values

        Args:
            features: Array of 21 technical indicators

        Returns:
            dict with is_reversal, confidence, and probabilities
        """
        if len(features) != self.feature_count:
            raise ValueError(f"Expected {self.feature_count} features, got {len(features)}")

        # Mock logic: check if RSI (first 4 features) indicates reversal
        rsi_14 = features[0]
        rsi_7 = features[1]

        # Oversold condition (potential reversal up)
        if rsi_14 < 35 and rsi_7 < 30:
            is_reversal = True
            confidence = 0.75 + (30 - rsi_7) / 100  # Higher confidence for lower RSI
        # Overbought condition (potential reversal down)
        elif rsi_14 > 65 and rsi_7 > 70:
            is_reversal = True
            confidence = 0.75 + (rsi_7 - 70) / 100  # Higher confidence for higher RSI
        else:
            is_reversal = False
            confidence = 0.45 + np.random.random() * 0.15  # Random confidence between 0.45-0.60

        # Cap confidence
        confidence = min(confidence, 0.95)

        return {
            'is_reversal': bool(is_reversal),
            'confidence': float(confidence),
            'probabilities': {
                'no_reversal': float(1 - confidence if is_reversal else confidence),
                'reversal_up': float(confidence / 2 if is_reversal and rsi_14 < 50 else 0.2),
                'reversal_down': float(confidence / 2 if is_reversal and rsi_14 > 50 else 0.2)
            }
        }


# Initialize model
ml_model = MockMLModel()


@app.route('/health', methods=['GET'])
def health():
    """
    Health check endpoint
    """
    return jsonify({
        'status': 'healthy',
        'service': 'AlgoTrendy ML Prediction Service',
        'version': '1.0.0',
        'timestamp': datetime.utcnow().isoformat()
    }), 200


@app.route('/predict/reversal', methods=['POST'])
def predict_reversal():
    """
    Predict potential price reversal based on technical indicators

    Request body:
    {
        "features": [21 technical indicator values]
    }

    Response:
    {
        "is_reversal": bool,
        "confidence": float (0-1),
        "probabilities": {
            "no_reversal": float,
            "reversal_up": float,
            "reversal_down": float
        },
        "timestamp": ISO datetime string
    }
    """
    try:
        data = request.get_json()

        if not data or 'features' not in data:
            logger.warning("Missing 'features' in request body")
            return jsonify({
                'error': "Missing 'features' in request body",
                'timestamp': datetime.utcnow().isoformat()
            }), 400

        features = data['features']

        if not isinstance(features, list):
            logger.warning(f"Invalid features type: {type(features)}")
            return jsonify({
                'error': "Features must be an array",
                'timestamp': datetime.utcnow().isoformat()
            }), 400

        if len(features) != 21:
            logger.warning(f"Invalid feature count: {len(features)}")
            return jsonify({
                'error': f"Expected 21 features, got {len(features)}",
                'timestamp': datetime.utcnow().isoformat()
            }), 400

        # Convert to numpy array
        features_array = np.array(features, dtype=float)

        # Log request
        logger.info(f"Predicting reversal with features: RSI_14={features_array[0]:.2f}, "
                   f"RSI_7={features_array[1]:.2f}, MACD={features_array[4]:.2f}")

        # Make prediction
        prediction = ml_model.predict(features_array)
        prediction['timestamp'] = datetime.utcnow().isoformat()

        # Log response
        logger.info(f"Prediction: is_reversal={prediction['is_reversal']}, "
                   f"confidence={prediction['confidence']:.3f}")

        return jsonify(prediction), 200

    except ValueError as e:
        logger.error(f"Validation error: {e}")
        return jsonify({
            'error': str(e),
            'timestamp': datetime.utcnow().isoformat()
        }), 400
    except Exception as e:
        logger.error(f"Prediction error: {e}", exc_info=True)
        return jsonify({
            'error': 'Internal server error',
            'details': str(e),
            'timestamp': datetime.utcnow().isoformat()
        }), 500


@app.route('/model/info', methods=['GET'])
def model_info():
    """
    Get information about the ML model
    """
    return jsonify({
        'model_type': 'Mock ML Model (for testing)',
        'feature_count': ml_model.feature_count,
        'features': [
            'RSI_14', 'RSI_7', 'RSI_21', 'RSI_28',
            'MACD_Value', 'MACD_Signal', 'MACD_Histogram',
            'BB_Upper', 'BB_Middle', 'BB_Lower',
            'ATR',
            'Stochastic_K', 'Stochastic_D',
            'SMA_20', 'EMA_20',
            'Volume_Ratio',
            'ROC',
            'MFI',
            'CCI',
            'Williams_R',
            'Price_Position'
        ],
        'version': '1.0.0',
        'status': 'active'
    }), 200


if __name__ == '__main__':
    logger.info("=" * 60)
    logger.info("Starting AlgoTrendy ML Prediction Service")
    logger.info("=" * 60)
    logger.info(f"Model type: {ml_model.__class__.__name__}")
    logger.info(f"Expected features: {ml_model.feature_count}")
    logger.info(f"Listening on: http://0.0.0.0:5003")
    logger.info("=" * 60)

    app.run(
        host='0.0.0.0',
        port=5003,
        debug=False  # Set to True for development
    )
