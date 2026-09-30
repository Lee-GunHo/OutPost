# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 참고하는 프로젝트 문서입니다.

## 플레이어 난관 진단 NPC 팁 시스템

목표: 플레이어 진행 데이터를 ML 분류 모델(scikit-learn → ONNX → Unity 로컬 추론)로 분석해
난관 유형을 판단하고, NPC가 선택지형 대화로 맞춤 팁을 제공. **외부 API(OpenAI 등)는 사용하지 않음.**

### 1단계 — 플레이 데이터 로깅 (`Assets/Script/PlayLog/`)

- `PlayLogManager` (싱글톤, DontDestroyOnLoad): 30초 주기 + 주요 이벤트(사망/퀘스트완료/강화) 시
  스냅샷을 CSV로 기록. `PlayerPresenter`/`GameProgressManager`/`NPCModel`의 기존 이벤트를
  구독하거나 값을 읽어오기만 하고, 그 반대(기존 코드가 PlayLog를 아는 것)는 없음.
- `PlayLogEntry`: 스냅샷 한 행의 데이터 구조(도구 등급, 사망 횟수/원인, 몬스터 처치 수,
  퀘스트 진행, 구역/체류시간, 좌표 등). **2단계 `IDifficultyDiagnoser`의 입력 타입으로 재사용.**
- `PlayLogWriter`: `Application.persistentDataPath/PlayLog/`에 CSV append 전용 기록
  (`play_snapshots.csv`, `session_labels.csv`, `tip_dialogue_log.csv`). 모두 `sessionId`로 조인 가능.
- `PlayLogZoneClassifier`: `SeedMapModel`의 스폰 기준 거리 링 공식을 재사용해 현재 위치를
  구역 라벨(Safe/Dirt/Stone_Copper/Silver_Gold/Gold)로 변환 + 체류시간 추적.

기존 코드에 추가한 최소 이벤트: `PlayerPresenter.OnPlayerDied/OnPlayerUpgraded`,
`NPCModel.OnQuestAccepted/OnQuestCompleted`, `SeedMapModel`의 구역 range getter 3개,
몬스터/보스 공격 경로에 `PlayerPresenter.SetPendingDamageSource(...)` 호출 추가(사망 원인 태깅용).

### 2단계 — 난관 진단기 + NPC 팁 대화 (`Assets/Script/NpcTip/`)

**진단기**
- `IDifficultyDiagnoser.Diagnose(PlayLogEntry) → DifficultyDiagnosis(Type, Confidence)`
- `DifficultyType`: EquipmentLack / CombatStruggle / Lost / ResourceLack / Smooth
- `RuleBasedDiagnoser`: 현재 유일한 구현체. 기준값은 `DifficultyRuleConfig`(ScriptableObject)에서
  읽음(코드에 하드코딩하지 않음). 여러 규칙이 동시에 해당하면
  CombatStruggle > EquipmentLack > Lost > ResourceLack 순으로 우선.
  **ML 진단기가 나와도 성능 비교 기준(baseline)으로 계속 유지할 것.**
  같은 `IDifficultyDiagnoser` 인터페이스로 `MLDiagnoser` 등을 만들면 교체 가능.

**대사 데이터**
- `NPCTipDialogueSet`(ScriptableObject): `DifficultyType`별 `NPCTipEntry[]` 목록.
  기획자가 인스펙터에서 코드 수정 없이 대사 추가 가능.
- `NPCTipEntry`(대화 한 편, weight 보유) → `NPCTipNode`(대사 한 줄) → `NPCTipChoice`(선택지,
  `nextNodeId`로 분기). `nextNodeId`가 비어있으면 해당 선택지에서 대화 종료.
- `WeightedRandomPicker`: `BossPatternController.GetRandomPatternByWeight`와 같은 누적가중치
  알고리즘을 재사용 가능한 제네릭 형태로 일반화. `NPCTipDialogueSet.PickEntry`가 가중치 랜덤 +
  직전 entryId 반복 회피에 사용.
