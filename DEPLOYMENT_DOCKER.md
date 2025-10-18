# AlgoTrendy v2.6 - Docker Deployment Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Environment Configuration](#environment-configuration)
4. [SSL Certificate Setup](#ssl-certificate-setup)
5. [Deployment Steps](#deployment-steps)
6. [Service Architecture](#service-architecture)
7. [Monitoring and Health Checks](#monitoring-and-health-checks)
8. [Troubleshooting](#troubleshooting)
9. [Backup Strategy](#backup-strategy)
10. [Production Considerations](#production-considerations)

---

## Quick Start

For those who want to get started immediately:

```bash
# 1. Clone or navigate to the project
cd /root/AlgoTrendy_v2.6

# 2. Copy and configure environment variables
cp .env.example .env
nano .env  # Edit with your API keys and credentials

# 3. Generate SSL certificates (self-signed for testing)
cd ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout key.pem -out cert.pem \
  -subj "/C=US/ST=Illinois/L=Chicago/O=AlgoTrendy/CN=localhost"
cd ..

# 4. Build and start all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest up -d

# 5. Verify services are running
docker ps

# 6. Test the API
curl http://localhost:5002/health
```

---

## Prerequisites

### System Requirements
- **OS**: Linux (Ubuntu 20.04+, Debian 11+, RHEL 8+)
- **RAM**: Minimum 4GB, Recommended 8GB+
- **Disk**: 20GB free space (50GB+ for production)
- **CPU**: 2+ cores recommended

### Software Requirements
- **Docker**: Version 20.10+
- **Docker Compose**: Version 2.0+ (or docker/compose image)
- **Network**: Ports 80, 443, 5002, 8812, 9000 available

### Installation

#### Docker Installation (if not already installed)
```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# RHEL/CentOS
sudo yum install -y docker-ce docker-ce-cli containerd.io

# Start Docker
sudo systemctl start docker
sudo systemctl enable docker
```

#### Docker Compose Installation
```bash
# Using Docker Compose plugin (recommended)
sudo apt-get update
sudo apt-get install docker-compose-plugin

# Verify installation
docker compose version

# Alternative: Use docker/compose image (as shown in Quick Start)
docker run --rm docker/compose:latest version
```

---

## Environment Configuration

### 1. Create Environment File

```bash
cp .env.example .env
```

### 2. Essential Variables to Configure

Edit `.env` and set the following:

```bash
# ============================================================================
# CRITICAL: Change These Before Production Deployment
# ============================================================================

# Database Credentials
QUESTDB_USER=admin
QUESTDB_PASSWORD=CHANGE_ME_SECURE_PASSWORD_HERE

# API Environment
ASPNETCORE_ENVIRONMENT=Production
API_LOG_LEVEL=Information

# Exchange API Credentials (Required for Trading)
BINANCE_API_KEY=your_actual_binance_api_key
BINANCE_API_SECRET=your_actual_binance_secret
BINANCE_TESTNET=true  # Set to false for live trading

# CORS Settings (Update with your frontend domain)
ALLOWED_ORIGINS=http://localhost:3000,https://yourdomain.com

# Market Data Settings
MARKET_DATA_FETCH_INTERVAL=60  # seconds
```

### 3. Security Best Practices

- **Never commit .env to version control**
- Use strong passwords (16+ characters, mixed case, numbers, symbols)
- Rotate credentials regularly
- Use separate credentials for development and production
- Enable API key restrictions on exchange platforms

---

## SSL Certificate Setup

### Development/Testing: Self-Signed Certificates

```bash
cd /root/AlgoTrendy_v2.6/ssl

openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout key.pem -out cert.pem \
  -subj "/C=US/ST=Illinois/L=Chicago/O=AlgoTrendy/OU=Development/CN=localhost"

# Verify certificates
ls -lh
# Should show: cert.pem, key.pem
```

**Note**: Self-signed certificates will show browser warnings. Only use for development/testing.

### Production: Let's Encrypt (Recommended)

#### Option 1: Using Certbot (Before Docker Deployment)

```bash
# Install Certbot
sudo apt-get update
sudo apt-get install certbot

# Obtain certificates (requires domain pointing to your server)
sudo certbot certonly --standalone -d yourdomain.com -d www.yourdomain.com

# Copy certificates to project
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem \
  /root/AlgoTrendy_v2.6/ssl/cert.pem
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem \
  /root/AlgoTrendy_v2.6/ssl/key.pem

# Set proper permissions
sudo chown $USER:$USER /root/AlgoTrendy_v2.6/ssl/*.pem
chmod 644 /root/AlgoTrendy_v2.6/ssl/cert.pem
chmod 600 /root/AlgoTrendy_v2.6/ssl/key.pem
```

#### Option 2: Using Certbot with Docker (Automated Renewal)

```bash
# Create certbot directory
mkdir -p /root/AlgoTrendy_v2.6/certbot/conf
mkdir -p /root/AlgoTrendy_v2.6/certbot/www

# Run Certbot in Docker
docker run -it --rm --name certbot \
  -v "/root/AlgoTrendy_v2.6/certbot/conf:/etc/letsencrypt" \
  -v "/root/AlgoTrendy_v2.6/certbot/www:/var/www/certbot" \
  certbot/certbot certonly --webroot \
  -w /var/www/certbot \
  -d yourdomain.com -d www.yourdomain.com

# Link certificates
ln -sf /root/AlgoTrendy_v2.6/certbot/conf/live/yourdomain.com/fullchain.pem \
  /root/AlgoTrendy_v2.6/ssl/cert.pem
ln -sf /root/AlgoTrendy_v2.6/certbot/conf/live/yourdomain.com/privkey.pem \
  /root/AlgoTrendy_v2.6/ssl/key.pem
```

#### Automated Renewal Cron Job

```bash
# Add to crontab (crontab -e)
0 3 * * 0 docker run --rm --name certbot \
  -v "/root/AlgoTrendy_v2.6/certbot/conf:/etc/letsencrypt" \
  -v "/root/AlgoTrendy_v2.6/certbot/www:/var/www/certbot" \
  certbot/certbot renew --quiet && \
  docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest restart nginx
```

---

## Deployment Steps

### Step 1: Prepare the Environment

```bash
# Navigate to project directory
cd /root/AlgoTrendy_v2.6

# Ensure .env is configured
cat .env | grep -E "QUESTDB_PASSWORD|BINANCE_API_KEY" | grep -v "CHANGE_ME"
# Should show your actual credentials (not placeholders)

# Ensure SSL certificates exist
ls -lh ssl/
# Should show: cert.pem, key.pem
```

### Step 2: Build Docker Images

```bash
# Build the API image
docker build -f backend/Dockerfile -t algotrendy-api:v2.6 backend/

# Verify image was created
docker images | grep algotrendy-api
# Expected output: algotrendy-api  v2.6  <image-id>  <size>
```

**Expected Build Time**: 30-60 seconds
**Expected Image Size**: 200-300MB

### Step 3: Start Services with Docker Compose

```bash
# Using docker compose (if installed)
docker compose up -d

# OR using docker/compose image
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest up -d
```

**Expected Output**:
```
Creating network "algotrendy_v26_algotrendy-network" with driver "bridge"
Creating volume "algotrendy_v26_questdb_data" with local driver
Creating volume "algotrendy_v26_api_logs" with local driver
Creating volume "algotrendy_v26_nginx_ssl" with local driver
Creating algotrendy-questdb ... done
Creating algotrendy-api     ... done
Creating algotrendy-nginx   ... done
```

### Step 4: Verify Services Are Running

```bash
# Check container status
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Expected output:
# NAMES               STATUS                    PORTS
# algotrendy-nginx    Up 30 seconds (healthy)   0.0.0.0:80->80/tcp, 0.0.0.0:443->443/tcp
# algotrendy-api      Up 40 seconds (healthy)   0.0.0.0:5002->5002/tcp
# algotrendy-questdb  Up 45 seconds (healthy)   0.0.0.0:8812->8812/tcp, 0.0.0.0:9000->9000/tcp
```

### Step 5: Test API Connectivity

```bash
# Test health endpoint
curl http://localhost:5002/health
# Expected: "Healthy"

# Test API info endpoint (if available)
curl http://localhost:5002/api/v1/info || echo "No info endpoint"

# Test Swagger UI (if enabled)
curl -I http://localhost:5002/swagger/index.html
# Expected: HTTP/1.1 200 OK
```

### Step 6: Test QuestDB Connectivity

```bash
# Test QuestDB web console
curl -I http://localhost:9000/
# Expected: HTTP/1.1 200 OK

# Test PostgreSQL wire protocol (from inside API container)
docker exec algotrendy-api curl -I http://questdb:9000/
# Expected: HTTP/1.1 200 OK
```

### Step 7: Test Nginx Reverse Proxy (if port 80/443 available)

```bash
# Test HTTP redirect to HTTPS
curl -I http://localhost/
# Expected: HTTP/1.1 301 Moved Permanently

# Test HTTPS endpoint (with self-signed cert)
curl -k -I https://localhost/health
# Expected: HTTP/2 200
```

---

## Service Architecture

### Docker Network Topology

```
Internet
    |
    v
[Nginx:80/443] ──> [API:5002] ──> [QuestDB:8812]
    |                   |              |
    |                   v              v
    |            [API Logs Volume]  [QuestDB Data Volume]
    |
    v
[SSL Certificates]
```

### Service Details

#### 1. QuestDB (Time-Series Database)
- **Image**: `questdb/questdb:latest`
- **Container Name**: `algotrendy-questdb`
- **Ports**:
  - `9000`: HTTP API & Web Console
  - `8812`: PostgreSQL wire protocol (used by API)
  - `9009`: InfluxDB line protocol
- **Volumes**: `questdb_data:/var/lib/questdb`
- **Network**: `172.20.0.10` (fixed IP)

#### 2. AlgoTrendy API (.NET 8)
- **Image**: `algotrendy-api:v2.6` (built from Dockerfile)
- **Container Name**: `algotrendy-api`
- **Port**: `5002` (HTTP)
- **Volumes**: `api_logs:/app/logs`
- **Network**: `172.20.0.20` (fixed IP)
- **Health Check**: `curl -f http://localhost:5002/health`

#### 3. Nginx (Reverse Proxy)
- **Image**: `nginx:alpine`
- **Container Name**: `algotrendy-nginx`
- **Ports**:
  - `80`: HTTP (redirects to HTTPS)
  - `443`: HTTPS (SSL termination)
- **Volumes**:
  - `./nginx.conf:/etc/nginx/nginx.conf:ro`
  - `./ssl:/etc/nginx/ssl:ro`
- **Network**: `172.20.0.30` (fixed IP)

### Persistent Volumes

```bash
# List Docker volumes
docker volume ls | grep algotrendy

# Inspect volume details
docker volume inspect algotrendy_v26_questdb_data
docker volume inspect algotrendy_v26_api_logs

# View volume data location
docker volume inspect algotrendy_v26_questdb_data | grep Mountpoint
```

---

## Monitoring and Health Checks

### Health Check Endpoints

```bash
# API Health
curl http://localhost:5002/health
# Expected: "Healthy"

# QuestDB Health
curl http://localhost:9000/
# Expected: HTML page (200 OK)

# Nginx Health (via Docker)
docker exec algotrendy-nginx wget -qO- http://localhost/health
# Expected: "healthy"
```

### Container Health Status

```bash
# Check all container health
docker ps --format "table {{.Names}}\t{{.Status}}"

# Watch health status in real-time
watch -n 5 'docker ps --format "table {{.Names}}\t{{.Status}}"'
```

### Resource Monitoring

```bash
# Real-time resource usage
docker stats

# One-time snapshot
docker stats --no-stream --format \
  "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"
```

### Log Monitoring

```bash
# View API logs
docker logs -f algotrendy-api

# View last 100 lines
docker logs --tail 100 algotrendy-api

# View logs with timestamps
docker logs -t algotrendy-api

# View QuestDB logs
docker logs -f algotrendy-questdb

# View Nginx logs
docker logs -f algotrendy-nginx

# View logs from all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest logs -f
```

### Persistent Log Files

```bash
# API logs (inside container)
docker exec algotrendy-api ls -lh /app/logs/

# View API log file
docker exec algotrendy-api tail -100 /app/logs/algotrendy-$(date +%Y%m%d).log

# Copy logs to host
docker cp algotrendy-api:/app/logs/ ./exported-logs/
```

---

## Troubleshooting

### Common Issues and Solutions

#### 1. Port Already in Use

**Error**: `Bind for 0.0.0.0:5002 failed: port is already allocated`

**Solution**:
```bash
# Find what's using the port
lsof -i :5002
# OR
netstat -tunlp | grep 5002

# Kill the process
kill <PID>

# Restart Docker Compose
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest restart
```

#### 2. Container Fails Health Check

**Error**: Container status shows "unhealthy"

**Solution**:
```bash
# Check container logs
docker logs algotrendy-api --tail 50

# Inspect health check details
docker inspect algotrendy-api | jq '.[0].State.Health'

# Manually run health check
docker exec algotrendy-api curl -f http://localhost:5002/health

# Restart container
docker restart algotrendy-api
```

#### 3. Database Connection Failed

**Error**: `Npgsql.PostgresException: Connection refused`

**Solution**:
```bash
# Ensure QuestDB is running and healthy
docker ps | grep questdb

# Test connectivity from API container
docker exec algotrendy-api curl http://questdb:9000/

# Check QuestDB logs
docker logs algotrendy-questdb --tail 50

# Verify connection string in .env
cat .env | grep QUESTDB
```

#### 4. SSL Certificate Errors

**Error**: `SSL certificate problem: self signed certificate`

**Solutions**:
```bash
# For testing, use -k flag with curl
curl -k https://localhost/health

# For production, ensure valid SSL certificates
ls -lh ssl/cert.pem ssl/key.pem

# Verify certificate details
openssl x509 -in ssl/cert.pem -text -noout

# Regenerate self-signed certificate
cd ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout key.pem -out cert.pem \
  -subj "/C=US/ST=Illinois/L=Chicago/O=AlgoTrendy/CN=localhost"
```

#### 5. Out of Memory

**Error**: Container keeps restarting, OOM killed

**Solution**:
```bash
# Check Docker system resources
docker system df

# Clean up unused resources
docker system prune -a --volumes

# Increase Docker memory limit (Docker Desktop)
# Settings > Resources > Memory > Increase to 4GB+

# Add memory limits to docker-compose.yml
# See docker-compose.prod.yml for examples
```

### Diagnostic Commands

```bash
# Full system diagnostics
docker info
docker version
docker ps -a
docker images
docker volume ls
docker network ls

# Check Docker Compose configuration
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest config

# Test network connectivity between containers
docker exec algotrendy-api ping -c 3 questdb
docker exec algotrendy-api curl http://questdb:9000/

# Export container filesystem for analysis
docker export algotrendy-api > algotrendy-api-export.tar
```

---

## Backup Strategy

### 1. Database Backup (QuestDB)

#### Manual Backup
```bash
# Stop QuestDB container
docker stop algotrendy-questdb

# Backup QuestDB data volume
docker run --rm \
  -v algotrendy_v26_questdb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/questdb-backup-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .

# Restart QuestDB
docker start algotrendy-questdb
```

#### Automated Daily Backup (Cron Job)
```bash
# Add to crontab (crontab -e)
0 2 * * * cd /root/AlgoTrendy_v2.6 && \
  docker run --rm \
  -v algotrendy_v26_questdb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/questdb-backup-$(date +\%Y\%m\%d).tar.gz -C /data . && \
  find /root/AlgoTrendy_v2.6/backups -name "questdb-backup-*.tar.gz" -mtime +30 -delete
```

### 2. Application Logs Backup

```bash
# Backup API logs
docker run --rm \
  -v algotrendy_v26_api_logs:/data \
  -v $(pwd)/backups:/backup \
  alpine tar czf /backup/api-logs-backup-$(date +%Y%m%d-%H%M%S).tar.gz -C /data .
```

### 3. Configuration Backup

```bash
# Backup critical configuration files
tar czf backups/config-backup-$(date +%Y%m%d-%H%M%S).tar.gz \
  .env docker-compose.yml nginx.conf ssl/
```

### 4. Full System Backup

```bash
# Create comprehensive backup
./scripts/backup.sh full

# Or manually
tar czf backups/full-backup-$(date +%Y%m%d-%H%M%S).tar.gz \
  --exclude='node_modules' \
  --exclude='bin' \
  --exclude='obj' \
  --exclude='.git' \
  .
```

### 5. Restore from Backup

```bash
# Stop all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest down -v

# Restore QuestDB data
docker volume create algotrendy_v26_questdb_data
docker run --rm \
  -v algotrendy_v26_questdb_data:/data \
  -v $(pwd)/backups:/backup \
  alpine sh -c "cd /data && tar xzf /backup/questdb-backup-YYYYMMDD-HHMMSS.tar.gz"

# Restore configuration
tar xzf backups/config-backup-YYYYMMDD-HHMMSS.tar.gz

# Restart services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest up -d
```

---

## Production Considerations

### 1. Use Production Docker Compose File

```bash
# Use docker-compose.prod.yml for production
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest \
  -f docker-compose.prod.yml up -d
```

### 2. Security Hardening

- **Disable Development Features**: Set `ASPNETCORE_ENVIRONMENT=Production`
- **Remove Swagger UI**: Comment out Swagger middleware in production
- **Enable HTTPS Only**: Configure HSTS headers
- **Firewall Configuration**: Block direct access to ports 5002, 8812, 9000
- **Rate Limiting**: Enable Nginx rate limiting
- **Secrets Management**: Use Docker secrets or external secret managers

### 3. Performance Optimization

```yaml
# In docker-compose.prod.yml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '1.0'
          memory: 1G
```

### 4. Monitoring and Alerting

- **Prometheus**: Add metrics endpoint
- **Grafana**: Visualize metrics
- **Alert Manager**: Configure alerts for critical events
- **Health Check Monitoring**: Use external monitoring (UptimeRobot, Pingdom)

### 5. High Availability

- **Load Balancer**: Deploy behind HAProxy or Nginx load balancer
- **Database Replication**: Configure QuestDB replication
- **Backup Nodes**: Set up failover nodes
- **Auto-restart**: Ensure `restart: unless-stopped` is set

### 6. Logging Best Practices

```yaml
# Centralized logging
services:
  api:
    logging:
      driver: "syslog"
      options:
        syslog-address: "tcp://logserver:514"
        tag: "algotrendy-api"
```

### 7. Update Strategy

```bash
# Zero-downtime update
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest \
  up -d --no-deps --build api

# Rolling update with health check
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest \
  up -d --scale api=2 --no-recreate
```

---

## Maintenance Commands

### Start/Stop Services

```bash
# Start all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest start

# Stop all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest stop

# Restart specific service
docker restart algotrendy-api

# Restart all services
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest restart
```

### Update Services

```bash
# Pull latest images
docker pull questdb/questdb:latest
docker pull nginx:alpine

# Rebuild API image
docker build -f backend/Dockerfile -t algotrendy-api:v2.6 backend/

# Recreate containers with new images
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  -v "$PWD:$PWD" -w "$PWD" docker/compose:latest up -d --force-recreate
```

### Cleanup

```bash
# Remove stopped containers
docker container prune

# Remove unused images
docker image prune -a

# Remove unused volumes (CAUTION: This deletes data!)
docker volume prune

# Complete cleanup (CAUTION!)
docker system prune -a --volumes
```

---

## Support and Additional Resources

### Documentation
- **Project README**: `/root/AlgoTrendy_v2.6/README.md`
- **API Documentation**: `http://localhost:5002/swagger` (if enabled)
- **QuestDB Docs**: https://questdb.io/docs/

### Logs Location
- **API Logs**: `docker logs algotrendy-api`
- **QuestDB Logs**: `docker logs algotrendy-questdb`
- **Nginx Logs**: `docker logs algotrendy-nginx`

### Health Endpoints
- **API Health**: `http://localhost:5002/health`
- **QuestDB Console**: `http://localhost:9000/`

---

**Last Updated**: October 18, 2025
**Version**: 2.6
**Maintainer**: AlgoTrendy Development Team
