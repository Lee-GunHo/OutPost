"""
[테스트용] 5단계 플레이테스트 분석 파이프라인을 가짜 데이터로 처음부터 끝까지 실행.

사용 예:
    python scripts/run_playtest_analysis_demo.py
"""

import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from ml_pipeline.config import SYNTHETIC_DATA_DIR, SYNTHETIC_LABEL_CSV_NAME
from ml_pipeline.logging_setup import setup_logging
from ml_pipeline.synthetic_data import SYNTHETIC_WARNING_BANNER

RUN_NAME = "synthetic_playtest_demo"
SCRIPTS_DIR = Path(__file__).resolve().parent


def run_step(description: str, args: list[str]):
    print(f"\n----- {description} -----")
    result = subprocess.run([sys.executable, *args], cwd=SCRIPTS_DIR)
    if result.returncode != 0:
        print(f"[실패] {description} (exit code {result.returncode})")
        sys.exit(result.returncode)


def main():
    setup_logging()
    print(SYNTHETIC_WARNING_BANNER)

    labels_csv = SYNTHETIC_DATA_DIR / SYNTHETIC_LABEL_CSV_NAME

    if not labels_csv.exists():
        run_step("0/2 (선행) 3단계 가짜 기본 데이터 생성", ["generate_synthetic_data.py"])

    run_step("1/2 가짜 플레이테스트 후속 로그 생성 (A/B, 만족도, 해결 여부)", [
        "generate_synthetic_playtest_data.py",
    ])

    run_step("2/2 플레이테스트 분석 보고서 생성", [
        "analyze_playtest.py",
        "--run-name", RUN_NAME,
    ])

    print("\n" + "=" * 70)
    print(f"[테스트용] 플레이테스트 분석 데모 완료. artifacts/{RUN_NAME}/playtest_report/ 확인하세요.")
    print("=" * 70)


if __name__ == "__main__":
    main()
