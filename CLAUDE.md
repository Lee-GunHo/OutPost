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
`NPCInteractionUI`는 싱글톤이 아님 — 역할별 NPC 프리팹(Dialogue/Shop/BattleNPC_Main)마다 버튼 구성이 다른 UI를
하나씩 갖고, `NPCPresenter`가 자기 자식 UI를 엶(없으면 `Instance`). `Instance`는 "지금(마지막으로) 열린 UI"라서
`NPCTipDialoguePresenter`/`UIPresenter`는 그대로 `Instance`를 씀.

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
- **`DiagnosisMode`**(Rule/ML/Compare) — `NPCTipDialoguePresenter` 인스펙터에서 선택.
  Compare 모드는 두 진단기를 모두 돌려 `PlayLogManager.RecordDiagnosisComparison(...)`으로
  `diagnosis_comparison_log.csv`에 기록하고, 실제 대사는 ML 결과로 표시.
- **일치성 테스트**: `Assets/Tests/Editor/MLDiagnoserParityTests.cs` (Test Runner, EditMode).
  `Assets/MLModels/DifficultyDiagnosis/`의 `test_vectors.json`(Python이 계산한 기대 확률)을
  `DifficultyFeatureBuilder` + 실제 ONNX 추론으로 재현해서 오차 0.02 이내로 일치하는지 확인.
  asmdef 없이 `Assets/Tests/Editor/`(폴더명만으로 암시적 Editor 어셈블리) 방식 — 커스텀
  asmdef의 `"Assembly-CSharp"` 문자열 참조가 이 환경에서 안 먹혀서 이 방식으로 우회함.

### 5단계 — 자체 테스트 평가 + 분석 (`Assets/Script/NpcTip/FollowUp/`, `ml/`)

외부 플레이테스터를 모을 시간/인원이 없어 **게임 내 설문(팁 만족도 `TipFeedbackUI`, 세션 종료
`SessionEndSurveyUI`)과 A/B 테스트(`DiagnosisMode.AB`)는 제거함**. 팀원 자체 테스트 로그만으로 평가.

**게임 내 평가**
- **`TipFollowUpManager`**(싱글톤, `System_Manager` 프리팹): `NPCTipDialoguePresenter.StartTipDialogue()`가
  팁을 준 직후 `OnTipGiven(type, entryId, baselineSnapshot)`을 호출해주면, 새 팁이 올 때까지(최신 팁
  하나만 추적) 자동 해결 추적 코루틴을 돌림: `resolutionCheckIntervalSec`(기본 30초)마다
  `DifficultyResolutionChecker`로 baseline과 새 스냅샷을 비교(장비 등급 상승/사망 빈도 감소/구역 이동/
  처치 수 증가 등 유형별 휴리스틱), 해결 시점 또는 `resolutionCheckMaxWaitSec`(기본 600초) 타임아웃 시
  `tip_resolution_log.csv`에 기록.
- Rule/ML 비교가 필요하면 `Diagnosis Mode`를 `Compare`로 두고 플레이(4단계 `diagnosis_comparison_log.csv`).
- `PlayLogManager.SetSessionEndLabel()`(1단계 훅)은 남아 있지만 이를 호출하던 설문 UI는 없음.

**Python 분석** (`ml/ml_pipeline/playtest_analysis.py`, `ml/ml_pipeline/playtest_report.py`)
- `data/playtest/<이름>/`에 모은 팀원들의 CSV를 재귀로 병합(`load_all_logs`) →
  `tip_resolution_log.csv`를 **난관 유형별** 해결률/평균 해결 시간으로 집계(그룹 간 통계 검정 없음).
  전체 표본이 `MIN_RECOMMENDED_N`(30) 미만이면 보고서에 자동으로 경고 문구 삽입.
- `diagnosis_comparison_log.csv`에서 Rule≠ML 사례를 뽑아 발표용 표로 추출.
- 산출물: `artifacts/<run>/playtest_report/summary.md` + PNG 2종(유형별 해결률/해결 시간) + `mismatched_cases.csv`.
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

### 6단계 — 조건 기반 조언 팁 + 진행도 추적 + 전투 동료 (`Assets/Script/NpcTip/Advice/`, `Assets/Script/Progress/`, `Assets/Script/companion/`)

플레이테스트 데이터를 모을 수 없어 추가한 순수 규칙 기반 경로. 수치는 전부 인스펙터 값.

