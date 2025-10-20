# AlgoTrendy v2.6 - Multi-Broker Production Deployment Checklist

**Date:** October 19, 2025
**Version:** 2.6.0
**Status:** Ready for Multi-Broker Production Deployment
**Brokers:** Binance, Bybit, TradeStation, NinjaTrader, Interactive Brokers

---

## 📋 Pre-Deployment Phase

### Environment Validation
- [ ] Verify Docker and Docker Compose installed: `docker --version && docker-compose --version`
- [ ] Check available disk space: minimum 10GB recommended
- [ ] Verify network connectivity to exchanges (Binance, OKX, Coinbase, Kraken)
- [ ] Confirm Linux kernel version ≥ 5.0 (for optimal container performance)

### Credentials & Secrets
- [ ] **Binance API Keys:** Obtained from https://www.binance.com/en/account/api-management
  - [ ] API Key ready (32+ alphanumeric characters)
  - [ ] Secret Key ready (secure, not shared)
  - [ ] IP whitelist configured (allow your VPS IP)
  - [ ] Testnet credentials (if using testnet: https://testnet.binance.vision)
- [ ] **DuckDNS Domain:** (if using dynamic DNS)
  - [ ] Domain registered (e.g., `algotrendy.duckdns.org`)
  - [ ] Token obtained for updates
- [ ] **SSL/TLS Certificates:** (choose one)
  - [ ] Option A: Let's Encrypt (recommended for production)
  - [ ] Option B: Self-signed (for testing only)
  - [ ] Option C: Existing certificate (provide paths)

### Infrastructure Preparation
- [ ] VPS created and accessible via SSH
- [ ] Firewall configured to allow ports:
  - [ ] 80 (HTTP)
  - [ ] 443 (HTTPS)
  - [ ] 22 (SSH)
- [ ] Non-root user created for deployment
- [ ] System packages updated: `sudo apt update && sudo apt upgrade`
- [ ] Required packages installed: `git`, `curl`, `wget`, `nano`

---

## 🏦 Multi-Broker Credential Setup (NEW)

### Option A: Retrieve from GCP Secret Manager (Recommended)

- [ ] **Provide GCP Service Account JSON**
  ```bash
  # Place service account key at:
  /root/gcp-credentials.json

  # Or set environment variable:
  export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"
  ```

- [ ] **Set GCP Project ID**
  ```bash
  export GCP_PROJECT_ID="your-gcp-project-id"
  ```

- [ ] **Run Credential Retrieval Script**
  ```bash
  cd /root/AlgoTrendy_v2.6
  python3 scripts/retrieve_gcp_secrets.py
  ```
  - Expected: Credentials written to `.env` file
  - Verify: `grep -v "your_.*_here" .env | grep "API_KEY"`

### Option B: Manual Credential Entry

- [ ] **Add Bybit Credentials** (CRITICAL - restored from v2.5)
  ```bash
  BYBIT_API_KEY=<actual_api_key>
  BYBIT_API_SECRET=<actual_secret>
  BYBIT_TESTNET=true  # Set to false for production
  ```

- [ ] **Add TradeStation Credentials**
  ```bash
  TRADESTATION_API_KEY=<actual_client_id>
  TRADESTATION_API_SECRET=<actual_client_secret>
  TRADESTATION_ACCOUNT_ID=<actual_account_id>
  TRADESTATION_USE_PAPER=true  # Set to false for production
  ```

- [ ] **Add NinjaTrader Credentials**
  ```bash
  NINJATRADER_USERNAME=<actual_username>
  NINJATRADER_PASSWORD=<actual_password>
  NINJATRADER_ACCOUNT_ID=<actual_account_id>
  NINJATRADER_CONNECTION_TYPE=REST
  NINJATRADER_HOST=localhost
  NINJATRADER_PORT=36973
  ```

- [ ] **Add Interactive Brokers Credentials**
  ```bash
  IBKR_USERNAME=<actual_username>
  IBKR_PASSWORD=<actual_password>
  IBKR_ACCOUNT_ID=<actual_account_id>
  IBKR_GATEWAY_HOST=localhost
  IBKR_GATEWAY_PORT=4002  # 4002 for paper, 4001 for live
  IBKR_CLIENT_ID=1
  IBKR_USE_PAPER=true  # Set to false for production
  ```

### Verify Broker-Specific Prerequisites

- [ ] **Binance:**
  - [ ] API key has trading permissions enabled
  - [ ] IP whitelist configured (if using IP restrictions)
  - [ ] For Binance US: Verify `Binance__UseBinanceUS=true` in .env

- [ ] **Bybit:**
  - [ ] Unified Trading Account created
  - [ ] API key has contract trading permissions
  - [ ] Testnet account for testing: https://testnet.bybit.com

- [ ] **TradeStation:**
  - [ ] OAuth app registered in TradeStation portal
  - [ ] Redirect URIs configured (if using OAuth flow)
  - [ ] Paper trading account accessible

- [ ] **NinjaTrader:**
  - [ ] NinjaTrader 8 platform installed (if using locally)
  - [ ] Automated Trading Interface (ATI) enabled
  - [ ] Listening on port 36973
  - [ ] Sim account configured for testing

- [ ] **Interactive Brokers:**
  - [ ] TWS or IB Gateway installed
  - [ ] API connections enabled in TWS/Gateway settings
  - [ ] Paper trading account for testing
  - [ ] Client ID configured (default: 1)

---

## 🔧 Configuration Phase

### Environment Variables Setup
- [ ] Create `.env` file from `.env.example`:
  ```bash
  cp /root/AlgoTrendy_v2.6/.env.example /root/AlgoTrendy_v2.6/.env
  ```
- [ ] Edit `.env` with production values:
  - [ ] `QUESTDB_ADMIN_USER=admin` (keep default or change)
  - [ ] `QUESTDB_ADMIN_PASSWORD=<secure_password>` (change from default!)
  - [ ] `API_LOG_LEVEL=Warning` (set to Warning for production)
  - [ ] `BINANCE_API_KEY=<your_key>`
  - [ ] `BINANCE_API_SECRET=<your_secret>`
  - [ ] `BINANCE_USE_TESTNET=false` (set to false for production)
  - [ ] `QUESTDB_PORT=8812` (keep default)
  - [ ] `API_PORT=5002` (keep default)
- [ ] Verify `.env` is in `.gitignore` (should not be committed)
- [ ] Secure file permissions: `chmod 600 /root/AlgoTrendy_v2.6/.env`

### SSL/TLS Certificate Setup
**Option A: Let's Encrypt (Recommended)**
```bash
# Install certbot
sudo apt install certbot python3-certbot-nginx

# Generate certificate
sudo certbot certonly --standalone -d algotrendy.duckdns.org

# Certificate will be at: /etc/letsencrypt/live/algotrendy.duckdns.org/
# Copy to project: sudo cp /etc/letsencrypt/live/algotrendy.duckdns.org/fullchain.pem ssl/cert.pem
# Copy to project: sudo cp /etc/letsencrypt/live/algotrendy.duckdns.org/privkey.pem ssl/key.pem
# Fix permissions: sudo chown $USER:$USER ssl/*.pem
```

**Option B: Self-Signed (Testing Only)**
```bash
openssl req -x509 -newkey rsa:2048 -keyout ssl/key.pem -out ssl/cert.pem -days 365 -nodes
```

- [ ] Certificates placed at:
  - [ ] `/root/AlgoTrendy_v2.6/ssl/cert.pem`
  - [ ] `/root/AlgoTrendy_v2.6/ssl/key.pem`
- [ ] Permissions set: `chmod 600 ssl/*.pem`
- [ ] Test certificate validity: `openssl x509 -in ssl/cert.pem -text -noout`

### Docker Configuration
- [ ] Review `docker-compose.prod.yml` for production settings
- [ ] Update resource limits if needed:
  ```yaml
  services:
    api:
      deploy:
        resources:
          limits:
            cpus: '2'        # Adjust based on CPU cores
            memory: 2G       # Adjust based on available RAM
  ```
- [ ] Configure log rotation (already configured in prod file)
- [ ] Backup database volume path verified

---

## 🚀 Deployment Phase

### Initial Deployment
- [ ] **Build Docker image:**
  ```bash
  cd /root/AlgoTrendy_v2.6
  docker build -f backend/Dockerfile -t algotrendy-api:v2.6 .
  ```
  - Expected time: 20-30 seconds
  - Expected final image size: ~245MB
  - Check result: `docker images | grep algotrendy-api`

- [ ] **Start services with production config:**
  ```bash
  cd /root/AlgoTrendy_v2.6
  docker-compose -f docker-compose.prod.yml up -d
  ```
  - Expected output: 3 services created and started
  - Check status: `docker-compose ps`

- [ ] **Verify services are running:**
  ```bash
  docker-compose ps
  # Expected: api (Up), questdb (Up), nginx (Up)
  ```

- [ ] **Check service logs:**
  ```bash
  docker-compose logs -f api
  # Should show: "AlgoTrendy API v2.6 starting..." + "Connected to Binance"

  docker-compose logs questdb
  # Should show: "server started successfully"
  ```

### Connectivity Validation
- [ ] **Test API health endpoint:**
  ```bash
  curl http://localhost:5002/health
  # Expected response: "Healthy"
  ```

- [ ] **Test Nginx reverse proxy:**
  ```bash
  curl -I http://localhost:80/health
  # Expected response: HTTP/1.1 301 (redirect to HTTPS)

  curl -I https://localhost:443/health
  # Expected response: HTTP/1.1 200 OK (may show self-signed warning)
  ```

- [ ] **Test QuestDB connectivity:**
  ```bash
  curl http://localhost:9000
  # Expected response: HTML home page or 405 error (both OK)
  ```

- [ ] **Test from remote host:**
  ```bash
  curl https://algotrendy.duckdns.org/health
  # Expected: "Healthy" (may show certificate warning initially)
  ```

### Data Verification
- [ ] **Check market data is being fetched:**
  ```bash
  # Wait 2-3 minutes for first fetch cycle
  curl http://localhost:5002/api/market-data/binance/btcusdt
  # Should return JSON array with market data
  ```

- [ ] **Verify database contains data:**
  ```bash
  # Access QuestDB web console at http://localhost:9000
  # Run SQL: SELECT COUNT(*) FROM market_data;
  # Should show > 0 records after 60 seconds
  ```

- [ ] **Check Binance broker connection:**
  ```bash
  docker-compose logs api | grep -i binance
  # Should see: "Binance broker configured for TESTNET" or "PRODUCTION"
  ```

---

## 📊 Post-Deployment Validation

### Performance Monitoring
- [ ] **Check resource usage:**
  ```bash
  docker stats
  # API container: CPU < 1%, Memory < 300MB expected at idle
  # QuestDB: CPU 5-15%, Memory 600MB-1GB expected
  ```

- [ ] **Monitor for crashes:**
  ```bash
  # Run for 5 minutes minimum
  watch -n 1 'docker-compose ps'
  # All services should maintain "Up" status
  ```

- [ ] **Check disk usage:**
  ```bash
  df -h
  docker system df
  # Verify plenty of disk space remaining
  ```

### Functional Testing
- [ ] **Market data ingestion:**
  - [ ] Wait 60+ seconds
  - [ ] Check API returns data from all 4 exchanges
  - [ ] Verify timestamps are recent (within last minute)

- [ ] **Order placement (Testnet only):**
  - [ ] Call POST /api/orders with test credentials
  - [ ] Verify order created with ID
  - [ ] Check order appears in Binance testnet account

- [ ] **Multi-Broker Integration Testing (NEW):**
  - [ ] Test Bybit connection:
    ```bash
    dotnet test --filter "Broker=Bybit&Category=Integration" --verbosity detailed
    ```
  - [ ] Test TradeStation connection:
    ```bash
    dotnet test --filter "Broker=TradeStation&Category=Integration" --verbosity detailed
    ```
  - [ ] Test NinjaTrader connection (requires NT8 running):
    ```bash
    dotnet test --filter "Broker=NinjaTrader&Category=Integration" --verbosity detailed
    ```
  - [ ] Test Interactive Brokers connection (requires TWS/Gateway running):
    ```bash
    dotnet test --filter "Broker=InteractiveBrokers&Category=Integration" --verbosity detailed
    ```

- [ ] **Strategy signal generation:**
  - [ ] Momentum strategy should generate signals
  - [ ] RSI strategy should generate signals
  - [ ] Confidence scores between 0-1

- [ ] **Real-time streaming (SignalR):**
  - [ ] Connect WebSocket client to `/hubs/marketdata`
  - [ ] Should receive market data updates

### Security Validation
- [ ] **Check no secrets in logs:**
  ```bash
  docker-compose logs | grep -i "password\|secret\|key"
  # Should return no results
  ```

- [ ] **Verify SSL certificate:**
  ```bash
  curl -v https://algotrendy.duckdns.org/health 2>&1 | grep -i "certificate"
  # Should show certificate details
  ```

- [ ] **Test firewall rules:**
  ```bash
  # From different machine:
  telnet algotrendy.duckdns.org 22  # Should fail (SSH blocked)
  telnet algotrendy.duckdns.org 80  # Should connect (HTTP allowed)
  ```

- [ ] **Verify Docker security:**
  - [ ] Non-root user running containers: `docker-compose ps | grep "root"` (should be empty)
  - [ ] Read-only filesystems where possible (already configured)

---

## 🔄 Operational Setup

### Backup Configuration
- [ ] **Daily database backups enabled:**
  ```bash
  # Backup script already in infrastructure/scripts/backup.sh
  # Set up cron job:
  (crontab -l 2>/dev/null; echo "0 2 * * * /path/to/backup.sh") | crontab -
  ```

- [ ] **Backup location verified:**
  - [ ] Location: `/root/AlgoTrendy_v2.6/backups/`
  - [ ] Space available: minimum 50GB recommended
  - [ ] Retention policy: keep last 7 days

- [ ] **Test restore procedure:**
  ```bash
  # Documented in /root/AlgoTrendy_v2.6/DEPLOYMENT_DOCKER.md
  # Should be able to restore from backup within 15 minutes
  ```

### Monitoring & Alerting
- [ ] **Log aggregation:**
  - [ ] Logs accessible via: `docker-compose logs api`
  - [ ] Consider: ELK stack, Splunk, or DataDog for production

- [ ] **Health checks:**
  - [ ] API health: `/health` endpoint every 30 seconds
  - [ ] Database health: QuestDB status every 60 seconds
  - [ ] Market data freshness: last data within 2 minutes

- [ ] **Alerts configured for:**
  - [ ] Service down (email/Slack)
  - [ ] High CPU/Memory usage
  - [ ] Failed database connection
  - [ ] SSL certificate expiry (30 days before)

### Maintenance Windows
- [ ] **Scheduled maintenance time:** [Specify time, e.g., 3-4 AM UTC]
- [ ] **Notification sent to users** before maintenance
- [ ] **Rollback plan documented** for failed deployments

---

## 🆘 Troubleshooting Quick Reference

| Issue | Solution |
|-------|----------|
| Container won't start | `docker-compose logs api` to see error |
| Port already in use | `sudo lsof -i :80` to find culprit |
| No market data | Wait 60+ seconds for first fetch, check Binance connectivity |
| API returns 500 | Check QuestDB connection, verify credentials |
| SSL certificate error | Verify paths: `ls -la ssl/` |
| Out of disk space | `docker system prune` to clean up old images |
| Memory leak | Check logs for specific process, restart service |

---

## 📋 Sign-Off

- [ ] **Deployer Name:** _______________
- [ ] **Date:** _______________
- [ ] **Environment:** ☐ Staging ☐ Production
- [ ] **All items checked:** ☐ Yes
- [ ] **Issues found and documented:** ☐ Yes ☐ No (if yes, list below)

**Issues Found:**
```
[List any issues encountered during deployment]
```

**Approval:**
- [ ] Technical Lead Approval: _______________
- [ ] Operations Approval: _______________

---

## 📞 Support & Documentation

- **Deployment Guide:** `/root/AlgoTrendy_v2.6/DEPLOYMENT_DOCKER.md`
- **Architecture Overview:** `/root/AlgoTrendy_v2.6/PROJECT_OVERVIEW.md`
- **API Documentation:** `http://localhost:5002/swagger` (development only)
- **Emergency Contact:** [Specify on-call support contact]

---

**Last Updated:** October 18, 2025
**Version:** 2.6.0
**Maintenance:** Review and update quarterly
