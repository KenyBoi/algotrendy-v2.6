#!/bin/bash
# AlgoTrendy Security Tools Setup Script
# This script installs Gitleaks and Semgrep for security scanning

set -e

echo "=================================="
echo "AlgoTrendy Security Tools Setup"
echo "=================================="
echo ""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if running as root (optional but recommended for system-wide install)
if [ "$EUID" -ne 0 ]; then
    echo -e "${YELLOW}Warning: Not running as root. Tools will be installed to /usr/local/bin${NC}"
    echo "You may need sudo privileges."
fi

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# 1. Install Gitleaks
echo -e "${YELLOW}[1/2] Installing Gitleaks...${NC}"
if command_exists gitleaks; then
    CURRENT_VERSION=$(gitleaks version 2>/dev/null || echo "unknown")
    echo -e "${GREEN}Gitleaks is already installed (version: $CURRENT_VERSION)${NC}"
    read -p "Do you want to reinstall? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Skipping Gitleaks installation."
    else
        curl -sSfL https://github.com/gitleaks/gitleaks/releases/download/v8.18.2/gitleaks_8.18.2_linux_x64.tar.gz | tar -xz -C /usr/local/bin gitleaks
        echo -e "${GREEN}Gitleaks reinstalled successfully!${NC}"
    fi
else
    curl -sSfL https://github.com/gitleaks/gitleaks/releases/download/v8.18.2/gitleaks_8.18.2_linux_x64.tar.gz | tar -xz -C /usr/local/bin gitleaks
    chmod +x /usr/local/bin/gitleaks
    echo -e "${GREEN}Gitleaks installed successfully!${NC}"
fi
gitleaks version

echo ""

# 2. Install Semgrep
echo -e "${YELLOW}[2/2] Installing Semgrep...${NC}"
if command_exists semgrep; then
    CURRENT_VERSION=$(semgrep --version 2>/dev/null || echo "unknown")
    echo -e "${GREEN}Semgrep is already installed (version: $CURRENT_VERSION)${NC}"
    read -p "Do you want to reinstall? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Skipping Semgrep installation."
    else
        pip3 install --break-system-packages --upgrade semgrep
        echo -e "${GREEN}Semgrep reinstalled successfully!${NC}"
    fi
else
    # Try pipx first, then fall back to pip3
    if command_exists pipx; then
        pipx install semgrep
        echo -e "${GREEN}Semgrep installed via pipx successfully!${NC}"
    else
        pip3 install --break-system-packages semgrep
        echo -e "${GREEN}Semgrep installed via pip3 successfully!${NC}"
    fi
fi
semgrep --version

echo ""
echo -e "${GREEN}=================================="
echo "Installation Complete!"
echo -e "==================================${NC}"
echo ""
echo "Installed tools:"
echo "  - Gitleaks: $(gitleaks version)"
echo "  - Semgrep: $(semgrep --version)"
echo ""
echo "Next steps:"
echo "  1. Run './scan-security.sh' to perform a security scan"
echo "  2. Review the generated reports in the current directory"
echo "  3. Set up pre-commit hooks with './setup-precommit-hooks.sh'"
echo ""