- `NPCTipTextFormatter`: 대사 템플릿의 `{toolGrade}`, `{deathCount}`, `{questName}` 치환.

**대화 흐름(FSM)** — Player/Monster FSM과 동일한 모양(Enter/Exit, StateManager + ChangeState)
- `INPCDialogueState` + `NPCDialogueStateManager`: Idle → ShowingLine → WaitingChoice → Ended
- `NPCTipDialoguePresenter`(싱글톤): 진단 → 대사 선택 → FSM 진행 → UI 호출 → 로그 기록을 조율.
  `NPCDialogueStateManager`와 같은 GameObject에 있어야 함(`[RequireComponent]`).

**UI 연동** — 기존 `NPCInteractionUI`의 "대화" 버튼을 재활용(기존 OpenAI 인사말 기능은 대체됨).
`npcNameText`/`dialogueText`를 그대로 쓰고, 타이핑 효과 코루틴과 선택지 버튼(2~4개,
`tipChoiceButtons`)만 추가. 진단/대사 데이터가 없으면 기존 고정 대사 랜덤 출력으로 폴백.

**로깅 연동** — `PlayLogManager.RecordTipDialogue(type, confidence, entryId, nodeId, choiceText)`가
`tip_dialogue_log.csv`에 진단 결과/확신도/보여준 대사 ID/선택지를 기록 (`sessionId`로
`play_snapshots.csv`와 조인 가능).

### 3단계 — 난관 분류 모델 학습 파이프라인 (`ml/`, Unity 바깥의 Python 프로젝트)

