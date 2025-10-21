#!/bin/bash
# Quick script to test Local LEAN backtesting

echo "==================================================================="
echo "Testing AlgoTrendy Local LEAN Backtesting Engine"
echo "==================================================================="
echo ""

# Set environment to use local engine
export BACKTEST_ENGINE=local
echo "✅ Set BACKTEST_ENGINE=local"
echo ""

# Test basic custom backtest first (this should work immediately)
echo "Test 1: Custom Engine Backtest (Baseline)"
echo "-------------------------------------------------------------------"
curl -X POST http://localhost:5002/api/v1/backtesting/run \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSDT",
    "startDate": "2024-01-01T00:00:00Z",
    "endDate": "2024-02-01T00:00:00Z",
    "initialCapital": 10000,
    "timeframe": "Hour",
    "assetClass": "Crypto",
    "strategyName": "Momentum"
  }' 2>&1

echo ""
echo ""
echo "==================================================================="
echo "Test Complete!"
echo "==================================================================="
echo ""
echo "To use Local LEAN when it's fully integrated:"
echo ""
echo "curl -X POST http://localhost:5002/api/v1/backtesting/run/with-engine \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{"
echo "    \"config\": {"
echo "      \"symbol\": \"BTCUSD\","
echo "      \"startDate\": \"2024-01-01T00:00:00Z\","
echo "      \"endDate\": \"2024-10-01T00:00:00Z\","
echo "      \"initialCapital\": 100000"
echo "    },"
echo "    \"engineType\": \"Local\""
echo "  }'"
echo ""
