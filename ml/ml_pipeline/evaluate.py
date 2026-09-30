"""세 모델(RuleBased/DecisionTree/MLP) 비교: accuracy, macro F1, 혼동 행렬, feature importance."""

import logging
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score

from .plot_style import configure_korean_font

logger = logging.getLogger(__name__)

configure_korean_font()


def compute_metrics(y_true, y_pred, class_order: list[str]) -> dict:
    accuracy = accuracy_score(y_true, y_pred)
    macro_f1 = f1_score(y_true, y_pred, labels=class_order, average="macro", zero_division=0)
    cm = confusion_matrix(y_true, y_pred, labels=class_order)

    return {"accuracy": accuracy, "macro_f1": macro_f1, "confusion_matrix": cm}


def save_confusion_matrix_figure(cm: np.ndarray, class_order: list[str], title: str, out_path: Path):
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    fig, ax = plt.subplots(figsize=(6, 5))
    im = ax.imshow(cm, cmap="Blues")

    ax.set_xticks(range(len(class_order)))
    ax.set_yticks(range(len(class_order)))
    ax.set_xticklabels(class_order, rotation=45, ha="right")
    ax.set_yticklabels(class_order)
    ax.set_xlabel("Predicted")
    ax.set_ylabel("True")
    ax.set_title(title)

    max_val = cm.max() if cm.max() > 0 else 1

    for i in range(cm.shape[0]):
        for j in range(cm.shape[1]):
            color = "white" if cm[i, j] > max_val / 2 else "black"
            ax.text(j, i, str(cm[i, j]), ha="center", va="center", color=color)

    fig.colorbar(im, ax=ax)
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)

    logger.info("혼동 행렬 저장: %s", out_path)


def save_comparison_table(results: dict[str, dict], out_dir: Path):
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    rows = [
        {"model": name, "accuracy": metrics["accuracy"], "macro_f1": metrics["macro_f1"]}
        for name, metrics in results.items()
    ]
    table_df = pd.DataFrame(rows)

    csv_path = out_dir / "model_comparison.csv"
    table_df.to_csv(csv_path, index=False)
    logger.info("모델 비교 표 저장: %s", csv_path)

    fig, ax = plt.subplots(figsize=(6, 1.2 + 0.4 * len(rows)))
    ax.axis("off")
    table = ax.table(
        cellText=table_df.round(4).astype(str).values,
        colLabels=table_df.columns,
        loc="center",
        cellLoc="center",
    )
    table.auto_set_font_size(False)
    table.set_fontsize(10)
    table.scale(1, 1.5)
    fig.tight_layout()

    png_path = out_dir / "model_comparison.png"
    fig.savefig(png_path, dpi=150, bbox_inches="tight")
    plt.close(fig)
    logger.info("모델 비교 표 이미지 저장: %s", png_path)

    return table_df


def save_feature_importance_figure(importance_df: pd.DataFrame, out_path: Path, top_n: int = 15):
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    plot_df = importance_df.head(top_n).iloc[::-1]

    fig, ax = plt.subplots(figsize=(8, 0.4 * len(plot_df) + 1))
    ax.barh(plot_df["feature"], plot_df["importance"])
    ax.set_xlabel("Importance")
    ax.set_title("Decision Tree Feature Importance")
    fig.tight_layout()
    fig.savefig(out_path, dpi=150)
    plt.close(fig)

    csv_path = out_path.with_suffix(".csv")
    importance_df.to_csv(csv_path, index=False)

    logger.info("Feature importance 저장: %s / %s", out_path, csv_path)
