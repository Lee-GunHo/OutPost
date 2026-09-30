"""
[테스트용] 5단계 분석 스크립트 동작 확인용 가짜 후속 로그(A/B 배정, 만족도, 해결 여부,
Rule/ML 비교) 생성. 3단계 synthetic_session_labels.csv가 먼저 있어야 함
(없으면 scripts/generate_synthetic_data.py를 먼저 실행).

사용 예:
    python scripts/generate_synthetic_playtest_data.py
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import pandas as pd

from ml_pipeline.config import DATA_DIR, SYNTHETIC_DATA_DIR, SYNTHETIC_LABEL_CSV_NAME
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.synthetic_playtest_data import generate_synthetic_playtest_followup

DEFAULT_OUT_DIR = DATA_DIR / "playtest" / "synthetic_demo_tester"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--labels-csv", type=Path,
                         default=SYNTHETIC_DATA_DIR / SYNTHETIC_LABEL_CSV_NAME)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    setup_logging()

    if not args.labels_csv.exists():
        raise SystemExit(
            f"{args.labels_csv}가 없습니다. 먼저 python scripts/generate_synthetic_data.py를 실행하세요.")

    labels_df = pd.read_csv(args.labels_csv)

    generate_synthetic_playtest_followup(labels_df, args.out_dir, seed=args.seed)


if __name__ == "__main__":
    main()
