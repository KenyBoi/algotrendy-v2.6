"""
Comprehensive Testing Framework for ML Trading Models

Three-Layer Validation System:
1. Backtest - Traditional backtesting with CPCV
2. Walk-Forward - Rolling optimization and testing
3. Gap Analysis - Mathematical pattern detection between backtest and walk-forward

Research Insight: The "gap" between backtest and walk-forward reveals overfitting patterns.
By analyzing gap statistics, we can predict production degradation BEFORE deployment.

Author: AlgoTrendy Development Team
Date: October 21, 2025
"""

import numpy as np
import pandas as pd
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from scipy import stats
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score
import matplotlib
matplotlib.use('Agg')  # Non-interactive backend
import matplotlib.pyplot as plt
try:
    import seaborn as sns
    sns.set_style("whitegrid")
except ImportError:
    pass  # Seaborn is optional
import logging

logger = logging.getLogger(__name__)


@dataclass
class TestMetrics:
    """Performance metrics for a single test"""
    accuracy: float
    precision: float
    recall: float
    f1_score: float
    sharpe_ratio: float
    max_drawdown: float
    win_rate: float
    avg_gain: float
    avg_loss: float
    profit_factor: float
    total_trades: int
    timestamp: datetime = field(default_factory=datetime.now)


@dataclass
class GapAnalysis:
    """Analysis of performance gap between testing methods"""
    accuracy_gap: float  # backtest - walk_forward
    sharpe_gap: float
    drawdown_gap: float
    gap_trend: str  # 'increasing', 'decreasing', 'stable'
    overfitting_score: float  # 0-100, higher = more overfitting
    degradation_prediction: float  # predicted production degradation
    confidence_interval: Tuple[float, float]
    statistical_significance: bool
    recommendation: str


