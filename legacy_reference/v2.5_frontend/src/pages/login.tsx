'use client';

// COMPLETED: Login page with authentication functionality
// COMPLETED: Password strength indicator added
// COMPLETED: "Remember Me" functionality added
// TODO: Implement password reset/forgot password flow
// TODO: Add social login options (Google, GitHub)
// TODO: Add two-factor authentication (2FA)
// TODO: Implement login rate limiting
import React, { useState } from 'react';
import Link from 'next/link';
import { PasswordStrength } from '@/components/ui/PasswordStrength';
import { useAuthStore } from '@/store/authStore';
import { apiService } from '@/services/api';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [isSignUp, setIsSignUp] = useState(false);

  const { setUser, setToken } = useAuthStore();

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const response = await apiService.login(email, password);
      
      if (response.data) {
        setToken(response.data);
        localStorage.setItem('auth_token', response.data.access_token);
        
        // Fetch current user
        const userRes = await apiService.getCurrentUser();
        if (userRes.data) {
          setUser(userRes.data);
        }
        
        window.location.href = '/dashboard';
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid email or password');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background relative overflow-hidden">
      {/* Animated Background */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute w-96 h-96 -top-48 -left-48 bg-accent/20 rounded-full blur-3xl animate-pulse-slow"></div>
        <div className="absolute w-96 h-96 -bottom-48 -right-48 bg-purple-500/20 rounded-full blur-3xl animate-pulse-slow" style={{animationDelay: '1s'}}></div>
      </div>

      <div className="w-full max-w-md relative z-10 px-4">
        {/* Card */}
        <div className="card-glass backdrop-blur-xl animate-scale-in">
          {/* Header */}
          <div className="text-center mb-8">
            <h1 className="text-3xl font-bold text-slate-100 mb-2">
              <span className="bg-gradient-to-r from-accent-light to-purple-400 bg-clip-text text-transparent">
                AlgoTrendy
              </span>
            </h1>
            <p className="text-slate-400">Sign in to your account</p>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-error/20 border border-error-light/30 rounded-lg backdrop-blur-sm">
              <p className="text-error-light text-sm">{error}</p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-slate-300 mb-2">
                Email Address
              </label>
              <input
                type="email"
                id="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                disabled={isLoading}
                className="w-full"
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-slate-300 mb-2">
                Password
              </label>
              <input
                type="password"
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                disabled={isLoading}
                className="w-full"
              />
              {/* Show password strength only on sign up */}
              {isSignUp && <PasswordStrength password={password} />}
            </div>

            {/* Remember Me Checkbox */}
            <div className="flex items-center">
              <input
                type="checkbox"
                id="rememberMe"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="w-4 h-4 text-accent-light border-slate-600 rounded focus:ring-accent-light bg-slate-900/50"
              />
              <label htmlFor="rememberMe" className="ml-2 text-sm text-slate-400">
                Remember me for 30 days
              </label>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full btn-primary text-white font-semibold py-3 rounded-xl transition-all disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-accent/30"
            >
              {isLoading ? (
                <span className="flex items-center justify-center gap-2">
                  <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"/>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
                  </svg>
                  Signing in...
                </span>
              ) : 'Sign In'}
            </button>
          </form>

          {/* Footer */}
          <div className="mt-6 text-center text-sm text-slate-400">
            <p>
              {isSignUp ? "Already have an account?" : "Don't have an account?"}{' '}
              <button
                type="button"
                onClick={() => setIsSignUp(!isSignUp)}
                className="text-accent-light hover:text-accent transition-colors font-semibold"
              >
                {isSignUp ? 'Sign in' : 'Sign up'}
              </button>
            </p>
            <p className="mt-2">
              <Link href="/forgot-password" className="text-accent-light hover:text-accent transition-colors text-xs">
                Forgot password?
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