- **`ProgressTracker`**(싱글톤, 씬 배치 불필요 — `RuntimeInitializeOnLoadMethod`로 자동 생성): 사망 횟수
  (누적/RecentDeath), NPC 발견 여부(`NPCRole` 이름), 상점 아이템별 수령 개수를 세고 `progress_save.json`에
  즉시 저장. 퀘스트 상태(QuestSaveManager + 인벤토리 보유량), 처치 수(GameProgressManager), 도구 등급은
  읽어오기만 함. `BuildSnapshot()` → `ProgressSnapshot`(순수 데이터).
- **`AdviceTipSelector.Select(snapshot, tips, defaultText)`**: UI/MonoBehaviour와 분리된 순수 함수.
  조건(`TipCondition` = kind + op + targetId + value, 클래스 계층 없음)을 모두 만족하는 팁 중 priority 최대,
  동률이면 목록 앞, 없으면 기본 대사. `{combatNpcHint}` 토큰이 든 팁은 전투 NPC 발견 후/힌트 계산 실패 시
  코드에서 강제로 제외.
- **`AdviceTipSet`**(ScriptableObject): 팁 목록·기본 대사·도구 등급 itemID 목록·힌트 거리 단계.
  프리젠터 인스펙터 → `Resources/NpcTip/AdviceTipSet` → 코드 기본값(`AdviceTipDefaults`) 순으로 찾음.
  에셋 생성은 `Tools > NpcTip > Create Advice Tip Set Asset`.
- **연결**: `NPCTipDialoguePresenter.StartTipDialogue()`가 `NPCData.Role == Advice`인 NPC면 조언 팁을 먼저
  시도하고, 기본 대사가 나온 경우에만 기존 난관 진단 대사(2~5단계)로 넘어감(`useDiagnosisDialogueAsDefault`).
  기본 팁 목록은 퀘스트 1~3의 모든 상태를 덮기 때문에 조언 NPC에서는 진단 대사가 사실상 나오지 않음.
- **퀘스트 연결**: `NPCData.questChain` 순서대로 한 NPC가 퀘스트를 줌. `NPCModel`은 상태를 캐시하지 않고
  `QuestSaveManager`를 그대로 읽음.
- **전투 동료 FSM**: Player FSM과 같은 모양(`ICompanionState`, `CompanionStateManager`). 상태 분류는
  `CompanionDecision.Decide()`(순수 함수) 한 곳에서만 결정. Idle/Follow/Chase/Attack만 구현
  (몬스터가 플레이어/넥서스만 공격해 동료는 피해를 받지 않으므로 Retreat/Down은 없음).
- **검증**: `Assets/Tests/Editor/AdviceTipSelectorTests.cs`(EditMode), 디버그 창 `Tools > NpcTip > Advice Tip Debugger`.

### 상호작용 안내 (F키) + 외곽선 (`Assets/Script/interaction/`)

- 예전엔 상자/제작대/NPC마다 Screen Space Overlay Canvas(`Canvas_InteractionMark`/`Canvas_NPCTrigger`)가 붙어 있었음 →
  씬에 하나뿐인 **`InteractionPromptUI`**(프리팹 `Resources/Interaction/InteractionPrompt`)가 대상 화면 테두리 오른쪽(밖으로 나가면 왼쪽)에
  "[F] 이름 / 눌러서 ○○"를 띄우는 방식으로 대체.
- **`InteractionFocusController`**: `PlayerPresenter.Awake`가 자동으로 붙임. 매 프레임 `PlayerPresenter.FindClosestInteractable()`
  (TryInteract와 공용 — F키가 실제로 먹힐 대상과 항상 같음) 하나에만 외곽선+안내 표시. UI 열림/사망 시 숨김.
- **`InteractionHighlight`**: 대상에 자동 부착. 렌더러마다 자식 렌더러(`__InteractionOutline`)를 만들어
  `Resources/Interaction/InteractionOutline.shader`(뒷면 + 법선 방향 화면 픽셀 두께 확장, URP)로 흰 테두리를 그림.
  큐브 모서리가 끊기지 않게 위치별 평균 법선 메시를 만들어 씀(Read/Write 꺼진 메시는 원본 사용).
- 이름/행동 문구: `IInteractionPromptSource`(CanInteract/InteractionDisplayName/InteractionActionText).
  Chest=인스펙터 값(상자/열기), 제작대=`CraftingTableModel.TableName`/사용하기, NPC=NPCName/대화하기.
  `CanInteract`가 false(트리거 범위 밖)인 대상은 F키 대상에서도 제외됨.