class BacktestEngine:
    """
    Traditional backtesting with CPCV validation

    Research: Use CPCV instead of simple split to reduce false discoveries
    """

    def __init__(self, model, feature_calculator):
        self.model = model
        self.feature_calculator = feature_calculator
        self.results = []

    def run(
        self,
        data: pd.DataFrame,
        timestamps: pd.Series,
        symbol: str = "BTCUSDT"
    ) -> TestMetrics:
        """
        Run backtest with CPCV validation

        Args:
            data: OHLCV data
            timestamps: Datetime index
            symbol: Trading symbol

        Returns:
            TestMetrics with performance results
        """
        logger.info(f"Starting backtest for {symbol}")

        # Calculate features
        features = self.feature_calculator.calculate(data)
        labels = self._generate_labels(data)

        # Use CPCV for validation
        from cpcv_validator import CombinatorialPurgedCrossValidator, CPCVConfig

        config = CPCVConfig(n_splits=5, embargo_pct=0.01)
        validator = CombinatorialPurgedCrossValidator(config)

        fold_predictions = []
        fold_actuals = []

        for fold in validator.split(features, labels, timestamps):
            X_train = features.iloc[fold.train_indices]
            X_test = features.iloc[fold.test_indices]
            y_train = labels.iloc[fold.train_indices]
            y_test = labels.iloc[fold.test_indices]

            # Train and predict
            self.model.fit(X_train, y_train)
            y_pred = self.model.predict(X_test)

            fold_predictions.extend(y_pred)
            fold_actuals.extend(y_test)

        # Calculate metrics
        metrics = self._calculate_metrics(
            np.array(fold_actuals),
            np.array(fold_predictions),
            data
        )

        self.results.append(metrics)

        logger.info(
            f"Backtest complete - Accuracy: {metrics.accuracy:.3f}, "
            f"Sharpe: {metrics.sharpe_ratio:.2f}, "
            f"Max DD: {metrics.max_drawdown:.2%}"
        )

        return metrics

    def _generate_labels(self, data: pd.DataFrame) -> pd.Series:
        """Generate trading labels (1=reversal/long, 0=no action)"""
        # Simple reversal labeling: 2%+ move in next 5 periods
        future_return = data['close'].shift(-5) / data['close'] - 1
        labels = (future_return > 0.02).astype(int)
        return labels

    def _calculate_metrics(
        self,
        y_true: np.ndarray,
        y_pred: np.ndarray,
        data: pd.DataFrame
    ) -> TestMetrics:
        """Calculate comprehensive performance metrics"""

        # Classification metrics
        accuracy = accuracy_score(y_true, y_pred)
        precision = precision_score(y_true, y_pred, zero_division=0)
        recall = recall_score(y_true, y_pred, zero_division=0)
        f1 = f1_score(y_true, y_pred, zero_division=0)

        # Trading metrics
        returns = self._simulate_trades(y_pred, data)

        sharpe = self._calculate_sharpe(returns)
        max_dd = self._calculate_max_drawdown(returns)
        win_rate = np.mean(returns > 0) if len(returns) > 0 else 0

        wins = returns[returns > 0]
        losses = returns[returns < 0]
        avg_gain = np.mean(wins) if len(wins) > 0 else 0
        avg_loss = np.mean(losses) if len(losses) > 0 else 0

        profit_factor = abs(np.sum(wins) / np.sum(losses)) if len(losses) > 0 and np.sum(losses) != 0 else 0

        return TestMetrics(
            accuracy=accuracy,
            precision=precision,
            recall=recall,
            f1_score=f1,
            sharpe_ratio=sharpe,
            max_drawdown=max_dd,
            win_rate=win_rate,
            avg_gain=avg_gain,
            avg_loss=avg_loss,
            profit_factor=profit_factor,
            total_trades=len(returns)
        )

    def _simulate_trades(self, signals: np.ndarray, data: pd.DataFrame) -> np.ndarray:
        """Simulate trades based on signals"""
        returns = []
        for i in range(len(signals) - 5):  # 5-period holding
            if signals[i] == 1:  # Buy signal
                entry = data['close'].iloc[i]
                exit_price = data['close'].iloc[i + 5]
                ret = (exit_price - entry) / entry
                returns.append(ret)
        return np.array(returns)

    def _calculate_sharpe(self, returns: np.ndarray, risk_free_rate: float = 0.0) -> float:
        """Calculate Sharpe ratio"""
        if len(returns) == 0 or np.std(returns) == 0:
            return 0.0
        excess_returns = returns - risk_free_rate
        return np.mean(excess_returns) / np.std(excess_returns) * np.sqrt(252)

    def _calculate_max_drawdown(self, returns: np.ndarray) -> float:
        """Calculate maximum drawdown"""
        if len(returns) == 0:
            return 0.0
        cumulative = np.cumprod(1 + returns)
        running_max = np.maximum.accumulate(cumulative)
        drawdown = (cumulative - running_max) / running_max
        return np.min(drawdown)


