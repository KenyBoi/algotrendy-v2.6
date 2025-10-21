# Step-by-Step Installation Guide
**Open-Source Version Upgrade Tools for AlgoTrendy**

**Target System**: Ubuntu/Debian Linux
**Estimated Time**: 2-3 hours for complete setup
**Difficulty**: Intermediate

---

## Prerequisites

```bash
# Update system
sudo apt-get update
sudo apt-get upgrade -y

# Install basic dependencies
sudo apt-get install -y \
  wget \
  curl \
  git \
  python3 \
  python3-pip \
  openjdk-11-jdk \
  postgresql-client

# Verify installations
python3 --version
java -version
psql --version
```

---

## 1. Install pgBackRest (30 minutes)

### Step 1.1: Install Package
```bash
# Ubuntu/Debian
sudo apt-get install -y pgbackrest

# Verify installation
pgbackrest version
```

### Step 1.2: Configure PostgreSQL
```bash
# Edit PostgreSQL config
sudo nano /etc/postgresql/14/main/postgresql.conf
```

Add these lines:
```ini
archive_mode = on
archive_command = 'pgbackrest --stanza=algotrendy archive-push %p'
max_wal_senders = 3
wal_level = replica
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### Step 1.3: Configure pgBackRest
```bash
# Create config file
sudo nano /etc/pgbackrest.conf
```

Add this configuration:
```ini
[global]
repo1-path=/var/lib/pgbackrest
repo1-retention-full=2
repo1-retention-diff=4
log-level-console=info
log-level-file=debug
start-fast=y

[algotrendy]
pg1-path=/var/lib/postgresql/14/main
pg1-port=5432
pg1-user=postgres
```

### Step 1.4: Initialize and Test
```bash
# Create stanza
sudo -u postgres pgbackrest --stanza=algotrendy stanza-create

# Run first backup
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full

# Verify
sudo -u postgres pgbackrest --stanza=algotrendy info

# Expected output:
# stanza: algotrendy
#     status: ok
#     cipher: none
#     db (current)
#         wal archive min/max (14): ...
#     full backup: 20251019-120000F
```

### Step 1.5: Setup Automated Backups
```bash
# Create cron job
sudo crontab -e
```

Add these lines:
```cron
# Full backup every Sunday at 2 AM
0 2 * * 0 pgbackrest --stanza=algotrendy --type=full backup

# Incremental backup daily at 2 AM
0 2 * * 1-6 pgbackrest --stanza=algotrendy --type=incr backup
```

✅ **pgBackRest installed and configured**

---

## 2. Install Liquibase (1 hour)

### Step 2.1: Download and Install
```bash
# Download Liquibase
cd /tmp
wget https://github.com/liquibase/liquibase/releases/download/v4.25.1/liquibase-4.25.1.tar.gz

# Extract to /opt
sudo mkdir -p /opt/liquibase
sudo tar -xzf liquibase-4.25.1.tar.gz -C /opt/liquibase

# Add to PATH
echo 'export PATH=$PATH:/opt/liquibase' >> ~/.bashrc
source ~/.bashrc

# Verify installation
liquibase --version
# Expected: Liquibase Version: 4.25.1
```

### Step 2.2: Install PostgreSQL JDBC Driver
```bash
# Download PostgreSQL JDBC driver
cd /opt/liquibase/lib
sudo wget https://jdbc.postgresql.org/download/postgresql-42.7.1.jar

# Verify
ls -la /opt/liquibase/lib/postgresql-*.jar
```

### Step 2.3: Create Liquibase Configuration
```bash
# Create directory structure
cd /root/AlgoTrendy_v2.6
mkdir -p database/migrations
cd database
```

Create `liquibase.properties`:
```bash
cat > liquibase.properties <<EOF
changeLogFile=db.changelog.xml
url=jdbc:postgresql://localhost:5432/algotrendy_production
username=algotrendy
password=\${DB_PASSWORD}
driver=org.postgresql.Driver
classpath=/opt/liquibase/lib/postgresql-42.7.1.jar
logLevel=INFO
EOF
```

Create `.env` for password:
```bash
cat > .env <<EOF
DB_PASSWORD=your_actual_password_here
EOF

chmod 600 .env
```

### Step 2.4: Generate Initial Changelog
```bash
# Set password environment variable
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)

# Generate changelog from existing database
liquibase --changeLogFile=db.changelog.xml \
  --url=jdbc:postgresql://localhost:5432/algotrendy_production \
  --username=algotrendy \
  --password=$DB_PASSWORD \
  generateChangeLog

