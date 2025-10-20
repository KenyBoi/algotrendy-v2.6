# Docker Setup Guide

## Quick Start

### Option 1: Run Everything with Docker Compose (Production)

```bash
# 1. Create .env file (copy from .env.example)
cp .env.example .env

# 2. Update .env with your backend URL
# Edit .env and set VITE_API_BASE_URL to your backend

# 3. Build and start all services
docker-compose up -d

# Access the app at http://localhost:3000
```

### Option 2: Frontend Only (If Backend is Already Running)

```bash
# 1. Build the frontend image
docker build -t trading-frontend .

# 2. Run the container
docker run -d \
  -p 3000:80 \
  -e VITE_API_BASE_URL=http://your-backend:5000/api \
  --name trading-frontend \
  trading-frontend

# Access at http://localhost:3000
```

### Option 3: Development Mode

```bash
# Run frontend in development mode with hot reload
docker-compose -f docker-compose.dev.yml up

# Access at http://localhost:3000
```

---

## Integration with Your Existing Docker Setup

### If you already have a docker-compose.yml for your backend:

1. **Copy the frontend service** from `docker-compose.yml` to your existing compose file:

```yaml
services:
  # ... your existing services ...

  frontend:
    build:
      context: ./path/to/frontend
      dockerfile: Dockerfile
    container_name: trading-frontend
    ports:
      - "3000:80"
    environment:
      - VITE_API_BASE_URL=http://backend:5000/api
    depends_on:
      - backend  # your backend service name
    networks:
      - your-existing-network
```

2. **Update the network** to match your existing network name

3. **Restart your stack:**
```bash
docker-compose down
docker-compose up -d
```

---

## Directory Structure

Assuming this structure:
```
your-project/
├── frontend/           # This React app
│   ├── Dockerfile
│   ├── nginx.conf
│   └── ...
├── backend/            # Your .NET backend
│   ├── Dockerfile
│   └── ...
└── docker-compose.yml  # Main compose file
```

Your main `docker-compose.yml` should look like:

```yaml
version: '3.8'

services:
  frontend:
    build: ./frontend
    ports:
      - "3000:80"
    environment:
      - VITE_API_BASE_URL=http://backend:5000/api
    depends_on:
      - backend

  backend:
    build: ./backend
    ports:
      - "5000:5000"
    depends_on:
      - questdb
    environment:
      - ConnectionStrings__QuestDB=http://questdb:9000

  questdb:
    image: questdb/questdb:latest
    ports:
      - "9000:9000"
    volumes:
      - questdb-data:/var/lib/questdb

volumes:
  questdb-data:
```

---

## Environment Variables

### Build-time variables (baked into the image):

These are set in your `.env` file and used during `docker build`:

```env
VITE_API_BASE_URL=http://backend:5000/api
VITE_WS_URL=ws://backend:5000/ws
```

**Important:** Since this is a static React app, environment variables are compiled into the build. To change them, you need to rebuild:

```bash
docker-compose build frontend
docker-compose up -d frontend
```

### Runtime configuration:

For true runtime configuration, use the nginx proxy approach (uncomment in `nginx.conf`):
- Frontend makes requests to `/api/*`
- Nginx proxies to your backend
- No need to rebuild when backend URL changes

---

## Common Commands

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f frontend
docker-compose logs -f backend

# Stop services
docker-compose down

# Rebuild and restart a service
docker-compose up -d --build frontend

# Remove everything including volumes
docker-compose down -v

# Check health status
docker ps
```

---

## Nginx Proxy Configuration (Recommended)

If you want to avoid rebuilding when backend changes, use nginx as a proxy:

1. **Uncomment the `/api` location block in `nginx.conf`:**

```nginx
location /api {
    proxy_pass http://backend:5000;
    # ... rest of config
}
```

2. **Update `config/api.ts` to use relative URLs:**

```typescript
const API_BASE_URL = '/api';  // Let nginx handle the proxy
```

3. **Frontend makes requests to `/api/*`**
4. **Nginx forwards to backend container**
5. **No CORS issues, no rebuilds needed**

---

## Production Deployment

### Using Docker Swarm:

```bash
docker stack deploy -c docker-compose.yml trading-platform
```

### Using Kubernetes:

Convert to Kubernetes manifests:
```bash
kompose convert -f docker-compose.yml
kubectl apply -f .
```

### AWS ECS/Fargate:

Use AWS Copilot CLI:
```bash
copilot init
copilot deploy
```

---

## Connecting to Your Existing Backend

### If your backend is on `localhost:5000`:

```bash
docker run -d \
  -p 3000:80 \
  -e VITE_API_BASE_URL=http://host.docker.internal:5000/api \
  trading-frontend
```

### If your backend is in another Docker container:

```bash
# Add to same network
docker run -d \
  -p 3000:80 \
  -e VITE_API_BASE_URL=http://backend-container-name:5000/api \
  --network your-backend-network \
  trading-frontend
```

### If your backend is on a remote server:

```bash
docker run -d \
  -p 3000:80 \
  -e VITE_API_BASE_URL=https://api.yourdomain.com/api \
  trading-frontend
```

---

## Troubleshooting

### Frontend can't connect to backend:

1. **Check network connectivity:**
   ```bash
   docker exec trading-frontend ping backend
   ```

2. **Verify environment variables:**
   ```bash
   docker exec trading-frontend env | grep VITE
   ```

3. **Check backend is accessible:**
   ```bash
   docker exec trading-frontend wget -O- http://backend:5000/health
   ```

### CORS errors:

Configure your .NET backend to allow the frontend origin:

```csharp
// In your Startup.cs or Program.cs
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("http://localhost:3000", "http://frontend")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
```

### Container keeps restarting:

```bash
# Check logs
docker logs trading-frontend

# Check health
docker inspect trading-frontend | grep -A 10 Health
```

---

## SSL/HTTPS Setup

For production with SSL:

1. **Use a reverse proxy like Traefik or nginx-proxy:**

```yaml
services:
  traefik:
    image: traefik:v2.10
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./traefik.yml:/traefik.yml
      - ./acme.json:/acme.json

  frontend:
    build: ./frontend
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.frontend.rule=Host(`trading.yourdomain.com`)"
      - "traefik.http.routers.frontend.tls=true"
      - "traefik.http.routers.frontend.tls.certresolver=letsencrypt"
```

2. **Or use Cloudflare for SSL termination**

---

## Performance Optimization

### Multi-stage builds (already implemented):
- Smaller image size (nginx:alpine vs full node)
- Faster deployments
- Separate build and runtime dependencies

### Caching:
```dockerfile
# Cache npm dependencies
COPY package*.json ./
RUN npm ci
# Then copy source
COPY . .
```

### Resource limits:
```yaml
services:
  frontend:
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
```

---

## Monitoring

### Health checks:
```bash
# Frontend health
curl http://localhost:3000/health

# QuestDB health
curl http://localhost:9000
```

### Logs:
```bash
# Stream all logs
docker-compose logs -f

# Just frontend
docker-compose logs -f frontend

# Last 100 lines
docker-compose logs --tail=100 frontend
```

### Metrics with Prometheus:
Add to docker-compose.yml:
```yaml
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
```

---

## Next Steps

1. ✅ Build the Docker image
2. ✅ Test locally with docker-compose
3. ✅ Connect to your backend
4. ✅ Test all API endpoints
5. ✅ Set up SSL for production
6. ✅ Deploy to your server
7. ✅ Set up monitoring
8. ✅ Configure backups for QuestDB

Need help? Check the main [DEPLOYMENT.md](./DEPLOYMENT.md) for cloud deployment options.
