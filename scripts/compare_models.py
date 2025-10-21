#!/usr/bin/env python3
"""
📊 Model Comparison Tool
Compare different model versions and their performance
"""

import json
import sys
from pathlib import Path
from datetime import datetime
import pandas as pd


class ModelComparator:
    """Compare ML model versions"""

    def __init__(self, models_dir="/root/AlgoTrendy_v2.6/ml_models/trend_reversals"):
        self.models_dir = Path(models_dir)

    def list_versions(self):
        """List all available model versions"""
        versions = []
        for version_dir in self.models_dir.iterdir():
            if version_dir.is_dir() and version_dir.name != 'latest':
                metrics_file = version_dir / "metrics.json"
                config_file = version_dir / "config.json"

                if metrics_file.exists() and config_file.exists():
                    with open(metrics_file) as f:
                        metrics = json.load(f)
                    with open(config_file) as f:
                        config = json.load(f)

                    versions.append({
                        'version': version_dir.name,
                        'path': version_dir,
                        'metrics': metrics,
                        'config': config
                    })

        return sorted(versions, key=lambda x: x['version'], reverse=True)

    def compare_all(self):
        """Compare all model versions"""
        print("\n" + "="*80)
        print("📊 ML MODEL VERSION COMPARISON")
        print("="*80)

        versions = self.list_versions()

        if not versions:
            print("❌ No model versions found")
            return

        print(f"\nFound {len(versions)} model versions\n")

        # Create comparison table
        comparison_data = []

        for v in versions:
            version = v['version']
            model_type = v['metrics'].get('best_model', 'unknown')
            val_metrics = v['metrics'].get('validation_metrics', {})

            row = {
                'Version': version,
                'Model Type': model_type,
                'Accuracy': f"{val_metrics.get('accuracy', 0):.1%}",
                'Precision': f"{val_metrics.get('precision', 0):.1%}",
                'Recall': f"{val_metrics.get('recall', 0):.1%}",
                'F1-Score': f"{val_metrics.get('f1', 0):.1%}",
                'Features': v['config'].get('n_features', 0),
                'Trained': v['config'].get('trained_at', 'unknown')[:10]
            }

            # Check for overfitting
            all_models = v['metrics'].get('all_models', {})
            if model_type in all_models:
                overfit = all_models[model_type].get('overfit_score', 0)
                row['Overfit'] = f"{overfit:+.1%}"
            else:
                row['Overfit'] = 'N/A'

            comparison_data.append(row)

        # Print table
        df = pd.DataFrame(comparison_data)
        print(df.to_string(index=False))

        # Find best model
        print("\n" + "="*80)
        print("🏆 BEST MODELS")
        print("="*80)

        # Best by F1-Score
        best_f1 = max(versions, key=lambda x: x['metrics'].get('validation_metrics', {}).get('f1', 0))
        print(f"\nBest F1-Score: {best_f1['version']}")
        print(f"  Model: {best_f1['metrics'].get('best_model')}")
        print(f"  F1: {best_f1['metrics'].get('validation_metrics', {}).get('f1', 0):.1%}")

        # Best by Precision
        best_precision = max(versions, key=lambda x: x['metrics'].get('validation_metrics', {}).get('precision', 0))
        print(f"\nBest Precision: {best_precision['version']}")
        print(f"  Model: {best_precision['metrics'].get('best_model')}")
        print(f"  Precision: {best_precision['metrics'].get('validation_metrics', {}).get('precision', 0):.1%}")

        # Latest model
        latest = versions[0]
        print(f"\nLatest Model: {latest['version']}")
        print(f"  Model: {latest['metrics'].get('best_model')}")
        print(f"  F1: {latest['metrics'].get('validation_metrics', {}).get('f1', 0):.1%}")

        print("\n" + "="*80)

    def detailed_comparison(self, version1, version2):
        """Detailed comparison between two specific versions"""
        print("\n" + "="*80)
        print(f"📊 DETAILED COMPARISON: {version1} vs {version2}")
        print("="*80)

        v1_dir = self.models_dir / version1
        v2_dir = self.models_dir / version2

        if not v1_dir.exists() or not v2_dir.exists():
            print("❌ One or both versions not found")
            return

        # Load metrics
        with open(v1_dir / "metrics.json") as f:
            v1_metrics = json.load(f)
        with open(v2_dir / "metrics.json") as f:
            v2_metrics = json.load(f)

        # Compare validation metrics
        print("\n📊 Validation Metrics Comparison:")
        print(f"\n{'Metric':<15} {version1:<15} {version2:<15} {'Diff':<15}")
        print("─"*60)

        v1_val = v1_metrics.get('validation_metrics', {})
        v2_val = v2_metrics.get('validation_metrics', {})

        for metric in ['accuracy', 'precision', 'recall', 'f1']:
            v1_score = v1_val.get(metric, 0)
            v2_score = v2_val.get(metric, 0)
            diff = v2_score - v1_score

            print(f"{metric.capitalize():<15} {v1_score:>13.1%} {v2_score:>13.1%} {diff:>+13.1%}")

        # Compare all models
        print("\n📊 All Models Comparison:")

        v1_all = v1_metrics.get('all_models', {})
        v2_all = v2_metrics.get('all_models', {})

        all_model_types = set(list(v1_all.keys()) + list(v2_all.keys()))

        for model_type in all_model_types:
            print(f"\n{model_type.upper()}:")
            if model_type in v1_all and model_type in v2_all:
                v1_f1 = v1_all[model_type].get('val_metrics', {}).get('f1', 0)
                v2_f1 = v2_all[model_type].get('val_metrics', {}).get('f1', 0)
                diff = v2_f1 - v1_f1
                print(f"  {version1}: {v1_f1:.1%}")
                print(f"  {version2}: {v2_f1:.1%}")
                print(f"  Difference: {diff:+.1%}")
            else:
                print("  Not available in both versions")

        print("\n" + "="*80)

    def recommend_deployment(self):
        """Recommend which model to deploy"""
        print("\n" + "="*80)
        print("💡 DEPLOYMENT RECOMMENDATION")
        print("="*80)

        versions = self.list_versions()
        if not versions:
            print("❌ No models available")
            return

        # Score each model
        scored_models = []
        for v in versions:
            val_metrics = v['metrics'].get('validation_metrics', {})
            all_models = v['metrics'].get('all_models', {})
            model_type = v['metrics'].get('best_model', '')

            # Calculate score
            f1 = val_metrics.get('f1', 0)
            precision = val_metrics.get('precision', 0)
            recall = val_metrics.get('recall', 0)

            # Penalty for overfitting
            overfit_penalty = 0
            if model_type in all_models:
                overfit = all_models[model_type].get('overfit_score', 0)
                overfit_penalty = max(0, overfit) * 0.5

            # Combined score: F1 * 0.5 + Precision * 0.3 + Recall * 0.2 - overfit_penalty
            score = (f1 * 0.5 + precision * 0.3 + recall * 0.2) - overfit_penalty

            scored_models.append({
                'version': v['version'],
                'score': score,
                'f1': f1,
                'precision': precision,
                'recall': recall,
                'overfit': overfit_penalty
            })

        # Sort by score
        scored_models.sort(key=lambda x: x['score'], reverse=True)

        # Recommend top 3
        print("\nTop 3 Models for Deployment:\n")
        for i, model in enumerate(scored_models[:3], 1):
            print(f"{i}. Version: {model['version']}")
            print(f"   Score: {model['score']:.3f}")
            print(f"   F1: {model['f1']:.1%} | Precision: {model['precision']:.1%} | Recall: {model['recall']:.1%}")
            print(f"   Overfit Penalty: {model['overfit']:.1%}")
            print()

        print(f"✅ RECOMMENDED: Deploy version {scored_models[0]['version']}")
        print("="*80)


def main():
    """Main execution"""
    import argparse

    parser = argparse.ArgumentParser(description='Compare ML model versions')
    parser.add_argument('--action', choices=['list', 'compare', 'recommend'], default='list',
                       help='Action to perform')
    parser.add_argument('--v1', help='First version for detailed comparison')
    parser.add_argument('--v2', help='Second version for detailed comparison')

    args = parser.parse_args()

    comparator = ModelComparator()

    if args.action == 'list':
        comparator.compare_all()
    elif args.action == 'compare':
        if not args.v1 or not args.v2:
            print("❌ Please provide --v1 and --v2 for comparison")
            sys.exit(1)
        comparator.detailed_comparison(args.v1, args.v2)
    elif args.action == 'recommend':
        comparator.recommend_deployment()


if __name__ == "__main__":
    main()
