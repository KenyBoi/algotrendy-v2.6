#!/bin/sh
set -e

echo "🚀 AlgoTrendy Frontend - Generating runtime configuration..."

# Default values (can be overridden by environment variables)
API_BASE_URL="${API_BASE_URL:-/api}"
WS_BASE_URL="${WS_BASE_URL:-}"
ENVIRONMENT="${ENVIRONMENT:-production}"
VERSION="${VERSION:-2.6.0}"
ENABLE_DEBUG="${ENABLE_DEBUG:-false}"

# Auto-detect WebSocket URL if not provided
if [ -z "$WS_BASE_URL" ]; then
  WS_BASE_URL="wss://\${window.location.host}"
fi

echo "  API_BASE_URL: $API_BASE_URL"
echo "  WS_BASE_URL: $WS_BASE_URL"
echo "  ENVIRONMENT: $ENVIRONMENT"
echo "  VERSION: $VERSION"
echo "  ENABLE_DEBUG: $ENABLE_DEBUG"

# Generate env-config.js with runtime environment variables
cat > /usr/share/nginx/html/env-config.js <<EOF
// Runtime Environment Configuration
// Generated at container startup - DO NOT EDIT
// Last generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")

window.ENV = {
  API_BASE_URL: "${API_BASE_URL}",
  WS_BASE_URL: "${WS_BASE_URL}",
  ENVIRONMENT: "${ENVIRONMENT}",
  VERSION: "${VERSION}",
  ENABLE_DEBUG: ${ENABLE_DEBUG}
};

console.log('✅ Runtime config loaded:', window.ENV);
EOF

echo "✅ Runtime configuration generated successfully!"
echo ""

# Start nginx
echo "🌐 Starting nginx..."
exec nginx -g 'daemon off;'
