"""학습된 MLP를 ONNX로 내보내고, PyTorch 출력과 일치하는지 onnxruntime으로 검증."""

import json
import logging
import shutil
from pathlib import Path

import numpy as np
import onnx
import onnxruntime
import pandas as pd
import torch

from .config import FEATURE_ORDER
from .data_prep import build_feature_matrix
from .models.mlp import DifficultyMLP, DifficultyMLPWithSoftmax

_RAW_FIELDS_FOR_TEST_VECTORS = [
    "toolGrade", "cumulativeDeathCount", "recentDeathCount5min", "monsterKillCount",
    "bossKillCount", "questAcceptedCount", "questCompletedCount", "currentZone",
    "zoneDwellTimeSec", "elapsedSessionSec",
]

logger = logging.getLogger(__name__)

DEFAULT_OPSET = 15  # Unity Sentis 기준 권장값. 설치된 Sentis 버전의 지원 opset을 다시 확인할 것.


def export_mlp_to_onnx(
    model: DifficultyMLP,
    input_dim: int,
    out_path: Path,
    opset: int = DEFAULT_OPSET,
) -> Path:
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    wrapped = DifficultyMLPWithSoftmax(model)
    wrapped.eval()

    dummy_input = torch.randn(1, input_dim, dtype=torch.float32)

    torch.onnx.export(
        wrapped,
        dummy_input,
        str(out_path),
        input_names=["input"],
        output_names=["difficulty_probabilities"],
        dynamic_axes={
            "input": {0: "batch"},
            "difficulty_probabilities": {0: "batch"},
        },
        opset_version=opset,
    )

    _inline_external_data(out_path)

    logger.info("ONNX 모델 저장: %s (opset=%d)", out_path, opset)
    return out_path


def _inline_external_data(onnx_path: Path):
    """모델이 이 정도로 작으면 .onnx.data로 가중치를 분리해 둘 필요가 없으므로,
    Unity에 파일 하나만 넘기면 되도록 다시 하나로 합침."""
    data_file = onnx_path.with_name(onnx_path.name + ".data")

    if not data_file.exists():
        return

    model = onnx.load(str(onnx_path), load_external_data=True)
    onnx.save_model(model, str(onnx_path), save_as_external_data=False)
    data_file.unlink()

    logger.info("ONNX 가중치를 단일 파일로 병합했습니다 (%s 삭제).", data_file.name)


def verify_onnx_output(
    onnx_path: Path,
    model: DifficultyMLP,
    input_dim: int,
    num_samples: int = 8,
    seed: int = 42,
    atol: float = 1e-4,
) -> bool:
    """무작위 입력에 대해 PyTorch(logits+softmax)와 ONNX Runtime 출력이 일치하는지 확인."""
    rng = np.random.default_rng(seed)
    sample_input = rng.standard_normal((num_samples, input_dim)).astype(np.float32)

    wrapped = DifficultyMLPWithSoftmax(model)
    wrapped.eval()
    with torch.no_grad():
        torch_output = wrapped(torch.tensor(sample_input)).numpy()

    session = onnxruntime.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    onnx_output = session.run(None, {"input": sample_input})[0]

    is_close = np.allclose(torch_output, onnx_output, atol=atol)

    if is_close:
        logger.info("ONNX 출력 검증 통과 (PyTorch와 최대 오차 %.2e)",
                     float(np.max(np.abs(torch_output - onnx_output))))
    else:
        logger.error("ONNX 출력이 PyTorch와 다릅니다! 최대 오차 %.4f",
                      float(np.max(np.abs(torch_output - onnx_output))))

    return is_close


def export_test_vectors(
    test_raw_df: pd.DataFrame,
    preprocessing_meta: dict,
    model: DifficultyMLP,
    out_path: Path,
    num_samples: int = 20,
    seed: int = 42,
) -> Path:
    """Python-Unity 추론 일치성 테스트용 샘플. test_raw.csv에서 무작위로 몇 개를 뽑아
    raw 값 + Python이 계산한 softmax 확률을 같이 저장. Unity 쪽 EditMode 테스트가
    이 raw 값으로 직접 전처리+추론을 돌려서 expectedProbabilities와 비교함."""
    out_path = Path(out_path)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    sample_count = min(num_samples, len(test_raw_df))
    sampled_df = test_raw_df.sample(n=sample_count, random_state=seed).reset_index(drop=True)

    scaler_mean = np.array(preprocessing_meta["scalerMean"])
    scaler_std = np.array(preprocessing_meta["scalerStd"])

    X_raw = build_feature_matrix(sampled_df)
    X_scaled = (X_raw - scaler_mean) / scaler_std

    wrapped = DifficultyMLPWithSoftmax(model)
    wrapped.eval()
    with torch.no_grad():
        probabilities = wrapped(torch.tensor(X_scaled, dtype=torch.float32)).numpy()

    samples = []
    for i in range(sample_count):
        row = sampled_df.iloc[i]
        sample = {field: (row[field].item() if hasattr(row[field], "item") else row[field])
                  for field in _RAW_FIELDS_FOR_TEST_VECTORS}
        sample["expectedProbabilities"] = probabilities[i].tolist()
        samples.append(sample)

    payload = {
        "featureOrder": FEATURE_ORDER,
        "classOrder": preprocessing_meta["classOrder"],
        "samples": samples,
    }

    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)

    logger.info("Python-Unity 일치성 테스트 벡터 %d개 저장: %s", sample_count, out_path)
    return out_path


def copy_preprocessing_metadata(run_dir: Path, onnx_dir: Path) -> Path:
    """data_prep이 저장한 preprocessing.json을 ONNX 배포 폴더로 복사."""
    src = Path(run_dir) / "preprocessing.json"
    dst = Path(onnx_dir) / "preprocessing.json"
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, dst)
    logger.info("전처리 메타데이터 복사: %s -> %s", src, dst)
    return dst
