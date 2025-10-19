'use client';

// COMPLETED: Settings page created with basic structure
// COMPLETED: User preferences section
// COMPLETED: Account settings section
// COMPLETED: API key management section
// COMPLETED: Notification preferences section
// TODO: Add actual API integration for saving settings
// TODO: Implement theme customization options
// TODO: Add export/import settings functionality
// TODO: Add timezone selection
// TODO: Implement email notification settings
import React, { useState } from 'react';
import Head from 'next/head';
import { Header } from '@/components/layout/Header';
import { Sidebar } from '@/components/layout/Sidebar';
import { ToastContainer } from '@/components/ui/ToastContainer';
import { useToast } from '@/hooks/useToast';
import { useAuthStore } from '@/store/authStore';
import { Settings as SettingsIcon, User, Key, Bell, Palette, Shield, Save } from 'lucide-react';

interface SettingSection {
  id: string;
  title: string;
  icon: React.ReactNode;
}

export default function SettingsPage() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [activeSection, setActiveSection] = useState('account');
  const [saving, setSaving] = useState(false);
  const { user } = useAuthStore();
  const { toasts, removeToast, success, error } = useToast();

  // Form state
  const [settings, setSettings] = useState({
    // Account settings
    name: user?.name || '',
    email: user?.email || '',
    avatar: user?.avatar || '',

    // API Keys
    algoliaApiKey: '',
    freqtradeApiKey: '',

    // Notifications
    emailNotifications: true,
    tradeAlerts: true,
    profitAlerts: true,
    lossAlerts: false,

    // Preferences
    theme: 'dark',
    currency: 'USD',
    timezone: 'UTC',
  });

  const sections: SettingSection[] = [
    { id: 'account', title: 'Account', icon: <User size={20} /> },
    { id: 'api', title: 'API Keys', icon: <Key size={20} /> },
    { id: 'notifications', title: 'Notifications', icon: <Bell size={20} /> },
    { id: 'preferences', title: 'Preferences', icon: <Palette size={20} /> },
    { id: 'security', title: 'Security', icon: <Shield size={20} /> },
  ];

  const handleSave = async () => {
    setSaving(true);
    try {
      // TODO: Implement actual API call to save settings
      await new Promise(resolve => setTimeout(resolve, 1000));
      success('Settings saved successfully!');
    } catch (err) {
      error('Failed to save settings. Please try again.');
    } finally {
      setSaving(false);
    }
  };

  const renderAccountSection = () => (
    <div className="space-y-6">
      <div>
        <label className="block text-sm font-medium text-primary mb-2">Name</label>
        <input
          type="text"
          value={settings.name}
          onChange={(e) => setSettings({ ...settings, name: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Email</label>
        <input
          type="email"
          value={settings.email}
          onChange={(e) => setSettings({ ...settings, email: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Avatar URL</label>
        <input
          type="url"
          value={settings.avatar}
          onChange={(e) => setSettings({ ...settings, avatar: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
          placeholder="https://example.com/avatar.jpg"
        />
      </div>

      <div className="pt-4">
        <button className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors">
          Delete Account
        </button>
        <p className="text-xs text-neutral mt-2">This action cannot be undone.</p>
      </div>
    </div>
  );

  const renderApiSection = () => (
    <div className="space-y-6">
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-800">
          <strong>Note:</strong> Keep your API keys secure. Never share them publicly.
        </p>
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Algolia API Key</label>
        <input
          type="password"
          value={settings.algoliaApiKey}
          onChange={(e) => setSettings({ ...settings, algoliaApiKey: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
          placeholder="Enter your Algolia API key"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Freqtrade API Key</label>
        <input
          type="password"
          value={settings.freqtradeApiKey}
          onChange={(e) => setSettings({ ...settings, freqtradeApiKey: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
          placeholder="Enter your Freqtrade API key"
        />
      </div>

      <div className="flex gap-2">
        <button className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
          Test Connection
        </button>
        <button className="px-4 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 transition-colors">
          Generate New Key
        </button>
      </div>
    </div>
  );

  const renderNotificationsSection = () => (
    <div className="space-y-6">
      <div className="space-y-4">
        <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
          <div>
            <h4 className="font-medium text-primary">Email Notifications</h4>
            <p className="text-sm text-neutral">Receive updates via email</p>
          </div>
          <label className="relative inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={settings.emailNotifications}
              onChange={(e) => setSettings({ ...settings, emailNotifications: e.target.checked })}
              className="sr-only peer"
            />
            <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
        </div>

        <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
          <div>
            <h4 className="font-medium text-primary">Trade Alerts</h4>
            <p className="text-sm text-neutral">Get notified when trades execute</p>
          </div>
          <label className="relative inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={settings.tradeAlerts}
              onChange={(e) => setSettings({ ...settings, tradeAlerts: e.target.checked })}
              className="sr-only peer"
            />
            <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
        </div>

        <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
          <div>
            <h4 className="font-medium text-primary">Profit Alerts</h4>
            <p className="text-sm text-neutral">Notify when positions are profitable</p>
          </div>
          <label className="relative inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={settings.profitAlerts}
              onChange={(e) => setSettings({ ...settings, profitAlerts: e.target.checked })}
              className="sr-only peer"
            />
            <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
        </div>

        <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
          <div>
            <h4 className="font-medium text-primary">Loss Alerts</h4>
            <p className="text-sm text-neutral">Notify when positions hit stop loss</p>
          </div>
          <label className="relative inline-flex items-center cursor-pointer">
            <input
              type="checkbox"
              checked={settings.lossAlerts}
              onChange={(e) => setSettings({ ...settings, lossAlerts: e.target.checked })}
              className="sr-only peer"
            />
            <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
          </label>
        </div>
      </div>
    </div>
  );

  const renderPreferencesSection = () => (
    <div className="space-y-6">
      <div>
        <label className="block text-sm font-medium text-primary mb-2">Theme</label>
        <select
          value={settings.theme}
          onChange={(e) => setSettings({ ...settings, theme: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
        >
          <option value="light">Light</option>
          <option value="dark">Dark</option>
          <option value="auto">Auto</option>
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Currency</label>
        <select
          value={settings.currency}
          onChange={(e) => setSettings({ ...settings, currency: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
        >
          <option value="USD">USD ($)</option>
          <option value="EUR">EUR (€)</option>
          <option value="GBP">GBP (£)</option>
          <option value="BTC">BTC (₿)</option>
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium text-primary mb-2">Timezone</label>
        <select
          value={settings.timezone}
          onChange={(e) => setSettings({ ...settings, timezone: e.target.value })}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
        >
          <option value="UTC">UTC</option>
          <option value="America/New_York">Eastern Time (ET)</option>
          <option value="America/Chicago">Central Time (CT)</option>
          <option value="America/Los_Angeles">Pacific Time (PT)</option>
          <option value="Europe/London">London (GMT)</option>
          <option value="Asia/Tokyo">Tokyo (JST)</option>
        </select>
      </div>
    </div>
  );

  const renderSecuritySection = () => (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-semibold text-primary mb-4">Change Password</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-primary mb-2">Current Password</label>
            <input
              type="password"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
              placeholder="Enter current password"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-primary mb-2">New Password</label>
            <input
              type="password"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
              placeholder="Enter new password"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-primary mb-2">Confirm Password</label>
            <input
              type="password"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-transparent"
              placeholder="Confirm new password"
            />
          </div>
          <button className="px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition-colors">
            Update Password
          </button>
        </div>
      </div>

      <div className="border-t pt-6">
        <h3 className="text-lg font-semibold text-primary mb-4">Two-Factor Authentication</h3>
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <p className="text-sm text-yellow-800 mb-3">
            Two-factor authentication is not enabled. Enable it for extra security.
          </p>
          <button className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
            Enable 2FA
          </button>
        </div>
      </div>

      <div className="border-t pt-6">
        <h3 className="text-lg font-semibold text-primary mb-4">Active Sessions</h3>
        <div className="space-y-3">
          <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
            <div>
              <p className="font-medium text-primary">Current Session</p>
              <p className="text-sm text-neutral">Chrome on macOS • Active now</p>
            </div>
            <span className="text-green-600 text-sm font-medium">Active</span>
          </div>
        </div>
        <button className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors">
          Sign Out All Devices
        </button>
      </div>
    </div>
  );

  const renderContent = () => {
    switch (activeSection) {
      case 'account':
        return renderAccountSection();
      case 'api':
        return renderApiSection();
      case 'notifications':
        return renderNotificationsSection();
      case 'preferences':
        return renderPreferencesSection();
      case 'security':
        return renderSecuritySection();
      default:
        return renderAccountSection();
    }
  };

  return (
    <>
      <Head>
        <title>Settings - AlgoTrendy</title>
        <meta name="description" content="Manage your AlgoTrendy account settings" />
      </Head>

      <div className="flex flex-col h-screen bg-gray-100">
        <ToastContainer toasts={toasts} onClose={removeToast} />
        <Header onMenuClick={() => setSidebarOpen(!sidebarOpen)} />
        <div className="flex flex-1 overflow-hidden">
          <Sidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />

          <main className="flex-1 overflow-auto">
            <div className="p-6 max-w-6xl mx-auto">
              {/* Header */}
              <div className="mb-8">
                <div className="flex items-center gap-3 mb-2">
                  <SettingsIcon size={32} className="text-primary" />
                  <h1 className="text-3xl font-bold text-primary">Settings</h1>
                </div>
                <p className="text-neutral">Manage your account settings and preferences</p>
              </div>

              <div className="flex gap-6">
                {/* Sidebar Navigation */}
                <div className="w-64 flex-shrink-0">
                  <div className="bg-white rounded-lg shadow-md p-2">
                    {sections.map((section) => (
                      <button
                        key={section.id}
                        onClick={() => setActiveSection(section.id)}
                        className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                          activeSection === section.id
                            ? 'bg-primary text-white'
                            : 'text-neutral hover:bg-gray-100'
                        }`}
                      >
                        {section.icon}
                        <span className="font-medium">{section.title}</span>
                      </button>
                    ))}
                  </div>
                </div>

                {/* Content Area */}
                <div className="flex-1">
                  <div className="bg-white rounded-lg shadow-md p-6">
                    {renderContent()}

                    {/* Save Button */}
                    <div className="mt-8 pt-6 border-t flex items-center justify-between">
                      <p className="text-sm text-neutral">
                        Changes will be saved to your account
                      </p>
                      <button
                        onClick={handleSave}
                        disabled={saving}
                        className="flex items-center gap-2 px-6 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        <Save size={18} />
                        {saving ? 'Saving...' : 'Save Changes'}
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </main>
        </div>
      </div>
    </>
  );
}
