#!/bin/bash
# ============================================================================
# AlgoTrendy v2.6 - SSL Certificate Setup Script
# ============================================================================
# This script sets up Let's Encrypt SSL certificates for algotrendy.com
#
# Prerequisites:
# 1. Domain DNS must be pointing to this server's IP address
# 2. Ports 80 and 443 must be accessible from the internet
# 3. Docker and docker-compose must be installed
# ============================================================================

set -e

# Configuration
DOMAIN="algotrendy.com"
WWW_DOMAIN="www.algotrendy.com"
API_DOMAIN="api.algotrendy.com"
APP_DOMAIN="app.algotrendy.com"
EMAIL="admin@algotrendy.com"  # Change this to your email

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}============================================================================${NC}"
echo -e "${GREEN}AlgoTrendy SSL Certificate Setup${NC}"
echo -e "${GREEN}============================================================================${NC}"

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root (use sudo)${NC}"
   exit 1
fi

# Create certbot directories
echo -e "${YELLOW}Creating certbot directories...${NC}"
mkdir -p certbot/www
mkdir -p certbot/conf

# Check if DNS is configured
echo -e "${YELLOW}Checking DNS configuration...${NC}"
for domain in $DOMAIN $WWW_DOMAIN $API_DOMAIN $APP_DOMAIN; do
    if host $domain > /dev/null 2>&1; then
        IP=$(host $domain | grep "has address" | awk '{print $4}' | head -1)
        echo -e "${GREEN}✓ $domain resolves to $IP${NC}"
    else
        echo -e "${RED}✗ $domain does not resolve. Please configure DNS first.${NC}"
        echo -e "${YELLOW}  Run: ./scripts/show-dns-instructions.sh${NC}"
        exit 1
    fi
done

# Stop any running containers
echo -e "${YELLOW}Stopping existing containers...${NC}"
docker-compose -f docker-compose.prod.yml down 2>/dev/null || true

# Start nginx temporarily for Let's Encrypt validation
echo -e "${YELLOW}Starting nginx for certificate validation...${NC}"
docker-compose -f docker-compose.prod.yml up -d nginx

# Wait for nginx to start
echo -e "${YELLOW}Waiting for nginx to start...${NC}"
sleep 5

# Request certificates
echo -e "${YELLOW}Requesting SSL certificates from Let's Encrypt...${NC}"
docker run --rm \
  -v "$(pwd)/certbot/conf:/etc/letsencrypt" \
  -v "$(pwd)/certbot/www:/var/www/certbot" \
  certbot/certbot certonly \
  --webroot \
  --webroot-path=/var/www/certbot \
  --email $EMAIL \
  --agree-tos \
  --no-eff-email \
  -d $DOMAIN \
  -d $WWW_DOMAIN \
  -d $API_DOMAIN \
  -d $APP_DOMAIN

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ SSL certificates obtained successfully!${NC}"

    # Update nginx configuration to use Let's Encrypt certificates
    echo -e "${YELLOW}Updating nginx configuration...${NC}"

    # Backup current nginx.conf
    cp nginx.conf nginx.conf.backup

    # Update nginx.conf to use Let's Encrypt certificates
    sed -i 's|ssl_certificate /etc/nginx/ssl/cert.pem;|ssl_certificate /etc/letsencrypt/live/algotrendy.com/fullchain.pem;|g' nginx.conf
    sed -i 's|ssl_certificate_key /etc/nginx/ssl/key.pem;|ssl_certificate_key /etc/letsencrypt/live/algotrendy.com/privkey.pem;|g' nginx.conf

    # Enable SSL stapling
    sed -i 's|# ssl_stapling on;|ssl_stapling on;|g' nginx.conf
    sed -i 's|# ssl_stapling_verify on;|ssl_stapling_verify on;|g' nginx.conf
    sed -i 's|# ssl_trusted_certificate /etc/nginx/ssl/chain.pem;|ssl_trusted_certificate /etc/letsencrypt/live/algotrendy.com/chain.pem;|g' nginx.conf

    echo -e "${GREEN}✓ Nginx configuration updated${NC}"

    # Restart nginx with new configuration
    echo -e "${YELLOW}Restarting services...${NC}"
    docker-compose -f docker-compose.prod.yml down
    docker-compose -f docker-compose.prod.yml up -d

    echo -e "${GREEN}============================================================================${NC}"
    echo -e "${GREEN}SSL Setup Complete!${NC}"
    echo -e "${GREEN}============================================================================${NC}"
    echo -e "${GREEN}Your website is now accessible at:${NC}"
    echo -e "${GREEN}  - https://algotrendy.com${NC}"
    echo -e "${GREEN}  - https://www.algotrendy.com${NC}"
    echo -e "${GREEN}  - https://api.algotrendy.com${NC}"
    echo -e "${GREEN}  - https://app.algotrendy.com${NC}"
    echo -e ""
    echo -e "${YELLOW}Note: Certificates will auto-renew via certbot container${NC}"
    echo -e "${GREEN}============================================================================${NC}"
else
    echo -e "${RED}✗ Failed to obtain SSL certificates${NC}"
    echo -e "${YELLOW}Please check:${NC}"
    echo -e "${YELLOW}  1. DNS is correctly configured${NC}"
    echo -e "${YELLOW}  2. Ports 80 and 443 are accessible${NC}"
    echo -e "${YELLOW}  3. No firewall is blocking Let's Encrypt${NC}"
    exit 1
fi
