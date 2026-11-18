# 개별 몬스터 온라인 신경망 학습 (2026-11-18)

## 이번 변경

사전 학습된 ONNX 모방 모델과 통계 평균 보상 배율을 **전투 결과로 직접 학습하는 C# MLP**로 대체했다.
별도 Python 실행, 데이터셋 생성, 사전 학습, ONNX 재출력이 필요 없다.
기존 ONNX 파일과 학습 보고서는 이전 방식의 기록으로 남겨 두었으며 현재 전투 경로에서는 호출하지 않는다.

- 모델: **7 입력 → 16 Tanh 은닉 유닛 → 공격별 Sigmoid 출력**. 일반 몬스터는 출력 4개, 학습 파라미터 196개.
- 입력: 거리/12, (거리−근접 사거리)/12, 자신의 체력 비율, 최근 피격, 플레이어 일반/공격/대시 상태(one-hot 3개).
- 출력: 각 공격이 실제 HP 피해를 줄 가능성에 대한 추정값. 네 값의 합이 1인 공격 선택 확률은 아니다.
- 학습: 사용한 공격의 실제 피해 여부를 1/0 정답으로 삼아 Binary Cross Entropy와 SGD 역전파 수행.
- 선택: 기존 상황·역할 규칙 가중치 × 신경망 학습 배율을 계산하고, 사용 가능한 공격 사이에서 정규화하여 추첨.

이 방식은 선택한 행동의 즉시 결과를 학습하는 contextual-bandit 형태의 보상 예측이다.
PPO, Q-Learning, ML-Agents는 사용하지 않으며 장기 승패를 최적화한다고 주장하지 않는다.

## 처음 시작과 실제 갱신

1. 처음에는 신경망 출력이 모두 0.5, 배율은 모두 1이다. 기존 규칙으로 공격을 선택한다.
2. 공격 선택 순간의 연속 입력과 실제 선택한 공격을 기록한다.
3. 정상적으로 끝난 공격의 실제 피해가 있으면 1, 빗나가거나 접촉했지만 피해가 없으면 0으로 학습한다.
4. 취소·중단·장애물 충돌 등 평가 제외 결과는 기존처럼 학습하지 않는다. 사용하지 않은 공격에 실패 정답을 부여하지 않는다.
5. 결과 한 건당 새 경험 1회와 재생 경험 3회, 총 4회의 작은 SGD 갱신을 수행한다. 프레임마다 학습하지 않는다.
6. 같은 종류 몬스터가 갱신된 신경망을 공유하고 **다음 공격 선택부터** 반영한다. 이미 진행 중인 공격은 바꾸지 않는다.

종류별 최근 128개 경험을 순환 저장한다. 은닉층은 공유되므로 관측한 상황과 비슷한 다른 상황의 예측에도 변화가 생길 수 있다.
단, 관측 경험이 전혀 없는 공격의 적용 배율은 1로 유지한다.

## 더 강한 적응 설정

| 항목 | 현재 설정 |
| --- | --- |
| 최대 배율 | 기존 2.0 → **2.5** |
| 최소 배율 | 기존 제한 0.65 → **0.5** |
| 경험 반영도 | `시도 횟수 / (시도 횟수 + 4)` (기존 분모 보정값 12) |
| SGD 학습률 | 기본 **0.06**, `MonsterAIExperimentModel.neuralLearningRate`에서 조정 |
| 결과당 갱신 | 최대 4회, 기울기와 파라미터 크기 제한 |

`c = 2 × (예측값 − 0.5)`일 때 배율은 `1 + c × 경험 반영도 × (c ≥ 0 ? 1.5 : 0.5)`.
배율은 0.5~2.5로 제한한다. 초기부터 극단적인 선택을 하지 않도록 경험량을 반영한다.
기존 EMA의 0.08과 신경망 SGD의 0.06은 의미가 달라 숫자만 직접 비교할 수 없다.
동일 성공·실패를 반복하는 합성 검사에서 이전보다 빠르게 배율이 변하는지 확인한다.
실제 전투 체감은 사거리·쿨다운·역할 제한 및 기본 가중치에도 영향을 받는다. 높은 배율도 사용 불가능한 공격을 활성화하지 않는다.

## 모드 / 저장

