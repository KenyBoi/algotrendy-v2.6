/**
 * Next.js Configuration - Optimized Build Settings
 *
 * COMPLETED: SWC minification for faster builds
 * COMPLETED: Advanced webpack code splitting
 * COMPLETED: Image optimization with AVIF/WebP support
 * COMPLETED: Aggressive caching headers for static assets
 * COMPLETED: Console.log removal in production
 * COMPLETED: Package import optimization
 * TODO: Implement bundle analyzer in CI/CD
 * TODO: Add PWA support with service worker
 * TODO: Configure ISR (Incremental Static Regeneration) for dynamic pages
 * TODO: Add Sentry for error tracking
 * OPTIMIZE: Enable React Server Components (Next.js 13+ App Router)
 * OPTIMIZE: Implement edge functions for API routes
 *
 * @type {import('next').NextConfig}
 */
const nextConfig = {
  reactStrictMode: true,
  swcMinify: true,
  compress: true,
  poweredByHeader: false,
  productionBrowserSourceMaps: false,

  // Experimental optimizations
  experimental: {
    // Optimize CSS - Disabled due to critters compatibility issues
    // optimizeCss: true,
    // Optimize package imports to reduce bundle size
    optimizePackageImports: ['lucide-react', 'axios', '@tanstack/react-query'],
  },

  // Image optimization
  images: {
    domains: ['localhost'],
    formats: ['image/avif', 'image/webp'],
    minimumCacheTTL: 60,
    deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048, 3840],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
  },

  // Compiler options
  compiler: {
    // Remove console.log in production
    removeConsole: process.env.NODE_ENV === 'production' ? {
      exclude: ['error', 'warn'],
    } : false,
  },

  // Webpack optimization
  webpack: (config, { dev, isServer }) => {
    // Production optimizations
    if (!dev && !isServer) {
      config.optimization = {
        ...config.optimization,
        moduleIds: 'deterministic',
        runtimeChunk: 'single',
        splitChunks: {
          chunks: 'all',
          cacheGroups: {
            default: false,
            vendors: false,
            // Vendor chunk for react/react-dom
            framework: {
              name: 'framework',
              chunks: 'all',
              test: /(?<!node_modules.*)[\\/]node_modules[\\/](react|react-dom|scheduler|prop-types)[\\/]/,
              priority: 40,
              enforce: true,
            },
            // Commons chunk for shared code
            commons: {
              name: 'commons',
              minChunks: 2,
              priority: 20,
            },
            // Lib chunk for large libraries
            lib: {
              test: /[\\/]node_modules[\\/]/,
              name(module) {
                const packageName = module.context.match(
                  /[\\/]node_modules[\\/](.*?)([\\/]|$)/
                )?.[1];
                return `npm.${packageName?.replace('@', '')}`;
              },
              priority: 30,
              minChunks: 1,
              reuseExistingChunk: true,
            },
          },
        },
      };
    }

    return config;
  },

  // Headers for better caching
  async headers() {
    return [
      {
        source: '/:all*(svg|jpg|jpeg|png|gif|ico|webp|avif)',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
      {
        source: '/_next/static/:path*',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
    ];
  },
};

module.exports = nextConfig;
