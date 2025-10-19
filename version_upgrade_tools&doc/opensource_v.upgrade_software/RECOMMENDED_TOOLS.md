# Recommended Open-Source Tools for AlgoTrendy Upgrades

**Problem**: Losing user data, configurations, and features during version upgrades

**Solution**: Industry-standard open-source migration toolkit

---

## 🎯 Essential Tools (Install These First)

### 1. Liquibase - Database Schema Version Control
**Purpose**: Never lose database structure or data during upgrades

```bash
# Install Liquibase
wget https://github.com/liquibase/liquibase/releases/download/v4.25.1/liquibase-4.25.1.tar.gz
tar -xzf liquibase-4.25.1.tar.gz -d /opt/liquibase
export PATH=$PATH:/opt/liquibase

# Or using Docker
docker pull liquibase/liquibase
```

**Setup for AlgoTrendy**:
```bash
cd /root/AlgoTrendy_v2.6/database

# Create initial changelog
liquibase init project

# Generate changelog from existing database
liquibase --changeLogFile=db.changelog.xml \
  --url=jdbc:postgresql://localhost:5432/algotrendy_production \
  --username=algotrendy \
  --password=$DB_PASSWORD \
  generateChangeLog

# Tag current version
liquibase tag v2.5

# Apply changes
liquibase update

# ROLLBACK if upgrade fails
liquibase rollback v2.5
```

**Why Liquibase**:
- ✅ Rollback to any previous version
- ✅ Compare databases (prod vs staging)
- ✅ Generate SQL scripts for review
- ✅ Works with PostgreSQL + QuestDB
- ✅ Track who changed what and when

---

### 2. pgBackRest - PostgreSQL Backup & Recovery
**Purpose**: Point-in-time recovery if anything goes wrong

```bash
# Install pgBackRest
sudo apt-get install pgbackrest

# Configure
sudo nano /etc/pgbackrest.conf
```

**Configuration**:
```ini
[global]
repo1-path=/var/lib/pgbackrest
repo1-retention-full=2
log-level-console=info
log-level-file=debug

[algotrendy]
pg1-path=/var/lib/postgresql/14/main
pg1-port=5432
pg1-socket-path=/var/run/postgresql
```

**Usage**:
```bash
# Full backup before upgrade
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full

# Incremental backup (daily)
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=incr

# Restore to point-in-time
sudo -u postgres pgbackrest --stanza=algotrendy restore \
  --set=20251019-120000F \
  --type=time \
  --target="2025-10-19 12:00:00"
```

**Why pgBackRest**:
- ✅ Restore to ANY point in time
- ✅ Parallel backup/restore (fast)
- ✅ Encryption support
- ✅ Automatic cleanup of old backups

---

### 3. Ansible - Upgrade Automation
**Purpose**: Automate entire upgrade process with rollback capability

```bash
# Install Ansible
pip install ansible

# Verify installation
ansible --version
```

**Create upgrade playbook**:
```yaml
# upgrade_algotrendy.yml
---
- name: Upgrade AlgoTrendy v2.5 → v2.6
  hosts: localhost
  vars:
    backup_dir: "/backups/{{ ansible_date_time.iso8601 }}"

  tasks:
    - name: Create backup directory
      file:
        path: "{{ backup_dir }}"
        state: directory

    - name: Backup PostgreSQL database
      postgresql_db:
        name: algotrendy_production
        state: dump
        target: "{{ backup_dir }}/database.sql"

    - name: Backup configuration files
      copy:
        src: /root/algotrendy_v2.5/
        dest: "{{ backup_dir }}/source_code/"

    - name: Tag Liquibase version
      shell: |
        liquibase --changeLogFile=db.changelog.xml tag v2.5
      args:
        chdir: /root/AlgoTrendy_v2.6/database

    - name: Export user data to JSON
      postgresql_query:
        db: algotrendy_production
        query: "COPY (SELECT row_to_json(u) FROM users u) TO '{{ backup_dir }}/users.json'"

    - name: Apply v2.6 database migrations
      shell: |
        liquibase update
      args:
        chdir: /root/AlgoTrendy_v2.6/database
      register: migration_result
      ignore_errors: yes

    - name: Rollback if migration failed
      shell: |
        liquibase rollback v2.5
      when: migration_result.failed

    - name: Deploy v2.6 application
      shell: |
        dotnet publish -c Release -o /opt/algotrendy/v2.6
      args:
        chdir: /root/AlgoTrendy_v2.6/backend
      when: not migration_result.failed

    - name: Restart application
      systemd:
        name: algotrendy
        state: restarted
      when: not migration_result.failed

    - name: Validate deployment
      uri:
        url: http://localhost:5002/health
        status_code: 200
      retries: 5
      delay: 10
```

**Run upgrade**:
```bash
ansible-playbook upgrade_algotrendy.yml
```

