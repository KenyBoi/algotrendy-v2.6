#!/bin/bash
# DNS Propagation Checker for AlgoTrendy

TARGET_IP="216.238.90.131"
OLD_IP="199.188.201.33"

echo "=========================================="
echo "   AlgoTrendy DNS Propagation Checker"
echo "=========================================="
echo ""
echo "Checking DNS every 30 seconds..."
echo "Press Ctrl+C to stop"
echo ""
echo "Target IP: $TARGET_IP (Your VPS)"
echo "Old IP:    $OLD_IP (Namecheap hosting)"
echo ""

while true; do
    CURRENT_IP=$(dig +short algotrendy.com @8.8.8.8 | head -1)
    APP_IP=$(dig +short app.algotrendy.com @8.8.8.8 | head -1)
    API_IP=$(dig +short api.algotrendy.com @8.8.8.8 | head -1)

    echo "─────────────────────────────────────────"
    echo "Time: $(date '+%Y-%m-%d %H:%M:%S')"
    echo ""

    # Check algotrendy.com
    if [ "$CURRENT_IP" == "$TARGET_IP" ]; then
        echo "✅ algotrendy.com     → $CURRENT_IP (READY!)"
    elif [ "$CURRENT_IP" == "$OLD_IP" ]; then
        echo "⏳ algotrendy.com     → $CURRENT_IP (Still old IP)"
    else
        echo "⏳ algotrendy.com     → $CURRENT_IP (Updating...)"
    fi

    # Check app.algotrendy.com
    if [ "$APP_IP" == "$TARGET_IP" ]; then
        echo "✅ app.algotrendy.com → $APP_IP (READY!)"
    elif [ -z "$APP_IP" ]; then
        echo "⏳ app.algotrendy.com → Not found yet"
    else
        echo "⏳ app.algotrendy.com → $APP_IP"
    fi

    # Check api.algotrendy.com
    if [ "$API_IP" == "$TARGET_IP" ]; then
        echo "✅ api.algotrendy.com → $API_IP (READY!)"
    elif [ -z "$API_IP" ]; then
        echo "⏳ api.algotrendy.com → Not found yet"
    else
        echo "⏳ api.algotrendy.com → $API_IP"
    fi

    echo ""

    # Check if all domains are ready
    if [ "$CURRENT_IP" == "$TARGET_IP" ] && [ "$APP_IP" == "$TARGET_IP" ] && [ "$API_IP" == "$TARGET_IP" ]; then
        echo "🎉🎉🎉 DNS READY! 🎉🎉🎉"
        echo ""
        echo "All domains now point to your VPS!"
        echo "Run this command to set up SSL:"
        echo ""
        echo "    sudo ./scripts/setup-ssl.sh"
        echo ""
        exit 0
    fi

    echo "Checking again in 30 seconds..."
    sleep 30
done
