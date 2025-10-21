/**
 * Training Configuration Panel
 * Configure ML training parameters
 */

import { useState } from 'react';
import { Play, Settings2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';

interface TrainingConfigPanelProps {
  onStartTraining: (config: TrainingConfig) => void;
}

export interface TrainingConfig {
  modelType: string;
  symbols: string[];
  days: number;
  interval: string;
  hyperparameters: {
    maxDepth: number;
    learningRate: number;
    nEstimators: number;
  };
}

export default function TrainingConfigPanel({ onStartTraining }: TrainingConfigPanelProps) {
  const [config, setConfig] = useState<TrainingConfig>({
    modelType: 'adaboost',
    symbols: ['BTC-USD', 'ETH-USD', 'BNB-USD', 'XRP-USD', 'SOL-USD', 'ADA-USD'],
    days: 30,
    interval: '5m',
    hyperparameters: {
      maxDepth: 4,
      learningRate: 0.1,
      nEstimators: 100,
    },
  });

  const [showAdvanced, setShowAdvanced] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onStartTraining(config);
  };

  return (
    <Card className="bg-slate-900 border-slate-800">
      <CardHeader>
        <CardTitle className="text-white flex items-center gap-2">
          <Settings2 className="w-5 h-5" />
          Training Configuration
        </CardTitle>
        <CardDescription>
          Configure model architecture, dataset, and hyperparameters
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Model Selection */}
          <div className="space-y-2">
            <Label htmlFor="model-type" className="text-white">Model Type</Label>
            <Select
              value={config.modelType}
              onValueChange={(value) => setConfig({ ...config, modelType: value })}
            >
              <SelectTrigger id="model-type" className="bg-slate-800 border-slate-700 text-white">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="bg-slate-800 border-slate-700">
                <SelectItem value="adaboost">AdaBoost (Recommended)</SelectItem>
                <SelectItem value="gradient_boosting">Gradient Boosting</SelectItem>
                <SelectItem value="random_forest">Random Forest</SelectItem>
                <SelectItem value="lstm">LSTM (Neural Network)</SelectItem>
              </SelectContent>
            </Select>
            <p className="text-xs text-gray-500">
              {config.modelType === 'adaboost' && 'Best F1-score: 99.87%'}
              {config.modelType === 'gradient_boosting' && 'Fast training, good accuracy'}
              {config.modelType === 'random_forest' && 'Robust to overfitting'}
              {config.modelType === 'lstm' && 'Advanced deep learning model'}
            </p>
          </div>

          {/* Dataset Configuration */}
          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-white">Dataset</h3>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="days" className="text-white">Training Period (days)</Label>
                <Input
                  id="days"
                  type="number"
                  value={config.days}
                  onChange={(e) => setConfig({ ...config, days: parseInt(e.target.value) })}
                  className="bg-slate-800 border-slate-700 text-white"
                  min={7}
                  max={365}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="interval" className="text-white">Interval</Label>
                <Select
                  value={config.interval}
                  onValueChange={(value) => setConfig({ ...config, interval: value })}
                >
                  <SelectTrigger id="interval" className="bg-slate-800 border-slate-700 text-white">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-slate-800 border-slate-700">
                    <SelectItem value="1m">1 minute</SelectItem>
                    <SelectItem value="5m">5 minutes</SelectItem>
                    <SelectItem value="15m">15 minutes</SelectItem>
                    <SelectItem value="1h">1 hour</SelectItem>
                    <SelectItem value="1d">1 day</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Symbols */}
            <div className="space-y-2">
              <Label className="text-white">Symbols</Label>
              <div className="flex flex-wrap gap-2">
                {config.symbols.map((symbol) => (
                  <span
                    key={symbol}
                    className="px-3 py-1 bg-blue-600/20 text-blue-400 text-xs rounded-full border border-blue-600/30"
                  >
                    {symbol}
                  </span>
                ))}
              </div>
            </div>
          </div>

          {/* Advanced Settings */}
          <div>
            <Button
              type="button"
              variant="ghost"
              className="text-gray-400 hover:text-white w-full justify-start"
              onClick={() => setShowAdvanced(!showAdvanced)}
            >
              <Settings2 className="w-4 h-4 mr-2" />
              {showAdvanced ? 'Hide' : 'Show'} Advanced Settings
            </Button>

            {showAdvanced && (
              <div className="mt-4 space-y-4 p-4 bg-slate-800/50 rounded-lg border border-slate-700">
                <div className="grid grid-cols-3 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="max-depth" className="text-white text-xs">Max Depth</Label>
                    <Input
                      id="max-depth"
                      type="number"
                      value={config.hyperparameters.maxDepth}
                      onChange={(e) =>
                        setConfig({
                          ...config,
                          hyperparameters: {
                            ...config.hyperparameters,
                            maxDepth: parseInt(e.target.value),
                          },
                        })
                      }
                      className="bg-slate-900 border-slate-600 text-white text-sm"
                      min={1}
                      max={10}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="learning-rate" className="text-white text-xs">Learning Rate</Label>
                    <Input
                      id="learning-rate"
                      type="number"
                      step="0.01"
                      value={config.hyperparameters.learningRate}
                      onChange={(e) =>
                        setConfig({
                          ...config,
                          hyperparameters: {
                            ...config.hyperparameters,
                            learningRate: parseFloat(e.target.value),
                          },
                        })
                      }
                      className="bg-slate-900 border-slate-600 text-white text-sm"
                      min={0.01}
                      max={1}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="n-estimators" className="text-white text-xs">N Estimators</Label>
                    <Input
                      id="n-estimators"
                      type="number"
                      value={config.hyperparameters.nEstimators}
                      onChange={(e) =>
                        setConfig({
                          ...config,
                          hyperparameters: {
                            ...config.hyperparameters,
                            nEstimators: parseInt(e.target.value),
                          },
                        })
                      }
                      className="bg-slate-900 border-slate-600 text-white text-sm"
                      min={10}
                      max={500}
                    />
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Submit Button */}
          <Button type="submit" className="w-full bg-blue-600 hover:bg-blue-700 text-white">
            <Play className="w-4 h-4 mr-2" />
            Start Training
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
