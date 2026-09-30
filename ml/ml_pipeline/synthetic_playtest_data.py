"""
[테스트용] 5단계 분석 스크립트 동작 확인용 가짜 후속 로그 생성기.
ab_test_assignment / tip_dialogue_log / tip_feedback_log / tip_resolution_log /
diagnosis_comparison_log를 3단계 synthetic_session_labels.csv의 세션들을 기반으로 만듦.

ML 그룹이 Rule 그룹보다 살짝 더 나은 결과가 나오도록 편향을 줘서(만족도 소폭 높음,
해결률 소폭 높음, 해결 시간 소폭 짧음) 분석 스크립트가 "차이가 있어 보이는" 그림을
그릴 수 있는지 확인. 실제 의미 있는 데이터가 아니라 파이프라인 동작 확인용.
"""

import logging
from datetime import datetime, timedelta, timezone
from pathlib import Path

import numpy as np
import pandas as pd

from .difficulty_types import DIFFICULTY_TYPES
from .synthetic_data import SYNTHETIC_WARNING_BANNER

logger = logging.getLogger(__name__)

_MONSTER_NAMES = ["GoblinA", "SlimeB", "WolfC", "BanditD"]


def generate_synthetic_playtest_followup(
    labels_df: pd.DataFrame,
    out_dir: Path,
    seed: int = 42,
    feedback_response_rate: float = 0.7,
    comparison_log_rate: float = 0.3,
):
    logger.info(SYNTHETIC_WARNING_BANNER)

    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    rng = np.random.default_rng(seed)
    base_time = datetime(2026, 1, 1, tzinfo=timezone.utc)

    ab_rows = []
    tip_rows = []
    feedback_rows = []
    resolution_rows = []
    comparison_rows = []

    for _, row in labels_df.iterrows():
        session_id = row["sessionId"]
        true_type = row["difficultyLabel"]

        if true_type not in DIFFICULTY_TYPES:
            continue

        tip_time = base_time + timedelta(minutes=int(rng.integers(0, 100000)))
        group = "ML" if rng.random() < 0.5 else "Rule"
        entry_id = f"{true_type.lower()}_demo"

        ab_rows.append({
            "sessionId": session_id,
            "timestamp": tip_time.isoformat(),
            "group": group,
        })

        # 진단 확신도는 그룹에 따라 살짝 다르게(ML이 근소하게 더 확신 있게 - 데모용 편향)
        confidence = rng.uniform(0.75, 0.95) if group == "ML" else rng.uniform(0.6, 0.9)

        tip_rows.append({
            "sessionId": session_id,
            "timestamp": tip_time.isoformat(),
            "difficultyType": true_type,
            "confidence": round(confidence, 2),
            "entryId": entry_id,
            "nodeId": "start",
            "chosenChoiceText": "",
        })

        # ---- 만족도 설문 (일부는 건너뜀) ----
        if rng.random() < feedback_response_rate:
            base_rating = 3.4 if group == "ML" else 3.0
            rating = int(np.clip(round(rng.normal(base_rating, 1.0)), 1, 5))
            feedback_rows.append({
                "sessionId": session_id,
                "timestamp": (tip_time + timedelta(minutes=3)).isoformat(),
                "difficultyType": true_type,
                "entryId": entry_id,
                "rating": rating,
                "skipped": False,
            })
        else:
            feedback_rows.append({
                "sessionId": session_id,
                "timestamp": (tip_time + timedelta(minutes=3)).isoformat(),
                "difficultyType": true_type,
                "entryId": entry_id,
                "rating": "",
                "skipped": True,
            })

        # ---- 자동 해결 여부(ML이 근소하게 더 잘 풀리는 것으로 - 데모용 편향) ----
        resolve_prob = 0.65 if group == "ML" else 0.5

        if true_type == "Smooth":
            resolve_prob = 1.0

        resolved = rng.random() < resolve_prob
        minutes_to_resolve = ""

        if resolved:
            mean_minutes = 4.0 if group == "ML" else 5.5
            minutes_to_resolve = round(float(np.clip(rng.normal(mean_minutes, 1.5), 0.5, 10.0)), 1)

        resolution_rows.append({
            "sessionId": session_id,
            "tipTimestamp": tip_time.isoformat(),
            "checkTimestamp": (tip_time + timedelta(minutes=10)).isoformat(),
            "difficultyType": true_type,
            "entryId": entry_id,
            "resolved": resolved,
            "minutesToResolve": minutes_to_resolve,
        })

        # ---- Compare 모드 로그(일부 세션만 - Rule/ML 동시 진단 상황을 흉내) ----
        if rng.random() < comparison_log_rate:
            rule_type = true_type
            ml_type = true_type

            # 20% 확률로 서로 다른 판단(불일치 사례 예시를 만들기 위함)
            if rng.random() < 0.2:
                other_types = [t for t in DIFFICULTY_TYPES if t != true_type]
                ml_type = rng.choice(other_types)

            comparison_rows.append({
                "sessionId": session_id,
                "timestamp": tip_time.isoformat(),
                "ruleType": rule_type,
                "ruleConfidence": round(rng.uniform(0.5, 0.85), 2),
                "mlType": ml_type,
                "mlConfidence": round(rng.uniform(0.6, 0.95), 2),
                "agreement": "Match" if rule_type == ml_type else "Mismatch",
            })

    _save_csv(pd.DataFrame(ab_rows), out_dir / "ab_test_assignment.csv")
    _save_csv(pd.DataFrame(tip_rows), out_dir / "tip_dialogue_log.csv")
    _save_csv(pd.DataFrame(feedback_rows), out_dir / "tip_feedback_log.csv")
    _save_csv(pd.DataFrame(resolution_rows), out_dir / "tip_resolution_log.csv")
    _save_csv(pd.DataFrame(comparison_rows), out_dir / "diagnosis_comparison_log.csv")

    logger.info("가짜 플레이테스트 후속 로그 생성 완료: %s", out_dir)


def _save_csv(df: pd.DataFrame, path: Path):
    df.to_csv(path, index=False)
    logger.info("  %s (%d행)", path.name, len(df))
