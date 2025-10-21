#!/usr/bin/env python3
"""
ML Visualization Module - Plotly-based Interactive Visualizations
Generates charts for overfitting detection, model comparison, and performance analysis
"""

import warnings
warnings.filterwarnings('ignore')

import numpy as np
import json
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from datetime import datetime

# Plotly
try:
    import plotly.graph_objects as go
    import plotly.express as px
    from plotly.subplots import make_subplots
    PLOTLY_AVAILABLE = True
except ImportError:
    PLOTLY_AVAILABLE = False
    print("⚠️  Plotly not available")

# ML metrics
try:
    from sklearn.metrics import (
        confusion_matrix, roc_curve, auc,
        precision_recall_curve, average_precision_score
    )
    SKLEARN_AVAILABLE = True
except ImportError:
    SKLEARN_AVAILABLE = False
    print("⚠️  scikit-learn not available")


class MLVisualizer:
    """Generate interactive Plotly visualizations for ML models"""

    @staticmethod
    def create_learning_curves(train_sizes, train_scores, val_scores, title="Learning Curves"):
        """
        Create interactive learning curves plot

        Args:
            train_sizes: Array of training set sizes
            train_scores: Training scores for each size
            val_scores: Validation scores for each size
            title: Plot title

        Returns:
            Plotly Figure object (JSON serializable)
        """
        if not PLOTLY_AVAILABLE:
            return {"error": "Plotly not available"}

        # Calculate means and stds if multiple runs
        if len(train_scores.shape) > 1:
            train_mean = np.mean(train_scores, axis=1)
            train_std = np.std(train_scores, axis=1)
            val_mean = np.mean(val_scores, axis=1)
            val_std = np.std(val_scores, axis=1)
        else:
            train_mean = train_scores
            train_std = np.zeros_like(train_scores)
            val_mean = val_scores
            val_std = np.zeros_like(val_scores)

        fig = go.Figure()

        # Training score with confidence interval
        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=train_mean + train_std,
            mode='lines',
            line=dict(width=0),
            showlegend=False,
            hoverinfo='skip'
        ))

        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=train_mean - train_std,
            mode='lines',
            line=dict(width=0),
            fillcolor='rgba(0, 100, 255, 0.2)',
            fill='tonexty',
            showlegend=False,
            hoverinfo='skip'
        ))

        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=train_mean,
            mode='lines+markers',
            name='Training Score',
            line=dict(color='rgb(0, 100, 255)', width=2),
            marker=dict(size=8)
        ))

        # Validation score with confidence interval
        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=val_mean + val_std,
            mode='lines',
            line=dict(width=0),
            showlegend=False,
            hoverinfo='skip'
        ))

        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=val_mean - val_std,
            mode='lines',
            line=dict(width=0),
            fillcolor='rgba(255, 100, 0, 0.2)',
            fill='tonexty',
            showlegend=False,
            hoverinfo='skip'
        ))

        fig.add_trace(go.Scatter(
            x=train_sizes,
            y=val_mean,
            mode='lines+markers',
            name='Validation Score',
            line=dict(color='rgb(255, 100, 0)', width=2),
            marker=dict(size=8)
        ))

        # Calculate overfitting gap
        gap = train_mean - val_mean
        max_gap = np.max(gap)

        # Overfitting warning annotation
        if max_gap > 0.05:
            fig.add_annotation(
                x=train_sizes[-1],
                y=val_mean[-1],
                text=f"⚠️ Overfitting Gap: {max_gap:.3f}",
                showarrow=True,
                arrowhead=2,
                bgcolor="rgba(255, 200, 0, 0.8)",
                bordercolor="orange",
                borderwidth=2
            )

        fig.update_layout(
            title=title,
            xaxis_title="Training Set Size",
            yaxis_title="Accuracy Score",
            hovermode='x unified',
            template='plotly_white',
            height=500,
            showlegend=True,
            legend=dict(
                orientation="h",
                yanchor="bottom",
                y=1.02,
                xanchor="right",
                x=1
            )
        )

        return fig.to_json()

    @staticmethod
    def create_roc_curve(y_true, y_pred_proba, title="ROC Curve"):
        """
        Create interactive ROC curve

        Args:
            y_true: True labels
            y_pred_proba: Predicted probabilities
            title: Plot title

        Returns:
            Plotly Figure object (JSON serializable)
        """
        if not PLOTLY_AVAILABLE or not SKLEARN_AVAILABLE:
            return {"error": "Required libraries not available"}

        fpr, tpr, thresholds = roc_curve(y_true, y_pred_proba)
        roc_auc = auc(fpr, tpr)

        fig = go.Figure()

        # ROC curve
        fig.add_trace(go.Scatter(
            x=fpr,
            y=tpr,
            mode='lines',
            name=f'ROC (AUC = {roc_auc:.3f})',
            line=dict(color='blue', width=3),
            hovertemplate='<b>FPR</b>: %{x:.3f}<br><b>TPR</b>: %{y:.3f}<extra></extra>'
        ))

        # Random classifier line
        fig.add_trace(go.Scatter(
            x=[0, 1],
            y=[0, 1],
            mode='lines',
            name='Random Classifier',
            line=dict(color='red', width=2, dash='dash')
        ))

        # Optimal threshold point (closest to top-left)
        optimal_idx = np.argmax(tpr - fpr)
        optimal_threshold = thresholds[optimal_idx]

        fig.add_trace(go.Scatter(
            x=[fpr[optimal_idx]],
            y=[tpr[optimal_idx]],
            mode='markers',
            name=f'Optimal Threshold ({optimal_threshold:.3f})',
            marker=dict(color='green', size=12, symbol='star')
        ))

        fig.update_layout(
            title=title,
            xaxis_title="False Positive Rate",
            yaxis_title="True Positive Rate",
            hovermode='closest',
            template='plotly_white',
            height=500,
            showlegend=True
        )

        return fig.to_json()

    @staticmethod
    def create_precision_recall_curve(y_true, y_pred_proba, title="Precision-Recall Curve"):
        """Create interactive Precision-Recall curve"""
        if not PLOTLY_AVAILABLE or not SKLEARN_AVAILABLE:
            return {"error": "Required libraries not available"}

        precision, recall, thresholds = precision_recall_curve(y_true, y_pred_proba)
        avg_precision = average_precision_score(y_true, y_pred_proba)

        fig = go.Figure()

        fig.add_trace(go.Scatter(
            x=recall,
            y=precision,
            mode='lines',
            name=f'PR (AP = {avg_precision:.3f})',
            line=dict(color='purple', width=3),
            fill='tozeroy',
            fillcolor='rgba(128, 0, 128, 0.2)'
        ))

        fig.update_layout(
            title=title,
            xaxis_title="Recall",
            yaxis_title="Precision",
            hovermode='closest',
            template='plotly_white',
            height=500
        )

        return fig.to_json()

    @staticmethod
    def create_confusion_matrix(y_true, y_pred, labels=None, title="Confusion Matrix"):
        """Create interactive confusion matrix heatmap"""
        if not PLOTLY_AVAILABLE or not SKLEARN_AVAILABLE:
            return {"error": "Required libraries not available"}

        cm = confusion_matrix(y_true, y_pred)

        if labels is None:
            labels = ['Negative', 'Positive']

        # Calculate percentages
        cm_pct = cm.astype('float') / cm.sum(axis=1)[:, np.newaxis] * 100

        # Create annotations
        annotations = []
        for i in range(len(labels)):
            for j in range(len(labels)):
                annotations.append(
                    dict(
                        x=j,
                        y=i,
                        text=f"{cm[i, j]}<br>({cm_pct[i, j]:.1f}%)",
                        showarrow=False,
                        font=dict(color='white' if cm[i, j] > cm.max() / 2 else 'black')
                    )
                )

        fig = go.Figure(data=go.Heatmap(
            z=cm,
            x=labels,
            y=labels,
            colorscale='Blues',
            showscale=True,
            hovertemplate='Predicted: %{x}<br>Actual: %{y}<br>Count: %{z}<extra></extra>'
        ))

        fig.update_layout(
            title=title,
            xaxis_title="Predicted Label",
            yaxis_title="True Label",
            template='plotly_white',
            height=500,
            annotations=annotations
        )

        return fig.to_json()

    @staticmethod
    def create_feature_importance(feature_names, importances, top_n=20, title="Feature Importance"):
        """Create interactive feature importance bar chart"""
        if not PLOTLY_AVAILABLE:
            return {"error": "Plotly not available"}

        # Sort by importance
        indices = np.argsort(importances)[::-1][:top_n]
        sorted_features = [feature_names[i] for i in indices]
        sorted_importances = importances[indices]

        fig = go.Figure()

        fig.add_trace(go.Bar(
            x=sorted_importances,
            y=sorted_features,
            orientation='h',
            marker=dict(
                color=sorted_importances,
                colorscale='Viridis',
                showscale=True,
                colorbar=dict(title="Importance")
            ),
            hovertemplate='<b>%{y}</b><br>Importance: %{x:.4f}<extra></extra>'
        ))

        fig.update_layout(
            title=title,
            xaxis_title="Importance Score",
            yaxis_title="Feature",
            template='plotly_white',
            height=max(400, top_n * 25),
            yaxis=dict(autorange="reversed")
        )

        return fig.to_json()

    @staticmethod
    def create_training_history(history_dict, title="Training History"):
        """Create interactive training history plot (for LSTM/neural networks)"""
        if not PLOTLY_AVAILABLE:
            return {"error": "Plotly not available"}

        fig = make_subplots(
            rows=2, cols=1,
            subplot_titles=('Accuracy', 'Loss'),
            vertical_spacing=0.12
        )

        epochs = list(range(1, len(history_dict['accuracy']) + 1))

        # Accuracy plot
        fig.add_trace(
            go.Scatter(x=epochs, y=history_dict['accuracy'],
                      mode='lines+markers', name='Train Accuracy',
                      line=dict(color='blue')),
            row=1, col=1
        )
        fig.add_trace(
            go.Scatter(x=epochs, y=history_dict['val_accuracy'],
                      mode='lines+markers', name='Val Accuracy',
                      line=dict(color='orange')),
            row=1, col=1
        )

        # Loss plot
        fig.add_trace(
            go.Scatter(x=epochs, y=history_dict['loss'],
                      mode='lines+markers', name='Train Loss',
                      line=dict(color='blue'), showlegend=False),
            row=2, col=1
        )
        fig.add_trace(
            go.Scatter(x=epochs, y=history_dict['val_loss'],
                      mode='lines+markers', name='Val Loss',
                      line=dict(color='orange'), showlegend=False),
            row=2, col=1
        )

        # Detect overfitting
        final_gap = history_dict['accuracy'][-1] - history_dict['val_accuracy'][-1]
        if final_gap > 0.05:
            # Add annotation for overfitting
            fig.add_annotation(
                x=epochs[-1],
                y=history_dict['val_accuracy'][-1],
                text=f"⚠️ Gap: {final_gap:.3f}",
                showarrow=True,
                row=1, col=1,
                bgcolor="rgba(255, 200, 0, 0.8)"
            )

        fig.update_xaxes(title_text="Epoch", row=2, col=1)
        fig.update_yaxes(title_text="Accuracy", row=1, col=1)
        fig.update_yaxes(title_text="Loss", row=2, col=1)

        fig.update_layout(
            title_text=title,
            template='plotly_white',
            height=700,
            hovermode='x unified'
        )

        return fig.to_json()

    @staticmethod
    def create_model_comparison(models_metrics: Dict[str, Dict], title="Model Comparison"):
        """Create interactive model comparison chart"""
        if not PLOTLY_AVAILABLE:
            return {"error": "Plotly not available"}

        model_names = list(models_metrics.keys())
        metrics = ['accuracy', 'precision', 'recall', 'f1_score']

        fig = go.Figure()

        for metric in metrics:
            values = [models_metrics[model].get(metric, 0) for model in model_names]
            fig.add_trace(go.Bar(
                name=metric.replace('_', ' ').title(),
                x=model_names,
                y=values,
                text=[f'{v:.3f}' for v in values],
                textposition='auto',
            ))

        fig.update_layout(
            title=title,
            xaxis_title="Model",
            yaxis_title="Score",
            barmode='group',
            template='plotly_white',
            height=500,
            yaxis=dict(range=[0, 1.1])
        )

        return fig.to_json()

    @staticmethod
    def create_overfitting_dashboard(
        learning_curve_data,
        train_scores,
        val_scores,
        y_true,
        y_pred,
        y_pred_proba
    ):
        """Create comprehensive overfitting detection dashboard"""
        if not PLOTLY_AVAILABLE:
            return {"error": "Plotly not available"}

        fig = make_subplots(
            rows=2, cols=2,
            subplot_titles=(
                'Learning Curves',
                'ROC Curve',
                'Confusion Matrix',
                'Overfitting Metrics'
            ),
            specs=[
                [{"type": "scatter"}, {"type": "scatter"}],
                [{"type": "heatmap"}, {"type": "indicator"}]
            ]
        )

        # Learning curves (top-left)
        train_sizes = learning_curve_data['train_sizes']
        train_mean = learning_curve_data['train_mean']
        val_mean = learning_curve_data['val_mean']

        fig.add_trace(
            go.Scatter(x=train_sizes, y=train_mean, name='Train',
                      line=dict(color='blue')),
            row=1, col=1
        )
        fig.add_trace(
            go.Scatter(x=train_sizes, y=val_mean, name='Val',
                      line=dict(color='orange')),
            row=1, col=1
        )

        # ROC curve (top-right)
        fpr, tpr, _ = roc_curve(y_true, y_pred_proba)
        roc_auc = auc(fpr, tpr)

        fig.add_trace(
            go.Scatter(x=fpr, y=tpr, name=f'ROC (AUC={roc_auc:.3f})',
                      line=dict(color='green')),
            row=1, col=2
        )
        fig.add_trace(
            go.Scatter(x=[0, 1], y=[0, 1], name='Random',
                      line=dict(color='red', dash='dash')),
            row=1, col=2
        )

        # Confusion matrix (bottom-left)
        cm = confusion_matrix(y_true, y_pred)
        fig.add_trace(
            go.Heatmap(z=cm, colorscale='Blues', showscale=False),
            row=2, col=1
        )

        # Overfitting gauge (bottom-right)
        train_acc = train_scores.mean()
        val_acc = val_scores.mean()
        gap = train_acc - val_acc
        overfitting_score = min(gap * 100, 100)

        fig.add_trace(
            go.Indicator(
                mode="gauge+number+delta",
                value=overfitting_score,
                title={'text': "Overfitting Score"},
                delta={'reference': 5, 'increasing': {'color': "red"}},
                gauge={
                    'axis': {'range': [0, 100]},
                    'bar': {'color': "darkblue"},
                    'steps': [
                        {'range': [0, 5], 'color': "lightgreen"},
                        {'range': [5, 15], 'color': "yellow"},
                        {'range': [15, 100], 'color': "red"}
                    ],
                    'threshold': {
                        'line': {'color': "red", 'width': 4},
                        'thickness': 0.75,
                        'value': 15
                    }
                }
            ),
            row=2, col=2
        )

        fig.update_layout(
            title_text="Overfitting Detection Dashboard",
            template='plotly_white',
            height=800,
            showlegend=True
        )

        return fig.to_json()


