# AlgoTrendy.com Domain Deployment Guide

Complete guide to deploying AlgoTrendy v2.6 to production with your algotrendy.com domain.

## Overview

This guide covers:
1. DNS configuration on Namecheap
2. SSL certificate setup with Let's Encrypt
3. Production deployment with Docker
4. Frontend configuration

## Prerequisites

- [x] algotrendy.com domain purchased on Namecheap
- [ ] Server with public IP address
- [ ] Docker and docker-compose installed
- [ ] Ports 80 and 443 open on firewall
- [ ] Root/sudo access to server

## Step-by-Step Deployment

### Step 1: Get Your Server IP Address

On your server, run:
```bash
curl -4 ifconfig.me
```

Save this IP address - you'll need it for DNS configuration.

### Step 2: Configure DNS on Namecheap

Display DNS instructions:
```bash
cd /root/AlgoTrendy_v2.6
./scripts/show-dns-instructions.sh
```

Or follow the detailed guide:
- See [NAMECHEAP_DNS_SETUP.md](./NAMECHEAP_DNS_SETUP.md)

**Summary:**
1. Log into Namecheap.com
2. Go to Domain List > algotrendy.com > Manage > Advanced DNS
3. Add these A records (replace with your server IP):

| Type | Host | Value              | TTL       |
|------|------|--------------------|-----------|
| A    | @    | YOUR_SERVER_IP     | Automatic |
| A    | www  | YOUR_SERVER_IP     | Automatic |
| A    | api  | YOUR_SERVER_IP     | Automatic |
| A    | app  | YOUR_SERVER_IP     | Automatic |

4. Save all changes
5. Wait 15-30 minutes for DNS propagation

### Step 3: Verify DNS Propagation

Check if DNS is working:
```bash
nslookup algotrendy.com
nslookup www.algotrendy.com
nslookup api.algotrendy.com
nslookup app.algotrendy.com
```

All should return your server's IP address.

Or check globally: https://dnschecker.org

### Step 4: Configure Production Environment

Create production environment file:
```bash
cd /root/AlgoTrendy_v2.6
cp .env.production.template .env.production
```

Edit the file with your credentials:
```bash
nano .env.production
```

**Required settings to update:**
- `QUESTDB_PASSWORD` - Strong database password
- `BINANCE_API_KEY` and `BINANCE_API_SECRET` - Your Binance credentials
- `BYBIT_API_KEY` and `BYBIT_API_SECRET` - Your Bybit credentials
- Other broker API keys as needed
- Market data provider API keys

Save and exit (Ctrl+X, Y, Enter)

Secure the file:
```bash
chmod 600 .env.production
```

### Step 5: Set Up SSL Certificates

Run the SSL setup script:
```bash
cd /root/AlgoTrendy_v2.6
sudo ./scripts/setup-ssl.sh
```

This script will:
1. Check DNS configuration
2. Request SSL certificates from Let's Encrypt
3. Update nginx configuration
4. Restart services with HTTPS enabled

**Expected output:**
```
✓ algotrendy.com resolves to 123.456.789.012
✓ www.algotrendy.com resolves to 123.456.789.012
✓ api.algotrendy.com resolves to 123.456.789.012
✓ app.algotrendy.com resolves to 123.456.789.012
✓ SSL certificates obtained successfully!
✓ Nginx configuration updated
```

### Step 6: Start Production Services

Start all services:
```bash
docker-compose --env-file .env.production -f docker-compose.prod.yml up -d
```

Check service status:
```bash
docker-compose -f docker-compose.prod.yml ps
```

All services should show "Up" status:
- algotrendy-questdb-prod
- algotrendy-api-prod
- algotrendy-nginx-prod
- algotrendy-certbot

### Step 7: Verify Deployment

Test each endpoint:

**1. Main domain:**
```bash
curl -I https://algotrendy.com
```

**2. API endpoint:**
```bash
curl https://api.algotrendy.com/health
```

Should return: `healthy`

**3. Swagger UI (Development/Testing):**
Open in browser: https://api.algotrendy.com/swagger

**4. Check SSL certificate:**
```bash
echo | openssl s_client -servername algotrendy.com -connect algotrendy.com:443 2>/dev/null | openssl x509 -noout -dates
```

Should show valid dates and Let's Encrypt issuer.

### Step 8: Configure Frontend

Update your Figma design or frontend application to use:

**API Base URL:**
```
https://api.algotrendy.com/api
```

**WebSocket URL (SignalR):**
```
https://api.algotrendy.com/hubs/marketdata
```

