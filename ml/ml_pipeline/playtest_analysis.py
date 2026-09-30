"""
5단계: 여러 테스터의 PlayLog CSV(ab_test_assignment / tip_feedback_log / tip_resolution_log /
diagnosis_comparison_log)를 모아서 Rule 그룹 vs ML 그룹을 비교하고 보고서를 만듦.

표본 수가 원래 적을 수밖에 없는 플레이테스트 특성상, 모든 통계 결과에 n을 같이 표시하고
n이 작으면 경고 문구를 자동으로 붙임 - 여기 숫자를 발표 자료에 그대로 인용할 때 과장하지 말 것.
"""

import logging
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats

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
    "feedback": "tip_feedback_log.csv",
    "resolution": "tip_resolution_log.csv",
    "comparison": "diagnosis_comparison_log.csv",
    "ab_assignment": "ab_test_assignment.csv",
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


def build_session_group_lookup(ab_df: pd.DataFrame) -> pd.Series:
    """sessionId -> group("Rule"/"ML") 매핑. 같은 세션이 중복 기록됐으면 첫 값 사용."""
    if ab_df.empty:
        return pd.Series(dtype=object)

    deduped = ab_df.drop_duplicates(subset="sessionId", keep="first")
    return deduped.set_index("sessionId")["group"]


def _warn_if_small(n: int) -> str:
    return SMALL_SAMPLE_WARNING.format(n=n) if n < MIN_RECOMMENDED_N else ""


def analyze_feedback(feedback_df: pd.DataFrame, group_lookup: pd.Series) -> dict:
    """그룹별 팁 만족도(1~5점) 평균/표준편차/n + Mann-Whitney U 검정."""
    if feedback_df.empty or group_lookup.empty:
        return {"available": False}

    df = feedback_df.copy()
    df["group"] = df["sessionId"].map(group_lookup)
    rated = df[df["rating"].notna() & df["group"].notna()]

    rule_ratings = rated.loc[rated["group"] == "Rule", "rating"].astype(float)
    ml_ratings = rated.loc[rated["group"] == "ML", "rating"].astype(float)

    result = {
        "available": True,
        "rule": {"n": len(rule_ratings), "mean": rule_ratings.mean(), "std": rule_ratings.std()},
        "ml": {"n": len(ml_ratings), "mean": ml_ratings.mean(), "std": ml_ratings.std()},
        "skippedCount": int((df["rating"].isna()).sum()),
    }

    if len(rule_ratings) >= 2 and len(ml_ratings) >= 2:
        u_stat, p_value = stats.mannwhitneyu(rule_ratings, ml_ratings, alternative="two-sided")
        result["mannWhitneyU"] = {"statistic": float(u_stat), "pValue": float(p_value)}
    else:
        result["mannWhitneyU"] = None

    result["warning"] = _warn_if_small(min(len(rule_ratings), len(ml_ratings)))
    return result


def analyze_resolution(resolution_df: pd.DataFrame, group_lookup: pd.Series) -> dict:
    """그룹별 난관 해결률(Fisher's exact) + 해결까지 걸린 시간(Mann-Whitney U)."""
    if resolution_df.empty or group_lookup.empty:
        return {"available": False}

    df = resolution_df.copy()
    df["group"] = df["sessionId"].map(group_lookup)
    df = df[df["group"].notna()]

    rule_df = df[df["group"] == "Rule"]
    ml_df = df[df["group"] == "ML"]

    rule_resolved = int(rule_df["resolved"].sum())
    rule_total = len(rule_df)
    ml_resolved = int(ml_df["resolved"].sum())
    ml_total = len(ml_df)

    result = {
        "available": True,
        "rule": {"n": rule_total, "resolvedCount": rule_resolved,
                 "rate": rule_resolved / rule_total if rule_total else None},
        "ml": {"n": ml_total, "resolvedCount": ml_resolved,
               "rate": ml_resolved / ml_total if ml_total else None},
    }

    if rule_total > 0 and ml_total > 0:
        table = [[rule_resolved, rule_total - rule_resolved],
                 [ml_resolved, ml_total - ml_resolved]]
        odds_ratio, p_value = stats.fisher_exact(table)
        result["fisherExact"] = {"oddsRatio": float(odds_ratio), "pValue": float(p_value)}
    else:
        result["fisherExact"] = None

    rule_minutes = rule_df.loc[rule_df["resolved"] == True, "minutesToResolve"].dropna().astype(float)
    ml_minutes = ml_df.loc[ml_df["resolved"] == True, "minutesToResolve"].dropna().astype(float)

    result["rule"]["meanMinutesToResolve"] = rule_minutes.mean() if len(rule_minutes) else None
    result["ml"]["meanMinutesToResolve"] = ml_minutes.mean() if len(ml_minutes) else None

    if len(rule_minutes) >= 2 and len(ml_minutes) >= 2:
        u_stat, p_value = stats.mannwhitneyu(rule_minutes, ml_minutes, alternative="two-sided")
        result["timeToResolveMannWhitneyU"] = {"statistic": float(u_stat), "pValue": float(p_value)}
    else:
        result["timeToResolveMannWhitneyU"] = None

    result["warning"] = _warn_if_small(min(rule_total, ml_total))
    return result


def extract_mismatched_cases(comparison_df: pd.DataFrame) -> pd.DataFrame:
    """Rule과 ML 판단이 갈렸던 사례. 발표 자료용 예시로 쓰기 좋은 것들."""
    if comparison_df.empty:
        return pd.DataFrame()

    mismatched = comparison_df[comparison_df["agreement"] == "Mismatch"].copy()
    columns = [c for c in
               ["sessionId", "timestamp", "ruleType", "ruleConfidence", "mlType", "mlConfidence"]
               if c in mismatched.columns]
    return mismatched[columns].reset_index(drop=True)
