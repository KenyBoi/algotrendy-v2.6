// COMPLETED: Added Header and Sidebar navigation to search page
// COMPLETED: Integrated navigation components for site-wide navigation
// TODO: Add search history feature
// TODO: Add saved searches functionality
// TODO: Implement search filters persistence
import React, { useState } from 'react';
import { NextPage } from 'next';
import Head from 'next/head';
import dynamic from 'next/dynamic';
import { Header } from '@/components/layout/Header';
import { Sidebar } from '@/components/layout/Sidebar';

// Dynamic import to avoid SSR issues with Algolia
const EnhancedAlgoliaSearch = dynamic(
    () => import('@/components/search/EnhancedAlgoliaSearch'),
    {
        ssr: false,
        loading: () => (
            <div className="min-h-screen bg-gradient-to-br from-gray-900 via-blue-900 to-gray-900 flex items-center justify-center">
                <div className="text-white text-center">
                    <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-cyan-500 mx-auto mb-4"></div>
                    <p className="text-lg">Loading AI-Powered Search...</p>
                    <p className="text-sm text-gray-400 mt-2">Initializing Algolia Intelligence</p>
                </div>
            </div>
        )
    }
);

const SearchPage: NextPage = () => {
    const [sidebarOpen, setSidebarOpen] = useState(false);

    return (
        <>
            <Head>
                <title>AI Search - AlgoTrendy | Advanced Trading Intelligence</title>
                <meta
                    name="description"
                    content="AI-powered search across algorithmic trading data, strategies, and market insights. Find profitable trades, analyze performance, and discover trading opportunities with advanced Algolia search."
                />
                <meta name="keywords" content="algorithmic trading, AI search, trading strategies, market analysis, Algolia, trading intelligence" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />

                {/* Open Graph */}
                <meta property="og:title" content="AI Search - AlgoTrendy Trading Intelligence" />
                <meta property="og:description" content="Advanced AI-powered search for algorithmic trading data and strategies" />
                <meta property="og:type" content="website" />
                <meta property="og:image" content="/og-search.png" />

                {/* Twitter Card */}
                <meta name="twitter:card" content="summary_large_image" />
                <meta name="twitter:title" content="AI Search - AlgoTrendy" />
                <meta name="twitter:description" content="Advanced AI-powered search for trading intelligence" />

                {/* Favicon */}
                <link rel="icon" href="/favicon.ico" />
                <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png" />

                {/* Preconnect to Algolia for performance */}
                <link rel="preconnect" href="https://latency-dsn.algolia.net" crossOrigin="anonymous" />
                <link rel="dns-prefetch" href="https://latency-dsn.algolia.net" />
            </Head>

            <div className="flex flex-col h-screen">
                <Header onMenuClick={() => setSidebarOpen(!sidebarOpen)} />
                <div className="flex flex-1 overflow-hidden">
                    <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />
                    <main className="flex-1 overflow-auto">
                        <EnhancedAlgoliaSearch />
                    </main>
                </div>
            </div>
        </>
    );
};

export default SearchPage;