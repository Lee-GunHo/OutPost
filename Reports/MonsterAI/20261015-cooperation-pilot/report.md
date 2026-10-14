# Monster AI recorded observations

Input sessions: 1; resolved action records: 73; battle records: 2.

These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.
Missing modes require additional play. Policy versions are kept separate. No values are invented.

Source sessions:

- C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_230945_5d4a0098eb754fb1b75e5270d2431e93

## Completed battles

| mode | cooperation | squadPolicyVersion | completedBattles | meanDirectDamageToPlayer | meanBattleSeconds | meanOverlapSeconds | meanSurroundSeconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Training | False | 2 | 1 | 12 | 27.480 | 0.000 | 0.000 |
| Training | True | 2 | 1 | 8 | 30.901 | 0.000 | 0.000 |

## Attack observations (rates are 0..1)

| mode | cooperation | learningSource | squadPolicyVersion | action | selected | releasedResolved | damageHitRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Training | False | RegisteredTest | 2 | Melee | 0 | 0 | N/A |
| Training | False | RegisteredTest | 2 | Projectile | 19 | 16 | 0.000 |
| Training | False | RegisteredTest | 2 | Circle | 12 | 10 | 0.100 |
| Training | False | RegisteredTest | 2 | Charge | 8 | 7 | 0.000 |
| Training | True | RegisteredTest | 2 | Melee | 0 | 0 | N/A |
| Training | True | RegisteredTest | 2 | Projectile | 16 | 14 | 0.071 |
| Training | True | RegisteredTest | 2 | Circle | 15 | 10 | 0.000 |
| Training | True | RegisteredTest | 2 | Charge | 3 | 3 | 0.000 |

## Formation decisions

| mode | cooperation | squadPolicyVersion | formation | decisionCount | formationEntries |
| --- | --- | --- | --- | --- | --- |
| Training | True | 2 | Pair | 3 | 2 |
| Training | True | 2 | Regroup | 1 | 1 |
| Training | True | 2 | Solo | 2 | 1 |
| Training | True | 2 | Surround | 21 | 3 |
| Training | True | 2 | Triangle | 7 | 3 |
