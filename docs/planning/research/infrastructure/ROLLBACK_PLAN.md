# AlgoTrendy v2.6 - Deployment Rollback Plan

**Document Version:** 1.0
**Created:** October 19, 2025
**Last Updated:** October 19, 2025

---

## 🎯 Purpose

This document provides step-by-step procedures for rolling back AlgoTrendy v2.6 deployment in case of failure or critical issues during deployment.

---

## 🚨 When to Execute Rollback

Trigger rollback immediately if any of the following occur:

### Critical Issues (Immediate Rollback)
- ❌ Services fail to start after 5 minutes
- ❌ Database corruption or data loss detected
- ❌ API returns 500 errors consistently (>80% failure rate)
- ❌ Memory leak causing system instability
- ❌ Security vulnerability discovered in deployment
- ❌ Complete loss of connectivity to exchanges
- ❌ Data integrity violations detected

### Major Issues (Rollback Recommended)
- ⚠️ Performance degradation >50% compared to baseline
- ⚠️ Critical features non-functional (order placement, market data)
- ⚠️ SSL certificate issues blocking user access
- ⚠️ Health checks failing consistently
- ⚠️ Resource usage exceeding system capacity

### Minor Issues (Rollback Optional)
- 📌 Non-critical feature bugs
- 📌 UI display issues
- 📌 Minor performance degradation <20%
- 📌 Logging issues

---

## 📋 Pre-Rollback Checklist

Before initiating rollback:

1. **Document the Issue**
   ```bash
   # Capture current state
   docker-compose ps > /tmp/deployment_state.txt
   docker-compose logs > /tmp/deployment_logs.txt
   docker stats --no-stream > /tmp/resource_usage.txt

   # Note issue details
   echo "Issue: [describe issue]" > /tmp/rollback_reason.txt
   echo "Time: $(date)" >> /tmp/rollback_reason.txt
   ```

2. **Notify Stakeholders**
   - Alert team members
   - Notify users if production
   - Document in incident log

3. **Verify Backup Availability**
   ```bash
   ls -lh /root/AlgoTrendy_v2.6/backups/questdb_backup_*.tar.gz | tail -3
   ```

4. **Confirm Rollback Authority**
   - Technical lead approval (if available)
   - Document decision maker

---

## 🔄 Rollback Procedures

### Option 1: Quick Rollback (Services Running, No Data Loss)

**Use when:** Services deployed but malfunctioning, no database changes

**Time to Complete:** 2-5 minutes

```bash
# 1. Stop current services
cd /root/AlgoTrendy_v2.6
docker-compose -f docker-compose.prod.yml down

# 2. Remove current containers and networks
docker-compose -f docker-compose.prod.yml rm -f

# 3. Remove current image (if needed)
docker rmi algotrendy-api:v2.6-prod

# 4. Restart with previous configuration
# (assuming you have a previous docker-compose or git tag)
git checkout v2.5  # or previous stable version
docker-compose -f docker-compose.prod.yml up -d

# 5. Verify services
docker-compose ps
curl http://localhost:5002/health
```

---

### Option 2: Full Rollback with Database Restore

**Use when:** Database corrupted or data integrity issues

**Time to Complete:** 10-15 minutes