# Tag current version
liquibase tag v2.5

# Verify
cat db.changelog.xml | head -20
```

### Step 2.5: Test Liquibase
```bash
# Create test changelog
cat > migrations/test_migration.xml <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
    http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.25.xsd">

    <changeSet id="1" author="algotrendy">
        <createTable tableName="test_table">
            <column name="id" type="int">
                <constraints primaryKey="true"/>
            </column>
            <column name="name" type="varchar(100)"/>
        </createTable>
    </changeSet>
</databaseChangeLog>
EOF

# Apply migration
liquibase --changeLogFile=migrations/test_migration.xml update

# Verify table created
psql -U algotrendy -d algotrendy_production -c "\dt test_table"

# Rollback
liquibase --changeLogFile=migrations/test_migration.xml rollback v2.5

# Verify table removed
psql -U algotrendy -d algotrendy_production -c "\dt test_table"
# Expected: Did not find any relation named "test_table"
```

✅ **Liquibase installed and tested**

---

## 3. Install Ansible (30 minutes)

### Step 3.1: Install via pip
```bash
# Install Ansible
pip3 install ansible

# Verify installation
ansible --version
# Expected: ansible [core 2.15.x]

# Install PostgreSQL collection
ansible-galaxy collection install community.postgresql
```

### Step 3.2: Create Ansible Configuration
```bash
cd /root/AlgoTrendy_v2.6
mkdir -p ansible

cat > ansible/ansible.cfg <<EOF
[defaults]
inventory = inventory.ini
host_key_checking = False
retry_files_enabled = False
log_path = /var/log/ansible.log

[privilege_escalation]
become = True
become_method = sudo
become_user = root
EOF
```

### Step 3.3: Create Inventory
```bash
cat > ansible/inventory.ini <<EOF
[local]
localhost ansible_connection=local

[algotrendy_servers]
localhost ansible_connection=local

[all:vars]
ansible_python_interpreter=/usr/bin/python3
EOF
```

### Step 3.4: Test Ansible
```bash
cd ansible

# Test connection
ansible all -m ping
# Expected: localhost | SUCCESS => { "ping": "pong" }

# Test command execution
ansible all -m shell -a "uptime"

# Test PostgreSQL module
ansible all -m postgresql_query \
  -a "db=algotrendy_production login_user=algotrendy query='SELECT version()'" \
  --become-user=postgres
```

### Step 3.5: Create Upgrade Playbook
```bash
cat > ansible/upgrade_algotrendy.yml <<'EOF'
---
- name: Upgrade AlgoTrendy v2.5 → v2.6
  hosts: localhost
  vars:
    backup_dir: "/backups/{{ ansible_date_time.iso8601 }}"
    app_dir: "/root/AlgoTrendy_v2.6"

  tasks:
    - name: Create backup directory
      file:
        path: "{{ backup_dir }}"
        state: directory
        mode: '0755'

    - name: Backup PostgreSQL database
      become: yes
      become_user: postgres
      shell: |
        pgbackrest --stanza=algotrendy backup --type=full
      register: backup_result

    - name: Display backup result
      debug:
        var: backup_result.stdout

    - name: Tag current Liquibase version
      shell: |
        export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
        liquibase tag v2.5-pre-upgrade-{{ ansible_date_time.iso8601 }}
      args:
        chdir: "{{ app_dir }}/database"

    - name: Apply database migrations
      shell: |
        export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
        liquibase update
      args:
        chdir: "{{ app_dir }}/database"
      register: migration_result
      ignore_errors: yes

    - name: Check migration status
      debug:
        msg: "Migration {{ 'succeeded' if migration_result.rc == 0 else 'FAILED' }}"

    - name: Rollback on migration failure
      shell: |
        export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
        liquibase rollback v2.5
      args:
        chdir: "{{ app_dir }}/database"
      when: migration_result.failed

    - name: Build v2.6 application
      shell: |
        dotnet publish -c Release -o /opt/algotrendy/v2.6
      args:
        chdir: "{{ app_dir }}/backend"
      when: not migration_result.failed

    - name: Create upgrade report
      copy:
        content: |
          AlgoTrendy Upgrade Report
          =========================
          Date: {{ ansible_date_time.iso8601 }}
          Status: {{ 'SUCCESS' if not migration_result.failed else 'FAILED' }}
          Backup Location: {{ backup_dir }}

          Migration Output:
          {{ migration_result.stdout }}

          {{ migration_result.stderr if migration_result.failed else '' }}
        dest: "{{ backup_dir }}/UPGRADE_REPORT.txt"

    - name: Display upgrade summary
      debug:
        msg: |
          ============================================
          Upgrade Complete!
          Status: {{ 'SUCCESS ✅' if not migration_result.failed else 'FAILED ❌' }}
          Backup: {{ backup_dir }}
          Report: {{ backup_dir }}/UPGRADE_REPORT.txt
          ============================================
