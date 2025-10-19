#!/bin/bash

# ============================================================================
# Seq Logging Integration Test Script
# Tests structured logging with Seq for AlgoTrendy v2.6
# ============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="${API_URL:-http://localhost:5002}"
SEQ_URL="${SEQ_URL:-http://localhost:5341}"
CORRELATION_ID="test-$(date +%s)-$(uuidgen | cut -d'-' -f1)"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Seq Structured Logging Test${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Function to print test results
print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ $2${NC}"
    else
        echo -e "${RED}✗ $2${NC}"
    fi
}

# Step 1: Check if Seq is running
echo -e "${YELLOW}[1/6] Checking Seq availability...${NC}"
if curl -s "${SEQ_URL}/api" > /dev/null 2>&1; then
    print_result 0 "Seq is running at ${SEQ_URL}"
else
    print_result 1 "Seq is NOT running at ${SEQ_URL}"
    echo -e "${RED}Please start Seq with: docker-compose up -d seq${NC}"
    exit 1
fi
echo ""

# Step 2: Check if API is running
echo -e "${YELLOW}[2/6] Checking API availability...${NC}"
if curl -s "${API_URL}/health" > /dev/null 2>&1; then
    print_result 0 "API is running at ${API_URL}"
else
    print_result 1 "API is NOT running at ${API_URL}"
    echo -e "${RED}Please start API with: docker-compose up -d api${NC}"
    exit 1
fi
echo ""

# Step 3: Test order placement (generates structured logs)
echo -e "${YELLOW}[3/6] Testing order placement logging...${NC}"
ORDER_RESPONSE=$(curl -s -X POST "${API_URL}/api/trading/orders" \
    -H "Content-Type: application/json" \
    -H "X-Correlation-ID: ${CORRELATION_ID}" \
    -d '{
        "symbol": "BTCUSDT",
        "side": "Buy",
        "type": "Limit",
        "quantity": 0.001,
        "price": 50000,
        "clientOrderId": "test-order-'$(date +%s)'"
    }' 2>&1)

if [ $? -eq 0 ]; then
    print_result 0 "Order placement request sent"
    echo "Response preview: $(echo $ORDER_RESPONSE | head -c 100)..."
else
    print_result 1 "Order placement request failed"
fi
echo ""

# Step 4: Test balance query (generates structured logs)
echo -e "${YELLOW}[4/6] Testing balance query logging...${NC}"
BALANCE_RESPONSE=$(curl -s "${API_URL}/api/trading/balance/bybit/USDT" \
    -H "X-Correlation-ID: ${CORRELATION_ID}" 2>&1)

if [ $? -eq 0 ]; then
    print_result 0 "Balance query request sent"
    echo "Response preview: $(echo $BALANCE_RESPONSE | head -c 100)..."
else
    print_result 1 "Balance query request failed"
fi
echo ""

# Step 5: Wait for logs to be ingested by Seq
echo -e "${YELLOW}[5/6] Waiting for logs to be ingested by Seq...${NC}"
sleep 3
print_result 0 "Wait complete (3 seconds)"
echo ""

# Step 6: Verify logs in Seq
echo -e "${YELLOW}[6/6] Verifying logs in Seq...${NC}"

# Query Seq for recent events with our correlation ID
SEQ_QUERY="CorrelationId%20%3D%20%22${CORRELATION_ID}%22"
SEQ_EVENTS=$(curl -s "${SEQ_URL}/api/events?filter=${SEQ_QUERY}&count=100" 2>&1)

if echo "$SEQ_EVENTS" | grep -q "Events"; then
    EVENT_COUNT=$(echo "$SEQ_EVENTS" | grep -o '"Events"' | wc -l)
    print_result 0 "Logs found in Seq (correlation ID: ${CORRELATION_ID})"
    echo ""

    # Display sample log properties
    echo -e "${GREEN}Sample Log Properties:${NC}"
    echo "$SEQ_EVENTS" | jq -r '.Events[] | select(.Properties) | {
        Timestamp: .Timestamp,
        Level: .Level,
        Message: .MessageTemplate,
        CorrelationId: .Properties.CorrelationId,
        Symbol: .Properties.Symbol,
        OperationType: .Properties.OperationType,
        Broker: .Properties.Broker
    }' 2>/dev/null | head -30 || echo "$SEQ_EVENTS" | head -100
else
    print_result 1 "No logs found in Seq with correlation ID: ${CORRELATION_ID}"
    echo -e "${YELLOW}Note: Logs may take a few seconds to appear. Check Seq UI manually.${NC}"
fi
echo ""

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo -e "Seq UI: ${GREEN}${SEQ_URL}${NC}"
echo -e "Correlation ID: ${GREEN}${CORRELATION_ID}${NC}"
echo ""
echo -e "${YELLOW}Useful Seq Queries:${NC}"
echo ""
echo "1. All events from this test:"
echo -e "   ${GREEN}CorrelationId = \"${CORRELATION_ID}\"${NC}"
echo ""
echo "2. Recent order placements:"
echo -e "   ${GREEN}OperationType = \"PlaceOrder\" and @Timestamp > Now() - 5m${NC}"
echo ""
echo "3. All trading operations:"
echo -e "   ${GREEN}OperationType in [\"PlaceOrder\", \"CancelOrder\", \"GetBalance\"]${NC}"
echo ""
echo "4. Errors only:"
echo -e "   ${GREEN}@Level = \"Error\" and @Timestamp > Now() - 1h${NC}"
echo ""
echo "5. Specific symbol activity:"
echo -e "   ${GREEN}Symbol = \"BTCUSDT\"${NC}"
echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}✓ Test complete! Open Seq UI to explore logs:${NC}"
echo -e "${GREEN}  ${SEQ_URL}${NC}"
echo -e "${BLUE}========================================${NC}"