**Why Ansible**:
- ✅ Repeatable upgrades (same every time)
- ✅ Automatic rollback on failure
- ✅ No agent required
- ✅ Easy to read YAML syntax

---

### 4. Apache NiFi - Data Migration (Optional)
**Purpose**: Complex data transformations between v2.5 and v2.6

```bash
# Install via Docker
docker run -d \
  --name nifi \
  -p 8443:8443 \
  -e SINGLE_USER_CREDENTIALS_USERNAME=admin \
  -e SINGLE_USER_CREDENTIALS_PASSWORD=ctsBtRBKHRAx69EqUghvvgEvjnaLjFEB \
  apache/nifi:latest

# Access UI at https://localhost:8443/nifi
```

**Use case**: Migrate trading positions data format changes
- Visual drag-and-drop interface
- Track every data transformation
- Rollback capability

---

## 🚀 Quick Start: First Upgrade with Tools

### Step 1: Install Core Tools (15 minutes)
```bash
# Install Liquibase
wget https://github.com/liquibase/liquibase/releases/download/v4.25.1/liquibase-4.25.1.tar.gz
tar -xzf liquibase-4.25.1.tar.gz -d /opt/liquibase
echo 'export PATH=$PATH:/opt/liquibase' >> ~/.bashrc
source ~/.bashrc

# Install pgBackRest
sudo apt-get update
sudo apt-get install -y pgbackrest

# Install Ansible
pip install ansible
```

### Step 2: Initialize Liquibase (10 minutes)
```bash
cd /root/AlgoTrendy_v2.6
mkdir -p database/migrations

# Generate changelog from current database
liquibase --changeLogFile=database/db.changelog.xml \
  --url=jdbc:postgresql://localhost:5432/algotrendy_production \
  --username=algotrendy \
  --password=$DB_PASSWORD \
  generateChangeLog

# Tag current version
liquibase tag v2.5
```

### Step 3: Configure pgBackRest (10 minutes)
```bash
# Create config
sudo tee /etc/pgbackrest.conf > /dev/null <<EOF
[global]
repo1-path=/var/lib/pgbackrest
repo1-retention-full=2

[algotrendy]
pg1-path=/var/lib/postgresql/14/main
pg1-port=5432
EOF

# Create stanza
sudo -u postgres pgbackrest --stanza=algotrendy stanza-create

# First backup
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full
```

### Step 4: Test Rollback Capability (5 minutes)
```bash
# Make a test change
psql -U algotrendy -d algotrendy_production -c "CREATE TABLE test_rollback (id INT);"

# Tag it
liquibase tag test_change

# Rollback
liquibase rollback v2.5

# Verify test table is gone
psql -U algotrendy -d algotrendy_production -c "\dt test_rollback"
```

---

## 📊 Comparison: Which Tool for What?

| Problem | Tool | Why |
|---------|------|-----|
| **Database schema changes** | Liquibase | Rollback + version control |
| **User data preservation** | pgBackRest | Point-in-time recovery |
| **Configuration migration** | Ansible | Automated + repeatable |
| **Complex data transforms** | Apache NiFi | Visual + trackable |
| **Dependency updates** | Renovate | Auto PRs for packages |
| **Code quality** | SonarQube | Catch issues pre-deploy |

---

## 🎓 Learning Resources

### Liquibase
- Docs: https://docs.liquibase.com/
- Tutorial: https://docs.liquibase.com/start/home.html
- PostgreSQL Guide: https://docs.liquibase.com/start/tutorials/postgresql.html

### pgBackRest
- Docs: https://pgbackrest.org/user-guide.html
- Quick Start: https://pgbackrest.org/user-guide.html#quickstart

### Ansible
- Docs: https://docs.ansible.com/
- PostgreSQL Module: https://docs.ansible.com/ansible/latest/collections/community/postgresql/

---

## ✅ Success Checklist

Before your next upgrade, ensure:
- [ ] Liquibase is tracking all database changes
- [ ] pgBackRest has a full backup from today
- [ ] Ansible playbook tested on staging environment
- [ ] Rollback procedure tested and documented
- [ ] All secrets moved to Key Vault (not in source code)
- [ ] Migration report generated and reviewed

---

## 🆘 Emergency Rollback Procedure

If upgrade fails:

```bash
# 1. Stop application
sudo systemctl stop algotrendy

# 2. Rollback database via Liquibase
cd /root/AlgoTrendy_v2.6/database
liquibase rollback v2.5

# OR restore from pgBackRest
sudo -u postgres pgbackrest --stanza=algotrendy restore --set=<backup-id>

# 3. Restore previous code version
git checkout v2.5

# 4. Restart application
sudo systemctl start algotrendy

# 5. Verify
curl http://localhost:5002/health
```

---

**Last Updated**: October 19, 2025
**For**: AlgoTrendy v2.5 → v2.6 Migration
**Status**: Ready for implementation
