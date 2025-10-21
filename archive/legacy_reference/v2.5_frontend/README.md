# AlgoTrendy Web Application

Modern algorithmic trading platform web application built with Next.js 14, React, TypeScript, and Tailwind CSS.

## Project Structure

```
algotrendy-web/
├── src/
│   ├── components/
│   │   ├── layout/          # Layout components (Header, Sidebar)
│   │   ├── dashboard/       # Dashboard components
│   │   ├── forms/           # Form components
│   │   └── common/          # Reusable components
│   ├── pages/               # Next.js pages
│   ├── store/               # Zustand state management
│   ├── services/            # API service client
│   ├── types/               # TypeScript type definitions
│   ├── hooks/               # Custom React hooks
│   ├── utils/               # Utility functions
│   ├── lib/                 # Library functions
│   └── styles/              # Global styles
├── public/                  # Static assets
├── .env.example             # Environment variables template
├── next.config.js           # Next.js configuration
├── tailwind.config.ts       # Tailwind CSS configuration
├── tsconfig.json            # TypeScript configuration
└── package.json             # Project dependencies
```

## Features

### Authentication
- Login/Register pages
- JWT token management
- Protected dashboard routes
- Automatic token refresh

### Trading Dashboard
- Portfolio overview with total value, cash, P&L
- Real-time position tracking
- Position management (open/close)
- Responsive design for mobile/tablet

### State Management
- Zustand for lightweight state management
- Separate stores for auth and trading
- React Query for server state

### UI/UX
- Tailwind CSS for styling
- Lucide React icons
- Mobile-responsive design
- Light/dark mode ready

## Getting Started

### Prerequisites
- Node.js 18+
- npm or yarn

### Installation

1. Clone the repository and navigate to the project:
```bash
cd algotrendy-web
```

2. Install dependencies:
```bash
npm install
```

3. Create environment file:
```bash
cp .env.example .env.local
```

4. Update `NEXT_PUBLIC_API_URL` in `.env.local` if needed:
```
NEXT_PUBLIC_API_URL=http://localhost:8000/api
```

### Development

Start the development server:
```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

### Build for Production

```bash
npm run build
npm start
```

### Type Checking

Run TypeScript type checking:
```bash
npm run type-check
```

## API Integration

The application connects to a FastAPI backend at `http://localhost:8000/api`. The backend should provide endpoints for authentication, portfolio management, strategies, and trading operations.

## Technology Stack

- **Frontend Framework**: Next.js 14
- **UI Library**: React 18
- **Language**: TypeScript
- **Styling**: Tailwind CSS + PostCSS
- **State Management**: Zustand
- **Server State**: React Query
- **HTTP Client**: Axios
- **Icons**: Lucide React
- **Form Handling**: React Hook Form
- **Validation**: Zod
- **Authentication**: NextAuth.js

## Deployment

### Vercel (Recommended)

1. Push code to GitHub
2. Connect repository to Vercel
3. Set environment variables
4. Deploy

### VPS/Self-hosted

```bash
npm run build
npm start
```

## License

MIT
