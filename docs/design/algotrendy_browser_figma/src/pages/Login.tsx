/**
 * Login Page
 * User authentication
 */

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { TrendingUp } from 'lucide-react';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      // TODO: Implement actual authentication
      // For now, accept any credentials
      if (username && password) {
        // Store mock token
        localStorage.setItem('authToken', 'mock-jwt-token');
        navigate('/dashboard');
      } else {
        setError('Please enter username and password');
      }
    } catch (err) {
      setError('Login failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="flex items-center justify-center mb-4">
            <TrendingUp className="w-12 h-12 text-blue-500" />
          </div>
          <h1 className="text-4xl font-bold text-white mb-2">AlgoTrendy</h1>
          <p className="text-gray-400">Professional Trading Platform v2.6</p>
        </div>

        <Card className="bg-slate-900 border-slate-800">
          <CardHeader>
            <CardTitle className="text-white text-xl">Welcome Back</CardTitle>
            <CardDescription>Sign in to your trading account</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleLogin} className="space-y-4">
              <div>
                <Label htmlFor="username" className="text-gray-300">Username</Label>
                <Input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="bg-slate-800 border-slate-700 text-white"
                  placeholder="Enter your username"
                  required
                />
              </div>
              <div>
                <Label htmlFor="password" className="text-gray-300">Password</Label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="bg-slate-800 border-slate-700 text-white"
                  placeholder="Enter your password"
                  required
                />
              </div>

              {error && (
                <div className="text-sm text-red-500 bg-red-500/10 border border-red-500/20 rounded p-3">
                  {error}
                </div>
              )}

              <Button
                type="submit"
                disabled={loading}
                className="w-full bg-blue-600 hover:bg-blue-700"
              >
                {loading ? 'Signing in...' : 'Sign In'}
              </Button>

              <p className="text-xs text-gray-500 text-center mt-4">
                Demo mode: Enter any username and password
              </p>
            </form>
          </CardContent>
        </Card>

        <p className="text-center text-gray-500 text-sm mt-6">
          © 2025 AlgoTrendy. All rights reserved.
        </p>
      </div>
    </div>
  );
}