**Example API Endpoints:**
- Market Data: `https://api.algotrendy.com/api/marketdata/BTCUSDT`
- Place Order: `https://api.algotrendy.com/api/trading/orders`
- Portfolio: `https://api.algotrendy.com/api/portfolio/summary`

### Step 9: Monitor Services

View logs:
```bash
# API logs
docker logs -f algotrendy-api-prod

# Nginx logs
docker logs -f algotrendy-nginx-prod

# QuestDB logs
docker logs -f algotrendy-questdb-prod
```

Check health:
```bash
curl https://api.algotrendy.com/health
```

## Domain Architecture

Your setup will use the following structure:

```
https://algotrendy.com          → Main website/landing page
https://www.algotrendy.com      → Redirects to main
https://app.algotrendy.com      → Trading application frontend
https://api.algotrendy.com      → Backend API
https://api.algotrendy.com/api  → API endpoints
https://api.algotrendy.com/swagger → API documentation
```

## Security Checklist

- [x] SSL/TLS certificates installed (Let's Encrypt)
- [x] HTTPS enforced (HTTP redirects to HTTPS)
- [x] Strong database password set
- [x] Environment file secured (chmod 600)
- [x] CORS configured for your domains only
- [ ] Firewall configured (only ports 22, 80, 443 open)
- [ ] API rate limiting enabled
- [ ] Regular backups configured
- [ ] Monitoring and alerts set up

## Firewall Configuration

Ensure your firewall allows:
```bash
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP (for Let's Encrypt)
sudo ufw allow 443/tcp  # HTTPS
sudo ufw enable
```

## SSL Certificate Renewal

SSL certificates auto-renew via the certbot container every 12 hours.

To manually renew:
```bash
docker-compose -f docker-compose.prod.yml restart certbot
```

## Troubleshooting

### DNS Issues
- Check Namecheap DNS settings are correct
- Wait full 30 minutes for propagation
- Use `dnschecker.org` to check global propagation
- Clear local DNS cache

### SSL Certificate Issues
- Ensure DNS is fully propagated first
- Check ports 80 and 443 are accessible
- Review certbot logs: `docker logs algotrendy-certbot`
- Verify domain ownership

### CORS Errors
- Check ALLOWED_ORIGINS in .env.production
- Ensure using HTTPS (not HTTP)
- Review API logs for CORS rejections
- Verify frontend origin matches CORS whitelist

### API Not Responding
- Check container status: `docker ps`
- View API logs: `docker logs algotrendy-api-prod`
- Check QuestDB is running: `docker logs algotrendy-questdb-prod`
- Verify nginx config: `docker exec algotrendy-nginx-prod nginx -t`

### Container Issues
```bash
# Restart specific service
docker-compose -f docker-compose.prod.yml restart api

# Restart all services
docker-compose -f docker-compose.prod.yml restart

# Stop and rebuild
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build
```

## Updating the Application

To update to a new version:

1. Pull latest code:
```bash
cd /root/AlgoTrendy_v2.6
git pull
```

2. Rebuild and restart:
```bash
docker-compose -f docker-compose.prod.yml down
docker-compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

3. Verify deployment:
```bash
curl https://api.algotrendy.com/health
```

## Backup and Restore

### Backup Database
```bash
# Coming soon - QuestDB backup procedure
```

### Backup Configuration
```bash
# Backup environment and SSL certificates
tar -czf algotrendy-backup-$(date +%Y%m%d).tar.gz \
  .env.production \
  certbot/conf \
  nginx.conf
```

## Performance Optimization

After deployment:
1. Monitor resource usage: `docker stats`
2. Adjust resource limits in docker-compose.prod.yml
3. Configure caching for market data
4. Set up CDN for static assets (if needed)

## Next Steps

1. Deploy frontend application to `app.algotrendy.com`
2. Set up monitoring (Grafana, Prometheus)
3. Configure automated backups
4. Set up logging aggregation (Seq, ELK)
5. Implement API authentication
6. Configure rate limiting per user/API key
7. Set up staging environment

## Support

For issues or questions:
- Review logs: `docker-compose -f docker-compose.prod.yml logs`
- Check documentation in `/docs` folder
- Review GitHub issues (if applicable)

---

**Deployment Checklist:**
- [ ] DNS configured on Namecheap
- [ ] DNS propagated (verified)
- [ ] Production .env file created
- [ ] SSL certificates obtained
- [ ] Services deployed and running
- [ ] Health check passing
- [ ] Swagger UI accessible
- [ ] Frontend configured with API URL
- [ ] Firewall configured
- [ ] Backups configured
- [ ] Monitoring set up

---

**Last Updated:** 2025-10-20
**AlgoTrendy Version:** v2.6
**Domain:** algotrendy.com