- 메뉴: `Tools > Interaction > Create Interaction Prompt Prefab`(DungGeunMo 폰트로 프리팹 생성),
  `Tools > Interaction > Remove Old F-Key Canvases From Prefabs`(`Assets/Prefab` 아래 예전 Canvas 삭제).
  각 스크립트의 `interactionMark` 필드/Show·HideInteractionMark는 남아 있지만 Canvas 삭제 후엔 아무 동작 안 함.

### NPC 외형 (`Assets/Editor/NPC/NPCVisualBuilder.cs`, `Assets/FBX/NPC/`)

- 플레이어 인게임 모델은 `player.fbx`가 아니라 리깅된 `Assets/FBX/Monster/Animation/CHA A.fbx`(Player_Main의 `PlayerWalkingVisual`).
  NPC도 같은 모델을 `NPCVisual` 자식으로 붙이고 루트 캡슐 MeshRenderer는 끔(Player_Main과 같은 방식).
  크기/위치는 씬에 놓인 `Player_Main Variant`의 `PlayerWalkingVisual`(스케일 1.949)을 읽어서 맞춤 — 플레이어 크기를 바꾸면 메뉴만 다시 실행.
- 대화 NPC = 청록 모자/보라 옷(`NPC_Dialogue.mat`), 전투 NPC = 빨간 모자/남색 옷(`NPC_Battle.mat`) — 플레이어 텍스처에서
  헬멧(UV 마스크)과 초록 옷만 색을 바꾼 사본. 상점 NPC = 플레이어 텍스처 그대로 + 큐브로 만든 표지판(`ShopSign`).
- 메뉴 `Tools > NPC > Build NPC Visuals`로 세 프리팹에 적용(다시 실행하면 지우고 새로 만듦). 애니메이터는 플레이어와 같은
  `PlayerLocomotion`(기본 Idle), 전투 동료는 `CompanionPresenter.LateUpdate`가 위치 변화로 `IsWalking`을 켬.
- `InteractionHighlight`는 꺼진 Renderer에 외곽선을 만들지 않음(숨긴 캡슐 외곽선 방지).

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
- **5단계**: `TipFollowUpManager`(System_Manager 프리팹에 포함)만 있으면 됨. 설문 팝업은 제거됨.

### 아이템/제작 데이터 — 엑셀 임포터 (`Assets/Editor/Crafting/`)

- **원본은 프로젝트 루트의 `아이템.xlsx`**. 수정 후 `Tools > Crafting > Import Items From Excel` 실행
  (여러 번 실행해도 같은 에셋을 덮어씀). `XlsxSheetReader`가 xlsx(zip+XML)를 직접 읽어서 외부 라이브러리 없음.
- 아이템: 이름으로 기존 ItemData 매칭(공백/기호 무시), 이름이 바뀐 행은 같은 itemID로 매칭, 없으면 생성.
  ID/타입/스탯/설명/최대 스택만 덮어쓰고 아이콘·프리팹 연결은 유지. 엑셀 `food` → `ItemType.Food`.
- Recipe 열 `Potato1,Wheat1` = 재료명+개수 → `Assets/Script/crafting/Data/Recipes/<결과>Recipe.asset`.
- Craftingbox Type 열: `Normal`=손 제작(`CraftingPresenter.handRecipes`), `Wood/Copper/Silver/Gold`=해당 등급 제작대에만 넣고,
  각 제작대 프리팹의 `CraftingTableModel.lowerTierTable`이 바로 아래 등급을 가리킴. 제작 UI 제목 옆 ◀▶로
  금→은→구리→나무 화면을 오가며 하위 레시피 제작(코어키퍼 방식, 열었던 제작대보다 위로는 불가). `Pot`=항아리(요리대).
- 제작 UI(`CraftingView`): 3열×2줄 보이는 세로 스크롤. 씬에서 크기가 덮어써져 있어 레이아웃은 Awake에서 코드로 잡음.
- 제작대 4종 + 항아리 설치 프리팹을 `Assets/Prefab/Crafting/Stations/`에 생성(큐브 + 아이콘 텍스처 + Chest의
  F키 표시 복사)하고 ItemData.placeablePrefab 연결, GameScene의 `SeedMapModel.placeableBlockItems`에 등록.
- itemID는 세이브 파일에 저장되므로 엑셀에서 ID를 바꾸면 기존 세이브의 아이템이 다른 아이템으로 읽힘.

### 농사 시스템 (`Assets/Script/Farming/`)

- 흐름: 괭이(`ToolType.Hoe`)로 빈 땅 좌클릭 → 밭 / 씨앗 들고 빈 밭 우클릭 → 심기 / 플레이 시간(초) 기준 성장 /
  작물 칸 좌클릭(공격) → 수확(다 자람: 작물 1~3 + 씨앗 1~2, 덜 자람: 씨앗 1) / 빈 밭을 괭이로 다시 치면 땅으로.
