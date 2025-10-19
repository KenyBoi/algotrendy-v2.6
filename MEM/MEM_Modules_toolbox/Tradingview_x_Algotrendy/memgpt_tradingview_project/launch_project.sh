#!/bin/bash

# MemGPT TradingView Integration Project Launcher
# ==============================================

PROJECT_DIR="/root/algotrendy_v2.4/memgpt_tradingview_project"
MAIN_DIR="/root/algotrendy_v2.4"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}🧠 MemGPT TradingView Integration Project v2.0${NC}"
echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
echo ""

# Check if project directory exists
if [ ! -d "$PROJECT_DIR" ]; then
    echo -e "${RED}❌ Project directory not found: $PROJECT_DIR${NC}"
    exit 1
fi

# Function to check if service is running
check_service() {
    if pgrep -f "$1" > /dev/null; then
        echo -e "${GREEN}✅ $2 is running${NC}"
        return 0
    else
        echo -e "${RED}❌ $2 is not running${NC}"
        return 1
    fi
}

# Function to start service
start_service() {
    local script=$1
    local name=$2
    local port=$3
    
    echo -e "${YELLOW}🔄 Starting $name...${NC}"
    
    if [ -f "$MAIN_DIR/$script" ]; then
        cd "$MAIN_DIR"
        nohup python3 "$script" > "${name// /_}_output.log" 2>&1 &
        sleep 3
        
        if [ ! -z "$port" ]; then
            if curl -s "http://localhost:$port" > /dev/null 2>&1; then
                echo -e "${GREEN}✅ $name started on port $port${NC}"
            else
                echo -e "${RED}❌ $name failed to start on port $port${NC}"
            fi
        else
            if pgrep -f "$script" > /dev/null; then
                echo -e "${GREEN}✅ $name started successfully${NC}"
            else
                echo -e "${RED}❌ $name failed to start${NC}"
            fi
        fi
    else
        echo -e "${RED}❌ Script not found: $MAIN_DIR/$script${NC}"
    fi
}

# Display project information
echo -e "${BLUE}📋 Project Overview${NC}"
echo -e "${BLUE}==================${NC}"
echo "• Enhanced Pine Script with move confidence & reversal tracking"
echo "• Real-time MemGPT analysis server (Flask API)"
echo "• TradingView → TradeStation webhook bridge"
echo "• Automated paper trading integration"
echo "• Performance tracking and accuracy metrics"
echo ""

# Check current system status
echo -e "${PURPLE}🔍 System Status Check${NC}"
echo -e "${PURPLE}=====================${NC}"

check_service "memgpt_tradingview_companion.py" "MemGPT Analysis Server"
COMPANION_RUNNING=$?

check_service "memgpt_tradingview_tradestation_bridge.py" "Webhook Bridge Server"
BRIDGE_RUNNING=$?

echo ""