# Standalone functions for easy import
def plot_learning_curves(train_sizes, train_scores, val_scores):
    """Quick learning curves plot"""
    return MLVisualizer.create_learning_curves(train_sizes, train_scores, val_scores)

def plot_roc_curve(y_true, y_pred_proba):
    """Quick ROC curve plot"""
    return MLVisualizer.create_roc_curve(y_true, y_pred_proba)

def plot_confusion_matrix(y_true, y_pred):
    """Quick confusion matrix plot"""
    return MLVisualizer.create_confusion_matrix(y_true, y_pred)

def plot_feature_importance(feature_names, importances):
    """Quick feature importance plot"""
    return MLVisualizer.create_feature_importance(feature_names, importances)


if __name__ == "__main__":
    # Demo/Test
    print("="*70)
    print("ML Visualizations Module")
    print("="*70)
    print("\nAvailable visualizations:")
    print("1. Learning Curves - detect overfitting via train/val gap")
    print("2. ROC Curves - evaluate classifier performance")
    print("3. Precision-Recall Curves - for imbalanced datasets")
    print("4. Confusion Matrix - detailed prediction breakdown")
    print("5. Feature Importance - understand model drivers")
    print("6. Training History - neural network training progression")
    print("7. Model Comparison - compare multiple models")
    print("8. Overfitting Dashboard - comprehensive overview")
    print("="*70)