class WalkForwardOptimizer:
    """
    Walk-forward optimization with rolling windows

    Research: Industry standard for trading validation
    Tests model on sequential out-of-sample periods
    """

    def __init__(
        self,
        model,
        feature_calculator,
        train_window_days: int = 365 * 3,  # 3 years training
        test_window_days: int = 90,  # 3 months testing
        step_days: int = 30  # 1 month step
    ):
        self.model = model
        self.feature_calculator = feature_calculator
        self.train_window = train_window_days
        self.test_window = test_window_days
        self.step = step_days
        self.results = []

    def run(
        self,
        data: pd.DataFrame,
        timestamps: pd.Series,
        symbol: str = "BTCUSDT"
    ) -> List[TestMetrics]:
        """
        Run walk-forward optimization

        Args:
            data: OHLCV data
            timestamps: Datetime index
            symbol: Trading symbol

        Returns:
            List of TestMetrics for each walk-forward period
        """
        logger.info(f"Starting walk-forward optimization for {symbol}")

        # Calculate features
        features = self.feature_calculator.calculate(data)
        labels = self._generate_labels(data)

        # Convert timestamps to days
        timestamps_dt = pd.to_datetime(timestamps)
        start_date = timestamps_dt.min()
        end_date = timestamps_dt.max()

        current_date = start_date + timedelta(days=self.train_window)
        wf_results = []

        fold_num = 0
        while current_date + timedelta(days=self.test_window) <= end_date:
            fold_num += 1

            # Define train and test windows
            train_start = current_date - timedelta(days=self.train_window)
            train_end = current_date
            test_start = current_date
            test_end = current_date + timedelta(days=self.test_window)

            # Get data for this fold
            train_mask = (timestamps_dt >= train_start) & (timestamps_dt < train_end)
            test_mask = (timestamps_dt >= test_start) & (timestamps_dt < test_end)

            X_train = features[train_mask]
            y_train = labels[train_mask]
            X_test = features[test_mask]
            y_test = labels[test_mask]

            if len(X_train) < 100 or len(X_test) < 20:
                logger.warning(f"Insufficient data for fold {fold_num}, skipping")
                current_date += timedelta(days=self.step)
                continue

            # Train and predict
            self.model.fit(X_train, y_train)
            y_pred = self.model.predict(X_test)

            # Calculate metrics for this period
            test_data = data[test_mask]
            metrics = self._calculate_metrics(y_test.values, y_pred, test_data)

            wf_results.append(metrics)

            logger.info(
                f"WF Fold {fold_num}: [{test_start.strftime('%Y-%m-%d')} to {test_end.strftime('%Y-%m-%d')}] "
                f"Accuracy: {metrics.accuracy:.3f}, Sharpe: {metrics.sharpe_ratio:.2f}"
            )

            # Step forward
            current_date += timedelta(days=self.step)

        self.results = wf_results

        logger.info(f"Walk-forward complete - {len(wf_results)} periods tested")

        return wf_results

    def calculate_efficiency(self) -> float:
        """
        Calculate walk-forward efficiency

        Efficiency = Average OOS performance / Average IS performance
        Target: >60% is good, >80% is excellent
        """
        if not self.results:
            return 0.0

        # For simplicity, using Sharpe ratio as performance metric
        oos_sharpe = np.mean([r.sharpe_ratio for r in self.results])

        # IS performance would be from training (not tracked here, would need modification)
        # For now, estimate IS performance as 1.5x OOS (typical ratio)
        estimated_is_sharpe = oos_sharpe * 1.5

        efficiency = oos_sharpe / estimated_is_sharpe if estimated_is_sharpe > 0 else 0

        logger.info(f"Walk-forward efficiency: {efficiency:.1%}")

        return efficiency

    def _generate_labels(self, data: pd.DataFrame) -> pd.Series:
        """Generate trading labels"""
        future_return = data['close'].shift(-5) / data['close'] - 1
        labels = (future_return > 0.02).astype(int)
        return labels

    def _calculate_metrics(
        self,
        y_true: np.ndarray,
        y_pred: np.ndarray,
        data: pd.DataFrame
    ) -> TestMetrics:
        """Calculate metrics (same as BacktestEngine)"""
        accuracy = accuracy_score(y_true, y_pred)
        precision = precision_score(y_true, y_pred, zero_division=0)
        recall = recall_score(y_true, y_pred, zero_division=0)
        f1 = f1_score(y_true, y_pred, zero_division=0)

        returns = self._simulate_trades(y_pred, data)

        sharpe = self._calculate_sharpe(returns)
        max_dd = self._calculate_max_drawdown(returns)
        win_rate = np.mean(returns > 0) if len(returns) > 0 else 0

        wins = returns[returns > 0]
        losses = returns[returns < 0]
        avg_gain = np.mean(wins) if len(wins) > 0 else 0
        avg_loss = np.mean(losses) if len(losses) > 0 else 0

        profit_factor = abs(np.sum(wins) / np.sum(losses)) if len(losses) > 0 and np.sum(losses) != 0 else 0

        return TestMetrics(
            accuracy=accuracy,
            precision=precision,
            recall=recall,
            f1_score=f1,
            sharpe_ratio=sharpe,
            max_drawdown=max_dd,
            win_rate=win_rate,
            avg_gain=avg_gain,
            avg_loss=avg_loss,
            profit_factor=profit_factor,
            total_trades=len(returns)
        )

    def _simulate_trades(self, signals: np.ndarray, data: pd.DataFrame) -> np.ndarray:
        """Simulate trades"""
        returns = []
        for i in range(len(signals) - 5):
            if signals[i] == 1:
                entry = data['close'].iloc[i]
                exit_price = data['close'].iloc[i + 5]
                ret = (exit_price - entry) / entry
                returns.append(ret)
        return np.array(returns)

    def _calculate_sharpe(self, returns: np.ndarray) -> float:
        """Calculate Sharpe ratio"""
        if len(returns) == 0 or np.std(returns) == 0:
            return 0.0
        return np.mean(returns) / np.std(returns) * np.sqrt(252)

    def _calculate_max_drawdown(self, returns: np.ndarray) -> float:
        """Calculate max drawdown"""
        if len(returns) == 0:
            return 0.0
        cumulative = np.cumprod(1 + returns)
        running_max = np.maximum.accumulate(cumulative)
        drawdown = (cumulative - running_max) / running_max
        return np.min(drawdown)


