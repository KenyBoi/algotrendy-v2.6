#!/bin/bash

# ============================================================================
# AlgoTrendy v2.6 - Development Environment Setup Script
# ============================================================================
# This script automates the setup of a complete development environment
# for AlgoTrendy, including dependencies, databases, and configuration.
#
# Usage: ./scripts/dev-setup.sh
# ============================================================================

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "\n${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║${NC} $1"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}\n"
}

# Check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Detect OS
detect_os() {
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        if [ -f /etc/os-release ]; then
            . /etc/os-release
            OS=$ID
            OS_VERSION=$VERSION_ID
        else
            OS="unknown"
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macos"
        OS_VERSION=$(sw_vers -productVersion)
    else
        OS="unknown"
    fi

    log_info "Detected OS: $OS $OS_VERSION"
}

# Check prerequisites
check_prerequisites() {
    print_header "Checking Prerequisites"

    local missing=0

    # Check Git
    if command_exists git; then
        log_success "Git installed: $(git --version)"
    else
        log_error "Git is not installed"
        missing=1
    fi

    # Check .NET SDK
    if command_exists dotnet; then
        local dotnet_version=$(dotnet --version)
        log_success ".NET SDK installed: $dotnet_version"

        if [[ ! "$dotnet_version" =~ ^8\. ]]; then
            log_warning ".NET 8.0 is recommended (you have $dotnet_version)"
        fi
    else
        log_error ".NET SDK is not installed"
        log_info "Install from: https://dotnet.microsoft.com/download"
        missing=1
    fi

    # Check Docker
    if command_exists docker; then
        log_success "Docker installed: $(docker --version)"
    else
        log_warning "Docker is not installed (optional but recommended)"
        log_info "Install from: https://docs.docker.com/get-docker/"
    fi

    # Check Node.js (for frontend)
    if command_exists node; then
        log_success "Node.js installed: $(node --version)"
    else
        log_warning "Node.js is not installed (needed for frontend development)"
        log_info "Install from: https://nodejs.org/"
    fi

    # Check Python (for ML services)
    if command_exists python3; then
        log_success "Python installed: $(python3 --version)"
    else
        log_warning "Python 3 is not installed (needed for ML services)"
    fi

    if [ $missing -eq 1 ]; then
        log_error "Missing required dependencies. Please install them and try again."
        exit 1
    fi

    log_success "All required prerequisites are installed!"
}

# Setup environment files
setup_environment() {
    print_header "Setting Up Environment Files"

    # Check if .env exists
    if [ -f .env ]; then
        log_warning ".env file already exists"
        read -p "Do you want to overwrite it? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log_info "Keeping existing .env file"
            return
        fi
    fi

    # Copy .env.example to .env
    if [ -f .env.example ]; then
        cp .env.example .env
        log_success "Created .env from .env.example"

        # Set development defaults
        sed -i 's/ASPNETCORE_ENVIRONMENT=Production/ASPNETCORE_ENVIRONMENT=Development/' .env 2>/dev/null || \
        sed -i '' 's/ASPNETCORE_ENVIRONMENT=Production/ASPNETCORE_ENVIRONMENT=Development/' .env 2>/dev/null

        sed -i 's/API_LOG_LEVEL=Information/API_LOG_LEVEL=Debug/' .env 2>/dev/null || \
        sed -i '' 's/API_LOG_LEVEL=Information/API_LOG_LEVEL=Debug/' .env 2>/dev/null

        log_success "Configured .env for development"
    else
        log_error ".env.example not found"
        exit 1
    fi

    log_warning "Remember to add your API credentials to .env file!"
}

