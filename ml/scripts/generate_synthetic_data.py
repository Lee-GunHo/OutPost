"""
[테스트용] 가짜 플레이 로그 CSV 생성.

사용 예:
    python scripts/generate_synthetic_data.py
    python scripts/generate_synthetic_data.py --sessions-per-class 100 --seed 7
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.config import SYNTHETIC_DATA_DIR
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.synthetic_data import generate_synthetic_dataset


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sessions-per-class", type=int, default=60)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--label-noise-rate", type=float, default=0.05)
    parser.add_argument("--out-dir", type=Path, default=SYNTHETIC_DATA_DIR)
    args = parser.parse_args()

    setup_logging()

    generate_synthetic_dataset(
        out_dir=args.out_dir,
        sessions_per_class=args.sessions_per_class,
        seed=args.seed,
        label_noise_rate=args.label_noise_rate,
    )


if __name__ == "__main__":
    main()
