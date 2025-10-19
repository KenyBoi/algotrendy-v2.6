'use client';

// COMPLETED: Header component with user profile and logout
// COMPLETED: Menu toggle for sidebar
// COMPLETED: User profile dropdown with settings link
// TODO: Implement notifications system for Bell icon
// TODO: Add theme toggle (dark/light mode)
// TODO: Implement search bar in header
// TODO: Add breadcrumb navigation
import React, { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { useAuthStore } from '@/store/authStore';
import { Menu, LogOut, Bell, Settings, User, ChevronDown } from 'lucide-react';
import { WebSocketStatus } from '@/components/ui/WebSocketStatus';

interface HeaderProps {
  onMenuClick: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onMenuClick }) => {
  const { user, logout } = useAuthStore();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const handleLogout = () => {
    logout();
    window.location.href = '/login';
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <header className="bg-gradient-primary text-white shadow-soft-lg border-b border-white/10">
      <div className="flex items-center justify-between px-6 py-4">
        <div className="flex items-center gap-4">
          <button
            onClick={onMenuClick}
            className="p-2 hover:bg-white/10 rounded-xl transition-all duration-300 hover:scale-105"
            aria-label="Toggle menu"
          >
            <Menu size={24} />
          </button>
          <h1 className="text-2xl font-bold tracking-tight">
            <span className="bg-gradient-to-r from-white to-blue-200 bg-clip-text text-transparent">
              AlgoTrendy
            </span>
          </h1>
        </div>

        <div className="flex items-center gap-4">
          {/* WebSocket Status Indicator */}
          <WebSocketStatus showLabel className="hidden md:flex" />

          <button className="p-2 hover:bg-white/10 rounded-xl transition-all duration-300 hover:scale-105 relative">
            <Bell size={20} />
            <span className="absolute top-1 right-1 w-2 h-2 bg-accent-light rounded-full animate-pulse"></span>
          </button>

          {/* User Profile Dropdown */}
          <div className="relative" ref={dropdownRef}>
            <button
              onClick={() => setIsDropdownOpen(!isDropdownOpen)}
              className="flex items-center gap-3 p-2 hover:bg-secondary rounded-lg transition-colors"
            >
              {user?.avatar ? (
                <img
                  src={user.avatar}
                  alt={user.name}
                  className="w-8 h-8 rounded-full"
                />
              ) : (
                <div className="w-8 h-8 rounded-full bg-accent flex items-center justify-center">
                  <span className="text-white font-semibold text-sm">
                    {user?.name?.charAt(0).toUpperCase() || 'U'}
                  </span>
                </div>
              )}
              <div className="text-sm text-left hidden md:block">
                <p className="font-semibold">{user?.name || 'User'}</p>
                <p className="text-neutral text-xs">{user?.role || 'Guest'}</p>
              </div>
              <ChevronDown
                size={16}
                className={`transition-transform ${isDropdownOpen ? 'rotate-180' : ''}`}
              />
            </button>

            {/* Dropdown Menu */}
            {isDropdownOpen && (
              <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50">
                {/* User Info */}
                <div className="px-4 py-3 border-b border-gray-200">
                  <p className="font-semibold text-primary">{user?.name || 'User'}</p>
                  <p className="text-sm text-neutral">{user?.email || 'user@example.com'}</p>
                </div>

                {/* Menu Items */}
                <Link
                  href="/settings"
                  className="flex items-center gap-3 px-4 py-2 hover:bg-gray-100 transition-colors"
                  onClick={() => setIsDropdownOpen(false)}
                >
                  <Settings size={18} className="text-neutral" />
                  <span className="text-sm">Settings</span>
                </Link>

                <Link
                  href="/profile"
                  className="flex items-center gap-3 px-4 py-2 hover:bg-gray-100 transition-colors"
                  onClick={() => setIsDropdownOpen(false)}
                >
                  <User size={18} className="text-neutral" />
                  <span className="text-sm">My Profile</span>
                </Link>

                <div className="border-t border-gray-200 my-2"></div>

                <button
                  onClick={() => {
                    setIsDropdownOpen(false);
                    handleLogout();
                  }}
                  className="flex items-center gap-3 px-4 py-2 hover:bg-gray-100 transition-colors w-full text-left text-red-600"
                >
                  <LogOut size={18} />
                  <span className="text-sm font-medium">Log Out</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
};
