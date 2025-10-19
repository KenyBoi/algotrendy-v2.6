#!/bin/bash

# 🚀 MEM ML PIPELINE LAUNCHER
# Starts all components: Connectivity, Data, Visualization, ETL, Monitoring

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
WORK_DIR="/root/algotrendy_v2.4"
LOG_DIR="$WORK_DIR/logs"
VENV_PATH="$WORK_DIR/snowflake_venv"

# Create log directory
mkdir -p "$LOG_DIR"

# Banner
echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════╗"
echo "║         🚀 MEM ML PIPELINE LAUNCHER                        ║"
echo "║    Multi-Phase Architecture for MemGPT Data Pipeline       ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# Check virtual environment
if [ ! -d "$VENV_PATH" ]; then
    echo -e "${YELLOW}⚠️  Virtual environment not found. Creating...${NC}"
    python3 -m venv "$VENV_PATH"
fi

# Activate virtual environment
source "$VENV_PATH/bin/activate"

# Function to start a service
start_service() {
    local name=$1
    local command=$2
    local log_file="$LOG_DIR/${name}.log"
    
    echo -e "${BLUE}▶ Starting $name...${NC}"
    
    # Run in background
    nohup bash -c "$command" > "$log_file" 2>&1 &
    local pid=$!
    
    sleep 2
    
    # Check if process is still running
    if ps -p $pid > /dev/null; then
        echo -e "${GREEN}✅ $name started (PID: $pid)${NC}"
        echo $pid >> "$LOG_DIR/services.pids"
    else
        echo -e "${RED}❌ $name failed to start${NC}"
        echo "Last 10 lines of log:"
        tail -10 "$log_file"
    fi
}

# Phase 1: Connectivity Layer Status
echo -e "\n${BLUE}📡 Phase 1: Connectivity Layer${NC}"
python3 "$WORK_DIR/mem_credentials.py" > /dev/null 2>&1 && echo -e "${GREEN}✅ Credentials manager ready${NC}" || echo -e "${RED}❌ Credentials manager failed${NC}"

# Phase 2: Data Layer Status
echo -e "\n${BLUE}💾 Phase 2: Data Layer${NC}"
python3 -c "from sqlite_manager import SQLiteManager; print('✅ SQLite manager ready')" 2>/dev/null || echo -e "${RED}❌ SQLite manager failed${NC}"

# Phase 3: Visualization Layer
echo -e "\n${BLUE}📊 Phase 3: Visualization Layer${NC}"
if command -v streamlit &> /dev/null; then
    echo -e "${GREEN}✅ Streamlit ready${NC}"
    
    echo -e "\n${YELLOW}Starting Streamlit dashboard...${NC}"
    start_service "streamlit_dashboard" "cd $WORK_DIR && streamlit run mem_live_dashboard.py --server.port=8501 --server.headless=true"
else
    echo -e "${RED}❌ Streamlit not installed${NC}"
fi

# Phase 4: ETL Pipeline Status
echo -e "\n${BLUE}🔄 Phase 4: ETL Pipeline${NC}"
if [ -f "$WORK_DIR/prefect_flows.py" ]; then
    echo -e "${GREEN}✅ Prefect flows ready${NC}"
else
    echo -e "${YELLOW}⏳ Prefect flows not yet created${NC}"
fi

# Phase 5: Monitoring Status
echo -e "\n${BLUE}📈 Phase 5: Monitoring${NC}"
if command -v prometheus &> /dev/null; then
    echo -e "${GREEN}✅ Prometheus available${NC}"
else
    echo -e "${YELLOW}⏳ Prometheus monitoring not yet configured${NC}"
fi

# Summary
echo -e "\n${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║              📊 SERVICE STATUS SUMMARY                       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"

echo -e "\n${GREEN}✅ Running Services:${NC}"
if [ -f "$LOG_DIR/services.pids" ]; then
    while read pid; do
        if ps -p $pid > /dev/null 2>&1; then
            cmd=$(ps -p $pid -o comm=)
            echo "   - $cmd (PID: $pid)"
        fi
    done < "$LOG_DIR/services.pids"
else
    echo "   (None started yet)"
fi

echo -e "\n${BLUE}📍 Access Points:${NC}"
echo "   - Streamlit Dashboard: http://127.0.0.1:8501"
echo "   - MEM WebSocket: ws://127.0.0.1:8765"
echo "   - REST API: http://127.0.0.1:5000"

echo -e "\n${YELLOW}📋 Log Files:${NC}"
echo "   - Location: $LOG_DIR/"
echo "   - View all: tail -f $LOG_DIR/*.log"

echo -e "\n${BLUE}🛑 To stop services:${NC}"
if [ -f "$LOG_DIR/services.pids" ]; then
    echo "   bash kill_services.sh"
else
    echo "   (Use pkill streamlit or similar)"
fi

echo -e "\n${GREEN}✅ MEM ML Pipeline launcher ready!${NC}\n"
