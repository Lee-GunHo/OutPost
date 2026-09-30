"""
Assets/Script/NpcTip/RuleBasedDiagnoser.cs를 그대로 이식한 Python 버전.
비교 기준(baseline)이므로 정규화 전 원본 값(raw feature)을 그대로 사용해야
C# 쪽 임계값과 같은 스케일로 동작함 — 절대 정규화된 값을 넣지 말 것.
"""

import numpy as np
import pandas as pd

from .config import DEFAULT_RULE_CONFIG, DifficultyRuleConfig


def _calc_confidence(value: float, threshold: float, config: DifficultyRuleConfig) -> float:
    if threshold <= 0:
        return config.max_confidence

    excess_ratio = np.clip((value - threshold) / threshold, 0.0, 1.0)
    return config.min_confidence + (config.max_confidence - config.min_confidence) * excess_ratio


def diagnose_row(row, config: DifficultyRuleConfig = DEFAULT_RULE_CONFIG):
    """row: toolGradeOrdinal/cumulativeDeathCount/recentDeathCount5min/monsterKillCount/
    questAcceptedCount/questCompletedCount/zoneDwellTimeSec/elapsedSessionSec 키를 가진 매핑.
    반환: (difficulty_type: str, confidence: float).
    RuleBasedDiagnoser.Diagnose()와 동일한 우선순위: CombatStruggle > EquipmentLack > Lost > ResourceLack > Smooth
    """
    recent_death = row["recentDeathCount5min"]

    if recent_death >= config.combat_struggle_recent_death_threshold:
        confidence = _calc_confidence(
            recent_death, config.combat_struggle_recent_death_threshold, config)
        return "CombatStruggle", confidence

    tool_grade_ordinal = row["toolGradeOrdinal"]
    cumulative_death = row["cumulativeDeathCount"]

    if (tool_grade_ordinal <= config.equipment_lack_max_grade_ordinal and
            cumulative_death >= config.equipment_lack_death_threshold):
        confidence = _calc_confidence(
            cumulative_death, config.equipment_lack_death_threshold, config)
        return "EquipmentLack", confidence

    quest_accepted = row["questAcceptedCount"]
    quest_completed = row["questCompletedCount"]
    has_active_quest = quest_accepted > quest_completed
    zone_dwell = row["zoneDwellTimeSec"]

    if (zone_dwell >= config.lost_zone_dwell_threshold_sec and
            (not config.lost_requires_active_quest or has_active_quest)):
        confidence = _calc_confidence(
            zone_dwell, config.lost_zone_dwell_threshold_sec, config)
        return "Lost", confidence

    elapsed = row["elapsedSessionSec"]
    monster_kill = row["monsterKillCount"]

    if (elapsed >= config.resource_lack_elapsed_threshold_sec and
            monster_kill <= config.resource_lack_max_monster_kill):
        confidence = _calc_confidence(
            elapsed, config.resource_lack_elapsed_threshold_sec, config)
        return "ResourceLack", confidence

    return "Smooth", config.smooth_confidence


def diagnose_dataframe(df: pd.DataFrame, config: DifficultyRuleConfig = DEFAULT_RULE_CONFIG):
    """df의 각 행에 규칙을 적용해 (예측 라벨 Series, 확신도 Series)를 반환."""
    results = df.apply(lambda row: diagnose_row(row, config), axis=1)
    predicted_labels = results.apply(lambda pair: pair[0])
    confidences = results.apply(lambda pair: pair[1])
    return predicted_labels, confidences