- **Training**: 자연 생성 또는 명시적으로 등록한 테스트 개체의 정상 공격 결과로 학습한다.
- **Frozen**: 저장된 신경망으로 선택하되 학습하지 않는다.
- **Baseline**: 신경망 배율을 적용하지 않고 기존 규칙을 사용하며 학습하지 않는다.
- `useNeuralAttackPolicy=false`: 규칙만 사용하며 학습도 중지한다.
- 습격 개체는 기존대로 학습하지 않는다. `wavesUseLearnedPolicy`를 켜면 저장된 경험을 선택에 적용할 수 있다.
- `monster_learning.json` schema 2에 신경망 가중치·편향·경험 횟수·재생 기록을 저장한다.
- 이어하기: 저장된 모델과 경험을 복원. 새로하기: 학습 파일과 메모리 초기화. 연구 CSV 폴더는 유지.
- schema 1 통계 파일은 연구 폴더의 `legacy_statistics_backup.json`으로 먼저 복사한 뒤 새 모델로 시작한다. 기존 통계에는 연속 입력이 없어 신경망 학습 데이터로 자동 변환하지 않는다. 백업 실패나 손상된 파일은 덮어쓰지 않고 저장을 차단한다.

## 보스 확장 준비

현재 보스의 공격 선택은 변경하지 않았다. `BossPatternController`가 계속 기존 패턴을 선택한다.
`MonsterLearningModel.GetOrCreateNetwork(policyKey, actionIds)`는 별도 키와 1~32개의 고정된 패턴 ID를 받는다.
예: 일반은 `normal/v2/{종류}`, 보스는 향후 `boss/{종류}/v1` 등 별도 키 사용.
패턴 수나 순서가 달라지면 새 버전 키가 필요하며, 잘못된 출력 순서를 재사용하지 않도록 검사한다.
추후 보스 Presenter에서 패턴 선택 직전 입력, 실제 선택 ID, 종료 후 실제 피해를 연결하면 된다.
보스 특유의 페이즈 등 입력이 필요하면 입력 계약도 별도로 버전 변경해야 한다.

## 담당 스크립트 (MVP)

| 파일 | 역할 |
| --- | --- |
| `MonsterOnlineNetworkModel.cs` | 신경망 데이터, 순전파·역전파, 배율 계산, 재생 경험 |
| `MonsterLearningModel.cs` | 종류별 모델 저장소, 액션 계약, 파일 데이터 검증 |
| `MonsterLearningPresenter.cs` | 결과 학습 요청, 저장·복원, 구형 파일 백업, 로그 |
| `MonsterCombatPresenter.cs` | 상황 입력 수집, 공격 가능 여부 필터링, 배율 적용 및 선택 |
| `MonsterCombatTelemetryModel.cs` | 공격 시작 시 입력과 종료 결과 보존 |
| `MonsterAIExperimentModel.cs` | 모드·학습률 설정 |
| `MonsterLearningView.cs` | 학습 상태 오버레이 표시 |

## 확인 방법

1. Play에서 `Monster AI research`의 모드를 Training, `Use Neural Attack Policy`를 활성화한다. 기본값은 이미 이 설정이다.
2. 자연 몬스터 또는 등록한 테스트 몬스터와 전투한다. 수동 배치만 하고 테스트 등록을 하지 않은 개체는 학습하지 않는다.
3. `Show Overlay`로 온라인 MLP 상태와 Policy version 증가를 확인한다.
4. `Flush Logs and Export Current Policy`를 실행한다. 학습은 공격 결과가 끝날 때 이미 진행되며 이 메뉴는 파일을 즉시 내보내기 위한 것이다.
5. `learning_updates.csv`의 `target`, `loss`, `predictionBefore/After`, `oldWeight/newWeight`, `version`을 확인한다.
6. `actions.csv`의 `neuralInput`에 7개 입력, `preferenceSource=OnlineMLP`, 최종 선택 확률이 기록된다. 18개 context 문자열은 기존 분석용 분류로만 남아 있고 학습 테이블로 쓰지 않는다.
7. Frozen으로 전환하면 모델이 고정된다. 비교 영상은 같은 조건에서 Baseline/Frozen으로 촬영한다.

`Demo/2 - Maximum Charge x2.5 (No Learning)`는 수동 테스트용 배율 강제 설정이다. 실제 신경망 학습 결과로 발표하면 안 된다.

## 검사

- `Tools/MonsterAI/Run-OnlineNeuralChecks.ps1`: 순수 모델의 성공·실패 적응, 상황 구분, 유형 분리, 입력 검증, 보스 확장 계약 검사.
- Unity 메뉴 `Tools > OutPost > Monster AI > Verify Online Neural Learning`: 실제 Unity JsonUtility 저장·복원 및 재학습 연속성 검사.
- `Tools/MonsterAI/Verify-OnlineNeuralUnity.ps1`: 동일 검사를 별도 임시 Unity 프로젝트에서 실행. 현재 씬과 사용자 저장 파일을 사용하지 않는다.
- 결과: `Reports/MonsterAI/online-neural/unity_verification.json`.

이 검사는 합성 경험에 대한 코드·직렬화 검증이며 실제 플레이 성능 향상 실험을 대신하지 않는다.
