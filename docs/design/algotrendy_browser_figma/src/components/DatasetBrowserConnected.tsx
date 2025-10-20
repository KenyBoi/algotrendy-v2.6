import { useState, useEffect } from 'react';
import { Card } from './ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Button } from './ui/button';
import { FileText } from 'lucide-react';
import { datasetService } from '../services';
import { useApi } from '../hooks/useApi';
import type { CompanyData, DailyData, FinancialMetric, Filing } from '../types';

export function DatasetBrowserConnected() {
  const [selectedTicker, setSelectedTicker] = useState('NVDA');
  const [period, setPeriod] = useState<'quarter' | 'annual'>('quarter');
  
  // API hooks
  const companyApi = useApi<CompanyData>();
  const dailyDataApi = useApi<DailyData>();
  const financialMetricsApi = useApi<FinancialMetric[]>();
  const filingsApi = useApi<Filing[]>();

  // Fetch data when ticker changes
  useEffect(() => {
    if (selectedTicker) {
      companyApi.execute(datasetService.getCompany(selectedTicker));
      dailyDataApi.execute(datasetService.getDailyData(selectedTicker));
      financialMetricsApi.execute(datasetService.getFinancialMetrics(selectedTicker, period));
      filingsApi.execute(datasetService.getFilings(selectedTicker));
    }
  }, [selectedTicker]);

  // Refetch financial metrics when period changes
  useEffect(() => {
    if (selectedTicker) {
      financialMetricsApi.execute(datasetService.getFinancialMetrics(selectedTicker, period));
    }
  }, [period]);

  const handleAddToMyCompanies = async () => {
    const response = await datasetService.addToMyCompanies(selectedTicker);
    if (response.success) {
      alert('Company added to your watchlist!');
    } else {
      alert('Failed to add company: ' + response.error);
    }
  };

  const isLoading = companyApi.loading || dailyDataApi.loading || financialMetricsApi.loading;
  const hasError = companyApi.error || dailyDataApi.error || financialMetricsApi.error;

  if (isLoading && !companyApi.data) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center">
          <div className="animate-spin h-8 w-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto mb-4"></div>
          <p className="text-gray-300">Loading company data...</p>
          <p className="text-xs text-gray-500 mt-2">Connecting to backend...</p>
        </div>
      </div>
    );
  }

  if (hasError) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center max-w-md">
          <div className="w-16 h-16 bg-red-500/10 rounded-full flex items-center justify-center mx-auto mb-4">
            <span className="text-2xl">⚠️</span>
          </div>
          <h3 className="mb-2 text-gray-100">Backend Connection Failed</h3>
          <p className="text-sm text-gray-400 mb-4">
            {companyApi.error || dailyDataApi.error || financialMetricsApi.error}
          </p>
          <div className="p-3 bg-slate-800 border border-slate-700 rounded-lg mb-4">
            <p className="text-xs text-gray-400 mb-1">Backend URL:</p>
            <p className="text-xs font-numeric text-blue-400">http://localhost:5298/api</p>
          </div>
          <p className="text-xs text-gray-500 mb-4">
            Make sure your .NET backend is running, or uncheck "Use Backend" to use mock data.
          </p>
          <Button 
            onClick={() => window.location.reload()} 
            className="bg-blue-600 hover:bg-blue-700"
          >
            Retry Connection
          </Button>
        </div>
      </div>
    );
  }

  const company = companyApi.data;
  const dailyData = dailyDataApi.data;
  const financialMetrics = financialMetricsApi.data || [];
  const filings = filingsApi.data || [];

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1>{company?.name || 'Loading...'} <span className="text-purple-600">${selectedTicker}</span></h1>
            {company?.sector && <Badge variant="outline">{company.sector}</Badge>}
            {company?.industry && <Badge variant="outline">{company.industry}</Badge>}
          </div>
          <div className="flex items-center gap-4 mt-2 text-sm text-gray-600">
            {company?.headquarters && <span>{company.headquarters}</span>}
            {company?.exchange && (
              <>
                <span>•</span>
                <span>{company.exchange}</span>
              </>
            )}
            {company?.fye && (
              <>
                <span>•</span>
                <span>{company.fye}</span>
              </>
            )}
            {company?.lastUpdated && (
              <>
                <span>•</span>
                <span>{company.lastUpdated}</span>
              </>
            )}
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
            <TabsTrigger value="filings">Filings & transcripts</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="financial-metrics" className="space-y-6">
          {/* Daily Data */}
          {dailyData && (
            <Card className="p-6">
              <h3 className="mb-4">Daily data</h3>
              <div className="grid grid-cols-4 gap-6">
                <div>
                  <div className="text-sm text-gray-600">Share price last close</div>
                  <div className="text-2xl mt-1">{dailyData.sharePrice}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">P/E (LTM)</div>
                  <div className="text-2xl mt-1">{dailyData.pe}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Daily volume</div>
                  <div className="text-2xl mt-1">{dailyData.dailyVolume}</div>
                </div>
                <div>
                  <div className="text-sm text-gray-600">Avg. 3 month volume</div>
                  <div className="text-2xl mt-1">{dailyData.avgVolume}</div>
                </div>
              </div>
            </Card>
          )}

          {/* Profitability Table */}
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3>Profitability</h3>
              <Select value={period} onValueChange={(value: 'quarter' | 'annual') => setPeriod(value)}>
                <SelectTrigger className="w-48">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="quarter">Fiscal quarter</SelectItem>
                  <SelectItem value="annual">Fiscal year</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {financialMetricsApi.loading ? (
              <div className="text-center py-8">
                <div className="animate-spin h-6 w-6 border-4 border-purple-600 border-t-transparent rounded-full mx-auto"></div>
              </div>
            ) : financialMetrics.length > 0 ? (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-3 px-4"></th>
                      <th className="text-right py-3 px-4">Q3 2024</th>
                      <th className="text-right py-3 px-4">Q2 2024</th>
                      <th className="text-right py-3 px-4">Q1 2024</th>
                      <th className="text-right py-3 px-4">Q4 2023</th>
                    </tr>
                  </thead>
                  <tbody>
                    {financialMetrics.map((row, idx) => (
                      <tr key={idx} className="border-b hover:bg-gray-50">
                        <td className="py-2 px-4 text-sm">{row.metric}</td>
                        <td className="text-right py-2 px-4">
                          <div className="text-sm">{row.q3_2024.value}</div>
                          {row.q3_2024.growth && (
                            <div className="text-xs text-green-600">{row.q3_2024.growth}</div>
                          )}
                        </td>
                        <td className="text-right py-2 px-4">
                          <div className="text-sm">{row.q2_2024.value}</div>
                          {row.q2_2024.growth && (
                            <div className="text-xs text-green-600">{row.q2_2024.growth}</div>
                          )}
                        </td>
                        <td className="text-right py-2 px-4">
                          <div className="text-sm">{row.q1_2024.value}</div>
                          {row.q1_2024.growth && (
                            <div className="text-xs text-green-600">{row.q1_2024.growth}</div>
                          )}
                        </td>
                        <td className="text-right py-2 px-4">
                          {row.q4_2023 ? (
                            <>
                              <div className="text-sm">{row.q4_2023.value}</div>
                              {row.q4_2023.growth && (
                                <div className="text-xs text-green-600">{row.q4_2023.growth}</div>
                              )}
                            </>
                          ) : (
                            <span className="text-gray-400">-</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No financial metrics available
              </div>
            )}
          </Card>

          {/* Expandable Sections */}
          <Card className="p-6">
            <button className="flex items-center justify-between w-full">
              <h3>Valuation</h3>
              <span className="text-2xl">+</span>
            </button>
          </Card>

          <Card className="p-6">
            <button className="flex items-center justify-between w-full">
              <h3>Balance Sheet</h3>
              <span className="text-2xl">+</span>
            </button>
          </Card>

          <Card className="p-6">
            <button className="flex items-center justify-between w-full">
              <h3>Cash Flow</h3>
              <span className="text-2xl">+</span>
            </button>
          </Card>
        </TabsContent>

        <TabsContent value="filings" className="space-y-4">
          <Card className="p-6">
            <h3 className="mb-4">Key filings and transcripts</h3>
            {filingsApi.loading ? (
              <div className="text-center py-8">
                <div className="animate-spin h-6 w-6 border-4 border-purple-600 border-t-transparent rounded-full mx-auto"></div>
              </div>
            ) : filings.length > 0 ? (
              <div className="space-y-3">
                {filings.map((filing, idx) => (
                  <div key={idx} className="flex items-center justify-between py-2 border-b last:border-b-0">
                    <div className="flex items-center gap-3">
                      <FileText className="h-4 w-4 text-gray-400" />
                      <span className="text-sm">{filing.type}</span>
                    </div>
                    <span className="text-sm text-gray-600">{filing.date}</span>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No filings available
              </div>
            )}
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
