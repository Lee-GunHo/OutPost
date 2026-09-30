"""
파이프라인 동작 검증용 가짜 플레이 로그 생성기.

*** 테스트용 데이터입니다. 실제 플레이테스트 데이터가 아니며, 최종 모델 평가/배포에 쓰면 안 됩니다. ***
난관 유형별로 그럴듯한 패턴(사망 빈도, 처치 수, 구역 체류, 퀘스트 진행 등)을 만들어서
전체 파이프라인(데이터 준비 -> 학습 -> 평가 -> ONNX 변환)이 끝까지 동작하는지 확인하는 용도.
"""

import logging
from datetime import datetime, timedelta, timezone
from pathlib import Path

import numpy as np
import pandas as pd

from .config import SYNTHETIC_LABEL_CSV_NAME, SYNTHETIC_SNAPSHOT_CSV_NAME, ZONE_CATEGORIES
from .difficulty_types import DIFFICULTY_TYPES

logger = logging.getLogger(__name__)

SYNTHETIC_WARNING_BANNER = (
    "=" * 70 + "\n"
    "[테스트용] 지금부터 생성/사용하는 데이터는 실제 플레이 로그가 아닌 가짜 데이터입니다.\n"
    "파이프라인 동작 확인 전용이며, 이 데이터로 학습한 모델은 실전 배포에 쓰면 안 됩니다.\n"
    + "=" * 70
)

_SNAPSHOT_INTERVAL_SEC = 30.0
_TOOL_TYPES = ["Pickaxe", "Sword"]
_MONSTER_NAMES = ["GoblinA", "SlimeB", "WolfC", "BanditD"]


def _pick_tool_grade(rng: np.random.Generator, profile: str, progress: float) -> str:
    if profile == "EquipmentLack" or profile == "ResourceLack":
        return "Normal"

    if profile == "Smooth":
        # 세션이 진행될수록 장비가 좋아짐
        if progress > 0.7:
            return rng.choice(["Rare", "Epic"], p=[0.5, 0.5])
        if progress > 0.3:
            return rng.choice(["Normal", "Rare"], p=[0.4, 0.6])
        return "Normal"

    # CombatStruggle / Lost: 장비 등급은 죽음의 직접 원인이 아님을 보여주기 위해 다양하게
    return rng.choice(["Normal", "Rare", "Epic", "Legendary"], p=[0.3, 0.4, 0.25, 0.05])


def _simulate_zone_sequence(rng: np.random.Generator, profile: str, num_snapshots: int):
    """각 스냅샷 시점의 (구역, 그 구역에 머문 시간[초]) 리스트를 반환."""
    zones = []
    dwell_times = []

    if profile == "Lost":
        # 세션 내내 같은 구역에 머무름 -> dwell time이 계속 누적됨
        stuck_zone = rng.choice(ZONE_CATEGORIES[:-1])  # Unknown 제외
        for t in range(num_snapshots):
            zones.append(stuck_zone)
            dwell_times.append((t + 1) * _SNAPSHOT_INTERVAL_SEC)
        return zones, dwell_times

    if profile == "ResourceLack":
        zone_pool = ["Safe", "Dirt"]
    elif profile == "Smooth":
        zone_pool = ZONE_CATEGORIES[:-1]  # 여러 구역을 골고루 이동(진행이 빠름)
    else:
        zone_pool = ["Safe", "Dirt", "Stone_Copper"]

    current_zone = rng.choice(zone_pool)
    dwell = 0.0
    change_prob = 0.35 if profile == "Smooth" else 0.15

    for _ in range(num_snapshots):
        if rng.random() < change_prob:
            current_zone = rng.choice(zone_pool)
            dwell = _SNAPSHOT_INTERVAL_SEC
        else:
            dwell += _SNAPSHOT_INTERVAL_SEC

        zones.append(current_zone)
        dwell_times.append(dwell)

    return zones, dwell_times


def _death_happens(rng: np.random.Generator, profile: str) -> bool:
    death_prob = {
        "CombatStruggle": 0.30,
        "EquipmentLack": 0.16,
        "Lost": 0.04,
        "ResourceLack": 0.04,
        "Smooth": 0.03,
    }[profile]
    return rng.random() < death_prob


def _monster_kill_increment(rng: np.random.Generator, profile: str) -> int:
    if profile == "ResourceLack":
        return rng.choice([0, 0, 0, 1], p=[0.5, 0.25, 0.15, 0.1])
    if profile == "Lost":
        return rng.choice([0, 0, 1], p=[0.6, 0.25, 0.15])
    if profile == "Smooth":
        return rng.choice([0, 1, 1, 2], p=[0.2, 0.4, 0.3, 0.1])
    return rng.choice([0, 0, 1], p=[0.5, 0.3, 0.2])


