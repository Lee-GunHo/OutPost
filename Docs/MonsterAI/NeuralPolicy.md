> **?? ?? ??:** 2026-11-18?? ?? ?? ??? [??? ??? ??](OnlineNeuralLearning.md)?? ???????. ?? ONNX ?? ??? ???? ?? ??? ?? ??? ????.

# 몬스터 공격 선호도 MLP 적용

## 적용 범위

일반 몬스터의 공격 선호도 계산을 **실제로 학습한 MLP 신경망**으로 교체했다.
기존 규칙의 출력값을 정답으로 사용하는 지도학습/정책 모방(policy distillation)이며, PPO 강화학습이 아니다.
모델을 게임에 넣은 이후의 신경망 가중치는 고정된다. 플레이 중 갱신되는 기존 온라인 경험 배율과는 별개다.

실행 흐름:

1. 기존 규칙으로 후퇴/추적/공격 여부를 결정한다.
2. 사거리·시야·쿨다운·보유 공격·협동 역할 제한을 통과한 공격 후보를 수집한다.
3. MLP가 네 공격의 **선호도**를 출력한다.
4. `몬스터 기본 가중치 × MLP 선호도 × 협동 역할 보정 × 기존 온라인 학습 배율`을 계산한다.
5. 가능한 공격 사이에서 가중 확률 추첨하고 기존 FSM으로 실행한다.

MLP는 공격 확률의 일부를 계산한다. 추적, 후퇴, 경로 탐색, 공격 판정, 진형 구성까지 신경망으로 교체한 것은 아니다.
모델 학습의 목표는 원래 정책의 재현이다. 모델 적용 자체가 전투 성능 향상을 의미하지 않는다.

## 모델 계약

| 항목 | 값 |
| --- | --- |
| 모델 | MLP 4 → 32 → 32 → 4, 은닉층 ReLU, 출력 Exp |
| 학습 | PyTorch, Adam, 로그 선호도의 MSE 최소화 |
| 입력 순서 | 거리/8, (거리−근접 사거리)/8, 저체력 여부, 최근 피격 여부 |
| 출력 순서 | Melee, Projectile, Circle, Charge의 양수 선호도 |
| 유효 범위 | 거리 0~12, 근접 사거리 0~6 |
| 실행 | Unity Inference Engine 2.6.1, CPU, 공유 Worker |
| 에셋 | `Assets/Resources/MonsterCombat/monster_attack_mlp.onnx` |
| 버전 | `attack-preference-mlp-v1` |

저체력 및 최근 피격 판정은 기존 인스펙터 설정으로 계산한다. 플레이어 공격/대시 상태는 기존 온라인 학습 컨텍스트에 계속 사용되며, 이 MLP의 입력은 아니다.
학습 범위를 벗어나거나 모델 로드/추론에 실패하면 원래 규칙으로 복귀한다. 실패를 신경망 실행으로 기록하지 않는다.
신경망 입력·출력과 데이터 인코딩 계약이 바뀌면 재학습이 필요하다. 외부에서 곱하는 기본 가중치·역할 보정·온라인 배율 변경만으로는 재학습이 필요하지 않다.

## 사용 방법

프리팹에 별도로 컴포넌트를 붙이거나 모델을 드래그할 필요가 없다.
기존 `MonsterLearningPresenter`가 모델 실행기를 생성하며 Resources의 ONNX를 자동 로드한다.
`Monster AI research`의 `MonsterAIExperimentModel.useNeuralAttackPolicy` 기본값은 true다.

- 체크: MLP 선호도 사용. 문제 발생 시 원래 규칙으로 복귀.
- 해제: 원래 규칙만 사용. 신경망 적용 전후를 비교할 때 사용.
- `showOverlay`: `MLP Ready`, 실제 추론 횟수, 복귀 횟수 확인.
- `actions.csv`의 `preferenceSource`: `MLP`, `Rules`, `RuleFallback`, `RuleOutOfDomain` 구분.
- `actions.csv`의 `neuralModelVersion`: 해당 공격에 실제로 MLP 선호도를 사용한 경우 버전 기록.

`Baseline / Training / Frozen`은 **기존 온라인 배율 학습 모드**다. 신경망 on/off 스위치가 아니다.
기존 규칙과 신경망을 비교하려면 두 실험 모두 Baseline 또는 같은 Frozen 정책을 사용하고, 협동 설정·몬스터·장소를 동일하게 유지한 채 `useNeuralAttackPolicy`만 바꾼다.
별도로 Python을 실행할 필요 없이 빌드에서도 모델 추론이 실행된다. 새로하기/이어하기 및 세이브 파일 형식은 변경하지 않았다.

## 파일 역할