EOF

# Test playbook syntax
ansible-playbook ansible/upgrade_algotrendy.yml --syntax-check
```

✅ **Ansible installed and configured**

---

## 4. Install Optional Tools

### 4.1: Apache NiFi (Optional)
```bash
# Using Docker (easiest method)
docker run -d \
  --name nifi \
  -p 8443:8443 \
  -e SINGLE_USER_CREDENTIALS_USERNAME=admin \
  -e SINGLE_USER_CREDENTIALS_PASSWORD=$(openssl rand -base64 32) \
  apache/nifi:latest

# Get the generated password
docker logs nifi 2>&1 | grep "Generated"

# Access: https://localhost:8443/nifi
```

### 4.2: Alembic (Optional - for v2.5 Python code)
```bash
pip3 install alembic

# Initialize in v2.5 directory
cd /root/algotrendy_v2.5/algotrendy
alembic init alembic

# Configure alembic.ini
sed -i 's|sqlalchemy.url = .*|sqlalchemy.url = postgresql://algotrendy:password@localhost/algotrendy_production|' alembic.ini
```

### 4.3: DVC (Optional - for data versioning)
```bash
pip3 install dvc[s3]

# Initialize in project
cd /root/AlgoTrendy_v2.6
dvc init
```

---

## 5. Verification & Testing

### Step 5.1: Test Full Backup & Restore
```bash
# Create test database
createdb -U algotrendy test_restore

# Restore from latest backup
sudo -u postgres pgbackrest --stanza=algotrendy \
  --pg1-path=/tmp/test_restore \
  --type=immediate restore

# Verify data
psql -U algotrendy -d test_restore -c "SELECT COUNT(*) FROM users;"

# Cleanup
dropdb -U algotrendy test_restore
```

### Step 5.2: Test Liquibase Rollback
```bash
cd /root/AlgoTrendy_v2.6/database

# Create test migration
cat > migrations/test_rollback.xml <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<databaseChangeLog
    xmlns="http://www.liquibase.org/xml/ns/dbchangelog"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.liquibase.org/xml/ns/dbchangelog
    http://www.liquibase.org/xml/ns/dbchangelog/dbchangelog-4.25.xsd">

    <changeSet id="test-1" author="test">
        <createTable tableName="rollback_test">
            <column name="id" type="int"/>
        </createTable>
    </changeSet>
</databaseChangeLog>
EOF

# Apply migration
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase --changeLogFile=migrations/test_rollback.xml update

# Verify table exists
psql -U algotrendy -d algotrendy_production -c "\dt rollback_test"

# Tag it
liquibase tag before-rollback-test

# Rollback
liquibase rollback v2.5

# Verify table is gone
psql -U algotrendy -d algotrendy_production -c "\dt rollback_test"
# Expected: Did not find any relation
```

### Step 5.3: Test Ansible Playbook (Dry Run)
```bash
cd /root/AlgoTrendy_v2.6

# Run in check mode (no changes)
ansible-playbook ansible/upgrade_algotrendy.yml --check

# Run with verbose output
ansible-playbook ansible/upgrade_algotrendy.yml -vv
```

---

## 6. Create Helper Scripts

### Step 6.1: Backup Script
```bash
cat > /root/AlgoTrendy_v2.6/scripts/quick_backup.sh <<'EOF'
#!/bin/bash
set -e

echo "🔒 Creating quick backup..."

# Backup database
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full

# Tag Liquibase
cd /root/AlgoTrendy_v2.6/database
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase tag "backup-$(date +%Y%m%d-%H%M%S)"

echo "✅ Backup complete!"
pgbackrest --stanza=algotrendy info
EOF

chmod +x /root/AlgoTrendy_v2.6/scripts/quick_backup.sh
```

### Step 6.2: Restore Script
```bash
cat > /root/AlgoTrendy_v2.6/scripts/emergency_restore.sh <<'EOF'
#!/bin/bash
set -e

