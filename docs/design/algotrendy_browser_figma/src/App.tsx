/**
 * AlgoTrendy Main Application
 * React + TypeScript Trading Platform
 */

import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import Dashboard from './pages/Dashboard';
import Orders from './pages/Orders';
import Positions from './pages/Positions';
import Strategies from './pages/Strategies';
import Login from './pages/Login';
import NotFound from './pages/NotFound';
import Layout from './components/Layout';
import { signalRClient } from './lib/signalr-client';

function App() {
  useEffect(() => {
    // Connect to SignalR on app mount
    const connectSignalR = async () => {
      try {
        await signalRClient.connect();
        console.log('SignalR connected successfully');
      } catch (error) {
        console.error('Failed to connect SignalR:', error);
      }
    };

    connectSignalR();

    // Cleanup on unmount
    return () => {
      signalRClient.disconnect();
    };
  }, []);

  return (
    <Router>
      <Routes>
        {/* Public routes */}
        <Route path="/login" element={<Login />} />

        {/* Protected routes with layout */}
        <Route path="/" element={<Layout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="orders" element={<Orders />} />
          <Route path="positions" element={<Positions />} />
          <Route path="strategies" element={<Strategies />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;
