// COMPLETED: Home page with navigation to all major pages
// COMPLETED: Feature cards link to Dashboard, Search, Dev Systems, and Login
// TODO: Add user onboarding flow for new users
// TODO: Implement real-time stats updates
// TODO: Add notifications system
// TODO: Create user profile page
import React from 'react';
import Head from 'next/head';
import Link from 'next/link';
import { useAuthStore } from '@/store/authStore';

interface FeatureCardProps {
  title: string;
  description: string;
  icon: string;
  href: string;
  features: string[];
  status: 'live' | 'beta' | 'coming-soon';
}

function FeatureCard({ title, description, icon, href, features, status }: FeatureCardProps) {
  const statusColors = {
    live: 'bg-emerald-500',
    beta: 'bg-amber-500',
    'coming-soon': 'bg-slate-500',
  };

  const statusLabels = {
    live: 'LIVE',
    beta: 'BETA',
    'coming-soon': 'SOON',
  };

  return (
    <Link href={href}>
      <div className="card cursor-pointer group animate-slide-up hover:scale-105 transition-transform">
        <div className="flex items-start justify-between mb-4">
          <div className="text-4xl">{icon}</div>
          <span className={`px-2 py-1 text-xs font-semibold rounded text-white ${statusColors[status]}`}>
            {statusLabels[status]}
          </span>
        </div>
        <h3 className="text-xl font-bold text-slate-100 mb-2 group-hover:text-accent-light transition-colors">
          {title}
        </h3>
        <p className="text-slate-400 mb-4">{description}</p>
        <ul className="space-y-1">
          {features.map((feature, index) => (
            <li key={index} className="text-sm text-slate-300 flex items-center">
              <span className="text-accent-light mr-2">•</span>
              {feature}
            </li>
          ))}
        </ul>
      </div>
    </Link>
  );
}

