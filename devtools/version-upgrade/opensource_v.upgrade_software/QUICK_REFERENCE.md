# Quick Reference Guide
**One-Page Cheat Sheet for AlgoTrendy Upgrade Tools**

---

## 🚀 Quick Commands

### Backup Database
```bash
# Full backup
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full

# Quick backup script
/root/AlgoTrendy_v2.6/scripts/quick_backup.sh
```

### Tag Current Version
```bash
cd /root/AlgoTrendy_v2.6/database
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase tag v2.5
```

### Run Upgrade
```bash
cd /root/AlgoTrendy_v2.6
ansible-playbook ansible/upgrade_algotrendy.yml
```

### Emergency Rollback
```bash
# Option 1: Liquibase (schema only)
cd /root/AlgoTrendy_v2.6/database
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase rollback v2.5

# Option 2: Full restore (schema + data)
/root/AlgoTrendy_v2.6/scripts/emergency_restore.sh
```

### Health Check
```bash
/root/AlgoTrendy_v2.6/scripts/health_check.sh
```

---

## 📊 Tool Comparison

| Need | Use This | Command |
|------|----------|---------|
| **Backup database** | pgBackRest | `pgbackrest backup` |
| **Restore database** | pgBackRest | `pgbackrest restore` |
| **Track schema changes** | Liquibase | `liquibase update` |
| **Rollback schema** | Liquibase | `liquibase rollback v2.5` |
| **Automate upgrade** | Ansible | `ansible-playbook upgrade.yml` |
| **Check system** | health_check.sh | `./health_check.sh` |

---

## 🔄 Typical Upgrade Workflow

```bash
# 1. Backup
pgbackrest --stanza=algotrendy backup --type=full

# 2. Tag current version
liquibase tag v2.5

# 3. Run upgrade
ansible-playbook upgrade.yml

# 4. If failed, rollback
liquibase rollback v2.5

# 5. Check health
./health_check.sh
```

---

## 📁 Important Files

```
/var/lib/pgbackrest/           # Backups stored here
/opt/liquibase/                # Liquibase program
/root/AlgoTrendy_v2.6/
├── database/
│   ├── db.changelog.xml       # Database change history
│   └── liquibase.properties   # DB connection config
├── ansible/
│   └── upgrade_algotrendy.yml # Automated upgrade
└── scripts/
    ├── quick_backup.sh        # Quick backup
    ├── emergency_restore.sh   # Emergency restore
    └── health_check.sh        # System check
```

---

## 🆘 Emergency Procedures

### System is Down
```bash
# 1. Check what's running
systemctl status postgresql
systemctl status algotrendy

# 2. Check health
./health_check.sh

# 3. View logs
tail -f /var/log/postgresql/postgresql-14-main.log
tail -f /var/log/ansible.log
```

### Upgrade Failed
```bash
# 1. STAY CALM - You have backups!

# 2. Rollback database schema
cd /root/AlgoTrendy_v2.6/database
liquibase rollback v2.5

# 3. Restore previous code
git checkout v2.5

# 4. Restart application
sudo systemctl restart algotrendy

# 5. Verify
curl http://localhost:5002/health
```

### Data Corrupted
```bash
# 1. Stop application
sudo systemctl stop algotrendy

# 2. Restore from latest backup
sudo -u postgres pgbackrest --stanza=algotrendy restore

# 3. Start PostgreSQL
sudo systemctl start postgresql

# 4. Verify data
psql -U algotrendy -d algotrendy_production -c "SELECT COUNT(*) FROM users;"

# 5. Restart application
sudo systemctl start algotrendy
```

---

## 🔍 Troubleshooting

### pgBackRest

**Problem**: Backup fails
```bash
# Check logs
sudo tail -50 /var/log/pgbackrest/algotrendy-backup.log

# Verify stanza
sudo -u postgres pgbackrest --stanza=algotrendy check

# Recreate stanza if needed
sudo -u postgres pgbackrest --stanza=algotrendy stanza-delete --force
sudo -u postgres pgbackrest --stanza=algotrendy stanza-create
```

**Problem**: Can't restore
```bash
# List available backups
sudo -u postgres pgbackrest --stanza=algotrendy info

# Restore specific backup
sudo -u postgres pgbackrest --stanza=algotrendy --set=20251019-120000F restore
```

### Liquibase

**Problem**: Connection failed
```bash
# Test database connection
psql -U algotrendy -d algotrendy_production -c "SELECT 1;"

# Check credentials
cat /root/AlgoTrendy_v2.6/database/liquibase.properties

# Test with verbose
liquibase --logLevel=DEBUG update
```

**Problem**: Rollback fails
```bash
# View change history
liquibase history

# List tags
liquibase tag-exists v2.5

# Rollback to specific changeset
liquibase rollback-count 1
```

