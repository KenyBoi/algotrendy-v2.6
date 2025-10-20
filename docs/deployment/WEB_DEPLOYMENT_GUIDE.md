# AlgoTrendy v2.6 - Web Deployment Guide

Complete guide to deploy AlgoTrendy (Backend + Frontend) to the internet.

## Current Status

✅ **Locally Built & Ready:**
- Backend API (.NET 8) - Healthy & Running
- Frontend (React + Vite) - Deployed in Docker
- QuestDB Database - Running
- Nginx Reverse Proxy - Configured with SSL support
- Production Docker Compose - Ready

## What You Need

### 1. A Server (VPS/Cloud)

Choose one of these providers:

**Budget Options ($5-20/month):**
- [DigitalOcean](https://www.digitalocean.com) - Droplet ($6/month for 1GB RAM)
- [Linode/Akamai](https://www.linode.com) - ($5/month for 1GB RAM)
- [Vultr](https://www.vultr.com) - ($6/month for 1GB RAM)
- [Hetzner](https://www.hetzner.com) - Cheapest (~$5/month)

**Premium Options:**
- [AWS EC2](https://aws.amazon.com) - t3.medium recommended
- [Google Cloud Compute](https://cloud.google.com) - e2-medium recommended
- [Azure VM](https://azure.microsoft.com) - B2s recommended

**Minimum Server Requirements:**
- 4 GB RAM (8 GB recommended)
- 2 CPU cores (4 cores recommended)
- 40 GB SSD storage
- Ubuntu 22.04 LTS or 24.04 LTS
- Public IPv4 address

### 2. A Domain Name (You Already Have This!)

**Your Domain:** algotrendy.com (Namecheap)
- Already purchased ✅
- Ready to configure

## Deployment Steps

### Step 1: Set Up Your Server

#### Option A: DigitalOcean (Recommended for Beginners)

1. **Create Account & Droplet:**
   ```
   1. Sign up at digitalocean.com
   2. Click "Create" → "Droplets"
   3. Choose Ubuntu 22.04 LTS
   4. Select "Regular" plan - $12/month (2GB RAM, 2 CPUs)
   5. Choose datacenter region (closest to you)
   6. Add SSH key or use password
   7. Click "Create Droplet"
   ```

2. **Note Your Server IP:**
   - After creation, you'll see: `123.456.789.012`
   - Save this IP address!

3. **Connect to Server:**
   ```bash
   ssh root@123.456.789.012
   ```

#### Option B: AWS EC2

1. **Launch Instance:**
   ```
   1. Log into AWS Console
   2. Go to EC2 → Launch Instance
   3. Name: algotrendy-prod
   4. Ubuntu Server 22.04 LTS (Free tier eligible)
   5. Instance type: t3.medium (or t2.medium for testing)
   6. Create key pair (download .pem file)
   7. Network: Allow SSH (22), HTTP (80), HTTPS (443)
   8. Storage: 40 GB
   9. Launch instance
   ```

2. **Get Public IP:**
   - In EC2 dashboard, find your instance
   - Note the "Public IPv4 address"

3. **Connect:**
   ```bash
   chmod 400 your-key.pem
   ssh -i your-key.pem ubuntu@YOUR_IP
   ```

### Step 2: Install Docker on Server

Once connected to your server:

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo apt install docker-compose -y

# Verify installation
docker --version
docker-compose --version

# Start Docker on boot
sudo systemctl enable docker
```

### Step 3: Configure Firewall

```bash
# If using UFW (Ubuntu Firewall)
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
sudo ufw status

# If using AWS Security Groups:
# Add inbound rules in EC2 console:
# - SSH (22) from your IP
# - HTTP (80) from anywhere
# - HTTPS (443) from anywhere
```

### Step 4: Get Your Server IP Address

```bash
# On your server, run:
curl -4 ifconfig.me
```

**Example output:** `123.456.789.012`

Save this IP - you'll need it for DNS!

### Step 5: Configure DNS (Namecheap)

#### Get DNS Instructions

On your **local machine** (where AlgoTrendy code is):

```bash
cd /root/AlgoTrendy_v2.6
./scripts/show-dns-instructions.sh
```

#### Manual DNS Setup

1. **Log into Namecheap:**
   - Go to https://www.namecheap.com
   - Sign in to your account

2. **Navigate to DNS Settings:**
   ```
   Dashboard → Domain List → algotrendy.com → Manage → Advanced DNS
   ```

3. **Add A Records:**

   Click "Add New Record" and create these (replace `123.456.789.012` with your server IP):

   | Type | Host | Value              | TTL       |
   |------|------|--------------------|-----------|
   | A    | @    | 123.456.789.012    | Automatic |
   | A    | www  | 123.456.789.012    | Automatic |
   | A    | api  | 123.456.789.012    | Automatic |
   | A    | app  | 123.456.789.012    | Automatic |

4. **Save Changes**

5. **Wait 15-30 minutes** for DNS propagation

#### Verify DNS

```bash
# Check if DNS is working (after 15-30 minutes)
nslookup algotrendy.com
nslookup app.algotrendy.com
nslookup api.algotrendy.com
```

Should all return your server IP: `123.456.789.012`

**Check globally:** https://dnschecker.org/?host=algotrendy.com&type=A

### Step 6: Transfer Code to Server

#### Option A: Using Git (Recommended)

On your **server**:

```bash
# Install git if not present
sudo apt install git -y

# Clone repository
cd /opt
sudo git clone https://github.com/KenyBoi/algotrendy.git
sudo chown -R $USER:$USER algotrendy
cd algotrendy

# Checkout your branch
git checkout fix/cleanup-orphaned-files
```

#### Option B: Using SCP (from local machine)

On your **local machine**:

```bash
# Create tarball
cd /root
tar -czf algotrendy.tar.gz AlgoTrendy_v2.6/

# Transfer to server
scp algotrendy.tar.gz root@YOUR_SERVER_IP:/opt/

# On server: Extract
ssh root@YOUR_SERVER_IP
cd /opt
tar -xzf algotrendy.tar.gz
cd AlgoTrendy_v2.6
```

### Step 7: Configure Production Environment

On your **server**:

```bash
cd /opt/algotrendy  # or /opt/AlgoTrendy_v2.6

# Create production environment file
cp .env.production.template .env.production

# Edit with your credentials
nano .env.production
```

**Update these values:**

```env
# Database
QUESTDB_PASSWORD=your_strong_password_here

# Binance API (if using)
BINANCE_API_KEY=your_binance_api_key
BINANCE_API_SECRET=your_binance_secret
BINANCE_TESTNET=false
BINANCE_US=true

# Finnhub API (market data)
FINHUB_API_KEY=your_finnhub_api_key

# Bybit API (if using)
BYBIT_API_KEY=your_bybit_key
BYBIT_API_SECRET=your_bybit_secret
BYBIT_TESTNET=false

# OKX API (if using)
OKX_API_KEY=your_okx_key
OKX_API_SECRET=your_okx_secret
OKX_PASSPHRASE=your_okx_passphrase

# Allowed domains for CORS
ALLOWED_ORIGINS=https://algotrendy.com,https://www.algotrendy.com,https://app.algotrendy.com,https://api.algotrendy.com

# Market data fetch interval (seconds)
MARKET_DATA_FETCH_INTERVAL=60
```

**Save:** Ctrl+X, Y, Enter

**Secure the file:**
```bash
chmod 600 .env.production
```

### Step 8: Set Up SSL Certificates

**IMPORTANT:** Only run this AFTER DNS is propagated (Step 5)!

On your **server**:

```bash
cd /opt/algotrendy  # or /opt/AlgoTrendy_v2.6

# Update email in SSL script (optional)
nano scripts/setup-ssl.sh
# Change: EMAIL="your-email@domain.com"

# Run SSL setup
sudo ./scripts/setup-ssl.sh
```

**What this script does:**
- Verifies DNS is configured correctly
- Requests SSL certificates from Let's Encrypt
- Configures nginx to use the certificates
- Sets up automatic certificate renewal

**Expected Output:**
```
✓ algotrendy.com resolves to 123.456.789.012
✓ www.algotrendy.com resolves to 123.456.789.012
✓ api.algotrendy.com resolves to 123.456.789.012
✓ app.algotrendy.com resolves to 123.456.789.012
✓ SSL certificates obtained successfully!
✓ Nginx configuration updated

Your website is now accessible at:
  - https://algotrendy.com
  - https://www.algotrendy.com
  - https://api.algotrendy.com
  - https://app.algotrendy.com
```

**If it fails:**
- Wait longer for DNS (try `nslookup algotrendy.com`)
- Check firewall allows ports 80 and 443
- Check domain actually resolves to your server IP

### Step 9: Deploy to Production

On your **server**:

```bash
cd /opt/algotrendy  # or /opt/AlgoTrendy_v2.6

# Deploy with production configuration
docker-compose --env-file .env.production -f docker-compose.prod.yml up -d --build

# Check status
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f
```

**Wait 2-3 minutes for all services to start**

### Step 10: Verify Deployment

```bash
# Check health endpoint
curl https://api.algotrendy.com/health
# Should return: Healthy

# Check frontend
curl https://app.algotrendy.com
# Should return HTML

# Check API
curl https://api.algotrendy.com/api/health
# Should return API health data

# Check all services
docker-compose -f docker-compose.prod.yml ps
# All should be "Up" and "healthy"
```

**Test in Browser:**
1. Open https://app.algotrendy.com
2. You should see the Login page
3. Enter any username/password (demo mode)
4. You should see the Dashboard

## Your URLs

After successful deployment:

| Service   | URL                              | Purpose                     |
|-----------|----------------------------------|-----------------------------|
| Frontend  | https://app.algotrendy.com       | Trading interface           |
| API       | https://api.algotrendy.com/api   | Backend API endpoints       |
| Health    | https://api.algotrendy.com/health| API health check            |
| Swagger   | https://api.algotrendy.com/swagger| API documentation (dev)    |
| WebSocket | wss://api.algotrendy.com/hubs/market | Real-time market data  |

## Troubleshooting

### 1. DNS Not Resolving

**Problem:** `nslookup algotrendy.com` doesn't show your server IP

**Solution:**
```bash
# Wait 15-30 minutes after DNS changes
# Check propagation: https://dnschecker.org

# Verify DNS records in Namecheap:
# - @ should point to YOUR_SERVER_IP
# - www should point to YOUR_SERVER_IP
# - api should point to YOUR_SERVER_IP
# - app should point to YOUR_SERVER_IP
```

### 2. SSL Certificate Fails

**Problem:** `setup-ssl.sh` script fails

**Solution:**
```bash
# Check DNS first
nslookup algotrendy.com
# Must return your server IP!

# Check ports are open
sudo ufw status
# 80 and 443 must be allowed

# Check nginx is running
docker ps | grep nginx

# Try manual certificate request
docker run --rm \
  -v "$(pwd)/certbot/conf:/etc/letsencrypt" \
  -v "$(pwd)/certbot/www:/var/www/certbot" \
  certbot/certbot certonly --dry-run \
  --webroot --webroot-path=/var/www/certbot \
  --email your-email@domain.com \
  --agree-tos -d algotrendy.com
```

### 3. Containers Won't Start

**Problem:** Services fail to start

**Solution:**
```bash
# Check logs
docker-compose -f docker-compose.prod.yml logs

# Common issues:
# - Port already in use: sudo lsof -i :80
# - Out of memory: free -h
# - Environment vars: cat .env.production

# Restart services
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

### 4. Frontend Shows 502 Bad Gateway

**Problem:** Can't access https://app.algotrendy.com

**Solution:**
```bash
# Check frontend container
docker ps | grep frontend

# Check logs
docker logs algotrendy-frontend-prod

# Restart frontend
docker-compose -f docker-compose.prod.yml restart frontend nginx

# Check nginx configuration
docker exec algotrendy-nginx-prod nginx -t
```

### 5. API Returns 500 Errors

**Problem:** Backend API errors

**Solution:**
```bash
# Check API logs
docker logs algotrendy-api-prod

# Common issues:
# - Database not ready: wait 1-2 minutes
# - Missing API keys: check .env.production
# - CORS issues: check ALLOWED_ORIGINS

# Check database
docker exec algotrendy-questdb-prod curl localhost:9000

# Restart API
docker-compose -f docker-compose.prod.yml restart api
```

## Monitoring & Maintenance

### View Logs

```bash
# All services
docker-compose -f docker-compose.prod.yml logs -f

# Specific service
docker-compose -f docker-compose.prod.yml logs -f api
docker-compose -f docker-compose.prod.yml logs -f frontend
docker-compose -f docker-compose.prod.yml logs -f nginx
```

### Check Service Health

```bash
# Quick status
docker-compose -f docker-compose.prod.yml ps

# Detailed health
curl https://api.algotrendy.com/health
curl https://app.algotrendy.com/health

# Database
curl http://localhost:9000
```

### Update Application

```bash
cd /opt/algotrendy

# Pull latest code
git pull origin fix/cleanup-orphaned-files

# Rebuild and restart
docker-compose -f docker-compose.prod.yml up -d --build

# View logs
docker-compose -f docker-compose.prod.yml logs -f
```

### Backup Database

```bash
# Manual backup
./scripts/backup_questdb.sh

# Backups are saved to: database/backups/
```

### Restart Services

```bash
# Restart all
docker-compose -f docker-compose.prod.yml restart

# Restart specific service
docker-compose -f docker-compose.prod.yml restart api
docker-compose -f docker-compose.prod.yml restart frontend
docker-compose -f docker-compose.prod.yml restart nginx
```

### Stop Services

```bash
# Stop all (keeps data)
docker-compose -f docker-compose.prod.yml down

# Stop all and remove volumes (DELETES DATA!)
docker-compose -f docker-compose.prod.yml down -v
```

## Security Checklist

After deployment, ensure:

- [ ] Changed default passwords in `.env.production`
- [ ] SSL certificates are working (https://)
- [ ] Firewall only allows ports 22, 80, 443
- [ ] Database not exposed to internet (only localhost)
- [ ] API keys are secure and not in git
- [ ] Regular backups configured
- [ ] Docker containers running as non-root where possible
- [ ] Security updates enabled: `sudo apt install unattended-upgrades`

## Cost Estimate

### Server Costs (Monthly)

| Provider      | Plan         | RAM  | CPU | Storage | Cost   |
|---------------|--------------|------|-----|---------|--------|
| DigitalOcean  | Basic        | 2GB  | 2   | 50GB    | $12    |
| DigitalOcean  | Recommended  | 4GB  | 2   | 80GB    | $24    |
| Linode        | Nanode       | 1GB  | 1   | 25GB    | $5     |
| Vultr         | Regular      | 2GB  | 1   | 55GB    | $12    |
| AWS EC2       | t3.medium    | 4GB  | 2   | 40GB    | $30-40 |
| Hetzner       | CX11         | 2GB  | 1   | 20GB    | €4.5   |

### Domain Costs

- **Namecheap:** ~$13/year (already purchased ✅)

### Total Estimated Cost

**Minimum:** $5-12/month + domain
**Recommended:** $24/month + domain
**Annual:** ~$150-300/year

## Next Steps

After successful deployment:

1. **Test All Features:**
   - Login at https://app.algotrendy.com
   - Place test orders
   - Check positions update in real-time
   - Run backtests

2. **Connect Real Brokers:**
   - Update API keys in `.env.production`
   - Switch from testnet to live
   - Test with small amounts first

3. **Set Up Monitoring:**
   - Configure alerts for downtime
   - Monitor API usage
   - Track database size

4. **Enable Analytics (Optional):**
   - Google Analytics
   - Error tracking (Sentry)
   - Performance monitoring

## Support & Documentation

- **Deployment Issues:** Check `docs/deployment/` directory
- **DNS Help:** `docs/deployment/dns/NAMECHEAP_DNS_SETUP.md`
- **SSL Issues:** Re-run `./scripts/setup-ssl.sh`
- **Database Backups:** `./scripts/backup_questdb.sh`

## Quick Reference Commands

```bash
# Deploy
docker-compose --env-file .env.production -f docker-compose.prod.yml up -d

# Status
docker-compose -f docker-compose.prod.yml ps

# Logs
docker-compose -f docker-compose.prod.yml logs -f

# Restart
docker-compose -f docker-compose.prod.yml restart

# Stop
docker-compose -f docker-compose.prod.yml down

# Update
git pull && docker-compose -f docker-compose.prod.yml up -d --build

# Backup
./scripts/backup_questdb.sh

# Health Check
curl https://api.algotrendy.com/health
```

---

**Your AlgoTrendy v2.6 deployment is ready for the web!** 🚀

**Domain:** algotrendy.com
**Server:** Your VPS/Cloud provider
**Stack:** React + .NET 8 + QuestDB + Nginx
**Deployment:** Docker Compose (Production)
