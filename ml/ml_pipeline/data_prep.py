"""
1단계 CSV(play_snapshots.csv + session_labels.csv)를 읽어서
학습 가능한 데이터셋(train/test, 세션 단위 split)으로 만드는 파이프라인.
"""

import json
import logging
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler

from .config import (
    FEATURE_ORDER,
    ITEM_GRADE_TO_ORDINAL,
    NUMERIC_FEATURES,
    RANDOM_SEED,
    TRAIN_TEST_SPLIT_RATIO,
    ZONE_CATEGORIES,
    ZONE_ONEHOT_FEATURES,
)
from .difficulty_types import DIFFICULTY_TYPES

logger = logging.getLogger(__name__)

_RAW_NUMERIC_COLUMNS_FOR_IMPUTATION = [
    "cumulativeDeathCount",
    "recentDeathCount5min",
    "monsterKillCount",
    "bossKillCount",
    "questAcceptedCount",
    "questCompletedCount",
    "zoneDwellTimeSec",
    "elapsedSessionSec",
]
_RAW_CATEGORICAL_COLUMNS_FOR_IMPUTATION = ["toolGrade", "currentZone"]


def load_snapshots(csv_path: Path) -> pd.DataFrame:
    df = pd.read_csv(csv_path)
    logger.info("스냅샷 CSV 로드: %s (%d행)", csv_path, len(df))
    return df


def load_labels(csv_path: Path) -> pd.DataFrame:
    df = pd.read_csv(csv_path)
    logger.info("라벨 CSV 로드: %s (%d행)", csv_path, len(df))
    return df


def merge_and_label(snapshots_df: pd.DataFrame, labels_df: pd.DataFrame) -> pd.DataFrame:
    """세션 ID로 조인. 설문 라벨이 없거나 5개 DifficultyType에 속하지 않는 세션은 제외."""
    labels_df = labels_df.dropna(subset=["sessionId", "difficultyLabel"])
    labels_df = labels_df[["sessionId", "difficultyLabel"]].drop_duplicates(subset=["sessionId"])

    merged = snapshots_df.merge(labels_df, on="sessionId", how="inner")

    dropped_unlabeled = snapshots_df["sessionId"].nunique() - merged["sessionId"].nunique()
    if dropped_unlabeled > 0:
        logger.warning(
            "설문 라벨이 없는 세션 %d개를 학습 데이터에서 제외했습니다.", dropped_unlabeled)

    valid_mask = merged["difficultyLabel"].isin(DIFFICULTY_TYPES)
    invalid_count = (~valid_mask).sum()

    if invalid_count > 0:
        invalid_values = merged.loc[~valid_mask, "difficultyLabel"].unique()
        logger.warning(
            "%d행이 DifficultyType 5종에 속하지 않는 라벨(%s)이라 제외했습니다. "
            "자유서술형 설문이라면 먼저 5개 카테고리로 매핑하는 전처리가 필요합니다.",
            invalid_count, list(invalid_values))

    merged = merged[valid_mask].reset_index(drop=True)
    return merged


def handle_missing_values(df: pd.DataFrame) -> pd.DataFrame:
    """수치형은 중앙값, 범주형은 최빈값(없으면 기본값)으로 결측치 보정."""
    df = df.copy()

    for column in _RAW_NUMERIC_COLUMNS_FOR_IMPUTATION:
        if column not in df.columns:
            continue

        missing_count = df[column].isna().sum()

        if missing_count > 0:
            median_value = df[column].median()
            df[column] = df[column].fillna(median_value)
            logger.warning("'%s' 결측치 %d개를 중앙값(%.2f)으로 채웠습니다.",
                            column, missing_count, median_value)

    default_values = {"toolGrade": "Normal", "currentZone": "Unknown"}

    for column in _RAW_CATEGORICAL_COLUMNS_FOR_IMPUTATION:
        if column not in df.columns:
            continue

        missing_count = df[column].isna().sum()

        if missing_count > 0:
            mode_series = df[column].mode()
            fill_value = mode_series.iat[0] if not mode_series.empty else default_values[column]
            df[column] = df[column].fillna(fill_value)
            logger.warning("'%s' 결측치 %d개를 '%s'로 채웠습니다.",
                            column, missing_count, fill_value)

    return df


def add_derived_features(df: pd.DataFrame) -> pd.DataFrame:
    """toolGradeOrdinal, 파생 비율 변수, 구역 원핫 인코딩 추가."""
    df = df.copy()

    df["toolGradeOrdinal"] = df["toolGrade"].map(ITEM_GRADE_TO_ORDINAL)
    # C# RuleBasedDiagnoser.ParseToolGrade와 동일하게, 매핑 안 되는 값("None" 포함)은 Normal(0) 취급.
    df["toolGradeOrdinal"] = df["toolGradeOrdinal"].fillna(ITEM_GRADE_TO_ORDINAL["Normal"]).astype(int)

    df["deathRatePerMin5"] = df["recentDeathCount5min"] / 5.0
    df["killDeathRatio"] = df["monsterKillCount"] / df["cumulativeDeathCount"].clip(lower=1)
    df["questCompletionRatio"] = df["questCompletedCount"] / df["questAcceptedCount"].clip(lower=1)

    safe_zone = df["currentZone"].where(df["currentZone"].isin(ZONE_CATEGORIES), "Unknown")

    for zone in ZONE_CATEGORIES:
        df[f"zone_{zone}"] = (safe_zone == zone).astype(float)

    return df


