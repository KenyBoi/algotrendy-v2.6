# AlgoTrendy ML Prediction Service

Flask-based microservice for machine learning predictions.

## Features

- Reversal prediction based on 21 technical indicators
- Health check endpoint
- Model information endpoint
- Logging to file and stdout

## Endpoints

### GET /health
Health check endpoint

### POST /predict/reversal
Predict potential price reversal

Request body:
```json
{
  "features": [21 technical indicator values]
}
```

Response:
```json
{
  "is_reversal": true,
  "confidence": 0.85,
  "probabilities": {
    "no_reversal": 0.15,
    "reversal_up": 0.425,
    "reversal_down": 0.425
  },
  "timestamp": "2025-10-19T12:34:56.789Z"
}
```

### GET /model/info
Get information about the ML model

## Running Locally

```bash
# Install dependencies
pip install -r requirements.txt

# Run the service
python app.py
```

Service will be available at: http://localhost:5003

## Running with Docker

```bash
# Build image
docker build -t algotrendy-ml-service .

# Run container
docker run -d -p 5003:5003 --name ml-service algotrendy-ml-service
```

## Integration with AlgoTrendy v2.6

The service is automatically integrated when using docker-compose:

```bash
cd /root/AlgoTrendy_v2.6
docker-compose up ml-service
```

The C# backend will communicate with this service via HTTP at `http://ml-service:5003`.

## Future Enhancements

- Replace MockMLModel with actual trained model from v2.5
- Add model versioning
- Add caching for predictions
- Add metrics and monitoring
- Add authentication/API keys