# Main menu
while true; do
    echo -e "${CYAN}🎯 Select Action:${NC}"
    echo "1. 🚀 Start Complete System (All Services)"
    echo "2. 📊 Start MemGPT Analysis Server Only"  
    echo "3. 🌉 Start Webhook Bridge Only"
    echo "4. 📈 View Pine Script (Enhanced Version)"
    echo "5. 🔧 Project Configuration"
    echo "6. 📋 System Status & Monitoring"
    echo "7. 🛑 Stop All Services"
    echo "8. ❌ Exit"
    echo ""
    
    read -p "$(echo -e ${YELLOW}Choose option [1-8]: ${NC})" choice
    
    case $choice in
        1)
            echo -e "${CYAN}🚀 Starting Complete MemGPT System...${NC}"
            echo ""
            
            # Start MemGPT Analysis Server
            if [ $COMPANION_RUNNING -ne 0 ]; then
                start_service "memgpt_tradingview_companion.py" "MemGPT Analysis Server" "5003"
            else
                echo -e "${GREEN}✅ MemGPT Analysis Server already running${NC}"
            fi
            
            echo ""
            
            # Start Webhook Bridge
            if [ $BRIDGE_RUNNING -ne 0 ]; then
                start_service "memgpt_tradingview_tradestation_bridge.py" "Webhook Bridge Server" "5004"
            else
                echo -e "${GREEN}✅ Webhook Bridge Server already running${NC}"
            fi
            
            echo ""
            echo -e "${GREEN}🎯 Complete System Started!${NC}"
            echo -e "${BLUE}📊 MemGPT Analysis: http://localhost:5003${NC}"
            echo -e "${BLUE}🌉 Webhook Bridge: http://localhost:5004${NC}"
            echo -e "${BLUE}📈 Pine Script: $PROJECT_DIR/pine_scripts/memgpt_companion_enhanced.pine${NC}"
            echo ""
            ;;
            
        2)
            echo -e "${CYAN}📊 Starting MemGPT Analysis Server...${NC}"
            start_service "memgpt_tradingview_companion.py" "MemGPT Analysis Server" "5003"
            echo ""
            ;;
            
        3)
            echo -e "${CYAN}🌉 Starting Webhook Bridge Server...${NC}"
            start_service "memgpt_tradingview_tradestation_bridge.py" "Webhook Bridge Server" "5004"
            echo ""
            ;;
            
        4)
            echo -e "${CYAN}📈 Enhanced Pine Script Location:${NC}"
            echo "$PROJECT_DIR/pine_scripts/memgpt_companion_enhanced.pine"
            echo ""
            echo -e "${YELLOW}📋 Key Features:${NC}"
            echo "• Move Confidence: Probability of 0.2%+ directional move"
            echo "• Reversal Risk: Real-time reversal probability tracking"
            echo "• Direction Predictions: Strong/moderate directional analysis"
            echo "• Performance Tracking: Historical accuracy metrics"
            echo "• Enhanced Visuals: Direction arrows, target lines, status table"
            echo ""
            
            read -p "$(echo -e ${BLUE}View Pine Script content? [y/N]: ${NC})" view_pine
            if [[ $view_pine =~ ^[Yy]$ ]]; then
                echo ""
                head -50 "$PROJECT_DIR/pine_scripts/memgpt_companion_enhanced.pine"
                echo ""
                echo -e "${BLUE}... (showing first 50 lines) ...${NC}"
            fi
            echo ""
            ;;
            
        5)
            echo -e "${CYAN}🔧 Project Configuration${NC}"
            echo -e "${CYAN}========================${NC}"
            
            if [ -f "$PROJECT_DIR/project_config.json" ]; then
                echo -e "${GREEN}📋 Configuration found:${NC}"
                echo "$PROJECT_DIR/project_config.json"
                echo ""
                
                read -p "$(echo -e ${BLUE}View configuration? [y/N]: ${NC})" view_config
                if [[ $view_config =~ ^[Yy]$ ]]; then
                    echo ""
                    cat "$PROJECT_DIR/project_config.json" | jq '.' 2>/dev/null || cat "$PROJECT_DIR/project_config.json"
                fi
            else
                echo -e "${RED}❌ Configuration file not found${NC}"
            fi
            echo ""
            ;;
            
        6)
            echo -e "${CYAN}📋 System Status & Monitoring${NC}"
            echo -e "${CYAN}=============================${NC}"
            
            # Check services
            check_service "memgpt_tradingview_companion.py" "MemGPT Analysis Server"
            check_service "memgpt_tradingview_tradestation_bridge.py" "Webhook Bridge Server"
            echo ""
            
            # Test endpoints if services are running
            echo -e "${BLUE}🔗 Endpoint Status:${NC}"
            
            if curl -s "http://localhost:5003" > /dev/null 2>&1; then
                echo -e "${GREEN}✅ MemGPT Analysis API: http://localhost:5003${NC}"
                echo -e "${BLUE}   📊 Live Analysis: http://localhost:5003/memgpt/live/BTCUSDT${NC}"
            else
                echo -e "${RED}❌ MemGPT Analysis API not responding${NC}"
            fi
            
            if curl -s "http://localhost:5004" > /dev/null 2>&1; then
                echo -e "${GREEN}✅ Webhook Bridge API: http://localhost:5004${NC}"
                echo -e "${BLUE}   📈 System Status: http://localhost:5004/status${NC}"
                echo -e "${BLUE}   📊 Trade History: http://localhost:5004/trades${NC}"
            else
                echo -e "${RED}❌ Webhook Bridge API not responding${NC}"
            fi
            
            echo ""
            echo -e "${YELLOW}📊 Monitoring Commands:${NC}"
            echo "• Live MemGPT Analysis: curl http://localhost:5003/memgpt/live/BTCUSDT | jq"
            echo "• System Status: curl http://localhost:5004/status | jq"
            echo "• Trade History: curl http://localhost:5004/trades | jq"
            echo "• Watch Logs: tail -f *_output.log"
            echo ""
            ;;
            
        7)
            echo -e "${YELLOW}🛑 Stopping All Services...${NC}"
            
            pkill -f "memgpt_tradingview_companion.py" 2>/dev/null
            pkill -f "memgpt_tradingview_tradestation_bridge.py" 2>/dev/null
            
            sleep 2
            
            echo -e "${GREEN}✅ All services stopped${NC}"
            echo ""
            ;;
            
        8)
            echo -e "${CYAN}👋 Exiting MemGPT TradingView Project${NC}"
            exit 0
            ;;
            
        *)
            echo -e "${RED}❌ Invalid option. Please choose 1-8.${NC}"
            echo ""
            ;;
    esac
done