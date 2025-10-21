// COMPLETED: Root app component with React Query setup
// COMPLETED: Optimized QueryClient configuration for caching
// TODO: Add error boundary for global error handling
// TODO: Implement global loading state
// TODO: Add analytics tracking
// TODO: Implement service worker for PWA support
import '@/styles/globals.css';
import type { AppProps } from 'next/app';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';

function MyApp({ Component, pageProps }: AppProps) {
  // Create QueryClient with optimized configuration
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        // Stale time: data is considered fresh for 5 seconds
        staleTime: 5000,
        // Cache time: unused data remains in cache for 10 minutes
        cacheTime: 10 * 60 * 1000,
        // Don't refetch on window focus (reduces API calls)
        refetchOnWindowFocus: false,
        // Retry failed requests only once
        retry: 1,
        // Retry delay
        retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
        // Keep previous data while refetching
        keepPreviousData: true,
      },
      mutations: {
        // Retry mutations once on failure
        retry: 1,
      },
    },
  }));

  return (
    <QueryClientProvider client={queryClient}>
      <Component {...pageProps} />
    </QueryClientProvider>
  );
}

export default MyApp;