```bash
# 1. Stop all services
cd /root/AlgoTrendy_v2.6
docker-compose -f docker-compose.prod.yml down

# 2. Identify latest good backup
LATEST_BACKUP=$(ls -t /root/AlgoTrendy_v2.6/backups/questdb_backup_*.tar.gz | head -1)
echo "Using backup: $LATEST_BACKUP"

# 3. Verify backup checksum (if available)
BACKUP_NAME=$(basename $LATEST_BACKUP .tar.gz)
if [ -f "/root/AlgoTrendy_v2.6/backups/${BACKUP_NAME}.sha256" ]; then
    cd /root/AlgoTrendy_v2.6/backups
    sha256sum -c "${BACKUP_NAME}.sha256"
fi

# 4. Backup current state (even if corrupted, for forensics)
docker volume inspect algotrendy_v26_questdb_data > /tmp/volume_state_before_rollback.txt
sudo tar -czf /tmp/failed_deployment_$(date +%Y%m%d_%H%M%S).tar.gz \
  /var/lib/docker/volumes/algotrendy_v26_questdb_data/_data

# 5. Remove corrupted volume
docker volume rm algotrendy_v26_questdb_data

# 6. Recreate volume
docker volume create algotrendy_v26_questdb_data

# 7. Restore from backup
VOLUME_PATH=$(docker volume inspect algotrendy_v26_questdb_data --format '{{.Mountpoint}}')
sudo tar -xzf $LATEST_BACKUP -C $(dirname $VOLUME_PATH) --strip-components=1

# 8. Verify restore
sudo ls -la $VOLUME_PATH/db/

# 9. Restart services
docker-compose -f docker-compose.prod.yml up -d

# 10. Wait for startup
sleep 30

# 11. Verify health
docker-compose ps
curl http://localhost:5002/health
docker-compose logs questdb | grep "server started"
```

---

### Option 3: Git-Based Rollback

**Use when:** Code changes caused issues

**Time to Complete:** 5-10 minutes

```bash
# 1. Stop services
cd /root/AlgoTrendy_v2.6
docker-compose -f docker-compose.prod.yml down

# 2. Check git history
git log --oneline -10

# 3. Identify stable commit/tag
git tag -l  # List all tags

# 4. Rollback to previous version
git checkout tags/v2.5  # or specific commit hash
# OR
git reset --hard HEAD~1  # rollback one commit

# 5. Rebuild and restart
docker build -f backend/Dockerfile -t algotrendy-api:v2.5 .
docker-compose -f docker-compose.prod.yml up -d

# 6. Verify
curl http://localhost:5002/health
```

---

### Option 4: Emergency Stop (Last Resort)

**Use when:** System stability at risk, need immediate shutdown

**Time to Complete:** 1 minute

```bash
# 1. Emergency stop all containers
docker stop $(docker ps -q)

# 2. Remove all AlgoTrendy containers
docker ps -a | grep algotrendy | awk '{print $1}' | xargs docker rm -f

# 3. Free up resources
docker system prune -f

# 4. Notify team
echo "EMERGENCY STOP executed at $(date)" | tee /tmp/emergency_stop.log

# 5. System is now offline - investigate before restart
```

---

## 🔍 Post-Rollback Verification

After completing rollback, verify:

### 1. Service Health
```bash
# Check all containers running
docker-compose ps
# Expected: All services "Up"

# Test API health
curl http://localhost:5002/health
# Expected: "Healthy"

# Test QuestDB
curl http://localhost:9000
# Expected: HTTP 200 response
```

### 2. Data Integrity
```bash
# Check database has data
docker-compose exec questdb sh -c \
  "curl -G http://localhost:9000/exec --data-urlencode 'query=SELECT COUNT(*) FROM market_data'"
# Expected: COUNT > 0

# Verify recent data
docker-compose logs api | grep "Market data fetched" | tail -5
```

### 3. Exchange Connectivity
```bash
# Check broker connections
docker-compose logs api | grep -i "binance" | tail -10
# Expected: "Connected to Binance" or similar
```

### 4. Resource Usage
```bash
# Monitor for 2 minutes
docker stats --no-stream
# Expected: Normal CPU/Memory levels
```

### 5. Functionality Tests
```bash
# Test market data endpoint
curl http://localhost:5002/api/market-data/binance/btcusdt
# Expected: JSON array with recent data

# Check logs for errors
docker-compose logs --tail=50 | grep -i "error\|exception\|fail"
# Expected: No critical errors
```

---

## 📊 Rollback Decision Matrix

| Scenario | Rollback Type | Estimated Downtime | Data Loss Risk |
|----------|---------------|-------------------|----------------|
| Service won't start | Quick Rollback | 2-5 min | None |
| API errors | Quick Rollback | 2-5 min | None |
| Database corruption | Full with Restore | 10-15 min | Minimal* |
| Code bugs | Git Rollback | 5-10 min | None |
| Security issue | Emergency Stop | 1 min | None |
| Performance issues | Quick Rollback | 2-5 min | None |

