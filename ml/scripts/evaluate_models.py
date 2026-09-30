"""RuleBasedDiagnoser / DecisionTree / MLP 세 모델을 test set에서 비교."""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import joblib
import numpy as np
import pandas as pd
import torch

from ml_pipeline.config import ARTIFACTS_DIR
from ml_pipeline.difficulty_types import DIFFICULTY_TYPES, INDEX_TO_DIFFICULTY_TYPE
from ml_pipeline.evaluate import (
    compute_metrics,
    save_comparison_table,
    save_confusion_matrix_figure,
)
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.rule_diagnoser import diagnose_dataframe


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True)
    args = parser.parse_args()

    setup_logging()

    run_dir = ARTIFACTS_DIR / args.run_name
    data_dir = run_dir / "data"
    model_dir = run_dir / "models"
    eval_dir = run_dir / "evaluation"
    eval_dir.mkdir(parents=True, exist_ok=True)

    test_raw_df = pd.read_csv(data_dir / "test_raw.csv")
    X_test = np.load(data_dir / "X_test.npy")
    y_test = np.load(data_dir / "y_test.npy", allow_pickle=True)

    # ---- RuleBasedDiagnoser: 정규화 전 원본 값 기준 ----
    rule_preds, _confidences = diagnose_dataframe(test_raw_df)

    # ---- DecisionTree ----
    dt_model = joblib.load(model_dir / "decision_tree.joblib")
    dt_preds = dt_model.predict(X_test)

    # ---- MLP ----
    mlp_model = torch.load(model_dir / "mlp.pt", weights_only=False)
    mlp_model.eval()
    with torch.no_grad():
        logits = mlp_model(torch.tensor(X_test, dtype=torch.float32))
        mlp_pred_idx = logits.argmax(dim=1).numpy()
    mlp_preds = np.array([INDEX_TO_DIFFICULTY_TYPE[i] for i in mlp_pred_idx])

    results = {}
    predictions_by_model = {
        "RuleBased": rule_preds.to_numpy() if hasattr(rule_preds, "to_numpy") else np.array(rule_preds),
        "DecisionTree": dt_preds,
        "MLP": mlp_preds,
    }

    for model_name, preds in predictions_by_model.items():
        metrics = compute_metrics(y_test, preds, DIFFICULTY_TYPES)
        results[model_name] = metrics

        save_confusion_matrix_figure(
            metrics["confusion_matrix"], DIFFICULTY_TYPES,
            title=f"{model_name} Confusion Matrix",
            out_path=eval_dir / f"confusion_{model_name.lower()}.png",
        )

    save_comparison_table(results, eval_dir)


if __name__ == "__main__":
    main()
