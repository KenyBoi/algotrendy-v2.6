#!/bin/bash

# Bybit Testnet Quick Test Script
# This script helps you quickly verify your Bybit testnet connection

set -e  # Exit on error

echo "=============================================="
echo "🚀 Bybit Testnet Connection Test"
echo "=============================================="
echo ""

# Check if .env file exists
if [ ! -f "/root/AlgoTrendy_v2.6/.env" ]; then
    echo "❌ .env file not found!"
    echo ""
    echo "Creating .env file from template..."
    cp /root/AlgoTrendy_v2.6/.env /root/AlgoTrendy_v2.6/.env.backup 2>/dev/null || true
    echo ""
    echo "⚠️  Please edit /root/AlgoTrendy_v2.6/.env and add your Bybit credentials:"
    echo ""
    echo "   BYBIT_API_KEY=your_api_key_here"
    echo "   BYBIT_API_SECRET=your_secret_key_here"
    echo "   BYBIT_TESTNET=true"
    echo "   DEFAULT_BROKER=bybit"
    echo ""
    exit 1
fi

# Load environment variables
echo "📁 Loading environment variables from .env..."
export $(grep -v '^#' /root/AlgoTrendy_v2.6/.env | xargs)

# Check if Bybit credentials are set
if [ -z "$BYBIT_API_KEY" ] || [ "$BYBIT_API_KEY" = "your_bybit_api_key_here" ]; then
    echo "❌ BYBIT_API_KEY not configured!"
    echo ""
    echo "Please edit /root/AlgoTrendy_v2.6/.env and set:"
    echo "   BYBIT_API_KEY=your_actual_api_key"
    echo ""
    exit 1
fi

if [ -z "$BYBIT_API_SECRET" ] || [ "$BYBIT_API_SECRET" = "your_bybit_secret_here" ]; then
    echo "❌ BYBIT_API_SECRET not configured!"
    echo ""
    echo "Please edit /root/AlgoTrendy_v2.6/.env and set:"
    echo "   BYBIT_API_SECRET=your_actual_secret_key"
    echo ""
    exit 1
fi

echo "✅ Bybit credentials found"
echo ""

# Show configuration (masked)
MASKED_KEY="${BYBIT_API_KEY:0:8}...${BYBIT_API_KEY: -4}"
MASKED_SECRET="${BYBIT_API_SECRET:0:8}...${BYBIT_API_SECRET: -4}"

echo "📋 Configuration:"
echo "   API Key: $MASKED_KEY"
echo "   Secret: $MASKED_SECRET"
echo "   Testnet: ${BYBIT_TESTNET:-false}"
echo ""

# Run integration tests
echo "🧪 Running Bybit integration tests..."
echo "=============================================="
echo ""

cd /root/AlgoTrendy_v2.6/backend

# Build first
echo "🔨 Building project..."
dotnet build AlgoTrendy.sln > /dev/null 2>&1 || {
    echo "❌ Build failed!"
    dotnet build AlgoTrendy.sln
    exit 1
}
echo "✅ Build successful"
echo ""

# Run Bybit tests
echo "🧪 Running Bybit connection tests..."
echo "--------------------------------------------"
dotnet test --filter "Broker=Bybit" --verbosity normal

echo ""
echo "=============================================="
echo "✅ Test Complete!"
echo "=============================================="
echo ""
echo "Next steps:"
echo "1. Check the test results above"
echo "2. If successful, your Bybit testnet is ready!"
echo "3. Start the API: cd backend && dotnet run --project AlgoTrendy.API/AlgoTrendy.API.csproj"
echo "4. Test trading: Visit http://localhost:5002/swagger"
echo ""
echo "Happy trading! 📈"
