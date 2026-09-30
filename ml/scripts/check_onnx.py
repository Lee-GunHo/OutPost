"""
임의의 ONNX 파일에 Unity(Sentis)가 지원하지 않을 가능성이 높은 연산자가 있는지 검사.

사용 예:
    python scripts/check_onnx.py --onnx-path artifacts/synthetic_demo/onnx/difficulty_mlp.onnx
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.check_onnx_ops import check_onnx_model, print_report
from ml_pipeline.logging_setup import setup_logging


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--onnx-path", type=Path, required=True)
    args = parser.parse_args()

    setup_logging()

    findings = check_onnx_model(args.onnx_path)
    is_safe = print_report(args.onnx_path, findings)

    sys.exit(0 if is_safe else 1)


if __name__ == "__main__":
    main()
