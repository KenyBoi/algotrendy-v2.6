#!/bin/bash
# AlgoTrendy v2.6 - Git Repository Initialization Script
# Sets up Git repository with branches, tags, and initial commit

set -e  # Exit on error
set -u  # Exit on undefined variable

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

V26_DIR="/root/AlgoTrendy_v2.6"

# Logging function
log() {
    echo -e "${2:-$NC}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

cd "$V26_DIR"

log "Initializing AlgoTrendy v2.6 Git Repository" "$BLUE"
log "========================================" "$BLUE"

# Check if Git is already initialized
if [ -d ".git" ]; then
    log "Git repository already initialized" "$YELLOW"
    read -p "Reinitialize? This will delete existing Git history (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log "Aborted by user" "$RED"
        exit 1
    fi
    rm -rf .git
    log "Removed existing .git directory" "$YELLOW"
fi

# Initialize Git repository
log "Initializing Git repository..." "$BLUE"
git init
git config user.name "AlgoTrendy Development Team"
git config user.email "dev@algotrendy.com"
log "✓ Git repository initialized" "$GREEN"

# Set default branch to main
log "Setting default branch to 'main'..." "$BLUE"
git branch -M main
log "✓ Default branch set to 'main'" "$GREEN"

# Create development branch
log "Creating 'development' branch..." "$BLUE"
git branch development
log "✓ Development branch created" "$GREEN"

# Create phase branches
log "Creating phase branches..." "$BLUE"
phases=("phase1-foundation" "phase2-core-trading" "phase3-ai-integration" "phase4-data-channels" "phase5-frontend" "phase6-deployment")
for phase in "${phases[@]}"; do
    git branch "$phase"
    log "✓ Branch created: $phase" "$GREEN"
done

# Create .gitattributes for line endings
log "Creating .gitattributes..." "$BLUE"
cat > .gitattributes << 'EOF'
# Auto detect text files and perform LF normalization
* text=auto

# Source code
*.cs text diff=csharp
*.py text diff=python
*.js text
*.ts text
*.tsx text
*.jsx text
*.json text
*.yml text
*.yaml text
*.md text
*.sh text eol=lf

# Binary files
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.ico binary
*.mov binary
*.mp4 binary
*.mp3 binary
*.flv binary
*.fla binary
*.swf binary
*.gz binary
*.zip binary
*.7z binary
*.ttf binary
*.eot binary
*.woff binary
*.woff2 binary
*.dll binary
*.exe binary
*.so binary
*.dylib binary
EOF
log "✓ .gitattributes created" "$GREEN"

# Create initial commit checklist
log "Preparing initial commit..." "$BLUE"
cat > INITIAL_COMMIT_CHECKLIST.md << 'EOF'
# Initial Commit Checklist

Before making the initial commit, ensure:

- [ ] All secrets removed from code
- [ ] .env file not committed (only .env.example)
- [ ] .gitignore is comprehensive
- [ ] No hardcoded API keys or passwords
- [ ] No database credentials in code
- [ ] Validation script passed: `python3 scripts/migration/validate_migration.py`
- [ ] README.md is up to date
- [ ] PROJECT_OVERVIEW.md is current
- [ ] All migration scripts are executable

## After Initial Commit

- [ ] Set up GitHub/GitLab remote repository
- [ ] Push to remote: `git remote add origin <URL>`
- [ ] Push all branches: `git push -u origin --all`
- [ ] Protect main branch (require PR reviews)
- [ ] Set up CI/CD pipelines
- [ ] Configure branch protection rules

## Git Commands Reference

```bash
# Add remote repository
git remote add origin https://github.com/yourusername/algotrendy-v2.6.git

# Push all branches
git push -u origin --all

# Push tags
git push --tags

# Create and switch to feature branch
git checkout -b feature/your-feature-name

# Merge feature to development
git checkout development
git merge feature/your-feature-name
```
EOF
log "✓ Initial commit checklist created" "$GREEN"

# Stage files for initial commit
log "Staging files for initial commit..." "$BLUE"
git add .gitignore
git add .gitattributes
git add .env.example
git add README.md
git add PROJECT_OVERVIEW.md
git add INITIAL_COMMIT_CHECKLIST.md
git add docs/
git add planning/
git add scripts/
git add backend/ 2>/dev/null || true
git add frontend/ 2>/dev/null || true
git add database/ 2>/dev/null || true
git add infrastructure/ 2>/dev/null || true

# Check what will be committed
log "" "$NC"
log "Files staged for initial commit:" "$BLUE"
git status --short

# Confirm before committing
log "" "$NC"
log "Review the staged files above." "$YELLOW"
read -p "Proceed with initial commit? (y/N): " -n 1 -r
echo

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    log "Initial commit cancelled by user" "$YELLOW"
    log "Git repository initialized but no commit made" "$YELLOW"
    log "You can manually commit when ready: git commit -m 'Initial commit'" "$BLUE"
    exit 0
fi

# Make initial commit
log "Creating initial commit..." "$BLUE"
git commit -m "Initial commit: AlgoTrendy v2.6 repository structure

- Complete directory structure for multi-stack project
- .NET 8 backend foundation
- Next.js 15 frontend foundation
- QuestDB database integration
- Migration scripts from v2.5
- Comprehensive .gitignore covering all stacks
- Security-focused configuration templates
- Phase-based development structure
- Documentation and planning artifacts

This commit represents the foundation for the v2.6 migration from v2.5.
No business logic or secrets included - only structure and templates."

log "✓ Initial commit created" "$GREEN"

# Create initial tag
log "Creating initial tag v2.6.0-init..." "$BLUE"
git tag -a v2.6.0-init -m "AlgoTrendy v2.6 - Initial repository setup"
log "✓ Tag v2.6.0-init created" "$GREEN"

# Summary
log "" "$NC"
log "========================================" "$BLUE"
log "Git Repository Initialization Complete!" "$GREEN"
log "========================================" "$BLUE"
log "" "$NC"
log "Repository Details:" "$BLUE"
log "  - Default branch: main" "$GREEN"
log "  - Development branch: development" "$GREEN"
log "  - Phase branches: 6 created" "$GREEN"
log "  - Initial commit: ✓" "$GREEN"
log "  - Initial tag: v2.6.0-init" "$GREEN"
log "" "$NC"
log "Current branch: $(git branch --show-current)" "$BLUE"
log "Total commits: $(git rev-list --count HEAD)" "$BLUE"
log "Total branches: $(git branch | wc -l)" "$BLUE"
log "" "$NC"
log "NEXT STEPS:" "$YELLOW"
log "1. Review INITIAL_COMMIT_CHECKLIST.md" "$YELLOW"
log "2. Set up remote repository (GitHub/GitLab)" "$YELLOW"
log "3. Push to remote: git push -u origin --all" "$YELLOW"
log "4. Begin Phase 1 implementation" "$YELLOW"
log "" "$NC"
