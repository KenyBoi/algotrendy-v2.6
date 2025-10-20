# ⚠️ SECURITY WARNING - ACTION REQUIRED
**AlgoTrendy Upgrade Tools Installation**

📅 **Created**: October 19, 2025
🔒 **Priority**: HIGH
⏰ **Action Required**: Before production use

---

## 🚨 Temporary Credentials Detected

During installation, a **temporary password** was set for demonstration purposes:

```
Username: postgres
Password: algotrendy_dev_pass_2025
```

### Where This Password Appears:
- `/root/AlgoTrendy_v2.6/ansible/upgrade_algotrendy.yml`
- `/root/AlgoTrendy_v2.6/scripts/emergency_restore.sh`
- `/root/AlgoTrendy_v2.6/INSTALLATION_COMPLETE.md`
- In-memory (Liquibase commands)

---

## ✅ Required Actions Before Production

### 1. Change PostgreSQL Password (CRITICAL)

```bash
# Generate a strong password (recommended)
NEW_PASSWORD=$(openssl rand -base64 32)

# Change postgres user password
sudo -u postgres psql -c "ALTER USER postgres PASSWORD '$NEW_PASSWORD';"

# Save password securely
echo "$NEW_PASSWORD" > ~/.postgres_password
chmod 600 ~/.postgres_password

echo "✅ New password saved to ~/.postgres_password"
```

---

### 2. Secure Ansible Playbook (CRITICAL)

Option A: Use Ansible Vault (Recommended)
```bash
# Create encrypted password variable
ansible-vault encrypt_string "$NEW_PASSWORD" --name 'db_password'

# Copy the output and update upgrade_algotrendy.yml
# Replace: db_password: "algotrendy_dev_pass_2025"
# With: db_password: !vault |
#         $ANSIBLE_VAULT;1.1;AES256...
```

Option B: Use Environment Variables
```bash
# Edit playbook to use environment variable
# Replace: db_password: "algotrendy_dev_pass_2025"
# With: db_password: "{{ lookup('env', 'DB_PASSWORD') }}"

# Set environment variable before running
export DB_PASSWORD="your_secure_password"
ansible-playbook upgrade_algotrendy.yml
```

---

### 3. Secure Emergency Restore Script

```bash
# Edit emergency_restore.sh
nano /root/AlgoTrendy_v2.6/scripts/emergency_restore.sh

# Replace hardcoded password with:
read -sp "Enter postgres password: " DB_PASSWORD
echo ""

# Then use $DB_PASSWORD in Liquibase commands
```

---

### 4. Use Azure Key Vault (Production Best Practice)

```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure
az login

# Create/use existing Key Vault
az keyvault secret set --vault-name "algotrendy-vault" \
  --name "postgres-password" \
  --value "$NEW_PASSWORD"

# Retrieve in scripts
DB_PASSWORD=$(az keyvault secret show --vault-name "algotrendy-vault" \
  --name "postgres-password" --query value -o tsv)
```

---

## 📋 Security Checklist

Before deploying to production:

- [ ] **Change postgres password** to strong random password
- [ ] **Remove hardcoded passwords** from all scripts
- [ ] **Use Ansible Vault** or environment variables
- [ ] **Set up Azure Key Vault** for production secrets
- [ ] **Restrict file permissions**:
  ```bash
  chmod 600 /root/AlgoTrendy_v2.6/ansible/upgrade_algotrendy.yml
  chmod 600 /root/AlgoTrendy_v2.6/scripts/emergency_restore.sh
  chmod 600 ~/.postgres_password
  ```
- [ ] **Enable PostgreSQL SSL** connections
- [ ] **Restrict pg_hba.conf** to specific IPs
- [ ] **Review audit logs** regularly
- [ ] **Rotate passwords** quarterly

---

## 🔒 Additional Security Hardening

### PostgreSQL SSL Connections

```bash
# Generate SSL certificate
cd /root/AlgoTrendy_v2.6/scripts
./generate-ssl-cert.sh

# Enable SSL in postgresql.conf
echo "ssl = on" >> /etc/postgresql/16/main/postgresql.conf
echo "ssl_cert_file = '/var/lib/postgresql/server.crt'" >> /etc/postgresql/16/main/postgresql.conf
echo "ssl_key_file = '/var/lib/postgresql/server.key'" >> /etc/postgresql/16/main/postgresql.conf

# Restart PostgreSQL
systemctl restart postgresql
```

### Restrict Database Access

```bash
# Edit pg_hba.conf to require SSL
sudo nano /etc/postgresql/16/main/pg_hba.conf

# Change this:
# host    all             all             127.0.0.1/32            scram-sha-256

# To this (requires SSL):
# hostssl all             all             127.0.0.1/32            scram-sha-256
```

### pgBackRest Encryption

```bash
# Add to /etc/pgbackrest.conf
[global]
repo1-cipher-type=aes-256-cbc
repo1-cipher-pass=YOUR_ENCRYPTION_PASSPHRASE

# Restart backups with encryption
sudo -u postgres pgbackrest --stanza=algotrendy backup --type=full
```

---

## 📊 Current Security Status

Run this command to check current security status:

```bash
/root/AlgoTrendy_v2.6/scripts/health_check.sh
```

---

## 🆘 If Credentials Are Compromised

If you suspect the temporary password has been exposed:

1. **Immediately change password**:
   ```bash
   sudo -u postgres psql -c "ALTER USER postgres PASSWORD 'NEW_SECURE_PASSWORD';"
   ```

2. **Review database logs**:
   ```bash
   sudo tail -100 /var/log/postgresql/postgresql-16-main.log
   ```

3. **Check for unauthorized access**:
   ```bash
   sudo -u postgres psql -c "SELECT * FROM pg_stat_activity WHERE usename = 'postgres';"
   ```

4. **Rotate all related credentials**

---

## 📞 Security Support

- PostgreSQL Security: https://www.postgresql.org/support/security/
- Azure Key Vault: https://docs.microsoft.com/en-us/azure/key-vault/
- Ansible Vault: https://docs.ansible.com/ansible/latest/vault_guide/

---

**⚠️ DO NOT ignore this warning! Hardcoded credentials are a major security risk.**

**Status**: ⚠️ TEMPORARY CREDENTIALS IN USE
**Action Required**: YES
**Priority**: HIGH
**Deadline**: Before production deployment

---

Last Updated: October 19, 2025
