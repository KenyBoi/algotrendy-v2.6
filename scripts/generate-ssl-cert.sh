#!/bin/bash
# ============================================================================
# Generate Self-Signed SSL Certificate for Development/Testing
# ============================================================================
# This script generates a self-signed SSL certificate for local development
# For production, use Let's Encrypt (see DEPLOYMENT_DOCKER.md)
# ============================================================================

set -e

# Configuration
SSL_DIR="${1:-./ssl}"
CERT_DAYS=365
COUNTRY="US"
STATE="Illinois"
CITY="Chicago"
ORG="AlgoTrendy"
OU="Development"
CN="${2:-localhost}"

echo "============================================================================"
echo "Generating Self-Signed SSL Certificate"
echo "============================================================================"
echo "Directory: $SSL_DIR"
echo "Common Name: $CN"
echo "Valid for: $CERT_DAYS days"
echo "============================================================================"

# Create SSL directory if it doesn't exist
mkdir -p "$SSL_DIR"

# Generate private key and certificate
openssl req -x509 -nodes -days $CERT_DAYS \
    -newkey rsa:2048 \
    -keyout "$SSL_DIR/key.pem" \
    -out "$SSL_DIR/cert.pem" \
    -subj "/C=$COUNTRY/ST=$STATE/L=$CITY/O=$ORG/OU=$OU/CN=$CN" \
    -addext "subjectAltName=DNS:$CN,DNS:localhost,DNS:*.localhost,IP:127.0.0.1"

# Set proper permissions
chmod 600 "$SSL_DIR/key.pem"
chmod 644 "$SSL_DIR/cert.pem"

echo ""
echo "============================================================================"
echo "SSL Certificate Generated Successfully!"
echo "============================================================================"
echo "Certificate: $SSL_DIR/cert.pem"
echo "Private Key: $SSL_DIR/key.pem"
echo ""
echo "WARNING: This is a SELF-SIGNED certificate for development/testing only."
echo "For production, use Let's Encrypt or a trusted CA."
echo "============================================================================"
