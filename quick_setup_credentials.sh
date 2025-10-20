#!/bin/bash
#
# AlgoTrendy v2.6 - Quick Credentials Setup Script
#
# This script helps you quickly configure API credentials for AlgoTrendy v2.6
# using .NET User Secrets (recommended for development)
#
# Usage: ./quick_setup_credentials.sh
#

set -e

echo "═══════════════════════════════════════════════════════════════"
echo "  AlgoTrendy v2.6 - Credentials Setup"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# Navigate to API project
cd /root/AlgoTrendy_v2.6/backend/AlgoTrendy.API

# Initialize user secrets
echo "📦 Initializing user secrets..."
dotnet user-secrets init 2>/dev/null || echo "User secrets already initialized"
echo ""

# Function to prompt for credentials
setup_integration() {
    local name=$1
    local key_path=$2
    local secret_path=$3
    local optional=$4

    echo "──────────────────────────────────────────────────────────────"
    echo "  $name"
    echo "──────────────────────────────────────────────────────────────"

    if [ "$optional" == "optional" ]; then
        read -p "Setup $name? (y/n) [n]: " setup
        if [ "$setup" != "y" ] && [ "$setup" != "Y" ]; then
            echo "⏭️  Skipped $name"
            echo ""
            return
        fi
    fi

    read -p "API Key/User ID: " key_value
    if [ -n "$key_value" ]; then
        dotnet user-secrets set "$key_path" "$key_value"
        echo "✅ Saved API Key"
    fi

    read -s -p "API Secret/Token: " secret_value
    echo ""
    if [ -n "$secret_value" ]; then
        dotnet user-secrets set "$secret_path" "$secret_value"
        echo "✅ Saved API Secret"
    fi
    echo ""
}

# Function to setup database
setup_database() {
    echo "──────────────────────────────────────────────────────────────"
    echo "  PostgreSQL Database"
    echo "──────────────────────────────────────────────────────────────"

    read -p "Database Host [localhost]: " db_host
    db_host=${db_host:-localhost}

    read -p "Database Name [algotrendy_v26]: " db_name
    db_name=${db_name:-algotrendy_v26}

    read -p "Database User [algotrendy]: " db_user
    db_user=${db_user:-algotrendy}

    read -s -p "Database Password: " db_pass
    echo ""

    if [ -n "$db_pass" ]; then
        conn_str="Host=$db_host;Database=$db_name;Username=$db_user;Password=$db_pass"
        dotnet user-secrets set "ConnectionStrings:AlgoTrendyDb" "$conn_str"
        echo "✅ Database connection configured"
    fi
    echo ""
}

# Function to setup QuestDB
setup_questdb() {
    echo "──────────────────────────────────────────────────────────────"
    echo "  QuestDB (Time-Series Database)"
    echo "──────────────────────────────────────────────────────────────"

    read -p "Setup QuestDB? (y/n) [y]: " setup
    setup=${setup:-y}

    if [ "$setup" == "y" ] || [ "$setup" == "Y" ]; then
        read -p "QuestDB Username [admin]: " qdb_user
        qdb_user=${qdb_user:-admin}

        read -s -p "QuestDB Password [quest]: " qdb_pass
        echo ""
        qdb_pass=${qdb_pass:-quest}

        dotnet user-secrets set "QuestDB:Username" "$qdb_user"
        dotnet user-secrets set "QuestDB:Password" "$qdb_pass"
        echo "✅ QuestDB configured"
    else
        echo "⏭️  Skipped QuestDB"
    fi
    echo ""
}

# ═════════════════════════════════════════════════════════════════
# Required Integrations
# ═════════════════════════════════════════════════════════════════

echo "Required Integrations"
echo ""

# QuantConnect
echo "1. QuantConnect (Backtesting Platform)"
echo "   Get credentials from: https://www.quantconnect.com/account"
echo ""
setup_integration "QuantConnect" "QuantConnect:UserId" "QuantConnect:ApiToken"

# Bybit (Primary Broker)
echo "2. Bybit (Primary Trading Broker)"
echo "   Get credentials from: https://www.bybit.com/app/user/api-management"
echo ""
setup_integration "Bybit" "Brokers:Bybit:ApiKey" "Brokers:Bybit:ApiSecret"

# ═════════════════════════════════════════════════════════════════
# Optional Broker Integrations
# ═════════════════════════════════════════════════════════════════

echo "═══════════════════════════════════════════════════════════════"
echo "  Optional Broker Integrations"
echo "═══════════════════════════════════════════════════════════════"
echo ""

