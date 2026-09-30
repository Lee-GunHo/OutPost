# 난관 분류 모델 학습 + 플레이테스트 분석 파이프라인 (3·5단계)

Unity `Assets/Script/PlayLog/`가 수집한 CSV(1단계)를 읽어, 난관 유형(2단계
`DifficultyType`: EquipmentLack/CombatStruggle/Lost/ResourceLack/Smooth)을 분류하는
모델을 학습하고 Unity에 배포 가능한 ONNX로 내보내는 Python 파이프라인(3단계).
여기에 더해 플레이테스트 결과(A/B 테스트, 만족도, 난관 해결률)를 분석하는
스크립트도 포함(5단계).

이 폴더는 Unity 프로젝트(`Assets/`) 바깥에 있으며, Unity는 이 폴더를 전혀 참조하지 않음.

## 설치

```bash
cd ml
python -m venv venv
venv\Scripts\activate        # Windows
pip install -r requirements.txt
```

## 실행 순서

### 0. 가짜 데이터로 전체 파이프라인 한 번에 확인 (권장 첫 실행)

```bash
python scripts/run_all_demo.py
```

**⚠️ 이 명령이 만드는 데이터/모델/지표는 전부 가짜(synthetic)입니다.** 실제 플레이테스트
데이터가 아직 없어서 파이프라인이 끝까지 정상 동작하는지만 확인하는 용도이며,
여기서 나온 모델을 실전에 배포하면 안 됩니다. 결과는 `artifacts/synthetic_demo/`에
저장되어 실제 데이터로 학습한 결과와 절대 섞이지 않습니다.

### 1. 실제 데이터로 학습할 때

1. Unity가 `Application.persistentDataPath/PlayLog/`에 쌓은 `play_snapshots.csv`,
   `session_labels.csv`를 이 저장소의 `ml/data/real/`로 복사.
   - Windows 기본 경로: `%userprofile%\AppData\LocalLow\<회사명>\<프로덕트명>\PlayLog\`
   - **`session_labels.csv`의 `difficultyLabel`은 반드시 `EquipmentLack` / `CombatStruggle`
     / `Lost` / `ResourceLack` / `Smooth` 중 하나여야 합니다.** 설문 UI가 자유서술형이라면
     이 5개 카테고리로 매핑하는 전처리를 먼저 거쳐야 함(`data_prep.merge_and_label`이
     이 5개가 아닌 라벨은 자동으로 걸러내고 경고를 남김).

2. 각 단계를 순서대로 실행 (run-name은 원하는 대로, 예: `real_v1`):

```bash
python scripts/prepare_dataset.py --run-name real_v1 ^
    --snapshot-csv data/real/play_snapshots.csv ^
    --label-csv data/real/session_labels.csv

python scripts/train_decision_tree.py --run-name real_v1
python scripts/train_mlp.py --run-name real_v1
python scripts/evaluate_models.py --run-name real_v1
python scripts/export_onnx.py --run-name real_v1
```

결과는 `artifacts/real_v1/`에 저장됨.

## 각 스크립트가 하는 일

| 스크립트 | 내용 |
|---|---|
| `generate_synthetic_data.py` | [테스트용] 가짜 세션 CSV 생성 |
| `prepare_dataset.py` | CSV 로드, 라벨 병합, 결측치 처리, 파생 변수, 세션 단위 train/test split, 정규화 |
| `train_decision_tree.py` | 결정 트리 학습 + 트리 시각화 PNG + feature importance |
| `train_mlp.py` | PyTorch MLP 학습 + 학습 곡선 PNG |
| `evaluate_models.py` | RuleBased(C# 이식) / DecisionTree / MLP 세 모델의 accuracy, macro F1, 혼동 행렬 비교 |
| `export_onnx.py` | MLP를 ONNX로 내보내고, PyTorch 출력과 일치하는지 검증 + 연산자 검사 |
| `check_onnx.py` | 임의의 ONNX 파일에 Unity 미지원 연산자가 있는지 단독 검사 |
| `run_all_demo.py` | 위 전체를 가짜 데이터로 순서대로 실행 |
| `generate_synthetic_playtest_data.py` | [테스트용] 가짜 A/B 배정·만족도·해결 여부·비교 로그 생성 |
| `analyze_playtest.py` | 테스터 CSV 병합, 그룹별 비교 + 통계 검정, 보고서 생성 |
| `run_playtest_analysis_demo.py` | 위 5단계 전체를 가짜 데이터로 순서대로 실행 |

## 결과물 구조 (`artifacts/<run-name>/`)

```
artifacts/<run-name>/
  preprocessing.json          # featureOrder, scaler mean/std, classOrder, zone/toolGrade 매핑
  data/
    train_raw.csv, test_raw.csv     # 파생변수까지 포함된 원본 스케일 데이터(RuleBased 평가용)
    X_train.npy, X_test.npy         # 정규화된 feature 행렬
    y_train.npy, y_test.npy         # 문자열 라벨
  models/
    decision_tree.joblib, decision_tree_plot.png, feature_importance.csv/.png
    mlp.pt, mlp_training_curve.png, mlp_training_history.csv
  evaluation/
    model_comparison.csv/.png
    confusion_rulebased.png, confusion_decisiontree.png, confusion_mlp.png
  onnx/
    difficulty_mlp.onnx
    preprocessing.json           # Unity 배포용 사본
