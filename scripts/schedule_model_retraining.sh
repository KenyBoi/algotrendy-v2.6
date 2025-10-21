#!/bin/bash
# Automated ML Model Retraining Scheduler
# Runs daily at 2 AM UTC

set -e

echo "════════════════════════════════════════════════════════════════"
echo "  🤖 AUTOMATED ML MODEL RETRAINING SCHEDULER"
echo "════════════════════════════════════════════════════════════════"
echo "Started: $(date)"

# Configuration
PROJECT_DIR="/root/AlgoTrendy_v2.6"
VENV_DIR="/tmp/analysis_venv"
LOG_DIR="$PROJECT_DIR/logs/model_retraining"
BACKUP_DIR="$PROJECT_DIR/ml_models/backups"

# Create directories
mkdir -p "$LOG_DIR"
mkdir -p "$BACKUP_DIR"

# Log file with timestamp
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
LOG_FILE="$LOG_DIR/retrain_$TIMESTAMP.log"

# Function to log with timestamp
log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

log "════════════════════════════════════════════════════════════════"
log "Starting automated model retraining"
log "Log file: $LOG_FILE"

# Backup existing models
log "Backing up existing models..."
if [ -d "$PROJECT_DIR/ml_models/trend_reversals/latest" ]; then
    BACKUP_NAME="backup_$TIMESTAMP"
    cp -r "$PROJECT_DIR/ml_models/trend_reversals/latest" "$BACKUP_DIR/$BACKUP_NAME"
    log "✅ Backed up to: $BACKUP_DIR/$BACKUP_NAME"
else
    log "⚠️  No existing model to backup"
fi

# Activate virtual environment
log "Activating virtual environment..."
if [ -d "$VENV_DIR" ]; then
    source "$VENV_DIR/bin/activate"
    log "✅ Virtual environment activated"
else
    log "❌ Virtual environment not found at $VENV_DIR"
    log "Creating new virtual environment..."
    python3 -m venv "$VENV_DIR"
    source "$VENV_DIR/bin/activate"
    pip install -q joblib yfinance scikit-learn pandas numpy
    log "✅ Virtual environment created and packages installed"
fi

# Run retraining script
log "Starting model retraining..."
cd "$PROJECT_DIR"

if python3 retrain_model_v2.py --source live 2>&1 | tee -a "$LOG_FILE"; then
    log "✅ Model retraining completed successfully"

    # Check if new model is better
    log "Comparing new model performance..."

    # Extract version number from latest
    NEW_VERSION=$(ls -t ml_models/trend_reversals/ | grep -E '^[0-9]{8}_[0-9]{6}$' | head -1)

    if [ -n "$NEW_VERSION" ]; then
        log "✅ New model version: $NEW_VERSION"

        # Check metrics
        METRICS_FILE="$PROJECT_DIR/ml_models/trend_reversals/$NEW_VERSION/metrics.json"
        if [ -f "$METRICS_FILE" ]; then
            F1_SCORE=$(python3 -c "import json; print(json.load(open('$METRICS_FILE'))['validation_metrics']['f1'])")
            log "📊 New model F1-Score: $F1_SCORE"

            # Send notification (placeholder - integrate with your notification system)
            log "📧 Notification: Model retrained successfully with F1=$F1_SCORE"
        fi
    fi

    EXIT_CODE=0
else
    log "❌ Model retraining failed"
    log "Restoring from backup..."

    # Restore from backup if retraining failed
    LATEST_BACKUP=$(ls -t "$BACKUP_DIR" | head -1)
    if [ -n "$LATEST_BACKUP" ]; then
        rm -rf "$PROJECT_DIR/ml_models/trend_reversals/latest"
        cp -r "$BACKUP_DIR/$LATEST_BACKUP" "$PROJECT_DIR/ml_models/trend_reversals/latest"
        log "✅ Restored from backup: $LATEST_BACKUP"
    fi

    EXIT_CODE=1
fi

# Cleanup old logs (keep last 30 days)
log "Cleaning up old logs..."
find "$LOG_DIR" -name "retrain_*.log" -mtime +30 -delete
log "✅ Old logs cleaned up"

# Cleanup old backups (keep last 7)
log "Cleaning up old backups..."
ls -t "$BACKUP_DIR" | tail -n +8 | xargs -I {} rm -rf "$BACKUP_DIR/{}"
log "✅ Old backups cleaned up"

log "════════════════════════════════════════════════════════════════"
log "Completed: $(date)"
log "Exit code: $EXIT_CODE"
log "════════════════════════════════════════════════════════════════"

exit $EXIT_CODE