# MEXC
echo "3. MEXC (Optional)"
echo "   Get credentials from: https://www.mexc.com/user/openapi"
echo ""
setup_integration "MEXC" "Brokers:MEXC:ApiKey" "Brokers:MEXC:ApiSecret" "optional"

# Binance
echo "4. Binance (Optional)"
echo "   Get credentials from: https://www.binance.com/en/my/settings/api-management"
echo ""
setup_integration "Binance" "Brokers:Binance:ApiKey" "Brokers:Binance:ApiSecret" "optional"

# OKX
echo "5. OKX (Optional)"
echo "   Get credentials from: https://www.okx.com/account/my-api"
echo ""
read -p "Setup OKX? (y/n) [n]: " setup_okx
if [ "$setup_okx" == "y" ] || [ "$setup_okx" == "Y" ]; then
    read -p "API Key: " okx_key
    read -s -p "API Secret: " okx_secret
    echo ""
    read -p "Passphrase: " okx_pass

    if [ -n "$okx_key" ]; then
        dotnet user-secrets set "Brokers:OKX:ApiKey" "$okx_key"
        dotnet user-secrets set "Brokers:OKX:ApiSecret" "$okx_secret"
        dotnet user-secrets set "Brokers:OKX:Passphrase" "$okx_pass"
        echo "✅ OKX configured"
    fi
else
    echo "⏭️  Skipped OKX"
fi
echo ""

# Alpaca
echo "6. Alpaca (Stock Trading - Optional)"
echo "   Get credentials from: https://alpaca.markets/docs/api-documentation/api-v2/"
echo ""
setup_integration "Alpaca" "MarketData:Alpaca:ApiKey" "MarketData:Alpaca:ApiSecret" "optional"

# ═════════════════════════════════════════════════════════════════
# Database Configuration
# ═════════════════════════════════════════════════════════════════

echo "═══════════════════════════════════════════════════════════════"
echo "  Database Configuration"
echo "═══════════════════════════════════════════════════════════════"
echo ""

setup_database
setup_questdb

# ═════════════════════════════════════════════════════════════════
# Optional Market Data Providers
# ═════════════════════════════════════════════════════════════════

echo "═══════════════════════════════════════════════════════════════"
echo "  Optional Market Data Providers"
echo "═══════════════════════════════════════════════════════════════"
echo ""

read -p "Setup market data providers? (y/n) [n]: " setup_market
if [ "$setup_market" == "y" ] || [ "$setup_market" == "Y" ]; then

    # Alpha Vantage
    echo "7. Alpha Vantage (Free tier: 25 req/day)"
    echo "   Get key from: https://www.alphavantage.co/support/#api-key"
    echo ""
    read -p "API Key (or press Enter to skip): " av_key
    if [ -n "$av_key" ]; then
        dotnet user-secrets set "MarketData:AlphaVantage:ApiKey" "$av_key"
        echo "✅ Alpha Vantage configured"
    fi
    echo ""

    # Finnhub
    echo "8. Finnhub (Free tier: 60 calls/min)"
    echo "   Get key from: https://finnhub.io/register"
    echo ""
    read -p "API Key (or press Enter to skip): " fh_key
    if [ -n "$fh_key" ]; then
        dotnet user-secrets set "MarketData:Finnhub:ApiKey" "$fh_key"
        echo "✅ Finnhub configured"
    fi
    echo ""

    # Twelve Data
    echo "9. Twelve Data"
    echo "   Get key from: https://twelvedata.com/"
    echo ""
    read -p "API Key (or press Enter to skip): " td_key
    if [ -n "$td_key" ]; then
        dotnet user-secrets set "MarketData:TwelveData:ApiKey" "$td_key"
        echo "✅ Twelve Data configured"
    fi
    echo ""
else
    echo "⏭️  Skipped market data providers"
    echo ""
fi

# ═════════════════════════════════════════════════════════════════
# Summary
# ═════════════════════════════════════════════════════════════════

echo "═══════════════════════════════════════════════════════════════"
echo "  ✅ Setup Complete!"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo "Configured secrets:"
dotnet user-secrets list | sed 's/=.*/= ***HIDDEN***/' | head -20
echo ""
echo "Next steps:"
echo "1. Verify configuration: dotnet user-secrets list"
echo "2. Build the project: cd /root/AlgoTrendy_v2.6/backend && dotnet build"
echo "3. Run the API: dotnet run --project AlgoTrendy.API"
echo ""
echo "For detailed documentation, see:"
echo "  /root/AlgoTrendy_v2.6/CREDENTIALS_SETUP_GUIDE.md"
echo ""
echo "═══════════════════════════════════════════════════════════════"
