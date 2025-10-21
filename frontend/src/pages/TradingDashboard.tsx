import { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown } from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { tradingApi } from '../lib/tradingApi';

interface MarketData {
  symbol: string;
  price: number;
  change24h: number;
  volume: number;
  timestamp: string;
}

interface OrderBook {
  bids: Array<{ price: number; quantity: number }>;
  asks: Array<{ price: number; quantity: number }>;
}

export default function TradingDashboard() {
  const [selectedSymbol, setSelectedSymbol] = useState('BTC-USD');
  const [marketData, setMarketData] = useState<MarketData[]>([]);
  const [priceHistory, setPriceHistory] = useState<any[]>([]);
  const [orderBook, setOrderBook] = useState<OrderBook>({ bids: [], asks: [] });
  const [orderType, setOrderType] = useState<'market' | 'limit'>('market');
  const [orderSide, setOrderSide] = useState<'buy' | 'sell'>('buy');
  const [quantity, setQuantity] = useState('');
  const [limitPrice, setLimitPrice] = useState('');

  const symbols = ['BTC-USD', 'ETH-USD', 'BNB-USD', 'XRP-USD', 'SOL-USD', 'ADA-USD'];

  useEffect(() => {
    loadMarketData();
    loadPriceHistory();
    loadOrderBook();

    const interval = setInterval(() => {
      loadMarketData();
      loadPriceHistory();
      loadOrderBook();
    }, 5000);

    return () => clearInterval(interval);
  }, [selectedSymbol]);

  const loadMarketData = async () => {
    try {
      const data = await tradingApi.getMarketData(symbols);
      setMarketData(data);
    } catch (error) {
      console.error('Error loading market data:', error);
      // Keep existing data on error
    }
  };

  const loadPriceHistory = async () => {
    try {
      const data = await tradingApi.getPriceHistory(selectedSymbol, '1h', 24);
      setPriceHistory(data);
    } catch (error) {
      console.error('Error loading price history:', error);
    }
  };

  const loadOrderBook = async () => {
    try {
      const data = await tradingApi.getOrderBook(selectedSymbol);
      setOrderBook(data);
    } catch (error) {
      console.error('Error loading order book:', error);
    }
  };

  const handlePlaceOrder = async () => {
    try {
      const orderData = {
        symbol: selectedSymbol,
        side: orderSide,
        type: orderType,
        quantity: parseFloat(quantity),
        ...(orderType === 'limit' && { price: parseFloat(limitPrice) }),
      };

      await tradingApi.placeOrder(orderData);

      alert(`${orderSide.toUpperCase()} order placed successfully!`);
      setQuantity('');
      setLimitPrice('');
    } catch (error) {
      console.error('Error placing order:', error);
      alert(`Failed to place order: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  const currentPrice = marketData.find(m => m.symbol === selectedSymbol)?.price || 0;
  const currentChange = marketData.find(m => m.symbol === selectedSymbol)?.change24h || 0;

  return (
    <div className="dashboard">
      {/* Market Overview */}
      <div className="grid grid-cols-4" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))' }}>
        {marketData.slice(0, 4).map((data) => (
          <div key={data.symbol} className="card" style={{ cursor: 'pointer' }} onClick={() => setSelectedSymbol(data.symbol)}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div style={{ fontSize: '0.875rem', color: 'var(--text-secondary)' }}>{data.symbol}</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 600, marginTop: '0.25rem' }}>
                  ${data.price.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </div>
              </div>
              {data.change24h >= 0 ? (
                <TrendingUp className="text-success" size={24} color="var(--success)" />
              ) : (
                <TrendingDown className="text-danger" size={24} color="var(--danger)" />
              )}
            </div>
            <div style={{
              marginTop: '0.5rem',
              color: data.change24h >= 0 ? 'var(--success)' : 'var(--danger)',
              fontWeight: 600
            }}>
              {data.change24h >= 0 ? '+' : ''}{data.change24h.toFixed(2)}%
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-2">
        {/* Price Chart */}
        <div className="card" style={{ gridColumn: 'span 2' }}>
          <div className="card-header">
            <div>
              <h3 className="card-title">{selectedSymbol} Price Chart</h3>
              <div style={{ fontSize: '2rem', fontWeight: 600, marginTop: '0.5rem' }}>
                ${currentPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                <span style={{
                  fontSize: '1rem',
                  marginLeft: '1rem',
                  color: currentChange >= 0 ? 'var(--success)' : 'var(--danger)'
                }}>
                  {currentChange >= 0 ? '+' : ''}{currentChange.toFixed(2)}%
                </span>
              </div>
            </div>
          </div>

          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={priceHistory}>
              <CartesianGrid strokeDasharray="3 3" stroke="#475569" />
              <XAxis dataKey="time" stroke="#cbd5e1" />
              <YAxis stroke="#cbd5e1" domain={['auto', 'auto']} />
              <Tooltip
                contentStyle={{
                  backgroundColor: '#1e293b',
                  border: '1px solid #475569',
                  borderRadius: '6px',
                }}
              />
              <Line type="monotone" dataKey="price" stroke="#3b82f6" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      <div className="grid grid-cols-2">
        {/* Order Book */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">📖 Order Book</h3>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            {/* Asks */}
            <div>
              <div style={{ fontSize: '0.875rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--danger)' }}>
                ASKS
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                  <span>Price</span>
                  <span>Amount</span>
                </div>
              </div>
              {orderBook.asks.slice(0, 8).reverse().map((ask, i) => (
                <div key={i} style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr',
                  gap: '0.5rem',
                  fontSize: '0.875rem',
                  padding: '0.25rem 0',
                  color: 'var(--danger)'
                }}>
                  <span>{ask.price.toFixed(2)}</span>
                  <span>{ask.quantity.toFixed(4)}</span>
                </div>
              ))}
            </div>

            {/* Bids */}
            <div>
              <div style={{ fontSize: '0.875rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--success)' }}>
                BIDS
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                  <span>Price</span>
                  <span>Amount</span>
                </div>
              </div>
              {orderBook.bids.slice(0, 8).map((bid, i) => (
                <div key={i} style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr',
                  gap: '0.5rem',
                  fontSize: '0.875rem',
                  padding: '0.25rem 0',
                  color: 'var(--success)'
                }}>
                  <span>{bid.price.toFixed(2)}</span>
                  <span>{bid.quantity.toFixed(4)}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Order Form */}
        <div className="card">
          <div className="card-header">
            <h3 className="card-title">📝 Place Order</h3>
          </div>

          <div style={{ display: 'grid', gap: '1rem' }}>
            {/* Symbol Selector */}
            <div>
              <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                Symbol
              </label>
              <select
                value={selectedSymbol}
                onChange={(e) => setSelectedSymbol(e.target.value)}
                style={{
                  width: '100%',
                  padding: '0.5rem',
                  backgroundColor: 'var(--bg-tertiary)',
                  border: '1px solid var(--border)',
                  borderRadius: '6px',
                  color: 'var(--text-primary)',
                }}
              >
                {symbols.map(symbol => (
                  <option key={symbol} value={symbol}>{symbol}</option>
                ))}
              </select>
            </div>

            {/* Order Side */}
            <div>
              <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                Side
              </label>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                <button
                  onClick={() => setOrderSide('buy')}
                  className={orderSide === 'buy' ? 'btn btn-primary' : 'btn'}
                  style={orderSide === 'buy' ? { backgroundColor: 'var(--success)' } : { backgroundColor: 'var(--bg-tertiary)' }}
                >
                  BUY
                </button>
                <button
                  onClick={() => setOrderSide('sell')}
                  className={orderSide === 'sell' ? 'btn btn-primary' : 'btn'}
                  style={orderSide === 'sell' ? { backgroundColor: 'var(--danger)' } : { backgroundColor: 'var(--bg-tertiary)' }}
                >
                  SELL
                </button>
              </div>
            </div>

            {/* Order Type */}
            <div>
              <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                Order Type
              </label>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
                <button
                  onClick={() => setOrderType('market')}
                  className={orderType === 'market' ? 'btn btn-primary' : 'btn'}
                  style={orderType !== 'market' ? { backgroundColor: 'var(--bg-tertiary)' } : {}}
                >
                  Market
                </button>
                <button
                  onClick={() => setOrderType('limit')}
                  className={orderType === 'limit' ? 'btn btn-primary' : 'btn'}
                  style={orderType !== 'limit' ? { backgroundColor: 'var(--bg-tertiary)' } : {}}
                >
                  Limit
                </button>
              </div>
            </div>

            {/* Quantity */}
            <div>
              <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                Quantity
              </label>
              <input
                type="number"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                placeholder="0.00"
                step="0.0001"
                style={{
                  width: '100%',
                  padding: '0.5rem',
                  backgroundColor: 'var(--bg-tertiary)',
                  border: '1px solid var(--border)',
                  borderRadius: '6px',
                  color: 'var(--text-primary)',
                }}
              />
            </div>

            {/* Limit Price */}
            {orderType === 'limit' && (
              <div>
                <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                  Limit Price
                </label>
                <input
                  type="number"
                  value={limitPrice}
                  onChange={(e) => setLimitPrice(e.target.value)}
                  placeholder="0.00"
                  step="0.01"
                  style={{
                    width: '100%',
                    padding: '0.5rem',
                    backgroundColor: 'var(--bg-tertiary)',
                    border: '1px solid var(--border)',
                    borderRadius: '6px',
                    color: 'var(--text-primary)',
                  }}
                />
              </div>
            )}

            {/* Total */}
            {quantity && (orderType === 'market' || limitPrice) && (
              <div style={{
                padding: '0.75rem',
                backgroundColor: 'var(--bg-tertiary)',
                borderRadius: '6px',
                display: 'flex',
                justifyContent: 'space-between',
              }}>
                <span style={{ color: 'var(--text-secondary)' }}>Total:</span>
                <span style={{ fontWeight: 600 }}>
                  ${(parseFloat(quantity) * (orderType === 'market' ? currentPrice : parseFloat(limitPrice) || 0)).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
            )}

            {/* Place Order Button */}
            <button
              onClick={handlePlaceOrder}
              className="btn btn-primary"
              disabled={!quantity || (orderType === 'limit' && !limitPrice)}
              style={{
                backgroundColor: orderSide === 'buy' ? 'var(--success)' : 'var(--danger)',
                opacity: !quantity || (orderType === 'limit' && !limitPrice) ? 0.5 : 1,
              }}
            >
              Place {orderSide.toUpperCase()} Order
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
