# How to Launch AlgoTrendy Web Application

## Quick Start (3 Steps)

### Step 1: Navigate to the project
```bash
cd /root/algotrendy_v2.5/algotrendy-web
```

### Step 2: Install dependencies (first time only)
```bash
npm install
```

### Step 3: Start the development server
```bash
npm run dev
```

**Output will show**: `ready - started server on http://localhost:3000`

### Step 4: Open in Browser
Visit: **http://localhost:3000**

You should see the login page with the AlgoTrendy interface.

---

## Alternative: Production Build

If you want to run a production-optimized version:

```bash
cd /root/algotrendy_v2.5/algotrendy-web
npm install        # if not done yet
npm run build      # creates optimized build
npm start          # starts production server
```

Then visit: **http://localhost:3000**

---

## What You Should See

1. **Login Page** (first visit)
   - Email/Password form
   - AlgoTrendy branding
   - "Sign up" link

2. **Dashboard** (after authentication)
   - Header with navigation and user menu
   - Sidebar with 6 main sections:
     - Dashboard
     - Strategies
     - Positions
     - Portfolio
     - Reports
     - Settings
   - Portfolio overview card
   - Positions table

---

## Troubleshooting

### Port 3000 already in use?
```bash
npm run dev -- -p 3001
# Then visit http://localhost:3001
```

### Dependencies not installing?
```bash
rm -rf node_modules package-lock.json
npm install
```

### Build errors?
```bash
npm run type-check    # Check TypeScript
npm run build         # Try building
```

---

## Backend API (Optional)

To also run the backend API server in another terminal:

```bash
cd /root/algotrendy_v2.5/algotrendy-api
source venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

API Docs available at: **http://localhost:8000/docs**

---

## Full System Startup Script

Create a script to start both frontend and backend:

```bash
#!/bin/bash

# Terminal 1: Frontend
cd /root/algotrendy_v2.5/algotrendy-web
npm run dev &

# Terminal 2: Backend
cd /root/algotrendy_v2.5/algotrendy-api
source venv/bin/activate
uvicorn app.main:app --reload --port 8000

echo "Frontend: http://localhost:3000"
echo "Backend: http://localhost:8000"
echo "API Docs: http://localhost:8000/docs"
```

---

## Project Structure Reference

```
algotrendy-web/
├── src/
│   ├── pages/              (Next.js pages)
│   │   ├── index.tsx       (Home - redirects to login/dashboard)
│   │   ├── login.tsx       (Login page)
│   │   └── dashboard.tsx   (Main dashboard)
│   ├── components/         (React components)
│   │   ├── layout/         (Header, Sidebar)
│   │   └── dashboard/      (Portfolio, Positions)
│   ├── store/              (Zustand state)
│   ├── services/           (API client)
│   └── styles/             (Tailwind CSS)
├── package.json            (Dependencies)
├── tsconfig.json           (TypeScript config)
├── tailwind.config.ts      (Tailwind config)
├── next.config.js          (Next.js config)
└── public/                 (Static files)
```

---

## Common Commands

| Command | Purpose |
|---------|---------|
| `npm run dev` | Start development server (hot reload) |
| `npm run build` | Create production build |
| `npm start` | Run production server |
| `npm run type-check` | Check TypeScript errors |
| `npm run lint` | Run ESLint |

---

## Expected Output

When you run `npm run dev`, you should see:

```
> algotrendy-web@1.0.0 dev
> next dev

  ▲ Next.js 14.0.0
  - Local:        http://localhost:3000
  - Environments: .env.local

✓ Ready in 2.3s
✓ Compiled successfully
```

Then open **http://localhost:3000** in your browser and you should see the login page!

---

## Environment Variables

Create `.env.local` in the project root if needed:

```
NEXT_PUBLIC_API_URL=http://localhost:8000/api
NEXT_PUBLIC_APP_NAME=AlgoTrendy
NEXT_PUBLIC_APP_VERSION=1.0.0
```

(Already has example in `.env.example`)

---

## Support

If the application doesn't load:
1. Check that you're in the `/root/algotrendy_v2.5/algotrendy-web` directory
2. Verify Node.js is installed: `node --version`
3. Try clearing build cache: `rm -rf .next`
4. Reinstall dependencies: `rm -rf node_modules && npm install`

You should now have a fully functional trading dashboard interface running in your browser!
