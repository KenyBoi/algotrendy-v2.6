import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom'
import { Home, TrendingUp, Briefcase, BarChart3, FileText, Activity, Brain, Cpu, TestTube } from 'lucide-react'
import MLTrainingDashboard from './app/MLTrainingDashboard'
import TradingDashboard from './pages/TradingDashboard'
import PortfolioPage from './pages/PortfolioPage'
import BacktestingPage from './pages/BacktestingPage'
import OrdersPage from './pages/OrdersPage'
import StrategyBuilderPage from './pages/StrategyBuilderPage'
import ModelTestingPanel from './components/testing/ModelTestingPanel'
import './styles/App.css'

function Navigation() {
  const location = useLocation()

  const navItems = [
    { path: '/', label: 'Trading', icon: Home },
    { path: '/portfolio', label: 'Portfolio', icon: Briefcase },
    { path: '/orders', label: 'Orders', icon: FileText },
    { path: '/strategies', label: 'Strategy Builder', icon: Cpu },
    { path: '/backtesting', label: 'Backtesting', icon: BarChart3 },
    { path: '/testing', label: 'Model Testing', icon: TestTube },
    { path: '/ml', label: 'AI/ML', icon: Brain },
    { path: '/performance', label: 'Performance', icon: Activity },
  ]

  return (
    <nav className="navbar">
      <div className="nav-brand">
        <TrendingUp size={24} color="var(--primary)" />
        <div style={{ marginLeft: '0.75rem' }}>
          <h1 style={{ margin: 0 }}>AlgoTrendy</h1>
          <span className="nav-subtitle">v2.6 Enterprise Trading Platform</span>
        </div>
      </div>
      <div className="nav-links">
        {navItems.map(({ path, label, icon: Icon }) => (
          <Link
            key={path}
            to={path}
            className="nav-link"
            style={{
              color: location.pathname === path ? 'var(--primary)' : 'var(--text-secondary)',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
            }}
          >
            <Icon size={18} />
            {label}
          </Link>
        ))}
      </div>
    </nav>
  )
}

function App() {
  return (
    <Router>
      <div className="app">
        <Navigation />

        <main className="main-content">
          <Routes>
            <Route path="/" element={<TradingDashboard />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/orders" element={<OrdersPage />} />
            <Route path="/strategies" element={<StrategyBuilderPage />} />
            <Route path="/backtesting" element={<BacktestingPage />} />
            <Route path="/testing" element={<ModelTestingPanel />} />
            <Route path="/ml" element={<MLTrainingDashboard />} />
            <Route path="/performance" element={
              <div style={{ textAlign: 'center', padding: '4rem' }}>
                <h2>Performance Analytics</h2>
                <p style={{ color: 'var(--text-secondary)', marginTop: '1rem' }}>
                  Coming Soon - Advanced performance metrics and analytics
                </p>
              </div>
            } />
          </Routes>
        </main>
      </div>
    </Router>
  )
}

export default App