def _simulate_session_rows(profile: str, session_id: str, session_start: datetime,
                            rng: np.random.Generator):
    total_minutes = rng.uniform(3.0, 20.0)
    num_snapshots = max(3, int((total_minutes * 60.0) / _SNAPSHOT_INTERVAL_SEC))

    zones, dwell_times = _simulate_zone_sequence(rng, profile, num_snapshots)

    cumulative_death = 0
    recent_death_window: list[float] = []  # 최근 사망이 발생한 elapsed 시각들(초)
    monster_kill = 0
    boss_kill = 0
    quest_accepted = 1 if rng.random() < 0.8 else 0
    quest_completed = 0

    rows = []

    for t in range(num_snapshots):
        elapsed = (t + 1) * _SNAPSHOT_INTERVAL_SEC
        event_type = "Periodic"

        if _death_happens(rng, profile):
            cumulative_death += 1
            recent_death_window.append(elapsed)
            event_type = "Death"

        # 최근 5분(300초) 윈도우만 유지
        recent_death_window = [t0 for t0 in recent_death_window if elapsed - t0 <= 300.0]

        monster_kill += _monster_kill_increment(rng, profile)

        if profile != "Lost" and quest_accepted == 1 and quest_completed == 0:
            complete_prob = {
                "Smooth": 0.06, "CombatStruggle": 0.02,
                "EquipmentLack": 0.02, "ResourceLack": 0.015,
            }.get(profile, 0.02)
            if rng.random() < complete_prob:
                quest_completed = 1
                event_type = "QuestComplete"

        progress = t / max(1, num_snapshots - 1)
        tool_grade = _pick_tool_grade(rng, profile, progress)
        tool_type = rng.choice(_TOOL_TYPES)

        timestamp = session_start + timedelta(seconds=elapsed)

        rows.append({
            "timestamp": timestamp.isoformat(),
            "sessionId": session_id,
            "elapsedSessionSec": round(elapsed, 1),
            "eventType": event_type,
            "toolGrade": tool_grade,
            "toolType": tool_type,
            "cumulativeDeathCount": cumulative_death,
            "recentDeathCount5min": len(recent_death_window),
            "lastDeathCause": rng.choice(_MONSTER_NAMES) if event_type == "Death" else "",
            "monsterKillCount": monster_kill,
            "bossKillCount": boss_kill,
            "questAcceptedCount": quest_accepted,
            "questCompletedCount": quest_completed,
            "currentZone": zones[t],
            "zoneDwellTimeSec": round(dwell_times[t], 1),
            "posX": round(rng.uniform(-200, 200), 2),
            "posY": 1.0,
            "posZ": round(rng.uniform(-200, 200), 2),
        })

    session_play_time_sec = num_snapshots * _SNAPSHOT_INTERVAL_SEC
    return rows, session_play_time_sec


def generate_synthetic_dataset(
    out_dir: Path,
    sessions_per_class: int = 60,
    seed: int = 42,
    label_noise_rate: float = 0.05,
):
    """난관 유형(DIFFICULTY_TYPES)마다 sessions_per_class개의 가짜 세션을 생성.

    label_noise_rate: 설문 응답이 실제 패턴과 다르게 나오는 현실적인 노이즈를 흉내내기 위해,
    이 확률로 세션 라벨을 다른 무작위 유형으로 바꿔치기함(패턴 자체는 원래 프로필 그대로 유지).
    """
    logger.info(SYNTHETIC_WARNING_BANNER)

    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    rng_master = np.random.default_rng(seed)
    session_start_base = datetime(2026, 1, 1, tzinfo=timezone.utc)

    all_rows = []
    label_rows = []

    for profile in DIFFICULTY_TYPES:
        for i in range(sessions_per_class):
            session_id = f"synthetic-{profile}-{i:03d}"
            session_rng = np.random.default_rng(rng_master.integers(0, 2**32 - 1))
            session_start = session_start_base + timedelta(
                minutes=int(rng_master.integers(0, 100000)))

            rows, play_time_sec = _simulate_session_rows(
                profile, session_id, session_start, session_rng)
            all_rows.extend(rows)

            recorded_label = profile
            if rng_master.random() < label_noise_rate:
                recorded_label = rng_master.choice(DIFFICULTY_TYPES)

            label_rows.append({
                "sessionId": session_id,
                "timestamp": (session_start + timedelta(seconds=play_time_sec)).isoformat(),
                "sessionPlayTimeSec": round(play_time_sec, 1),
                "difficultyLabel": recorded_label,
            })

    snapshots_df = pd.DataFrame(all_rows)
    labels_df = pd.DataFrame(label_rows)

    snapshot_path = out_dir / SYNTHETIC_SNAPSHOT_CSV_NAME
    label_path = out_dir / SYNTHETIC_LABEL_CSV_NAME

    snapshots_df.to_csv(snapshot_path, index=False)
    labels_df.to_csv(label_path, index=False)

    logger.info(
        "가짜 데이터 생성 완료: 세션 %d개(%d행) -> %s / %s",
        len(labels_df), len(snapshots_df), snapshot_path, label_path)

    return snapshot_path, label_path
