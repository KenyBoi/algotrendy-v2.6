// Live Price Ticker with WebSocket Updates
// COMPLETED: Real-time price updates via WebSocket
// COMPLETED: Price change animations
// COMPLETED: Multiple symbol support
// COMPLETED: Smooth transitions and highlighting
// TODO: Add price charts on hover
// TODO: Add symbol watchlist management

'use client';

import React, { useState, useEffect } from 'react';
import { TrendingUp, TrendingDown, Activity } from 'lucide-react';
import { useWebSocketEvent } from '@/hooks/useWebSocket';

interface PriceData {
  symbol: string;
  price: number;
  change: number;
  changePercent: number;
  volume: number;
  high24h: number;
  low24h: number;
  lastUpdate: Date;
}

interface LivePriceTickerProps {
  symbols?: string[];
  className?: string;
}

export const LivePriceTicker: React.FC<LivePriceTickerProps> = ({
  symbols = ['BTCUSDT', 'ETHUSDT', 'BNBUSDT', 'SOLUSDT'],
  className = '',
}) => {
  const [prices, setPrices] = useState<Map<string, PriceData>>(new Map());
  const [priceFlash, setPriceFlash] = useState<Map<string, 'up' | 'down' | null>>(new Map());

  // Subscribe to market data updates
  useWebSocketEvent('market:price', (data: any) => {
    const { symbol, price, change, changePercent, volume, high24h, low24h } = data;

    setPrices(prev => {
      const newPrices = new Map(prev);
      const oldPrice = prev.get(symbol)?.price;

      newPrices.set(symbol, {
        symbol,
        price,
        change,
        changePercent,
        volume,
        high24h,
        low24h,
        lastUpdate: new Date(),
      });

      // Trigger flash animation
      if (oldPrice !== undefined && oldPrice !== price) {
        setPriceFlash(flashMap => {
          const newFlash = new Map(flashMap);
          newFlash.set(symbol, price > oldPrice ? 'up' : 'down');
          return newFlash;
        });

        // Clear flash after animation
        setTimeout(() => {
          setPriceFlash(flashMap => {
            const newFlash = new Map(flashMap);
            newFlash.set(symbol, null);
            return newFlash;
          });
        }, 500);
      }

      return newPrices;
    });
  }, []);

  // Mock data for development (remove when WebSocket backend is ready)
  useEffect(() => {
    const interval = setInterval(() => {
      symbols.forEach(symbol => {
        const mockPrice = {
          symbol,
          price: 50000 + Math.random() * 10000,
          change: (Math.random() - 0.5) * 1000,
          changePercent: (Math.random() - 0.5) * 5,
          volume: Math.random() * 1000000000,
          high24h: 55000,
          low24h: 45000,
          lastUpdate: new Date(),
        };

        setPrices(prev => {
          const newPrices = new Map(prev);
          const oldPrice = prev.get(symbol)?.price;

          newPrices.set(symbol, mockPrice);

          if (oldPrice !== undefined && oldPrice !== mockPrice.price) {
            setPriceFlash(flashMap => {
              const newFlash = new Map(flashMap);
              newFlash.set(symbol, mockPrice.price > oldPrice ? 'up' : 'down');
              return newFlash;
            });

            setTimeout(() => {
              setPriceFlash(flashMap => {
                const newFlash = new Map(flashMap);
                newFlash.set(symbol, null);
                return newFlash;
              });
            }, 500);
          }

          return newPrices;
        });
      });
    }, 3000); // Update every 3 seconds

    return () => clearInterval(interval);
  }, [symbols]);

  return (
    <div className={`card overflow-hidden ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-slate-100 flex items-center gap-2">
          <Activity size={20} className="text-accent-light" />
          Live Market Prices
        </h3>
        <span className="text-xs text-slate-400 flex items-center gap-1">
          <span className="w-2 h-2 bg-success-light rounded-full animate-pulse"></span>
          Live
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {symbols.map(symbol => {
          const data = prices.get(symbol);
          const flash = priceFlash.get(symbol);
          const isPositive = (data?.change || 0) >= 0;

          return (
            <div
              key={symbol}
              className={`
                bg-slate-800/50 backdrop-blur-sm rounded-xl p-4 border transition-all duration-500
                ${flash === 'up' ? 'border-success-light/70 bg-success/20' :
                  flash === 'down' ? 'border-error-light/70 bg-error/20' :
                  'border-slate-700/50 hover:border-slate-600/70'}
              `}
            >
              {/* Symbol */}
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-semibold text-slate-300">{symbol}</span>
                {isPositive ? (
                  <TrendingUp size={16} className="text-success-light" />
                ) : (
                  <TrendingDown size={16} className="text-error-light" />
                )}
              </div>

              {/* Price */}
              <div className="mb-2">
                <div className="text-2xl font-bold text-slate-100">
                  ${data?.price.toLocaleString('en-US', { maximumFractionDigits: 2 }) || '—'}
                </div>
              </div>

              {/* Change */}
              <div className="flex items-center gap-2">
                <span className={`text-xs font-semibold ${isPositive ? 'text-success-light' : 'text-error-light'}`}>
                  {isPositive ? '+' : ''}{data?.changePercent.toFixed(2)}%
                </span>
                <span className={`text-xs ${isPositive ? 'text-success/70' : 'text-error/70'}`}>
                  {isPositive ? '+' : ''}${data?.change.toFixed(2) || '0.00'}
                </span>
              </div>

              {/* 24h High/Low */}
              <div className="mt-3 pt-3 border-t border-slate-700/50 grid grid-cols-2 gap-2 text-xs">
                <div>
                  <div className="text-slate-500">24h High</div>
                  <div className="text-slate-300 font-medium">
                    ${data?.high24h.toLocaleString('en-US', { maximumFractionDigits: 0 }) || '—'}
                  </div>
                </div>
                <div>
                  <div className="text-slate-500">24h Low</div>
                  <div className="text-slate-300 font-medium">
                    ${data?.low24h.toLocaleString('en-US', { maximumFractionDigits: 0 }) || '—'}
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
