# Monster AI recorded observations

Input sessions: 1; resolved action records: 391; battle records: 10.

These are observations, not a controlled causal comparison. Training rows are not frozen evaluation results.
Missing modes require additional play. Policy versions are kept separate. No values are invented.

Source sessions:

- C:\Users\minsa\AppData\LocalLow\DefaultCompany\OutPost\MonsterAIResearch\20261014_202241_72d41102716848c6992b69d13f22d115\Frozen

## Completed battles

| mode | cooperation | squadPolicyVersion | completedBattles | meanDirectDamageToPlayer | meanBattleSeconds | meanOverlapSeconds | meanSurroundSeconds |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | 2 | 3 | 15.333 | 58.225 | 0.000 | 0.000 |
| Frozen | False | 2 | 2 | 24.500 | 134.841 | 0.000 | 0.000 |
| Training | False | 2 | 5 | 17.200 | 86.604 | 0.000 | 0.000 |

## Attack observations (rates are 0..1)

| mode | cooperation | naturalSpawn | squadPolicyVersion | action | selected | releasedResolved | damageHitRate |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline | False | False | 2 | Melee | 6 | 4 | 1.000 |
| Baseline | False | False | 2 | Projectile | 34 | 31 | 0.839 |
| Baseline | False | False | 2 | Circle | 23 | 20 | 0.150 |
| Baseline | False | False | 2 | Charge | 18 | 13 | 0.000 |
| Frozen | False | False | 2 | Melee | 2 | 1 | 1.000 |
| Frozen | False | False | 2 | Projectile | 48 | 48 | 0.896 |
| Frozen | False | False | 2 | Circle | 42 | 36 | 0.028 |
| Frozen | False | False | 2 | Charge | 26 | 25 | 0.000 |
| Training | False | False | 2 | Melee | 0 | 0 | N/A |
| Training | False | False | 2 | Projectile | 87 | 87 | 0.897 |
| Training | False | False | 2 | Circle | 71 | 62 | 0.032 |
| Training | False | False | 2 | Charge | 34 | 30 | 0.000 |

## Formation decisions

No recorded samples for this table yet.
