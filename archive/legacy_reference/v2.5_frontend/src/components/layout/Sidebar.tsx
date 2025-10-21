'use client';

import React from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { BarChart3, Search, Settings, Home, Wrench, X, FlaskConical } from 'lucide-react';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

// COMPLETED: Updated sidebar navigation to include all existing pages
// COMPLETED: Added Home, Dashboard, Search, Dev Systems, and Settings links
// COMPLETED: Settings page created with full functionality
// COMPLETED: Backtesting Module page created with full AI integration
// TODO: Add /portfolio page for portfolio management
// TODO: Add /strategies page for strategy configuration
// TODO: Add /positions page for detailed position management
// TODO: Add /reports page for trading reports and analytics
const menuItems = [
  { href: '/', label: 'Home', icon: Home },
  { href: '/dashboard', label: 'Dashboard', icon: BarChart3 },
  { href: '/backtesting', label: 'Backtesting', icon: FlaskConical }, // ✅ Page created
  { href: '/search', label: 'Search', icon: Search },
  { href: '/dev-systems', label: 'Dev Systems', icon: Wrench },
  { href: '/settings', label: 'Settings', icon: Settings }, // ✅ Page created
];

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onClose }) => {
  const pathname = usePathname();

  return (
    <>
      {/* Mobile Overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 lg:hidden z-40"
          onClick={onClose}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`fixed left-0 top-16 h-[calc(100vh-64px)] w-64 bg-secondary text-white transition-transform duration-300 z-50 lg:z-auto lg:translate-x-0 ${
          isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        }`}
      >
        <div className="flex items-center justify-between p-4 lg:hidden">
          <h2 className="text-lg font-semibold">Menu</h2>
          <button
            onClick={onClose}
            className="p-2 hover:bg-primary rounded-lg"
            aria-label="Close menu"
          >
            <X size={20} />
          </button>
        </div>

        <nav className="space-y-2 p-4">
          {menuItems.map(({ href, label, icon: Icon }) => {
            const isActive = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                  isActive
                    ? 'bg-accent text-white'
                    : 'text-neutral hover:bg-primary'
                }`}
                onClick={onClose}
              >
                <Icon size={20} />
                <span className="font-medium">{label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>
    </>
  );
};
