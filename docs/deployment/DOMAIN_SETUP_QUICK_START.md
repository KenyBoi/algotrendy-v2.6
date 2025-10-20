# AlgoTrendy.com - Quick Start Guide

## What's Been Configured

Your AlgoTrendy v2.6 backend has been configured for the **algotrendy.com** domain:

✅ Backend CORS updated to allow algotrendy.com domains
✅ Nginx configured for domain routing
✅ SSL/HTTPS setup scripts created
✅ Production docker-compose updated
✅ DNS setup instructions prepared
✅ Production environment template created

## URLs You'll Use

### For Figma Design (Development)
```
http://localhost:5298/api
```

### For Production (After Deployment)
```
https://api.algotrendy.com/api
```

## Quick Setup Steps

### 1. Show DNS Instructions
```bash
cd /root/AlgoTrendy_v2.6
./scripts/show-dns-instructions.sh
```

### 2. Configure DNS on Namecheap
Follow the displayed instructions to add A records for:
- algotrendy.com
- www.algotrendy.com
- api.algotrendy.com
- app.algotrendy.com

### 3. Wait for DNS Propagation (15-30 minutes)
Check with:
```bash
nslookup algotrendy.com
```

### 4. Set Up SSL Certificates
```bash
sudo ./scripts/setup-ssl.sh
```

### 5. Configure Production Environment
```bash
cp .env.production.template .env.production
nano .env.production  # Edit with your API keys
chmod 600 .env.production
```

### 6. Deploy to Production
```bash
docker-compose --env-file .env.production -f docker-compose.prod.yml up -d
```

### 7. Verify Deployment
```bash
curl https://api.algotrendy.com/health
```

## For Your Figma Design

**Right Now (Development):**
Use: `http://localhost:5298/api`

**After Deployment (Production):**
Use: `https://api.algotrendy.com/api`

## Key Files Created

| File | Purpose |
|------|---------|
| `scripts/show-dns-instructions.sh` | Display DNS setup guide |
| `scripts/setup-ssl.sh` | Automated SSL certificate setup |
| `docs/NAMECHEAP_DNS_SETUP.md` | Detailed Namecheap DNS guide |
| `docs/DOMAIN_DEPLOYMENT_GUIDE.md` | Complete deployment walkthrough |
| `.env.production.template` | Production environment template |

## Important Security Notes

🔒 **Never commit these files to git:**
- `.env.production` (already in .gitignore)
- Any files containing API keys or passwords

🔒 **Keep template files in git:**
- `.env.production.template` ✅ (committed as example)
- `.env.example` ✅

## Need Help?

📖 **Detailed Guides:**
- DNS Setup: `docs/NAMECHEAP_DNS_SETUP.md`
- Full Deployment: `docs/DOMAIN_DEPLOYMENT_GUIDE.md`

🔧 **Scripts:**
- DNS Instructions: `./scripts/show-dns-instructions.sh`
- SSL Setup: `./scripts/setup-ssl.sh`

## What to Do Next

1. **Continue Figma design** using `http://localhost:5298/api`
2. **When ready to deploy:**
   - Follow DNS setup instructions
   - Run SSL setup script
   - Deploy with docker-compose
3. **Update Figma** to use `https://api.algotrendy.com/api`

---

**Your domain:** algotrendy.com
**Setup date:** 2025-10-20
**Version:** AlgoTrendy v2.6
