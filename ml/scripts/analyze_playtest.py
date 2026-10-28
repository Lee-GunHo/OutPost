"""
팀원들의 PlayLog 폴더(각각 data/playtest/<이름>/ 아래 복사)를 모아서
팁 이후 난관 해결률(난관 유형별) + Rule/ML 판단 불일치 사례 보고서를 생성.

사용 예:
    python scripts/analyze_playtest.py --run-name real_playtest_1
    python scripts/analyze_playtest.py --run-name synthetic_playtest_demo --playtest-root data/playtest
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.config import ARTIFACTS_DIR, DATA_DIR
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.playtest_analysis import (
    analyze_resolution,
    extract_mismatched_cases,
    load_all_logs,
)
from ml_pipeline.playtest_report import (
    save_resolution_rate_chart,
    save_time_to_resolve_chart,
    write_summary_markdown,
)

DEFAULT_PLAYTEST_ROOT = DATA_DIR / "playtest"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-name", required=True)
    parser.add_argument("--playtest-root", type=Path, default=DEFAULT_PLAYTEST_ROOT)
    args = parser.parse_args()

    setup_logging()

    logs = load_all_logs(args.playtest_root)
    resolution_result = analyze_resolution(logs["resolution"])
    mismatched_df = extract_mismatched_cases(logs["comparison"])

    report_dir = ARTIFACTS_DIR / args.run_name / "playtest_report"

    save_resolution_rate_chart(resolution_result, report_dir / "resolution_rate_by_type.png")
    save_time_to_resolve_chart(logs["resolution"], report_dir / "time_to_resolve_by_type.png")

    chart_paths = {
        "resolution_rate": "resolution_rate_by_type.png",
        "time_to_resolve": "time_to_resolve_by_type.png",
    }

    report_dir.mkdir(parents=True, exist_ok=True)
    write_summary_markdown(
        resolution_result, mismatched_df,
        report_dir / "summary.md", chart_paths)

    mismatched_df.to_csv(report_dir / "mismatched_cases.csv", index=False)

    print(f"\n보고서 저장 위치: {report_dir}")


if __name__ == "__main__":
    main()