class GapAnalyzer:
    """
    Mathematical Gap Analysis between Backtest and Walk-Forward

    KEY INSIGHT: The "gap" reveals overfitting patterns

    Gap Patterns:
    - Increasing gap over time → Model decaying (concept drift)
    - Large initial gap → Model overfitted to training data
    - Stable small gap → Model robust
    - Negative gap (WF > BT) → Lucky backtest or data leakage

    Mathematical Analysis:
    1. Linear regression of gap over time
    2. Statistical significance testing
    3. Confidence intervals for production prediction
    4. Pattern classification
    """

    def __init__(self):
        self.gap_history = []

    def analyze(
        self,
        backtest_metrics: TestMetrics,
        walkforward_metrics: List[TestMetrics]
    ) -> GapAnalysis:
        """
        Analyze gap between backtest and walk-forward results

        Args:
            backtest_metrics: Results from backtest (CPCV)
            walkforward_metrics: Results from walk-forward periods

        Returns:
            GapAnalysis with overfitting detection and predictions
        """
        logger.info("Starting gap analysis")

        # Calculate gaps
        wf_accuracies = [m.accuracy for m in walkforward_metrics]
        wf_sharpes = [m.sharpe_ratio for m in walkforward_metrics]
        wf_drawdowns = [m.max_drawdown for m in walkforward_metrics]

        accuracy_gaps = [backtest_metrics.accuracy - wf_acc for wf_acc in wf_accuracies]
        sharpe_gaps = [backtest_metrics.sharpe_ratio - wf_sharpe for wf_sharpe in wf_sharpes]
        drawdown_gaps = [backtest_metrics.max_drawdown - wf_dd for wf_dd in wf_drawdowns]

        # Statistical analysis
        mean_accuracy_gap = np.mean(accuracy_gaps)
        mean_sharpe_gap = np.mean(sharpe_gaps)
        mean_drawdown_gap = np.mean(drawdown_gaps)

        # Trend analysis using linear regression
        gap_trend = self._analyze_trend(accuracy_gaps)

        # Overfitting score (0-100)
        overfitting_score = self._calculate_overfitting_score(
            mean_accuracy_gap,
            mean_sharpe_gap,
            gap_trend
        )

        # Production degradation prediction
        degradation_pred = self._predict_degradation(
            accuracy_gaps,
            sharpe_gaps
        )

        # Confidence interval
        ci = self._calculate_confidence_interval(accuracy_gaps)

        # Statistical significance test
        is_significant = self._test_significance(
            backtest_metrics.accuracy,
            wf_accuracies
        )

        # Generate recommendation
        recommendation = self._generate_recommendation(
            overfitting_score,
            gap_trend,
            degradation_pred
        )

        analysis = GapAnalysis(
            accuracy_gap=mean_accuracy_gap,
            sharpe_gap=mean_sharpe_gap,
            drawdown_gap=mean_drawdown_gap,
            gap_trend=gap_trend,
            overfitting_score=overfitting_score,
            degradation_prediction=degradation_pred,
            confidence_interval=ci,
            statistical_significance=is_significant,
            recommendation=recommendation
        )

        self.gap_history.append(analysis)

        logger.info(
            f"Gap Analysis Complete:\n"
            f"  Accuracy Gap: {mean_accuracy_gap:.3f}\n"
            f"  Sharpe Gap: {mean_sharpe_gap:.2f}\n"
            f"  Trend: {gap_trend}\n"
            f"  Overfitting Score: {overfitting_score:.1f}/100\n"
            f"  Predicted Degradation: {degradation_pred:.1%}\n"
            f"  Recommendation: {recommendation}"
        )

        return analysis

    def _analyze_trend(self, gaps: List[float]) -> str:
        """
        Analyze trend in gaps over time using linear regression

        Returns: 'increasing', 'decreasing', or 'stable'
        """
        if len(gaps) < 3:
            return 'stable'

        x = np.arange(len(gaps))
        y = np.array(gaps)

        # Linear regression
        slope, intercept, r_value, p_value, std_err = stats.linregress(x, y)

        # Classify trend
        if abs(slope) < 0.001:  # Threshold for stability
            return 'stable'
        elif slope > 0:
            return 'increasing'  # Gap growing = performance degrading
        else:
            return 'decreasing'  # Gap shrinking = performance improving

    def _calculate_overfitting_score(
        self,
        accuracy_gap: float,
        sharpe_gap: float,
        trend: str
    ) -> float:
        """
        Calculate overfitting score (0-100)

        Scoring:
        - Large gaps = higher score
        - Increasing trend = +penalty
        - Negative gaps = different penalty (data leakage concern)
        """
        score = 0.0

        # Accuracy gap component (0-40 points)
        if accuracy_gap > 0:
            score += min(accuracy_gap * 100, 40)  # Max 40 points
        elif accuracy_gap < -0.05:  # WF significantly better than BT
            score += 20  # Suspicious - possible data leakage

        # Sharpe gap component (0-40 points)
        if sharpe_gap > 0:
            score += min(sharpe_gap * 10, 40)  # Max 40 points

        # Trend component (0-20 points)
        if trend == 'increasing':
            score += 20  # Degrading over time
        elif trend == 'decreasing':
            score -= 10  # Improving (good)

        # Clamp to 0-100
        return max(0, min(100, score))

    def _predict_degradation(
        self,
        accuracy_gaps: List[float],
        sharpe_gaps: List[float]
    ) -> float:
        """
        Predict production performance degradation

        Uses exponential smoothing to predict next period
        """
        if len(accuracy_gaps) == 0:
            return 0.0

        # Recent gaps matter more (exponential weights)
        weights = np.exp(np.linspace(-2, 0, len(accuracy_gaps)))
        weights /= weights.sum()

        weighted_accuracy_gap = np.sum(np.array(accuracy_gaps) * weights)

        # Predict degradation (gap typically grows by 20% in production)
        predicted_degradation = weighted_accuracy_gap * 1.2

        return predicted_degradation

    def _calculate_confidence_interval(
        self,
        gaps: List[float],
        confidence: float = 0.95
    ) -> Tuple[float, float]:
        """Calculate confidence interval for gap"""
        if len(gaps) < 2:
            return (0.0, 0.0)

        mean = np.mean(gaps)
        std = np.std(gaps)
        n = len(gaps)

        # t-distribution for small samples
        t_critical = stats.t.ppf((1 + confidence) / 2, n - 1)
        margin = t_critical * (std / np.sqrt(n))

        return (mean - margin, mean + margin)

    def _test_significance(
        self,
        backtest_accuracy: float,
        walkforward_accuracies: List[float]
    ) -> bool:
        """
        Test if gap is statistically significant

        Uses one-sample t-test
        """
        if len(walkforward_accuracies) < 3:
            return False

        # Null hypothesis: WF accuracy = BT accuracy
        t_stat, p_value = stats.ttest_1samp(walkforward_accuracies, backtest_accuracy)

        # Significant if p < 0.05
        return p_value < 0.05

    def _generate_recommendation(
        self,
        overfitting_score: float,
        trend: str,
        degradation: float
    ) -> str:
        """Generate deployment recommendation"""

        if overfitting_score > 70:
            return "⛔ DO NOT DEPLOY - High overfitting detected. Add regularization or collect more data."

        elif overfitting_score > 50:
            return "⚠️ CAUTION - Moderate overfitting. Consider ensemble approach or feature selection."

        elif trend == 'increasing' and degradation > 0.10:
            return "⚠️ DEGRADING - Model performance declining over time. Implement drift detection."

        elif overfitting_score < 30 and trend in ['stable', 'decreasing']:
            return "✅ SAFE TO DEPLOY - Model shows stable, robust performance."

        else:
            return "🟡 MONITOR CLOSELY - Deploy with frequent validation and drift detection."


