import { useState } from 'react';
import { Sidebar } from './components/Sidebar';
import { Dashboard } from './components/Dashboard';
import { DatasetBrowser } from './components/DatasetBrowser';
import { DatasetBrowserConnected } from './components/DatasetBrowserConnected';
import { StrategyBuilder } from './components/StrategyBuilder';
import { QueryBuilder } from './components/QueryBuilder';
import { AIAssistant } from './components/AIAssistant';
import { MEMCorner } from './components/MEMCorner';
import { Login } from './components/Login';

export default function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [activeView, setActiveView] = useState('dashboard');
  const [isAIOpen, setIsAIOpen] = useState(false);
  const [isAIMinimized, setIsAIMinimized] = useState(false);
  const [isMEMExpanded, setIsMEMExpanded] = useState(false);
  const [useBackend, setUseBackend] = useState(false); // Default to mock data

  // Show login page if not authenticated
  if (!isAuthenticated) {
    return <Login onLogin={() => setIsAuthenticated(true)} />;
  }

  return (
    <div className="flex h-screen bg-slate-950">
      <Sidebar activeView={activeView} onViewChange={setActiveView} />
      
      <main className="flex-1 overflow-auto">
        <div className="p-8">
          {/* Toggle between connected and mock data */}
          <div className="mb-4 flex items-center justify-end gap-4 p-2.5 bg-slate-900 border border-slate-800 rounded-lg">
            <span className="text-xs text-gray-400">Brokers:</span>
            <div className="flex items-center gap-2 px-2.5 py-1 bg-slate-800/50 rounded border border-slate-700">
              <div className="w-1.5 h-1.5 bg-green-400 rounded-full animate-pulse" />
              <span className="text-xs text-gray-300 font-numeric">Interactive Brokers</span>
            </div>
            <div className="flex items-center gap-2 px-2.5 py-1 bg-slate-800/50 rounded border border-slate-700">
              <div className="w-1.5 h-1.5 bg-green-400 rounded-full animate-pulse" />
              <span className="text-xs text-gray-300 font-numeric">Alpaca</span>
            </div>
            <div className="flex items-center gap-2 px-2.5 py-1 bg-slate-800/50 rounded border border-slate-700">
              <div className="w-1.5 h-1.5 bg-yellow-400 rounded-full" />
              <span className="text-xs text-gray-300 font-numeric">TD Ameritrade</span>
            </div>
          </div>
          
          {activeView === 'dashboard' && <Dashboard />}
          {activeView === 'datasets' && (
            useBackend ? <DatasetBrowserConnected /> : <DatasetBrowser />
          )}
          {activeView === 'strategy' && <StrategyBuilder />}
          {activeView === 'query' && <QueryBuilder />}
          {activeView === 'analytics' && (
            <div className="flex items-center justify-center h-96">
              <div className="text-center">
                <h2 className="mb-2 text-gray-100">Metrics & Reports</h2>
                <p className="text-gray-400">Analytics and reporting dashboard coming soon</p>
              </div>
            </div>
          )}
        </div>
      </main>

      {/* MEM's Corner - Always visible */}
      {activeView !== 'datasets' && (
        <MEMCorner
          onOpenChat={() => {
            setIsAIOpen(true);
            setIsMEMExpanded(false);
          }}
          isExpanded={isMEMExpanded && !isAIOpen}
          onToggleExpand={() => setIsMEMExpanded(!isMEMExpanded)}
        />
      )}

      {/* MEM Chat (Full conversation interface) */}
      <AIAssistant
        isOpen={isAIOpen}
        onClose={() => setIsAIOpen(false)}
        isMinimized={isAIMinimized}
        onToggleMinimize={() => setIsAIMinimized(!isAIMinimized)}
      />
    </div>
  );
}
