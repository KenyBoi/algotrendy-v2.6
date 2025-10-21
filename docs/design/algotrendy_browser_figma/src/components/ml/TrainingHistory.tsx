/**
 * Training History
 * Display past training runs
 */

import { History, CheckCircle2, XCircle, Clock } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';

interface TrainingRun {
  id: string;
  modelType: string;
  trainedAt: string;
  status: 'completed' | 'failed' | 'running';
  f1Score: number;
  accuracy: number;
  duration: number;
}

export default function TrainingHistory() {
  // Mock data - in production, fetch from API
  const trainingRuns: TrainingRun[] = [
    {
      id: '20251020_223413',
      modelType: 'AdaBoost',
      trainedAt: '2025-10-20 22:35:34',
      status: 'completed',
      f1Score: 0.9987,
      accuracy: 0.9998,
      duration: 45,
    },
    {
      id: '20251020_215713',
      modelType: 'AdaBoost',
      trainedAt: '2025-10-20 21:58:33',
      status: 'completed',
      f1Score: 0.9987,
      accuracy: 0.9998,
      duration: 42,
    },
    {
      id: '20251016_234123',
      modelType: 'Gradient Boosting',
      trainedAt: '2025-10-17 00:31:49',
      status: 'completed',
      f1Score: 0.2500,
      accuracy: 0.9985,
      duration: 38,
    },
  ];

  return (
    <Card className="bg-slate-900 border-slate-800">
      <CardHeader>
        <CardTitle className="text-white flex items-center gap-2">
          <History className="w-5 h-5" />
          Training History
        </CardTitle>
        <CardDescription>
          Past {trainingRuns.length} training runs
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          {trainingRuns.map((run) => (
            <div
              key={run.id}
              className="p-4 bg-slate-800/50 rounded-lg border border-slate-700 hover:border-slate-600 transition-colors cursor-pointer"
            >
              <div className="flex items-start justify-between mb-2">
                <div className="flex items-center gap-2">
                  {run.status === 'completed' && <CheckCircle2 className="w-4 h-4 text-green-500" />}
                  {run.status === 'failed' && <XCircle className="w-4 h-4 text-red-500" />}
                  {run.status === 'running' && <Clock className="w-4 h-4 text-blue-500 animate-pulse" />}
                  <div>
                    <p className="text-sm font-semibold text-white">{run.modelType}</p>
                    <p className="text-xs text-gray-500">{run.trainedAt}</p>
                  </div>
                </div>
                <Badge
                  variant={run.status === 'completed' ? 'default' : 'secondary'}
                  className={`
                    ${run.status === 'completed' ? 'bg-green-600/20 text-green-400 border-green-600/30' : ''}
                    ${run.status === 'failed' ? 'bg-red-600/20 text-red-400 border-red-600/30' : ''}
                    ${run.status === 'running' ? 'bg-blue-600/20 text-blue-400 border-blue-600/30' : ''}
                  `}
                >
                  {run.status}
                </Badge>
              </div>

              <div className="grid grid-cols-3 gap-4 mt-3">
                <div>
                  <p className="text-xs text-gray-400">F1-Score</p>
                  <p className="text-sm font-semibold text-white">
                    {(run.f1Score * 100).toFixed(2)}%
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-400">Accuracy</p>
                  <p className="text-sm font-semibold text-white">
                    {(run.accuracy * 100).toFixed(2)}%
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-400">Duration</p>
                  <p className="text-sm font-semibold text-white">
                    {run.duration}s
                  </p>
                </div>
              </div>

              <div className="mt-3 pt-3 border-t border-slate-700">
                <p className="text-xs text-gray-500">
                  ID: {run.id}
                </p>
              </div>
            </div>
          ))}
        </div>

        {trainingRuns.length === 0 && (
          <div className="text-center py-8 text-gray-500">
            <History className="w-12 h-12 mx-auto mb-2 opacity-50" />
            <p>No training runs yet</p>
            <p className="text-xs mt-1">Start your first training to see history</p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