### Ansible

**Problem**: Playbook fails
```bash
# Run with verbose output
ansible-playbook upgrade.yml -vvv

# Run in check mode (dry run)
ansible-playbook upgrade.yml --check

# Test specific task
ansible all -m ping
```

**Problem**: Module not found
```bash
# Reinstall collections
ansible-galaxy collection install community.postgresql --force

# Verify installation
ansible-galaxy collection list
```

---

## 📈 Monitoring

### Check Backup Status
```bash
# View backup info
sudo -u postgres pgbackrest --stanza=algotrendy info

# Expected output:
# full backup: 20251019-120000F
#     timestamp start/stop: 2025-10-19 12:00:00 / 12:05:00
#     database size: 2.1GB, backup size: 2.1GB
```

### Check Database Version
```bash
# View Liquibase history
cd /root/AlgoTrendy_v2.6/database
liquibase history

# View current tag
psql -U algotrendy -d algotrendy_production -c \
  "SELECT tag FROM databasechangelog WHERE tag IS NOT NULL ORDER BY dateexecuted DESC LIMIT 1;"
```

### Check Disk Space
```bash
# Backup directory
du -sh /var/lib/pgbackrest/

# Database size
sudo -u postgres psql -c \
  "SELECT pg_size_pretty(pg_database_size('algotrendy_production'));"
```

---

## ⏰ Scheduled Tasks

### Cron Jobs
```bash
# View cron jobs
sudo crontab -l

# Expected:
# 0 2 * * 0 pgbackrest --stanza=algotrendy --type=full backup
# 0 2 * * 1-6 pgbackrest --stanza=algotrendy --type=incr backup
```

### Backup Retention
```bash
# pgBackRest keeps:
- 2 full backups
- 4 differential backups
- Automatic cleanup of old backups
```

---

## 🎯 Best Practices

### Before Every Upgrade
1. ✅ Run full backup
2. ✅ Tag current version
3. ✅ Test in staging first
4. ✅ Have rollback plan ready
5. ✅ Schedule during low-traffic time

### After Every Upgrade
1. ✅ Run health check
2. ✅ Verify user data intact
3. ✅ Check application logs
4. ✅ Monitor for 24 hours
5. ✅ Document what changed

### Regular Maintenance
1. ✅ Verify backups weekly
2. ✅ Test restore procedure monthly
3. ✅ Review disk space monthly
4. ✅ Update tools quarterly
5. ✅ Review change logs quarterly

---

## 📞 Support Resources

### Official Documentation
- Liquibase: https://docs.liquibase.com/
- pgBackRest: https://pgbackrest.org/user-guide.html
- Ansible: https://docs.ansible.com/

### Community
- Liquibase Forum: https://forum.liquibase.org/
- PostgreSQL Slack: https://pgtreats.info/slack-invite
- Ansible Forum: https://forum.ansible.com/

### AlgoTrendy Docs
- Complete catalog: `COMPLETE_TOOLS_CATALOG.md`
- Installation guide: `INSTALLATION_GUIDE.md`
- What tools do: `WHAT_THESE_TOOLS_DO.md`

---

## 🔐 Security Checklist

- [ ] Database password in `.env` file (not in code)
- [ ] `.env` file permissions: `chmod 600 .env`
- [ ] Backup directory permissions: `700` (postgres only)
- [ ] No secrets in `db.changelog.xml`
- [ ] No secrets in Ansible playbooks
- [ ] Backups encrypted (if storing off-site)

---

## 📊 Success Metrics

Track these after implementing tools:

| Metric | Before | After Goal |
|--------|--------|-----------|
| **Upgrade time** | 2-4 hours | 10-20 minutes |
| **Failed upgrades** | 30-40% | <5% |
| **Data loss incidents** | 1-2/year | 0 |
| **Recovery time** | 4-8 hours | 2-5 minutes |
| **Downtime per upgrade** | 1-2 hours | <10 minutes |

---

## 💡 Pro Tips

1. **Always backup before experimenting**
   ```bash
   pgbackrest backup && liquibase tag before-experiment
   ```

2. **Use descriptive tags**
   ```bash
   liquibase tag v2.5-before-leverage-feature-$(date +%Y%m%d)
   ```

3. **Test rollback before upgrade**
   ```bash
   # In staging:
   liquibase update
   liquibase rollback v2.5
   # If this works, proceed to production
   ```

4. **Keep changelogs in git**
   ```bash
   git add database/db.changelog.xml
   git commit -m "Add leverage column migration"
   ```

5. **Automate everything**
   ```bash
   # Don't run manual commands
   # Use: ansible-playbook upgrade.yml
   ```

---

**Print this page and keep it handy during upgrades!**

**Last Updated**: October 19, 2025
