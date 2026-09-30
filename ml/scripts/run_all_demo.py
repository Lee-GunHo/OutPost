"""
[테스트용] 가짜 데이터로 전체 파이프라인(데이터 생성 -> 준비 -> 학습 -> 평가 -> ONNX 변환/검사)을
처음부터 끝까지 실행. 파이프라인이 제대로 동작하는지 확인하는 용도이며,
여기서 나온 모델/지표는 실전 배포에 쓰면 안 됨.

사용 예:
    python scripts/run_all_demo.py
"""

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.config import ARTIFACTS_DIR, SYNTHETIC_DATA_DIR
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.synthetic_data import SYNTHETIC_WARNING_BANNER

RUN_NAME = "synthetic_demo"

SCRIPTS_DIR = Path(__file__).resolve().parent


def run_step(description: str, args: list[str]):
    print(f"\n----- {description} -----")
    result = subprocess.run([sys.executable, *args], cwd=SCRIPTS_DIR)
    if result.returncode != 0:
        print(f"[실패] {description} (exit code {result.returncode})")
        sys.exit(result.returncode)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sessions-per-class", type=int, default=60)
    parser.add_argument("--epochs", type=int, default=150)
    args = parser.parse_args()

    setup_logging()
    print(SYNTHETIC_WARNING_BANNER)

    snapshot_csv = SYNTHETIC_DATA_DIR / "synthetic_play_snapshots.csv"
    label_csv = SYNTHETIC_DATA_DIR / "synthetic_session_labels.csv"

    run_step("1/6 가짜 데이터 생성", [
        "generate_synthetic_data.py",
        "--sessions-per-class", str(args.sessions_per_class),
    ])

    run_step("2/6 데이터셋 준비 (train/test 세션 단위 split)", [
        "prepare_dataset.py",
        "--run-name", RUN_NAME,
        "--snapshot-csv", str(snapshot_csv),
        "--label-csv", str(label_csv),
    ])

    run_step("3/6 결정 트리 학습", [
        "train_decision_tree.py",
        "--run-name", RUN_NAME,
    ])

    run_step("4/6 MLP 학습", [
        "train_mlp.py",
        "--run-name", RUN_NAME,
        "--epochs", str(args.epochs),
    ])

    run_step("5/6 세 모델 평가/비교", [
        "evaluate_models.py",
        "--run-name", RUN_NAME,
    ])

    run_step("6/6 MLP ONNX 변환 + 연산자 검사", [
        "export_onnx.py",
        "--run-name", RUN_NAME,
    ])

    run_dir = ARTIFACTS_DIR / RUN_NAME
    print("\n" + "=" * 70)
    print(f"[테스트용] 데모 파이프라인 완료. 결과물: {run_dir}")
    print("=" * 70)


if __name__ == "__main__":
    main()
