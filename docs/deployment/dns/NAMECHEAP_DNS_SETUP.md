# Namecheap DNS Setup for AlgoTrendy.com

This guide will help you configure DNS settings for algotrendy.com on Namecheap to point to your server.

## Prerequisites

1. You need your server's public IP address
2. Access to Namecheap account with algotrendy.com domain

## Finding Your Server's IP Address

On your server, run:
```bash
curl -4 ifconfig.me
```

This will display your server's public IPv4 address (e.g., `123.456.789.012`)

## DNS Configuration Steps

### 1. Log into Namecheap

1. Go to https://www.namecheap.com
2. Click "Sign In" and log into your account
3. Go to "Domain List"
4. Find `algotrendy.com` and click "Manage"

### 2. Configure Advanced DNS

1. Click on the "Advanced DNS" tab
2. You'll see a section called "Host Records"

### 3. Add/Update DNS Records

Add or update the following records (replace `YOUR_SERVER_IP` with your actual server IP):

| Type  | Host | Value           | TTL       |
|-------|------|-----------------|-----------|
| A     | @    | YOUR_SERVER_IP  | Automatic |
| A     | www  | YOUR_SERVER_IP  | Automatic |
| A     | api  | YOUR_SERVER_IP  | Automatic |
| A     | app  | YOUR_SERVER_IP  | Automatic |

#### Detailed Instructions:

**Record 1: Main Domain (@)**
- Click "+ ADD NEW RECORD"
- Type: `A Record`
- Host: `@`
- Value: `YOUR_SERVER_IP` (e.g., `123.456.789.012`)
- TTL: `Automatic`
- Click the checkmark ✓

**Record 2: WWW Subdomain**
- Click "+ ADD NEW RECORD"
- Type: `A Record`
- Host: `www`
- Value: `YOUR_SERVER_IP`
- TTL: `Automatic`
- Click the checkmark ✓

**Record 3: API Subdomain**
- Click "+ ADD NEW RECORD"
- Type: `A Record`
- Host: `api`
- Value: `YOUR_SERVER_IP`
- TTL: `Automatic`
- Click the checkmark ✓

**Record 4: APP Subdomain**
- Click "+ ADD NEW RECORD"
- Type: `A Record`
- Host: `app`
- Value: `YOUR_SERVER_IP`
- TTL: `Automatic`
- Click the checkmark ✓

### 4. Save Changes

Click "Save all changes" button at the bottom of the page.

## DNS Propagation

After saving:
- DNS changes can take anywhere from 5 minutes to 48 hours to fully propagate
- Typically, changes are visible within 15-30 minutes
- You can check DNS propagation at: https://dnschecker.org

## Verify DNS Configuration

After waiting 15-30 minutes, verify your DNS setup:

```bash
# Check main domain
nslookup algotrendy.com

# Check www subdomain
nslookup www.algotrendy.com

# Check api subdomain
nslookup api.algotrendy.com

# Check app subdomain
nslookup app.algotrendy.com
```

All should return your server's IP address.

## Example Configuration Screenshot Reference

Your final DNS records should look like this:

```
┌──────────────────────────────────────────────────────────┐
│ HOST RECORDS                                             │
├──────┬──────┬─────────────────┬──────────────────────────┤
│ Type │ Host │ Value           │ TTL                      │
├──────┼──────┼─────────────────┼──────────────────────────┤
│ A    │ @    │ 123.456.789.012 │ Automatic                │
│ A    │ www  │ 123.456.789.012 │ Automatic                │
│ A    │ api  │ 123.456.789.012 │ Automatic                │
│ A    │ app  │ 123.456.789.012 │ Automatic                │
└──────┴──────┴─────────────────┴──────────────────────────┘
```

## After DNS is Configured

Once DNS is configured and propagating:

1. **Wait for DNS propagation** (15-30 minutes typically)

2. **Set up SSL certificates**:
   ```bash
   cd /root/AlgoTrendy_v2.6
   sudo ./scripts/setup-ssl.sh
   ```

3. **Start production services**:
   ```bash
   docker-compose -f docker-compose.prod.yml up -d
   ```

4. **Verify the website is accessible**:
   - https://algotrendy.com
   - https://www.algotrendy.com
   - https://api.algotrendy.com/swagger
   - https://app.algotrendy.com

## Troubleshooting

### DNS Not Resolving

If DNS isn't resolving after 1 hour:
1. Check that you clicked "Save all changes" in Namecheap
2. Verify the IP address is correct
3. Try flushing your local DNS cache:
   ```bash
   # On Linux
   sudo systemd-resolve --flush-caches

   # On macOS
   sudo dscacheutil -flushcache

   # On Windows
   ipconfig /flushdns
   ```

### SSL Certificate Issues

If SSL setup fails:
1. Ensure DNS is fully propagated first
2. Check firewall allows ports 80 and 443:
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   ```
3. Check nginx logs:
   ```bash
   docker logs algotrendy-nginx-prod
   ```

### CORS Issues

If you get CORS errors from your frontend:
1. Check that the domain is in the CORS whitelist
2. Ensure you're using HTTPS (not HTTP)
3. Check backend logs:
   ```bash
   docker logs algotrendy-api-prod
   ```

## Optional: Email Records

If you want to set up email for @algotrendy.com (e.g., support@algotrendy.com), you'll need to add MX records. Common options:

### Using Google Workspace (formerly G Suite)
Contact Google Workspace for MX record configuration

### Using ProtonMail
Contact ProtonMail for MX record configuration

### Using Namecheap Email
Follow Namecheap's email hosting setup guide

## Need Help?

If you encounter issues:
1. Check Namecheap's support documentation: https://www.namecheap.com/support/
2. Review the AlgoTrendy deployment logs
3. Check the main README.md for additional deployment information

## Next Steps

After DNS and SSL are configured:
1. Update your Figma design to use `https://api.algotrendy.com/api`
2. Deploy your frontend to the server
3. Configure environment variables for production
4. Set up monitoring and backups

---

**Last Updated**: 2025-10-20
**AlgoTrendy Version**: v2.6
