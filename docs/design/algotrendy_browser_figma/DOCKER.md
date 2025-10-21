# AlgoTrendy Frontend - Docker Deployment Guide

## Overview

The AlgoTrendy frontend uses a **runtime configuration system** that allows a single Docker image to work across all environments (development, staging, production) without rebuilding. Configuration is injected at container startup via environment variables.

## Architecture

### Multi-Stage Build

- **Stage 1 (Builder)**: Compiles the React/TypeScript application using Vite
- **Stage 2 (Production)**: Serves the built static files using nginx with runtime configuration

### Runtime Configuration Flow

1. Container starts with environment variables
2. `docker-entrypoint.sh` generates `/usr/share/nginx/html/env-config.js` with actual values
3. `index.html` loads `env-config.js` before the application bundle
4. Application reads `window.ENV` for API URLs and configuration
5. nginx starts and serves the application

## Building the Docker Image

### Build from Project Root

```bash
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma

# Build the production image
docker build -f src/Dockerfile.production -t algotrendy-frontend:latest .

# Build with version tag
docker build -f src/Dockerfile.production -t algotrendy-frontend:2.6.0 .
```

### Build Context

- **Build Context**: `/root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma/`
- **Dockerfile**: `src/Dockerfile.production`

The build requires these files from the build context:
- `package*.json` - Dependencies
- `src/` - Source code
- `public/` - Static assets
- `index.html` - Entry point
- `tsconfig.json` - TypeScript config
- `vite.config.ts` - Vite config
- `nginx.conf` - nginx configuration
- `docker-entrypoint.sh` - Runtime config script

## Running the Container

### Development Mode

Connect to local backend running on host:

```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=http://host.docker.internal:5002/api \
  -e WS_BASE_URL=ws://host.docker.internal:5002 \
  -e ENVIRONMENT=development \
  -e VERSION=2.6.0 \
  -e ENABLE_DEBUG=true \
  -p 3000:80 \
  algotrendy-frontend:latest
```

Access at: http://localhost:3000

### Production Mode

With reverse proxy (nginx/Traefik in front):

```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=/api \
  -e WS_BASE_URL= \
  -e ENVIRONMENT=production \
  -e VERSION=2.6.0 \
  -e ENABLE_DEBUG=false \
  -p 80:80 \
  algotrendy-frontend:latest
```

### Production Mode (Direct Backend Access)

Without reverse proxy:

```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=http://backend.yourdomain.com:5002/api \
  -e WS_BASE_URL=wss://backend.yourdomain.com \
  -e ENVIRONMENT=production \
  -e VERSION=2.6.0 \
  -e ENABLE_DEBUG=false \
  -p 80:80 \
  algotrendy-frontend:latest
```

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `API_BASE_URL` | No | `/api` | Backend API base URL |
| `WS_BASE_URL` | No | (auto-detect) | WebSocket base URL for SignalR |
| `ENVIRONMENT` | No | `production` | Environment name (development/staging/production) |
| `VERSION` | No | `2.6.0` | Application version |
| `ENABLE_DEBUG` | No | `false` | Enable debug logging |

### WebSocket URL Auto-Detection

If `WS_BASE_URL` is empty, it auto-detects:
- `wss://${window.location.host}` (uses current browser URL)

## Docker Compose

### Full Stack Deployment

```yaml
version: '3.8'

services:
  backend:
    image: algotrendy-backend:latest
    container_name: algotrendy-backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5002
    ports:
      - "5002:5002"
    networks:
      - algotrendy-network

  frontend:
    image: algotrendy-frontend:latest
    container_name: algotrendy-frontend
    environment:
      - API_BASE_URL=http://backend:5002/api
      - WS_BASE_URL=ws://backend:5002
      - ENVIRONMENT=production
      - VERSION=2.6.0
      - ENABLE_DEBUG=false
    ports:
      - "80:80"
    depends_on:
      - backend
    networks:
      - algotrendy-network

networks:
  algotrendy-network:
    driver: bridge
```

### With Reverse Proxy (nginx)

```yaml
version: '3.8'

services:
  backend:
    image: algotrendy-backend:latest
    container_name: algotrendy-backend
    networks:
      - algotrendy-network

  frontend:
    image: algotrendy-frontend:latest
    container_name: algotrendy-frontend
    environment:
      - API_BASE_URL=/api
      - WS_BASE_URL=
      - ENVIRONMENT=production
      - VERSION=2.6.0
      - ENABLE_DEBUG=false
    networks:
      - algotrendy-network

  nginx:
    image: nginx:alpine
    container_name: algotrendy-proxy
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx-proxy.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - frontend
      - backend
    networks:
      - algotrendy-network

networks:
  algotrendy-network:
    driver: bridge
```

## Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: algotrendy-frontend
  labels:
    app: algotrendy-frontend
