#!/bin/bash

# ============================================================================
# AlgoTrendy ML Training System - Stop Script
# ============================================================================

echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║                                                                  ║"
echo "║     🛑 AlgoTrendy ML Training System - Stopping All Services    ║"
echo "║                                                                  ║"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Stop Python ML API Server
echo -e "${YELLOW}Stopping Python ML API Server...${NC}"
if pgrep -f "ml_api_server.py" > /dev/null; then
    pkill -f "ml_api_server.py"
    echo -e "${GREEN}✅ Python ML API stopped${NC}"
else
    echo -e "${YELLOW}⚠️  ML API was not running${NC}"
fi

# Stop C# Backend API
echo -e "${YELLOW}Stopping C# Backend API...${NC}"
if pgrep -f "AlgoTrendy.API" > /dev/null; then
    pkill -f "AlgoTrendy.API"
    echo -e "${GREEN}✅ Backend API stopped${NC}"
else
    echo -e "${YELLOW}⚠️  Backend API was not running${NC}"
fi

# Stop Frontend
echo -e "${YELLOW}Stopping Frontend...${NC}"
if pgrep -f "react-scripts" > /dev/null; then
    pkill -f "react-scripts"
    echo -e "${GREEN}✅ Frontend stopped${NC}"
else
    echo -e "${YELLOW}⚠️  Frontend was not running${NC}"
fi

# Clean up PID file
rm -f /tmp/algotrendy_ml_pids.txt

echo ""
echo -e "${GREEN}✅ All services stopped${NC}"
echo ""