echo "⚠️  EMERGENCY RESTORE"
echo "This will restore the database to the last backup"
read -p "Are you sure? (type YES): " confirm

if [ "$confirm" != "YES" ]; then
    echo "Restore cancelled"
    exit 1
fi

# Stop application
sudo systemctl stop algotrendy || true

# Restore from pgBackRest
sudo -u postgres pgbackrest --stanza=algotrendy restore

# Start PostgreSQL
sudo systemctl start postgresql

# Rollback Liquibase to v2.5
cd /root/AlgoTrendy_v2.6/database
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase rollback v2.5

echo "✅ Restore complete!"
EOF

chmod +x /root/AlgoTrendy_v2.6/scripts/emergency_restore.sh
```

### Step 6.3: Health Check Script
```bash
cat > /root/AlgoTrendy_v2.6/scripts/health_check.sh <<'EOF'
#!/bin/bash

echo "🔍 AlgoTrendy System Health Check"
echo "=================================="

# PostgreSQL
echo -n "PostgreSQL: "
if systemctl is-active --quiet postgresql; then
    echo "✅ Running"
else
    echo "❌ Stopped"
fi

# pgBackRest
echo -n "Latest Backup: "
sudo -u postgres pgbackrest --stanza=algotrendy info --output=json | \
  jq -r '.[0].backup[0].timestamp.stop' 2>/dev/null || echo "❌ No backups"

# Liquibase
echo -n "Database Version: "
cd /root/AlgoTrendy_v2.6/database
export DB_PASSWORD=$(cat .env | grep DB_PASSWORD | cut -d'=' -f2)
liquibase history 2>/dev/null | tail -3 | head -1 || echo "❌ Error"

# Application
echo -n "Application: "
curl -s http://localhost:5002/health > /dev/null && echo "✅ Healthy" || echo "❌ Down"

echo "=================================="
EOF

chmod +x /root/AlgoTrendy_v2.6/scripts/health_check.sh
```

---

## 7. Final Verification

```bash
# Run health check
/root/AlgoTrendy_v2.6/scripts/health_check.sh

# Expected output:
# 🔍 AlgoTrendy System Health Check
# ==================================
# PostgreSQL: ✅ Running
# Latest Backup: 2025-10-19 12:00:00
# Database Version: v2.5
# Application: ✅ Healthy
# ==================================
```

---

## 📋 Installation Checklist

- [ ] pgBackRest installed and first backup completed
- [ ] Automated backups scheduled (cron)
- [ ] Liquibase installed and changelog generated
- [ ] Database tagged with v2.5
- [ ] Rollback tested successfully
- [ ] Ansible installed and playbook created
- [ ] Playbook tested in check mode
- [ ] Helper scripts created and tested
- [ ] Health check script runs without errors
- [ ] All tools verified with test commands

---

## 🆘 Troubleshooting

### pgBackRest Issues

**Error: stanza not found**
```bash
sudo -u postgres pgbackrest --stanza=algotrendy stanza-create
```

**Error: archive command failed**
```bash
# Check PostgreSQL logs
sudo tail -f /var/log/postgresql/postgresql-14-main.log

# Verify permissions
sudo chown -R postgres:postgres /var/lib/pgbackrest
```

### Liquibase Issues

**Error: Driver not found**
```bash
# Verify JDBC driver
ls -la /opt/liquibase/lib/postgresql-*.jar

# Re-download if missing
cd /opt/liquibase/lib
sudo wget https://jdbc.postgresql.org/download/postgresql-42.7.1.jar
```

**Error: Connection refused**
```bash
# Test PostgreSQL connection
psql -U algotrendy -d algotrendy_production -c "SELECT 1;"

# Check credentials in liquibase.properties
```

### Ansible Issues

**Error: Module not found**
```bash
# Reinstall collections
ansible-galaxy collection install community.postgresql --force
```

**Error: Permission denied**
```bash
# Check become settings
ansible all -m shell -a "whoami" --become
```

---

## 📞 Support

If you encounter issues:
1. Check logs: `/var/log/ansible.log`, PostgreSQL logs
2. Verify configurations: `liquibase.properties`, `pgbackrest.conf`
3. Test components individually before integration
4. Refer to COMPLETE_TOOLS_CATALOG.md for detailed documentation

---

**Installation Complete!** ✅

Next steps:
1. Review RECOMMENDED_TOOLS.md for usage guide
2. Test backup/restore procedure
3. Practice upgrade in staging environment
4. Document any custom configurations

**Last Updated**: October 19, 2025
