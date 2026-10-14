# Monster AI recorded observations

Input sessions: 1; resolved action records: 101; battle records: 2.

These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.
Missing modes require additional play. Policy versions are kept separate. No values are invented.

Source sessions:

- C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_231635_5ff3174160e344df90963409c2c41fb9

## Completed battles

| mode | cooperation | squadPolicyVersion | completedBattles | meanDirectDamageToPlayer | meanBattleSeconds | meanOverlapSeconds | meanSurroundSeconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | 2 | 1 | 22 | 35.634 | 0.000 | 0.000 |
| Baseline | True | 2 | 1 | 36 | 36.465 | 0.000 | 0.000 |

## Attack observations (rates are 0..1)

| mode | cooperation | learningSource | squadPolicyVersion | action | selected | releasedResolved | damageHitRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | RegisteredTest | 2 | Melee | 1 | 1 | 1.000 |
| Baseline | False | RegisteredTest | 2 | Projectile | 27 | 26 | 0.000 |
| Baseline | False | RegisteredTest | 2 | Circle | 17 | 13 | 0.077 |
| Baseline | False | RegisteredTest | 2 | Charge | 10 | 7 | 0.000 |
| Baseline | True | RegisteredTest | 2 | Melee | 0 | 0 | N/A |
| Baseline | True | RegisteredTest | 2 | Projectile | 22 | 22 | 0.000 |
| Baseline | True | RegisteredTest | 2 | Circle | 20 | 19 | 0.158 |
| Baseline | True | RegisteredTest | 2 | Charge | 4 | 3 | 0.000 |

## Formation decisions

| mode | cooperation | squadPolicyVersion | formation | decisionCount | formationEntries |
| --- | --- | --- | --- | --- | --- |
| Baseline | True | 2 | Pair | 2 | 2 |
| Baseline | True | 2 | Solo | 2 | 1 |
| Baseline | True | 2 | Surround | 22 | 2 |
| Baseline | True | 2 | Triangle | 6 | 3 |
