# Deployment Guide

## Overview
Your trading platform consists of two parts:
1. **Frontend** (React/TypeScript) - This application
2. **Backend** (.NET/C# with QuestDB) - Your existing backend

## Frontend Deployment Options

### Option 1: Vercel (Recommended - Easiest)

Vercel is perfect for React applications and offers free hosting with great performance.

**Steps:**
1. Install Vercel CLI:
   ```bash
   npm i -g vercel
   ```

2. From your project directory, run:
   ```bash
   vercel
   ```

3. Follow the prompts:
   - Login/signup to Vercel
   - Confirm project settings
   - Deploy!

4. For production deployment:
   ```bash
   vercel --prod
   ```

**Environment Variables:**
- In Vercel dashboard, go to Project Settings → Environment Variables
- Add your backend API URL:
  - `VITE_API_BASE_URL` = `https://your-backend-api.com/api`

**Custom Domain:**
- Go to Vercel Project Settings → Domains
- Add your custom domain (e.g., trading.yourdomain.com)

---

### Option 2: Netlify

Another excellent option for React applications.

**Steps:**
1. Install Netlify CLI:
   ```bash
   npm install -g netlify-cli
   ```

2. Build your app:
   ```bash
   npm run build
   ```

3. Deploy:
   ```bash
   netlify deploy
   ```

4. For production:
   ```bash
   netlify deploy --prod
   ```

**Or use Netlify Drop:**
- Go to https://app.netlify.com/drop
- Drag and drop your `dist` folder after running `npm run build`

---

### Option 3: Docker (For Self-Hosting)

If you want to host on your own server alongside your backend.

**Create Dockerfile:**
```dockerfile
FROM node:18-alpine as builder
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Create nginx.conf:**
```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy (optional - if backend is on same server)
    location /api {
        proxy_pass http://backend:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

**Build and run:**
```bash
docker build -t trading-frontend .
docker run -p 80:80 trading-frontend
```

---

## Backend Deployment (.NET/C#)

### Option 1: Azure App Service (Recommended for .NET)

**Steps:**
1. Install Azure CLI:
   ```bash
   # macOS
   brew install azure-cli
   
   # Windows
   # Download from https://aka.ms/installazurecliwindows
   ```

2. Login to Azure:
   ```bash
   az login
   ```

3. Create App Service:
   ```bash
   az webapp up --name your-trading-api --runtime "DOTNET|8.0"
   ```

4. Configure QuestDB connection in App Settings

---

### Option 2: Docker Compose (Self-Hosting)

Create a `docker-compose.yml` to run everything together:

```yaml
version: '3.8'

services:
  frontend:
    build: ./frontend
    ports:
      - "80:80"
    depends_on:
      - backend
    environment:
      - VITE_API_BASE_URL=http://backend:5000/api

  backend:
    build: ./backend
    ports:
      - "5000:5000"
    depends_on:
      - questdb
    environment:
      - QuestDB__ConnectionString=http://questdb:9000
      - ASPNETCORE_ENVIRONMENT=Production

  questdb:
    image: questdb/questdb:latest
    ports:
      - "9000:9000"
      - "8812:8812"
      - "9009:9009"
    volumes:
      - questdb-data:/var/lib/questdb

volumes:
  questdb-data:
```

**Run everything:**
```bash
docker-compose up -d
```

---

## Configuration

### Update API Endpoint

Before deploying, update `/config/api.ts`:

```typescript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 
  (import.meta.env.DEV 
    ? 'http://localhost:5000/api' 
    : 'https://your-production-api.com/api');
```

### Environment Variables

Create `.env.production`:
```
VITE_API_BASE_URL=https://your-backend-api.com/api
```

---

## Quick Start Deployment (Recommended)

**For the fastest deployment:**

1. **Frontend on Vercel:**
   ```bash
   # In your frontend directory
   vercel --prod
   ```

2. **Backend on Azure:**
   ```bash
   # In your backend directory
   az webapp up --name your-trading-api --runtime "DOTNET|8.0"
   ```

3. **QuestDB on DigitalOcean/AWS:**
   - Launch a VM/Droplet
   - Install Docker
   - Run: `docker run -p 9000:9000 questdb/questdb`

4. **Connect them:**
   - Update Vercel environment variables with your Azure API URL
   - Update Azure app settings with your QuestDB URL

---

## Security Checklist

- [ ] Enable HTTPS on all services
- [ ] Set up CORS properly in backend
- [ ] Use environment variables for all secrets
- [ ] Enable authentication (OAuth, JWT)
- [ ] Set up API rate limiting
- [ ] Configure firewall rules for QuestDB
- [ ] Use secure WebSocket connections (WSS) for real-time data
- [ ] Implement API key rotation
- [ ] Set up monitoring and alerts

---

## Monitoring

**Frontend (Vercel):**
- Built-in analytics at vercel.com/analytics
- Error tracking with Sentry

**Backend:**
- Application Insights (Azure)
- Custom logging to QuestDB
- Health check endpoints

**QuestDB:**
- Built-in Web Console at http://your-questdb:9000
- Grafana for visualization

---

## DNS Configuration

Once deployed, point your domain:

```
trading.yourdomain.com    → Vercel (Frontend)
api.yourdomain.com        → Azure/Your backend
db.yourdomain.com         → QuestDB (restrict access!)
```

---

## Cost Estimates

**Free Tier Option:**
- Vercel: Free (with limits)
- Azure: Free tier or ~$13/month
- DigitalOcean Droplet: $6/month
- **Total: ~$19/month**

**Production Option:**
- Vercel Pro: $20/month
- Azure App Service: ~$55/month
- DigitalOcean + QuestDB: $24/month
- **Total: ~$99/month**

---

## Need Help?

Common issues:
- **CORS errors:** Configure backend to allow frontend domain
- **API not connecting:** Check environment variables
- **Build fails:** Run `npm run build` locally first
- **WebSocket issues:** Ensure WSS is enabled in production

## Next Steps

1. Deploy frontend to Vercel (5 minutes)
2. Deploy backend to Azure (15 minutes)
3. Set up QuestDB on a cloud VM (10 minutes)
4. Configure environment variables
5. Test the connection
6. Set up custom domain
7. Enable monitoring
