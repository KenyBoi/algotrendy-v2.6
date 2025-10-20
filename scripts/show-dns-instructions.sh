#!/bin/bash
# ============================================================================
# AlgoTrendy v2.6 - DNS Setup Instructions
# ============================================================================
# This script displays DNS setup instructions for Namecheap
# ============================================================================

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${GREEN}============================================================================${NC}"
echo -e "${GREEN}AlgoTrendy DNS Setup Instructions${NC}"
echo -e "${GREEN}============================================================================${NC}"
echo ""

# Get server IP
echo -e "${YELLOW}Detecting server IP address...${NC}"
SERVER_IP=$(curl -4 -s ifconfig.me)

if [ -z "$SERVER_IP" ]; then
    echo -e "${RED}Could not detect server IP automatically.${NC}"
    echo -e "${YELLOW}Please find your server IP manually and use it below.${NC}"
    SERVER_IP="YOUR_SERVER_IP"
else
    echo -e "${GREEN}✓ Server IP: $SERVER_IP${NC}"
fi

echo ""
echo -e "${CYAN}============================================================================${NC}"
echo -e "${CYAN}NAMECHEAP DNS CONFIGURATION${NC}"
echo -e "${CYAN}============================================================================${NC}"
echo ""
echo -e "${YELLOW}1. Log into Namecheap:${NC}"
echo -e "   Go to: https://www.namecheap.com"
echo -e "   Navigate to: Domain List > algotrendy.com > Manage > Advanced DNS"
echo ""
echo -e "${YELLOW}2. Add the following DNS records:${NC}"
echo ""
echo -e "┌──────────────────────────────────────────────────────────────┐"
echo -e "│ ${CYAN}Type${NC}  │ ${CYAN}Host${NC}  │ ${CYAN}Value${NC}               │ ${CYAN}TTL${NC}         │"
echo -e "├───────┼───────┼─────────────────────┼──────────────┤"
echo -e "│ A     │ @     │ ${GREEN}$SERVER_IP${NC}     │ Automatic    │"
echo -e "│ A     │ www   │ ${GREEN}$SERVER_IP${NC}     │ Automatic    │"
echo -e "│ A     │ api   │ ${GREEN}$SERVER_IP${NC}     │ Automatic    │"
echo -e "│ A     │ app   │ ${GREEN}$SERVER_IP${NC}     │ Automatic    │"
echo -e "└───────┴───────┴─────────────────────┴──────────────┘"
echo ""
echo -e "${YELLOW}3. Save all changes in Namecheap${NC}"
echo ""
echo -e "${YELLOW}4. Wait for DNS propagation (15-30 minutes)${NC}"
echo ""
echo -e "${YELLOW}5. Verify DNS with these commands:${NC}"
echo ""
echo -e "   ${CYAN}nslookup algotrendy.com${NC}"
echo -e "   ${CYAN}nslookup www.algotrendy.com${NC}"
echo -e "   ${CYAN}nslookup api.algotrendy.com${NC}"
echo -e "   ${CYAN}nslookup app.algotrendy.com${NC}"
echo ""
echo -e "${YELLOW}6. Check DNS propagation globally:${NC}"
echo -e "   Visit: ${CYAN}https://dnschecker.org${NC}"
echo ""
echo -e "${CYAN}============================================================================${NC}"
echo -e "${YELLOW}After DNS is configured, run:${NC}"
echo -e "   ${GREEN}sudo ./scripts/setup-ssl.sh${NC}"
echo -e "${CYAN}============================================================================${NC}"
echo ""
echo -e "${YELLOW}For detailed instructions, see:${NC}"
echo -e "   ${CYAN}docs/NAMECHEAP_DNS_SETUP.md${NC}"
echo ""