```

## Unity에서 재사용할 때 알아야 할 것

- **입력 순서**: `preprocessing.json`의 `featureOrder`(18개, 고정 순서)와 정확히 같은 순서로
  입력 벡터를 만들어야 함. 마지막 6개는 `currentZone` 원핫 인코딩(`zoneCategories` 순서).
- **정규화**: Unity 쪽에서도 `(x - scalerMean) / scalerStd`를 각 feature마다 적용해야 함.
- **출력 순서**: ONNX 모델의 출력(softmax 확률, `difficulty_probabilities`)은 `classOrder`
  순서(`EquipmentLack, CombatStruggle, Lost, ResourceLack, Smooth`) — C#
  `DifficultyType` enum 선언 순서와 동일.
- **opset**: 기본 15로 내보냄 (`--opset`으로 변경 가능). 프로젝트는 Unity 6(6000.3.10f1)이며
  아직 Sentis 패키지를 설치하지 않은 상태이므로, Sentis 설치 후 지원 opset을 문서에서
  재확인하고 필요하면 다시 내보낼 것.
- **연산자 검사**: `export_onnx.py`가 자동으로 `check_onnx_ops`를 돌리지만, 참고 목록
  (`KNOWN_SUPPORTED_OPS`)은 보수적인 추정치일 뿐이므로 최종적으로는 Unity 에디터에
  ONNX를 넣어보고 Sentis가 실제로 로드/추론하는지 확인해야 함.

## 플레이테스트 분석 (5단계)

### 0. 가짜 데이터로 먼저 확인

```bash
python scripts/run_playtest_analysis_demo.py
```

**⚠️ 역시 가짜 데이터입니다.** `artifacts/synthetic_playtest_demo/playtest_report/`에
결과가 저장되며, 실제 플레이테스트 결과와 섞이지 않습니다.

### 1. 실제 플레이테스트 준비

**빌드 설정**
- 테스터에게 빌드를 줄 때, 로그가 `Application.persistentDataPath/PlayLog/`에 쌓인다고
  안내해주세요. Windows 기준 기본 경로:
  `%userprofile%\AppData\LocalLow\<회사명>\<제품명>\PlayLog\`
  (현재 프로젝트 설정 기준: `%userprofile%\AppData\LocalLow\DefaultCompany\OutPost\PlayLog\`
  — `DefaultCompany`는 Project Settings > Player에서 실제 회사명으로 바꾼 뒤 빌드하는 걸 권장)
- `NPCTipDialoguePresenter`의 `Diagnosis Mode`를 **`AB`**로 설정해서 빌드하면, 테스터별로
  세션 시작 시 Rule/ML 그룹이 자동 배정되고 `ab_test_assignment.csv`에 기록됩니다.
- 씬에 `TipFeedbackUI`, `TipFollowUpManager`, `SessionEndSurveyUI`가 배치되어 있어야
  만족도 설문/자동 해결 추적/종료 설문이 동작합니다 (Unity 쪽 설정 안내 참고).

**테스터 CSV 모으기**
- 각 테스터의 `PlayLog` 폴더 전체를 복사해서 `ml/data/playtest/<테스터명>/`에 넣기
  (예: `ml/data/playtest/tester_kim/`, `ml/data/playtest/tester_lee/`).
- 파일명은 그대로 두면 됨 — `analyze_playtest.py`가 `data/playtest/` 아래를 재귀로 훑어서
  같은 이름의 CSV를 전부 합침 (세션 ID가 GUID라 테스터 간 충돌 없음).

**분석 실행**

```bash
python scripts/analyze_playtest.py --run-name playtest_2026w1
```

결과는 `artifacts/playtest_2026w1/playtest_report/`에:
- `summary.md` — 그룹별 만족도/해결률/해결 시간 표 + 통계 검정(Mann-Whitney U, Fisher's
  exact) + 표본 수 경고 + Rule/ML 불일치 사례 표
- `feedback_by_group.png`, `resolution_rate_by_group.png`, `time_to_resolve_by_group.png`
- `mismatched_cases.csv`

## RuleBasedDiagnoser 비교 기준에 대해

`ml_pipeline/rule_diagnoser.py`는 `Assets/Script/NpcTip/RuleBasedDiagnoser.cs`를 그대로
Python으로 옮긴 것으로, DecisionTree/MLP의 성능을 "지금 게임에 이미 들어가 있는 규칙
기반 진단기보다 얼마나 더 정확한가?"로 비교하기 위한 baseline. 기준값이 바뀌면
`ml_pipeline/config.py`의 `DifficultyRuleConfig` 기본값도 C# `DifficultyRuleConfig.cs`
인스펙터 값과 맞춰서 함께 수정해야 함.
