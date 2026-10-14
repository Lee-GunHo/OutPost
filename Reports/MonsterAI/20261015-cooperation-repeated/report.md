# Monster AI recorded observations

Input sessions: 1; resolved action records: 124; battle records: 3.

These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.
Missing modes require additional play. Policy versions are kept separate. No values are invented.

Source sessions:

- C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_232641_79ed80cc96fc47049761bf7b042f4e39

## Completed battles

| mode | cooperation | squadPolicyVersion | completedBattles | meanDirectDamageToPlayer | meanBattleSeconds | meanOverlapSeconds | meanSurroundSeconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | 2 | 1 | 24 | 26.603 | 0.000 | 0.000 |
| Baseline | True | 2 | 2 | 37.500 | 34.579 | 0.000 | 0.000 |

## Attack observations (rates are 0..1)

| mode | cooperation | learningSource | squadPolicyVersion | action | selected | releasedResolved | damageHitRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | RegisteredTest | 2 | Melee | 0 | 0 | N/A |
| Baseline | False | RegisteredTest | 2 | Projectile | 21 | 21 | 0.000 |
| Baseline | False | RegisteredTest | 2 | Circle | 14 | 13 | 0.154 |
| Baseline | False | RegisteredTest | 2 | Charge | 8 | 6 | 0.000 |
| Baseline | True | RegisteredTest | 2 | Melee | 2 | 2 | 1.000 |
| Baseline | True | RegisteredTest | 2 | Projectile | 40 | 38 | 0.026 |
| Baseline | True | RegisteredTest | 2 | Circle | 33 | 28 | 0.107 |
| Baseline | True | RegisteredTest | 2 | Charge | 6 | 5 | 0.200 |

## Formation decisions

| mode | cooperation | squadPolicyVersion | formation | decisionCount | formationEntries |
| --- | --- | --- | --- | --- | --- |
| Baseline | True | 2 | Pair | 4 | 4 |
| Baseline | True | 2 | Regroup | 2 | 1 |
| Baseline | True | 2 | Solo | 4 | 2 |
| Baseline | True | 2 | Surround | 34 | 3 |
| Baseline | True | 2 | Triangle | 8 | 5 |
