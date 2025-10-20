import { useState } from 'react';
import { Card } from './ui/card';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Badge } from './ui/badge';
import { FileText, ThumbsUp, MessageSquare, Share2, Bookmark, ExpandIcon } from 'lucide-react';

export function QueryBuilder() {
  const [query] = useState('$NVDA Query: Revenue by product line');

  const queryResults = {
    title: 'NVIDIA: Revenue by Product Line (Last 8 Quarters)',
    quarters: [
      {
        quarter: 'Q1 2026',
        dataCenter: '$39,100,000,000',
        gaming: '$3,800,000,000',
        proViz: '$500,900,000',
        automotive: '$367,000,000'
      },
      {
        quarter: 'Q4 2025',
        dataCenter: '$35,600,000,000',
        gaming: '$2,500,000,000',
        proViz: '$511,000,000',
        automotive: '$570,000,000'
      },
      {
        quarter: 'Q3 2025',
        dataCenter: '$30,800,000,000',
        gaming: '$2,860,000,000',
        proViz: '$486,000,000',
        automotive: '$449,000,000'
      },
      {
        quarter: 'Q2 2025',
        dataCenter: '$26,300,000,000',
        gaming: '$2,880,000,000',
        proViz: '$454,000,000',
        automotive: '$346,000,000'
      },
      {
        quarter: 'Q1 2025',
        dataCenter: '$22,600,000,000',
        gaming: '$2,600,000,000',
        proViz: '$427,000,000',
        automotive: '$329,000,000'
      },
      {
        quarter: 'Q4 2024',
        dataCenter: '$18,400,000,000',
        gaming: '$2,900,000,000',
        proViz: '$463,000,000',
        automotive: '$281,000,000'
      },
      {
        quarter: 'Q3 2024',
        dataCenter: '$14,510,000,000',
        gaming: '$2,860,000,000',
        proViz: '$416,000,000',
        automotive: '$261,000,000'
      },
      {
        quarter: 'Q2 2024',
        dataCenter: '$10,320,000,000',
        gaming: '$2,490,000,000',
        proViz: '$379,000,000',
        automotive: '$253,000,000'
      }
    ],
    commentary: [
      {
        quarter: 'Q1 2026',
        text: 'Data Center revenue grew 10% Q/Q and 73% Y/Y, driven by strong demand for AI Infrastructure. Gaming hit a record high, up 48% Y/Q and 42% Y/Y, fueled by Blackwell GPU adoption. ProViz was flat Q/Q but up 19% Y/Y. Automotive declined 1% Q/Q but rose 72% Y/Y, boosted by NEV demand.'
      },
      {
        quarter: 'Q4 2025',
        text: 'Data Center revenue rose 16% Q/Q and 93% Y/Y, with Blackwell ramping. Gaming declined 22% Q/Q and 11% Y/Y. ProViz increased 5% Q/Q and 10% Y/Y. Automotive surged 27% Q/Q and 28% Y/Y, driven by strong growth.'
      },
      {
        quarter: 'Q3 2025',
        text: 'Data Center revenue up 17% Q/Q and 112% Y/Y, led by Hopper and H200 ramp. Gaming rose 15% Q/Q and 81% Y/Y. ProViz up 7% Q/Q and 17% Y/Y. Automotive up 30% Q/Q and 72% Y/Y.'
      }
    ],
    sources: [
      {
        company: 'NVIDIA CORP ($NVDA)',
        items: [
          { type: 'May 28, 2025: Q1\'26 Earnings Release', date: '' },
          { type: 'May 28, 2025: Q1\'26 Earnings Transcript', date: '' },
          { type: 'Feb 26, 2025: Q4\'25 Earnings Release', date: '' },
          { type: 'Feb 26, 2025: Q4\'25 Earnings Transcript', date: '' },
          { type: 'Nov 20, 2024: Q3\'25 Earnings Release', date: '' },
          { type: 'Nov 20, 2024: Q3\'25 Earnings Transcript', date: '' },
          { type: 'Aug 28, 2024: Q2\'25 Earnings Release', date: '' },
          { type: 'Aug 28, 2024: Q2\'25 Earnings Transcript', date: '' },
          { type: 'May 22, 2024: Q1\'25 Earnings Release', date: '' },
          { type: 'Feb 21, 2024: Q4\'24 Earnings Release', date: '' },
          { type: 'Nov 21, 2023: Q3\'24 Earnings Release', date: '' },
          { type: 'Aug 23, 2023: Q2\'24 Earnings Release', date: '' }
        ]
      }
    ]
  };

  return (
    <div className="flex flex-col gap-6">
      {/* Query Header */}
      <div className="bg-purple-50 p-4 rounded-lg">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-lg">{query}</h2>
          <div className="flex gap-2">
            <Button variant="ghost" size="sm">
              <ThumbsUp className="h-4 w-4 mr-1" /> 0
            </Button>
            <Button variant="ghost" size="sm">
              <MessageSquare className="h-4 w-4 mr-1" /> 0
            </Button>
            <Button variant="ghost" size="sm">
              <Share2 className="h-4 w-4 mr-1" />
            </Button>
            <Button variant="ghost" size="sm">
              <Bookmark className="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="sm">
              <ExpandIcon className="h-4 w-4" /> Expand View
            </Button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Results Section */}
        <div className="col-span-2 space-y-4">
          <Card className="p-6">
            <div className="mb-4">
              <Badge>ANSWER</Badge>
              <h3 className="mt-2">{queryResults.title}</h3>
            </div>

            {/* Data Table */}
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-3 px-4">Fiscal Quarter</th>
                    <th className="text-right py-3 px-4">Data Center Revenue</th>
                    <th className="text-right py-3 px-4">Gaming Revenue</th>
                    <th className="text-right py-3 px-4">Professional Visualization Revenue</th>
                    <th className="text-right py-3 px-4">Automotive Revenue</th>
                  </tr>
                </thead>
                <tbody>
                  {queryResults.quarters.map((quarter, idx) => (
                    <tr key={idx} className="border-b hover:bg-gray-50">
                      <td className="py-3 px-4">{quarter.quarter}</td>
                      <td className="text-right py-3 px-4">{quarter.dataCenter}</td>
                      <td className="text-right py-3 px-4">{quarter.gaming}</td>
                      <td className="text-right py-3 px-4">{quarter.proViz}</td>
                      <td className="text-right py-3 px-4">{quarter.automotive}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>

          {/* Commentary */}
          <Card className="p-6">
            <h3 className="mb-4">Commentary</h3>
            <div className="space-y-4">
              {queryResults.commentary.map((comment, idx) => (
                <div key={idx} className="pb-4 border-b last:border-b-0">
                  <div className="text-sm text-gray-600 mb-2">{comment.quarter}</div>
                  <p className="text-sm">{comment.text}</p>
                </div>
              ))}
            </div>
          </Card>
        </div>

        {/* Sources Section */}
        <div className="space-y-4">
          <Card className="p-6">
            <div className="flex items-center justify-between mb-4">
              <h3>SOURCES</h3>
              <Badge variant="outline">36</Badge>
            </div>

            <Input placeholder="Search" className="mb-4" />

            <div className="space-y-4">
              {queryResults.sources.map((source, idx) => (
                <div key={idx}>
                  <div className="mb-2">{source.company}</div>
                  <div className="space-y-2">
                    {source.items.map((item, itemIdx) => (
                      <div key={itemIdx} className="flex items-start gap-2 text-sm py-1">
                        <FileText className="h-4 w-4 text-gray-400 mt-0.5 flex-shrink-0" />
                        <span className="text-sm text-gray-700">{item.type}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