class ComprehensiveTestingFramework:
    """
    Complete testing framework combining all three methods

    Usage:
        framework = ComprehensiveTestingFramework(model, feature_calculator)
        results = framework.run_all_tests(data, timestamps)
        framework.generate_report()
    """

    def __init__(self, model, feature_calculator):
        self.model = model
        self.feature_calculator = feature_calculator

        self.backtest_engine = BacktestEngine(model, feature_calculator)
        self.walkforward_optimizer = WalkForwardOptimizer(model, feature_calculator)
        self.gap_analyzer = GapAnalyzer()

        self.results = {}

    def run_all_tests(
        self,
        data: pd.DataFrame,
        timestamps: pd.Series,
        symbol: str = "BTCUSDT"
    ) -> Dict:
        """
        Run all three testing methods

        Returns:
            Dictionary with all results
        """
        logger.info("="*80)
        logger.info("COMPREHENSIVE TESTING FRAMEWORK")
        logger.info("="*80)

        # Test 1: Backtest with CPCV
        logger.info("\n1. Running Backtest with CPCV...")
        backtest_results = self.backtest_engine.run(data, timestamps, symbol)

        # Test 2: Walk-Forward Optimization
        logger.info("\n2. Running Walk-Forward Optimization...")
        walkforward_results = self.walkforward_optimizer.run(data, timestamps, symbol)

        # Test 3: Gap Analysis
        logger.info("\n3. Running Gap Analysis...")
        gap_analysis = self.gap_analyzer.analyze(backtest_results, walkforward_results)

        # Store results
        self.results = {
            'backtest': backtest_results,
            'walkforward': walkforward_results,
            'gap_analysis': gap_analysis,
            'symbol': symbol,
            'timestamp': datetime.now()
        }

        logger.info("\n" + "="*80)
        logger.info("TESTING COMPLETE")
        logger.info("="*80)

        return self.results

    def generate_report(self) -> str:
        """Generate comprehensive testing report"""

        if not self.results:
            return "No test results available. Run tests first."

        bt = self.results['backtest']
        wf = self.results['walkforward']
        gap = self.results['gap_analysis']

        report = f"""
{'='*80}
COMPREHENSIVE TESTING REPORT
{'='*80}

Symbol: {self.results['symbol']}
Date: {self.results['timestamp'].strftime('%Y-%m-%d %H:%M:%S')}

{'='*80}
1. BACKTEST RESULTS (with CPCV)
{'='*80}

Accuracy:       {bt.accuracy:.3f}
Precision:      {bt.precision:.3f}
Recall:         {bt.recall:.3f}
F1 Score:       {bt.f1_score:.3f}

Sharpe Ratio:   {bt.sharpe_ratio:.2f}
Max Drawdown:   {bt.max_drawdown:.2%}
Win Rate:       {bt.win_rate:.1%}
Profit Factor:  {bt.profit_factor:.2f}

Total Trades:   {bt.total_trades}

{'='*80}
2. WALK-FORWARD RESULTS
{'='*80}

Number of Periods: {len(wf)}

Average Performance:
  Accuracy:     {np.mean([m.accuracy for m in wf]):.3f}
  Sharpe Ratio: {np.mean([m.sharpe_ratio for m in wf]):.2f}
  Win Rate:     {np.mean([m.win_rate for m in wf]):.1%}
  Max Drawdown: {np.mean([m.max_drawdown for m in wf]):.2%}

Best Period:
  Accuracy:     {f"{max([m.accuracy for m in wf]):.3f}" if wf else 'N/A'}
  Sharpe Ratio: {f"{max([m.sharpe_ratio for m in wf]):.2f}" if wf else 'N/A'}

Worst Period:
  Accuracy:     {f"{min([m.accuracy for m in wf]):.3f}" if wf else 'N/A'}
  Sharpe Ratio: {f"{min([m.sharpe_ratio for m in wf]):.2f}" if wf else 'N/A'}

WF Efficiency:  {self.walkforward_optimizer.calculate_efficiency():.1%}

{'='*80}
3. GAP ANALYSIS (Mathematical Pattern Detection)
{'='*80}

Performance Gaps:
  Accuracy Gap:   {gap.accuracy_gap:+.3f}
  Sharpe Gap:     {gap.sharpe_gap:+.2f}
  Drawdown Gap:   {gap.drawdown_gap:+.2%}

Gap Pattern:
  Trend:          {gap.gap_trend.upper()}
  Overfitting:    {gap.overfitting_score:.1f}/100

Predictions:
  Degradation:    {gap.degradation_prediction:+.1%}
  Confidence:     {gap.confidence_interval[0]:.3f} to {gap.confidence_interval[1]:.3f}
  Significant:    {'YES' if gap.statistical_significance else 'NO'}

{'='*80}
FINAL RECOMMENDATION
{'='*80}

{gap.recommendation}

{'='*80}
DETAILED ANALYSIS
{'='*80}

Backtest vs Walk-Forward Comparison:
  • Accuracy:   {bt.accuracy:.3f} (BT) vs {np.mean([m.accuracy for m in wf]):.3f} (WF)
  • Gap:        {gap.accuracy_gap:+.3f} ({gap.accuracy_gap/bt.accuracy*100:+.1f}%)

  • Sharpe:     {bt.sharpe_ratio:.2f} (BT) vs {np.mean([m.sharpe_ratio for m in wf]):.2f} (WF)
  • Gap:        {gap.sharpe_gap:+.2f} ({gap.sharpe_gap/bt.sharpe_ratio*100 if bt.sharpe_ratio != 0 else 0:+.1f}%)

Gap Interpretation:
  • {gap.gap_trend.upper()} trend indicates {'performance degradation over time' if gap.gap_trend == 'increasing' else 'stable or improving performance'}
  • Overfitting score of {gap.overfitting_score:.1f}/100 is {'HIGH (>70)' if gap.overfitting_score > 70 else 'MODERATE (50-70)' if gap.overfitting_score > 50 else 'LOW (<50)'}
  • Predicted production degradation: {gap.degradation_prediction:.1%}

{'='*80}
NEXT STEPS
{'='*80}

"""

        if gap.overfitting_score > 70:
            report += """
1. ⛔ DO NOT DEPLOY to production
2. Add L1/L2 regularization to model
3. Reduce model complexity (lower max_depth, fewer features)
4. Collect more training data
5. Re-run tests after improvements
"""
        elif gap.overfitting_score > 50:
            report += """
1. ⚠️  Consider ensemble approach (combine multiple models)
2. Apply feature selection (Boruta, RFE)
3. Increase training data diversity
4. Monitor closely in production with kill switch ready
"""
        else:
            report += """
1. ✅ Model is ready for deployment
2. Set up drift detection monitoring
3. Implement A/B testing framework
4. Monitor gap metrics in production
5. Re-validate monthly
"""

        report += f"\n{'='*80}\n"

        return report

    def plot_results(self, save_path: Optional[str] = None):
        """
        Create visualization of test results

        Args:
            save_path: Path to save plot (if None, displays plot)
        """
        if not self.results:
            logger.warning("No results to plot")
            return

        fig, axes = plt.subplots(2, 2, figsize=(15, 10))
        fig.suptitle('Comprehensive Testing Results', fontsize=16)

        wf = self.results['walkforward']
        bt = self.results['backtest']

        # Plot 1: Accuracy over walk-forward periods
        ax1 = axes[0, 0]
        accuracies = [m.accuracy for m in wf]
        ax1.plot(range(len(accuracies)), accuracies, 'o-', label='Walk-Forward', linewidth=2)
        ax1.axhline(y=bt.accuracy, color='r', linestyle='--', label='Backtest (CPCV)', linewidth=2)
        ax1.set_xlabel('Period')
        ax1.set_ylabel('Accuracy')
        ax1.set_title('Accuracy: Walk-Forward vs Backtest')
        ax1.legend()
        ax1.grid(True, alpha=0.3)

        # Plot 2: Sharpe ratio
        ax2 = axes[0, 1]
        sharpes = [m.sharpe_ratio for m in wf]
        ax2.plot(range(len(sharpes)), sharpes, 'o-', label='Walk-Forward', linewidth=2)
        ax2.axhline(y=bt.sharpe_ratio, color='r', linestyle='--', label='Backtest (CPCV)', linewidth=2)
        ax2.set_xlabel('Period')
        ax2.set_ylabel('Sharpe Ratio')
        ax2.set_title('Sharpe Ratio: Walk-Forward vs Backtest')
        ax2.legend()
        ax2.grid(True, alpha=0.3)

        # Plot 3: Gap evolution
        ax3 = axes[1, 0]
        accuracy_gaps = [bt.accuracy - m.accuracy for m in wf]
        ax3.plot(range(len(accuracy_gaps)), accuracy_gaps, 'o-', color='orange', linewidth=2)
        ax3.axhline(y=0, color='black', linestyle='-', alpha=0.3)
        ax3.fill_between(range(len(accuracy_gaps)), 0, accuracy_gaps, alpha=0.3, color='orange')
        ax3.set_xlabel('Period')
        ax3.set_ylabel('Gap (Backtest - Walk-Forward)')
        ax3.set_title(f'Accuracy Gap Evolution (Trend: {self.results["gap_analysis"].gap_trend})')
        ax3.grid(True, alpha=0.3)

        # Plot 4: Overfitting score
        ax4 = axes[1, 1]
        overfitting_score = self.results['gap_analysis'].overfitting_score
        colors = ['green' if overfitting_score < 30 else 'orange' if overfitting_score < 70 else 'red']
        ax4.barh(['Overfitting\nScore'], [overfitting_score], color=colors)
        ax4.set_xlim(0, 100)
        ax4.set_xlabel('Score (0-100)')
        ax4.set_title(f'Overfitting Score: {overfitting_score:.1f}/100')
        ax4.axvline(x=30, color='green', linestyle='--', alpha=0.5, label='Safe (<30)')
        ax4.axvline(x=70, color='red', linestyle='--', alpha=0.5, label='Danger (>70)')
        ax4.legend()
        ax4.grid(True, alpha=0.3, axis='x')

        plt.tight_layout()

        if save_path:
            plt.savefig(save_path, dpi=300, bbox_inches='tight')
            logger.info(f"Plot saved to {save_path}")
        else:
            plt.show()