- **`FarmManager`**: 씬 배치 불필요 — ChunkManager가 있는 씬이 열리면 자동 생성. 설정은
  `Resources/Farming/FarmSettings.asset`(작물 목록, 드롭 프리팹, 밭 금지 레이어, 색, 자동 저장 주기).
  저장 불러오기는 월드 시드 확정 후인 첫 Update에서.
- 수확/갈기는 `PlayerPresenter.ExecuteMousePriorityAction`이 공격 전에 `FarmManager.TryHandlePrimaryAction`을
  호출해 처리. **마우스가 가리키는 바닥(y=0) 칸만** 대상(몬스터 공격 중 사이의 작물이 수확되지 않게).
  우클릭 심기와 파란/빨간 미리보기는 `FarmInputController`(FarmManager가 자동으로 붙임).
- **`CropData`**(작물별 SO): 씨앗/작물 아이템, growSeconds, stageCount, 수확량 범위, 색, 선택적 stagePrefabs
  (비우면 `FarmPlotView`가 줄기+열매를 큐브로 만듦). 규칙(단계/수확량)은 순수 함수 `FarmRules` →
  `Assets/Tests/Editor/FarmRulesTests.cs`.
- 저장: `FarmSaveStore` → `farm_{seed}.json`(작물은 씨앗 itemID로 저장). `GameSaveModel.DeleteAllProgress`에 포함.
- 밭 칸에는 블록 설치 불가(`BlockPlacementController`). 나무를 베면 `RandomItemDrop`(Prefab_Tree)으로 20% 확률 씨앗.
- 괭이 4종(엑셀 Itemtype `Hoe`, ID 28/29/39/49, 나무/구리/은/금 제작대) → 임포터가 Weapon + `ToolType.Hoe`로 생성.
  한 번에 가는 범위는 `FarmSettings.hoeAreas`(나무 1x1, 구리 1x3 일자, 은 3x3, 금 5x5, 목표 칸 중심·바라보는 방향 기준,
  `FarmRules.GetAreaCells`). 수확/빈 밭 되돌리기는 항상 가리킨 한 칸.

### 효과음 + 타격감 (`Assets/Script/sound/`, `Assets/Script/combat/`)

- **`SfxPlayer`**(씬 배치 불필요, 자동 생성): `SfxPlayer.Play(SfxId)` / `PlayAt(SfxId, 위치)`. AudioSource 20개를 돌려쓰고
  재생마다 피치를 랜덤으로 흔듦(코어키퍼식). 위치 소리는 플레이어 기준 거리 감쇠 + 화면 좌우 스테레오. 전체 음량은 `SoundSettingManager`(AudioListener.volume).
- **`SfxLibrary`**(`Resources/Sound/SfxLibrary.asset`): `SfxId`별 클립 배열/볼륨/피치 범위/최소 간격. 소리 교체는 인스펙터에서 clips만 바꾸면 됨.
  `SfxId`는 int로 저장되므로 **항상 맨 뒤에 추가**.
- 현재 클립(`Assets/Music/Sfx/*.wav`)은 `Tools/Sfx/synth_sfx.py`로 합성한 임시 효과음(외부 에셋 아님). 실제 에셋으로 바꿔도 코드 수정 불필요.
- **`HitFeedback`**(static 진입점): 전투/채굴 코드는 `EnemyDamaged`/`BossDamaged`/`PlayerStruckEnemy`/`PlayerHurt`/`BlockHit`만 호출.
  히트스톱·화면 흔들림은 **플레이어가 직접 때린 경우만**(동료 공격엔 소리/숫자만). 수치는 `Resources/GameFeel/HitFeedbackSettings.asset`.
- **`GameFeelRunner`**(자동 생성): 히트스톱(timeScale 0.02 — UIState의 0/1과 구분해서 UI가 바꾼 값은 덮어쓰지 않음),
  화면 흔들림(URP begin/endCameraRendering에서 렌더 직전에만 카메라를 움직임 → `CameraManager` 추적과 충돌 없음),
  데미지 숫자(DungGeunMo, Overlay 셰이더로 벽 뒤에서도 보임), 파편(맞은 블록/몬스터 머티리얼을 쓰는 콜라이더 없는 작은 큐브).
- 블록 소리 종류는 `BreakableWallPresenter.GetSoundType()`이 나무 여부/프리팹 이름(Dirt/Stone/Copper·Silver·Gold)으로 판단.
