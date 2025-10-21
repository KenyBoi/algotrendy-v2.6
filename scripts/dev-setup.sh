#!/bin/bash
# ==============================================================================
# AlgoTrendy v2.6 - Development Environment Setup Script
# ==============================================================================
# This script automates the setup of the development environment including:
#   - Dependency installation
#   - Database setup
#   - Initial configuration
#   - Test data seeding
# ==============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_ROOT=$(pwd)
BACKEND_DIR="$PROJECT_ROOT/backend"
FRONTEND_DIR="$PROJECT_ROOT/docs/design/algotrendy_browser_figma"

# ==============================================================================
# Helper Functions
# ==============================================================================

print_header() {
    echo -e "${BLUE}============================================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}============================================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

check_command() {
    if command -v $1 &> /dev/null; then
        print_success "$1 is installed"
        return 0
    else
        print_error "$1 is not installed"
        return 1
    fi
}

# ==============================================================================
# System Requirements Check
# ==============================================================================

print_header "Checking System Requirements"

REQUIREMENTS_MET=true

# Check .NET SDK
if check_command dotnet; then
    DOTNET_VERSION=$(dotnet --version)
    print_info "  Version: $DOTNET_VERSION"
    if [[ "$DOTNET_VERSION" < "8.0" ]]; then
        print_warning "  .NET 8.0 or higher is recommended"
    fi
else
    print_error "  Install from: https://dotnet.microsoft.com/download"
    REQUIREMENTS_MET=false
fi

# Check Docker
if check_command docker; then
    DOCKER_VERSION=$(docker --version | awk '{print $3}' | tr -d ',')
    print_info "  Version: $DOCKER_VERSION"
else
    print_error "  Install from: https://docs.docker.com/get-docker/"
    REQUIREMENTS_MET=false
fi

# Check Docker Compose
if check_command docker-compose; then
    COMPOSE_VERSION=$(docker-compose --version | awk '{print $4}' | tr -d ',')
    print_info "  Version: $COMPOSE_VERSION"
else
    print_warning "  Docker Compose not found, trying 'docker compose'"
    if docker compose version &> /dev/null; then
        print_success "  'docker compose' command available"
    else
        print_error "  Install Docker Compose"
        REQUIREMENTS_MET=false
    fi
fi

# Check Git
if check_command git; then
    GIT_VERSION=$(git --version | awk '{print $3}')
    print_info "  Version: $GIT_VERSION"
else
    print_error "  Install from: https://git-scm.com/downloads"
    REQUIREMENTS_MET=false
fi

# Check Node.js (for frontend)
if check_command node; then
    NODE_VERSION=$(node --version)
    print_info "  Version: $NODE_VERSION"
else
    print_warning "  Node.js not found (optional for frontend development)"
fi

# Check npm (for frontend)
if check_command npm; then
    NPM_VERSION=$(npm --version)
    print_info "  Version: $NPM_VERSION"
else
    print_warning "  npm not found (optional for frontend development)"
fi

if [ "$REQUIREMENTS_MET" = false ]; then
    print_error "Please install missing requirements before continuing"
    exit 1
fi

echo ""

# ==============================================================================
# Install .NET Dependencies
# ==============================================================================

print_header "Installing .NET Dependencies"

cd "$BACKEND_DIR"

print_info "Restoring NuGet packages..."
if dotnet restore; then
    print_success "NuGet packages restored successfully"
else
    print_error "Failed to restore NuGet packages"
    exit 1
fi

echo ""

# ==============================================================================
# Build Backend
# ==============================================================================

print_header "Building Backend"

print_info "Building solution..."
if dotnet build; then
    print_success "Backend built successfully"
else
    print_error "Backend build failed"
    exit 1
fi

echo ""

# ==============================================================================
# Setup Docker Environment
# ==============================================================================

print_header "Setting up Docker Environment"

cd "$PROJECT_ROOT"

# Create required directories
print_info "Creating required directories..."
mkdir -p questdb-data
mkdir -p logs
mkdir -p ssl
mkdir -p certbot/www
mkdir -p certbot/conf
print_success "Directories created"

# Check if Docker is running
if docker info &> /dev/null; then
    print_success "Docker daemon is running"
else
    print_error "Docker daemon is not running. Please start Docker and try again."
    exit 1
fi

# Start Docker services
print_info "Starting Docker services..."
if docker-compose up -d questdb; then
    print_success "Docker services started"
else
    print_error "Failed to start Docker services"
    exit 1
fi

# Wait for QuestDB to be ready
print_info "Waiting for QuestDB to be ready..."
sleep 10

if docker ps | grep -q algotrendy-questdb; then
    print_success "QuestDB is running"
else
    print_warning "QuestDB may not be running properly"
fi

echo ""

# ==============================================================================
# Setup User Secrets
# ==============================================================================

print_header "Setting up User Secrets"

cd "$BACKEND_DIR/AlgoTrendy.API"

# Initialize user secrets
print_info "Initializing user secrets..."
if dotnet user-secrets init; then
    print_success "User secrets initialized"
else
    print_warning "User secrets may already be initialized"
fi

# Check if credentials script exists
if [ -f "$PROJECT_ROOT/quick_setup_credentials.sh" ]; then
    print_info "Credential setup script available at: ./quick_setup_credentials.sh"
    print_info "Run it manually to configure API credentials"
else
    print_warning "Credential setup script not found"
fi

echo ""

# ==============================================================================
# Setup Frontend (if available)
# ==============================================================================

print_header "Setting up Frontend"

if [ -d "$FRONTEND_DIR" ]; then
    cd "$FRONTEND_DIR"

    if [ -f "package.json" ]; then
        if command -v npm &> /dev/null; then
            print_info "Installing frontend dependencies..."
            if npm install; then
                print_success "Frontend dependencies installed"
            else
                print_error "Failed to install frontend dependencies"
            fi
        else
            print_warning "npm not available, skipping frontend setup"
        fi
    else
        print_warning "package.json not found in frontend directory"
    fi
else
    print_warning "Frontend directory not found, skipping frontend setup"
fi

cd "$PROJECT_ROOT"
echo ""

# ==============================================================================
# Run Tests
# ==============================================================================

print_header "Running Tests"

cd "$BACKEND_DIR"

print_info "Running unit tests..."
if dotnet test --filter "FullyQualifiedName!~Integration" --verbosity quiet; then
    print_success "All tests passed"
else
    print_warning "Some tests failed (this is normal for a fresh setup)"
fi

echo ""

# ==============================================================================
# Create .env file if it doesn't exist
# ==============================================================================

print_header "Environment Configuration"

cd "$PROJECT_ROOT"

if [ ! -f ".env" ]; then
    print_info "Creating .env file from template..."
    if [ -f ".env.example" ]; then
        cp .env.example .env
        print_success ".env file created"
        print_warning "Please edit .env file with your actual credentials"
    else
        print_warning ".env.example not found, skipping .env creation"
    fi
else
    print_success ".env file already exists"
fi

echo ""

# ==============================================================================
# Generate SSL Certificates (for HTTPS development)
# ==============================================================================

print_header "SSL Certificate Setup"

if [ ! -f "ssl/key.pem" ] || [ ! -f "ssl/cert.pem" ]; then
    print_info "Generating self-signed SSL certificates for development..."
    if [ -f "scripts/generate-ssl-cert.sh" ]; then
        if bash scripts/generate-ssl-cert.sh; then
            print_success "SSL certificates generated"
        else
            print_warning "Failed to generate SSL certificates"
        fi
    else
        print_warning "SSL generation script not found"
    fi
else
    print_success "SSL certificates already exist"
fi

echo ""

# ==============================================================================
# Summary
# ==============================================================================

print_header "Setup Complete!"

echo ""
echo -e "${GREEN}✓ Development environment is ready!${NC}"
echo ""
echo -e "${BLUE}Next Steps:${NC}"
echo ""
echo "1. Configure credentials:"
echo -e "   ${YELLOW}./quick_setup_credentials.sh${NC}"
echo ""
echo "2. Start the API:"
echo -e "   ${YELLOW}cd backend/AlgoTrendy.API${NC}"
echo -e "   ${YELLOW}dotnet run${NC}"
echo ""
echo "3. Start the frontend (in another terminal):"
echo -e "   ${YELLOW}cd docs/design/algotrendy_browser_figma${NC}"
echo -e "   ${YELLOW}npm run dev${NC}"
echo ""
echo "4. Access the application:"
echo -e "   ${YELLOW}Frontend: http://localhost:3000${NC}"
echo -e "   ${YELLOW}API:      http://localhost:5002${NC}"
echo -e "   ${YELLOW}Swagger:  http://localhost:5002/swagger${NC}"
echo -e "   ${YELLOW}QuestDB:  http://localhost:9000${NC}"
echo ""
echo -e "${BLUE}Useful Commands:${NC}"
echo ""
echo "  Start all services:    docker-compose up -d"
echo "  Stop all services:     docker-compose down"
echo "  View logs:             docker-compose logs -f"
echo "  Run tests:             dotnet test"
echo "  Build solution:        dotnet build"
echo ""
echo -e "${BLUE}Documentation:${NC}"
echo ""
echo "  README:                ./README.md"
echo "  Contributing:          ./CONTRIBUTING.md"
echo "  Credentials Guide:     ./CREDENTIALS_SETUP_GUIDE.md"
echo ""
print_header "Happy Coding! 🚀"