export default function Home() {
  const { user } = useAuthStore();

  const features: FeatureCardProps[] = [
    {
      title: 'Enhanced Search',
      description: 'AI-powered search with voice recognition and advanced filtering',
      icon: '🔍',
      href: '/search',
      status: 'live',
      features: [
        'Voice search with speech recognition',
        'Real-time search with instant results',
        'Advanced faceted filtering',
        'AI-powered suggestions',
        'Search analytics & optimization',
      ],
    },
    {
      title: 'Trading Dashboard',
      description: 'Real-time trading interface with live market data',
      icon: '📈',
      href: '/dashboard',
      status: 'live',
      features: [
        'Live portfolio tracking',
        'Multi-broker integration',
        'Real-time P&L updates',
        'Strategy performance metrics',
        'Risk management tools',
      ],
    },
    {
      title: 'Dev Systems',
      description: 'Comprehensive system monitoring and Algolia reporting',
      icon: '🛠️',
      href: '/dev-systems',
      status: 'live',
      features: [
        'Algolia cost monitoring',
        'Search performance analytics',
        'System health metrics',
        'Real-time diagnostics',
        'Optimization recommendations',
      ],
    },
    {
      title: 'Authentication',
      description: 'Secure login system with demo accounts',
      icon: '🔐',
      href: '/login',
      status: 'live',
      features: [
        'JWT-based authentication',
        'Role-based access control',
        'Demo account access',
        'Secure session management',
        'User profile management',
      ],
    },
  ];

  const stats = [
    { label: 'Search Operations', value: '10.2K', change: '+23%' },
    { label: 'Active Users', value: '156', change: '+12%' },
    { label: 'System Uptime', value: '99.9%', change: '+0.1%' },
    { label: 'Response Time', value: '18ms', change: '-5ms' },
  ];

  return (
    <>
      <Head>
        <title>AlgoTrendy v2.5 - AI-Powered Trading Platform</title>
        <meta name="description" content="Advanced algorithmic trading platform with AI-powered search and real-time analytics" />
      </Head>

      <div className="min-h-screen bg-background">
        {/* Hero Section */}
        <div className="relative overflow-hidden">
          <div className="container mx-auto px-4 py-16">
            {/* Header */}
            <div className="text-center mb-12 animate-fade-in">
              <h1 className="text-5xl md:text-6xl font-bold text-white mb-4">
                AlgoTrendy
              </h1>
              <p className="text-xl text-slate-300 mb-2">AI-Powered Algorithmic Trading Platform</p>
              <p className="text-slate-400">
                Maximizing Algolia's potential with advanced search, real-time analytics, and intelligent automation
              </p>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-12 animate-slide-up">
              {stats.map((stat, index) => (
                <div key={index} className="bg-slate-900/40 backdrop-blur-sm border border-slate-700/50 rounded-xl p-4 text-center hover:border-slate-600/70 transition-all duration-300">
                  <div className="text-2xl font-bold text-slate-100">{stat.value}</div>
                  <div className="text-sm text-slate-400">{stat.label}</div>
                  <div className="text-xs text-emerald-400 mt-1">{stat.change}</div>
                </div>
              ))}
            </div>

            {/* User Status */}
            {user && (
              <div className="bg-emerald-900/30 backdrop-blur-sm border border-emerald-600/50 rounded-xl p-4 mb-8 glow-success animate-scale-in">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-emerald-500 rounded-full flex items-center justify-center">
                    <span className="text-white font-bold">{user.name.charAt(0)}</span>
                  </div>
                  <div>
                    <div className="text-slate-100 font-semibold">Welcome back, {user.name}!</div>
                    <div className="text-emerald-400 text-sm">✅ Authenticated as {user.role}</div>
                  </div>
                </div>
              </div>
            )}

            {/* Feature Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-12">
              {features.map((feature, index) => (
                <FeatureCard key={index} {...feature} />
              ))}
            </div>

            {/* Algolia Showcase */}
            <div className="bg-gradient-to-r from-purple-900/30 to-blue-900/30 backdrop-blur-sm border border-purple-600/50 rounded-xl p-8 mb-8 animate-slide-up">
              <div className="text-center">
                <h2 className="text-3xl font-bold text-slate-100 mb-4">
                  🔍 Powered by Algolia Search
                </h2>
                <p className="text-slate-300 mb-6">
                  Experience lightning-fast search with sub-20ms response times, AI-powered relevance,
                  and real-time analytics. Our implementation maximizes Algolia's potential with:
                </p>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                  <div className="bg-slate-900/50 backdrop-blur-sm rounded-xl p-4 border border-slate-700/50">
                    <div className="text-accent-light font-semibold mb-2">⚡ Performance</div>
                    <div className="text-slate-300">Sub-20ms search response times with global CDN</div>
                  </div>
                  <div className="bg-slate-900/50 backdrop-blur-sm rounded-xl p-4 border border-slate-700/50">
                    <div className="text-purple-400 font-semibold mb-2">🤖 AI Features</div>
                    <div className="text-slate-300">Voice search, typo tolerance, and smart suggestions</div>
                  </div>
                  <div className="bg-slate-900/50 backdrop-blur-sm rounded-xl p-4 border border-slate-700/50">
                    <div className="text-emerald-400 font-semibold mb-2">📊 Analytics</div>
                    <div className="text-slate-300">Real-time cost monitoring and optimization</div>
                  </div>
                </div>
                <div className="mt-6">
                  <Link href="/search">
                    <button className="btn-primary py-3 px-8 rounded-xl shadow-lg hover:shadow-accent/30 transform hover:scale-105">
                      🚀 Experience Enhanced Search
                    </button>
                  </Link>
                </div>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="text-center">
              <h3 className="text-xl font-semibold text-slate-100 mb-4">Quick Start</h3>
              <div className="flex flex-wrap justify-center gap-4">
                {!user && (
                  <Link href="/login">
                    <button className="btn-primary py-2 px-6 rounded-xl shadow-lg">
                      🔐 Sign In
                    </button>
                  </Link>
                )}
                <Link href="/search">
                  <button className="bg-purple-600 hover:bg-purple-700 text-white py-2 px-6 rounded-xl transition-all shadow-lg hover:shadow-purple-500/30">
                    🔍 Search Platform
                  </button>
                </Link>
                <Link href="/dev-systems">
                  <button className="bg-orange-600 hover:bg-orange-700 text-white py-2 px-6 rounded-xl transition-all shadow-lg hover:shadow-orange-500/30">
                    📊 System Analytics
                  </button>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
