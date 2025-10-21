/**
 * ML Training Control Center
 * Advanced machine learning model training and management
 */

import { useState, useEffect } from 'react';
import { Brain, Play, TrendingUp, Database, Settings2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import TrainingConfigPanel from '../components/ml/TrainingConfigPanel';
import LiveTrainingMonitor from '../components/ml/LiveTrainingMonitor';
import TrainingHistory from '../components/ml/TrainingHistory';

export default function MLTraining() {
  const [isTraining, setIsTraining] = useState(false);
  const [currentJobId, setCurrentJobId] = useState<string | null>(null);

  const handleStartTraining = (config: any) => {
    console.log('Starting training with config:', config);
    setIsTraining(true);
    // TODO: Call API to start training
    // const jobId = await api.startTraining(config);
    // setCurrentJobId(jobId);
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-3 mb-2">
          <Brain className="w-8 h-8 text-blue-500" />
          <h1 className="text-3xl font-bold text-white">ML Training Control Center</h1>
        </div>
        <p className="text-gray-400">
          Configure, train, and monitor machine learning models for trend reversal prediction
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="pb-3">
            <CardDescription className="text-gray-400">Active Models</CardDescription>
            <CardTitle className="text-2xl text-white">3</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-xs text-gray-500">
              <TrendingUp className="w-3 h-3 inline mr-1" />
              AdaBoost, GradBoost, RF
            </div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="pb-3">
            <CardDescription className="text-gray-400">Best F1-Score</CardDescription>
            <CardTitle className="text-2xl text-green-500">99.87%</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-xs text-gray-500">AdaBoost model</div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="pb-3">
            <CardDescription className="text-gray-400">Training Runs</CardDescription>
            <CardTitle className="text-2xl text-white">12</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-xs text-gray-500">Last 30 days</div>
          </CardContent>
        </Card>

        <Card className="bg-slate-900 border-slate-800">
          <CardHeader className="pb-3">
            <CardDescription className="text-gray-400">Dataset Size</CardDescription>
            <CardTitle className="text-2xl text-white">25.9K</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-xs text-gray-500">
              <Database className="w-3 h-3 inline mr-1" />
              51K candles → 25.9K samples
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column: Training Config */}
        <div className="space-y-6">
          <TrainingConfigPanel onStartTraining={handleStartTraining} />
        </div>

        {/* Right Column: Live Monitor + History */}
        <div className="space-y-6">
          {isTraining && currentJobId ? (
            <LiveTrainingMonitor jobId={currentJobId} onComplete={() => setIsTraining(false)} />
          ) : (
            <Card className="bg-slate-900 border-slate-800">
              <CardHeader>
                <CardTitle className="text-white flex items-center gap-2">
                  <Settings2 className="w-5 h-5" />
                  Ready to Train
                </CardTitle>
                <CardDescription>
                  Configure your training parameters and click "Start Training" to begin
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col items-center justify-center py-8 text-gray-500">
                  <Brain className="w-16 h-16 mb-4 opacity-50" />
                  <p className="text-center">
                    Waiting for training configuration...
                  </p>
                </div>
              </CardContent>
            </Card>
          )}

          <TrainingHistory />
        </div>
      </div>
    </div>
  );
}
