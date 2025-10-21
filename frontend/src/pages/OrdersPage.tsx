import { useState, useEffect } from 'react';
import { CheckCircle, XCircle, Clock, TrendingUp, TrendingDown } from 'lucide-react';
import { tradingApi } from '../lib/tradingApi';

interface Order {
  id: string;
  symbol: string;
  side: 'buy' | 'sell';
  type: 'market' | 'limit' | 'stop';
  quantity: number;
  price?: number;
  filled: number;
  status: 'pending' | 'filled' | 'partial' | 'cancelled' | 'rejected';
  timestamp: string;
  broker: string;
}

interface Position {
  id: string;
  symbol: string;
  side: 'long' | 'short';
  quantity: number;
  entryPrice: number;
  currentPrice: number;
  pnl: number;
  pnlPercent: number;
  broker: string;
  openedAt: string;
}

export default function OrdersPage() {
  const [activeTab, setActiveTab] = useState<'orders' | 'positions'>('orders');
  const [orders, setOrders] = useState<Order[]>([]);
  const [positions, setPositions] = useState<Position[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadData();
    const interval = setInterval(loadData, 5000);
    return () => clearInterval(interval);
  }, []);

  const loadData = async () => {
    try {
      const [ordersData, positionsData] = await Promise.all([
        tradingApi.getOrders(),
        tradingApi.getPositions(),
      ]);

      setOrders(ordersData);
      setPositions(positionsData);
      setLoading(false);
    } catch (error) {
      console.error('Error loading data:', error);
      setLoading(false);
    }
  };

  const handleCancelOrder = async (orderId: string) => {
    try {
      await tradingApi.cancelOrder(orderId);
      alert(`Order ${orderId} cancelled successfully`);
      loadData();
    } catch (error) {
      console.error('Error cancelling order:', error);
      alert(`Failed to cancel order: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  const handleClosePosition = async (positionId: string) => {
    try {
      await tradingApi.closePosition(positionId);
      alert(`Position ${positionId} closed successfully`);
      loadData();
    } catch (error) {
      console.error('Error closing position:', error);
      alert(`Failed to close position: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  const getStatusIcon = (status: Order['status']) => {
    switch (status) {
      case 'filled':
        return <CheckCircle size={16} color="var(--success)" />;
      case 'cancelled':
      case 'rejected':
        return <XCircle size={16} color="var(--danger)" />;
      case 'pending':
      case 'partial':
        return <Clock size={16} color="var(--warning)" />;
    }
  };

  const totalPnL = positions.reduce((sum, p) => sum + p.pnl, 0);
  const activeOrders = orders.filter(o => o.status === 'pending' || o.status === 'partial').length;

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <h2>Loading...</h2>
      </div>
    );
  }

  return (
    <div className="dashboard">
      {/* Summary Cards */}
      <div className="grid grid-cols-4" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))' }}>
        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <Clock size={20} color="var(--warning)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Active Orders</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            {activeOrders}
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <TrendingUp size={20} color="var(--primary)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Open Positions</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            {positions.length}
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            {totalPnL >= 0 ? (
              <TrendingUp size={20} color="var(--success)" />
            ) : (
              <TrendingDown size={20} color="var(--danger)" />
            )}
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Total P&L</span>
          </div>
          <div style={{
            fontSize: '1.75rem',
            fontWeight: 600,
            color: totalPnL >= 0 ? 'var(--success)' : 'var(--danger)'
          }}>
            {totalPnL >= 0 ? '+' : ''}${totalPnL.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </div>
        </div>

        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
            <CheckCircle size={20} color="var(--success)" />
            <span style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Filled Today</span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 600 }}>
            {orders.filter(o => o.status === 'filled').length}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="card">
        <div style={{ display: 'flex', gap: '1rem', borderBottom: '1px solid var(--border)', marginBottom: '1.5rem' }}>
          <button
            onClick={() => setActiveTab('orders')}
            style={{
              padding: '0.75rem 1.5rem',
              border: 'none',
              background: 'none',
              color: activeTab === 'orders' ? 'var(--primary)' : 'var(--text-secondary)',
              fontWeight: 600,
              borderBottom: activeTab === 'orders' ? '2px solid var(--primary)' : '2px solid transparent',
              cursor: 'pointer',
            }}
          >
            Orders ({orders.length})
          </button>
          <button
            onClick={() => setActiveTab('positions')}
            style={{
              padding: '0.75rem 1.5rem',
              border: 'none',
              background: 'none',
              color: activeTab === 'positions' ? 'var(--primary)' : 'var(--text-secondary)',
              fontWeight: 600,
              borderBottom: activeTab === 'positions' ? '2px solid var(--primary)' : '2px solid transparent',
              cursor: 'pointer',
            }}
          >
            Positions ({positions.length})
          </button>
        </div>

        {/* Orders Table */}
        {activeTab === 'orders' && (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border)' }}>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Order ID</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Symbol</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Side</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Type</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Quantity</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Price</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Filled</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Status</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Broker</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Time</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '0.75rem', fontFamily: 'monospace', fontSize: '0.875rem' }}>{order.id}</td>
                    <td style={{ padding: '0.75rem', fontWeight: 600 }}>{order.symbol}</td>
                    <td style={{ padding: '0.75rem' }}>
                      <span style={{
                        padding: '0.25rem 0.5rem',
                        borderRadius: '4px',
                        fontSize: '0.75rem',
                        fontWeight: 600,
                        backgroundColor: order.side === 'buy' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)',
                        color: order.side === 'buy' ? 'var(--success)' : 'var(--danger)',
                      }}>
                        {order.side.toUpperCase()}
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem', textTransform: 'capitalize' }}>{order.type}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>{order.quantity.toLocaleString()}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      {order.price ? `$${order.price.toLocaleString()}` : 'Market'}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      {order.filled.toLocaleString()} / {order.quantity.toLocaleString()}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}>
                        {getStatusIcon(order.status)}
                        <span className={`badge badge-${
                          order.status === 'filled' ? 'success' :
                          order.status === 'pending' || order.status === 'partial' ? 'warning' :
                          'danger'
                        }`}>
                          {order.status}
                        </span>
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem' }}>{order.broker}</td>
                    <td style={{ padding: '0.75rem', fontSize: '0.875rem' }}>
                      {new Date(order.timestamp).toLocaleString()}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      {(order.status === 'pending' || order.status === 'partial') && (
                        <button
                          onClick={() => handleCancelOrder(order.id)}
                          className="btn"
                          style={{
                            fontSize: '0.75rem',
                            padding: '0.25rem 0.5rem',
                            backgroundColor: 'var(--danger)',
                            color: 'white',
                          }}
                        >
                          Cancel
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {orders.length === 0 && (
              <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
                <p>No orders found.</p>
              </div>
            )}
          </div>
        )}

        {/* Positions Table */}
        {activeTab === 'positions' && (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border)' }}>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Position ID</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Symbol</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Side</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Quantity</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Entry Price</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Current Price</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>P&L</th>
                  <th style={{ padding: '0.75rem', textAlign: 'right', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>P&L %</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Broker</th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Opened At</th>
                  <th style={{ padding: '0.75rem', textAlign: 'center', fontSize: '0.875rem', color: 'var(--text-secondary)' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {positions.map((position) => (
                  <tr key={position.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '0.75rem', fontFamily: 'monospace', fontSize: '0.875rem' }}>{position.id}</td>
                    <td style={{ padding: '0.75rem', fontWeight: 600 }}>{position.symbol}</td>
                    <td style={{ padding: '0.75rem' }}>
                      <span style={{
                        padding: '0.25rem 0.5rem',
                        borderRadius: '4px',
                        fontSize: '0.75rem',
                        fontWeight: 600,
                        backgroundColor: position.side === 'long' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)',
                        color: position.side === 'long' ? 'var(--success)' : 'var(--danger)',
                      }}>
                        {position.side.toUpperCase()}
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>{position.quantity.toLocaleString()}</td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      ${position.entryPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                      ${position.currentPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                    <td style={{
                      padding: '0.75rem',
                      textAlign: 'right',
                      fontWeight: 600,
                      color: position.pnl >= 0 ? 'var(--success)' : 'var(--danger)'
                    }}>
                      {position.pnl >= 0 ? '+' : ''}${position.pnl.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                    <td style={{
                      padding: '0.75rem',
                      textAlign: 'right',
                      fontWeight: 600,
                      color: position.pnlPercent >= 0 ? 'var(--success)' : 'var(--danger)'
                    }}>
                      {position.pnlPercent >= 0 ? '+' : ''}{position.pnlPercent.toFixed(2)}%
                    </td>
                    <td style={{ padding: '0.75rem' }}>{position.broker}</td>
                    <td style={{ padding: '0.75rem', fontSize: '0.875rem' }}>
                      {new Date(position.openedAt).toLocaleString()}
                    </td>
                    <td style={{ padding: '0.75rem', textAlign: 'center' }}>
                      <button
                        onClick={() => handleClosePosition(position.id)}
                        className="btn btn-primary"
                        style={{ fontSize: '0.75rem', padding: '0.25rem 0.5rem' }}
                      >
                        Close
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {positions.length === 0 && (
              <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>
                <p>No open positions.</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
