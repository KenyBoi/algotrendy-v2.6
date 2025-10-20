/**
 * Orders Page
 * Place new orders and view order history
 */

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import api, { Order, OrderRequest } from '../lib/api-client';
import { Plus, X, Check, Clock, AlertCircle } from 'lucide-react';
import { useToast } from '../components/ui/use-toast';

export default function Orders() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [showOrderForm, setShowOrderForm] = useState(false);
  const [formData, setFormData] = useState<OrderRequest>({
    symbol: 'BTC-USD',
    side: 'Buy',
    type: 'Market',
    quantity: 0.01,
  });
  const { toast } = useToast();

  useEffect(() => {
    fetchOrders();
  }, []);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      const response = await api.orders.getAll();
      setOrders(response.data);
    } catch (error) {
      console.error('Error fetching orders:', error);
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };

  const handlePlaceOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await api.orders.place(formData);
      toast({
        title: 'Order Placed',
        description: `${formData.side} order for ${formData.quantity} ${formData.symbol}`,
      });
      setShowOrderForm(false);
      fetchOrders();
    } catch (error) {
      toast({
        title: 'Order Failed',
        description: 'Failed to place order',
        variant: 'destructive',
      });
    }
  };

  const getStatusIcon = (status: Order['status']) => {
    switch (status) {
      case 'Filled':
        return <Check className="w-4 h-4 text-green-500" />;
      case 'Cancelled':
      case 'Rejected':
        return <X className="w-4 h-4 text-red-500" />;
      case 'Pending':
      case 'Open':
        return <Clock className="w-4 h-4 text-yellow-500" />;
      default:
        return <AlertCircle className="w-4 h-4 text-gray-500" />;
    }
  };

  return (
    <div className="p-8">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Orders</h1>
          <p className="text-gray-400">Manage and track your trading orders</p>
        </div>
        <Button onClick={() => setShowOrderForm(true)} className="bg-blue-600 hover:bg-blue-700">
          <Plus className="w-4 h-4 mr-2" />
          New Order
        </Button>
      </div>

      {/* New Order Form */}
      {showOrderForm && (
        <Card className="bg-slate-900 border-slate-800 mb-6">
          <CardHeader>
            <CardTitle className="text-white">Place New Order</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handlePlaceOrder} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="symbol" className="text-gray-300">Symbol</Label>
                  <Input
                    id="symbol"
                    value={formData.symbol}
                    onChange={(e) => setFormData({ ...formData, symbol: e.target.value })}
                    className="bg-slate-800 border-slate-700 text-white"
                  />
                </div>
                <div>
                  <Label htmlFor="side" className="text-gray-300">Side</Label>
                  <Select
                    value={formData.side}
                    onValueChange={(value: 'Buy' | 'Sell') => setFormData({ ...formData, side: value })}
                  >
                    <SelectTrigger className="bg-slate-800 border-slate-700 text-white">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Buy">Buy</SelectItem>
                      <SelectItem value="Sell">Sell</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label htmlFor="type" className="text-gray-300">Order Type</Label>
                  <Select
                    value={formData.type}
                    onValueChange={(value: any) => setFormData({ ...formData, type: value })}
                  >
                    <SelectTrigger className="bg-slate-800 border-slate-700 text-white">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Market">Market</SelectItem>
                      <SelectItem value="Limit">Limit</SelectItem>
                      <SelectItem value="StopMarket">Stop Market</SelectItem>
                      <SelectItem value="StopLimit">Stop Limit</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <Label htmlFor="quantity" className="text-gray-300">Quantity</Label>
                  <Input
                    id="quantity"
                    type="number"
                    step="0.01"
                    value={formData.quantity}
                    onChange={(e) => setFormData({ ...formData, quantity: parseFloat(e.target.value) })}
                    className="bg-slate-800 border-slate-700 text-white"
                  />
                </div>
              </div>
              <div className="flex gap-2">
                <Button type="submit" className="bg-blue-600 hover:bg-blue-700">
                  Place Order
                </Button>
                <Button type="button" variant="outline" onClick={() => setShowOrderForm(false)}>
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* Orders Table */}
      <Card className="bg-slate-900 border-slate-800">
        <CardHeader>
          <CardTitle className="text-white">Order History</CardTitle>
          <CardDescription>Recent and active orders</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="text-center py-8 text-gray-400">Loading orders...</div>
          ) : orders.length === 0 ? (
            <div className="text-center py-8 text-gray-400">No orders yet</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800">
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Symbol</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Side</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Type</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Quantity</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Filled</th>
                    <th className="text-right py-3 px-4 text-sm font-medium text-gray-400">Avg Price</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Status</th>
                    <th className="text-left py-3 px-4 text-sm font-medium text-gray-400">Time</th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map((order) => (
                    <tr key={order.id} className="border-b border-slate-800 hover:bg-slate-800/50">
                      <td className="py-3 px-4 text-white font-medium">{order.symbol}</td>
                      <td className="py-3 px-4">
                        <span className={`px-2 py-1 rounded text-xs font-medium ${
                          order.side === 'Buy' ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-400'
                        }`}>
                          {order.side}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-gray-300">{order.type}</td>
                      <td className="py-3 px-4 text-right text-gray-300">{order.quantity}</td>
                      <td className="py-3 px-4 text-right text-gray-300">{order.filledQuantity}</td>
                      <td className="py-3 px-4 text-right text-white">
                        ${order.averagePrice.toLocaleString()}
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(order.status)}
                          <span className="text-gray-300">{order.status}</span>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-gray-400 text-sm">
                        {new Date(order.createdAt).toLocaleString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
