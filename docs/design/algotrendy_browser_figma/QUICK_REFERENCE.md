# AlgoTrendy Frontend - Quick Reference Card

## Build Commands

```bash
# Navigate to frontend directory
cd /root/AlgoTrendy_v2.6/docs/design/algotrendy_browser_figma

# Build Docker image
docker build -f src/Dockerfile.production -t algotrendy-frontend:latest .

# Build with version tag
docker build -f src/Dockerfile.production -t algotrendy-frontend:2.6.0 .
```

## Run Commands

### Development (with local backend on host)
```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=http://host.docker.internal:5002/api \
  -e WS_BASE_URL=ws://host.docker.internal:5002 \
  -e ENVIRONMENT=development \
  -e ENABLE_DEBUG=true \
  -p 3000:80 \
  algotrendy-frontend:latest
```
Access: http://localhost:3000

### Production (with reverse proxy)
```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=/api \
  -e WS_BASE_URL= \
  -e ENVIRONMENT=production \
  -e ENABLE_DEBUG=false \
  -p 80:80 \
  algotrendy-frontend:latest
```
Access: http://localhost

### Staging (direct backend access)
```bash
docker run -d \
  --name algotrendy-frontend \
  -e API_BASE_URL=http://backend-staging.yourdomain.com:5002/api \
  -e WS_BASE_URL=wss://backend-staging.yourdomain.com \
  -e ENVIRONMENT=staging \
  -p 8080:80 \
  algotrendy-frontend:latest
```
Access: http://localhost:8080

## Container Management

```bash
# View logs
docker logs algotrendy-frontend

# Follow logs
docker logs -f algotrendy-frontend

# Check health
docker inspect algotrendy-frontend --format='{{.State.Health.Status}}'

# Check runtime config
docker exec algotrendy-frontend cat /usr/share/nginx/html/env-config.js

# Restart container
docker restart algotrendy-frontend

# Stop container
docker stop algotrendy-frontend

# Remove container
docker rm algotrendy-frontend
```

## Environment Variables Reference

| Variable | Example Value | Description |
|----------|--------------|-------------|
| `API_BASE_URL` | `/api` | Backend API URL |
| `WS_BASE_URL` | `wss://api.example.com` | WebSocket URL (empty = auto-detect) |
| `ENVIRONMENT` | `production` | Environment name |
| `VERSION` | `2.6.0` | App version |
| `ENABLE_DEBUG` | `false` | Debug logging |

## Health Checks

```bash
# Test from outside container
curl http://localhost:3000/health

# Expected response
healthy

# Test frontend loads
curl -I http://localhost:3000/

# Expected response
HTTP/1.1 200 OK
```

## Troubleshooting

### Container won't start
```bash
docker logs algotrendy-frontend
```

### Check nginx config
```bash
docker exec algotrendy-frontend nginx -t
```

### Test API connectivity
```bash
# From browser console
console.log(window.ENV)
```

### Rebuild with no cache
```bash
docker build --no-cache -f src/Dockerfile.production -t algotrendy-frontend:latest .
```

## Development Workflow

```bash
# Local development (no Docker)
npm run dev
# Access: http://localhost:3000

# Build static files
npm run build
# Output: build/

# Test production build locally
docker build -f src/Dockerfile.production -t algotrendy-frontend:test .
docker run -p 3000:80 algotrendy-frontend:test
```

## Docker Compose (Full Stack)

Create `docker-compose.yml`:
```yaml
version: '3.8'

services:
  frontend:
    image: algotrendy-frontend:latest
    environment:
      - API_BASE_URL=http://backend:5002/api
      - WS_BASE_URL=ws://backend:5002
      - ENVIRONMENT=production
    ports:
      - "80:80"
    depends_on:
      - backend

  backend:
    image: algotrendy-backend:latest
    ports:
      - "5002:5002"
```

Run with:
```bash
docker-compose up -d
```

## Key Files

| File | Purpose |
|------|---------|
| `src/Dockerfile.production` | Production build |
| `docker-entrypoint.sh` | Runtime config script |
| `nginx.conf` | nginx server config |
| `src/config/api.ts` | API configuration |
| `.env.example` | Environment docs |
| `DOCKER.md` | Full deployment guide |

## URLs

- **Frontend Dev**: http://localhost:3000
- **Frontend Docker**: http://localhost:8080 (or 80/3000 depending on -p flag)
- **ML Training Page**: http://localhost:3000/ml-training
- **Health Check**: http://localhost:3000/health
- **Runtime Config**: http://localhost:3000/env-config.js

## Image Registry

```bash
# Tag for registry
docker tag algotrendy-frontend:latest registry.example.com/algotrendy-frontend:2.6.0

# Push to registry
docker push registry.example.com/algotrendy-frontend:2.6.0

# Pull from registry
docker pull registry.example.com/algotrendy-frontend:2.6.0
```

## CI/CD Integration

GitHub Actions snippet:
```yaml
- name: Build Docker Image
  run: |
    cd docs/design/algotrendy_browser_figma
    docker build -f src/Dockerfile.production -t algotrendy-frontend:${{ github.sha }} .
```

## Support

- Full docs: `DOCKER.md`
- Implementation details: `RUNTIME_CONFIG_COMPLETE.md`
- Environment variables: `.env.example`
