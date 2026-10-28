"""
5단계: 팀원 자체 테스트로 모은 PlayLog CSV(tip_resolution_log / diagnosis_comparison_log)를
합쳐서 팁 이후 난관 해결률(난관 유형별)과 Rule/ML 판단 불일치 사례를 보고서로 만듦.

외부 플레이테스터를 모을 수 없어 게임 내 만족도 설문과 A/B 테스트(Rule vs ML 그룹 비교)는 제거함.
표본이 적으므로 모든 수치에 n을 같이 표시하고, n이 작으면 경고 문구를 자동으로 붙임.
"""

import logging
from pathlib import Path

import pandas as pd

logger = logging.getLogger(__name__)

MIN_RECOMMENDED_N = 30  # 이보다 표본이 적으면 "참고용" 경고를 붙임
SMALL_SAMPLE_WARNING = (
    "⚠️ 표본 수가 적어(n={n}) 통계적 검정력이 낮습니다. "
    "이 결과는 경향성 참고용이며, 확정적인 결론으로 발표하지 마세요."
)

_LOG_FILENAMES = {
    "snapshots": "play_snapshots.csv",
    "labels": "session_labels.csv",
    "tip_dialogue": "tip_dialogue_log.csv",
    "resolution": "tip_resolution_log.csv",
    "comparison": "diagnosis_comparison_log.csv",
}


def load_and_merge_logs(playtest_root: Path, filename: str) -> pd.DataFrame:
    """playtest_root 아래 모든 하위 폴더(테스터별)에서 같은 이름의 CSV를 찾아 합침.
    세션 ID가 GUID라 테스터 간 충돌 걱정 없이 그냥 이어붙이면 됨."""
    playtest_root = Path(playtest_root)
    matches = sorted(playtest_root.glob(f"**/{filename}"))

    if not matches:
        logger.warning("%s를 %s 아래에서 찾지 못했습니다.", filename, playtest_root)
        return pd.DataFrame()

    frames = []
    for path in matches:
        try:
            df = pd.read_csv(path)
            df["_sourceFile"] = str(path)
            frames.append(df)
        except pd.errors.EmptyDataError:
            logger.warning("%s가 비어 있어 건너뜁니다.", path)

    if not frames:
        return pd.DataFrame()

    merged = pd.concat(frames, ignore_index=True)
    logger.info("%s: 파일 %d개, 총 %d행 병합", filename, len(matches), len(merged))
    return merged


def load_all_logs(playtest_root: Path) -> dict:
    return {key: load_and_merge_logs(playtest_root, filename)
            for key, filename in _LOG_FILENAMES.items()}


def _warn_if_small(n: int) -> str:
    return SMALL_SAMPLE_WARNING.format(n=n) if n < MIN_RECOMMENDED_N else ""


def analyze_resolution(resolution_df: pd.DataFrame) -> dict:
    """팁을 준 뒤 난관이 해결됐는지(tip_resolution_log)를 전체 + 난관 유형별로 집계.
    A/B 그룹이 없으므로 그룹 간 통계 검정은 하지 않고 해결률/평균 해결 시간과 n만 보고."""
    if resolution_df.empty:
        return {"available": False}

    df = resolution_df.copy()
    df["resolved"] = df["resolved"].astype(str).str.lower() == "true"

    def summarize(part: pd.DataFrame) -> dict:
        total = len(part)
        resolved_count = int(part["resolved"].sum())
        minutes = part.loc[part["resolved"], "minutesToResolve"].dropna().astype(float)
        return {
            "n": total,
            "resolvedCount": resolved_count,
            "rate": resolved_count / total if total else None,
            "meanMinutesToResolve": minutes.mean() if len(minutes) else None,
        }

    by_type = {
        difficulty_type: summarize(part)
        for difficulty_type, part in df.groupby("difficultyType", sort=True)
    }

    overall = summarize(df)
    return {
        "available": True,
        "overall": overall,
        "byType": by_type,
        "warning": _warn_if_small(overall["n"]),
    }


def extract_mismatched_cases(comparison_df: pd.DataFrame) -> pd.DataFrame:
    """Rule과 ML 판단이 갈렸던 사례. 발표 자료용 예시로 쓰기 좋은 것들."""
    if comparison_df.empty:
        return pd.DataFrame()

    mismatched = comparison_df[comparison_df["agreement"] == "Mismatch"].copy()
    columns = [c for c in
               ["sessionId", "timestamp", "ruleType", "ruleConfidence", "mlType", "mlConfidence"]
               if c in mismatched.columns]
    return mismatched[columns].reset_index(drop=True)
