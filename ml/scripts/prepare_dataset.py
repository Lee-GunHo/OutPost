"""
play_snapshots.csv + session_labels.csv -> 학습용 train/test 데이터셋 준비.

사용 예 (가짜 데이터):
    python scripts/prepare_dataset.py --run-name synthetic_demo ^
        --snapshot-csv data/synthetic/synthetic_play_snapshots.csv ^
        --label-csv data/synthetic/synthetic_session_labels.csv

사용 예 (실제 데이터, data/real/ 에 CSV를 복사해둔 뒤):
    python scripts/prepare_dataset.py --run-name real_v1 ^
        --snapshot-csv data/real/play_snapshots.csv ^
        --label-csv data/real/session_labels.csv
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.config import ARTIFACTS_DIR
from ml_pipeline.data_prep import prepare_dataset
from ml_pipeline.logging_setup import setup_logging


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True, help="artifacts/<run-name>/ 에 결과 저장")
    parser.add_argument("--snapshot-csv", type=Path, required=True)
    parser.add_argument("--label-csv", type=Path, required=True)
    parser.add_argument("--test-ratio", type=float, default=0.2)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    setup_logging()

    out_dir = ARTIFACTS_DIR / args.run_name
    prepare_dataset(
        snapshot_csv=args.snapshot_csv,
        label_csv=args.label_csv,
        out_dir=out_dir,
        test_ratio=args.test_ratio,
        seed=args.seed,
    )


if __name__ == "__main__":
    main()