| 파일 | 역할 |
| --- | --- |
| `MonsterNeuralPolicyModel.cs` | 입력 계약·진단 상태·교사/복귀 계산식. Unity 의존성 없이 데이터 생성에도 사용 |
| `MonsterNeuralPolicyPresenter.cs` | 모델 자동 로드, CPU 추론, 출력 검증, 복귀 및 자원 해제 |
| `MonsterBehaviorModel.cs` | 역할 보정 및 기존 행동 규칙 유지 |
| `MonsterBehaviorPresenter.cs` | 현재 개체의 모델 입력 제공, 역할 보정 연결 |
| `MonsterCombatPresenter.cs` | 실제 추첨과 모델 출력 연결. 추첨·로그에서 같은 계산 결과 재사용 |
| `MonsterLearningPresenter.cs` | 공유 실행기 수명 관리, 온라인 학습 유지, 실행 출처 기록 |
| `MonsterLearningView.cs` | 기존 오버레이에 모델 상태 표시 |
| `Assets/Editor/MonsterNeuralPolicyVerification.cs` | Unity 실제 추론 결과와 Python 출력 비교 및 역할 보정 확인 |
| `Tools/MonsterAI/Neural/ExportDataset.cs` | 게임과 동일한 교사 계산식으로 정답 데이터 자동 생성 |
| `Tools/MonsterAI/Neural/train.py` | 학습, 검증, ONNX 내보내기, 보고서 생성 |

## 학습 및 검증 자료

`Reports/MonsterAI/neural-policy`에 다음 자료를 보관한다.

- `checkpoint.pt`: 학습된 PyTorch 가중치와 구조·시드.
- `dataset.npz`: 분리된 학습/검증/시험 입력·정답·원본 상황.
- `training_history.csv`, `training_curve.png`: 학습 및 검증 오차.
- `metrics.json`: 버전·시드·파일 SHA256·시험 결과.
- `test_predictions.csv`: 미학습 상황 4,000개의 교사 선호도와 MLP 선호도.
- `unity_probes.json`: Unity/Python 출력 일치 검증 입력 256개.
- `unity_verification.json`: Unity 검증 실행 결과. 실행 전에는 존재하지 않을 수 있다.

데이터 19,000개를 학습 12,000 / 검증 3,000 / 시험 4,000개로 나눴다. 동일 입력의 분할 간 중복은 검사한다.
검증 오차로 체크포인트를 선택하고, 마지막에 별도 시험 데이터로 평가한다.
시드는 20261118, 1,200 epoch 중 1,199 epoch의 모델이 선택되었다.

첫 모델의 시험 결과:

| 지표 | 측정값 |
| --- | ---: |
| 기본 가중치 3:2:2:2 적용 후 공격 확률 평균 절대 오차 | 0.116%p |
| 조건 경계에서 0.05 이상 떨어진 상황의 평균 오차 | 0.060%p |
| 조건 경계 주변 상황의 평균 오차 | 0.620%p |
| 단일 상황·공격의 최대 확률 오차 | 13.574%p |
| 역할 보정·가능 공격 마스크 조합별 평균 오차 중 최대 | 0.137%p |
| PyTorch와 ONNX Runtime 선호도 최대 절대 차이 | 약 0.00000668 |

위 확률 오차는 교사 정책을 얼마나 재현하는지에 대한 수치다. 명중률, 피해량, 실제 게임 성능 향상 수치가 아니다.
원래 계산의 불연속적인 거리 경계에서는 신경망이 근사하므로 오차가 커질 수 있다.

## 재현 명령

프로젝트 루트에서 PowerShell로 실행한다. 현재 작업에서는 기존 PyTorch를 재사용하는 격리 venv를 Temp에 준비했다.

```powershell
# 새 PC에서는 먼저 Python 환경을 준비한다.
python -m venv Temp/MonsterNeuralTraining
Temp/MonsterNeuralTraining/Scripts/python.exe -m pip install -r Tools/MonsterAI/Neural/requirements.txt

powershell -NoProfile -ExecutionPolicy Bypass -File Tools/MonsterAI/Neural/Export-Dataset.ps1
Temp/MonsterNeuralTraining/Scripts/python.exe Tools/MonsterAI/Neural/train.py
```

Unity에서 `Tools > OutPost > Monster AI > Verify MLP Model`로 추론 검증을 다시 실행할 수 있다.
현재 열린 씬과 무관한 격리 프로젝트에서 검증하려면 다음 명령을 사용한다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/MonsterAI/Neural/Verify-Unity.ps1
```

이 명령은 별도 Unity 프로세스를 시작하고 결과 경로를 출력한다. 프로세스 종료 후 출력한 `unity_verification.json`의 `passed`를 확인한다.
재학습 명령은 모델 및 보고서를 갱신한다. 기존 발표 자료와 비교하려면 먼저 이전 모델/보고서를 다른 폴더에 보관한다.

## 발표용 설명

“기존 공격 판단을 정답으로 삼아 MLP 신경망을 학습했습니다. 게임에서는 신경망이 거리·체력 상태·최근 피격 여부에 따른 공격 선호도를 계산하고, 협동 역할과 누적 전투 경험을 반영해 실제 공격을 선택합니다.”

신경망의 오프라인 모방학습과 기존 배율의 온라인 갱신은 각각 구분해 설명한다.
