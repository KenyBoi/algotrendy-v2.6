# AlgoTrendy v2.6 - Web Deployment Checklist

Quick reference checklist for deploying to production.

## Pre-Deployment

- [ ] Domain purchased: algotrendy.com ✅
- [ ] Server selected (DigitalOcean, AWS, etc.)
- [ ] Server requirements met (4GB RAM, 2 CPU, 40GB storage)
- [ ] API keys ready (Binance, Finnhub, etc.)

## Step 1: Server Setup (15 minutes)

- [ ] Create server/VPS
- [ ] Note server IP address: `_________________`
- [ ] Connect via SSH
- [ ] Install Docker: `curl -fsSL https://get.docker.com -o get-docker.sh && sudo sh get-docker.sh`
- [ ] Install Docker Compose: `sudo apt install docker-compose -y`
- [ ] Configure firewall (ports 22, 80, 443)

## Step 2: DNS Configuration (30 minutes)

- [ ] Log into Namecheap.com
- [ ] Go to: Domain List → algotrendy.com → Manage → Advanced DNS
- [ ] Add A record: `@` → Your Server IP
- [ ] Add A record: `www` → Your Server IP
- [ ] Add A record: `api` → Your Server IP
- [ ] Add A record: `app` → Your Server IP
- [ ] Save changes
- [ ] Wait 15-30 minutes for propagation
- [ ] Verify: `nslookup algotrendy.com` returns your server IP

## Step 3: Transfer Code (10 minutes)

Choose one method:

### Method A: Git Clone (Recommended)
- [ ] `cd /opt`
- [ ] `sudo git clone https://github.com/KenyBoi/algotrendy.git`
- [ ] `cd algotrendy`
- [ ] `git checkout fix/cleanup-orphaned-files`

### Method B: SCP Transfer
- [ ] Create tarball on local machine
- [ ] `scp algotrendy.tar.gz root@YOUR_IP:/opt/`
- [ ] Extract on server

## Step 4: Configure Environment (5 minutes)

- [ ] `cd /opt/algotrendy`
- [ ] `cp .env.production.template .env.production`
- [ ] `nano .env.production`
- [ ] Update: `QUESTDB_PASSWORD`
- [ ] Update: `BINANCE_API_KEY` and `BINANCE_API_SECRET`
- [ ] Update: `FINHUB_API_KEY`
- [ ] Update: `BYBIT_API_KEY` and `BYBIT_API_SECRET` (if using)
- [ ] Update: `OKX_API_KEY`, `OKX_API_SECRET`, `OKX_PASSPHRASE` (if using)
- [ ] Update: `ALLOWED_ORIGINS`
- [ ] Save (Ctrl+X, Y, Enter)
- [ ] `chmod 600 .env.production`

## Step 5: SSL Certificates (10 minutes)

**IMPORTANT: Only after DNS is propagated!**

- [ ] Verify DNS: `nslookup algotrendy.com` shows your IP
- [ ] Verify DNS: `nslookup app.algotrendy.com` shows your IP
- [ ] `sudo ./scripts/setup-ssl.sh`
- [ ] Wait for certificate generation
- [ ] Verify success message

## Step 6: Deploy (15 minutes)

- [ ] `docker-compose --env-file .env.production -f docker-compose.prod.yml up -d --build`
- [ ] Wait 2-3 minutes for containers to start
- [ ] Check status: `docker-compose -f docker-compose.prod.yml ps`
- [ ] All services should be "Up"

## Step 7: Verify (5 minutes)

### Command Line Tests
- [ ] `curl https://api.algotrendy.com/health` returns "Healthy"
- [ ] `curl https://app.algotrendy.com` returns HTML
- [ ] `docker-compose -f docker-compose.prod.yml ps` all "healthy"

### Browser Tests
- [ ] Open https://app.algotrendy.com
- [ ] See login page ✅
- [ ] Login with any credentials (demo mode)
- [ ] Dashboard loads ✅
- [ ] Navigate to Orders page ✅
- [ ] Navigate to Positions page ✅
- [ ] Navigate to Strategies page ✅

## Post-Deployment

- [ ] Bookmark admin URLs:
  - Frontend: https://app.algotrendy.com
  - API Health: https://api.algotrendy.com/health
  - QuestDB: http://YOUR_IP:9000

- [ ] Set up monitoring alerts (optional)
- [ ] Configure backup schedule
- [ ] Document admin procedures
- [ ] Test with small trades first

## Troubleshooting

If something fails, check:

### DNS Issues
```bash
nslookup algotrendy.com
# Should return your server IP
# If not, wait longer or check Namecheap DNS settings
```

### SSL Issues
```bash
sudo ufw status
# Ports 80 and 443 must be open
sudo ./scripts/setup-ssl.sh
# Re-run if DNS just propagated
```

### Container Issues
```bash
docker-compose -f docker-compose.prod.yml logs
# Check for errors in logs
docker-compose -f docker-compose.prod.yml restart
# Restart all services
```

### Frontend 502 Error
```bash
docker-compose -f docker-compose.prod.yml restart frontend nginx
# Restart frontend and nginx
```

## Deployment Complete! 🎉

Your application is now live at:
- **Frontend:** https://app.algotrendy.com
- **API:** https://api.algotrendy.com/api
- **Health:** https://api.algotrendy.com/health

## Quick Commands Reference

```bash
# View logs
docker-compose -f docker-compose.prod.yml logs -f

# Restart service
docker-compose -f docker-compose.prod.yml restart [service]

# Stop all
docker-compose -f docker-compose.prod.yml down

# Update code
cd /opt/algotrendy && git pull && docker-compose -f docker-compose.prod.yml up -d --build

# Backup database
./scripts/backup_questdb.sh
```

---

**Estimated Total Time:** 1.5 - 2 hours (including DNS propagation)

**Need Help?** See `WEB_DEPLOYMENT_GUIDE.md` for detailed instructions.