*Data loss limited to transactions since last backup (max 24 hours if daily backups)

---

## 🛠️ Troubleshooting Common Rollback Issues

### Issue: Backup restore fails

**Symptoms:** `tar` command errors, incomplete restore

**Solution:**
```bash
# Verify backup integrity
tar -tzf /root/AlgoTrendy_v2.6/backups/questdb_backup_YYYYMMDD.tar.gz > /dev/null
echo $?  # Should be 0

# Try alternate backup
ls -lt /root/AlgoTrendy_v2.6/backups/questdb_backup_*.tar.gz | head -3
```

### Issue: Containers won't stop

**Symptoms:** `docker-compose down` hangs

**Solution:**
```bash
# Force stop
docker-compose -f docker-compose.prod.yml kill

# Force remove
docker-compose -f docker-compose.prod.yml rm -f

# If still hanging, restart Docker
sudo systemctl restart docker
```

### Issue: Volume won't delete

**Symptoms:** "volume is in use" error

**Solution:**
```bash
# Find what's using it
docker ps -a | grep questdb

# Force remove containers
docker rm -f algotrendy-questdb-prod

# Now remove volume
docker volume rm algotrendy_v26_questdb_data
```

### Issue: Git conflicts during rollback

**Symptoms:** "Your local changes" error

**Solution:**
```bash
# Stash current changes
git stash

# Force checkout
git checkout -f tags/v2.5

# If needed, hard reset
git reset --hard tags/v2.5
```

---

## 📝 Post-Incident Actions

After successful rollback:

1. **Document the Incident**
   - Root cause analysis
   - Timeline of events
   - Impact assessment
   - Lessons learned

2. **Update Deployment Checklist**
   - Add new validation steps
   - Update rollback procedures
   - Improve monitoring

3. **Fix the Issue**
   - Address root cause
   - Test in staging
   - Plan re-deployment

4. **Communicate**
   - Status update to team
   - User notification (if production)
   - Management report

5. **Archive Evidence**
   ```bash
   mkdir -p /root/AlgoTrendy_v2.6/incidents/$(date +%Y%m%d)
   mv /tmp/*deployment* /root/AlgoTrendy_v2.6/incidents/$(date +%Y%m%d)/
   mv /tmp/rollback_reason.txt /root/AlgoTrendy_v2.6/incidents/$(date +%Y%m%d)/
   ```

---

## 🔐 Rollback Authority & Escalation

### Decision Makers

| Issue Severity | Authority Required | Max Decision Time |
|----------------|-------------------|-------------------|
| Critical (P1) | Any engineer on-call | Immediate |
| Major (P2) | Senior engineer | 15 minutes |
| Minor (P3) | Tech lead approval | 1 hour |

### Escalation Path

1. Engineer on-call → Make initial assessment
2. Senior Engineer → Approve rollback if P2
3. Tech Lead → Approve rollback if P3, review all rollbacks
4. Engineering Manager → Post-incident review

### Emergency Contact

```
On-Call Engineer: [TBD]
Tech Lead: [TBD]
Emergency Hotline: [TBD]
Slack Channel: #algotrendy-incidents
```

---

## 📞 Quick Reference

### Common Commands

```bash
# Stop services
docker-compose -f docker-compose.prod.yml down

# Quick restart
docker-compose -f docker-compose.prod.yml restart

# View logs
docker-compose logs -f api

# Check health
curl http://localhost:5002/health

# List backups
ls -lht /root/AlgoTrendy_v2.6/backups/*.tar.gz

# Restore backup (replace TIMESTAMP)
sudo tar -xzf /root/AlgoTrendy_v2.6/backups/questdb_backup_TIMESTAMP.tar.gz \
  -C /var/lib/docker/volumes/algotrendy_v26_questdb_data/_data
```

---

**Last Updated:** October 19, 2025
**Maintained By:** DevOps Team
**Review Schedule:** Quarterly or after each incident
