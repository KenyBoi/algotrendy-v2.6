#!/bin/bash

# ============================================================================
# AlgoTrendy ML Training System - Complete Startup Script
# ============================================================================

echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║                                                                  ║"
echo "║     🚀 AlgoTrendy ML Training System - Starting All Services    ║"
echo "║                                                                  ║"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}❌ This script must be run as root${NC}"
   exit 1
fi

# Base directory
BASE_DIR="/root/AlgoTrendy_v2.6"
cd "$BASE_DIR" || exit 1

echo -e "${BLUE}📂 Working directory: ${BASE_DIR}${NC}"
echo ""

# ============================================================================
# Step 1: Check Prerequisites
# ============================================================================

echo -e "${YELLOW}[1/6] Checking prerequisites...${NC}"

# Check Python
if command -v python3 &> /dev/null; then
    echo -e "${GREEN}✅ Python 3 installed: $(python3 --version)${NC}"
else
    echo -e "${RED}❌ Python 3 not found. Please install Python 3.${NC}"
    exit 1
fi

# Check .NET
if command -v dotnet &> /dev/null; then
    echo -e "${GREEN}✅ .NET SDK installed: $(dotnet --version)${NC}"
else
    echo -e "${RED}❌ .NET SDK not found. Please install .NET 8.${NC}"
    exit 1
fi

# Check Node.js (optional for frontend)
if command -v node &> /dev/null; then
    echo -e "${GREEN}✅ Node.js installed: $(node --version)${NC}"
else
    echo -e "${YELLOW}⚠️  Node.js not found. Frontend won't start.${NC}"
fi

echo ""

# ============================================================================
# Step 2: Install Python Dependencies
# ============================================================================

echo -e "${YELLOW}[2/6] Installing Python dependencies...${NC}"

pip3 install --break-system-packages -q \
    scikit-learn \
    yfinance \
    joblib \
    pandas \
    numpy \
    fastapi \
    uvicorn \
    python-multipart \
    || echo -e "${RED}❌ Failed to install some Python packages${NC}"

echo -e "${GREEN}✅ Python dependencies installed${NC}"
echo ""

# ============================================================================
# Step 3: Start Python ML API Server
# ============================================================================

echo -e "${YELLOW}[3/6] Starting Python ML API Server...${NC}"

# Check if already running
if pgrep -f "ml_api_server.py" > /dev/null; then
    echo -e "${YELLOW}⚠️  ML API Server already running. Stopping it...${NC}"
    pkill -f "ml_api_server.py"
    sleep 2
fi

# Start ML API server in background
nohup python3 ml_api_server.py > logs/ml_api_server.log 2>&1 &
ML_API_PID=$!

# Wait for server to start
echo "Waiting for ML API Server to start..."
for i in {1..10}; do
    if curl -s http://localhost:5050/ > /dev/null 2>&1; then
        echo -e "${GREEN}✅ ML API Server started (PID: $ML_API_PID)${NC}"
        echo -e "${BLUE}   URL: http://localhost:5050${NC}"
        echo -e "${BLUE}   Docs: http://localhost:5050/docs${NC}"
        break
    fi
    sleep 1
done

echo ""

# ============================================================================
# Step 4: Build C# Backend (if needed)
# ============================================================================

echo -e "${YELLOW}[4/6] Building C# Backend...${NC}"

cd backend/AlgoTrendy.API || exit 1

# Check if already built
if [ ! -d "bin/Release/net8.0" ]; then
    echo "Building in Release mode..."
    dotnet build -c Release --nologo
else
    echo -e "${GREEN}✅ Backend already built${NC}"
fi

echo ""

# ============================================================================
# Step 5: Start C# Backend API
# ============================================================================

echo -e "${YELLOW}[5/6] Starting C# Backend API...${NC}"

# Check if already running
if pgrep -f "AlgoTrendy.API" > /dev/null; then
    echo -e "${YELLOW}⚠️  Backend API already running. Stopping it...${NC}"
    pkill -f "AlgoTrendy.API"
    sleep 2
fi

# Start backend in background
nohup dotnet run --no-build -c Release > ../../logs/backend_api.log 2>&1 &
BACKEND_PID=$!