spec:
  replicas: 3
  selector:
    matchLabels:
      app: algotrendy-frontend
  template:
    metadata:
      labels:
        app: algotrendy-frontend
    spec:
      containers:
      - name: frontend
        image: algotrendy-frontend:2.6.0
        ports:
        - containerPort: 80
        env:
        - name: API_BASE_URL
          value: "/api"
        - name: WS_BASE_URL
          value: ""
        - name: ENVIRONMENT
          value: "production"
        - name: VERSION
          value: "2.6.0"
        - name: ENABLE_DEBUG
          value: "false"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 3
          periodSeconds: 10
        resources:
          requests:
            memory: "128Mi"
            cpu: "100m"
          limits:
            memory: "256Mi"
            cpu: "200m"
---
apiVersion: v1
kind: Service
metadata:
  name: algotrendy-frontend
spec:
  selector:
    app: algotrendy-frontend
  ports:
  - protocol: TCP
    port: 80
    targetPort: 80
  type: LoadBalancer
```

## Health Checks

The container includes a health check endpoint:

```bash
# Check container health
curl http://localhost/health
# Response: healthy

# Docker health status
docker ps --filter name=algotrendy-frontend --format "{{.Status}}"
```

## Troubleshooting

### Verify Runtime Configuration

```bash
# View generated env-config.js
docker exec algotrendy-frontend cat /usr/share/nginx/html/env-config.js

# Expected output:
# window.ENV = {
#   API_BASE_URL: "/api",
#   WS_BASE_URL: "wss://${window.location.host}",
#   ENVIRONMENT: "production",
#   VERSION: "2.6.0",
#   ENABLE_DEBUG: false
# };
```

### View Container Logs

```bash
# View startup logs
docker logs algotrendy-frontend

# Expected output includes:
# 🚀 AlgoTrendy Frontend - Generating runtime configuration...
#   API_BASE_URL: /api
#   WS_BASE_URL: wss://${window.location.host}
#   ENVIRONMENT: production
#   VERSION: 2.6.0
#   ENABLE_DEBUG: false
# ✅ Runtime configuration generated successfully!
# 🌐 Starting nginx...
```

### Test API Connectivity

```bash
# From inside container
docker exec algotrendy-frontend wget -qO- http://localhost/health

# Test API proxy
curl http://localhost/api/health
```

### Debug Mode

Enable detailed browser console logs:

```bash
docker run -d \
  --name algotrendy-frontend-debug \
  -e ENABLE_DEBUG=true \
  -p 3000:80 \
  algotrendy-frontend:latest
```

Open browser console to see runtime config logs.

## File Structure

```
/root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma/
├── src/
│   ├── Dockerfile.production       # Multi-stage build
│   ├── config/
│   │   ├── api.ts                  # Runtime config reader
│   │   └── env.d.ts               # TypeScript definitions
│   └── ...
├── docker-entrypoint.sh           # Runtime config generator
├── nginx.conf                     # nginx server configuration
├── index.html                     # Loads env-config.js
├── package.json
└── .env.example                   # Environment variable documentation
```

## Security Considerations

1. **No secrets in image**: All configuration via environment variables
2. **Runtime injection**: Configuration loaded at startup, not build time
3. **nginx hardening**: Security headers configured in nginx.conf
4. **Health checks**: Kubernetes liveness/readiness probes
5. **Resource limits**: CPU and memory constraints in Kubernetes

## CI/CD Pipeline

### GitHub Actions Example

```yaml
name: Build and Push Frontend

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Build Docker image
        run: |
          cd docs/design/algotrendy_browser_figma
          docker build -f src/Dockerfile.production -t algotrendy-frontend:${{ github.sha }} .

      - name: Tag as latest
        run: docker tag algotrendy-frontend:${{ github.sha }} algotrendy-frontend:latest

      - name: Push to registry
        run: |
          echo "${{ secrets.DOCKER_PASSWORD }}" | docker login -u "${{ secrets.DOCKER_USERNAME }}" --password-stdin
          docker push algotrendy-frontend:${{ github.sha }}
          docker push algotrendy-frontend:latest
```

## Best Practices

1. **Single Image**: Build once, deploy everywhere
2. **12-Factor App**: Configuration via environment variables
3. **Immutable Infrastructure**: No runtime file modifications (except env-config.js)
4. **Health Checks**: Always include liveness and readiness probes
5. **Version Tagging**: Tag images with git commit SHA and semantic version
6. **Resource Limits**: Set appropriate CPU/memory limits in production
7. **Logging**: Use `ENABLE_DEBUG=false` in production to reduce noise

## Scaling

The frontend is stateless and can be horizontally scaled:

```bash
# Docker Swarm
docker service scale algotrendy-frontend=5

# Kubernetes
kubectl scale deployment algotrendy-frontend --replicas=5
```

## Monitoring

Monitor key metrics:
- nginx access logs
- Response times
- Error rates
- Container resource usage (CPU, memory)
- Health check status

Example prometheus metrics collection:
```yaml
# Add to docker-compose.yml
- "9113:9113"  # nginx-prometheus-exporter
```
