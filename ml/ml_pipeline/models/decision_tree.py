"""결정 트리: 해석/발표용 baseline 비교 모델."""

import logging
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd
from sklearn.tree import DecisionTreeClassifier, plot_tree

from ..config import RANDOM_SEED
from ..plot_style import configure_korean_font

logger = logging.getLogger(__name__)

configure_korean_font()


def train_decision_tree(
    X_train,
    y_train,
    max_depth: int = 5,
    seed: int = RANDOM_SEED,
) -> DecisionTreeClassifier:
    model = DecisionTreeClassifier(
        max_depth=max_depth,
        random_state=seed,
        class_weight="balanced",
    )
    model.fit(X_train, y_train)
    logger.info("결정 트리 학습 완료 (max_depth=%d, 실제 깊이=%d)", max_depth, model.get_depth())
    return model


def save_tree_visualization(
    model: DecisionTreeClassifier,
    feature_names: list[str],
    class_names: list[str],
    out_path: Path,
):
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    fig, ax = plt.subplots(figsize=(22, 12))
    plot_tree(
        model,
        feature_names=feature_names,
        class_names=class_names,
        filled=True,
        rounded=True,
        fontsize=8,
        ax=ax,
    )
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)

    logger.info("결정 트리 시각화 저장: %s", out_path)


def get_feature_importance(model: DecisionTreeClassifier, feature_names: list[str]) -> pd.DataFrame:
    importance_df = pd.DataFrame({
        "feature": feature_names,
        "importance": model.feature_importances_,
    }).sort_values("importance", ascending=False).reset_index(drop=True)

    return importance_df
