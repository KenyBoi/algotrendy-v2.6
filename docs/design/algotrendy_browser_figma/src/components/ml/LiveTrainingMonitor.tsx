/**
 * Live Training Monitor
 * Real-time training progress visualization
 */

import { useState, useEffect } from 'react';
import { Activity, CheckCircle2, XCircle } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Progress } from '../ui/progress';

interface LiveTrainingMonitorProps {
  jobId: string;
  onComplete: () => void;
}

export default function LiveTrainingMonitor({ jobId, onComplete }: LiveTrainingMonitorProps) {
  const [progress, setProgress] = useState(0);
  const [status, setStatus] = useState<'running' | 'completed' | 'failed'>('running');
  const [currentEpoch, setCurrentEpoch] = useState(0);
  const [totalEpochs] = useState(100);
  const [metrics, setMetrics] = useState({
    accuracy: 0,
    loss: 0,
    f1Score: 0,
  });

  useEffect(() => {
    // Simulate training progress
    const interval = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 100) {
          clearInterval(interval);
          setStatus('completed');
          onComplete();
          return 100;
        }
        return prev + 2;
      });

      setCurrentEpoch((prev) => Math.min(prev + 2, totalEpochs));
      setMetrics({
        accuracy: Math.min(0.95 + Math.random() * 0.05, 1),
        loss: Math.max(0.05 - Math.random() * 0.03, 0.01),
        f1Score: Math.min(0.90 + Math.random() * 0.1, 1),
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [onComplete]);

  return (
    <Card className="bg-slate-900 border-slate-800">
      <CardHeader>
        <CardTitle className="text-white flex items-center gap-2">
          {status === 'running' && <Activity className="w-5 h-5 animate-pulse text-blue-500" />}
          {status === 'completed' && <CheckCircle2 className="w-5 h-5 text-green-500" />}
          {status === 'failed' && <XCircle className="w-5 h-5 text-red-500" />}
          Training Progress
        </CardTitle>
        <CardDescription>
          Job ID: {jobId} • Status: {status}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-gray-400">Epoch {currentEpoch} / {totalEpochs}</span>
            <span className="text-white font-semibold">{progress}%</span>
          </div>
          <Progress value={progress} className="h-2" />
          {status === 'running' && (
            <p className="text-xs text-gray-500">
              ETA: {Math.ceil((totalEpochs - currentEpoch) * 0.5)} seconds
            </p>
          )}
        </div>

        {/* Metrics */}
        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-1">
            <p className="text-xs text-gray-400">Accuracy</p>
            <p className="text-2xl font-bold text-green-500">
              {(metrics.accuracy * 100).toFixed(2)}%
            </p>
          </div>
          <div className="space-y-1">
            <p className="text-xs text-gray-400">Loss</p>
            <p className="text-2xl font-bold text-orange-500">
              {metrics.loss.toFixed(4)}
            </p>
          </div>
          <div className="space-y-1">
            <p className="text-xs text-gray-400">F1-Score</p>
            <p className="text-2xl font-bold text-blue-500">
              {(metrics.f1Score * 100).toFixed(2)}%
            </p>
          </div>
        </div>

        {/* Training Curve Placeholder */}
        <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
          <p className="text-xs text-gray-400 mb-2">Training Curve</p>
          <div className="h-32 flex items-end justify-between gap-1">
            {Array.from({ length: 20 }).map((_, i) => (
              <div
                key={i}
                className="bg-blue-600 rounded-t flex-1 transition-all duration-300"
                style={{
                  height: `${Math.max(20, Math.min(100, 30 + i * 3 + Math.random() * 10))}%`,
                  opacity: i < currentEpoch / 5 ? 1 : 0.2,
                }}
              />
            ))}
          </div>
        </div>

        {/* Completion Message */}
        {status === 'completed' && (
          <div className="bg-green-600/20 border border-green-600/30 rounded-lg p-4">
            <p className="text-green-400 font-semibold flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4" />
              Training completed successfully!
            </p>
            <p className="text-sm text-gray-400 mt-1">
              Final F1-Score: {(metrics.f1Score * 100).toFixed(2)}%
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