def session_level_split(
    df: pd.DataFrame,
    test_ratio: float = TRAIN_TEST_SPLIT_RATIO,
    seed: int = RANDOM_SEED,
):
    """같은 세션의 스냅샷이 train/test에 동시에 섞이지 않도록 세션 ID 단위로 분할."""
    session_label = df.groupby("sessionId")["difficultyLabel"].agg(lambda s: s.mode().iat[0])
    session_ids = session_label.index.to_numpy()
    labels_for_split = session_label.to_numpy()

    _, class_counts = np.unique(labels_for_split, return_counts=True)
    can_stratify = len(session_ids) >= 2 and class_counts.min() >= 2

    train_ids, test_ids = train_test_split(
        session_ids,
        test_size=test_ratio,
        random_state=seed,
        stratify=labels_for_split if can_stratify else None,
    )

    if not can_stratify:
        logger.warning(
            "일부 난관 유형의 세션 수가 너무 적어 stratified split을 쓰지 못했습니다 "
            "(세션 수를 늘리거나 클래스 비율을 확인하세요).")

    train_df = df[df["sessionId"].isin(train_ids)].reset_index(drop=True)
    test_df = df[df["sessionId"].isin(test_ids)].reset_index(drop=True)

    logger.info(
        "세션 단위 split: train 세션 %d개(%d행) / test 세션 %d개(%d행)",
        len(train_ids), len(train_df), len(test_ids), len(test_df))

    return train_df, test_df


def build_feature_matrix(df: pd.DataFrame) -> np.ndarray:
    return df[FEATURE_ORDER].to_numpy(dtype=np.float64)


def prepare_dataset(
    snapshot_csv: Path,
    label_csv: Path,
    out_dir: Path,
    test_ratio: float = TRAIN_TEST_SPLIT_RATIO,
    seed: int = RANDOM_SEED,
) -> dict:
    """전체 데이터 준비 파이프라인. out_dir/data/에 학습용 산출물을 저장하고 요약 dict를 반환."""
    out_dir = Path(out_dir)
    data_out_dir = out_dir / "data"
    data_out_dir.mkdir(parents=True, exist_ok=True)

    snapshots_df = load_snapshots(snapshot_csv)
    labels_df = load_labels(label_csv)

    merged = merge_and_label(snapshots_df, labels_df)
    merged = handle_missing_values(merged)
    merged = add_derived_features(merged)

    train_df, test_df = session_level_split(merged, test_ratio=test_ratio, seed=seed)

    X_train_raw = build_feature_matrix(train_df)
    X_test_raw = build_feature_matrix(test_df)
    y_train = train_df["difficultyLabel"].to_numpy()
    y_test = test_df["difficultyLabel"].to_numpy()

    scaler = StandardScaler()
    X_train = scaler.fit_transform(X_train_raw)
    X_test = scaler.transform(X_test_raw)

    train_df.to_csv(data_out_dir / "train_raw.csv", index=False)
    test_df.to_csv(data_out_dir / "test_raw.csv", index=False)
    np.save(data_out_dir / "X_train.npy", X_train)
    np.save(data_out_dir / "X_test.npy", X_test)
    np.save(data_out_dir / "y_train.npy", y_train)
    np.save(data_out_dir / "y_test.npy", y_test)

    preprocessing_meta = {
        "featureOrder": FEATURE_ORDER,
        "numericFeatures": NUMERIC_FEATURES,
        "zoneOneHotFeatures": ZONE_ONEHOT_FEATURES,
        "zoneCategories": ZONE_CATEGORIES,
        "itemGradeToOrdinal": ITEM_GRADE_TO_ORDINAL,
        "classOrder": DIFFICULTY_TYPES,
        "scalerMean": scaler.mean_.tolist(),
        "scalerStd": scaler.scale_.tolist(),
        "trainSessionCount": int(train_df["sessionId"].nunique()),
        "testSessionCount": int(test_df["sessionId"].nunique()),
        "trainRowCount": int(len(train_df)),
        "testRowCount": int(len(test_df)),
    }

    with open(out_dir / "preprocessing.json", "w", encoding="utf-8") as f:
        json.dump(preprocessing_meta, f, ensure_ascii=False, indent=2)

    logger.info("전처리 메타데이터 저장: %s", out_dir / "preprocessing.json")

    return preprocessing_meta