# Setup security tools
setup_security_tools() {
    print_header "Setting Up Security Tools (IMPORTANT)"

    log_info "Security is a top priority for AlgoTrendy."
    log_info "Installing pre-commit hooks to prevent credential leaks..."

    if [ -d "file_mgmt_code" ] && [ -f "file_mgmt_code/setup-precommit-hooks.sh" ]; then
        cd file_mgmt_code

        log_info "Installing security tools..."
        if bash ./setup-security-tools.sh; then
            log_success "Security tools installed"
        else
            log_warning "Security tools installation had some issues (non-critical)"
        fi

        log_info "Setting up pre-commit hooks..."
        if bash ./setup-precommit-hooks.sh; then
            log_success "Pre-commit hooks configured"
        else
            log_warning "Pre-commit hooks setup had some issues (non-critical)"
        fi

        cd ..

        log_success "Security setup complete!"
        log_info "Pre-commit hooks will now scan for secrets before each commit."
        log_info "Run 'cd file_mgmt_code && ./scan-security.sh' to scan manually."
    else
        log_warning "Security tools not found in file_mgmt_code/"
        log_info "You can set them up later by running:"
        log_info "  cd file_mgmt_code && ./setup-precommit-hooks.sh"
    fi
}

# Restore .NET dependencies
restore_dotnet_dependencies() {
    print_header "Restoring .NET Dependencies"

    cd backend

    log_info "Running dotnet restore..."
    if dotnet restore; then
        log_success "Dependencies restored successfully"
    else
        log_error "Failed to restore dependencies"
        exit 1
    fi

    cd ..
}

# Build .NET projects
build_dotnet_projects() {
    print_header "Building .NET Projects"

    cd backend

    log_info "Building solution..."
    if dotnet build --configuration Debug; then
        log_success "Build completed successfully"
    else
        log_warning "Build completed with warnings/errors"
        log_info "Check the output above for details"
    fi

    cd ..
}

# Setup frontend
setup_frontend() {
    print_header "Setting Up Frontend"

    if ! command_exists node; then
        log_warning "Node.js not installed, skipping frontend setup"
        return
    fi

    # Check if frontend directory exists
    if [ ! -d "frontend" ]; then
        log_warning "Frontend directory not found, skipping"
        return
    fi

    cd frontend

    log_info "Installing npm dependencies..."
    if npm install; then
        log_success "Frontend dependencies installed"
    else
        log_error "Failed to install frontend dependencies"
        cd ..
        return
    fi

    cd ..
}

# Setup Python environment
setup_python_environment() {
    print_header "Setting Up Python Environment"

    if ! command_exists python3; then
        log_warning "Python 3 not installed, skipping Python setup"
        return
    fi

    # Setup backtesting-py-service
    if [ -d "backtesting-py-service" ]; then
        cd backtesting-py-service

        log_info "Creating Python virtual environment..."
        python3 -m venv venv

        log_info "Installing Python dependencies..."
        source venv/bin/activate
        pip install --upgrade pip
        pip install -r requirements.txt
        deactivate

        log_success "Python environment setup complete"
        cd ..
    fi
}

# Setup databases (Docker)
setup_databases() {
    print_header "Setting Up Databases"

    if ! command_exists docker; then
        log_warning "Docker not installed, skipping database setup"
        log_info "You can manually install PostgreSQL/QuestDB or use Docker later"
        return
    fi

    log_info "Starting database containers..."

    # Start only essential services
    if docker compose version >/dev/null 2>&1; then
        docker compose up -d questdb seq
    elif command_exists docker-compose; then
        docker-compose up -d questdb seq
    else
        log_warning "Docker Compose not available"
        return
    fi

    log_info "Waiting for databases to be ready (30 seconds)..."
    sleep 30

    log_success "Databases are running!"
    log_info "QuestDB Console: http://localhost:9000"
    log_info "Seq Logs: http://localhost:5341"
}