# Wait for server to start
echo "Waiting for Backend API to start..."
cd "$BASE_DIR" || exit 1
for i in {1..15}; do
    if curl -s http://localhost:5000/swagger > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Backend API started (PID: $BACKEND_PID)${NC}"
        echo -e "${BLUE}   URL: http://localhost:5000${NC}"
        echo -e "${BLUE}   Swagger: http://localhost:5000/swagger${NC}"
        break
    fi
    sleep 1
done

echo ""

# ============================================================================
# Step 6: Start Frontend (Optional)
# ============================================================================

echo -e "${YELLOW}[6/6] Starting Frontend (if available)...${NC}"

if [ -d "frontend" ] && command -v npm &> /dev/null; then
    cd frontend || exit 1

    # Check if node_modules exists
    if [ ! -d "node_modules" ]; then
        echo "Installing frontend dependencies..."
        npm install
    fi

    # Check if already running
    if pgrep -f "react-scripts start" > /dev/null; then
        echo -e "${YELLOW}⚠️  Frontend already running.${NC}"
    else
        # Start frontend in background
        nohup npm start > ../logs/frontend.log 2>&1 &
        FRONTEND_PID=$!

        echo -e "${GREEN}✅ Frontend starting (PID: $FRONTEND_PID)${NC}"
        echo -e "${BLUE}   URL: http://localhost:3000${NC}"
    fi

    cd "$BASE_DIR" || exit 1
else
    echo -e "${YELLOW}⚠️  Frontend not available or npm not installed. Skipping.${NC}"
fi

echo ""

# ============================================================================
# Summary
# ============================================================================

echo "╔══════════════════════════════════════════════════════════════════╗"
echo "║                                                                  ║"
echo "║          ✅ AlgoTrendy ML Training System Started                ║"
echo "║                                                                  ║"
echo "╚══════════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${GREEN}🚀 Services Running:${NC}"
echo ""
echo -e "${BLUE}Python ML API:${NC}      http://localhost:5050"
echo -e "${BLUE}   - Documentation:${NC} http://localhost:5050/docs"
echo -e "${BLUE}   - Health Check:${NC}  curl http://localhost:5050/"
echo ""
echo -e "${BLUE}C# Backend API:${NC}     http://localhost:5000"
echo -e "${BLUE}   - Swagger UI:${NC}    http://localhost:5000/swagger"
echo -e "${BLUE}   - SignalR Hub:${NC}   ws://localhost:5000/hubs/mltraining"
echo ""

if [ -n "$FRONTEND_PID" ]; then
    echo -e "${BLUE}Frontend:${NC}           http://localhost:3000"
    echo -e "${BLUE}   - ML Training:${NC}   http://localhost:3000/ml-training"
    echo ""
fi

echo -e "${YELLOW}📊 Available Endpoints:${NC}"
echo ""
echo "  GET  /api/mltraining/models           - List all ML models"
echo "  GET  /api/mltraining/models/{id}      - Get model details"
echo "  POST /api/mltraining/train            - Start training job"
echo "  GET  /api/mltraining/drift/{id}       - Check model drift"
echo "  GET  /api/mltraining/patterns         - Get latest patterns"
echo ""

echo -e "${YELLOW}📝 Logs:${NC}"
echo ""
echo "  ML API Server:  tail -f logs/ml_api_server.log"
echo "  Backend API:    tail -f logs/backend_api.log"
if [ -n "$FRONTEND_PID" ]; then
    echo "  Frontend:       tail -f logs/frontend.log"
fi
echo ""

echo -e "${YELLOW}🛑 Stop Services:${NC}"
echo ""
echo "  pkill -f ml_api_server.py"
echo "  pkill -f AlgoTrendy.API"
if [ -n "$FRONTEND_PID" ]; then
    echo "  pkill -f react-scripts"
fi
echo ""

echo -e "${GREEN}✨ System ready for ML training and analysis!${NC}"
echo ""

# Create PID file
cat > /tmp/algotrendy_ml_pids.txt <<EOF
ML_API_PID=$ML_API_PID
BACKEND_PID=$BACKEND_PID
FRONTEND_PID=${FRONTEND_PID:-N/A}
EOF

echo -e "${BLUE}💡 Tip: Use './stop_ml_system.sh' to stop all services${NC}"
echo ""
