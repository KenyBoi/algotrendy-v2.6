#!/bin/bash
# ============================================================================
# AlgoTrendy v2.6 - SSL Certificate Setup Script (Main Domains Only)
# ============================================================================
# This script sets up Let's Encrypt SSL certificates for configured domains
#
# Prerequisites:
# 1. Domain DNS must be pointing to this server's IP address
# 2. Ports 80 and 443 must be accessible from the internet
# 3. Docker and docker-compose must be installed
# ============================================================================

set -e

# Configuration
EMAIL="admin@algotrendy.com"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${GREEN}============================================================================${NC}"
echo -e "${GREEN}AlgoTrendy SSL Certificate Setup (Main Domains)${NC}"
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

# Check which domains are configured
echo -e "${YELLOW}Checking DNS configuration...${NC}"
CONFIGURED_DOMAINS=()

check_domain() {
    local domain=$1
    if host $domain > /dev/null 2>&1; then
        IP=$(host $domain | grep "has address" | awk '{print $4}' | head -1)
        echo -e "${GREEN}✓ $domain resolves to $IP${NC}"
        CONFIGURED_DOMAINS+=("$domain")
        return 0
    else
        echo -e "${YELLOW}⚠ $domain does not resolve (skipping)${NC}"
        return 1
    fi
}

# Check all possible domains
check_domain "algotrendy.com" || true
check_domain "www.algotrendy.com" || true
check_domain "api.algotrendy.com" || true
check_domain "app.algotrendy.com" || true

# Verify we have at least one domain
if [ ${#CONFIGURED_DOMAINS[@]} -eq 0 ]; then
    echo -e "${RED}✗ No domains are configured. Please set up DNS first.${NC}"
    echo -e "${YELLOW}  Run: ./scripts/show-dns-instructions.sh${NC}"
    exit 1
fi

echo -e ""
echo -e "${BLUE}Will request SSL certificates for ${#CONFIGURED_DOMAINS[@]} domain(s):${NC}"
for domain in "${CONFIGURED_DOMAINS[@]}"; do
    echo -e "${BLUE}  - $domain${NC}"
done
echo -e ""

# Build certbot command with only configured domains
CERTBOT_DOMAINS=""
for domain in "${CONFIGURED_DOMAINS[@]}"; do
    CERTBOT_DOMAINS="$CERTBOT_DOMAINS -d $domain"
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
  $CERTBOT_DOMAINS

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ SSL certificates obtained successfully!${NC}"

    # Update nginx configuration to use Let's Encrypt certificates
    echo -e "${YELLOW}Updating nginx configuration...${NC}"

    # Backup current nginx.conf
    cp nginx.conf nginx.conf.backup.$(date +%Y%m%d_%H%M%S)

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

    for domain in "${CONFIGURED_DOMAINS[@]}"; do
        echo -e "${GREEN}  - https://$domain${NC}"
    done

    echo -e ""
    echo -e "${YELLOW}Note: Certificates will auto-renew via certbot container${NC}"

    if [ ${#CONFIGURED_DOMAINS[@]} -lt 4 ]; then
        echo -e ""
        echo -e "${YELLOW}To add more subdomains later:${NC}"
        echo -e "${YELLOW}  1. Configure DNS for missing domains${NC}"
        echo -e "${YELLOW}  2. Re-run this script${NC}"
    fi

    echo -e "${GREEN}============================================================================${NC}"
else
    echo -e "${RED}✗ Failed to obtain SSL certificates${NC}"
    echo -e "${YELLOW}Please check:${NC}"
    echo -e "${YELLOW}  1. DNS is correctly configured${NC}"
    echo -e "${YELLOW}  2. Ports 80 and 443 are accessible${NC}"
    echo -e "${YELLOW}  3. No firewall is blocking Let's Encrypt${NC}"
    exit 1
fi