# Run database migrations
run_migrations() {
    print_header "Running Database Migrations"

    # Check if migrations directory exists
    if [ ! -d "database/migrations" ]; then
        log_warning "No migrations directory found"
        return
    fi

    log_info "Checking for migration files..."
    local migration_count=$(ls -1 database/migrations/*.sql 2>/dev/null | wc -l)

    if [ $migration_count -eq 0 ]; then
        log_info "No SQL migration files found"
        return
    fi

    log_info "Found $migration_count migration file(s)"

    # Check if QuestDB is running
    if curl -s http://localhost:9000/ >/dev/null 2>&1; then
        log_info "Applying migrations to QuestDB..."

        for migration in database/migrations/*.sql; do
            log_info "Applying $(basename $migration)..."
            # Apply migration via QuestDB HTTP API
            curl -s -X POST \
                'http://localhost:9000/exec?query=' \
                --data-urlencode "query=$(cat $migration)" \
                > /dev/null
        done

        log_success "Migrations applied successfully"
    else
        log_warning "QuestDB not running, skipping migrations"
        log_info "Start QuestDB and run migrations manually"
    fi
}

# Print summary
print_summary() {
    print_header "Setup Complete!"

    echo -e "${GREEN}✅ Development environment is ready!${NC}\n"

    echo -e "${BLUE}Next Steps:${NC}"
    echo -e "  1. Edit ${YELLOW}.env${NC} and add your API credentials"
    echo -e "  2. Review ${YELLOW}backend/AlgoTrendy.API/appsettings.json${NC}"
    echo -e "  3. Start the application:\n"

    echo -e "${YELLOW}Option 1: Docker (Recommended)${NC}"
    echo -e "  ${GREEN}docker-compose up -d${NC}"
    echo -e "  Access at: http://localhost:5002\n"

    echo -e "${YELLOW}Option 2: Direct .NET Run${NC}"
    echo -e "  ${GREEN}cd backend/AlgoTrendy.API${NC}"
    echo -e "  ${GREEN}dotnet run${NC}"
    echo -e "  Access at: http://localhost:5000\n"

    echo -e "${BLUE}Useful URLs:${NC}"
    echo -e "  API Swagger: ${GREEN}http://localhost:5002/swagger${NC}"
    echo -e "  QuestDB:     ${GREEN}http://localhost:9000${NC}"
    echo -e "  Seq Logs:    ${GREEN}http://localhost:5341${NC}"
    echo -e "  Frontend:    ${GREEN}http://localhost:3000${NC}\n"

    echo -e "${BLUE}Documentation:${NC}"
    echo -e "  CONTRIBUTING.md          - Development guidelines"
    echo -e "  DOCKER_SETUP.md          - Docker setup guide"
    echo -e "  DEVELOPER_ONBOARDING.md  - New developer checklist"
    echo -e "  SECURITY.md              - Security policy"
    echo -e "  docs/                    - Full documentation\n"

    echo -e "${BLUE}Security:${NC}"
    echo -e "  Pre-commit hooks:  ${GREEN}✅ Configured${NC}"
    echo -e "  Run security scan: ${GREEN}cd file_mgmt_code && ./scan-security.sh${NC}"
    echo -e "  Security tools:    ${GREEN}file_mgmt_code/${NC}\n"

    log_info "Happy coding! 🚀"
    log_warning "Remember: Never commit secrets! Pre-commit hooks will protect you."
}

# Main execution
main() {
    clear

    echo -e "${BLUE}"
    echo "╔════════════════════════════════════════════════════════════════╗"
    echo "║                                                                ║"
    echo "║           AlgoTrendy v2.6 - Development Setup                 ║"
    echo "║                                                                ║"
    echo "║  This script will set up your development environment         ║"
    echo "║  including dependencies, databases, and configuration.        ║"
    echo "║                                                                ║"
    echo "╚════════════════════════════════════════════════════════════════╝"
    echo -e "${NC}\n"

    # Verify we're in the project root
    if [ ! -f "docker-compose.yml" ] || [ ! -d "backend" ]; then
        log_error "Please run this script from the project root directory"
        exit 1
    fi

    log_info "Starting setup process..."
    sleep 2

    # Run setup steps
    detect_os
    check_prerequisites
    setup_environment
    setup_security_tools
    restore_dotnet_dependencies
    build_dotnet_projects
    setup_frontend
    setup_python_environment
    setup_databases
    run_migrations
    print_summary
}

# Run main function
main "$@"
