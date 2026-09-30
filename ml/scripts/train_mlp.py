"""artifacts/<run-name>/data/의 학습 데이터로 작은 MLP(PyTorch)를 학습."""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import torch

from ml_pipeline.config import ARTIFACTS_DIR
from ml_pipeline.difficulty_types import DIFFICULTY_TYPE_TO_INDEX
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.models.mlp import train_mlp
from ml_pipeline.plot_style import configure_korean_font

configure_korean_font()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True)
    parser.add_argument("--epochs", type=int, default=200)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--lr", type=float, default=1e-3)
    parser.add_argument("--hidden-dims", type=int, nargs="+", default=[32, 16])
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    setup_logging()

    run_dir = ARTIFACTS_DIR / args.run_name
    data_dir = run_dir / "data"
    model_dir = run_dir / "models"
    model_dir.mkdir(parents=True, exist_ok=True)

    X_train = np.load(data_dir / "X_train.npy")
    X_test = np.load(data_dir / "X_test.npy")
    y_train = np.load(data_dir / "y_train.npy", allow_pickle=True)
    y_test = np.load(data_dir / "y_test.npy", allow_pickle=True)

    y_train_idx = np.array([DIFFICULTY_TYPE_TO_INDEX[label] for label in y_train])
    y_test_idx = np.array([DIFFICULTY_TYPE_TO_INDEX[label] for label in y_test])

    model, history = train_mlp(
        X_train, y_train_idx, X_test, y_test_idx,
        num_classes=len(DIFFICULTY_TYPE_TO_INDEX),
        hidden_dims=tuple(args.hidden_dims),
        epochs=args.epochs,
        batch_size=args.batch_size,
        lr=args.lr,
        seed=args.seed,
    )

    torch.save(model, model_dir / "mlp.pt")

    history_df = pd.DataFrame(history)
    history_df.to_csv(model_dir / "mlp_training_history.csv", index=False)

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(10, 4))
    ax1.plot(history_df["epoch"], history_df["train_loss"], label="train_loss")
    ax1.plot(history_df["epoch"], history_df["test_loss"], label="test_loss")
    ax1.set_xlabel("epoch")
    ax1.legend()
    ax1.set_title("Loss")

    ax2.plot(history_df["epoch"], history_df["test_acc"], color="green")
    ax2.set_xlabel("epoch")
    ax2.set_title("Test Accuracy")

    fig.tight_layout()
    fig.savefig(model_dir / "mlp_training_curve.png", dpi=150)
    plt.close(fig)


if __name__ == "__main__":
    main()
