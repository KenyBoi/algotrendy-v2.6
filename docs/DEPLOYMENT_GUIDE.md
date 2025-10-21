# AlgoTrendy v2.6 - Production Deployment Guide

This guide covers deploying AlgoTrendy to a production environment using Docker Compose on a VPS.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Server Setup](#server-setup)
- [Installation](#installation)
- [Configuration](#configuration)
- [SSL Setup](#ssl-setup)
- [Database Migration](#database-migration)
- [Deployment](#deployment)
- [Monitoring](#monitoring)
- [Backup & Recovery](#backup--recovery)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Server Requirements

**Minimum Specifications:**
- **OS:** Ubuntu 22.04 LTS or later
- **CPU:** 4 cores
- **RAM:** 8 GB
- **Storage:** 100 GB SSD
- **Network:** 100 Mbps

**Recommended Specifications:**
- **OS:** Ubuntu 22.04 LTS
- **CPU:** 8 cores
- **RAM:** 16 GB
- **Storage:** 250 GB NVMe SSD
- **Network:** 1 Gbps

### Domain & DNS

- Domain name configured and pointing to your server
- DNS A records for:
  - `algotrendy.com` → Your server IP
  - `www.algotrendy.com` → Your server IP
  - `api.algotrendy.com` → Your server IP
  - `app.algotrendy.com` → Your server IP

### Required Accounts

- Broker accounts (Bybit, Binance, etc.)
- QuantConnect account (optional for backtesting)
- Email service for notifications

---

## Server Setup

### 1. Initial Server Configuration

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install required packages
sudo apt install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    git \
    ufw

# Set timezone
sudo timedatectl set-timezone UTC

# Set hostname
sudo hostnamectl set-hostname algotrendy
```

### 2. Install Docker

```bash
# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Add Docker repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Start and enable Docker
sudo systemctl enable docker
sudo systemctl start docker

# Add user to docker group
sudo usermod -aG docker $USER
```

### 3. Configure Firewall

```bash
# Enable UFW
sudo ufw enable

# Allow SSH (change port if using non-standard)
sudo ufw allow 22/tcp

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Check status
sudo ufw status
```

### 4. Install Docker Compose

```bash
# Download Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose

# Make executable
sudo chmod +x /usr/local/bin/docker-compose

# Verify installation
docker-compose --version
```

---

## Installation

### 1. Clone Repository

```bash
# Create application directory
sudo mkdir -p /opt/algotrendy
sudo chown $USER:$USER /opt/algotrendy
cd /opt/algotrendy

# Clone repository
git clone https://github.com/KenyBoi/algotrendy-v2.6.git .

# Checkout main branch
git checkout main
```

### 2. Create Directory Structure

```bash
# Create required directories
mkdir -p \
    questdb-data \
    logs \
    ssl \
    certbot/www \
    certbot/conf \
    backups
```

---

## Configuration

### 1. Environment Variables

Create `.env` file:

```bash
cp .env.example .env
nano .env
```

Configure the following variables:

```env
# Environment
ASPNETCORE_ENVIRONMENT=Production

# Database
QUESTDB_USER=admin
QUESTDB_PASSWORD=CHANGE_THIS_STRONG_PASSWORD

# API Keys - Binance
BINANCE_API_KEY=your_binance_api_key
BINANCE_API_SECRET=your_binance_api_secret
BINANCE_TESTNET=false
BINANCE_US=true

# API Keys - Bybit
BYBIT_API_KEY=your_bybit_api_key
BYBIT_API_SECRET=your_bybit_api_secret
BYBIT_TESTNET=false

# QuantConnect (Optional)
QUANTCONNECT_USER_ID=your_qc_user_id
QUANTCONNECT_API_TOKEN=your_qc_api_token

# Logging
SEQ_API_KEY=your_seq_api_key
API_LOG_LEVEL=Warning

# CORS
ALLOWED_ORIGINS=https://algotrendy.com,https://www.algotrendy.com,https://app.algotrendy.com

# Market Data
MARKET_DATA_FETCH_INTERVAL=60

# ML Enhancement
ML_ENHANCEMENT_ENABLED=true

# Flask Environment
FLASK_ENV=production
```

### 2. Secure Sensitive Files

```bash
# Set proper permissions
chmod 600 .env
chmod 600 ssl/*

# Never commit sensitive files
echo ".env" >> .gitignore
echo "ssl/*.key" >> .gitignore
```

---

## SSL Setup

### 1. DNS Verification

Verify DNS is pointing to your server:

```bash
./scripts/show-dns-instructions.sh
```

Wait for DNS propagation (check with):

```bash
dig algotrendy.com
dig www.algotrendy.com
dig api.algotrendy.com
dig app.algotrendy.com
```

### 2. Obtain SSL Certificates

```bash
# Run SSL setup script
sudo ./scripts/setup-ssl.sh
```

This will:
- Check DNS configuration
- Start nginx for Let's Encrypt validation
- Request SSL certificates from Let's Encrypt
- Update nginx configuration
- Restart all services with HTTPS enabled

### 3. Verify SSL

```bash
# Test SSL certificate
openssl s_client -connect algotrendy.com:443 -servername algotrendy.com

# Check certificate expiration
echo | openssl s_client -connect algotrendy.com:443 2>/dev/null | openssl x509 -noout -dates
```

---

## Database Migration

### 1. QuestDB Setup

QuestDB is automatically configured on first start. No manual migration needed.

### 2. Initial Data Seeding (Optional)

```bash
# Connect to API container
docker exec -it algotrendy-api-prod bash

# Run data seeding (if applicable)
dotnet run --project AlgoTrendy.API/AlgoTrendy.API.csproj -- seed-data
```

---

## Deployment

### 1. Build Docker Images

```bash
# Build all images
docker-compose -f docker-compose.prod.yml build

# Or build individually
docker-compose -f docker-compose.prod.yml build api
docker-compose -f docker-compose.prod.yml build frontend
docker-compose -f docker-compose.prod.yml build ml-service
```

### 2. Start Services

```bash
# Start all services
docker-compose -f docker-compose.prod.yml up -d

# Check status
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f
```

### 3. Verify Deployment

```bash
# Check container health
docker ps --format "table {{.Names}}\t{{.Status}}"

# Test API health
curl -k https://algotrendy.com/health

# Test frontend
curl -k https://www.algotrendy.com

# Check logs
docker logs algotrendy-api-prod --tail 50
docker logs algotrendy-nginx-prod --tail 50
```

### 4. Post-Deployment Checks

```bash
# Verify all endpoints
curl -k https://algotrendy.com/health
curl -k https://api.algotrendy.com/health
curl -k https://app.algotrendy.com

# Check QuestDB
curl http://localhost:9000

# Check Seq logs
curl http://localhost:5341
```

---

## Monitoring

### 1. Container Monitoring

```bash
# View resource usage
docker stats

# Check container logs
docker logs -f algotrendy-api-prod
docker logs -f algotrendy-questdb-prod
docker logs -f algotrendy-nginx-prod

# Monitor all containers
docker-compose -f docker-compose.prod.yml logs -f --tail=100
```

### 2. Application Monitoring

**Seq Dashboard:**
- URL: `http://localhost:5341`
- View structured logs
- Set up alerts

**QuestDB Console:**
- URL: `http://localhost:9000`
- Query market data
- Monitor database performance

### 3. System Monitoring

```bash
# Install monitoring tools
sudo apt install -y htop iotop nethogs

# Monitor system resources
htop

# Monitor disk I/O
sudo iotop

# Monitor network
sudo nethogs
```

### 4. Set Up Alerts

Create monitoring script:

```bash
cat > /opt/algotrendy/scripts/health-check.sh << 'EOF'
#!/bin/bash
# Health check script

# Check if containers are running
if ! docker ps | grep -q algotrendy-api-prod; then
    echo "ALERT: API container is down!"
    # Send notification (email, Slack, etc.)
fi

# Check disk space
DISK_USAGE=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    echo "ALERT: Disk usage is at ${DISK_USAGE}%"
fi

# Check memory usage
MEM_USAGE=$(free | grep Mem | awk '{print ($3/$2) * 100.0}' | cut -d. -f1)
if [ $MEM_USAGE -gt 90 ]; then
    echo "ALERT: Memory usage is at ${MEM_USAGE}%"
fi
EOF

chmod +x /opt/algotrendy/scripts/health-check.sh

# Add to crontab (run every 5 minutes)
(crontab -l 2>/dev/null; echo "*/5 * * * * /opt/algotrendy/scripts/health-check.sh") | crontab -
```

---

## Backup & Recovery

### 1. Database Backup

```bash
# Create backup script
cat > /opt/algotrendy/scripts/backup.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="/opt/algotrendy/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Backup QuestDB data
echo "Backing up QuestDB..."
docker exec algotrendy-questdb-prod tar -czf - /var/lib/questdb > $BACKUP_DIR/questdb_$DATE.tar.gz

# Backup environment files
cp .env $BACKUP_DIR/env_$DATE.backup

# Remove backups older than 30 days
find $BACKUP_DIR -name "*.tar.gz" -mtime +30 -delete
find $BACKUP_DIR -name "*.backup" -mtime +30 -delete

echo "Backup completed: $DATE"
EOF

chmod +x /opt/algotrendy/scripts/backup.sh

# Schedule daily backups at 2 AM
(crontab -l 2>/dev/null; echo "0 2 * * * /opt/algotrendy/scripts/backup.sh") | crontab -
```

### 2. Recovery Process

```bash
# Stop services
docker-compose -f docker-compose.prod.yml down

# Restore QuestDB data
BACKUP_FILE="/opt/algotrendy/backups/questdb_YYYYMMDD_HHMMSS.tar.gz"
docker run --rm -v questdb_data:/var/lib/questdb -v $(pwd)/backups:/backup alpine \
    sh -c "cd /var/lib/questdb && tar -xzf /backup/$(basename $BACKUP_FILE)"

# Restore environment
cp /opt/algotrendy/backups/env_YYYYMMDD_HHMMSS.backup .env

# Restart services
docker-compose -f docker-compose.prod.yml up -d
```

### 3. Automated Backups to Remote

```bash
# Install rclone for remote backups
curl https://rclone.org/install.sh | sudo bash

# Configure rclone (S3, Google Drive, etc.)
rclone config

# Add remote backup to script
cat >> /opt/algotrendy/scripts/backup.sh << 'EOF'

# Upload to remote storage
rclone sync $BACKUP_DIR remote:algotrendy-backups
EOF
```

---

## Troubleshooting

### Common Issues

#### 1. Containers Won't Start

```bash
# Check logs
docker-compose -f docker-compose.prod.yml logs

# Check disk space
df -h

# Check memory
free -h

# Restart Docker service
sudo systemctl restart docker
```

#### 2. Database Connection Errors

```bash
# Check QuestDB is running
docker ps | grep questdb

# Check QuestDB logs
docker logs algotrendy-questdb-prod

# Test database connection
docker exec -it algotrendy-questdb-prod psql -U admin -d qdb
```

#### 3. SSL Certificate Issues

```bash
# Check certificate validity
certbot certificates

# Renew certificate manually
sudo certbot renew

# Check nginx configuration
docker exec algotrendy-nginx-prod nginx -t
```

#### 4. High Memory Usage

```bash
# Check container memory usage
docker stats --no-stream

# Restart specific container
docker-compose -f docker-compose.prod.yml restart api

# Clear Docker cache
docker system prune -a
```

### Log Locations

- **API Logs:** `docker logs algotrendy-api-prod`
- **Nginx Logs:** `docker logs algotrendy-nginx-prod`
- **QuestDB Logs:** `docker logs algotrendy-questdb-prod`
- **Seq UI:** `http://localhost:5341`

---

## Maintenance

### Regular Tasks

**Daily:**
- Monitor logs for errors
- Check system resources
- Verify backup completion

**Weekly:**
- Review Dependabot alerts
- Update dependencies
- Check SSL certificate expiration
- Review security logs

**Monthly:**
- Test backup recovery
- Update Docker images
- Review and rotate API keys
- Performance optimization

### Updates

```bash
# Pull latest changes
git pull origin main

# Rebuild images
docker-compose -f docker-compose.prod.yml build

# Rolling update (zero downtime)
docker-compose -f docker-compose.prod.yml up -d --no-deps --build api
docker-compose -f docker-compose.prod.yml up -d --no-deps --build frontend

# Or full restart
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

---

## Security Checklist

- [ ] Strong passwords for all services
- [ ] SSL certificates installed and auto-renewing
- [ ] Firewall configured properly
- [ ] SSH key authentication enabled
- [ ] API keys rotated regularly
- [ ] Regular backups tested
- [ ] Monitoring and alerting configured
- [ ] Logs reviewed regularly
- [ ] Dependencies kept up to date
- [ ] Security patches applied

---

## Performance Optimization

### 1. Enable Swap (if needed)

```bash
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### 2. Docker Optimizations

```bash
# Add to /etc/docker/daemon.json
sudo cat > /etc/docker/daemon.json << 'EOF'
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2"
}
EOF

sudo systemctl restart docker
```

### 3. QuestDB Optimizations

Adjust memory settings in `docker-compose.prod.yml`:

```yaml
environment:
  - JAVA_OPTS=-Xms4g -Xmx8g  # Adjust based on available RAM
```

---

## Additional Resources

- [CONTRIBUTING.md](../CONTRIBUTING.md) - Development guidelines
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [API_USAGE_EXAMPLES.md](API_USAGE_EXAMPLES.md) - API integration
- [GitHub Repository](https://github.com/KenyBoi/algotrendy-v2.6)

---

**Deployment Complete!** 🚀

Your AlgoTrendy instance should now be running at:
- Frontend: https://www.algotrendy.com
- API: https://api.algotrendy.com
- Application: https://app.algotrendy.com

For support, open an issue on GitHub or contact support@algotrendy.com

---

*Last Updated: October 21, 2025*
