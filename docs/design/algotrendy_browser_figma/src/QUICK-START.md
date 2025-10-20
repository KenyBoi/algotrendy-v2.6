# Quick Start - Docker Deployment

## Problem: Dockerfile Directory Issue

Your `Dockerfile` appears to be a directory instead of a file. I've created `Dockerfile.production` and `.dockerignore.production` as the correct versions.

---

## How Docker Collects Files

When you run `docker build`, Docker automatically collects ALL files from your project directory, except those listed in `.dockerignore`.

**Your project structure:**
```
/ (root directory - this is where Docker starts)
├── App.tsx
├── components/
├── styles/
├── services/
├── hooks/
├── types/
├── config/
├── package.json
└── ... all other files
```

**What happens during `docker build`:**

1. **COPY package*.json ./**
   - Collects `package.json` and `package-lock.json`
   
2. **RUN npm ci**
   - Installs all dependencies from package.json
   
3. **COPY . .**
   - Collects ALL remaining files (except .dockerignore items):
     - All `.tsx` files from `components/`, `services/`, etc.
     - `styles/globals.css`
     - `App.tsx`
     - `vite.config.ts` (if you have one)
     - Everything needed to build your React app

4. **RUN npm run build**
   - Vite bundles everything into the `dist/` folder
   - Creates optimized HTML, CSS, JS files

5. **COPY --from=builder /app/dist /usr/share/nginx/html**
   - Takes the built `dist/` folder and puts it in nginx

**You don't need to manually collect files** - Docker does it automatically!

---

## Deploy Now - Step by Step

### Step 1: Fix the Dockerfile Issue

Since `Dockerfile` is a directory, you have 2 options:

**Option A: Rename (Recommended)**
```bash
# In your terminal, from the project root:
mv Dockerfile Dockerfile.old
mv Dockerfile.production Dockerfile
mv .dockerignore.production .dockerignore
```

**Option B: Use the production file directly**
```bash
# Build using the production Dockerfile
docker build -f Dockerfile.production -t trading-frontend .
```

---

### Step 2: Build the Docker Image

```bash
# Make sure you're in the project root directory (where package.json is)
cd /path/to/your/frontend/project

# Build the image
docker build -t trading-frontend .

# This will:
# - Collect all your files
# - Install dependencies
# - Build the React app
# - Package it with nginx
```

---

### Step 3: Run the Container

```bash
# Run connected to your existing backend
docker run -d \
  --name trading-frontend \
  -p 3000:80 \
  -e VITE_API_BASE_URL=http://your-backend-host:5000/api \
  -e VITE_WS_URL=ws://your-backend-host:5000/ws \
  trading-frontend

# Access at http://localhost:3000
```

---

### Step 4: Add to Your Existing Docker Setup

If you already have a `docker-compose.yml` for your backend, add this service:

```yaml
services:
  # ... your existing backend, questdb, etc ...

  frontend:
    build:
      context: ./path/to/this/frontend/directory
      dockerfile: Dockerfile.production
    container_name: trading-frontend
    ports:
      - "3000:80"
    environment:
      - VITE_API_BASE_URL=http://backend:5000/api  # Use your backend service name
      - VITE_WS_URL=ws://backend:5000/ws
    networks:
      - your-existing-network  # Use your existing network
    depends_on:
      - backend  # Your backend service name
```

Then restart your stack:
```bash
docker-compose down
docker-compose up -d --build
```

---

## Where Are Your Files?

**Before Docker Build:**
- All source files are in your project directory
- `components/`, `services/`, `styles/`, etc.

**During Docker Build:**
- Docker copies files into the container at `/app`
- Dependencies installed in `/app/node_modules`
- Build output goes to `/app/dist`

**In Final Container:**
- Only the built files are kept
- Located at `/usr/share/nginx/html` in the container
- Original source code is NOT in the final image (smaller, faster)

**On Your Host Machine:**
- Files stay exactly where they are
- Docker just reads them, doesn't move them
- You can keep developing normally

---

## Verify Everything Works

```bash
# 1. Check the image was built
docker images | grep trading-frontend

# 2. Check the container is running
docker ps | grep trading-frontend

# 3. Check the logs
docker logs trading-frontend

# 4. Test the health endpoint
curl http://localhost:3000/health

# 5. Access the app
# Open browser to http://localhost:3000
```

---

## Common Issues

### "Cannot find package.json"
- Make sure you run `docker build` from the directory containing `package.json`

### "Module not found" errors
- Check `.dockerignore` isn't excluding important files
- Verify all imports in your code are correct

### Build fails
- Try building locally first: `npm run build`
- Check that all dependencies are in `package.json`

### Can't connect to backend
- Verify `VITE_API_BASE_URL` is correct
- If backend is on your host machine, use `host.docker.internal` instead of `localhost`
- Check CORS settings in your .NET backend

---

## Environment Variables Explained

**Build Time (baked into the app):**
```bash
VITE_API_BASE_URL=http://api.example.com
```
These are compiled into your JavaScript during build. To change them, you must rebuild the image.

**Runtime (for dynamic config):**
Use nginx proxy approach - requests go to `/api/*` and nginx forwards to backend. No rebuild needed when backend changes.

---

## Next Steps

1. ✅ Fix the Dockerfile directory issue
2. ✅ Build the Docker image
3. ✅ Test it locally
4. ✅ Add to your docker-compose setup
5. ✅ Configure environment variables
6. ✅ Deploy to production

---

## Full Example with Your Existing Backend

Assuming this project structure:
```
your-trading-platform/
├── frontend/          # This React app (all the files you see)
├── backend/           # Your .NET backend
└── docker-compose.yml
```

**In your root `docker-compose.yml`:**
```yaml
version: '3.8'

services:
  questdb:
    image: questdb/questdb:latest
    ports:
      - "9000:9000"
    volumes:
      - questdb-data:/var/lib/questdb

  backend:
    build: ./backend
    ports:
      - "5000:5000"
    environment:
      - ConnectionStrings__QuestDB=http://questdb:9000
    depends_on:
      - questdb

  frontend:
    build: 
      context: ./frontend
      dockerfile: Dockerfile.production
    ports:
      - "3000:80"
    environment:
      - VITE_API_BASE_URL=http://backend:5000/api
      - VITE_WS_URL=ws://backend:5000/ws
    depends_on:
      - backend

volumes:
  questdb-data:
```

**Deploy everything:**
```bash
docker-compose up -d --build
```

That's it! Your entire trading platform will be running.

---

## Questions?

- **Where do my React files go?** They stay in your frontend directory. Docker just reads them.
- **Do I need to copy files manually?** No! Docker's COPY commands handle it automatically.
- **How do I update the app?** Make changes to your code, then `docker-compose up -d --build frontend`
- **Can I develop while Docker is running?** Yes! Docker uses a copy of your files, doesn't lock them.

Check [DOCKER-SETUP.md](./DOCKER-SETUP.md) for advanced topics.
