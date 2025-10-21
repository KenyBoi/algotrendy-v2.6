import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'
import MLTrainingDashboard from './app/MLTrainingDashboard'
import './styles/App.css'

function App() {
  return (
    <Router>
      <div className="app">
        <nav className="navbar">
          <div className="nav-brand">
            <h1>🤖 AlgoTrendy v2.6</h1>
            <span className="nav-subtitle">ML Training Dashboard</span>
          </div>
          <div className="nav-links">
            <Link to="/" className="nav-link">Dashboard</Link>
            <Link to="/models" className="nav-link">Models</Link>
            <Link to="/patterns" className="nav-link">Patterns</Link>
          </div>
        </nav>

        <main className="main-content">
          <Routes>
            <Route path="/" element={<MLTrainingDashboard />} />
            <Route path="/models" element={<div>Models Page (Coming Soon)</div>} />
            <Route path="/patterns" element={<div>Patterns Page (Coming Soon)</div>} />
          </Routes>
        </main>
      </div>
    </Router>
  )
}

export default App
