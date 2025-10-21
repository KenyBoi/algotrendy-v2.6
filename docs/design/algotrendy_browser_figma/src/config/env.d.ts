/**
 * Runtime Environment Configuration
 * These values are injected at container startup, not build time
 */

export interface RuntimeConfig {
  API_BASE_URL: string;
  WS_BASE_URL: string;
  ENVIRONMENT: 'development' | 'staging' | 'production';
  VERSION: string;
  ENABLE_DEBUG: boolean;
}

declare global {
  interface Window {
    ENV: RuntimeConfig;
  }
}

export {};