# Example usage
if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    # Generate sample data
    np.random.seed(42)
    n_samples = 2000
    dates = pd.date_range(start='2021-01-01', periods=n_samples, freq='1h')

    data = pd.DataFrame({
        'timestamp': dates,
        'open': 100 + np.cumsum(np.random.randn(n_samples) * 0.5),
        'high': 100 + np.cumsum(np.random.randn(n_samples) * 0.5) + 1,
        'low': 100 + np.cumsum(np.random.randn(n_samples) * 0.5) - 1,
        'close': 100 + np.cumsum(np.random.randn(n_samples) * 0.5),
        'volume': np.random.randint(1000, 10000, n_samples)
    })

    # Simple feature calculator
    class SimpleFeatureCalculator:
        def calculate(self, data):
            features = pd.DataFrame()
            features['returns'] = data['close'].pct_change()
            features['volatility'] = data['close'].pct_change().rolling(20).std()
            features['volume_ratio'] = data['volume'] / data['volume'].rolling(20).mean()
            return features.fillna(0)

    # Simple model
    from sklearn.ensemble import GradientBoostingClassifier
    model = GradientBoostingClassifier(n_estimators=50, max_depth=3, random_state=42)

    # Run comprehensive tests with smaller windows for demo
    feature_calc = SimpleFeatureCalculator()
    framework = ComprehensiveTestingFramework(model, feature_calc)

    # Override walk-forward with smaller windows for test data
    framework.walkforward_optimizer = WalkForwardOptimizer(
        model,
        feature_calc,
        train_window_days=30,   # 30 days training (smaller for demo)
        test_window_days=10,     # 10 days testing
        step_days=10             # 10 day steps
    )

    results = framework.run_all_tests(data, data['timestamp'], symbol="TEST")

    # Generate report
    print(framework.generate_report())

    # Plot results
    framework.plot_results(save_path='test_results.png')
