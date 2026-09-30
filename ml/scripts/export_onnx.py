"""학습된 MLP를 ONNX로 내보내고, 출력 검증 + Unity 미지원 연산자 검사까지 수행."""

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import pandas as pd
import torch

from ml_pipeline.check_onnx_ops import check_onnx_model, print_report
from ml_pipeline.config import ARTIFACTS_DIR, FEATURE_ORDER
from ml_pipeline.export_onnx import (
    DEFAULT_OPSET,
    copy_preprocessing_metadata,
    export_mlp_to_onnx,
    export_test_vectors,
    verify_onnx_output,
)
from ml_pipeline.logging_setup import setup_logging


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True)
    parser.add_argument("--opset", type=int, default=DEFAULT_OPSET)
    parser.add_argument("--test-vector-count", type=int, default=20)
    args = parser.parse_args()

    setup_logging()

    run_dir = ARTIFACTS_DIR / args.run_name
    model_dir = run_dir / "models"
    onnx_dir = run_dir / "onnx"

    mlp_model = torch.load(model_dir / "mlp.pt", weights_only=False)
    mlp_model.eval()

    onnx_path = export_mlp_to_onnx(
        mlp_model, input_dim=len(FEATURE_ORDER),
        out_path=onnx_dir / "difficulty_mlp.onnx", opset=args.opset)

    verify_onnx_output(onnx_path, mlp_model, input_dim=len(FEATURE_ORDER))

    copy_preprocessing_metadata(run_dir, onnx_dir)

    with open(run_dir / "preprocessing.json", encoding="utf-8") as f:
        preprocessing_meta = json.load(f)

    test_raw_df = pd.read_csv(run_dir / "data" / "test_raw.csv")

    export_test_vectors(
        test_raw_df, preprocessing_meta, mlp_model,
        out_path=onnx_dir / "test_vectors.json",
        num_samples=args.test_vector_count,
    )

    findings = check_onnx_model(onnx_path)
    is_safe = print_report(onnx_path, findings)

    if not is_safe:
        sys.exit(1)


if __name__ == "__main__":
    main()
