import { useState } from 'react';
import { Card } from './ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { Calendar } from './ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from './ui/popover';
import { FileText, Download, CalendarIcon } from 'lucide-react';
import { format, subDays } from 'date-fns';

interface CompanyData {
  ticker: string;
  name: string;
  sector: string;
  headquarters: string;
  exchange: string;
  fye: string;
  lastUpdated: string;
}

export function DatasetBrowser() {
  const [selectedCompany, setSelectedCompany] = useState<CompanyData>({
    ticker: '$NVDA',
    name: 'NVIDIA CORP',
    sector: 'Technology',
    headquarters: 'HQ: California, United States',
    exchange: 'NasdaqGS',
    fye: 'FYE: Jan 26',
    lastUpdated: 'Last updated: Jan 12, 2025'
  });

  // Date range state - default to last 30 days
  const [dateRange, setDateRange] = useState<{ from: Date; to: Date }>({
    from: subDays(new Date(), 30),
    to: new Date()
  });
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);
  const [selectingDateFor, setSelectingDateFor] = useState<'from' | 'to'>('from');

  const dailyData = {
    sharePrice: '$135.91',
    dailyVolume: '207.60M',
    pe: '53.67x',
    avgVolume: '218.55M'
  };

  // Mock profitability data based on selected date range
  const profitabilityData = [
    {
      metric: 'Total Revenue',
      value: '$35.08B',
      growth: '93.61%'
    },
    {
      metric: 'YoY Growth %',
      value: '93.61%',
      growth: null
    },
    {
      metric: 'EBITDA',
      value: '$27.35B',
      growth: '107.13%'
    },
    {
      metric: 'YoY Growth %',
      value: '107.13%',
      growth: null
    },
    {
      metric: 'Gross Profit',
      value: '$26.14B',
      growth: '99.19%'
    },
    {
      metric: 'YoY Growth %',
      value: '99.19%',
      growth: null
    },
    {
      metric: 'Net Income',
      value: '$19.31B',
      growth: '108.96%'
    },
    {
      metric: 'YoY Growth %',
      value: '108.96%',
      growth: null
    },
    {
      metric: 'Basic EPS',
      value: '$0.79',
      growth: '110.16%'
    },
    {
      metric: 'YoY Growth %',
      value: '110.16%',
      growth: null
    },
    {
      metric: 'Diluted EPS',
      value: '$0.78',
      growth: '110.81%'
    },
    {
      metric: 'YoY Growth %',
      value: '110.81%',
      growth: null
    }
  ];

  const filings = [
    { type: '8-K: Current report filing', date: 'Nov 20, 2024' },
    { type: '10-Q: Quarterly report', date: 'Nov 20, 2024' },
    { type: 'NVIDIA Corporation, Q3 2025 Earnings Call, Nov 20, 2024', date: 'Nov 20, 2024' }
  ];

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-gray-100">{selectedCompany.name} <span className="text-blue-400 font-numeric">{selectedCompany.ticker}</span></h1>
            <Badge variant="outline" className="border-slate-600 text-gray-300">{selectedCompany.sector}</Badge>
            <Badge variant="outline" className="border-slate-600 text-gray-300">Semiconductors</Badge>
          </div>
          <div className="flex items-center gap-4 mt-2 text-sm text-gray-400">
            <span>{selectedCompany.headquarters}</span>
            <span>•</span>
            <span>{selectedCompany.exchange}</span>
            <span>•</span>
            <span>{selectedCompany.fye}</span>
            <span>•</span>
            <span>{selectedCompany.lastUpdated}</span>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <Tabs defaultValue="financial-metrics" className="w-full">
        <div className="flex justify-end">
          <TabsList>
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="customers">Customers</TabsTrigger>
            <TabsTrigger value="financial-metrics">Financial metrics</TabsTrigger>
            <TabsTrigger value="sentiment">Intraday Sentiment</TabsTrigger>
            <TabsTrigger value="filings">Filings & transcripts</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="financial-metrics" className="space-y-6">
          {/* Daily Data */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Daily data</h3>
            <div className="grid grid-cols-4 gap-6">
              <div>
                <div className="text-sm text-gray-400">Share price last close</div>
                <div className="text-xl mt-1 font-numeric text-gray-100">{dailyData.sharePrice}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">P/E (LTM)</div>
                <div className="text-xl mt-1 font-numeric text-gray-100">{dailyData.pe}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Daily volume</div>
                <div className="text-xl mt-1 font-numeric text-gray-100">{dailyData.dailyVolume}</div>
              </div>
              <div>
                <div className="text-sm text-gray-400">Avg. 3 month volume</div>
                <div className="text-xl mt-1 font-numeric text-gray-100">{dailyData.avgVolume}</div>
              </div>
            </div>
          </Card>

          {/* Profitability Table */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-gray-100">Profitability</h3>
              <div className="flex items-center gap-4">
                <Popover open={isCalendarOpen} onOpenChange={setIsCalendarOpen}>
                  <PopoverTrigger asChild>
                    <Button 
                      variant="outline" 
                      className="w-auto bg-slate-800 border-slate-700 text-gray-100 hover:bg-slate-700 hover:text-gray-100"
                    >
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {format(dateRange.from, 'MMM d, yyyy')} - {format(dateRange.to, 'MMM d, yyyy')}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0 bg-slate-800 border-slate-700" align="end">
                    <div className="p-4 space-y-4">
                      <div className="space-y-2">
                        <div className="text-sm text-gray-400">From Date</div>
                        <Calendar
                          mode="single"
                          selected={dateRange.from}
                          onSelect={(date) => date && setDateRange(prev => ({ ...prev, from: date }))}
                          className="rounded-md border border-slate-700"
                          classNames={{
                            months: "flex flex-col sm:flex-row space-y-4 sm:space-x-4 sm:space-y-0",
                            month: "space-y-4",
                            caption: "flex justify-center pt-1 relative items-center text-gray-100",
                            caption_label: "text-sm font-medium",
                            nav: "space-x-1 flex items-center",
                            nav_button: "h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100 text-gray-100",
                            nav_button_previous: "absolute left-1",
                            nav_button_next: "absolute right-1",
                            table: "w-full border-collapse space-y-1",
                            head_row: "flex",
                            head_cell: "text-gray-400 rounded-md w-9 font-normal text-[0.8rem]",
                            row: "flex w-full mt-2",
                            cell: "text-center text-sm p-0 relative [&:has([aria-selected])]:bg-slate-700 first:[&:has([aria-selected])]:rounded-l-md last:[&:has([aria-selected])]:rounded-r-md focus-within:relative focus-within:z-20",
                            day: "h-9 w-9 p-0 font-normal text-gray-100 hover:bg-slate-700 hover:text-gray-100 aria-selected:opacity-100",
                            day_selected: "bg-blue-600 text-white hover:bg-blue-600 hover:text-white focus:bg-blue-600 focus:text-white",
                            day_today: "bg-slate-700 text-gray-100",
                            day_outside: "text-gray-500 opacity-50",
                            day_disabled: "text-gray-500 opacity-50",
                            day_range_middle: "aria-selected:bg-slate-700 aria-selected:text-gray-100",
                            day_hidden: "invisible",
                          }}
                        />
                      </div>
                      <div className="space-y-2">
                        <div className="text-sm text-gray-400">To Date</div>
                        <Calendar
                          mode="single"
                          selected={dateRange.to}
                          onSelect={(date) => date && setDateRange(prev => ({ ...prev, to: date }))}
                          className="rounded-md border border-slate-700"
                          classNames={{
                            months: "flex flex-col sm:flex-row space-y-4 sm:space-x-4 sm:space-y-0",
                            month: "space-y-4",
                            caption: "flex justify-center pt-1 relative items-center text-gray-100",
                            caption_label: "text-sm font-medium",
                            nav: "space-x-1 flex items-center",
                            nav_button: "h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100 text-gray-100",
                            nav_button_previous: "absolute left-1",
                            nav_button_next: "absolute right-1",
                            table: "w-full border-collapse space-y-1",
                            head_row: "flex",
                            head_cell: "text-gray-400 rounded-md w-9 font-normal text-[0.8rem]",
                            row: "flex w-full mt-2",
                            cell: "text-center text-sm p-0 relative [&:has([aria-selected])]:bg-slate-700 first:[&:has([aria-selected])]:rounded-l-md last:[&:has([aria-selected])]:rounded-r-md focus-within:relative focus-within:z-20",
                            day: "h-9 w-9 p-0 font-normal text-gray-100 hover:bg-slate-700 hover:text-gray-100 aria-selected:opacity-100",
                            day_selected: "bg-blue-600 text-white hover:bg-blue-600 hover:text-white focus:bg-blue-600 focus:text-white",
                            day_today: "bg-slate-700 text-gray-100",
                            day_outside: "text-gray-500 opacity-50",
                            day_disabled: "text-gray-500 opacity-50",
                            day_range_middle: "aria-selected:bg-slate-700 aria-selected:text-gray-100",
                            day_hidden: "invisible",
                          }}
                        />
                      </div>
                      <div className="flex justify-end gap-2 pt-2 border-t border-slate-700">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setDateRange({
                              from: subDays(new Date(), 30),
                              to: new Date()
                            });
                          }}
                          className="bg-slate-700 border-slate-600 text-gray-100 hover:bg-slate-600 hover:text-gray-100"
                        >
                          Last 30 days
                        </Button>
                        <Button
                          size="sm"
                          onClick={() => setIsCalendarOpen(false)}
                          className="bg-blue-600 text-white hover:bg-blue-700"
                        >
                          Apply
                        </Button>
                      </div>
                    </div>
                  </PopoverContent>
                </Popover>
              </div>
            </div>

            <div className="overflow-x-auto border border-slate-800 rounded-lg">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800 bg-slate-800/50">
                    <th className="text-left py-3 px-4 text-xs text-gray-400 font-medium">Metric</th>
                    <th className="text-right py-3 px-4 text-xs text-gray-400 font-medium">
                      {format(dateRange.from, 'MMM d')} - {format(dateRange.to, 'MMM d, yyyy')}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {profitabilityData.map((row, idx) => (
                    <tr key={idx} className="border-b border-slate-800 hover:bg-slate-800/30">
                      <td className="py-2 px-4 text-sm text-gray-300">{row.metric}</td>
                      <td className="text-right py-2 px-4">
                        <div className="text-sm font-numeric text-gray-100">{row.value}</div>
                        {row.growth && (
                          <div className="text-xs text-green-400">{row.growth}</div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>

          {/* Expandable Sections */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <button className="flex items-center justify-between w-full hover:opacity-80 transition-opacity">
              <h3 className="text-gray-100">Valuation</h3>
              <span className="text-2xl text-gray-400">+</span>
            </button>
          </Card>

          <Card className="p-6 bg-slate-900 border-slate-800">
            <button className="flex items-center justify-between w-full hover:opacity-80 transition-opacity">
              <h3 className="text-gray-100">Balance Sheet</h3>
              <span className="text-2xl text-gray-400">+</span>
            </button>
          </Card>

          <Card className="p-6 bg-slate-900 border-slate-800">
            <button className="flex items-center justify-between w-full hover:opacity-80 transition-opacity">
              <h3 className="text-gray-100">Cash Flow</h3>
              <span className="text-2xl text-gray-400">+</span>
            </button>
          </Card>
        </TabsContent>

        <TabsContent value="sentiment" className="space-y-6">
          {/* Real-Time Sentiment Overview */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Live Market Sentiment - {selectedCompany.ticker}</h3>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="p-4 bg-slate-800/50 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-1">Overall Sentiment</div>
                <div className="text-2xl font-numeric text-green-400">Bullish</div>
                <div className="text-xs text-gray-500 mt-1">Last 15 min</div>
              </div>
              <div className="p-4 bg-slate-800/50 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-1">Social Buzz</div>
                <div className="text-2xl font-numeric text-orange-400">+847%</div>
                <div className="text-xs text-gray-500 mt-1">vs 1h avg</div>
              </div>
              <div className="p-4 bg-slate-800/50 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-1">Options Flow</div>
                <div className="text-2xl font-numeric text-green-400">+$42.8M</div>
                <div className="text-xs text-gray-500 mt-1">Call bias</div>
              </div>
              <div className="p-4 bg-slate-800/50 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-1">Momentum</div>
                <div className="text-2xl font-numeric text-green-400">Strong</div>
                <div className="text-xs text-gray-500 mt-1">Accelerating</div>
              </div>
            </div>
          </Card>

          {/* Pre-Market & Opening Sentiment */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Session Sentiment Tracker</h3>
            <div className="space-y-4">
              <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                <div className="flex items-center gap-4">
                  <div className="text-xs text-gray-400 w-24">Pre-Market</div>
                  <div className="flex items-center gap-2">
                    <div className="w-32 h-2 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-green-400" style={{ width: '68%' }}></div>
                    </div>
                    <span className="text-sm text-green-400 font-numeric">68% Bullish</span>
                  </div>
                </div>
                <span className="text-xs text-gray-500">04:00 - 09:30</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                <div className="flex items-center gap-4">
                  <div className="text-xs text-gray-400 w-24">Opening Hour</div>
                  <div className="flex items-center gap-2">
                    <div className="w-32 h-2 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-green-400" style={{ width: '72%' }}></div>
                    </div>
                    <span className="text-sm text-green-400 font-numeric">72% Bullish</span>
                  </div>
                </div>
                <span className="text-xs text-gray-500">09:30 - 10:30</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                <div className="flex items-center gap-4">
                  <div className="text-xs text-gray-400 w-24">Mid-Day</div>
                  <div className="flex items-center gap-2">
                    <div className="w-32 h-2 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-yellow-400" style={{ width: '52%' }}></div>
                    </div>
                    <span className="text-sm text-yellow-400 font-numeric">52% Neutral</span>
                  </div>
                </div>
                <span className="text-xs text-gray-500">10:30 - 15:00</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                <div className="flex items-center gap-4">
                  <div className="text-xs text-gray-400 w-24">Power Hour</div>
                  <div className="flex items-center gap-2">
                    <div className="w-32 h-2 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-green-400" style={{ width: '81%' }}></div>
                    </div>
                    <span className="text-sm text-green-400 font-numeric">81% Bullish</span>
                  </div>
                </div>
                <span className="text-xs text-gray-500">15:00 - 16:00</span>
              </div>
            </div>
          </Card>

          {/* Social Media Sentiment */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card className="p-6 bg-slate-900 border-slate-800">
              <h3 className="mb-4 text-gray-100">Social Media Sentiment (Last Hour)</h3>
              <div className="space-y-3">
                <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                  <div>
                    <div className="text-sm text-gray-300">Twitter/X</div>
                    <div className="text-xs text-gray-500 mt-1">2,847 mentions</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm text-green-400 font-numeric">+74% Bull</div>
                    <div className="text-xs text-gray-500 mt-1">Trending #3</div>
                  </div>
                </div>
                <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                  <div>
                    <div className="text-sm text-gray-300">Reddit WSB</div>
                    <div className="text-xs text-gray-500 mt-1">428 posts</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm text-green-400 font-numeric">+68% Bull</div>
                    <div className="text-xs text-gray-500 mt-1">Hot topic</div>
                  </div>
                </div>
                <div className="flex items-center justify-between p-3 bg-slate-800/30 border border-slate-700 rounded">
                  <div>
                    <div className="text-sm text-gray-300">StockTwits</div>
                    <div className="text-xs text-gray-500 mt-1">1,234 watchers</div>
                  </div>
                  <div className="text-right">
                    <div className="text-sm text-green-400 font-numeric">+71% Bull</div>
                    <div className="text-xs text-gray-500 mt-1">Very active</div>
                  </div>
                </div>
              </div>
            </Card>

            <Card className="p-6 bg-slate-900 border-slate-800">
              <h3 className="mb-4 text-gray-100">Breaking News Impact (15 min)</h3>
              <div className="space-y-3">
                <div className="p-3 bg-slate-800/30 border border-green-500/20 rounded">
                  <div className="flex items-center justify-between mb-2">
                    <Badge className="bg-green-500/10 text-green-400 border-green-500/20">Positive</Badge>
                    <span className="text-xs text-gray-500">2 min ago</span>
                  </div>
                  <p className="text-sm text-gray-300">AI chip demand exceeds expectations for Q4</p>
                  <div className="flex items-center gap-2 mt-2">
                    <div className="text-xs text-gray-500">Impact:</div>
                    <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-green-400" style={{ width: '89%' }}></div>
                    </div>
                    <span className="text-xs text-green-400">+89</span>
                  </div>
                </div>
                <div className="p-3 bg-slate-800/30 border border-blue-500/20 rounded">
                  <div className="flex items-center justify-between mb-2">
                    <Badge className="bg-blue-500/10 text-blue-400 border-blue-500/20">Neutral</Badge>
                    <span className="text-xs text-gray-500">8 min ago</span>
                  </div>
                  <p className="text-sm text-gray-300">Analyst maintains price target at $165</p>
                  <div className="flex items-center gap-2 mt-2">
                    <div className="text-xs text-gray-500">Impact:</div>
                    <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-blue-400" style={{ width: '42%' }}></div>
                    </div>
                    <span className="text-xs text-blue-400">+42</span>
                  </div>
                </div>
                <div className="p-3 bg-slate-800/30 border border-green-500/20 rounded">
                  <div className="flex items-center justify-between mb-2">
                    <Badge className="bg-green-500/10 text-green-400 border-green-500/20">Positive</Badge>
                    <span className="text-xs text-gray-500">14 min ago</span>
                  </div>
                  <p className="text-sm text-gray-300">Major cloud provider announces GPU purchase</p>
                  <div className="flex items-center gap-2 mt-2">
                    <div className="text-xs text-gray-500">Impact:</div>
                    <div className="flex-1 h-1.5 bg-slate-700 rounded-full overflow-hidden">
                      <div className="h-full bg-green-400" style={{ width: '76%' }}></div>
                    </div>
                    <span className="text-xs text-green-400">+76</span>
                  </div>
                </div>
              </div>
            </Card>
          </div>

          {/* Volume & Flow Detection */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Institutional Flow Detection (Real-Time)</h3>
            <div className="overflow-x-auto border border-slate-800 rounded-lg">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-slate-800 bg-slate-800/50">
                    <th className="text-left py-3 px-4 text-xs text-gray-400 font-medium">Time</th>
                    <th className="text-left py-3 px-4 text-xs text-gray-400 font-medium">Type</th>
                    <th className="text-right py-3 px-4 text-xs text-gray-400 font-medium">Size</th>
                    <th className="text-right py-3 px-4 text-xs text-gray-400 font-medium">Price</th>
                    <th className="text-left py-3 px-4 text-xs text-gray-400 font-medium">Signal</th>
                  </tr>
                </thead>
                <tbody>
                  <tr className="border-b border-slate-800 hover:bg-slate-800/30">
                    <td className="py-2 px-4 text-sm text-gray-400 font-mono">09:31:18</td>
                    <td className="py-2 px-4">
                      <Badge className="bg-green-500/10 text-green-400 border-green-500/20">Block Buy</Badge>
                    </td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">250,000</td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">$135.92</td>
                    <td className="py-2 px-4 text-sm text-green-400">Bullish</td>
                  </tr>
                  <tr className="border-b border-slate-800 hover:bg-slate-800/30">
                    <td className="py-2 px-4 text-sm text-gray-400 font-mono">09:28:42</td>
                    <td className="py-2 px-4">
                      <Badge className="bg-green-500/10 text-green-400 border-green-500/20">Sweep</Badge>
                    </td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">180,000</td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">$135.88</td>
                    <td className="py-2 px-4 text-sm text-green-400">Bullish</td>
                  </tr>
                  <tr className="border-b border-slate-800 hover:bg-slate-800/30">
                    <td className="py-2 px-4 text-sm text-gray-400 font-mono">09:26:15</td>
                    <td className="py-2 px-4">
                      <Badge className="bg-orange-500/10 text-orange-400 border-orange-500/20">Dark Pool</Badge>
                    </td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">500,000</td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">$135.85</td>
                    <td className="py-2 px-4 text-sm text-orange-400">Watch</td>
                  </tr>
                  <tr className="border-b border-slate-800 hover:bg-slate-800/30">
                    <td className="py-2 px-4 text-sm text-gray-400 font-mono">09:24:33</td>
                    <td className="py-2 px-4">
                      <Badge className="bg-red-500/10 text-red-400 border-red-500/20">Block Sell</Badge>
                    </td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">120,000</td>
                    <td className="text-right py-2 px-4 text-sm font-numeric text-gray-100">$135.81</td>
                    <td className="py-2 px-4 text-sm text-red-400">Bearish</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </Card>

          {/* Technical Momentum Indicators */}
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Short-Term Technical Sentiment</h3>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="p-4 bg-slate-800/30 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-2">RSI (5-min)</div>
                <div className="text-xl font-numeric text-green-400 mb-1">68.4</div>
                <div className="text-xs text-green-400">Bullish</div>
              </div>
              <div className="p-4 bg-slate-800/30 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-2">MACD (15-min)</div>
                <div className="text-xl font-numeric text-green-400 mb-1">+0.42</div>
                <div className="text-xs text-green-400">Buy Signal</div>
              </div>
              <div className="p-4 bg-slate-800/30 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-2">Volume Surge</div>
                <div className="text-xl font-numeric text-orange-400 mb-1">+284%</div>
                <div className="text-xs text-orange-400">Above Avg</div>
              </div>
              <div className="p-4 bg-slate-800/30 border border-slate-700 rounded">
                <div className="text-xs text-gray-400 mb-2">Momentum Score</div>
                <div className="text-xl font-numeric text-green-400 mb-1">8.7/10</div>
                <div className="text-xs text-green-400">Strong</div>
              </div>
            </div>
          </Card>
        </TabsContent>

        <TabsContent value="filings" className="space-y-4">
          <Card className="p-6 bg-slate-900 border-slate-800">
            <h3 className="mb-4 text-gray-100">Key filings and transcripts</h3>
            <div className="space-y-3">
              {filings.map((filing, idx) => (
                <div key={idx} className="flex items-center justify-between py-2 border-b border-slate-800 last:border-b-0 hover:bg-slate-800/30 transition-colors px-2 -mx-2 rounded">
                  <div className="flex items-center gap-3">
                    <FileText className="h-4 w-4 text-blue-400" />
                    <span className="text-sm text-gray-300">{filing.type}</span>
                  </div>
                  <span className="text-sm text-gray-500">{filing.date}</span>
                </div>
              ))}
            </div>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