- Unity가 쌓은 `play_snapshots.csv` + `session_labels.csv`를 읽어 RuleBasedDiagnoser(C# 이식) /
  DecisionTree / MLP(PyTorch) 세 모델을 학습·비교하고, MLP를 ONNX로 내보냄.
- `ml_pipeline/config.py`의 `FEATURE_ORDER`(18개, 고정 순서)와 `DifficultyRuleConfig` 기본값은
  각각 C# `PlayLogEntry`/`DifficultyRuleConfig.cs`와 반드시 동기화되어야 함 — 한쪽을 바꾸면
  반대쪽도 같이 바꿀 것.
- 산출물(`ml/artifacts/<run>/onnx/`): `difficulty_mlp.onnx`, `preprocessing.json`(featureOrder,
  scalerMean/Std, classOrder, zoneCategories), `test_vectors.json`(Python-Unity 일치성 테스트용
  샘플+기대 확률). 자세한 사용법은 `ml/README.md` 참고.
- **가짜(synthetic) 데이터 전용 실행**: `python scripts/run_all_demo.py` — 파일명/로그에
  "테스트용" 표시가 명확히 남고 결과는 `artifacts/synthetic_demo/`에만 저장됨. 실전 배포 금지.

### 4단계 — Unity 로컬 ML 추론 (`Assets/Script/NpcTip/Ml/`)

패키지: **Inference Engine**(표시명은 "Sentis"로 재변경됨, 패키지 ID `com.unity.ai.inference`,
네임스페이스 `Unity.InferenceEngine`, Unity 6 이상 필요). Package Manager에서
`com.unity.ai.inference`로 설치. 완전 로컬 추론이라 네트워크 호출 없음.

- **`DifficultyModelMetadata`**: 3단계 `preprocessing.json`을 `JsonUtility`로 그대로 읽음
  (`featureOrder`, `zoneCategories`, `classOrder`, `scalerMean`, `scalerStd`). `itemGradeToOrdinal`은
  JSON에서 안 읽고 기존 `ItemGrade` enum 순서를 그대로 재사용(둘 다 Normal=0 순서로 이미 일치).
- **`DifficultyFeatureBuilder`**(static): `ml_pipeline/data_prep.py`의 파생변수+정규화를 그대로
  이식. `metadata.featureOrder`를 그대로 따라가므로 Python 쪽 피처 순서/개수가 바뀌어도
  이 코드는 안 건드려도 됨. `MLDiagnoser`와 Unity EditMode 테스트가 공통으로 사용.
- **`MLDiagnoser`**(`IDifficultyDiagnoser, IDisposable`): 생성자에서 `ModelAsset`+`Worker`를
  1회 로드(`BackendType.CPU`). `Diagnose()`는 추론 실패/모델 없음/확신도 미달(`mlMinConfidence`,
  인스펙터 조절) 시 생성자로 주입받은 `RuleBasedDiagnoser`로 자동 대체. `Dispose()`에서 Worker 해제.
- **`DiagnosisMode`**(Rule/ML/Compare/AB) — `NPCTipDialoguePresenter` 인스펙터에서 선택.
  Compare 모드는 두 진단기를 모두 돌려 `PlayLogManager.RecordDiagnosisComparison(...)`으로
  `diagnosis_comparison_log.csv`에 기록하고, 실제 대사는 ML 결과로 표시. AB 모드는 5단계 참고.
- **일치성 테스트**: `Assets/Tests/Editor/MLDiagnoserParityTests.cs` (Test Runner, EditMode).
  `Assets/MLModels/DifficultyDiagnosis/`의 `test_vectors.json`(Python이 계산한 기대 확률)을
  `DifficultyFeatureBuilder` + 실제 ONNX 추론으로 재현해서 오차 0.02 이내로 일치하는지 확인.
  asmdef 없이 `Assets/Tests/Editor/`(폴더명만으로 암시적 Editor 어셈블리) 방식 — 커스텀
  asmdef의 `"Assembly-CSharp"` 문자열 참조가 이 환경에서 안 먹혀서 이 방식으로 우회함.

### 5단계 — 플레이테스트 평가 + 분석 (`Assets/Script/NpcTip/FollowUp/`, `Assets/Script/PlayLog/SessionEndSurveyUI.cs`, `ml/`)

**게임 내 평가**
- **`TipFollowUpManager`**(싱글톤): `NPCTipDialoguePresenter.StartTipDialogue()`가 팁을 준
  직후 `OnTipGiven(type, entryId, baselineSnapshot)`을 호출해주면, 새 팁이 올 때까지(최신 팁
  하나만 추적) 두 코루틴을 돌림:
  - 만족도 설문: `feedbackDelaySec`(기본 180초) 뒤 `TipFeedbackUI` 팝업(1~5점+건너뛰기, 무응답
    15초면 자동 건너뜀) → `tip_feedback_log.csv`
  - 자동 해결 추적: `resolutionCheckIntervalSec`(기본 30초)마다 `DifficultyResolutionChecker`로
    baseline과 새 스냅샷을 비교(장비 등급 상승/사망 빈도 감소/구역 이동/처치 수 증가 등 유형별
    휴리스틱), 해결 시점 또는 `resolutionCheckMaxWaitSec`(기본 600초) 타임아웃 시
    `tip_resolution_log.csv`에 기록.
- **A/B 테스트**: `DiagnosisMode.AB`를 고르면 `NPCTipDialoguePresenter.Start()`에서 세션당 1회
  Rule/ML을 50/50 무작위 고정 배정(`effectiveMode`)하고 `ab_test_assignment.csv`에 기록.
- **세션 종료 설문**: `SessionEndSurveyUI`가 `Application.wantsToQuit`를 가로채서 종료 직전
  DifficultyType 5지선다(자유서술 아님 — 3단계 라벨 검증 규칙과 맞추기 위함) 팝업을 띄우고,
  답변을 기존 `PlayLogManager.SetSessionEndLabel()`에 넣은 뒤 실제 종료 진행. 1단계 파일은
  수정 없음(이미 있던 훅을 그대로 사용).

**Python 분석** (`ml/ml_pipeline/playtest_analysis.py`, `ml/ml_pipeline/playtest_report.py`)
- `data/playtest/<테스터명>/`에 모은 여러 테스터의 CSV를 재귀로 병합(`load_all_logs`) →
  `ab_test_assignment.csv`로 세션→그룹 매핑 → Rule vs ML 그룹별 만족도(Mann-Whitney U),
  해결률(Fisher's exact test), 해결 시간(Mann-Whitney U) 비교. 표본이 `MIN_RECOMMENDED_N`(30)
  미만이면 보고서에 자동으로 경고 문구 삽입.
- `diagnosis_comparison_log.csv`에서 Rule≠ML 사례를 뽑아 발표용 표로 추출.
- 산출물: `artifacts/<run>/playtest_report/summary.md` + PNG 3종(만족도/해결률/해결시간).
- `ml_pipeline/plot_style.py`: matplotlib 차트에 한글을 쓰면 기본 폰트(DejaVu Sans)에
  한글 글리프가 없어서 깨지므로, `configure_korean_font()`로 Malgun Gothic 등을 적용
  (3단계 차트 모듈들도 예방 차원에서 같이 적용해둠).
- **가짜 데이터 전용 실행**: `python scripts/run_playtest_analysis_demo.py`.

**모델 교체 방법** (재학습 후):
1. `ml/scripts/export_onnx.py --run-name <새 run>` 실행 → `artifacts/<새 run>/onnx/`에
   `difficulty_mlp.onnx`, `preprocessing.json`, `test_vectors.json` 생성.
2. 이 3개 파일을 `Assets/MLModels/DifficultyDiagnosis/`에 덮어쓰기.
3. Unity Test Runner(EditMode)에서 `MLDiagnoserParityTests`가 통과하는지 확인.
4. `featureOrder`(피처 개수/순서)가 바뀌었다면 C# 수정 없이도 `DifficultyFeatureBuilder`가
   JSON 기준으로 알아서 맞춰 돌아감 — 단, 피처 "이름"이 바뀌었다면(`Dictionary` 키가 안 맞으면)
   `DifficultyFeatureBuilder.Build()`에 해당 파생 변수 계산을 추가해야 함.

### 에디터 설정 참고

- **Inference Engine 패키지 설치가 항상 최우선**: 이게 없으면 `Unity.InferenceEngine`을 쓰는
  모든 스크립트가 컴파일 에러를 내서 프로젝트 전체가 "cannot find script" 상태가 됨.
- 샘플 데이터: `Tools > PlayLog > Create Sample Tip Dialogue Data` 메뉴로
  `Assets/SO/NpcTip/`에 `DifficultyRuleConfig.asset`, `NPCTipDialogueSet.asset` 자동 생성
  (난관 유형별 샘플 대사 2개씩 포함).
- `PlayLogManager`, `NPCTipDialoguePresenter`(+`NPCDialogueStateManager`)는 씬에 빈 GameObject로
  각각 배치하고, `NPCTipDialoguePresenter`에는 위 두 ScriptableObject +
  `Assets/MLModels/DifficultyDiagnosis/`의 `difficulty_mlp.onnx`(ModelAsset)/`preprocessing.json`
  (TextAsset)을 인스펙터에 연결.
- `NPCInteractionUI`에 `tipChoiceButtons`(Button 3개, 자식에 TMP_Text)와
  `endTipDialogueButton`(대화 종료하기, TipChoice4 자리) 연결 필요.
- **5단계**: 씬에 `TipFeedbackUI`(작은 팝업 패널 + 1~5점 버튼 5개 + 건너뛰기 버튼),
  `TipFollowUpManager`(빈 GameObject), `SessionEndSurveyUI`(팝업 패널 + DifficultyType
  5개 버튼 + 건너뛰기 버튼)를 배치. 플레이테스트 빌드에서는
  `NPCTipDialoguePresenter`의 `Diagnosis Mode`를 `AB`로 설정.
