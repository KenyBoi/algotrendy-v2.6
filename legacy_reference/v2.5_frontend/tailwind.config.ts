import type { Config } from 'tailwindcss'

const config: Config = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        // Professional Corporate Palette
        primary: {
          DEFAULT: '#0F172A', // Slate 900 - Deep navy for headers
          light: '#1E293B',   // Slate 800
          dark: '#020617',    // Slate 950
        },
        secondary: {
          DEFAULT: '#334155', // Slate 700 - Secondary text
          light: '#475569',   // Slate 600
          dark: '#1E293B',    // Slate 800
        },
        accent: {
          DEFAULT: '#2563EB', // Blue 600 - Professional blue
          light: '#3B82F6',   // Blue 500
          dark: '#1D4ED8',    // Blue 700
        },
        success: {
          DEFAULT: '#059669', // Emerald 600 - Profit green
          light: '#10B981',   // Emerald 500
          dark: '#047857',    // Emerald 700
        },
        warning: {
          DEFAULT: '#D97706', // Amber 600
          light: '#F59E0B',   // Amber 500
          dark: '#B45309',    // Amber 700
        },
        error: {
          DEFAULT: '#DC2626', // Red 600 - Loss red
          light: '#EF4444',   // Red 500
          dark: '#B91C1C',    // Red 700
        },
        neutral: {
          DEFAULT: '#64748B', // Slate 500
          light: '#94A3B8',   // Slate 400
          dark: '#475569',    // Slate 600
        },
        background: {
          DEFAULT: '#040323', // Deep space blue - Main background
          secondary: '#0A0A2E', // Slightly lighter
          tertiary: '#16213E', // Card background
        },
      },
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
        'gradient-primary': 'linear-gradient(135deg, #1E293B 0%, #0F172A 100%)',
        'gradient-accent': 'linear-gradient(135deg, #3B82F6 0%, #2563EB 100%)',
        'gradient-success': 'linear-gradient(135deg, #10B981 0%, #059669 100%)',
      },
      boxShadow: {
        'soft': '0 2px 8px rgba(15, 23, 42, 0.08)',
        'soft-lg': '0 4px 16px rgba(15, 23, 42, 0.12)',
        'soft-xl': '0 8px 32px rgba(15, 23, 42, 0.16)',
        'inner-soft': 'inset 0 2px 4px rgba(15, 23, 42, 0.06)',
      },
      animation: {
        'fade-in': 'fadeIn 0.3s ease-in-out',
        'slide-up': 'slideUp 0.3s ease-in-out',
        'slide-down': 'slideDown 0.3s ease-in-out',
        'scale-in': 'scaleIn 0.2s ease-in-out',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { transform: 'translateY(10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        slideDown: {
          '0%': { transform: 'translateY(-10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        scaleIn: {
          '0%': { transform: 'scale(0.95)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
      },
      transitionDuration: {
        '400': '400ms',
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}
export default config
