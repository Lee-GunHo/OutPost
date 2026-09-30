"""artifacts/<run-name>/data/의 학습 데이터로 결정 트리를 학습하고 시각화/중요도를 저장."""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import joblib
import numpy as np

from ml_pipeline.config import ARTIFACTS_DIR, FEATURE_ORDER
from ml_pipeline.difficulty_types import DIFFICULTY_TYPES
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.models.decision_tree import (
    get_feature_importance,
    save_tree_visualization,
    train_decision_tree,
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True)
    parser.add_argument("--max-depth", type=int, default=5)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    setup_logging()

    run_dir = ARTIFACTS_DIR / args.run_name
    data_dir = run_dir / "data"
    model_dir = run_dir / "models"
    model_dir.mkdir(parents=True, exist_ok=True)

    X_train = np.load(data_dir / "X_train.npy")
    y_train = np.load(data_dir / "y_train.npy", allow_pickle=True)

    model = train_decision_tree(X_train, y_train, max_depth=args.max_depth, seed=args.seed)

    joblib.dump(model, model_dir / "decision_tree.joblib")

    save_tree_visualization(
        model, FEATURE_ORDER, DIFFICULTY_TYPES, model_dir / "decision_tree_plot.png")

    importance_df = get_feature_importance(model, FEATURE_ORDER)
    importance_df.to_csv(model_dir / "feature_importance.csv", index=False)


if __name__ == "__main__":
    main()
