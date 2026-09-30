"""
경로 상수, feature 목록, RuleBasedDiagnoser 기준값(DifficultyRuleConfig.cs 미러링)을 모아둠.
"""

from dataclasses import dataclass
from pathlib import Path

# ml/ 디렉터리를 기준으로 한 경로들
ML_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = ML_ROOT / "data"
SYNTHETIC_DATA_DIR = DATA_DIR / "synthetic"
REAL_DATA_DIR = DATA_DIR / "real"
ARTIFACTS_DIR = ML_ROOT / "artifacts"

SNAPSHOT_CSV_NAME = "play_snapshots.csv"
LABEL_CSV_NAME = "session_labels.csv"

SYNTHETIC_SNAPSHOT_CSV_NAME = "synthetic_play_snapshots.csv"
SYNTHETIC_LABEL_CSV_NAME = "synthetic_session_labels.csv"

# ---- Assets/Script/Item/ItemData.cs의 ItemGrade enum과 순서 동일 ----
ITEM_GRADE_ORDER = ["Normal", "Rare", "Epic", "Legendary"]
ITEM_GRADE_TO_ORDINAL = {name: i for i, name in enumerate(ITEM_GRADE_ORDER)}

# ---- Assets/Script/PlayLog/PlayLogZoneClassifier.cs가 만드는 구역 라벨 ----
ZONE_CATEGORIES = ["Safe", "Dirt", "Stone_Copper", "Silver_Gold", "Gold", "Unknown"]

# ---- 최종 feature 목록(순서 고정). Unity 쪽 런타임 전처리도 반드시 이 순서를 따라야 함 ----
NUMERIC_FEATURES = [
    "toolGradeOrdinal",
    "cumulativeDeathCount",
    "recentDeathCount5min",
    "deathRatePerMin5",       # 파생: recentDeathCount5min / 5.0
    "monsterKillCount",
    "bossKillCount",
    "killDeathRatio",         # 파생: monsterKillCount / max(cumulativeDeathCount, 1)
    "questAcceptedCount",
    "questCompletedCount",
    "questCompletionRatio",   # 파생: questCompletedCount / max(questAcceptedCount, 1)
    "zoneDwellTimeSec",
    "elapsedSessionSec",
]
ZONE_ONEHOT_FEATURES = [f"zone_{zone}" for zone in ZONE_CATEGORIES]
FEATURE_ORDER = NUMERIC_FEATURES + ZONE_ONEHOT_FEATURES


@dataclass(frozen=True)
class DifficultyRuleConfig:
    """Assets/Script/NpcTip/DifficultyRuleConfig.cs의 기본값과 동일."""

    combat_struggle_recent_death_threshold: int = 2
    equipment_lack_max_grade_ordinal: int = ITEM_GRADE_TO_ORDINAL["Normal"]
    equipment_lack_death_threshold: int = 1
    lost_zone_dwell_threshold_sec: float = 180.0
    lost_requires_active_quest: bool = True
    resource_lack_elapsed_threshold_sec: float = 300.0
    resource_lack_max_monster_kill: int = 3
    min_confidence: float = 0.5
    max_confidence: float = 0.95
    smooth_confidence: float = 0.8


DEFAULT_RULE_CONFIG = DifficultyRuleConfig()

RANDOM_SEED = 42
TRAIN_TEST_SPLIT_RATIO = 0.2  # 세션 단위 test 비율
